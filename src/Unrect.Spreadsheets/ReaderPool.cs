using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The workbook's forward readers: a small set of positions in one file, lent out to whichever
  /// sheet store needs to read next.
  ///
  /// <para><b>Why the pool is workbook-level and not sheet-level.</b> A reader is a position in a
  /// <em>workbook</em> — it can move to a later sheet but never back to an earlier one. Owning the
  /// readers at the book means a warm spare can be opened at <c>Open</c> before any sheet has been
  /// named, a lease parked at sheet 1 row 900 can serve sheet 2 row 0 by moving forward with no
  /// open at all, and "how many file handles does this workbook hold" has one answer. The cost is
  /// that lease <em>selection</em> is a workbook-wide critical section — a short one: choosing is
  /// done under the gate, and the reading happens with the lease checked out and the gate
  /// released.</para>
  ///
  /// <para><b>What the pool does and does not buy.</b> Measured on a 1,000,000-row workbook: the
  /// pool alone buys nothing. Two unwarmed readers cost the same as one, because converting a
  /// reopen into a spare open is converting a ~5s open into a ~5s open. <em>Warming</em> is the win
  /// — the same two readers, warmed on a background task, took 19.6s against 24.6s — because a
  /// background open is one already paid for by the time it is wanted. Readers beyond demand are
  /// never opened, so a generous <c>MaxReaders</c> is not itself a cost; warming one speculatively
  /// is, which is why the warm target grows only on evidence.</para>
  ///
  /// <para>The law the counters exist to expose: <c>Reopens == passes − readers</c>, exactly and
  /// linearly. Three concurrent passes over a sheet with one reader reopen five times; with three
  /// readers, three; with six, none.</para>
  /// </summary>
  internal sealed class ReaderPool : IDisposable
  {
    private readonly object _gate = new object();
    private readonly IRowSource _source;
    private readonly ReaderLease[] _leases;
    private readonly bool _warmReaders;
    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

    private bool _disposed;
    private bool _parkedReserved;
    private int _warmTarget = InitialWarmTarget;

    private long _opens;
    private long _reopens;
    private long _spareOpens;
    private long _warmHits;
    private long _warmWaitMilliseconds;
    private long _cheapRewinds;

    /// <summary>
    /// The lead reader plus exactly one spare. The owner's policy is that nothing beyond one spare
    /// is speculative; growth past this has to be earned by evidence of demand.
    /// </summary>
    private const int InitialWarmTarget = 2;

    internal ReaderPool(IRowSource source, int maxReaders, bool warmReaders)
    {
      _source = source;
      _warmReaders = warmReaders;
      _leases = Enumerable.Range(0, maxReaders).Select(index => new ReaderLease(index)).ToArray();
    }

    internal int MaxReaders => _leases.Length;

    /// <summary>
    /// Takes ownership of an already-open cursor as the first lease — the reader
    /// <c>Workbook.Open</c> parked and then walked to the wanted sheet. It is already open, already
    /// on the right sheet and already at the right row, which is why the common single-sheet case
    /// costs one open rather than two.
    /// </summary>
    internal void Adopt(IRowCursor cursor, int sheetIndex, int cursorRow)
    {
      lock (_gate)
      {
        var lease = _leases[0];

        // The reservation should have kept this slot empty. Closing rather than overwriting means
        // that if it ever is not, the consequence is a wasted open and not a leaked file handle.
        if (lease.IsOpen)
          lease.Close();

        lease.Cursor = cursor;
        lease.SheetIndex = sheetIndex;
        lease.CursorRow = cursorRow;
        _parkedReserved = false;

        // No Opens here: the workbook counted this open when it made it.
        StartWarmers();
      }
    }

    /// <summary>
    /// Opens the reader the workbook parks at <c>Open</c>, and counts it.
    /// <para>
    /// One call rather than an open and a separate tally, because the tally is the easy half to
    /// forget and the cost is real either way: a parked reader is counted whether it goes on to be
    /// adopted as a lease or is walked to the end of the catalogue and thrown away.
    /// <see cref="ReaderPoolStatistics.Opens"/> promises every multi-second event, including the
    /// ones that bought nothing.
    /// </para>
    /// </summary>
    internal IRowCursor OpenParked()
    {
      var cursor = _source.Open();

      lock (_gate)
      {
        _opens++;

        // Slot 0 is now spoken for. Reserving it is what makes the warming policy correct by
        // construction rather than by luck of ordering: a warmer that filled this slot would be
        // thrown away by the adoption that follows, which is one wasted multi-second open and one
        // leaked handle. A slot that WILL be adopted is never a warm target.
        _parkedReserved = true;
      }

      return cursor;
    }

    /// <summary>
    /// Gives up the reservation without adopting: the parked reader was walked somewhere it cannot
    /// come back from, so the slot is free for ordinary use.
    /// </summary>
    internal void ReleaseParked()
    {
      lock (_gate)
      {
        _parkedReserved = false;
        StartWarmers();
      }
    }

    /// <summary>
    /// Begins warming spare slots up to the current target. Called at <c>Open</c> and again whenever
    /// pool pressure raises the target.
    /// </summary>
    internal void BeginWarming()
    {
      lock (_gate)
        StartWarmers();
    }

    /// <summary>
    /// Hands back a reader standing exactly at <paramref name="sheetIndex"/>,
    /// <paramref name="startRow"/>, having reported the rows it discarded getting there.
    ///
    /// <para>The choice is the whole policy. The reader FURTHEST ALONG but still at or behind the
    /// target is preferred, because it has the fewest rows left to skip — which also means a reader
    /// parked at the top is not disturbed while any other reader can do the job, and that is what
    /// leaves a chase reader available for the second pass over a band. Only when every reader
    /// stands ahead of the target does anything cost: a spare slot is opened if the pool has one
    /// left, and failing that the reader that has travelled LEAST is thrown away and re-opened, on
    /// the grounds that it is the position with the least distance banked in it.</para>
    ///
    /// <para>A backward reach served by a parked reader is a <c>CheapRewind</c>: it cost the rows
    /// between that reader and the target, and nothing else.</para>
    /// </summary>
    internal ReaderLease Borrow(int sheetIndex, int startRow, out long rowsSkipped)
      => Borrow(sheetIndex, startRow, reposition: true, out rowsSkipped);

    /// <summary>
    /// A reader, wherever it happens to stand — for work that only moves forward through sheets and
    /// does not care about the row.
    /// <para>
    /// The catalogue walk is the caller. Asking for a <em>position</em> to do it would be actively
    /// harmful: requesting row 0 of the last known sheet is a backward reach once anything has been
    /// read, and a backward reach with no reader behind it costs a multi-second open — to run a walk
    /// that never reads a row. Asking for "whatever is free" costs nothing when any reader exists,
    /// and the reader furthest along is preferred because it has the fewest sheets left to step
    /// through.
    /// </para>
    /// </summary>
    internal ReaderLease BorrowAnywhere() => Borrow(0, 0, reposition: false, out _);

    private ReaderLease Borrow(int sheetIndex, int startRow, bool reposition, out long rowsSkipped)
    {
      ReaderLease lease;
      Task? warming;
      var mustOpen = false;

      lock (_gate)
      {
        ThrowIfDisposed();

        // Every lease checked out means another sheet is mid-load; wait for one back rather than
        // failing. One reader and two sheets is a legitimate configuration, just a serial one.
        ReaderLease? chosen;
        while ((chosen = Select(sheetIndex, startRow, reposition, out mustOpen)) is null)
        {
          Monitor.Wait(_gate);
          ThrowIfDisposed();
        }

        lease = chosen;
        warming = lease.Warming;

        if (lease.IsWarm)
        {
          lease.IsWarm = false;
          _warmHits++;
        }
      }

      // Everything expensive happens with the gate released: waiting on a warmer's open, paying for
      // one ourselves, and skipping rows to reach the target. Every one of those can throw — an
      // IOException part-way through an advance is an ordinary thing for a disk to do — and a lease
      // abandoned checked out is never coming back. Once every lease has leaked that way, selection
      // returns null and callers park in Monitor.Wait with nobody left to wake them: a read that
      // failed would turn every later read into a hang. A failure must surface as the fault it is.
      try
      {
        return Acquire(lease, warming, mustOpen, sheetIndex, startRow, reposition, out rowsSkipped);
      }
      catch
      {
        Return(lease);
        throw;
      }
    }

    private ReaderLease Acquire(
      ReaderLease lease,
      Task? warming,
      bool mustOpen,
      int sheetIndex,
      int startRow,
      bool reposition,
      out long rowsSkipped)
    {
      if (warming is not null)
      {
        var waited = Stopwatch.StartNew();
        // A warm that faults is not this borrow's problem: the slot simply stays closed and the
        // open below pays for it, where a caller can attribute the failure.
        try { warming.GetAwaiter().GetResult(); } catch (Exception) { /* see Warm */ }
        Interlocked.Add(ref _warmWaitMilliseconds, waited.ElapsedMilliseconds);

        lock (_gate)
        {
          if (lease.IsWarm)
          {
            lease.IsWarm = false;
            _warmHits++;
          }

          mustOpen = !lease.IsOpen;

          if (mustOpen)
          {
            // The warm failed. This borrow pays for the open instead, and it is still a spare
            // slot being opened for the first time.
            _spareOpens++;
            _opens++;
          }
        }
      }

      if (mustOpen)
        Fill(lease);

      rowsSkipped = reposition ? lease.AdvanceTo(sheetIndex, startRow) : 0;

      return lease;
    }

    /// <summary>Returns a lease to the pool, leaving it parked where the reading left it.</summary>
    internal void Return(ReaderLease lease)
    {
      lock (_gate)
      {
        lease.InUse = false;
        Monitor.PulseAll(_gate);
      }
    }

    /// <summary>
    /// Chooses the lease to serve <paramref name="sheetIndex"/>, <paramref name="startRow"/>,
    /// counting what the choice cost. Called holding the gate.
    /// </summary>
    private ReaderLease? Select(int sheetIndex, int startRow, bool reposition, out bool mustOpen)
    {
      ReaderLease? best = null;
      ReaderLease? spare = null;
      ReaderLease? laggard = null;
      var anyAhead = false;

      foreach (var lease in _leases)
      {
        if (lease.InUse)
        {
          // A checked-out lease is being read from right now; its position is moving and it cannot
          // be handed to a second borrower. It still counts as evidence of where the pool stands.
          anyAhead |= lease.IsPast(sheetIndex, startRow);
          continue;
        }

        if (!lease.IsOpen)
        {
          // The reserved slot is not free: it is holding a place for a reader already open in the
          // workbook's hand.
          if (IsReserved(lease))
            continue;

          // A slot a warmer has already filled is free to take; a cold one costs an open. Prefer the
          // warm one whatever its place in the pool.
          if (spare is null || (lease.Warming is not null && spare.Warming is null))
            spare = lease;

          continue;
        }

        if (!reposition)
        {
          // Anywhere will do, so nothing is ever "past" the target and there is no rewind to count.
          // Furthest along wins: fewest sheets to step through from there.
          if (best is null || lease.IsAheadOf(best))
            best = lease;
        }
        else if (lease.IsPast(sheetIndex, startRow))
        {
          anyAhead = true;
        }
        else if (best is null || lease.IsAheadOf(best))
        {
          best = lease;
        }

        if (laggard is null || laggard.RowsAdvanced > lease.RowsAdvanced)
          laggard = lease;
      }

      if (best is not null)
      {
        // A parked reader served a backward reach: the cheap case the pool exists to create.
        if (anyAhead)
          _cheapRewinds++;

        best.InUse = true;
        mustOpen = false;

        return best;
      }

      if (spare is not null)
      {
        // Claimed before the warm target grows: raising it starts warmers, and a warmer landing in
        // the slot this borrow is about to fill would be overwritten. Marking it in use first is
        // what makes "a slot about to be filled is never a warm target" hold under the gate.
        spare.InUse = true;

        // Nothing could reach the target by moving forward, and a slot had to be opened: that is a
        // pool-pressure event and the evidence the warm target grows on.
        RaiseWarmTarget();

        mustOpen = !spare.IsOpen && spare.Warming is null;

        if (mustOpen)
        {
          _spareOpens++;
          _opens++;
        }

        return spare;
      }

      // Every reader is open and ahead of the target: throw away the one with the least distance
      // banked in it. If there is no such reader, every one of them is checked out and the caller
      // waits — which is contention, not pool pressure, so the warm target must not grow on it.
      if (laggard is null)
      {
        mustOpen = false;
        return null;
      }

      var victim = laggard;
      victim.InUse = true;
      victim.Close();
      RaiseWarmTarget();
      _reopens++;
      _opens++;
      mustOpen = true;

      return victim;
    }

    /// <summary>Opens a cursor into a lease that has none. Called with the gate released.</summary>
    private void Fill(ReaderLease lease)
    {
      var cursor = _source.Open();

      lock (_gate)
      {
        if (_disposed)
        {
          cursor.Dispose();
          ThrowIfDisposed();
        }

        // The same defensive close Adopt makes: if a warm did land here despite the rule above,
        // overwriting would leak its file handle. Closing costs a wasted open instead.
        if (lease.IsOpen)
          lease.Close();

        lease.Cursor = cursor;
        lease.SheetIndex = cursor.SheetIndex;
        lease.CursorRow = 0;
      }
    }

    /// <summary>
    /// Raises the warm target by one and starts warming toward it. Called holding the gate.
    /// <para>
    /// The owner's policy is "warm one spare eagerly, then only on evidence of multi-pass demand",
    /// and the literal trigger — the first reopen — turns out to be unreachable: while an unopened
    /// slot remains, selection takes the spare rather than reopening, so a reopen can only happen
    /// once every slot is already open, by which point there is nothing left to warm. The trigger is
    /// therefore the pool-pressure event itself, a spare open <em>or</em> a reopen. Both mean the
    /// pool was one reader short at that instant, which is exactly the evidence wanted.
    /// </para>
    /// </summary>
    private void RaiseWarmTarget()
    {
      _warmTarget = Math.Min(_warmTarget + 1, MaxReaders);
      StartWarmers();
    }

    /// <summary>Starts a warm for each closed slot up to the target. Called holding the gate.</summary>
    private void StartWarmers()
    {
      if (!_warmReaders || _disposed)
        return;

      // The reserved slot counts toward the target: it is about to hold the parked reader, so
      // warming another on its behalf would overshoot the "exactly one spare" policy.
      var open = _leases.Count(lease => lease.IsOpen || lease.InUse || lease.Warming is not null) + (_parkedReserved ? 1 : 0);

      foreach (var lease in _leases)
      {
        if (open >= _warmTarget)
          return;

        // InUse matters as much as IsOpen: a lease checked out to a borrower may be about to have
        // a cursor put into it by Fill, and a warm landing in the same slot would be overwritten
        // and leaked. The rule is the reservation invariant generalised — a slot that is about to
        // be filled is never a warm target.
        if (lease.IsOpen || lease.InUse || lease.Warming is not null || IsReserved(lease))
          continue;

        lease.Warming = Warm(lease, _cancellation.Token);
        open++;
      }
    }

    /// <summary>
    /// Opens a cursor for a slot on a background task, so the ~5s is already spent by the time a
    /// reach wants it.
    /// <para>
    /// A <see cref="Task"/> and a token, never a raw thread: an earlier prototype leaked a
    /// background thread holding a five-second open with no way to cancel it. An open already under
    /// way cannot be interrupted, so the completion path checks for disposal under the gate and
    /// disposes the cursor it just opened rather than parking it — which is what lets
    /// <see cref="Dispose"/> return without waiting and still promise no handle outlives its
    /// workbook.
    /// </para>
    /// <para>
    /// A warm that fails swallows its exception: a warm reader is an optimisation, and anything
    /// genuinely wrong resurfaces on the on-demand path, where a caller can attribute it.
    /// </para>
    /// </summary>
    private Task Warm(ReaderLease lease, CancellationToken cancellation) =>
      Task.Run(
        () =>
        {
          IRowCursor? cursor = null;

          try
          {
            if (!cancellation.IsCancellationRequested)
              cursor = _source.Open();
          }
          catch (Exception)
          {
            cursor = null;
          }

          lock (_gate)
          {
            lease.Warming = null;

            if (cursor is null)
              return;

            // Counted here, at the open, not where it is parked: the open happened and cost what an
            // open costs whether or not this cursor ends up with a home. Opens promises every
            // multi-second event, so the tally lives on the open path.
            _opens++;

            if (_disposed || cancellation.IsCancellationRequested || lease.IsOpen)
            {
              // Disposed underneath us, or a borrower filled this slot while the open was in
              // flight. Either way this cursor has nowhere to live and must not outlive the pool.
              cursor.Dispose();
              return;
            }

            lease.Cursor = cursor;
            lease.SheetIndex = cursor.SheetIndex;
            lease.CursorRow = 0;
            lease.IsWarm = true;
            _spareOpens++;
          }
        },
        CancellationToken.None);

    /// <summary>
    /// Completes when no warm is in flight — the hook that makes the dispose-race test deterministic
    /// rather than timing-hopeful.
    /// </summary>
    internal Task WhenWarmersIdle()
    {
      Task[] warming;

      lock (_gate)
        warming = _leases.Select(lease => lease.Warming).Where(task => task is not null).ToArray()!;

      return warming.Length == 0 ? Task.CompletedTask : Task.WhenAll(warming);
    }

    /// <summary>A snapshot of what the readers have cost, taken under the gate.</summary>
    internal ReaderPoolStatistics Snapshot()
    {
      lock (_gate)
        return new ReaderPoolStatistics(
          MaxReaders,
          _leases.Count(lease => lease.IsOpen),
          _opens,
          _reopens,
          _spareOpens,
          _warmHits,
          Interlocked.Read(ref _warmWaitMilliseconds),
          _cheapRewinds,
          _leases.Select(lease => lease.RowsAdvanced).ToArray());
    }

    /// <summary>Whether this slot is being held for the parked reader awaiting adoption.</summary>
    private bool IsReserved(ReaderLease lease) => _parkedReserved && lease.Index == 0;

    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException("Workbook", $"The workbook '{_source.Name}' has been disposed.");
    }

    /// <summary>
    /// Closes every reader. Does not wait for a warm in flight — the warm's own completion path
    /// disposes what it opened once it sees this flag, so returning promptly costs no handle.
    /// </summary>
    public void Dispose()
    {
      lock (_gate)
      {
        if (_disposed)
          return;

        _disposed = true;

        foreach (var lease in _leases)
          lease.Close();

        // Wake anyone parked waiting for a lease. Without this a borrower that arrived when every
        // lease was checked out waits forever, and the disposal check written for exactly that
        // moment never runs.
        Monitor.PulseAll(_gate);
      }

      _cancellation.Cancel();
      _cancellation.Dispose();
    }
  }
}
