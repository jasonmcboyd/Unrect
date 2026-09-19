# Header bands — type sketch

A design record for a live arc (branch `feature/header-bands`). It is deleted when the arc lands
and CLAUDE.md carries what stands. Nothing here is built except where it says so.

## What is settled

- **The call site.** `Table(2, r => r["From", "Id"].Integer())`. `headerRows` states the header's
  depth; a path only addresses within it.
- **A path is a list of typed segments, never text.** A segment is a name `"Id"`, the nth of a
  name `("Id", 1)`, or a position `1`, at any level: `r["From", ("Id", 1)]`,
  `r[("Totals", 1), "Amount"]`, `r["From", 1]`. Nothing is ever joined into a string and read back:
  `"From 2"` could be a band and a position, or a caption that literally says so.
- **Everything is zero-based.**
- **Fail fast.** A bare name that matches several columns is an error naming the paths to choose
  from; `("Id", 0)` is how a declaration says it knows there are several.
- **The region rule.** A merged cell's text belongs to every cell it covers, and a column's path
  is the distinct REGIONS it passes through, top to bottom. "Date" merged down two rows is one
  region, so its column's path is `["Date"]`; "From" merged across two columns is one region at
  the top of two columns.
- **Fill-right is the convention, stated as one.** From the cells alone "From, blank" over "Id,
  Code" is the same whether From was merged over both or sits over Id only; no rule can deduce
  which. So, as every reader of multi-row headers does: a label claims the blank header cells
  beneath it; if it has none beneath it, it claims the blank cells to its right, up to the next
  label in its row, never past the end of the region above it, and **never over a column with
  nothing beneath it** (a spacer column ends a band). The caption row never claims sideways (a
  blank caption is a column with no label). A wrong guess never reads a different cell: the path
  `["From", "Code"]` names the same physical column either way, and a bare `"Code"` that is
  duplicated fails with the candidates listed.
- **Recorded merges override the convention** where a file records any in its header: then the
  merges are trusted outright and nothing is filled, so an unmerged "From" covers Id alone. No
  merges in the header at all means the file cannot say, and the convention applies. (Merges are a
  backend capability, later; `ISpace` stays flat — the owner, 2026-09-19: merged points as
  first-class citizens of the space "would be a nightmare with all of the different projections".)
- **A limit to document, not to guess at:** a spacer column blank all the way DOWN ends the table
  itself (its width rule is "columns while any value", which is what lets two tables sit side by
  side), so the bands to its right are outside the table. From the cells a spacer inside one table
  and a gap between two are the same thing; the declaration says which by declaring its width
  (`Sized(RowsWhileAnyValue()).Of(Table(2, …))`). A by-name miss should say so when there is more
  header to the right of where the table ended.
- **A label is whatever a non-blank header cell SAYS** — not only a text cell. A header cell is a
  label by position, not by kind: period columns (`2023 | 2024 | 2025`, period-end dates) are
  captions, and a band row of years is a band row. The `IsText` guard in today's header parse
  goes. (Whether `ISpace` keeps `IsText` at all is a separate open question, in CLAUDE.md.)
- **Resolution.** A one-segment path is an exact path first (a column under no band), then a
  caption that is unique anywhere. A longer path is exact. A position must fall inside what the
  path has reached.
- **One `params` indexer**, not a family of row types. A path deeper than the header fails at the
  header, every time, whatever the data; the binder refuses it where it is written; an analyzer
  (`UNR004`) can move the literal case into the editor once the API has settled.

## The data

`LabelMap` is flat today: one string per column and a name → columns lookup. It becomes a forest
of regions. A path of strings is not enough on its own — two separate "Totals" bands have the same
text and are different regions, which is what `("Totals", 1)` addresses — so the structure keeps
region identity:

```csharp
internal sealed class LabelRegion
{
  string Text;                          // what the header says
  int From, To;                         // the columns it covers, [From, To), in the map's frame
  IReadOnlyList<LabelRegion> Children;  // empty for a column's own caption
}
```

A leaf region is one labelled column. A column with no label at all is in no region and is
reachable by position only. Ordinals stay in the frame the header was read in, translated per row
by the captured origin exactly as today — a band changes nothing about that.

## The public surface

```csharp
public readonly struct LabelStep            // nobody names it: implicit from string, int, (string, int)

public sealed class LabelMap
{
  public int this[string caption] { get; }                  // today's, unchanged (the exact overload)
  public int this[params LabelStep[] path] { get; }         // NEW
  public bool Has(params LabelStep[] path);                 // today's Has(string), widened
  public LabelMap Under(params LabelStep[] band);           // NEW: the band as a map of its own children

  public IReadOnlyList<string> Labels { get; }              // today's: each column's own caption, "" for none
  public IReadOnlyList<IReadOnlyList<string>> Paths { get; }// NEW: each column's full path, empty for none
  public int Depth { get; }                                 // NEW: header rows read; 1 for a literal map
}

public sealed class TableRow<TSpace>
{
  public Point<TSpace> this[int column] { get; }            // today's
  public Point<TSpace> this[string columnName] { get; }     // today's
  public Point<TSpace> this[params LabelStep[] path] { get; } // NEW
}

// TableView: ColumnNames stays the captions; ColumnPaths is new.
// TableBinding: .Column(member, params LabelStep[] path), beside the caption, index and row forms.
```

`Under` is what makes nested types cheap. `Record<Party>(labels.Under("From"))` is the reflective
record binder that exists, handed a smaller map — so `Table<Transfer>` over bands is: for each
member whose type is itself a bindable record, take the sub-map of its band and bind the inner
type against it. No second binder.

Internally `ILabelSource.IndicesOf(string)` becomes one `Resolve(path, comparer)` that answers a
column, a band, "nothing by that name here (these are the names here)", or "several (these are
their paths)". The two comparers stay as they are: a row read matches trimmed, case-insensitive
text; the binder matches ignoring whitespace too.

## Where the regions come from

The fold — words in, forest out — is a pure function over an n-row block of header words. It is
generic: it asks only whether a cell is blank and what it says, which every `ISpace` answers.

**Proved by spike** (`spike/header-as-projection`): the header can be declared in today's
vocabulary with no engine change. The shortest form reads one column at a time, by position, and
folds:

```csharp
HorizontalRepeat(Column(2, c => (Top: word(c[0]), Caption: word(c[1]), At: c[0].Column)))
  .Select(columns => /* the region rule */)
```

It streams, needs no custom strategy, and gave correct paths over the owner's workbook, a tall
"Date" beside a bare "Rate", a bare caption before any band, and a gap column inside a header.
Header rows must be read by POSITION: a flow or a discovered strip treats a blank as a gap, and in
a header a blank is structure.

**Decision for the owner — how the standard header is built:**

- (a) *Inside the collectors that already read the header.* `ColumnLabels` and `TableView` both
  already cut an n-row header band and hand it to one parse. The fold replaces that parse. Smallest
  change; the header already folds out of failure paths; one home, as now.
- (b) *As a declaration in the public vocabulary,* the spike's form, wrapped as scaffolding.
  Purer, but the lambda table rungs read their region whole and would have to drive a header
  machine over their own header rows, and the header's internals must be folded out of failure
  paths by hand.

Recommended: (a) for the standard header, with the projection seam made PUBLIC for headers the
standard one cannot read — `Table(header: someProjectionYieldingALabelMap, eachRow: …)` plus a
public way to build a `LabelMap` from regions. That is where "reuse the machinery" pays: a units
row between bands and captions, a band row that restarts half-way, a header that is not at the
top. The seam already exists internally (`UnderColumnLabels(header, body)` takes any
`IProjectionDefinition<TSpace, LabelMap>`).

**Recorded merges** never reach `LabelMap`. They are a capability of the spreadsheet backend (as
formulas and formatting are), and the backend ships a header declaration that takes exact spans
from it and hands `LabelMap` the same regions. Core learns nothing about merging. The two can
disagree only where the reconstruction was a guess — a wide band whose first caption is blank —
and there the recorded merge is right.

## Replacing the mapping — three levels, smallest first

All three hand `LabelMap` the same data, so nothing downstream can tell which produced a map.

1. **Say what each header row is.** `Table(Header(Band, Caption), …)` is what `headerRows: 2`
   means; `Header(Band, Skip, Caption)` steps over a units row; `Header(Caption, Skip)` has the
   units under the captions; `MergedBand` trusts merges only. A closed set of roles, and nearly
   free: the fold already goes a row at a time.
2. **State the layout.** `Labels(("Date", 0), Band("From", ("Id", 1), ("Code", 2)))` — today's
   `LabelMap.Of`, with a way to write a band — for the fixed extract whose header is not parsed.
3. **Project it.** Any projection yielding each labelled column's PATH and the POINT its label
   sits on: `HorizontalRepeat(Column(3, c => Label(c[2], c[0].AsText(), c[2].AsText()))).ToLabelMap()`,
   then `Table(header, r => …)`. The point supplies the column; neighbouring columns sharing a
   path prefix are one region, and the same band text appearing again further right is a second
   one. The spike produced exactly this shape.

First version: the standard header with level 1's roles, and level 3's seam. Level 2 last.

## Open

1. ~~(a) or (b), above.~~ Working default (owner, 2026-09-19: "go with your table solutions"):
   (a), with the public seam.
2. `Table<T>`: flat members bind only to unique captions and never across a band; a band binds to
   a nested member; `.Column(member, "From", "Id")` is the flat escape hatch, checked against
   `headerRows` where it is written. (The owner's lean.) Should `headerRows` be INFERRED from the
   type's nesting for `Table<T>`, so there is no number to mismatch?
3. The exploratory `Table()` (a dictionary per row) over a banded header: refuse and point at
   `Table(n, r => …)`, or nest the dictionaries by band?
4. A literal map with paths: `LabelMap.Of(…)` needs a spelling for a band. Low priority.
5. Merge capability in the first version, or the reconstruction alone first?

## Order of work

1. `LabelRegion`, the fold, `Resolve`; rework the committed step (`8fa3927`, joined names) onto
   it. `r["From Id"]` stops resolving; a test pins that.
2. `LabelStep` and the `params` indexers on `LabelMap` and `TableRow`; `Has`, `Under`, `Paths`,
   `Depth`; the failures that name the alternatives.
3. The binder: `.Column(member, path)` with the depth check; `headerRows:` on `Table<T>` and
   `LooseTable<T>`.
4. Nested types through `Under`.
5. The public header seam; then the merge capability and the backend's header.
6. `UNR004`. Docs, a LINQPad example, the scaffolder.
