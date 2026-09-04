# Spec: Streaming (the `Workbook` owner, the windowed store, the reader pool, lazy extents)

**Status:** Part 1 IMPLEMENTED (2026-09-03) — the `Workbook` owner, `IRowSource` seam,
`SheetStore`, `ReaderPool` with adaptive warming and `BorrowAnywhere` catalogue walks, the
IO fault discipline, the `Streaming` benchmark family, and a 159-test suite; four defects
found by that suite and fixed pre-commit, corrections marked `[corrected
post-implementation]` throughout. **Part 2 (lazy extents, §11) is IMPLEMENTED
(2026-09-04)** — all eight steps of §10.2, pinned by five test suites (differential
denotation sweep, forcing counts, error timing, fold identities, column-rewrite oracle).
Measured against §11.8's prediction on the 1M-row monotone `TableRows` parse:
`RowsMaterialised` 1,000,001 exactly as predicted, `ChunkReloads` 0, wall time ~13s — the
19.6s-vs-14.7s gap against eager is gone, at 1.6 MB peak resident. Two honest corrections
to §11.5's table: a public `ISpace.Area` width query forces *permanently* (Area is one
struct; the free width landed as an internal seam on `BoundedSpace`, observable on the
views — `CellBlock.Width` costs zero rows), and `CellBlock`'s validating indexer/`Row(i)`
stream rather than force since step 6. `RowFirst=false` (`ColumnsThenRows`) is deliberately
NOT incremental: its width decision reads rows the height scan may never reach, which
`IAreaScan.Width` forbids.
Originally settled by two spikes and an owner design conversation; branch base `master` @
`3e69dc5` (struct-era `CellValue`).

**The prototype referenced below was superseded by the Part 1 implementation** (it survives
as `scratchpad/pool-probe.patch` for the record). Historical note: `src/Unrect.Spreadsheets/`
at spec time held an uncommitted probe — `RowChunkStore.cs`, `WindowedSpace.cs`,
`StreamingStatistics.cs`, and streaming additions to `SpreadsheetSpace.cs`. It is the
reference implementation this document turns into a real one; §9 says exactly which parts
carry over and which are rewritten. Every one of its measurements is quoted here as law
(§1), because the design decisions below are consequences of those numbers.

Conventions inherited: `wave2-shapes-spec.md` (engine rules, error-message template, file
layout, test style), `flow-vocabulary-spec.md` (removal orders, `[decided here]` markers),
`capability-seam-notes.md` (additive capability recipe, default interface members),
`docs/benchmarking.md` (benchmark family rules).

Everything the owner settled is recorded as settled. Where a detail had to be decided to
make this mechanical, it is marked **[decided here]** — §15 lists them all in one place.

---

## 0. What this pass delivers

| Part | # | Change | Kind |
|---|---|---|---|
| 1 | 1 | `Workbook : IDisposable` — the disposable space factory; `Sheet(name)` vends a lent `ISpace` | addition |
| 1 | 2 | `SheetStore` — chunked rows under a window budget, the memory knob | addition |
| 1 | 3 | `ReaderPool` — lead/chase forward readers, adaptive warming | addition |
| 1 | 4 | `StreamingStatistics` / `ReaderPoolStatistics` — the honest vocabulary; `Rewinds` never ships | addition |
| 1 | 5 | IO faults are non-absorbable: `IOException` / `ObjectDisposedException` can never be swallowed by `.Optional()` | correctness fix |
| 1 | 6 | An internal row-source seam, so the store is testable and benchmarkable without a workbook | addition |
| 2 | 7 | Lazy extents: an area strategy that decides its bound row by row as the projection consumes | addition |

Part 1 ships first and completely. Part 2 is gated on Part 1's merge and can be abandoned
after any of its steps without leaving the tree in a half-state (§10.2).

**Not in scope, deliberately:** writing spreadsheets, async APIs, a public row-source seam
(CSV/database cursors), a streaming *result* type (`IEnumerable<T>` out of `TableRows`),
and any change to the eager path's semantics.

---

## 1. The measurements, as law

Every number below is from the probe runs (`pool-run1..5`, 1,000,000-row × 8-column xlsx,
client GC, warm page cache). They are the constraints the design has to satisfy, and the
"why" behind every default. Reproduce them before changing any default.

### 1.1 Eager vs windowed, one monotone `TableRows<Row>()` parse

| Configuration | Wall | Peak heap | Live at end | Rows parsed | Reopens |
|---|---|---|---|---|---|
| Eager `SpreadsheetSpace.Create` | 14.7s (13.2 load + 1.5 parse) | 537 MB | 291 MB (214 MB is the space) | 1,000,000 | — |
| Windowed, 1 reader, no warming | 24.8s | 383 MB | 108 MB | 2,000,002 | 1 |
| Windowed, 2 readers, no warming | 24.6s | 384 MB | 109 MB | 2,000,002 | 0 |
| Windowed, 2 readers, warmed | **19.6s** | 383 MB | 109 MB | 2,000,002 | 0 |
| Windowed, 3 readers, warmed | 19.6s | 385 MB | 109 MB | 2,000,002 | 0 (2 opened) |
| Windowed, 6 readers, warmed | 20.2s | 377 MB | 109 MB | 2,000,002 | 0 (2 opened) |

Findings, all load-bearing:

1. **`ExcelReaderFactory.CreateReader` costs 5.1–5.9s and is CPU-bound**, not IO-bound —
   it is the shared-string table parse. It never gets cheap; it can only be *overlapped*.
   Measured directly as `WarmWaitMilliseconds` = 5,129 and 5,152 when a backward reach
   arrived before the warmer finished.
2. **The pool alone buys nothing. Warming is the win.** 2 readers unwarmed = 24.6s, the
   same as 1 reader; 2 readers warmed = 19.6s. The pool converts a *reopen* into a *spare
   open*, which is the same 5s — unless a background task has already paid it.
3. **Readers beyond demand are never opened.** 3 and 6 configured readers still opened 2.
   Configuring a large pool is not itself a cost; warming one speculatively is.
4. **Rows-skipped is invariant.** In every configuration the pool changes *opens*, never
   repositioning. `RowsSkipped` is 0 here and identical across pool sizes everywhere else.
   Window sizing owns band behaviour; the pool owns opens. They are orthogonal knobs.
5. **Streaming bounds *our* memory, not the process's.** The resident window was 1 MB
   (5,456 rows) yet peak heap only fell 537 → 383 MB. The floor is the reader's own state
   — above all the shared-string table, which owns every string a `Text` cell points at.
   Live-at-end 291 → 109 MB is the honest headline: **what streaming removes is the
   materialised grid, not the parser.**

### 1.2 The pool law: reopens = passes − readers

`root-hflow`, a horizontal flow of 3 children at the root, each of which walks the sheet
(6.0 full passes measured):

| Readers | Reopens | Wall |
|---|---|---|
| 1 | 5 | 61.7s |
| 2 | 4 | 59.0s |
| 3 | 3 | 51.9s |
| 6 | 0 | 37.2s |

Exact and linear. `CheapRewinds` was 14,660 in every pooled configuration — the backward
reaches are unchanged; only their price moves.

### 1.3 The sizing law: the window must hold the tallest simultaneously-open extent

`hflow`, five children sweeping one band:

| Window vs band | Loads | Reloads | Rows skipped | Wall |
|---|---|---|---|---|
| 10 chunks vs a 7-chunk band | 7 | 0 | 496 | **0.01s** |
| 4 chunks vs a 13-chunk band | 65 | 52 | 2,000,496 | 29.5s (1 rdr) … 13.4s (6 rdrs) |

Three orders of magnitude, from one chunk of shortfall. `overlay` says the same in a
gentler register: a 16,384-row window over a 4,000-row band = 4 loads / 0 reloads / 6.0s;
a 1,024-row window over the same band = 30 loads / 14 reloads / 10.4s.

**Therefore the primary knob is the window, expressed in rows, and its law is: the window
must be at least as tall as the tallest extent that is open at one time.** A vertical walk
down a sheet has an open extent of one chunk. A `HorizontalFlow` or `Overlay` over a band
has an open extent of the whole band. Undersize it and the cost is not degradation, it is
collapse.

### 1.4 Locus, LRU and the pin all measured identically

Across `hflow` (fits / too small) × {locus, plain LRU, explicit `Pin`}, every timing was
identical to two decimal places, and the pinned run differed only by reporting 61 overruns
where the others reported 0.

**Consequence [owner-settled]:** the explicit `Pin` API does not ship. The locus stays —
it is the residency law stated rather than inferred, and it costs nothing — but its only
observable output is the **overrun counter**, which is the diagnostic that says *your
window is smaller than the band you are sweeping*. That counter is the reason the pin
survives at all.

### 1.5 The chunk constant

`BytesPerCell = 24`. Chunks are sized to 64 KB to stay under the 85,000-byte Large Object
Heap threshold, because chunks are allocated and dropped continuously as the window slides
and an LOH allocation per chunk would trade a bounded heap for a fragmenting one.

**Record this because it was nearly a silent bug:** the constant was `8` while `CellValue`
was a class. Leaving it at 8 after the struct merge would have tripled every chunk — a
default 8-column chunk would have been 196,608 bytes and gone straight to the LOH that the
64 KB target exists to avoid. `BytesPerCell` must be asserted against
`Unsafe.SizeOf<CellValue>()` in a test (§8.1) so the next representation change cannot
repeat it.

---

## 2. The public face: `Workbook`

### 2.1 The idiom

```csharp
using var book = Workbook.Open(path);              // owns file handles, reader pool, chunk stores
var result = projection.Map(book.Sheet("Data"));   // Sheet(name) vends a lent ISpace view
```

and the motivating one, which the XML docs and `docs/streaming.md` must both carry
verbatim:

```csharp
var report = VerticalFlow(v => ...);               // one declaration, reused

foreach (var path in monthlyCloseOfFunds)
{
  using var book = Workbook.Open(path);
  Publish(report.Map(book.Sheet("Detail")));       // bounded memory per iteration
}
```

Shapes are immutable and thread-safe and workbooks are independent, so
`Parallel.ForEach(monthlyCloseOfFunds, path => { using var book = …; })` falls out with
nothing added. Within one workbook, see §5.4 for what serialises.

### 2.2 Surface

```csharp
namespace Unrect.Spreadsheets
{
  public sealed class Workbook : IDisposable
  {
    public static Workbook Open(string path);
    public static Workbook Open(string path, WorkbookOptions options);

    public string Path { get; }

    /// Every sheet name, in workbook order. Forces the catalogue walk — see §2.5.
    public IReadOnlyList<string> SheetNames { get; }

    /// The named sheet as a space. Idempotent: the same name returns a view over the same store.
    public ISpace Sheet(string name);

    /// What reading this sheet has cost so far; null when the sheet has never been vended.
    public StreamingStatistics? Statistics(string sheetName);

    /// What the workbook's readers have cost — shared across every sheet.
    public ReaderPoolStatistics ReaderStatistics { get; }

    public void Dispose();
  }

  public sealed class WorkbookOptions
  {
    /// Which cells count as empty space. Default: whitespace-only text is blank — the same
    /// default, and the same words, as the eager SpreadsheetSpace.Create overloads.
    public Func<CellValue, bool>? IsBlank { get; init; }

    /// The memory knob, in ROWS. Must be at least as tall as the tallest extent open at one
    /// time (§1.3). Default 8,192. Rounded up to a whole number of chunks, minimum 4 chunks.
    public int WindowRows { get; init; } = 8192;

    /// Rows per chunk, or 0 to derive one from the sheet's width (64 KB / (24 * columns)).
    public int ChunkRows { get; init; }

    /// The most forward readers this workbook will hold open at once. Default 3.
    public int MaxReaders { get; init; } = 3;

    /// Whether spare readers are opened ahead of need on a background task. Default true.
    /// Off is for tests and measurement; leaving it off costs about one open per pass (§1.1).
    public bool WarmReaders { get; init; } = true;

    /// Whether sheet names match exactly. Default false, as SpreadsheetSpace.Create.
    public bool CaseSensitiveSheetNames { get; init; }
  }
}
```

`WorkbookOptions` is a class with `init` accessors so a future option is additive and no
existing call site is binary-broken (the discipline from `capability-seam-notes.md`:
additive members are safe, new optional parameters on existing methods are not).

### 2.3 Lifetime — the whole of it lives on `Workbook`

- The workbook owns **every** disposable: file streams, `IExcelDataReader`s, the warming
  tasks and their `CancellationTokenSource`, and each sheet's chunk store.
- **A vended view is pure and undisposable.** `Sheet(name)` returns an `ISpace`, not a
  handle; it has no `Dispose`, no `Close`, no `Pin`. It can be sliced freely
  (`GetSubspace` returns another view over the same store), passed to any shape, and held
  as long as the caller likes — it is a value, and the only thing that can invalidate it is
  the workbook it came from being disposed.
- **A view touched after `Dispose` throws `ObjectDisposedException`, deterministically, and
  that exception is a FAULT** (§6): no tolerance boundary may absorb it as "section
  absent". Deterministically means the disposed check happens before the resident-chunk
  fast path, so a read does not accidentally succeed on a chunk that happens still to be in
  memory. One volatile bool read per cell; the indexer already does two bounds checks.
- `Dispose` is idempotent and does not block on a warming task (§5.3).
- Disposing while a map is running is a caller error, not corruption: the running map fails
  with `ObjectDisposedException` wrapped as a `ShapeException` naming the shape and cell.

### 2.4 `Sheet(name)` is idempotent, and that is the warm-reuse story

Repeated calls for the same sheet share one `SheetStore`. Mapping a second declaration over
an already-open book therefore does not re-pay the reader open (~5.1s, CPU-bound), and may
not re-read a row at all if what it wants is still resident. This is the single most
valuable property of the owner design and it must be pinned by a test that asserts
`ReaderStatistics.Opens` is unchanged across a second `Sheet` call plus map (§8.3).

Sheet resolution uses `WorkbookOptions.CaseSensitiveSheetNames`; an unknown name throws
`ArgumentException` with the same message the eager path uses today —
`No sheet named 'X' in 'path'.` — listing the names known so far.

### 2.5 The catalogue, and the one open at `Open` **[decided here]**

`Open` performs exactly one file open and leaves that reader **parked** at sheet 0. It is
not thrown away and it is not walked to the end:

- `Sheet(name)` walks the parked reader forward with `NextResult()` until it finds the
  name, recording every sheet it passes (name, index, `RowCount`, `FieldCount`) into the
  catalogue as it goes; the reader is then **adopted** as that sheet's first pool lease,
  already open, already on the right sheet, already at row 0. This is the spike's behaviour
  and it is why the common single-sheet case costs one open, not two.
- **[corrected post-implementation]** The walk must not be the parked reader's privilege.
  As first written this paragraph left the catalogue *dead-ended after adoption*: `_parked`
  became null, nothing could extend the catalogue, and every sheet **ahead** of the
  first-adopted one was unreachable — `Sheet("Summary")` then `Sheet("Detail")` failed with
  a "sheets seen so far" error naming only the sheets already passed. The corrected rule:
  **any reader can walk, and the pool serves the walk like any other forward read.** The
  parked reader does it while it is still parked; afterwards a lease is borrowed at the
  catalogue's edge (the last sheet anyone has seen) and stepped forward from there,
  discovering the sheets beyond. The cost story is unchanged and stays honest — walking to a
  far sheet is a real forward read, and the rows and opens it costs appear in the statistics
  like any other.
- A later `Sheet(other)` for a sheet *behind* the parked reader needs a fresh reader; that
  is what the warm spare (§5.3) is for.
- `SheetNames` forces the walk to the end of the workbook. The parked reader is then
  positioned past every sheet and is retired (closed), releasing the pool slot it was
  holding. **Documented cost: asking for `SheetNames` before the first `Sheet` call costs
  one extra reader open.**
- **[corrected post-implementation]** The slot the parked reader will be adopted into is
  **reserved** from the moment it is opened. Without that reservation the eager warm at
  `Open` targets the very slot adoption is about to fill, and all three of its consequences
  were measured: the warm's reader is overwritten and leaked, its open goes uncounted, and
  two spares warm where the policy allows exactly one. Reserving the slot makes the
  invariant structural — *a slot that will be adopted is never a warm target* — rather than
  a consequence of the order two calls happen to be made in.

Rejected alternative: walk the whole catalogue eagerly at `Open`. It gives better errors
and a complete `SheetNames` for free, but it pays a second 5s open in the single-sheet case
that is most usage. Lazy catalogue + fast-fail on the name actually asked for is the better
trade, and the error message stays honest by listing what has been seen so far.

### 2.6 The eager path is untouched

`SpreadsheetSpace.Create(...)` — both overloads — keep their present behaviour, signatures
and docs, and the whole uncommitted streaming addition to that file is deleted (§9). There
is no `CreateStreaming`. The simple path stays simple, and the streaming path has exactly
one door.

### 2.7 The cost model, for the docs

The two paths differ in *shape of cost*, not in results. `docs/streaming.md` must say this
in these terms:

| | Eager (`SpreadsheetSpace.Create`) | Streaming (`Workbook`) |
|---|---|---|
| When the file is read | once, entirely, before `Map` | as the shape asks for cells |
| Memory | the whole grid: rows × columns × 24 bytes, plus the string table | the window: `WindowRows` × columns × 24 bytes, plus the string table |
| Cost of a *second* pass over the same rows | free (it is an array) | a cheap rewind if a parked reader is behind it; a chunk reload if the window has moved on |
| Cost of a backward reach with no reader behind it | free | one file open (~5s on a 1M-row workbook), unless warmed |
| What makes it slow | nothing you can change | a declaration that sweeps a band taller than the window (§1.3), or more concurrent passes than readers (§1.2) |

**Streaming's cost is declaration-shaped.** A monotone walk down a sheet is ~35% slower
than eager and holds ~2.7× less live memory. A declaration that reaches backwards, or
sweeps a band wider than its window, can be arbitrarily slower. Use eager when the file
fits comfortably in memory; use `Workbook` when it does not, or when you are mapping many
files in sequence and want the peak bounded per iteration.

**And say the floor out loud:** the reader's shared-string table is not part of the window
and does not shrink with it. On a text-heavy sheet it can dominate. Streaming removes the
materialised grid; it does not make ExcelDataReader smaller.

---

## 3. The row-source seam **[decided here]**

The store must not know about ExcelDataReader. One internal interface pair, in
`Unrect.Spreadsheets`, mirroring the reader's own shape so the load loop is unchanged:

```csharp
internal interface IRowSource
{
  string Name { get; }
  /// Opens an independent forward-only cursor over the whole workbook, positioned before
  /// the first row of sheet 0. Expensive: this is the ~5s open.
  IRowCursor Open();
}

internal interface IRowCursor : IDisposable
{
  int SheetIndex { get; }
  string SheetName { get; }
  int RowCount { get; }                 // of the current sheet
  int ColumnCount { get; }              // of the current sheet
  bool NextSheet();                     // forward only
  bool Read();                          // advances one row of the current sheet
  CellValue this[int column] { get; }   // of the current row, blankness ALREADY applied
}
```

Three reasons this seam is mandatory rather than nice:

1. **Blankness moves to where the project says it belongs.** The adapter
   (`SpreadsheetRowSource`) applies `isBlank` as it produces `CellValue`s; the store never
   sees the predicate. That is "blankness is decided at adaptation time", which the eager
   path already honours and the probe's store violated.
2. **The store becomes testable.** Chunk maths, eviction, pool selection, adaptive warming,
   dispose-racing-warmer, and above all *IO faults at a chosen row* are all impossible to
   arrange with a real workbook and trivial with a fake source.
3. **The benchmarks can exist at all.** `docs/benchmarking.md`: *fixtures are
   GridSpace-built synthetics — CI runners get no workbooks.* A synthetic `IRowSource`
   keeps the Streaming family inside that rule.

`InternalsVisibleTo` for `Unrect.Tests` and `Unrect.Benchmarks` goes on
`Unrect.Spreadsheets.csproj`. Publishing this seam (CSV, database cursors, Parquet) is the
obvious follow-on and is deliberately deferred: making it public commits to an API before a
second implementation has argued with it.

---

## 4. `SheetStore` — the window

One per sheet, owned by the workbook, created on first `Sheet(name)`.

### 4.1 Shape

```csharp
internal sealed class SheetStore : IDisposable
{
  internal SheetStore(ReaderPool pool, int sheetIndex, string sheetName,
                      int rowCount, int columnCount, int chunkRows, int windowChunks);

  internal int RowCount { get; }
  internal int ColumnCount { get; }
  internal int ChunkRows { get; }
  internal int WindowChunks { get; }

  internal CellValue GetCell(int column, int row, int extentTop, int extentHeight);
  internal StreamingStatistics Snapshot();
  public void Dispose();
}
```

`extentTop`/`extentHeight` are the locus signal, passed down from the view with every cell
because it is the one thing the view knows and the store does not: whether this read is part
of a bounded sweep or a walk down the sheet.

### 4.2 Rules that carry over from the probe unchanged

- **Chunked rows, `BytesPerCell = 24`, target 64 KB, capped at 8,192 rows and floored at 1**
  (§1.5): `DefaultChunkRows(columns) = clamp(65536 / (24 * columns), 1, 8192)`.
- **No pre-fill.** `default(CellValue)` *is* Blank since the struct merge, so a freshly
  allocated chunk is already an all-blank band and a short row leaves untouched cells
  exactly right. The probe's deleted fill loop was a second pass doing nothing.
- **Locus residency.** A chunk overlapping the current locus is not an eviction candidate;
  LRU is the tie-break *outside* it. Plain LRU is precisely wrong for a repeated sweep of a
  band one chunk larger than the budget, where the least-recently-used chunk is always the
  one wanted next. The locus grows by union (so nested single-row extents inside a band do
  not shrink it to a row) and re-anchors when the union would exceed the budget.
- **Cell reads are lock-free on the resident path.** A stale null costs one trip through the
  gate, which re-checks; a stale non-null is a chunk another thread evicted, whose contents
  are immutable and still correct. Recency bookkeeping is deliberately unsynchronised — a
  lost increment picks a slightly worse victim and nothing else.

### 4.3 Rules that change

| Change | Why |
|---|---|
| `residentChunks` → **`WindowRows`**, converted to `windowChunks = max(4, ceil(WindowRows / ChunkRows))` | The sizing law (§1.3) is stated in rows — "at least as tall as the tallest open extent". Chunks are an implementation detail a user should never have to do arithmetic in. |
| The public `Pin(from, height)` API is **deleted** | Measured identical to no pin in every case (§1.4). |
| `LocusOverruns` → **`WindowOverruns`**, promoted to the headline diagnostic | It is the one signal that says the window is too small for the declaration; it is what the pin leaves behind. |
| `UseLocus` flag deleted | It existed to measure locus against LRU. The measurement is done (§1.4); the flag is a knob with no user. |
| `Slices` counter deleted | It existed to size a per-slice pin protocol that is not being built — and it was an unsynchronised `++` from arbitrary threads, i.e. a live data race in the probe. |
| `ResidentCellBytes` (computed from *live* residency, documented as *peak*) split into **`ResidentBytes`** and **`PeakResidentBytes`** | The probe's number said one thing and computed another. |
| `isBlank` no longer reaches the store | §3. |
| `RowCount <= 0` from the source is rejected at `Sheet(name)` with `NotSupportedException` naming the sheet | The fixed resident index needs the row count, and some xlsx files carry no `dimension` element. Part 2 step 7 removes the restriction by making the index growable; until then, failing loudly beats a silently truncated sheet. **[superseded by Part 2 step 7]** The index is now a chunk-keyed growable map and needs no row count; `Sheet(name)` *measures* an undimensioned sheet instead — one forward pass counting rows and watching the width, materialising nothing — and hands the real extent down. Deciding this way rather than reporting an upper-bound extent is what keeps every blank-row scan and every unconsumed-space diagnostic honest; the pass is a reader movement and appears in `ReaderStatistics`, not in the sheet's own. |

### 4.4 Load

```
GetCell(column, row, extentTop, extentHeight):
  throw ObjectDisposedException if disposed              // §2.3, before everything
  chunk = row / ChunkRows
  cells = _resident[chunk]                               // plain read
  if cells is null: cells = Load(chunk, extentTop, extentHeight)
  else if chunk != _lastChunk: Touch(chunk); Anchor(extentTop, extentHeight)
  return cells[(row - chunk*ChunkRows) * ColumnCount + column]

Load(chunk, extentTop, extentHeight):
  lock (storeGate):                                      // §5.4: held across the load, v1
    Anchor(extentTop, extentHeight)
    re-check residency
    lease = pool.Borrow(sheetIndex, chunk * ChunkRows)   // takes the POOL gate, briefly
    try:     stream min(ChunkRows, RowCount - start) rows into a new CellValue[rows*columns]
    finally: pool.Return(lease)
    Evict(); install; Touch(chunk)
```

Lock ordering is store gate → pool gate, never the reverse. Nothing takes two store gates.

---

## 5. `ReaderPool` — lead and chase

### 5.1 Why the pool is workbook-level, not sheet-level **[decided here]**

A reader is a position in a *workbook*, not a sheet: it can move to the next sheet but never
back. Making the pool workbook-level:

- lets a warm spare be opened at `Open`, before any sheet has been named — which is exactly
  what the owner's "warm exactly one spare eagerly at `Open`" requires;
- makes multi-sheet cheap: a lease parked at sheet 1 row 900 serves a request for sheet 2
  row 0 by moving forward, with no open;
- keeps "how many file handles does this workbook hold" answerable in one place, which is
  what `MaxReaders` means.

The cost is that lease *selection* is a workbook-wide critical section. It is a short one
(§5.4).

### 5.2 Positioning, generalised to (sheet, row)

A lease's position is the pair `(SheetIndex, CursorRow)`, ordered lexicographically. A
request for `(s, r)` is served by:

1. **The lease furthest along but still at or behind `(s, r)`** — fewest rows to skip, and,
   crucially, it leaves a reader parked further back available for a chase. If any lease was
   ahead of the target, count a **`CheapRewind`**: the reach was backward but cost only the
   rows between that lease and the target.
2. Otherwise **a spare slot**, if the pool has one below `MaxReaders`: count a
   **`SpareOpen`**; if a warmer has already filled it, count a **`WarmHit`** instead of
   paying; if a warmer is mid-flight on that slot, *wait for it* rather than start a second
   open of the same file (two opens finish no sooner than one and the loser's work is thrown
   away), accumulating **`WarmWaitMilliseconds`**.
3. Otherwise **recycle the lease that has travelled least** — it has the least distance
   banked in it — and count a **`Reopen`**. This is the old fixed cost and the number the
   pool exists to drive to zero.

Advancing to the target counts `RowsSkipped` **against the requesting sheet's store**, and
`RowsAdvanced` against the lease.

### 5.3 Adaptive warming **[decided here, from an owner-settled policy]**

Owner's policy: *warm exactly one spare eagerly at `Open`; warm further spares only after
the first reopen proves multi-pass demand.*

Taken literally the trigger is unreachable: while unopened slots remain, `Position` takes a
spare rather than reopening, so a `Reopen` can only happen once every slot up to
`MaxReaders` is already open — by which point there is nothing left to warm. The policy's
*intent* is: nothing speculative beyond one, growth must be evidence-driven. So the trigger
is generalised to a **pool-pressure event** — a backward reach that no parked lease could
serve, i.e. either a `SpareOpen` **or** a `Reopen`. Both mean "the pool was one reader short
at that instant", which is exactly the evidence wanted.

```
WarmTarget starts at 2 (the lead + one spare).
At Open:                 start warming slots up to WarmTarget.
On every pool-pressure:  WarmTarget = min(WarmTarget + 1, MaxReaders); warm up to it.
```

Mechanics, all required:

- **`Task.Run` + a `CancellationToken`, never a raw `Thread`.** The probe leaked a
  background thread holding a 5-second open with no way to cancel it. `Workbook.Dispose`
  cancels the source; a warm that has already begun its open cannot be interrupted, so its
  completion path must check `disposed` under the pool gate and dispose the reader it just
  opened rather than parking it.
- `Dispose` does **not** block on in-flight warms. It must nevertheless guarantee no handle
  outlives them, which the completion check above provides.
- An internal `Task WhenWarmersIdle()` test hook exists so the dispose-race test is
  deterministic rather than timing-hopeful (§8.2).
- A warm that fails to open swallows the exception: a warm reader is an optimisation, and
  anything genuinely wrong resurfaces on the on-demand path where a caller can attribute it.

`MaxReaders` defaults to **3** [decided here]: the measurements show 2 removes every reopen
for the ordinary two-phase (bound-then-project) pattern, a third helps when several passes
are open at once (§1.2), and each additional reader costs both a ~5s CPU open and a second
resident copy of the workbook's string table — so the default must not be generous. Raise it
for root-level flows; the `Reopens` counter is the evidence that you should.

### 5.4 Concurrency: what is promised, what is not

| Scenario | Behaviour |
|---|---|
| Parallel maps over **different workbooks** | Fully parallel. Nothing is shared. |
| Parallel maps over **different sheets of one workbook** | Parallel chunk loads. Lease *selection* serialises briefly on the pool gate; the streaming of rows happens with the lease checked out and the pool gate released. |
| Parallel maps over **one sheet** | **Serialised** on that sheet's store gate. Documented limitation, accepted for v1. |
| `Dispose` racing a map | The map fails with `ObjectDisposedException` as a fault. Not corruption, not a hang. |
| `Dispose` racing a warmer | No leaked handle (§5.3). |

`Sheet(name)`, `Statistics(name)` and `ReaderStatistics` are safe to call from any thread.
Statistics are snapshots taken under the relevant gate.

The v1 limitation is deliberate and cheap to revisit: making one sheet's loads concurrent
means allowing two threads to load different chunks at once, which needs only per-chunk load
coordination (double-loading is harmless — the contents are deterministic and immutable). It
is not done now because nothing measured wants it.

---

## 6. IO fault discipline (ships with Part 1, and is a correctness fix)

### 6.1 The bug being fixed

`ShapeEngine.IsFault` today classifies only *projection* exceptions, and only four types:

```csharp
private static bool IsFault(Exception exception)
  => exception is NullReferenceException or IndexOutOfRangeException
     or ArgumentOutOfRangeException or ArgumentNullException;
```

It is consulted at exactly one of the four sites where the engine wraps a foreign exception
(`Project`). At the other three — the offset strategy, the area strategy, and a repeat's
separator — the resulting `ShapeException` is built with `isProjectionFault: false` and is
therefore **absorbable**. Both absorbing sites (`BoundaryShape`, `ChoiceShape`) filter on
`when (!failure.IsProjectionFault)`.

Under streaming, a strategy reads cells. So today, a disk read failing in the middle of
`SkipBlankRows()` inside `section.Optional()` would be reported as *"section absent"*, with
a warning, and the parse would continue and produce a wrong answer quietly. That is the
single worst failure mode this feature could ship with.

### 6.2 The fix, exactly

1. Rename `ShapeException.IsProjectionFault` → **`IsFault`** and the constructor parameter
   `isProjectionFault` → `isFault`. Both are `internal`; this is not a public break. Update
   the two `when (!failure.IsFault)` filters and the XML docs, which currently say "the
   projection did not merely disagree with the data but broke" and must now say: *broke
   rather than disagreed — a bug in the reading code, or the environment failing underneath
   it.*
2. Make `ShapeEngine.IsFault` `internal static` and extend it:

```csharp
internal static bool IsFault(Exception exception)
  => exception is NullReferenceException
    or IndexOutOfRangeException
    or ArgumentOutOfRangeException
    or ArgumentNullException
    or IOException                  // the disk, the network share, the workbook replaced mid-read
    or ObjectDisposedException      // a view outliving its Workbook
    or OutOfMemoryException;        // never a statement about the data
```

   `FileNotFoundException`, `DirectoryNotFoundException` and ExcelDataReader's own IO
   failures derive from `IOException` and are covered. `ObjectDisposedException` derives
   from `InvalidOperationException`, which is *not* listed and must not be — parse helpers
   throw it for data reasons — so it is named explicitly. `ArgumentException` itself stays
   absorbable, as today, for the same reason.
3. Pass `IsFault(exception)` at **all four** wrapping sites: `ShapeEngine.TryPlace`'s offset
   catch, its area catch, `ShapeEngine.Project`'s catch, and `RepeatShape.TrySeparate`'s
   catch. The last needs a fault-carrying overload of the public
   `ShapeContext.Failure(string, ISpace, Exception?)` — add an `internal` overload rather
   than an optional parameter on the public one.
4. `OutOfBoundsException` handling is untouched: it is the "ran out of room" signal, it is
   how a `Repeat` stops, and no IO condition produces it.

### 6.3 Routing, stated for the implementer

| Where the exception comes from | Route | Absorbable? |
|---|---|---|
| `ISpace` indexer inside an offset/area strategy (placement) | `TryPlace` catch → `ShapeException(isFault: true)` | **No** |
| `ISpace` indexer inside a landmark/matcher (placement) | same | **No** |
| `ISpace` indexer inside a projection or a view | `Project` catch → `ShapeException(isFault: true)` | **No** |
| `ISpace` indexer inside a repeat separator | `TrySeparate` catch → `ShapeException(isFault: true)` | **No** |
| A cell of the wrong kind, an unparseable value, a missing column | unchanged | Yes |
| A missing anchor / a bound that ran past the space | unchanged (`OutOfBoundsException`) | Yes |

Note the consequence for `TryApply` (a repeat item): an `IOException` from the item's own
placement is *not* a stopping condition. `TryPlace(strict: false)` returns false only for
`OutOfBoundsException`; everything else already throws, and now throws marked as a fault. A
repeat cannot end because the disk failed.

---

## 7. Statistics — the honest vocabulary

`Rewinds` does not ship. It was defined as `Reopens + SpareOpens` while being documented as
"backward reaches", and it is the number the pool made meaningless: in the canonical run it
reported `1` next to 2,932 actual backward reaches, all of them cheap. Two types replace the
one, split by what owns each counter.

```csharp
namespace Unrect.Spreadsheets
{
  /// What reading ONE sheet through a window has cost.
  public readonly struct StreamingStatistics
  {
    public string SheetName { get; }
    public int ChunkRows { get; }              // rows per chunk
    public int WindowChunks { get; }           // the budget, in chunks
    public int WindowRows { get; }             // the budget, in rows — ChunkRows * WindowChunks
    public long ChunkLoads { get; }            // chunks materialised, re-materialisations included
    public long ChunkReloads { get; }          // loads of a chunk this store had already thrown away
    public long Evictions { get; }
    public long WindowOverruns { get; }        // evictions forced from inside the open band: the window is too small
    public long RowsMaterialised { get; }      // rows adapted into cells
    public long RowsSkipped { get; }           // rows parsed and discarded to reach a wanted chunk
    public int ResidentChunks { get; }         // chunks held right now
    public int PeakResidentChunks { get; }
    public long ResidentBytes { get; }         // ResidentChunks * ChunkRows * ColumnCount * 24
    public long PeakResidentBytes { get; }     // the same at the peak
    public override string ToString();
  }

  /// What ONE WORKBOOK's readers have cost. Shared by every sheet of it.
  public readonly struct ReaderPoolStatistics
  {
    public int MaxReaders { get; }
    public int ReadersOpen { get; }
    public long Opens { get; }                 // file opens of every kind — the total 5s events
    public long Reopens { get; }               // a live reader thrown away because all of them were ahead
    public long SpareOpens { get; }            // a spare slot opened for the first time
    public long WarmHits { get; }              // spare opens the warmer had already paid for
    public long WarmWaitMilliseconds { get; }  // time a reach spent blocked on an unfinished warmer
    public long CheapRewinds { get; }          // backward reaches served by a parked reader: no open, no re-stream
    public IReadOnlyList<long> RowsPerReader { get; }  // rows each lease moved over, skipped and read alike
    public override string ToString();
  }
}
```

Semantics that must hold, and are tested (§8.4):

- `Opens == 1 + SpareOpens + Reopens` for a workbook whose parked reader was adopted;
  `WarmHits <= SpareOpens`.
- `Reopens == 0` for any declaration whose concurrently-open passes do not exceed
  `MaxReaders` (§1.2).
- `CheapRewinds` counts reaches, not rows: it goes *up* when the pool is working.
- `RowsSkipped` is invariant under `MaxReaders` for a fixed declaration (§1.1 finding 4).
- `WindowOverruns > 0` means the window is smaller than the band being swept — it is the
  diagnostic a user acts on by raising `WindowRows`.
- `PeakResidentBytes` counts the `CellValue` structs only. **Documented floor:** strings
  referenced by `Text` cells are owned by the reader's shared-string table, are not counted
  here, and do not shrink with the window (§1.1 finding 5).
- `RowsPerReader` is a list, not the probe's pre-rendered string; `ToString()` renders it.

`ToString()` on both keeps the probe's one-line diagnostic format — it was genuinely good
for reading a run — minus the retired counters.

---

## 8. Tests (Part 1)

New folder `src/Unrect.Tests/Streaming/`. House style: synthetic sources, one behaviour per
fact, names that state the rule. The probe shipped zero tests; none of the following is
optional.

`FakeRowSource` (test support) generates rows from a `Func<int, int, CellValue>`, counts
`Open()` and close calls, can be told to **throw `IOException` at row N**, and can gate an
open on a `ManualResetEventSlim` so warming races are deterministic rather than timed.

### 8.1 `SheetStoreTests`

- `DefaultChunkRows` maths at 1, 8, 64, 1024 columns; the 64 KB target respected; result
  clamped to [1, 8192].
- **`BytesPerCell` equals `Unsafe.SizeOf<CellValue>()`** — the LOH regression guard (§1.5).
- A chunk array never exceeds 85,000 bytes for any column count.
- A freshly loaded chunk over a short row is Blank in the untouched cells, with no fill pass.
- Reading every cell of a sheet taller than the window: `RowsMaterialised == RowCount`,
  `ChunkReloads == 0`, `PeakResidentChunks <= WindowChunks`.
- Re-reading a row already evicted: `ChunkReloads == 1`.
- Locus: sweeping a band that fits the window loads each chunk once, whatever the order of
  the sweep; sweeping a band one chunk taller reloads and reports `WindowOverruns > 0`.
  (**[corrected post-implementation]** The overrun is the *reset*: an extent too tall to be
  anchored at all is the band-didn't-fit event, counted once per distinct band rather than
  once per cell of it. `WindowOverruns` says a band did not fit; `ChunkReloads` says what
  not fitting cost. Measured on the canonical case — a 7-chunk band in a 6-chunk window,
  swept five times — `overruns 1, reloads 28`; the same band in a 10-chunk window, `0` and
  `0`. A plain monotone walk whose root extent exceeds the window reports `overruns 1,
  reloads 0`: the extent genuinely did not fit, and the zero reloads say it cost nothing.)
- Cross-chunk boundary reads return the right cell (the `(row - chunk*ChunkRows)` maths).
- ~~`RowCount <= 0` from the source → `NotSupportedException` naming the sheet.~~ Replaced at
  Part 2 step 7 by the measured-sheet trio, driven through `Workbook.Over(fakeSource, …)`
  because only a synthetic source can report no dimension at all: a sheet reporting none is
  measured and reads end to end, a measured sheet is still read a window at a time, and a read
  past its measured end is an `OutOfBoundsException`.

### 8.2 `ReaderPoolTests`

- Selection: with leases parked at rows 0 and 500, a request for row 600 takes the one at
  500 and leaves the one at 0 alone.
- A backward reach served by a parked lease counts `CheapRewinds`, not `Reopens`, and opens
  nothing.
- **Reopens = passes − readers**: N full passes over a sheet with M readers gives
  `Reopens == max(0, N - M)` (§1.2) for N, M in {1,2,3} × {1,2,3}.
- **`RowsSkipped` is invariant** across `MaxReaders` 1..3 for one fixed access script
  (§1.1 finding 4).
- Multi-sheet: a lease at (sheet 1, row 900) serves (sheet 2, row 0) with no open.
- A request behind every lease with the pool full recycles the *least-travelled* lease.
- Adaptive warming: no pool pressure → exactly one spare is ever warmed; one pool-pressure
  event raises the warm target by one; `MaxReaders` caps it.
- `WarmHits` is counted when the gated open completes before the reach; `WarmWait` is
  counted when it does not (gate released after the reach begins waiting).
- **Dispose racing a warmer**: `Dispose()` while a gated open is in flight; release the gate;
  `await WhenWarmersIdle()`; assert the fake source's open count equals its close count — no
  reader survives its workbook.
- `Dispose` is idempotent and does not throw.

### 8.3 `WorkbookTests`

- `Sheet(name)` twice returns views over the same store: the second map opens no reader
  (`ReaderStatistics.Opens` unchanged) and may reload no chunk.
- A view sliced with `GetSubspace` and used after further reads still returns the right cells
  (slices share the store).
- **A view read after `Dispose` throws `ObjectDisposedException`**, including when the chunk
  is still resident (the deterministic-check rule, §2.3).
- Unknown sheet name → `ArgumentException` naming the workbook and the name.
- Case-insensitive by default; `CaseSensitiveSheetNames` honoured.
- `SheetNames` returns the whole catalogue; asking for it first still lets `Sheet(name)` work
  afterwards (the retired-parked-reader path).
- `Statistics(name)` is null before the sheet is vended, non-null after.
- `Area` of a vended view equals the eager `SpreadsheetSpace.Create` view's `Area` for every
  workbook in `TestData`.
- Parallel maps over two different sheets of one book complete and agree with serial runs.
- Options validation: `WindowRows < 1`, `MaxReaders < 1`, `ChunkRows < 0` →
  `ArgumentOutOfRangeException`.

### 8.4 `StreamingStatisticsTests`

One scripted access pattern per invariant listed in §7, asserted exactly (not "greater than
zero" where an exact number is knowable).

### 8.5 `StreamingFaultTests` — the ones that matter most

For each of `IOException` and `ObjectDisposedException`, thrown from the fake source at a
chosen row, with the failure arising from:

- an **offset strategy** (`AfterBlankRows` scanning into the failing row),
- an **area strategy** (`RowsWhileAnyValue` scanning into it),
- a **landmark** (`After(To(RowContaining(...)))`),
- a **repeat separator** (`Repeat(item, separatedBy: BlankRows())`),
- a **projection** (`TableRows(r => r["Amount"].GetDouble())`),

assert that the failure is **not absorbed** by each of `.Optional()`, `.Else(fallback)`,
`.Else(value)` and `Choice(...)`, that it surfaces as a `ShapeException` whose
`GetBaseException()` is the original, and that no `Warning` diagnostic claiming the section
was absent is recorded. That is 5 × 2 × 4 combinations; a `[Theory]` over the shape
declarations and exception factories is the right form.

Controls, in the same class, so the tests prove the fault list is a discrimination and not a
blanket: a wrong-kind cell **is** absorbed by `.Optional()`; a missing anchor **is**
absorbed; a repeat still stops normally at the end of its sections.

### 8.6 `StreamingIdentityTests` — the end-to-end acceptance

For every workbook in `src/Unrect.Tests/TestData/`, and specifically for
`investor-irr.xlsx` with the existing `ShapeExampleTests.InvestorIrr()` declaration (a
`VerticalFlow` of `Column`, `TableRows`, and two `Repeat`s under captions, one of them
`Until`-bounded — i.e. exactly the backward-reaching, multi-pass shape that exercises the
pool):

```
eager    = declaration.Apply(SpreadsheetSpace.Create(path, sheet));
streamed = declaration.Apply(book.Sheet(sheet));
```

assert **identical** projected values, identical `Consumed`, and identical diagnostics
(severity, subject, message, path, location, in order). **[corrected post-implementation]**
The snippet said `MapWithDiagnostics`, whose `MapResult<T>` carries the value and the
diagnostics but no `Consumed`; `Apply` is what returns the consumed extent. Adding
`Consumed` to `MapResult<T>` would be a public-surface change and is deliberately not made
here — use `Apply` for the extent and `MapWithDiagnostics` for the diagnostics. Repeat with `WindowRows` set to one
chunk — deliberately far below the sizing law — to prove that an undersized window is slow
and never wrong.

Also assert the failure path is identical: a declaration that fails on `edge-cases.xlsx`
produces the same `ShapeException.Message` from both paths.

---

## 9. What happens to the uncommitted probe tree

| Probe file | Fate |
|---|---|
| `SpreadsheetSpace.cs` (modified) | **Reverted.** `CreateStreaming`, `Statistics`, `Pin`, `Close`, `NothingToRelease` and the `InnerSpace is WindowedSpace` type-tests all go; the eager path returns to its committed form. The one keeper is folding `Encoding.RegisterProvider` into a shared internal `SpreadsheetEncodings.Register()`. |
| `RowChunkStore.cs` | **Split and rewritten** into `SheetStore.cs` + `ReaderPool.cs` + `ReaderLease.cs`. Carrying over in substance: the chunk maths and the `BytesPerCell`/LOH commentary, the no-pre-fill note, `Anchor`/`InLocus`/`Evict`/`Victim`, and the `Position` selection policy with its doc comment (the best prose in the probe). Not carrying over: `Pin`/`Unpin`, `UseLocus`, `Slices`/`CountSlice`, `Rewinds`, `isBlank`, `RowsPerReader`-as-string, the raw-`Thread` warmer, `Adopt` in its current form. |
| `WindowedSpace.cs` | **Carries over nearly verbatim** — the offset/extent slicing and the locus signal are right. Additions: the disposed check, `OutOfBoundsException` on an out-of-range index (it currently throws bare `IndexOutOfRangeException`, which the new fault list would classify as a *bug* rather than a bounds condition — §15 #11), and XML docs. |
| `StreamingStatistics.cs` | **Rewritten** per §7 (split into two types, counters renamed, `Rewinds`/`Slices`/`Locus` retired). |

Nothing in the probe is committed as-is. Step 1 of §10.1 starts by reverting the working
tree and re-introducing the pieces behind the seam, with tests, in order.

---

## 10. Removal order

Every step ends green: `dotnet build src/Unrect.sln -v q --no-incremental` with zero
warnings, and `dotnet test src/Unrect.sln`.

### 10.1 Part 1 — nine steps

| # | Step | Ends green with |
|---|---|---|
| 1 | Revert the probe additions to `SpreadsheetSpace.cs`; add the `IRowSource`/`IRowCursor` seam and `SpreadsheetRowSource` (blankness applied here); `SpreadsheetEncodings.Register()`; `InternalsVisibleTo` for Tests and Benchmarks | source-adapter tests; existing suite untouched |
| 2 | `ReaderLease` + `ReaderPool`: lexicographic positioning, borrow/return, adaptive warming on `Task` + `CancellationToken`, `ReaderPoolStatistics` | `ReaderPoolTests` (§8.2) |
| 3 | `SheetStore`: chunks, `WindowRows`, locus, eviction, `WindowOverruns`, `StreamingStatistics` | `SheetStoreTests` (§8.1), `StreamingStatisticsTests` (§8.4) |
| 4 | `WindowedSpace`: lent view, disposed check, bounds, locus signal | view tests |
| 5 | `Workbook` + `WorkbookOptions`: parked-reader catalogue, `Sheet` idempotence, `Statistics`, `Dispose` | `WorkbookTests` (§8.3) |
| 6 | IO fault discipline: `IsFault` rename + list + all four wrap sites + `ShapeContext` overload | `StreamingFaultTests` (§8.5); the whole existing suite green **unchanged** |
| 7 | End-to-end identity over `TestData` | `StreamingIdentityTests` (§8.6) |
| 8 | The `Streaming` benchmark family (§12) + the three workflow lists + `docs/benchmarking.md` family count | benchmarks run locally with `--job short` |
| 9 | Docs: XML on the public surface, `docs/streaming.md` (cost model §2.7 + the idiom §2.1), README section, `CLAUDE.md` status and known-limitations updates | — |

Step 6 is independent of steps 1–5 and could be done first; it is placed after them so
`StreamingFaultTests` has a fake source to throw from. It must not be deferred past step 7:
shipping a streaming space whose IO errors are absorbable is the one outcome this spec exists
to prevent.

### 10.2 Part 2 — eight steps, gated on Part 1's merge

Ordered so value lands early and the hardest step is last and droppable. Stopping after any
step leaves a coherent system.

| # | Step | Value if you stop here |
|---|---|---|
| 1 | Core: `IIncrementalRowStrategy`/`IRowScan` with the **definitional fold** as a default interface member; implement on `TakeWhileAnyRowStrategy`, `TakeWhileAllRowStrategy`, `TakeToRowStrategy` | none yet; eager behaviour provably unchanged (existing `StrategyTests` untouched) |
| 2 | Row-major rewrite of `TakeWhileAnyColumnStrategy` / `TakeWhileAllColumnStrategy` (§11.3), denotation-identical | a genuinely better column scan for every path |
| 3 | `IIncrementalSizeStrategy` / `IIncrementalAreaStrategy` and the lifting rules through `RowsWhileAnySizeStrategy` and the `ToAreaStrategy` adapter | none yet |
| 4 | `BoundedSpace`: the lazily bounded space, forcing rules, pre-built failure identity | none yet |
| 5 | Engine: `TryPlace` defers for incremental strategies **when strict**; `Project` receives the bounded extent; consumed forced afterwards | **the win, for fixed-width declarations** |
| 6 | Views: `TableView.StreamRows()`; the three built-in row projections consume it; `Rows`/`RowCount`/`Location` documented as forcing | **the win, for `TableRows`** |
| 7 | Growable resident index in `SheetStore`; drop the `RowCount > 0` restriction | sheets with no `dimension` element work |
| 8 | `RowAndColumnSizeStrategy` implements `IIncrementalAreaStrategy` by **interleaving** its row scan and its column scan (§11.4) | **the win, for `Table`'s default placement** |

---

## 11. Part 2 in detail: lazy extents

### 11.1 The insight, and why it is still declarative

An area strategy need not answer *"how tall are you"* up front. The bound can be a per-row
stop predicate evaluated as the projection consumes. Same denotation, interleaved
evaluation.

This does not weaken the algebra's promise — *declarations decide boundaries, projections
never do*. The predicate that decides the boundary is still the declaration's, still written
before any data was seen, still incapable of being influenced by the projection. What changes
is only *when* it runs. It is bound+project fusion: deforestation round two, after wave 2
fused shape and projection.

The raw material is already there:

- The strategies' predicates **are** per-row already. `TakeWhileAnyRowStrategy` is a
  `while (row < height) { if (no cell matches) return row; row++; }` loop — a per-row
  predicate behind an eager veneer.
- `ShapeResult.Consumed` already reports extent post-hoc for undeclared areas, so the channel
  for "how much did this actually take" exists and is load-bearing.
- Sibling placement in a flow only needs a child's consumed extent **after** that child
  completes (`FlowState.Advance` runs on the way out), so nothing upstream needs the height
  early.

### 11.2 The interfaces

In `Unrect.Core`, additive, mirroring the existing three-layer strategy calculus exactly
(row → size → area). Each layer's eager method is *defined* as the fold of its own scan, as a
default interface member — so eager and lazy cannot disagree by construction, which is worth
more than any number of equivalence tests. (netstandard2.1 supports DIMs; the capability-seam
note already blesses them.)

```csharp
public interface IRowScan
{
  /// Called with row = 0, 1, 2, … in order, never repeated, never skipped. True means the
  /// row is inside the extent; false ends it. May carry state (TakeRowsTo keeps a bit).
  bool IncludesRow(ISpace space, int row);
}

public interface IIncrementalRowStrategy : IRowStrategy
{
  IRowScan BeginRows();

  int IRowStrategy.SelectRows(ISpace space)          // the definitional fold
  {
    var scan = BeginRows();
    var count = 0;
    while (count < space.Area.Height && scan.IncludesRow(space, count))
      count++;
    return count;
  }
}

public interface IAreaScan
{
  /// The width, decided when the scan begins. Deciding it MAY consume leading rows through
  /// this same scan (§11.4); it may never consume rows the height scan would not.
  int Width { get; }
  bool IncludesRow(ISpace space, int row);
}

public interface IIncrementalAreaStrategy : IAreaStrategy
{
  IAreaScan BeginArea(ISpace availableSpace);

  Area IAreaStrategy.GetArea(ISpace space) { /* the same fold, returning new Area(scan.Width, h) */ }
}
```

`IIncrementalSizeStrategy` is the same shape at the size layer; the `ISizeStrategy →
IAreaStrategy` adapter forwards incrementality when its inner strategy has it, and does not
when it does not. (Incidentally, this is the first argument in favour of the three-layer
strategy calculus that CLAUDE.md lists as an open question: laziness lifts cleanly through
each layer precisely because the layers exist.)

### 11.3 Which strategies go lazy, and which must not

| Strategy | Lazy? | Why |
|---|---|---|
| `TakeRowsWhile`, `TakeRowsWhileAll`, `TakeRowsWhileAny`, `TakeRowsWhileAnyValue` | **Yes** | Already per-row predicates. |
| `TakeRowsTo`, `TakeRowsToValue` | **Yes** | Per-row, plus one bit of state for `keepMatchingRow`. This is why `IRowScan` is an object and not a `Func`. |
| `RowsWhileAny` / `RowsWhileAnyValue` (size) | **Yes** | Lifts its row strategy. |
| `TakeRows(n)` / explicit sizes / `ExplicitArea` | **No, and no loss** | They read no cells; they are already free. Their `OutOfBoundsException` on overrun is a declaration guarantee a lazy loop (which does not know the height) cannot express. |
| `MaxSize` | **No, and no loss** | Reads no cells. |
| `SkipBlankRows` and the offset family | **No** | An offset must be resolved before the extent exists — there is nothing to defer it past. They are also short by nature. |
| Landmark row strategies (`To`, `Past`), `Until` | **Yes in principle, not in these eight steps** | The scan is forward and per-row, but `Until` is a wrapper shape rather than an area strategy and needs its own plumbing. §13. |
| Column strategies | **No** — but they are made *bound-aware* | Below. |

**The column problem, and the fix.** `TakeWhileAnyColumnStrategy` is column-major: for each
column, scan down rows until a value is found. Its inner loop reads `space.Area.Height`,
which on a lazily bounded space **forces the whole scan** — so `Table`'s default placement
(`TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue()`) would force at placement time and
Part 2 would deliver nothing for the commonest declaration in the library.

Step 2 rewrites those two strategies **row-major with an early exit**, which is
denotation-identical:

> Column `c` is included iff some row has a value at `c`, and columns are taken while that
> holds contiguously from 0. Row-major: walk rows forward, marking columns; stop as soon as
> the leading marked run reaches the full width (the common dense case: one row), or when the
> rows run out.

Same answer, same order of magnitude of cell reads, one forward pass instead of one pass per
column, and — crucially — it never asks for `Height`.

### 11.4 The width/height interleave (step 8)

`RowAndColumnSizeStrategy` with `RowFirst = true` — `Table`'s default — measures rows over
the full width, then columns *within the discovered band*. The width therefore depends on the
row bound, and `IAreaScan.Width` must be answerable before the projection starts.

The resolution is that both scans consume the same rows in the same order, so **one forward
walk serves both**: `BeginArea` advances the row scan, feeding each accepted row into the
column accumulator, until the column answer is settled (one row, on dense data) or the row
scan stops. The rows it consumed are exactly the rows the height scan would have consumed
first, so nothing is read twice and nothing is read early. From there the height stays lazy.

If the data is sparse enough that the column answer needs the whole band, the width decision
forces the whole bound — correctly and honestly, under the same hybrid rule as any other
dimension query.

This is the one genuinely fiddly step, which is why it is last. Without it, Part 2 still
delivers laziness for every declaration whose width is fixed — full available width
(`.Sized(RowsWhileAnyValue().ToAreaStrategy())`), explicit widths, `Column(n, …)` — and
`docs/streaming.md` documents that spelling as the streaming idiom.

### 11.5 The hybrid rule: what forces

Forward-only consumption streams; a dimension query forces. Exactly:

| Member | Behaviour on a lazily bounded space |
|---|---|
| `Area.Width` | Decided when the scan begins (§11.4). Never forces the height. |
| `Area.Height` | **Forces** the scan to completion. |
| `this[column, row]` | Forces the scan **through `row` only**. If the scan stopped at or before `row`, throws `OutOfBoundsException`. |
| `GetSubspace(offset, area)` | Forces through `offset.Height + area.Height` — an explicit request, not the whole bound. |
| `TableView.StreamRows()` (new) | Streams. Forces one row per `MoveNext`. |
| `TableView.Rows`, `.RowCount`, `.Location`, `.Failure(...)` | **Force** — they are dimension queries or diagnostics. Documented as such. |
| `CellStrip` / `CellBlock` enumeration | Streams where the extent is the lazy one; the views' `Count` forces. |

`TableRows<T>()`, `TableRows<T>(project)` and `TableRows()` are rewritten to consume
`StreamRows()`. The *result* list is still materialised — that is the caller's memory, not
ours — but the space is consumed forward-only, which is what halves `RowsMaterialised`. A
streaming result type is deferred (§13).

### 11.6 Engine changes

```
TryPlace:
  if shape.Placement.Area is IIncrementalAreaStrategy incremental AND strict:
      scan   = incremental.BeginArea(inner)             // may throw; same catches as today
      extent = new BoundedSpace(inner, scan, failure: <the ShapeException TryPlace would throw>)
      placed = new Placed(offset, extent, scope, hasDeclaredArea: true, bound: extent)
  else: exactly as today

Project:
  result   = shape.Project(placed.Extent, placed.Scope)         // unchanged
  consumed = placed.HasDeclaredArea
               ? (placed.Bound?.ForceResolved() ?? placed.Extent.Area.Size)
               : result.Consumed
```

Three rules make this denotation-preserving:

1. **A declared area is consumed in full**, exactly as today — so a projection that read only
   three rows still consumes the whole bound, which means the engine forces the bound after
   projecting. For the canonical case (a table that reads every row) the projection has
   already forced it and the force is a no-op; the work is never done twice.
2. **Laziness is disabled for a non-strict placement** (`TryApply`, i.e. a `Repeat` item). A
   repeat's stopping condition is its item's *placement* failing; a deferred failure would
   arrive after the item had been collected. Repeat items are small — a block, a row — so
   nothing is lost. This is a hard rule, not an optimisation.
3. **A deferred failure is the placement's failure, not the projection's.** `BoundedSpace` is
   constructed with the exact `ShapeException` `TryPlace` would have thrown, and throws *that*
   when the scan overruns. `ShapeEngine.Project` already rethrows `ShapeException` untouched,
   so the message, path, location and fault flag are identical to the eager path's. Only the
   *moment* differs.

### 11.7 Store interaction

- The row scan is always at or ahead of the projection, and it advances **only on demand** —
  it never reads ahead speculatively. So lead and chase stay within one chunk of each other
  and the store sees a single monotone pass. This is the within-pass lead/chase the
  `Position(startRow)` contract already serves: keyed purely on row number, with no assumption
  about pass structure. Nothing in the pool changes for Part 2.
- The logical extent becomes discovered, which is why step 7 makes the resident index growable
  (a chunk-keyed growable map instead of an array sized from `RowCount`). That also removes
  the `RowCount > 0` restriction of §4.3 as a side effect.

### 11.8 The expected win

On the canonical monotone `TableRows<Row>()` parse of the 1M-row workbook: the second pass is
the bound scan, and lazy binding removes it. `RowsMaterialised` **2,000,002 → 1,000,001**,
closing most of the remaining 19.6s-vs-14.7s gap against eager.

Precondition, stated so the number is not quoted out of context: the win requires the width
not to be discovered over the whole band — i.e. either a fixed-width declaration (available
from step 5) or the interleave of step 8. If step 8 is dropped, `Table`'s default placement
keeps both passes and the win applies only to explicitly-sized declarations.

### 11.9 Part 2 tests

- **`LazyDenotationTests` — the differential suite.** The pattern from the flow era: run every
  shape-example declaration in the suite twice, once with lazy binding enabled and once forced
  eager (an internal, test-only engine switch), and assert *identical* values, identical
  `Consumed`, identical diagnostics in order. This is the primary evidence and it should be a
  `[Theory]` over the existing declarations, not new ones.
- **`LazyForcingTests`** — one fact per row of the §11.5 table, asserting through a counting
  space how many rows were touched. `Area.Height` touches all; `this[c, r]` touches `r+1`;
  `StreamRows().Take(3)` touches 3 plus the header.
- **`LazyErrorTimingTests`** — a declaration whose bound overruns produces the same
  `ShapeException.Message`, `Path`, `Location` and `IsFault` from both paths; an `IOException`
  during a deferred scan is still a fault (§6 composes with §11); a `Repeat` item never defers
  (rule 2 of §11.6), pinned by asserting the repeat still stops rather than throwing.
- **`IncrementalStrategyTests`** — the fold identity: for each incremental strategy,
  `SelectRows(space)` equals the manual fold of `BeginRows()` over the same space, on dense,
  sparse, empty and all-blank grids.
- **`ColumnStrategyRewriteTests`** — the row-major rewrite agrees with the old column-major
  answer on a matrix of shapes (dense, ragged, a hole in column 3, all-blank, one row, one
  column).

---

## 12. The `Streaming` benchmark family

One class, `src/Unrect.Benchmarks/Streaming.cs`, `[BenchmarkCategory("Streaming")]`,
`[MemoryDiagnoser]` — one class per family, the family is the first category, per
`docs/benchmarking.md`. Fixture: a synthetic `IRowSource` (§3) in `StreamingSpaces.cs`, not a
workbook — CI runners get none. Size it so every row lands between 50 ms and 1 s per op and
record the constant at its declaration; **250,000 rows × 8 columns** is the starting point.

| # | Benchmark | What it answers |
|---|---|---|
| 1 | `Monotone_Eager` | The ratio baseline: the same declaration over a `GridSpace` built from the same source. |
| 2 | `Monotone_Windowed` | The headline: a monotone `TableRows<T>()` through the window. Read as a ratio against #1 (same family, same run — the only trustworthy comparison). |
| 3 | `Band_WindowFits` | A `HorizontalFlow` of 5 children over a band **inside** the window: the good case (§1.3, 0.01s). |
| 4 | `Band_WindowTooSmall` | The same declaration with the window one chunk short: the collapse the sizing law exists to prevent. #3 vs #4 is the sizing law's trend line. |
| 5 | `Adversarial_OneReader` | Top/bottom/top/bottom reaches with `MaxReaders = 1`. |
| 6 | `Adversarial_Pooled` | The same with `MaxReaders = 3`. #5 vs #6 is the pool's trend line. |
| 7 | `Monotone_Resident` | **[corrected post-implementation]** The warm-reuse row, added during implementation: a second pass over a sheet already read, with a window large enough to have kept it. No reader opened and no row re-read — the property that makes `Sheet(name)`'s idempotence worth relying on, and the one the table originally had no row for. |

Honesty note for the class doc: with a synthetic source an "open" is free, so #5/#6 measure
the *repositioning* half of the pool's value and not the ~5s open half, which is a property of
ExcelDataReader and is deliberately measured nowhere in CI. Check outputs, not just timings
(`docs/benchmarking.md`: both fidelity bugs found while building the rig produced plausible
timings of the wrong thing) — every benchmark returns a checksum of what it read.

Wiring: add `Streaming` to the three lists in `.github/workflows/benchmarks.yml` (the matrix
at ~line 46, the results loop at ~line 130, the Bencher loop at ~line 386) and update the
"34 benchmarks in six families" line in `docs/benchmarking.md`.

---

## 13. Deliberately deferred

| Deferred | Trigger to revisit |
|---|---|
| `Workbook.Sheets` enumeration (vending every sheet as spaces) | A real multi-sheet declaration. `SheetNames` + `Sheet(name)` covers the demand seen so far. |
| A public `IRowSource` (CSV, database cursors, Parquet) | A second implementation that argues with the interface. |
| Async (`OpenAsync`, async cell access) | Nothing in the shape layer is async and making it so is a whole-surface change. The open is CPU-bound anyway (§1.1), so async would not help it. |
| Concurrent chunk loads within one sheet | A measured parallel-map-over-one-sheet workload. |
| A streaming *result* type (`TableRows` yielding `IEnumerable<T>`) | A caller whose result set, not whose input, is the memory problem. |
| Lazy bounds for `Until`/landmark wrappers | After Part 2 step 8 lands. |
| **Part 3: bound-aware composite placement** — a re-based `BoundedSpace` for `GetSubspace(offset)` (sharing the parent's scan at a row offset) and `Exceeds` answered via `HasRow`, so a sized composite's band settles when its last child finishes rather than before its first child is placed. DEFERRED (owner, 2026-09-04). This is principled completion of the lazy-extents thesis, not an edge case: the engine's remaining greed is one *necessary* force (`Repeat` items — the item's existence is the question), one *free* force (post-`Project` consumption, amortised by the root's unconsumed-space accounting), and this one *debt* (composite child placement, whose questions have lazy answers nobody asks for). `.Sized`'s irreplaceable role is exactly on composites — a composite has no intrinsic extent, and a declared band is what scopes its internal seeks and settles its consumption (the K-1 `Overlay` header is the corpus's one production use) — so the docs' "prefer the leaf spelling" is a partial answer, not a law. Priced at roughly steps 4–6 of Part 2; wrinkles: the offset-only `GetSubspace` extension lives in `Unrect.Strategies`, below `BoundedSpace`, so the engine routes around it rather than the extension type-testing upward; `Overlay` children force inherently (they ask dimension questions to place themselves), so the win is flow-shaped. | The first real declaration that pays the debt: a *tall* sized composite — a long heterogeneous region bounded by a content rule. Sized composites in the corpus are short (header bands), where forcing costs nothing. The K-1 campaign is the likely judge. Pinned meanwhile by the `LazyDenotationTests` census (`ASizedLayoutCompositeIsEagerBothWays…`), which must be flipped deliberately. |
| A reader budget shared across many open workbooks | Someone opens hundreds at once. |
| `OperationCanceledException` in the fault list | An async or cancellable surface exists to produce it. |

---

## 14. Open questions

Two, both narrow, neither blocking.

1. ~~Should `Table`'s default width discovery change?~~ **DEFERRED (owner, 2026-09-04),
   superseding the 2026-09-03 "yes — header-derived".** Part 2 did not take the directed
   kickoff step; the step-8 interleave delivered the entire lazy win with today's
   denotation intact, so the performance half of the rationale is gone and only the
   semantic argument ("a table is as wide as its header") remains — against the full cost
   of a denotation change (wider-than-header data rows would clip; gated step, respelled
   pins, re-verified known-goods). The K-1 campaign votes: if real sections show
   header-clipping helping rather than hurting, implement then. Original decision text
   kept below for the record. `Table`'s default width becomes "columns while the header row carries
   captions" (a one-row scan; the strategy already exists — phase A's mirror hygiene built
   `TakeColumnsWhile(row, predicate)` for exactly this shape of need). "A table is as wide
   as its header" is the truer reading; other width strategies can be supported later if a
   real file demands them (the explicit-width and strategy overloads already exist for
   opting out). Consequences: `Table`'s default lands on the lazy fast path with NO
   interleave, making Part 2 step 8 droppable for the default case; and it is a DENOTATION
   change — data rows wider than the header now clip to header width — so it lands as its
   own gated step with its own pins (width-discovery tests respelled deliberately, script
   known-goods re-verified) at the Part 2 kickoff, not smuggled into a performance step.
2. ~~Does `MaxReaders = 3` want to be adaptive too?~~ **DECIDED (owner, 2026-09-04): 3
   stays, no longer provisional — and no number is "right", which is the decision.** Reader
   demand is the count of monotone cursors a declaration holds open at once (backward
   reaches spanning more than the window, landmark lookaheads, sheet alternations); it is a
   static property of the declaration, unbounded in principle because declarations compose
   — for any fixed N there is a declaration wanting N+1. So the ceiling is a user-settable
   cost knob, not a number to get right: it fails in the gentle direction (`Reopens =
   passes − readers`, counted and named — time, never wrongness — where undersizing
   `WindowRows` is collapse), the demand is data-independent (one glance at `Reopens` after
   the first file of a monthly-close loop settles the setting for the campaign), and the
   per-reader economics keep sane values in the single digits (an open is ~5s CPU on a
   1M-row file — its own shared-strings parse — and a reader's position must be *walked*,
   so reader-per-row is O(n²); readers are expensive to create, cheap to hold, valuable
   only for their position). 3 = lead + chase + spare covers one backward level plus one
   lookahead — every declaration in the corpus. The scenario matrix demotes from decision
   input to a docs illustration. A declaration reporting its own cursor demand would be the
   principled upgrade, but is blocked on layout-composite opacity, same as the dry-run
   renderer; it falls out for free if wave-3 tooling solves introspection.

---

## 15. Decisions taken here, beyond the owner brief

| # | Decision | § |
|---|---|---|
| 1 | An internal `IRowSource`/`IRowCursor` seam; blankness moves into the adapter | 3 |
| 2 | The window knob is expressed in **rows** (`WindowRows`), not chunks | 2.2, 4.3 |
| 3 | The reader pool is **workbook-level**, positions keyed on (sheet, row) lexicographically | 5.1 |
| 4 | Statistics split into `StreamingStatistics` (per sheet) + `ReaderPoolStatistics` (per workbook); `Statistics(name)` is nullable | 2.2, 7 |
| 5 | The adaptive-warming trigger is a **pool-pressure event** (spare open *or* reopen), because a literal reopen-only trigger is unreachable | 5.3 |
| 6 | `MaxReaders` defaults to 3; `WindowRows` to 8,192 | 2.2, 5.3 |
| 7 | Lazy catalogue with a **parked, adoptable** reader; `SheetNames` forces the walk and costs one extra open | 2.5 |
| 8 | Two-lock scheme (store gate → pool gate) so different sheets of one book do not serialise, as promised | 4.4, 5.4 |
| 9 | `OutOfMemoryException` joins the fault list; `ObjectDisposedException` is named explicitly rather than reached via `InvalidOperationException` | 6.2 |
| 10 | `IsProjectionFault` is renamed `IsFault` (internal, non-breaking) since it now covers placement | 6.2 |
| 11 | `WindowedSpace` throws `OutOfBoundsException`, not bare `IndexOutOfRangeException`, for an out-of-range index — otherwise the new fault list classifies a bounds condition as a bug | 9 |
| 12 | `BytesPerCell` is asserted against `Unsafe.SizeOf<CellValue>()` in a test | 8.1 |
| 13 | The definitional fold as a default interface member, so eager and lazy cannot disagree by construction | 11.2 |
| 14 | The row-major rewrite of the `WhileAny`/`WhileAll` column strategies, without which Part 2 delivers nothing for `Table` | 11.3 |
| 15 | Laziness is disabled for non-strict placement, so a `Repeat`'s stop condition is unaffected | 11.6 |
| 16 | The benchmark family measures a synthetic source, with the free-open caveat documented | 12 |
| 17 | **[corrected post-implementation]** The adoption slot is reserved from the moment the parked reader is opened, so a slot that will be adopted is never a warm target — the structural form of the "exactly one spare" policy | 2.5, 5.3 |
| 18 | **[corrected post-implementation]** `WindowOverruns` counts the locus *reset*: one band-didn't-fit event per distinct oversized extent. `ChunkReloads` remains the cost meter | 4.3, 7, 8.1 |
| 19 | **[corrected post-implementation]** The catalogue walk is servable by any pool lease, not only the parked reader, so sheets ahead of the first-adopted one stay reachable | 2.5 |
