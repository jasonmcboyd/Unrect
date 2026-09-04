# Streaming: Reading a Workbook a Window at a Time

`SpreadsheetSpace.Create` reads a sheet whole: one file open, one pass, the whole grid
resident before any shape sees it. `Workbook`, in the same `Unrect.Spreadsheets` package,
reads the same files a *window* at a time — a bounded band of rows held in memory, refilled
from the file as a shape's reads move past it. Same shapes, same results; the two doors
differ only in the shape of their cost. This is the user-facing guide to that door: when to
reach for it, the lifecycle rules, the sizing law, the statistics vocabulary, and the
limits that are honestly still limits.

For the mechanics behind every claim here — the load loop, the reader pool's selection
policy, the lock ordering — see `docs/design/streaming-spec.md`, the implementer's document.
This file is the other one: what a caller needs to know to use `Workbook` correctly, not to
build it.

## When to reach for it

**Eager stays the default.** `SpreadsheetSpace.Create` is simpler, and for anything that
fits comfortably in memory it is also faster and holds nothing back. Reach for `Workbook`
when a file is too large to hold whole, or when one declaration runs over many files in
sequence and the peak per iteration matters more than the total across the run.

The cost model is **declaration-shaped**, not a flat tax — what a declaration does with the
sheet determines whether streaming is cheap, free, or a bad idea:

- **A monotone walk down a sheet costs moderately more than eager, for substantially less
  live memory.** Measured: about 35% more wall time for about 2.7× less live memory than
  `SpreadsheetSpace.Create`. What streaming removes is the materialised grid, not the
  parser — the reader's shared-string table still holds every string a `Text` cell points
  at, is not part of the window, and does not shrink with it. On a text-heavy sheet that
  table can dominate either way.
- **Warm reuse is cheaper than eager on a second pass.** `Sheet(name)` is idempotent:
  calling it twice for the same sheet returns views over one store, so a second declaration
  mapped over an already-open `Workbook` pays no reader open and, if the rows it wants are
  still in the window, no re-read either. A second `SpreadsheetSpace.Create` call has no
  such thing — it re-parses the file from nothing every time. This is the property that
  makes holding a workbook open worth doing, and it is pinned by
  `Unrect.Benchmarks.Streaming.Monotone_Resident`, measured against `Monotone_Eager` and
  `Monotone_Windowed` in the same run.
- **A band that fits the window is free.** Several children sweeping one region — a
  `HorizontalFlow`, an `Overlay` — each load every chunk of that region exactly once,
  whatever order they read in, when the window holds the whole region. Verified case: a
  five-chunk band swept three times inside a six-chunk window costs 5 chunk loads and
  reloads nothing (`SheetStoreTests.ABandThatFitsTheWindowReportsNoOverrun`).
- **The degenerate cases are documented, not silent.** Grow that same band past the window
  by one chunk and the same three sweeps cost 21 chunk loads, 14 of them reloads — the
  shortfall compounds with every pass
  (`SheetStoreTests.ABandOneChunkTallerThanTheWindowReportsOneOverrunAndPaysForItInReloads`).
  And a backward reach with no reader parked behind it costs a full reader reopen (the
  ~5s, CPU-bound `ExcelDataReader` open) unless the pool has a spare —
  `ReaderPoolStatistics.Reopens` is exactly `passes − readers` for a fixed access pattern,
  so undersizing `MaxReaders` for a declaration that keeps several passes open at once is
  the other way to pay for this door twice. Both failure modes have a counter that says
  so — see [The sizing law](#the-sizing-law) and the statistics table below — so "slow" is
  always diagnosable, never mysterious.
- **A row-wise leaf extent reads its rows once, not twice.** Where a **leaf** extent is
  sized by a per-row rule — `Range(RowsWhileAnyValue(), …)`, a `Range` or a `Table` left on
  its default placement, and `.Sized(RowsWhileAnyValue())` applied *directly to one of
  those* — its height is discovered *as the projection consumes it* rather than measured
  first, so the rows pass the window once. The three built-in table projections
  (`TableRows<T>()`, `TableRows()`, `TableRows(row => …)`) are written against that reading,
  through `TableView.StreamRows()`, and so is a block read by `Row(i)` or
  `block[column, row]`. A **dimension query** asks how far the extent goes and settles it
  there and then: `TableView.Rows`, `.RowCount`, `.Location`, `CellBlock.Height`, `.Rows`,
  `.Columns`, `.Column`, `.Location`, `.AddressOf`, and `ISpace.Area` itself. Widths are
  free either way.
- **Laziness stops at the first composite.** A `.Sized(RowsWhileAnyValue())` on a
  `VerticalFlow`, a `HorizontalFlow` or an `Overlay` resolves eagerly, in full, today.
  Placing that composite's first child asks the parent extent whether the child fits and
  then slices it, and both questions settle the bound before any child projection runs — so
  a lazy extent buys nothing once there is a box around it. Put the `.Sized` on the leaf
  that reads the rows rather than on the flow that holds it. This limit is pinned by
  `LazyDenotationTests`' census, so lifting it is a deliberate change rather than an
  accident.
- **A width discovered from the data costs the rows it takes to settle, and no more.** A
  `Table` on its default placement finds its width from the sheet too, in the *same* forward
  walk as its height: each row the height rule accepts is fed to the width rule as it is
  taken, and the walk stops as soon as no further row could change the width. On the usual
  sheet — a full header row, or a first body row with every column occupied — that is one
  row, and the table then streams exactly as a fixed-width one does. Where a leading column
  is blank for a long stretch, settling the width honestly costs the rows it takes to fill
  it, and the extent is forced that far before the projection sees its first row. That is
  the one case where `.Sized(RowsWhileAnyValue())` — full available width, nothing to
  discover — still buys something.

## The `Workbook` / `Sheet` lifecycle

```csharp
using var book = Workbook.Open(path);              // owns file handles, reader pool, chunk stores
var result = projection.Map(book.Sheet("Data"));    // Sheet(name) vends a lent ISpace view
```

- **The workbook owns everything disposable**: file streams, readers, background warming
  tasks, and every sheet's chunk store.
- **A vended view is a value, not a handle.** `Sheet(name)` returns an `ISpace` with no
  `Dispose` of its own. It can be sliced (`GetSubspace` returns another view over the same
  store, at no extra memory cost), passed to any shape, and held as long as the caller
  likes. The only thing that invalidates it is the `Workbook` it came from being disposed.
- **`Sheet(name)` is idempotent.** Repeated calls for the same name return views over one
  store — this is the warm-reuse property the cost model above depends on. Resolution
  respects `WorkbookOptions.CaseSensitiveSheetNames` (default off, as the eager path); an
  unknown name throws `ArgumentException` naming the sheets seen so far.
- **A read after `Dispose` throws `ObjectDisposedException`, deterministically** — whether
  or not the chunk it wants happens still to be resident. The check runs before the
  resident-chunk fast path on purpose, so a stale read can never accidentally succeed.
  This exception is a **fault** (see [IO errors are faults](#io-errors-are-faults)): no
  tolerance boundary may absorb it as "section absent".
- **Disposing while a map is running is a caller error, not corruption.** The running map
  fails with `ObjectDisposedException`, wrapped by the engine as a `ShapeException` naming
  the shape and the cell it was reading.
- **`Dispose` is idempotent and never blocks on a background warm.** A warm reader that is
  mid-open when `Dispose` runs finishes its open and then discovers the workbook is gone,
  disposing what it just opened rather than parking it — so returning promptly still
  leaks no file handle.
- **Concurrency**: maps over *different* workbooks are fully parallel; maps over
  *different sheets* of one workbook run in parallel (lease *selection* briefly serialises
  on the pool, the actual row streaming does not); maps over *one* sheet serialise on that
  sheet's store — a documented v1 limitation, not a correctness one (see
  [Honest limits](#honest-limits)).

**The idiom `Workbook` exists for** — one declaration, reused across a directory of files,
with the peak bounded per iteration instead of by the largest file in the run:

```csharp
var report = VerticalFlow(v => ...);              // one declaration, reused

foreach (var path in monthlyCloseOfFunds)
{
  using var book = Workbook.Open(path);
  Publish(report.Map(book.Sheet("Detail")));       // bounded memory per iteration
}
```

Shapes are immutable and thread-safe, and workbooks are independent of each other, so this
loop parallelises with nothing added:

```csharp
Parallel.ForEach(monthlyCloseOfFunds, path =>
{
  using var book = Workbook.Open(path);
  Publish(report.Map(book.Sheet("Detail")));
});
```

## The sizing law

**`WorkbookOptions.WindowRows` (8,192 rows by default) must be at least as tall as the
tallest extent a declaration holds open at one time.** A vertical walk down a sheet has one chunk open at a time. A
`HorizontalFlow` or `Overlay` over a band has the *whole band* open, because every child
reads across it before any of them advances. Undersizing the window is not a gentle
slowdown — it is collapse, and the shortfall compounds with every pass over the band (the
five-vs-seven-chunk pair above is the miniature of it). The design probe that set this law
measured a ten-chunk window over a seven-chunk band at 0.01s against a four-chunk window
over a thirteen-chunk band at 29.5s — three orders of magnitude from one chunk of
shortfall (`docs/design/streaming-spec.md` §1.3); `Streaming.Band_WindowFits` vs
`Streaming.Band_WindowTooSmall` is that law's benchmark trend line going forward.

Two counters divide the diagnosis between them, both on `StreamingStatistics`:

- **`WindowOverruns`** says a band **did not fit** — once per distinct extent too tall to be
  held, regardless of how many cells it contains or how many times it is read.
- **`ChunkReloads`** says **what not fitting cost** — how many chunk loads were of a chunk
  the window had already thrown away.

Raise `WindowRows` when both are non-zero together; that pairing is the collapse. A single
`WindowOverruns` with `ChunkReloads` at zero is not a problem — see the next paragraph.

### The counterintuitive reading: a plain walk down a tall sheet reports one overrun that costs nothing

`WindowOverruns` is counted against the *extent a view declares*, not against the access
pattern read through it. `book.Sheet(name)` vends a view whose `Area` is the **whole
sheet**, and that declared extent travels down to every cell read through it unless a
narrower `GetSubspace` slice is in play. So a plain walk down a sheet taller than the
window — no flow, no overlay, nothing that holds a band open on purpose — still reports
exactly **one** `WindowOverruns`, because the root extent itself (the whole sheet) does not
fit the window. It costs nothing, because nothing about a monotone walk ever asks for a
chunk twice:

```csharp
using var book = Workbook.Open(path, new WorkbookOptions { WindowRows = 256 });
var space = book.Sheet("Ledger");              // 1,201 rows tall

for (var row = 0; row < space.Area.Size.Height; row++)
  _ = space[0, row];

var stats = book.Statistics("Ledger")!.Value;
// stats.WindowOverruns == 1   (the whole-sheet extent did not fit the window — expected)
// stats.ChunkReloads   == 0   (nothing was ever read twice — it cost nothing)
```

Pinned by `WorkbookTests.AWalkDownASheetTallerThanTheWindowReportsOneOverrunThatCostNothing`
and its control, `AWalkDownASheetThatFitsTheWindowReportsNoOverrunAtAll` (a sheet small
enough to be held whole reports neither counter). Read `WindowOverruns` in isolation as "a
band this large did not fit" — true and unremarkable for any sheet taller than the window —
and read the *pair* with `ChunkReloads` as the number to act on.

## The statistics vocabulary

`Workbook.Statistics(sheetName)` returns `StreamingStatistics?` — null until that sheet has
been vended, non-null after. `Workbook.ReaderStatistics` returns `ReaderPoolStatistics`,
shared across every sheet of the book. Both render a one-line diagnostic via `ToString()`:

```
'Data' chunk 10r x 6 (60 rows) | loads 14 (reloads 7) | evictions 8 | overruns 1 |
  rows read 140 skipped 100 | resident 6 chunks / 2,880B (peak 6 / 2,880B)

readers 1/2 | opens 1 | reopens 0 | spare opens 1 (warm 0, waited 0ms) |
  cheap rewinds 0 | per reader 10/0
```

### `StreamingStatistics` — what reading one sheet has cost

| Member | Meaning |
|---|---|
| `SheetName` | The sheet these numbers describe. |
| `ChunkRows` | Rows in one chunk — the unit the window is loaded and evicted in. |
| `WindowChunks` | The window budget, in chunks. |
| `WindowRows` | The window budget, in rows (`ChunkRows × WindowChunks`). |
| `ChunkLoads` | Chunks materialised, re-materialisations included. |
| `ChunkReloads` | Loads of a chunk this store had already thrown away — the cost half of the sizing-law pair. |
| `Evictions` | Chunks dropped to stay inside the budget. |
| `WindowOverruns` | How many times a band did not fit the window — the diagnosis half of the pair; see [the counterintuitive reading](#the-counterintuitive-reading-a-plain-walk-down-a-tall-sheet-reports-one-overrun-that-costs-nothing) above. |
| `RowsMaterialised` | Rows read from the source and adapted into cells. |
| `RowsSkipped` | Rows parsed and discarded to move a reader to a wanted chunk — owned by window sizing, invariant under `MaxReaders`. |
| `RowsMeasured` | Rows read by the survey that sized a sheet with no `dimension` element; `0` for a sheet that reported its own. Above zero means a whole extra forward pass over the file was paid for before the window saw anything. Appears in `ToString()` only when non-zero. |
| `ResidentChunks` | Chunks held right now. |
| `PeakResidentChunks` | The most chunks ever held at once; never exceeds `WindowChunks`. |
| `ResidentBytes` | Bytes of `CellValue`s resident right now. |
| `PeakResidentBytes` | The same at the peak. **Not the whole floor**: strings a `Text` cell points at live in the reader's shared-string table, are not counted here, and do not shrink with the window. |

### `ReaderPoolStatistics` — what one workbook's readers have cost

| Member | Meaning |
|---|---|
| `MaxReaders` | The ceiling on readers held open at once (`WorkbookOptions.MaxReaders`). |
| `ReadersOpen` | How many readers are open right now. |
| `Opens` | File opens of every kind — the total number of expensive (multi-second) events. |
| `Reopens` | A live reader thrown away and reopened because every reader stood ahead of a wanted row. Zero while concurrently-open passes do not exceed `MaxReaders`; above zero is the signal to raise it. |
| `SpareOpens` | A spare slot opened for the first time, on demand or by a warmer. |
| `WarmHits` | Spare opens a background warmer had already paid for by the time they were wanted. |
| `WarmWaitMilliseconds` | Time a reach spent blocked on a warmer that had started but not finished. |
| `CheapRewinds` | Backward reaches served by a reader parked behind the target — no open, no re-stream. Goes *up* when the pool is doing its job. |
| `RowsPerReader` | Rows each reader has moved over, skipped and read alike. |

## IO errors are faults

A disk failure — or a read against a view whose `Workbook` has been disposed — is
classified a **fault**, never a data disagreement. Faults propagate through every tolerance
boundary unchanged: `.Optional()`, `.Else(fallback)`, `.Else(value)`, and `Choice(...)` all
let a fault through rather than absorbing it. This applies at every point a strategy or a
projection reads a cell, not only inside a projection — an offset strategy scanning past
blank rows, an area strategy sizing a region, a repeat's separator, and a projection itself
all wrap a foreign exception through the same fault check.

Concretely, `IOException` (and its derivatives — `FileNotFoundException`,
`DirectoryNotFoundException`, and the reader's own IO failures), `ObjectDisposedException`,
and `OutOfMemoryException` are faults, alongside the pre-existing bug list
(`NullReferenceException`, `IndexOutOfRangeException`, `ArgumentOutOfRangeException`,
`ArgumentNullException`). A wrong-kind cell, an unparseable value, and a missing anchor
(`OutOfBoundsException`) remain ordinary, absorbable failures — the fault list is a
discrimination, not a blanket. Without this rule, a disk failure in the middle of
`SkipBlankRows()` inside `section.Optional()` would have been reported as *"section
absent"*, with a warning, and the parse would have continued and produced a wrong answer
quietly — the one failure mode this feature could not ship with.

## Honest limits

- **`.xls` is unverified.** `ExcelDataReader` nominally reads both formats, but every
  streaming test fixture, and the identity suite that proves a window reads the same cells
  as `SpreadsheetSpace.Create`, is `.xlsx`. Treat `.xls` through `Workbook` as unproven
  until it has its own fixture.
- **A sheet with no `dimension` element costs one pass to measure.** Some exported `.xlsx`
  files omit it, and the reader then cannot say how big the sheet is. `Sheet(name)` reads
  such a sheet once, counting rows and watching the width, and hands the real extent to the
  window — so a declaration sees exactly the space it would have seen from a file that
  described itself, and running off the end is the ordinary `OutOfBoundsException`. The pass
  costs time, not memory: no row is materialised by it. Where the cost shows is
  `Statistics(sheet)!.Value.RowsMeasured` — the rows the survey read, and `0` for every
  sheet that reported its own dimension — plus the reader's own travel in
  `ReaderStatistics`. It also runs holding the workbook's gate, so vending such a sheet
  blocks other `Sheet()` and `Statistics()` calls on that workbook until it finishes. Note
  that the eager path has no equivalent — `SpreadsheetSpace.Create` sizes its grid from the
  same absent dimension and yields an empty sheet — so this is the door to use for such a
  file, not the one to avoid.
- **Reads against one sheet serialise.** Different workbooks, and different sheets of one
  workbook, read in parallel; two threads mapping the *same* sheet at once are correct —
  no torn read, no chunk installed twice — but serialised on that sheet's store, one load
  at a time. A concurrent-map-over-one-sheet workload that actually wants parallel chunk
  loads would need per-chunk load coordination that does not exist yet.

Further-out deferrals (a public row-source seam, async APIs, a streaming result type, an
adaptive `MaxReaders`) are tracked in `docs/design/streaming-spec.md` §13–§14, not repeated
here.
