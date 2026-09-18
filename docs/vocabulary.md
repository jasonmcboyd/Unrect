# The Unrect Vocabulary

A survey of every operator in the projection layer, grouped by role in the algebra. There is one
vocabulary — `ProjectionBuilders<TSpace>` — and one mapping interface — `IProjectionDefinition<TSpace,
TResult>`. A declaration file imports the vocabulary once, closed over the space every
declaration in the file is written against:

```csharp
using Unrect.Projections;                                                      // the postfix half
using Unrect.Spreadsheets;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

var report = VerticalFlow(v =>
{
    var title = v.Next(Text());
    var rows  = v.Next(Table(headerRows: 1, eachRow: row));

    return v.Build(read => new Report(Title: read.Of(title), Rows: read.Of(rows)));
});
```

Everything below that pair of `using static` lines is written with **zero prefix**: no
`Projection.`, no scope local, no type argument naming the space anywhere in the file's body. See
"Entry — the one door in," below, for what that buys and the two laws it rests on.

When this file and a design draft in `docs/design/` disagree, this file describes what the tree
does today.

## Leaves — where cells become values

Two families: the **canonical** pair, closed over the four questions every `ISpace` answers and
living in `Unrect` itself; and the **kinded** six, closed over a backend's own capability and
living beside that backend.

| Operator | Yields | Notes |
|---|---|---|
| `Point()` | `Point<TSpace>` | One cell, as the address of itself. The leaf for a reading this vocabulary does not name: a backend's own point-extension answers the rest (`Record(r => r["Amount"].Decimal())`, `Range(b => b[0, 0].Value())`), and it is also what a reader hands to its own complaint about a cell |
| `AsText()` | `string` | One cell, read as what it says: a text cell's own value, or the backend's rendering of anything else. **Total** — every space renders every cell, so the only failure is a blank one, and `OrBlank()` turns that into `null`. `Choice(AsText(), …)` is therefore degenerate: `AsText` cannot fail, so nothing after it in the choice is reachable — put the narrower leaf first |
| `Text()` `Decimal()` `Integer()` `Double()` `Date()` `Boolean()` | typed value | One cell; asserts its kind, applies the kinded accessor. Live in `Unrect.Spreadsheets`, imported via `SheetProjectionBuilders<TSpace>` (over `ISheetCells`) or `SpreadsheetProjectionBuilders<TSpace>` (over `ISpreadsheetSpace`, which also carries `Formula()`). The family is CLOSED over `ISheetCells`'s kinded reads and never leads it — no `Long()`, ever; a conversion beyond the set is `Select` territory |
| `AsText().OrBlank()` / `Text().OrBlank()` / `Decimal().OrBlank()` … | `T?` | The same reading, tolerating a BLANK cell: null, quietly, with no diagnostic — where `.Optional()` absorbs a *failure* and records a Warning. A wrong kind still fails loudly. The standalone spelling of a nullable table member's tolerance |
| `Cell(v => ...)` | `T` | Removed name; the escape hatch for one cell is `Point(...)` composed with a caller's own read, or a bespoke leaf over `DefinitionNode<TSpace, T>` |
| `Row(r => ...)` / `Row(width, r => ...)` / `Row(IColumnStrategy, r => ...)` | from `CellStrip<TSpace>` | One row; width discovered (`while any value`), explicit count, or by column strategy |
| `Column(c => ...)` / `Column(height, c => ...)` / `Column(IRowStrategy, c => ...)` | from `CellStrip<TSpace>` | One column; height discovered, explicit count, or by row strategy |
| `Range(b => ...)` / `Range(w, h, ...)` / `Range(area, ...)` | from `CellBlock<TSpace>` | Rectangular block |
| `Caption(text)` | matched text (verbatim) | A declared anchor: seeks its row by the content rule, consumes exactly that row, asserts the text |
| `Fields(Field(a), Field(b), ...)` | `IReadOnlyDictionary<string, Point<TSpace>>` | Labelled-pair block; self-anchors on its first label; labels matched colon-tolerantly (`LabelEquals`) |
| `Heading(text)` | — (a pipeline stage, not a value-yielding leaf) | Locates its row by content, asserts the text, consumes it at full width, places the section immediately below. Mints the same `Caption` leaf internally |

Every leaf, matcher and view member hands back a `Point<TSpace>` (or a collection of them) rather
than a value: reading one is `point.AsText()`/`point.Decimal()`/`point.Value()` — whatever the
point's space can answer — not a property the framework already decided to expose.

**The locked taxonomy — one sentence, four words, no overlap: `Heading` asserts · `Caption`
captures · geometry skips · matchers locate.** Unchanged from before the point-substrate arc.

## Tables — one family, split across two packages

The core five rungs live in `Unrect`, over any `ISpace`; the two reflective rungs live in
`Unrect.Spreadsheets`, over `ISheetCells`, because binding a member asserts a *kind* and only a
kinded space can answer that.

| Rung | Operator | Package | Yields | Notes |
|---|---|---|---|---|
| bind | `Table(headerRows: 1, eachRow: captions => ...)` | `Unrect` | `IReadOnlyList<T>` | The header is read, and the captions it carries (a `LabelMap`) are handed to a lambda that returns the projection for one record — `Overlay(o => new Row(o.Next(Decimal().Right(captions["Amount"]))))`. The bind runs ONCE PER application of the table (after the header, before any row) and builds a description; the description is applied per row by the engine. A missing or duplicated caption is a loud failure naming the header cells |
| row-slot | `Table(headerRows:, eachRow: someProjection)` | `Unrect` | `IReadOnlyList<T>` | Every body row is handed to a PROJECTION as its own one-row extent. Composed from `VerticalBands` under a discovered header (`ColumnLabels`/`WithColumnLabels`), wrapped as one `UnitDefinition` so a failure inside a record reads `Table[3] -> 'eachRow' -> …`, byte-identical to a hand-written leaf's path |
| dictionary | `Table()` | `Unrect` | rows of `IReadOnlyDictionary<string, Point<TSpace>>` | Exploratory: keys discovered from the file's header, looked up under the binding comparer; a column with no caption and two captions that collide are both loud failures |
| lambda / row | `Table(r => ...)` / `Table(headerRows, r => ...)` | `Unrect` | `IReadOnlyList<T>` (`T` per row) | Full control: hand-written per-row reading over `TableRow<TSpace>` — `r["Caption"]` / `r[i]`, both yielding a `Point<TSpace>` |
| lambda / view | `Table(t => ...)` / `Table(headerRows, t => ...)` | `Unrect` | `T` for the whole table | Full control over `TableView<TSpace>`, for a table that does not decompose row-by-row |
| reflective | `Table<T>()` | `Unrect.Spreadsheets` | `IReadOnlyList<T>` | Captions bound to properties by `CaptionComparer` (case- and whitespace-insensitive), kinds inferred from property types (the closed set the kinded leaves cover, plus `Point<TSpace>` for an unassertive escape hatch), `Nullable<>`/`string?` meaning per-column blank tolerance. **Composed**, not implemented: reflection (`RowBinding`/`MemberPlan`) writes the record projection the *bind* rung above already knows how to apply, wrapped in `.AsUnit("Table<T>")` so its failure path is a hand-written bind's path with the member's own column named — `Table<Money>[0] -> column 'Amount'` |
| reflective, adjusted | `Table<T>(bind => ...)` | `Unrect.Spreadsheets` | `IReadOnlyList<T>` | The same, adjusted: `bind.Column(t => t.X, "caption")` for a caption the comparer would not find, `bind.Ignore(t => t.X)` for a member this table does not carry |

Every rung takes an optional `BlankRowStrategy onBlank` (`Stop`/`Skip`/`Fault`/`Tolerate`) for a
fully-blank body row; `Stop` is the default and is self-bounding, the other three run the table to
the enclosing edge (bound it with `.Until` or a count).

`Record<T>` names two different single-row rungs, one per package, distinguished by signature —
the record family's own `Table<T>()`/`Table<T>(bind)` split. `Record<T>(Func<TableRow<TSpace>, T>
record)` (`Unrect`) is the decoupled half of the bind rung: one body row read by a hand-written
projection, with columns resolved by name through whatever `WithColumnLabels` pushed — the
primitive `Table(headerRows, eachRow)` composes from underneath it. `Record<T>(LabelMap labels)`
(`Unrect.Spreadsheets`) is the single-row analog of the reflective `Table<T>()`: one row filled
from `T`'s members by caption match against `labels`, through the same `RowBinding`/`MemberPlan`
machinery — for a record read once rather than as a table's body, typically under a
`WithColumnLabels` scope of its own.

Two notes the ladder earns:

- **A record projection that discovers its own extent measures itself, not the band.**
  `Row(cells => ...)` as an `eachRow` is "as wide as the leading columns that carry values", which
  over a sparse export is width 0. `Range(WholeExtent(), ...)`, or an `Overlay` whose children
  place themselves, reads the band as it was handed over.
- **A lambda that touches nothing distinctive is ambiguous between the two lambda rungs**
  (`Table(x => 0)` — `TableRow<TSpace>` and `TableView<TSpace>` both have a `Location`). Type the
  parameter: `Table((TableRow<TSpace> r) => 0)`.

## Layout composites — the geometry claims

| Operator | Claim |
|---|---|
| `VerticalFlow(v => ...)` / `HorizontalFlow(v => ...)` | Stacked bands, one per child: each child's band spans the flow's full width, so no sibling ever shares it. The lambda runs once, at declaration: `v.Next(projection)` declares the next child and hands back a `Slot<T>`, and `v.Build(read => ...)` closes the layout with the combiner that reads the slots (`read.Of(slot)`); any arity, and nothing in the lambda has a value to branch on |
| `Overlay(o => ...)` | One shared band; each child finds its own place by its own placement; no advance between children; consumed = bounding box |
| `VerticalRepeat(item, separatedBy:, atLeast:)` / `HorizontalRepeat(...)` | N items with separators. A blank band is a separator, never a terminator — bound the repeat with `.Until` to end it at content |
| `VerticalBands(rows, each, onBlank:)` / `HorizontalBands(columns, ...)` | The extent cut into bands of a fixed stride, each projected by `each`. Nothing is searched for; the tiling ends when a whole band is no longer left. The contrast with a repeat: a repeat repeats a *pattern*, a tiler repeats a *fixed-dimension space* |
| `Choice(a, b, ...)` | The first alternative that fits; an Info per near-miss; a losing branch's diagnostics roll back |

The composite you pick is the geometric claim you make. Flows never negotiate — a child that does
not fit throws, with a path and a cell.

## Placement — where things start

**Silence is adjacency.** A projection with no placement modifier starts exactly where the one
before it left off. Every operator below is therefore a *declared exception*, and each word names
the kind of reason it is an exception for.

Every word below has two spellings: **postfix**, `section.Below(mark)`, an extension on an
already-built projection (`ProjectionExtensions`); and **prefix**, `Below(mark).Of(section)`, a
factory on `ProjectionBuilders<TSpace>` that opens the compile-time-checked **placement pipeline**.
Same words, same semantics, same silence-is-adjacency law — the pipeline is a spelling, not a
second calculus; `Below(mark).Of(x)` and `x.Below(mark)` replay the identical `Steps` onto the same
terminal.

| Operator | Kind of reason | Meaning |
|---|---|---|
| `.On(rowLandmark)` / `.On(columnLandmark)` | a relation | The projection starts AT the match and OWNS that row/column. One word for both axes |
| `.Below(rowLandmark)` | a relation | Starts on the row directly below the match |
| `.RightOf(columnLandmark)` | a relation | The column twin of `.Below` |
| `.AfterBlankRows()` / `.AfterBlankColumns()` | filler | Step over the blank band in front. Tolerant by nature |
| `.SkipToFirstNonBlankCell()` | filler | Down to the first content row, then across it to its first non-blank cell — the lazy top-left corner heuristic |
| `.Down(n)` / `.Right(n)` | a distance | Fixed movement, honestly named as one |
| `.OffsetBy(offsetStrategy)` | delegated | *My start is where that resolves to* — the one marked crossing from the cell-model surface into the interval-model strategy calculus |
| `.Sized(area)` | — | Not placement; the extent's own replace |

The anchors and `.OffsetBy` **REPLACE a default** offset and **REFUSE a declared one**
(`ArgumentException` at construction) — a contradiction has no denotation. The movements
**COMPOSE** onto whatever the projection already had. A declared area survives all of them.
Inside the placement pipeline the identical contradiction is **unspellable at compile time**
rather than refused at construction: a stage type simply does not offer the member a second
declaration would need (`On(a).On(b)` does not compile; the stub it would resolve to is
`[Obsolete(error: true)]`, throwing a sentence in the library's own words rather than the
compiler's).

Absence semantics live in the word, not in a flag: a landmark that matches nothing is **loud**
(`Optional`/`Else` absorb it; a repeat reads it as having run out of sections), while a
filler-skip that finds no filler is **tolerant** and simply does not move.

The strategy vocabulary `.OffsetBy` takes:

| Operator | Meaning |
|---|---|
| `SkipRows(n)` `SkipColumns(n)` `BlankRows()` `BlankColumns()` | Fixed and blank-skipping offsets |
| `Then(a, b, ...)` | Sequence offsets; each searches only the space the previous shift left |
| `FromRight(w)` / `FromBottom(h)` | From-end anchoring |
| `OffsetStrategies.To(m)` / `Past(m)` | The lifts `.On` / `.Below` / `.RightOf` are built on; public in `Unrect.Strategies`, deliberately NOT re-exported on `ProjectionBuilders<TSpace>` |

### The pipeline's stages and terminals

`Below(mark)` opens an `OffsetStage<TSpace>`, which offers movements, an optional `.Sized`,
`.Until`/`.UntilColumn`, `.Heading`, and every terminal; `.Sized(...)` narrows to an
`OffsetAndSizeStage<TSpace>` (drops the movements and a second `.Sized`, keeps `.Until`/
`.Heading`); `.Until(...)` narrows to a `BoundStage<TSpace>` (drops everything geometric); a
`.Heading(...)` opens or chains a `HeadingStage<TSpace>` (drops everything except more headings
and the terminals). Every stage carries the full terminal spread — the three layouts, both
composing `Table` rungs, every leaf, `Point`/`Row`/`Column`/`Range`, `Fields`, `Choice` and both
repeats — plus `.Of(projection)` for anything already declared elsewhere (a hoisted local, a
backend's own leaf: `Below(mark).Of(Formula())`).

**A demanding matcher opens a demanding pipeline with nothing annotated.** `On(rowMatcher)` and
`Below(rowMatcher)` are overloaded on `IRowLandmark<TSpace>` as well as the plain `IRowLandmark`
(and the column twins), so `On(RowWithFormula())` infers `TSpace : IFormulaSpace` from the
matcher's own type and hands back a demanding `OffsetStage<TSpace>` — see "The typed phantoms"
under Matchers, below.

## Extent — where things end

| Operator | Meaning |
|---|---|
| `.Sized(area)` | Declared extent, consumed in full; replaces a shape's own default extent, refuses a second `.Sized` |
| `.Until(matcher)` / `.Until(matcher, orEnd: true)` / `.UntilColumn(...)` | Extent ends just BEFORE a forward landmark; the bound is consumed in full so the next sibling starts AT the landmark |
| `Extent(w, h)` `WholeExtent()` `NoExtent()` `RowsWhileAnyValue()` `RowsWhileAny(p)` `ColumnsWhileAnyValue()` `ColumnsWhileAny(p)` | The area vocabulary, mirrored on both axes; `p` is `Func<Point<TSpace>, bool>` over the file's own space, so it may ask a cell's kind or its value |
| `TakeRows(n)` `TakeColumns(n)` `AllRows()` `AllColumns()` | Axis selectors, not area strategies — for `Row(AllColumns(), ...)` and for composing an extent from its two axes |
| `TakeRowsWhile(p)` `TakeRowsTo(p)` `TakeRowsWhileAll(p)` `TakeRowsWhileAny(p)` and the four `TakeColumns…` twins | Predicate-driven axis selectors, over the file's space |
| `RowsThenColumns(rows, columns)` / `ColumnsThenRows(columns, rows)` | The two axes as one extent; each axis is taken as it comes, demanding or not |
| `SelectSize(f)` `SelectArea(f)` `SelectOffset(f)` | Measured by hand, `f` being `Func<Plane<TSpace>, Size>` |
| `SkipRowsWhileAll(p)` `SkipRowsWhileAny(p)` and the column twins | Offsets past a leading band, over the file's space |

## Matchers — one family, four rules, four modifiers

`RowContaining(text)` · `RowWhere(spacePredicate)` · `RowWithCell(cellPredicate)` · `RowSaying(text)`
— and the three column twins. Predicates read the file's own space:
`Func<Plane<TSpace>, int, bool>` for `Where`, `Func<Point<TSpace>, bool>` for `WithCell`, so a
matcher can ask what a cell *is* as well as what it says, and what it hands back carries that
demand (see "The typed phantoms", below). The erased spellings live on in `Unrect.Strategies`
(`RowLandmarks`/`ColumnLandmarks`), which is the calculus a helper writes against. Naming
law: bare `Where`/`While` = a space predicate; a cell predicate is always marked (`WithCell`,
`WhileAll`, `WhileAny`); `Containing` = whole-cell text, trimmed, case-insensitive; `Saying` is the
one rule that looks past a cell's kind — the same whole-cell comparison against what a cell
*renders*, so a numeric 42, a date, a boolean and an error are all reachable through it and none
of them through `Containing`.

A matcher only *locates* and reports absence; what absence means belongs to the modifier that
takes it — `.On` (own the match), `.Below` / `.RightOf` (one beyond), `.Until` (bound by it).
Because a section can start at `.On(RowContaining("A"))` and end at `.Until(RowContaining("B"))`
through the same matcher, the start and the end cannot disagree about what a caption is.

Three matching rules exist in the library and deliberately never unify: the **content rule**
above (matchers, `Caption`, and `TableView`/`TableRow`'s by-caption row access), **`LabelEquals`**
(`Field` only — content rule plus a trailing colon-run ignored), and **`CaptionComparer`** (the
reflective `Table<T>` binding and the `Table()` dictionary's keys — case- and
whitespace-insensitive, bridging caption ↔ identifier). A declaration must never start in one and
end in another.

### The typed phantoms — a demand that crosses the erased seam

**A canonical predicate asks the four questions; anything about kind or value is a typed predicate
and names its space.** The strategy and landmark interfaces in `Unrect.Core` speak
`Plane<ISpace>`/`Point<ISpace>`, which answers `IsBlank`/`HasValue`/`IsText`/`AsText` and nothing
else — so a rule that asks "is this a number" has to carry the space it needs. Seven interfaces
do that, one per thing the calculus takes:

```csharp
public interface IRowLandmark<in TSpace>    where TSpace : class, ISpace { IRowLandmark    Landmark { get; } }
public interface IColumnLandmark<in TSpace> where TSpace : class, ISpace { IColumnLandmark Landmark { get; } }

public interface ISizeStrategy<in TSpace>   where TSpace : class, ISpace { ISizeStrategy   Strategy { get; } }
public interface IOffsetStrategy<in TSpace> where TSpace : class, ISpace { IOffsetStrategy Strategy { get; } }
public interface IAreaStrategy<in TSpace>   where TSpace : class, ISpace { IAreaStrategy   Strategy { get; } }
public interface IRowStrategy<in TSpace>    where TSpace : class, ISpace { IRowStrategy    Strategy { get; } }
public interface IColumnStrategy<in TSpace> where TSpace : class, ISpace { IColumnStrategy Strategy { get; } }
```

None of them derives from the plain form. That is the whole mechanism: a member that takes a
phantom (`Sized`, `OffsetBy`, `Row`, `Column`, `Range`, the repeats' `separatedBy:`, `On`,
`Below`, `RightOf`, `Until`) is overloaded on both, so the demanding argument is the one the
compiler picks and the demand is inferred with nothing annotated. `Strategy` (or `Landmark` for a
matcher) unwraps back to what the calculus takes, and unwrapping is what the lift does, once, at
construction — the object the engine receives is the calculus's own, so the scan it builds is the
one the rule would build unwrapped.

The vocabulary's own factories build them: `RowsWhileAny(p => p.Kind() == CellKind.Number)` over
`ProjectionBuilders<ISheetCells>` lowers the predicate and hands back an `IAreaStrategy<ISheetCells>`.
`in TSpace` is what makes a shared helper work — a rule built at `ProjectionBuilders<ISpace>` flows
into every file:

```csharp
static IAreaStrategy<ISpace> Populated() => ProjectionBuilders<ISpace>.RowsWhileAny(p => !p.IsBlank);

var header = Sized(Populated()).Row(r => r[0].Text());   // in an ISheetCells file, nothing annotated
```

A capability-demanding *matcher* is the same trick from the other end:
`Unrect.Spreadsheets.SpreadsheetProjections.RowWithFormula()` implements the plain `IRowLandmark`
the calculus takes *and* `IRowLandmark<IFormulaSpace>`, so `On(RowWithFormula())` hands back a
pipeline demanding `IFormulaSpace`. It is ordinary contravariant inference throughout, never a
runtime capability walk.

The predicate is lowered, so the cast to `TSpace` happens once per cell rather than once per
measurement. A rule that reaches a space it was not written for — which takes unwrapping it and
passing the bare strategy through the canonical door — faults with `InvalidCastException`, and a
fault is never absorbed by `.Optional()` or `.Else()`.

## Wrappers and boundaries

| Operator | Meaning |
|---|---|
| `.Under(params captions)` | Captions stacked above the projection, in reading order — sugar desugaring to a plain flow. `Heading(text)` is the pipeline's canonical prefix spelling of the assert-and-consume case; `.Under` is more general (any string-valued projection) |
| `.Padded(all)` / `(h, v)` / `(l, t, r, b)` | Shrink the inside; consumed includes the border. The one geometry modifier that stays postfix — it nests rather than erases, so it needs no pipeline stage |
| `.Optional()` | Tolerance boundary: absorbs a failure, yields `default`, records a Warning |
| `.Else(fallbackProjection)` / `.Else(value)` | Fallback boundary; Warning carries the primary's failure |
| `.Select(f)` | Transform the value (single-value only) |
| `.Named(name)` | Explicit name — an override on the naming ladder, below |
| `.AsUnit(name)` | Labels the node AND makes it opaque: a composition presented as one node in a failure path, its label unquoted (`Table:fruit` when also `.Named`). What a composed rung (a reflective `Table<T>`, the row-slot `Table`) is wrapped in so it reads as one thing |
| `.AsScaffolding()` | Marks a projection a *factory's own* internal plumbing: it contributes no segment of its own to a rendered path, carrying only its occurrence index up onto the nearest kept segment, while the uncollapsed `FullPath` keeps it for drill-through. Never applied to something a declaration wrote itself |

## Application

| Operator | Returns |
|---|---|
| `projection.Map(space)` | `T` (absorbed-tolerance diagnostics discarded) |
| `projection.MapWithDiagnostics(space)` | `MapResult<T>`: value + `ProjectionDiagnostic` list |
| `projection.Apply(space)` | value + offset + consumed |

All three are usable as method groups — `spaces.Select(report.Map)`.

**Where the `space` comes from.** `SpreadsheetSpace.Create(path, sheet)` / `CreateWithFormulas(...)`
(`Unrect.Spreadsheets`) read a whole sheet eagerly into a `SpreadsheetGridSpace : ISpreadsheetSpace`
— the simple default. `Workbook.Open(path)` (same namespace) is the streaming door: `book.Sheet(name)`
is one forward pass over the sheet's own cursor — read it inside the projection, once, before the
workbook that lent it is disposed. `Formula()`
composes into a file scoped to `ISheetCells`, but a *declaration* that calls it demands
`IFormulaSpace`, so it will not compile against the streaming door — read formulas through the
eager door instead. `projection.MapWorkbook(path, sheet)` / `MapWorkbookWithDiagnostics` are sugar
over the streaming loop's body (open, read, close) for a declaration over `ISheetCells` or the
canonical `ISpace`:

```csharp
var report = VerticalFlow(v => ...);               // one declaration, reused

foreach (var path in monthlyCloseOfFunds)
  Publish(report.MapWorkbook(path, "Detail"));      // bounded memory per iteration
```

Streaming's cost is declaration-shaped, not a flat tax — see `docs/streaming.md`.

## Entry — the one door in

A declaration file reaches the vocabulary through exactly one door: `using static
Unrect.Projections.ProjectionBuilders<TSpace>;`, plus a backend's own sibling closed class
(`using static Unrect.Spreadsheets.SheetProjectionBuilders<TSpace>;` or
`SpreadsheetProjectionBuilders<TSpace>`) for a backend's own leaves and matchers. There is no
non-generic entry point and no runtime scope value any more — `ProjectionBuilders<TSpace>`
answers the space once, at the class, in the `using` block, where C# already puts file-level
bindings, so every declaration below it is written with zero prefix.

```csharp
using Unrect.Projections;                                                      // the postfix half
using Unrect.Spreadsheets;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

var report = VerticalFlow(v =>
{
    var title = v.Next(Text());
    var rows  = v.Next(Table(headerRows: 1, eachRow: row));

    return v.Build(read => new Report(Title: read.Of(title), Rows: read.Of(rows)));
});
```

**The space is spelled in full, in the `using static` line, exactly once** — a `using` directive
resolves without the other `using`s around it, the one place in a C# file where a namespace import
does not help. What cannot follow into the closed class is the *postfix* half: C# forbids
extension methods inside a generic static class (CS1106), so `.Named`, `.Optional`, `.OrBlank`,
`.Select`, `.Until`, `.AsUnit`, `.AsScaffolding` and the placement modifiers still arrive through
the ordinary `using Unrect.Projections;` — the seam falls exactly on the geography law: what
spells before the subject is imported from the closed class, what spells after it is an extension.

Two boundaries, both load-bearing rather than incidental:

- **(a) One space per file — the dichotomy theorem.** Two closed `using static` imports over
  different space types collide on every shared member name (CS0121). For any two space types,
  either their difference *matters* to a declaration — in which case no single projection can
  serve both, and separate files are the semantic reality — or it does *not*, in which case both
  declarations target the shared base one import already covers. A file that seems to need both
  is two parsers sharing a file, and the fix is the file split.
- **(b) Scope the file to what the declarations READ, not to what the file parses.** A workbook
  opened `CreateWithFormulas` whose projections never call `Formula()` should be an `ISheetCells`
  file, not an `ISpreadsheetSpace` one. A **generic helper method**, not a file import, is how a
  hoisted library projection states its own minimum: `static IProjectionDefinition<TSpace, T>
  Helper<TSpace>(...) where TSpace : class, ISheetCells` composes into any file whose space can
  answer it, instantiated at that file's own space — write library helpers this way, against the
  narrowest constraint, and reserve a full file scope for application declaration files, which are
  one-document-one-space by their nature.

**(c) The backend pattern: a sibling closed class, disjoint names.** `Unrect` is backend-agnostic
and cannot name `Formula()`, `Decimal()` or any other kind-specific leaf, so a backend ships its
own closed generic class beside `ProjectionBuilders<TSpace>` — `SheetProjectionBuilders<TSpace>`
(over `ISheetCells`, no formulas) and `SpreadsheetProjectionBuilders<TSpace>` (over
`ISpreadsheetSpace`, with them) in `Unrect.Spreadsheets` — each re-exporting exactly its own
vocabulary and never a member the core class already publishes with the same signature. `Table`
and `Record` are the one exception, deliberately: the backend classes add OVERLOADS of those two
names (`Table<T>()`, `Table<T>(bind)`, `Record<T>(LabelMap)`), never a second member of the core's
own signature. Two `using static`s of two closed classes coexist exactly when they share no
colliding signature; that is the rule stated from the backend's side, the same rule (a) states
from the application side. The two backend classes are never imported together — a file imports
`SheetProjectionBuilders<TSpace>` where its declarations read a streamed sheet (no formulas) and
`SpreadsheetProjectionBuilders<TSpace>` where they read one that might have them; both publish the
kinded vocabulary under the same names on purpose, so they never meet.

**(d) The analyzer's diagnostics — the guidance rail.** `Unrect.Analyzers` (packed into the
`Unrect` package) reports two things the compiler cannot say about a demand:

| ID | Name | What it catches |
|---|---|---|
| `UNR001` | *(retired)* | Was the unnecessary-scope rule; retired with the scope apparatus itself. The identifier stays spent, not reused |
| `UNR002` | The demand door | Reserved for the code-fix on the compiler's own `CS1503`/`CS0311` at a composition site (`v.Next(...)`, `Choice(...)`, `Else(...)`) — "child demands `X`; use the backend's factory here." It reports no diagnostic of its own, so the ID is spent here rather than handed to a future rule |
| `UNR003` | Demands exceed offer | A `Map`/`Apply`/`MapWithDiagnostics`/`MapWorkbook` call whose projection demands a space the argument does not offer — a compile error already, reported by the compiler as an inference failure naming neither side. This rewrites it: `"this projection demands 'X'; this space offers 'Y'"` |

## Capabilities — what a backend adds to the vocabulary

A **capability** is what a class of spaces can do beyond `ISpace`: an interface the space
implements, demanded by the projections that use it, checked by the compiler at the point a
declaration composes or a `Map` call is made — there is no runtime capability transport any more.
`Unrect.Spreadsheets` ships one (`IFormulaSpace`, bundled with `ISheetCells` into
`ISpreadsheetSpace`):

| Operator | Meaning |
|---|---|
| `Formula()` | One cell, read as the formula behind it — the file's own expression without the `=`, null where the cell is a plain value. A cell has a value *and* a formula, so reading both is an `Overlay`, never a flow |
| `RowWithFormula()` / `RowWithFormula(containing)` and the column twins | Matchers over formulas — see "The typed phantoms," above, for how they carry their demand through `On`/`Below`/`Until` with nothing annotated. `containing` is a **substring, case-insensitively** |
| `IFormulaSpace` / `ISpreadsheetSpace` | The capability, and the bundle a declaration written over "a spreadsheet" demands |

**Where they come from.** `SpreadsheetSpace.CreateWithFormulas(path, sheet)` is a second factory
rather than a flag on the first, because the two answers differ in their *type*: what comes back
is an `ISpreadsheetSpace`, and the plain `Create` hands back a space that does not implement the
capability at all. The streaming door reads no formulas and says so by absence — `Workbook.Sheet`
returns a plain `ISheetCells`, so a formula-reading declaration will not compile against it, a
compile error rather than a run-time surprise or a file's formulas quietly read as none.

**A capability is spelled as a leaf or a matcher, never as a reach-through.** A hypothetical
`row.FormulaAt(2)` extension would compile against any table and read null over a plain grid, with
nothing in its type saying the declaration needs formulas. `Formula()` is a leaf precisely so that
composing it raises the demand in the type system, where a helper that only reads it under the
right constraint (`where TSpace : class, IFormulaSpace`) composes and one that does not never
compiles against a space that lacks it.

## The cross-cutting laws

- **The naming ladder.** A child's diagnostic identity is the first of: its own `.Named`; the
  bare identifier it was written as (captured at `v.Next(x)`, at `VerticalRepeat(x, ...)`'s item,
  and at `.Else(x)`'s fallback); otherwise `Description#ordinal`. Hoist projections into
  well-named locals and let the use site name them; a helper must not name what it returns.
- **Transparency, units and scaffolding — three separate marks.** An unnamed wrapper (`Select`,
  `Padded`, `Until`, a boundary) contributes no path segment by default; naming it makes it
  opaque and it claims the segment. `.AsUnit(name)` opaques a *composition* under its own label
  regardless of naming. `.AsScaffolding()` is the opposite direction: it hides a node a *factory*
  assembled (never one the user wrote) so a composed rung's rendered path reads exactly like a
  leaf's, while `FullPath` keeps the whole chain for drill-through.
- **Replace vs compose vs refuse.** Anchors (`.On`, `.Below`, `.RightOf`), `.OffsetBy` and extent
  (`.Sized`) replace a shape's own *default* and **refuse** a *declared* one. Movements compose.
  A second `Until` applied directly to another `Until` refuses; through a wrapper it nests. Inside
  the placement pipeline the same law holds with "refuse" tightened to "unspellable."
- **Failure discipline.** Kind failures speak kind ("expected Number at B4, found Text"); a
  conversion failure speaks conversion ("the Number at B4 is not a whole number"); every failure
  carries subject, declaration path, and an A1 location, resolved off the plane or point that
  failed — nothing is accumulated down the tree to produce it. A read failure inside a backend's
  own point-extension (`CellReadException`, carrying a `Point<ISpace>` and a `CellProblem`
  sentence) is caught and rethrown through the same one seam every lambda-calling primitive goes
  through, so a leaf and a bound table column describe the same bad cell identically.
- **The two design tests.** Does an operator let the user *say what the data looks like*, or *say
  how to walk it*? And could a writer execute the declaration — produce the file as well as read
  it? Declarations run backward; opaque code does not.
- **IO faults are not tolerance.** A disk failure, or a read against a `Workbook` view after its
  workbook is disposed, classifies as a fault (`EngineRules.IsFault`) rather than a
  disagreement about the data, at every site that could otherwise absorb a foreign exception as
  "section absent."
