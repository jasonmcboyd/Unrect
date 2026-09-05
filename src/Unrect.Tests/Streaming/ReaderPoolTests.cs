using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The readers: which one is handed to a request, what it costs when none of them will do, and
  /// when a spare is opened ahead of need.
  /// <para>
  /// A reader can move to a later row or a later sheet and never back. Every law here follows from
  /// that one constraint, and every one of them is stated in counters rather than in wall time,
  /// because an "open" against a synthetic source is free and against a workbook is five seconds.
  /// </para>
  /// </summary>
  public class ReaderPoolTests
  {
    private static FakeRowSource Sheet(int rows = 1000) => FakeRowSource.Of(rows, columns: 2);

    // --- Selection --------------------------------------------------------------------------------

    [Fact]
    public void ARequestTakesTheReaderFurthestAlongThatIsStillBehindIt()
    {
      // The whole selection policy in one arrangement. The reader at 500 has the fewest rows left
      // to skip — but the reason it is preferred is the other one: taking it leaves the reader at
      // the top parked where it is, available for the next backward reach. A "nearest reader" rule
      // would consume both and have nothing left to chase with.
      var source = Sheet();
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      pool.Return(pool.Borrow(0, 500, out _));    // opens one, walks it to 500
      pool.Return(pool.Borrow(0, 0, out _));      // the first is past 0, so a spare opens at the top

      pool.Return(pool.Borrow(0, 600, out var skipped));

      Assert.Equal(100, skipped);

      var stats = pool.Snapshot();

      Assert.Equal(new long[] { 600, 0 }, stats.RowsPerReader.ToArray());
      Assert.Equal(2, stats.Opens);
      Assert.Equal(0, stats.Reopens);
    }

    [Fact]
    public void ABackwardReachAParkedReaderCanServeCostsNothingButTheRowsBetween()
    {
      // The case the pool exists to create, and the counter that says so. CheapRewinds goes UP when
      // the pool is working: it is reaches served for the price of a walk, not reaches that hurt.
      var source = Sheet();
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      pool.Return(pool.Borrow(0, 500, out _));
      pool.Return(pool.Borrow(0, 0, out _));      // a second reader, parked at the top

      pool.Return(pool.Borrow(0, 100, out var skipped));

      var stats = pool.Snapshot();

      Assert.Equal(100, skipped);
      Assert.Equal(1, stats.CheapRewinds);
      Assert.Equal(0, stats.Reopens);
      Assert.Equal(2, stats.Opens);
      Assert.Equal(2, source.Opens);
    }

    [Fact]
    public void ARequestBehindEveryReaderThrowsAwayTheOneThatHasTravelledLeast()
    {
      // When nothing can reach the target by moving forwards, something has to be reopened, and the
      // reader with the least distance banked in it is the cheapest thing to lose. The travel
      // counter survives the close, which is what makes the choice visible here at all.
      var source = Sheet();
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      pool.Return(pool.Borrow(0, 500, out _));    // reader 0 travels 500
      pool.Return(pool.Borrow(0, 100, out _));    // reader 1 opens and travels 100

      pool.Return(pool.Borrow(0, 50, out var skipped));

      var stats = pool.Snapshot();

      Assert.Equal(50, skipped);
      Assert.Equal(1, stats.Reopens);
      Assert.Equal(3, stats.Opens);
      // Reader 1 was the laggard: it lost its position and walked 50 rows from the top again.
      Assert.Equal(new long[] { 500, 150 }, stats.RowsPerReader.ToArray());
    }

    [Fact]
    public void AReaderOnAnEarlierSheetServesALaterSheetWithoutOpeningAnything()
    {
      // A position is (sheet, row) ordered lexicographically, not a row number — which is the whole
      // reason the pool belongs to the workbook rather than to a sheet. A reader deep in sheet 1
      // reaches the top of sheet 2 by moving forwards, so a multi-sheet parse costs no more opens
      // than a single-sheet one.
      var source = new FakeRowSource(
        new FakeSheet("One", 1000, 2),
        new FakeSheet("Two", 1000, 2),
        new FakeSheet("Three", 1000, 2));
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      pool.Return(pool.Borrow(1, 900, out _));

      pool.Return(pool.Borrow(2, 0, out var skipped));

      var stats = pool.Snapshot();

      Assert.Equal(0, skipped);            // moving to the next sheet skips no rows OF THAT SHEET
      Assert.Equal(1, stats.Opens);
      Assert.Equal(0, stats.Reopens);
      Assert.Equal(1, source.Opens);
    }

    // --- The pool law -----------------------------------------------------------------------------

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(3, 3)]
    public void Reopens_AreThePassesThatOutnumberTheReaders(int readers, int passes)
    {
      // Exact and linear: reopens = max(0, passes - readers). Measured on a million-row workbook as
      // 5/4/3/0 reopens for six passes over one/two/three/six readers, and it holds here for the
      // same reason it held there — each pass leaves its reader at the bottom, so the next pass
      // starting at the top needs a reader of its own or it pays for one.
      var source = Sheet(rows: 10);
      using var pool = new ReaderPool(source, readers, warmReaders: false);

      for (var pass = 0; pass < passes; pass++)
        for (var row = 0; row < 10; row++)
          pool.Return(pool.Borrow(0, row, out _));

      var stats = pool.Snapshot();

      Assert.Equal(Math.Max(0, passes - readers), stats.Reopens);
      Assert.Equal(passes, stats.Opens);
      Assert.Equal(passes, source.Opens);
    }

    [Fact]
    public void OpensAreTheSpareOpensAndTheReopens()
    {
      // The arithmetic identity of the statistics: every expensive event is one kind or the other,
      // and nothing is counted twice. (This pool was never handed a parked reader, so there is no
      // adopted open to add.)
      var source = Sheet();
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      foreach (var row in new[] { 500, 100, 50, 900, 10, 700 })
        pool.Return(pool.Borrow(0, row, out _));

      var stats = pool.Snapshot();

      Assert.Equal(stats.SpareOpens + stats.Reopens, stats.Opens);
      Assert.Equal(stats.Opens, source.Opens);
      Assert.True(stats.WarmHits <= stats.SpareOpens);
    }

    [Fact]
    public void AParkedReaderIsCountedWhenItIsOpenedAndNotAgainWhenItIsAdopted()
    {
      // Adoption is not an open. The workbook counts the parked reader when it pays for it, and
      // counting it again when a sheet takes it over would hide the case that matters — a parked
      // reader walked past every sheet and thrown away, which is a real open that bought nothing.
      var source = Sheet();
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      var parked = pool.OpenParked();

      Assert.Equal(1, pool.Snapshot().Opens);

      pool.Adopt(parked, 0, 0);

      Assert.Equal(1, pool.Snapshot().Opens);
      Assert.Equal(1, source.Opens);
    }

    // --- Waiting ----------------------------------------------------------------------------------

    [Fact]
    public async Task ARequestWaitsWhenEveryReaderIsCheckedOut()
    {
      // One reader and two sheets is a legitimate configuration — just a serial one. The pool waits
      // for a reader to come back rather than failing or opening past MaxReaders, because
      // MaxReaders is a promise about file handles and a wait is the only way to keep it.
      var source = new FakeRowSource(new FakeSheet("One", 1000, 2), new FakeSheet("Two", 1000, 2));
      using var pool = new ReaderPool(source, 1, warmReaders: false);

      var held = pool.Borrow(0, 0, out _);
      var waiting = OnItsOwnThread(() => pool.Return(pool.Borrow(1, 0, out _)));

      Assert.False(waiting.IsCompleted, "the second borrow must not proceed while the only reader is out");
      Assert.NotSame(waiting, await Task.WhenAny(waiting, Task.Delay(200)));

      pool.Return(held);

      await WithTimeout(waiting, TimeSpan.FromSeconds(10));
      Assert.Equal(1, source.Opens);
    }

    // --- Warming ----------------------------------------------------------------------------------

    [Fact]
    public async Task TheSlotAParkedReaderWillBeAdoptedIntoIsNeverWarmed()
    {
      // The reservation, stated in the three things it was worth making. Slot 0 is spoken for from
      // the moment the parked reader is opened, so a warmer never fills it — which means the
      // adoption cannot throw away a cursor (no leaked handle), cannot pay for an open the counters
      // never see (Opens tells the truth), and cannot turn "warm exactly one spare" into two.
      //
      // Before the reservation, whether any of that happened depended on whether a background task
      // won a race against the caller. Correct by construction beats correct by luck of ordering.
      var source = FakeRowSource.Of(rows: 100, columns: 2);
      var pool = new ReaderPool(source, 3, warmReaders: true);

      var parked = pool.OpenParked();
      pool.BeginWarming();
      await pool.WhenWarmersIdle();

      // One spare warmed, not two: the parked reader's own slot is not a candidate.
      Assert.Equal(2, source.Opens);
      Assert.Equal(2, pool.Snapshot().Opens);
      Assert.Equal(1, pool.Snapshot().SpareOpens);

      pool.Adopt(parked, 0, 0);
      await pool.WhenWarmersIdle();

      // Adoption fills the reserved slot and warming does not chase it with a third reader.
      Assert.Equal(2, source.Opens);
      Assert.Equal(2, pool.Snapshot().ReadersOpen);

      pool.Dispose();

      // The invariant that says no handle was orphaned along the way.
      Assert.Equal(source.Opens, source.Closes);
    }

    [Fact]
    public async Task EveryOpenIsCountedEvenTheOnesThatBoughtNothing()
    {
      // Opens promises every multi-second event. An open the pool paid for and then decided it had
      // no use for is exactly the one worth knowing about, so the counter is checked against the
      // source's own tally rather than against itself.
      var source = FakeRowSource.Of(rows: 1000, columns: 2);
      using var pool = new ReaderPool(source, 3, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);
      await pool.WhenWarmersIdle();

      foreach (var row in new[] { 500, 100, 20, 900, 5, 700, 3 })
        pool.Return(pool.Borrow(0, row, out _));

      await pool.WhenWarmersIdle();

      Assert.Equal(source.Opens, pool.Snapshot().Opens);
    }

    [Fact]
    public async Task GivingUpTheReservationLetsTheSlotBeWarmedLikeAnyOther()
    {
      // The other end of the reservation. A parked reader walked past every sheet can never be a
      // first lease, so the slot it was holding has to go back into ordinary use — otherwise asking
      // for the sheet names would quietly cost a reader for the rest of the workbook's life.
      var source = FakeRowSource.Of(rows: 100, columns: 2);
      var pool = new ReaderPool(source, 3, warmReaders: true);

      var parked = pool.OpenParked();
      pool.BeginWarming();
      await pool.WhenWarmersIdle();

      Assert.Equal(1, pool.Snapshot().ReadersOpen);      // the reserved slot is empty, one spare warmed

      parked.Dispose();
      pool.ReleaseParked();
      await pool.WhenWarmersIdle();

      Assert.Equal(2, pool.Snapshot().ReadersOpen);      // the freed slot is warmed like any other

      pool.Return(pool.Borrow(0, 10, out _));

      Assert.Equal(1, pool.Snapshot().WarmHits);

      pool.Dispose();

      Assert.Equal(source.Opens, source.Closes);
    }

    [Fact]
    public async Task WithoutPoolPressureExactlyOneSpareIsEverWarmed()
    {
      // The owner's policy: nothing speculative beyond one. Cheap rewinds are not evidence of
      // demand — they are the pool working — so however many of them happen, a third reader is
      // never opened on a guess.
      var source = Sheet();
      using var pool = new ReaderPool(source, 3, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);
      await pool.WhenWarmersIdle();

      Assert.Equal(2, source.Opens);
      Assert.Equal(2, pool.Snapshot().ReadersOpen);

      for (var reach = 0; reach < 5; reach++)
      {
        pool.Return(pool.Borrow(0, 50 + reach, out _));
        pool.Return(pool.Borrow(0, 10, out _));      // served by the spare: a cheap rewind
      }

      await pool.WhenWarmersIdle();

      Assert.True(pool.Snapshot().CheapRewinds > 0);
      Assert.Equal(2, source.Opens);
    }

    [Fact]
    public async Task OnePoolPressureEventWarmsOneMoreReader()
    {
      // The trigger is generalised from the owner's words to their intent. Taken literally — "warm
      // more only after the first REOPEN" — it is unreachable: while an unopened slot remains,
      // selection takes the spare instead of reopening, so a reopen cannot happen until there is
      // nothing left to warm. A pool-pressure event (a reach no parked reader could serve) is the
      // same evidence, one step earlier.
      var source = Sheet();
      using var pool = new ReaderPool(source, 3, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);
      await pool.WhenWarmersIdle();

      pool.Return(pool.Borrow(0, 50, out _));       // reader 0 to row 50
      pool.Return(pool.Borrow(0, 10, out _));       // the spare to row 10 — still no pressure
      await pool.WhenWarmersIdle();

      Assert.Equal(2, source.Opens);

      pool.Return(pool.Borrow(0, 5, out _));        // behind BOTH readers: pressure
      await pool.WhenWarmersIdle();

      Assert.Equal(3, source.Opens);
      Assert.Equal(3, pool.Snapshot().ReadersOpen);
      Assert.Equal(0, pool.Snapshot().Reopens);     // the growth is what kept this at zero
    }

    [Fact]
    public async Task TheWarmTargetNeverExceedsMaxReaders()
    {
      // MaxReaders is a hard ceiling on file handles, not a hint. Once it is reached, further
      // pressure has nowhere to grow to and shows up as reopens instead — which is exactly the
      // signal that says "raise MaxReaders".
      var source = Sheet();
      using var pool = new ReaderPool(source, 2, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);
      await pool.WhenWarmersIdle();

      for (var pressure = 0; pressure < 3; pressure++)
      {
        pool.Return(pool.Borrow(0, 100, out _));   // reader 0 down the sheet
        pool.Return(pool.Borrow(0, 50, out _));    // reader 1 behind it
        pool.Return(pool.Borrow(0, 10, out _));    // behind BOTH: pressure, with nowhere to grow
      }

      await pool.WhenWarmersIdle();

      var stats = pool.Snapshot();

      Assert.Equal(2, stats.MaxReaders);
      Assert.Equal(2, stats.ReadersOpen);
      Assert.True(stats.Reopens > 0, "pressure the pool cannot absorb has to surface as reopens");
    }

    [Fact]
    public async Task AWarmedReaderIsAWarmHitRatherThanAnOpenTheReachPaysFor()
    {
      // The difference between a warmed pool and a cold one is entirely this counter: the same
      // spare open, already paid for by the time it was wanted.
      var source = Sheet();
      using var pool = new ReaderPool(source, 2, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);
      await pool.WhenWarmersIdle();

      pool.Return(pool.Borrow(0, 500, out _));
      pool.Return(pool.Borrow(0, 10, out _));      // takes the warmed slot

      var stats = pool.Snapshot();

      Assert.Equal(1, stats.WarmHits);
      Assert.True(stats.WarmHits <= stats.SpareOpens);
    }

    [Fact]
    public async Task AReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile()
    {
      // Two opens of one file finish no sooner than one and the loser's work is thrown away, so
      // waiting is the right move — and this is what that wait costs. Two things make the race an
      // arrangement rather than a hope, and each proves a different half: the gate holds the warmer
      // provably inside its open, and the dedicated thread makes the reach provably ARRIVE while it
      // is there — this is the test the OnItsOwnThread doc tells the story of, the one that can
      // FAIL under starvation, because a reach that runs after the warmer parked waits 0ms and 0ms
      // is the honest count.
      var source = Sheet();
      using var gate = new ManualResetEventSlim(initialState: false);
      using var pool = new ReaderPool(source, 2, warmReaders: true);

      source.OpenGate = gate;
      pool.BeginWarming();                          // the warmers block inside Open

      var reaching = OnItsOwnThread(() => pool.Return(pool.Borrow(0, 10, out _)));

      Assert.NotSame(reaching, await Task.WhenAny(reaching, Task.Delay(200)));
      Assert.False(reaching.IsCompleted, "the reach must wait for the warmer it would otherwise duplicate");

      gate.Set();

      await WithTimeout(reaching, TimeSpan.FromSeconds(10));
      await pool.WhenWarmersIdle();

      var stats = pool.Snapshot();

      Assert.Equal(1, stats.WarmHits);              // it took the warmer's reader
      Assert.True(stats.WarmWaitMilliseconds > 0, "the wait is counted, because it is a real cost");
      Assert.Equal(stats.Opens, source.Opens);      // and started no open of its own
    }

    // --- Failure and contention: the shapes that used to hang -----------------------------------------
    //
    // Everything below is a regression pin with a hard timeout, and the timeouts are the point. Each
    // of these bugs presented as a permanent block rather than as a wrong answer, and a test that
    // waits forever is reported by the runner as "running", not "failed" — so a regression would
    // wedge CI instead of naming itself. Every wait here is bounded, and every bound carries the
    // invariant it is protecting.

    /// <summary>How long a thing that should take microseconds is allowed before it counts as wedged.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>`Task.WaitAsync(TimeSpan)` is .NET 6 and up; this is the same thing, portably.</summary>
    private static async Task WithTimeout(Task task, TimeSpan patience)
    {
      if (await Task.WhenAny(task, Task.Delay(patience)) != task)
        throw new TimeoutException($"The task did not complete within {patience}.");

      await task;   // observe the fault, unwrapped, so ThrowsAsync still sees the original
    }

    /// <summary>Long enough that "still not finished" means blocked rather than merely slow.</summary>
    private static readonly TimeSpan LongEnoughToProveItIsBlocked = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// A borrower whose blocked-ness a test asserts must run on its own thread. Task.Run is not
    /// good enough: the pool's warmers also ride the thread pool, and in the gated arrangements
    /// they BLOCK inside their opens holding a pool thread each — on a two-core CI runner that is
    /// the entire starting pool, and a queued borrower does not run until thread injection gets
    /// around to it (~1/sec). "Not finished after 200ms" is then true because the borrower never
    /// STARTED, not because it is waiting — every blocked-proof passes vacuously, and a test that
    /// asserts the cost of the wait fails, because by the time the borrower runs there is nothing
    /// left to wait for. A dedicated thread starts unconditionally, so "started, and still not
    /// finished" really does mean "parked inside Borrow". (Found by CI on ef348dd, 2026-09-03.)
    /// </summary>
    private static Task OnItsOwnThread(Action borrower)
      => Task.Factory.StartNew(borrower, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    [Fact]
    public async Task AFailedBorrowReturnsItsLease()
    {
      // The worst of the concurrency bugs, because of how it presented. A borrow does its expensive
      // work — waiting on a warmer, opening, skipping rows — with the pool's gate RELEASED, and any
      // of it can throw; a disk failing part-way through an advance is an ordinary thing for a disk
      // to do. A lease abandoned while checked out never comes back, and once every lease has leaked
      // that way selection returns null forever and every later borrow parks in Monitor.Wait with
      // nobody left to wake it. One failed read turned every subsequent read into a permanent hang.
      var source = Sheet(rows: 5000);
      using var pool = new ReaderPool(source, 1, warmReaders: false);

      pool.Adopt(pool.OpenParked(), 0, 0);
      pool.Return(pool.Borrow(0, 100, out _));

      source.Fault = () => new IOException("the disk stopped answering mid-advance");
      source.FaultRow = 300;

      var failing = Task.Run(() => pool.Return(pool.Borrow(0, 900, out _)));

      // The failing borrow must COMPLETE — as a failure. Before the fix it completed too; it was
      // what came after that hung, so this bound is here to keep the arrangement honest.
      var failure = await Assert.ThrowsAsync<IOException>(() => WithTimeout(failing, Patience));

      Assert.Contains("mid-advance", failure.Message);

      source.FaultRow = -1;

      // ...and this is the assertion the fix exists for. If the lease was stranded, this borrow
      // waits for a reader that is never coming back, and the bound below is what turns that from a
      // wedged run into a named failure.
      var afterwards = Task.Run(() => pool.Return(pool.Borrow(0, 1200, out _)));

      await WithTimeout(afterwards, Patience);

      Assert.Equal(1, source.Opens);      // and it was served by the same reader, not a new one
    }

    [Fact]
    public async Task DisposeWakesABorrowerWaitingForALease()
    {
      // The disposal check at the top of Borrow was written for exactly this moment and could never
      // run: a borrower parked waiting for a lease was woken by nothing but a Return, and Dispose
      // issued none. Disposing a workbook while a map was in flight hung the map instead of failing
      // it — the one outcome §5.4 promises against, in as many words: "not corruption, not a hang".
      var source = Sheet(rows: 5000);
      var pool = new ReaderPool(source, 1, warmReaders: false);

      pool.Adopt(pool.OpenParked(), 0, 0);

      var held = pool.Borrow(0, 0, out _);
      var waiting = OnItsOwnThread(() => pool.Return(pool.Borrow(0, 10, out _)));

      // Not a sleep: this PROVES the borrower is parked. On its own thread it has certainly
      // started; with the only lease checked out there is nowhere else it can be, and it
      // demonstrably has not finished.
      Assert.NotSame(waiting, await Task.WhenAny(waiting, Task.Delay(LongEnoughToProveItIsBlocked)));

      pool.Dispose();

      var failure = await Assert.ThrowsAsync<ObjectDisposedException>(() => WithTimeout(waiting, Patience));

      Assert.Contains("disposed", failure.Message);
    }

    [Fact]
    public async Task AWarmNeverLandsInASlotABorrowerIsFilling()
    {
      // Arranged rather than hoped for. Holding the gate closed keeps every open in flight until the
      // test says otherwise, so a warm and a Fill are provably overlapping when the interleaving
      // matters — which beats running a parallel burst a dozen times and hoping.
      //
      // The race: claiming a cold slot raises the warm target, which starts warmers; a warmer
      // landing in the very slot the borrower is about to fill would have its cursor overwritten.
      // The slot is marked in use before the target grows, so it is never a warm target — and the
      // arithmetic that proves nothing was orphaned is opens against closes.
      var source = Sheet(rows: 4000);
      using var gate = new ManualResetEventSlim(initialState: false);
      var pool = new ReaderPool(source, 3, warmReaders: true);

      var parked = pool.OpenParked();
      source.OpenGate = gate;                       // from here on, every open blocks
      pool.Adopt(parked, 0, 0);                     // starts the one eager warm, which blocks
      pool.Return(pool.Borrow(0, 100, out _));      // move the adopted reader off the top

      // Two reaches behind it: the first takes the warming slot, the second claims a cold one and
      // fills it — so a warm and a Fill are in flight at the same moment.
      var first = Task.Run(() => pool.Return(pool.Borrow(0, 5, out _)));
      var second = Task.Run(() => pool.Return(pool.Borrow(0, 6, out _)));

      Assert.True(
        SpinWait.SpinUntil(() => source.OpensStarted >= 2, Patience),
        $"a warm and a fill should both be in flight; {source.OpensStarted} open(s) had started");

      gate.Set();

      await WithTimeout(Task.WhenAll(first, second), Patience);
      await pool.WhenWarmersIdle();

      pool.Dispose();

      Assert.Equal(source.Opens, source.Closes);
      Assert.True(source.Opens <= 3, $"a three-reader pool opened {source.Opens} readers");
    }

    [Fact]
    public async Task AParallelBurstOfReachesLeavesNoReaderBehind()
    {
      // The interleavings the gated arrangement above cannot name. Its assertion is exact rather
      // than hopeful — after Dispose and the warmers settling, opens equal closes or they do not —
      // so it is a real pin even on the runs where nothing interesting happened.
      var source = Sheet(rows: 4000);
      var pool = new ReaderPool(source, 3, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);

      Parallel.For(0, 8, reach => pool.Return(pool.Borrow(0, 100 * (reach + 1), out _)));

      await pool.WhenWarmersIdle();
      pool.Dispose();
      await pool.WhenWarmersIdle();

      Assert.Equal(source.Opens, source.Closes);
    }

    [Fact]
    public async Task WaitingForABusyReaderIsNotEvidenceOfPoolPressure()
    {
      // Contention is not pressure, and the difference decides whether a reader gets opened. A reach
      // that no parked reader could serve means the pool was one reader SHORT — evidence worth
      // growing on. A reach that simply found every reader busy means the pool was one reader
      // BUSY — evidence of nothing, and treating it as pressure would open readers in response to
      // ordinary traffic, which is how a memory knob turns into a memory leak.
      var source = Sheet(rows: 4000);
      using var pool = new ReaderPool(source, 1, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);
      await pool.WhenWarmersIdle();

      var held = pool.Borrow(0, 0, out _);
      var waiting = OnItsOwnThread(() => pool.Return(pool.Borrow(0, 5, out _)));

      Assert.NotSame(waiting, await Task.WhenAny(waiting, Task.Delay(LongEnoughToProveItIsBlocked)));

      pool.Return(held);

      await WithTimeout(waiting, Patience);
      await pool.WhenWarmersIdle();

      var stats = pool.Snapshot();

      Assert.Equal(1, stats.Opens);
      Assert.Equal(1, stats.ReadersOpen);
      Assert.Equal(0, stats.SpareOpens);      // the wait counted as neither of the two ways
      Assert.Equal(0, stats.Reopens);         // a pool-pressure event is recorded
    }

    // --- Lifetime ----------------------------------------------------------------------------------

    [Fact]
    public async Task DisposingWhileAWarmerIsOpeningLeavesNoReaderBehind()
    {
      // Dispose does not wait for a warm in flight — an open already under way cannot be
      // interrupted, and blocking on five seconds of it would make Dispose unusable. The promise is
      // kept at the other end instead: the warm's completion path finds the pool gone and disposes
      // what it just opened. Opens equal closes, which is the only form of "no handle outlived its
      // workbook" that can be asserted rather than hoped for.
      var source = Sheet();
      using var gate = new ManualResetEventSlim(initialState: false);
      using var started = new ManualResetEventSlim(initialState: false);
      var pool = new ReaderPool(source, 3, warmReaders: true);

      source.OpenStarted = started;
      source.OpenGate = gate;
      pool.BeginWarming();

      // Not "dispose and hope a warmer was running": the warmer is provably inside its open before
      // the workbook goes away, which is the only version of this race worth testing.
      Assert.True(started.Wait(TimeSpan.FromSeconds(10)), "a warmer should have begun opening");

      pool.Dispose();
      gate.Set();

      await pool.WhenWarmersIdle();

      Assert.True(source.Opens > 0, "the warmer must actually have opened something for this to prove anything");
      Assert.Equal(source.Opens, source.Closes);
    }

    [Fact]
    public void DisposeClosesEveryReaderAndIsIdempotent()
    {
      var source = Sheet();
      var pool = new ReaderPool(source, 3, warmReaders: false);

      pool.Return(pool.Borrow(0, 500, out _));
      pool.Return(pool.Borrow(0, 0, out _));

      Assert.Equal(2, source.Opens);
      Assert.Equal(0, source.Closes);

      pool.Dispose();
      pool.Dispose();

      Assert.Equal(2, source.Closes);
    }

    [Fact]
    public void BorrowingFromADisposedPoolThrows()
    {
      var source = Sheet();
      var pool = new ReaderPool(source, 2, warmReaders: false);
      pool.Dispose();

      Assert.Throws<ObjectDisposedException>(() => pool.Borrow(0, 0, out _));
    }
  }
}
