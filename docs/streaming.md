# Streaming: Reading a Workbook as a Forward Pass

`SpreadsheetSpace.Create` reads a sheet whole: one file open, one pass, the whole grid
resident before any projection sees it. `Workbook`, in the same `Unrect.Spreadsheets`
package, reads the same files as a *forward pass* — one cursor walked from the top, each row
offered to the declaration as it arrives and released as soon as nothing still open may read
it. Same projections, same results; the two doors differ only in the shape of their cost.
This is the user-facing guide to that door: when to reach for it, the lifecycle rules, what a
declaration holds and the cap on it, the statistics, and the limits that are honestly still
limits.

`book.Sheet(name)` hands back an `ISheetCells` — the kinded, no-formulas face a streamed sheet
answers — so a declaration file over it imports `SheetProjectionBuilders<ISheetCells>` beside
`ProjectionBuilders<ISheetCells>`, never `SpreadsheetProjectionBuilders<TSpace>`: a streamed
sheet carries no formulas, and a declaration that calls `Formula()` will not compile against
one. Read formulas through the eager door instead
(`SpreadsheetSpace.CreateWithFormulas(path, sheet)`).

The interpreter underneath is the push engine: a
declaration builds a tree of machines, the engine feeds them one row span at a time, and
what a machine may still read back is announced by its node, not guessed by a cache.

## When to reach for it

**Eager stays the default.** `SpreadsheetSpace.Create` is simpler, and for anything that
fits comfortably in memory it is also the one with nothing to explain. Reach for `Workbook`
when a file is too large to hold whole, or when one declaration runs over many files in
sequence and the peak per iteration matters more than the total across the run.

The cost model is **declaration-shaped**, not a flat tax — what a declaration does with the
sheet determines what a pass has to hold:

- **A monotone walk holds almost nothing.** A table read one band per row (`Table<T>()`,
  `Table(headerRows, eachRow: …)`, a `VerticalRepeat` of a streaming item) is offered each row
  and done with it: the pass holds the row in hand, the band being placed, and the attempt a
  repeat has in progress. The tall-ledger walk in `WorkbookTests` reads 1,201 rows with a
  peak well under sixteen.
- **A shape that reads its extent whole holds its extent.** A `Range(…)` block, a `Column(…)`
  strip, a `Table(view => …)` or `Table(row => …)` lambda rung reads the region it collected
  at close, so every row of that region is held until then. That is the right cost for a
  block a reader genuinely wants whole; it is the wrong shape for a sheet-sized region.
- **A tolerance boundary holds until it settles.** `.Optional()`, `.Else(…)` and `Choice(…)`
  may have to replay what their inner took to the successor, so they hold from where they
  started. Put the boundary around the part that may be absent, not around the sheet.
- **A repeat holds one attempt.** A committed occurrence is final, so a walk over a
  thousand blocks retains one block, its separator and the row in hand.
- **A shape driven across its axis is held and driven again.** A `HorizontalFlow` under a
  row-major source holds its band until its extent is known, then runs along it. So does a
  column landmark (`RightOf(ColumnContaining(…))`, `UntilColumn`), which can only be found
  over the whole extent.

**Ask before reading.** `CostReport.Of(definition)` is the dry run: one line per node saying
whether the engine drives it row by row or holds it whole and why, how far back its own machine
may still read once placed, and the axis it announces. It is a pure function of the declaration, so it
can be printed from a test or a script with no file in hand:

```csharp
Console.WriteLine(CostReport.Of(report));
// driver: rows
// VerticalFlow          streams  retains none    axis vertical
//   'title'             streams  retains extent  axis either
//   Table<Transaction>  streams  retains none    axis vertical
```

## The `Workbook` / `Sheet` lifecycle

```csharp
using var book = Workbook.Open(path);              // owns every cursor it opens, and one string table
var result = projection.Map(book.Sheet("Data"));    // Sheet(name) is one forward pass over its own cursor
```

- **Each `Sheet(name)` is a fresh pass.** It opens a cursor of its own, walked to that sheet,
  and hands back a space the engine drives from the top. Ask again to read the sheet again;
  a second declaration over an already-open book is a second pass, with the string table
  already warm. Resolution respects `WorkbookOptions.CaseSensitiveSheetNames` (default off,
  as Excel itself); an unknown name throws `ArgumentException` naming the sheets seen so far.
- **A pass is read once, forward.** As the declaration moves down the sheet, rows no open
  machine may still read are released. A cell of a released row is a located read failure —
  `row 7 of 'Data' has left the buffer: a streamed sheet is read once, forward, so read B7
  inside the projection rather than after it` — which is what a `Point()` escaping its leaf
  and read in a combiner, or the same sheet value mapped twice, will see.
- **A cell read directly is loaded on the way to it.** Outside the engine nothing is
  released, so a sheet can be walked forward by hand — `sheet.AsText(0, 0)`, then row 1, then
  row 40 — and every row up to the one asked for is loaded and held. Walking backwards over
  rows a *map* has released is the failure above.
- **One consumer per pass.** A pass is not shared between threads. Many threads over one
  workbook each ask for their own `Sheet(name)`; they share only the string table, which is
  safe to share.
- **Points have a lifetime, minting does not.** A `Point<ISheetCells>` is a space, a column
  and a row — minting one touches nothing. *Reading* through one is what reaches the pass.
- **A read after `Dispose` throws `ObjectDisposedException`, deterministically**, whether
  or not the row it wants happens still to be held. This exception is a **fault** (see
  [IO errors are faults](#io-errors-are-faults)): no tolerance boundary may absorb it.
- **`Dispose` closes every cursor the workbook opened** and lets go of the string table; the
  interning counters survive it. It is idempotent.

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

Projections are immutable and thread-safe, and workbooks are independent of each other, so this
loop parallelises with nothing added:

```csharp
Parallel.ForEach(monthlyCloseOfFunds, path =>
{
  using var book = Workbook.Open(path);
  Publish(report.Map(book.Sheet("Detail")));
});
```

`MapWorkbook(path, sheet)` is the one-line form of the same thing.

## The cap

**`WorkbookOptions.BufferRows` (null by default: no cap) is the most rows a pass may hold at
once.** It is not a window to size to the data; it is a promise about the declaration. A pass
that would hold more is a **fault** naming the shape that holds them:

```
Column: Column is holding 101 rows, from row 1 through row 101, more than the 100 the source
allows: the declaration asks for more than a forward pass can keep. Raise the source's buffer
cap, or bound the shape that holds
```

The holder named is the innermost machine still holding the oldest row — the leaf or boundary
whose reach is the cost — never the boundary wrapping it. A cap is never a degraded read: with
none set, a declaration that holds the sheet simply holds the sheet, which is what the peak in
`Statistics` will say.

## The statistics vocabulary

`Workbook.Statistics(sheetName)` returns `StreamingStatistics?` — null until that sheet has
been asked for, then the figures of the most recent pass over it. `Workbook.InterningStatistics`
belongs to the *book*, shared across every sheet of it. Both render one line via `ToString()`:

```
'Ledger': 1201 rows read, peak 12 retained
'Undeclared': 4 rows read, peak 4 retained, 4 measured

shared 1,014,999 | distinct 5,009/65,536 | saved ~32,319,976B (estimated)
```

### `StreamingStatistics` — what one pass has cost

| Member | Meaning |
|---|---|
| `SheetName` | The sheet these numbers describe. |
| `RowsRead` | Rows loaded by the pass — every row the declaration was offered, or a direct read asked for. A rule that stops sees the row it stops at, so this is one more than a bounded shape consumed. |
| `PeakRetained` | The most rows held at once — what the declaration's holds cost. A peak near the sheet's height says something holds its extent; `CostReport` says which. |
| `Cap` | The cap the pass ran under (`WorkbookOptions.BufferRows`), or null. |
| `RowsMeasured` | Rows read to measure a sheet whose reader reported no extent (a sheet with no valued cell); `0` for a sheet that reported its own. Above zero means a whole extra forward pass over the file was paid for before the declaration saw anything. |

### `InterningStatistics` — what sharing repeated text has earned one workbook

Equal `Text` cells are handed **one instance** of their characters rather than one each. The
duplicate is built by the reader before the adapter sees it, so this saves *retention*, not
*allocation*: the twin dies in gen0 and what survives is one string per distinct value. On a
text-heavy sheet it is the largest single reduction available to either door — measured on a
250,000-row, five-text-column ledger, a held grid fell from 112.0 MB to 58.2 MB (exactly what
the same file costs when the *reader* has already deduped it, via shared strings) and a held
projection from 86.1 MB to 32.3 MB. Both doors do it, at their own adapter seam, under the
same length guard, so for any file whose distinct text fits the cap the two produce
byte-identical live sets.

| Member | Meaning |
|---|---|
| `DistinctValues` | Distinct values the table took in. Does not fall when `Dispose` drops the entries — it is the count reached, not the entries alive at the moment of asking. |
| `Capacity` | The ceiling on it (`WorkbookOptions.MaxInternedStrings`, 65,536 by default; `0` turns sharing off). |
| `AtCapacity` | Whether the table stopped growing. Past the cap, values already in it go on being shared and one newly met does not — degradation, never failure. **It does not say on its own which way to move the cap** — see [what a full table costs](#what-a-full-table-costs) below. |
| `Hits` | Cells handed an instance the table already held. Each is a duplicate string that did not have to be retained. |
| `EstimatedBytesSaved` | What those hits are worth, **estimated** and named so: the sum over every hit of what a string of that length occupies on a 64-bit runtime. It models the layout, not the heap. To know a live set, measure a live set. |

Two guards bound what the table itself can pin, because it lives as long as the workbook.
`Capacity` bounds the entries; a **256-character limit** bounds each one — long spreadsheet
text is a memo or a free-text note, nearly always unique, so it would take an entry that never
scores a hit while pinning the most bytes of anything in the table. What actually repeats —
captions, currency and account codes, categories, party names — is short.

#### What a full table costs

Reaching the cap costs nothing *in sharing*. A column that never repeats — a transaction
reference, an invoice number — fills any cap without displacing anything, because the values
that *do* repeat are met in the sheet's first rows and are in the table long before it fills.
So `AtCapacity` alone is not a reason to raise `MaxInternedStrings`.

What it costs is the entries. Every one is held for the life of the workbook whether it ever
scores a hit or not, at roughly `Capacity × 530` bytes of characters plus the table's own
~56 bytes an entry. **So the knob turns both ways:** raise it when a sheet's genuinely
repeating vocabulary is larger than the cap, and lower it — or pass `0` — when the text does
not repeat and the memory floor is the point. Where an `.xlsx` spells its text through the
workbook's shared-string table, the reader pins those strings for its own lifetime anyway and
an entry here adds its dictionary node rather than its characters; the cost lands on files
whose text is inline, and on `.xls`.

The eager door does the same thing without the counters and without a cap:
`SpreadsheetSpace.Create` shares text across every sheet of one call.

## IO errors are faults

A disk failure — or a read against a sheet whose `Workbook` has been disposed — is
classified a **fault**, never a data disagreement. Faults propagate through every tolerance
boundary unchanged: `.Optional()`, `.Else(fallback)`, `.Else(value)`, and `Choice(...)` all
let a fault through rather than absorbing it. This applies at every point a strategy or a
projection reads a cell, and to the pass itself: a cursor that throws while the engine is
loading the next row is a fault naming the declaration and the row.

Concretely, `IOException` (and its derivatives), `ObjectDisposedException` and
`OutOfMemoryException` are faults, alongside the bug list (`NullReferenceException`,
`IndexOutOfRangeException`, `ArgumentOutOfRangeException`, `ArgumentNullException`,
`InvalidCastException`). A wrong-kind cell, an unparseable value, and a missing anchor
(`OutOfBoundsException`) remain ordinary, absorbable failures — the fault list is a
discrimination, not a blanket. Without this rule, a disk failure in the middle of
`SkipBlankRows()` inside `section.Optional()` would have been reported as *"section
absent"*, with a warning, and the parse would have continued and produced a wrong answer
quietly.

## Honest limits

- **`.xls` is unverified.** `ExcelDataReader` nominally reads both formats, but every
  streaming test fixture, and the identity suite that proves a pass reads the same cells as
  `SpreadsheetSpace.Create`, is `.xlsx`. Treat `.xls` through `Workbook` as unproven until
  it has its own fixture.
- **A sheet whose reader reports no extent costs one pass to measure.** For the formats
  ExcelDataReader handles that means a sheet with no valued cell — rows of
  formatted-but-valueless cells, a pre-formatted export region — since the reader derives
  its counts from a pre-scan of the cells, not from the `dimension` element. `Sheet(name)`
  reads such a sheet once on a cursor of its own, counting rows and watching the width, and
  the pass then sees exactly the space a self-describing file would have given it. The cost
  is time, not memory, and `Statistics(sheet)!.Value.RowsMeasured` says what it was. The
  eager door measures such a sheet too, so the two doors report the same extent.
- **A pass cannot go back.** Reading a cell of a row the engine has released fails, by
  design; a shape that needs the whole region is the shape to declare, and the cost report
  will say it holds.
- **A rule that stops sees one row past its stop.** A bounded shape consumes its rows and the
  engine loads the next to offer it; `RowsRead` counts that row.

Further-out deferrals — a public row-source seam, async APIs, a streaming result type — are
recorded in CLAUDE.md's "Where Work Left Off", not repeated here.
