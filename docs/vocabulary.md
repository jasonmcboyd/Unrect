# The Unrect Vocabulary

A survey of every operator in the projection layer, grouped by role in the algebra. Everything
here is available from a single `using static Unrect.Projections.Projection;` — except the
two raw lifts noted under Placement (`OffsetStrategies.To`/`Past`), which are an escape hatch
by design and spelled like one. For semantics in depth, each group cites its governing spec
in `docs/design/`.

Current as of 2026-09-06 (post the projection rename: the layer is `Unrect.Projections`, the
static vocabulary class is `Projection`, and "shape" now means only the geometry of a space).
When this file and a spec disagree, the spec is wrong or this file is stale — fix whichever it
is; do not let them drift silently.

## Leaves — where cells become values

| Operator | Yields | Notes |
|---|---|---|
| `Text()` `Decimal()` `Integer()` `Double()` `Date()` `Boolean()` | typed value | One cell; asserts its `CellKind`, applies the canonical accessor. The family is CLOSED over `CellValue`'s accessor set and never leads it — no `Long()`, ever; conversions beyond the set are `Select` territory (typed-leaves-and-tables-spec §2, "the firewall") |
| `Cell(v => ...)` | `T` | One cell, arbitrary projection — the escape hatch |
| `Row(r => ...)` / `Row(width, r => ...)` / `Row(IColumnStrategy, r => ...)` | from `CellStrip` | One row; width discovered (`while any value`), explicit count, or by column strategy (explicit counts are for structurally fixed regions only) |
| `Column(c => ...)` / `Column(height, c => ...)` / `Column(IRowStrategy, c => ...)` | from `CellStrip` | One column; height discovered (`while any value`), explicit count, or by row strategy (explicit counts are for structurally fixed regions only) |
| `Range(b => ...)` / `Range(w, h, ...)` / `Range(area, ...)` | from `CellBlock` | Rectangular block |
| `Caption(text)` | matched text (verbatim) | A declared anchor: seeks its row by the content rule, consumes exactly that row, asserts the text (matcher-and-caption-spec) |
| `Fields(Field(a), Field(b), ...)` | `IReadOnlyDictionary<string, CellValue>` | Labeled-pair block (label column + value column); self-anchors on its first label; labels matched colon-tolerantly (`LabelEquals`) |

## Tables — the ladder of commitment

| Operator | Yields | Notes |
|---|---|---|
| `TableRows()` | rows as caption-keyed dictionaries of `CellValue` | Exploratory: keys discovered from the file, looked up under the binding comparer; duplicate captions are a loud failure |
| `TableRows<T>()` / `TableRows<T>(bind => ...)` | `IReadOnlyList<T>` | Typed: captions bound to properties by `CaptionComparer` (case- and whitespace-insensitive), kinds inferred from property types (the closed set: `string`, `decimal`, `double`, `int`, `DateTime`, `bool`, their `Nullable<>` forms, and `CellValue`), `Nullable<>` AND an annotated `string?` both mean per-column blank tolerance, strict by default with `bind.Ignore(t => t.X)`; overrides `bind.Column(t => t.X, "caption")` |
| `TableRows(r => ...)` | `IReadOnlyList<T>` (`T` per row) | Full control: hand-written per-row projection with `r["Caption"]` / `r[i]` |
| `Table(t => ...)` | `T` for the whole table | Full control: one hand-written projection over the `TableView`, for tables that don't decompose row-by-row |

Graduate up the ladder as a table's shape firms: dictionary first to sight-read an
unfamiliar workbook, typed once you commit, lambda only when a column needs logic.

## Layout composites — the geometry claims

| Operator | Claim |
|---|---|
| `VerticalFlow(v => ...)` / `HorizontalFlow(v => ...)` | Stacked bands, one per child: each child's band spans the flow's full width, so no sibling ever shares it, even where the child's own content is narrower — but that is a claim on the band, not on what the flow reports consumed. Consumed across the axis is the max over children of their own consumed width (bounding box), not automatically the full width. `v.Next(projection)` declares the next child and returns its value; any arity |
| `Overlay(o => ...)` | One shared band; each child finds its own place by its own placement; no advance between children; consumed = bounding box |
| `VerticalRepeat(item, separatedBy:, atLeast:)` / `HorizontalRepeat(...)` | N items with separators (`sepBy`). A blank band is a separator, never a terminator — bound the repeat with `.Until` to end it at content. Both axes are marked, like the flows: no substrate's dominant axis is the unmarked normal case |
| `Choice(a, b, ...)` | The first alternative that fits; an Info per near-miss; a losing branch's diagnostics roll back |

The composite you pick is the geometric claim you make: flows say "stacked, one after
another"; overlays say "sharing a band, each finds its place." Flows never negotiate —
a child that does not fit throws, with a path and a cell; drift is an error, not a
layout problem.

## Placement — where things start

**Silence is adjacency.** A projection with no placement modifier starts exactly where the one
before it left off. Every operator below is therefore a *declared exception*, and each word
names the kind of reason it is an exception for — so reading a declaration you never have to
ask why a projection moved.

| Operator | Kind of reason | Meaning |
|---|---|---|
| `.On(rowLandmark)` / `.On(columnLandmark)` | a relation | The projection starts AT the match and OWNS that row/column. One word for both axes: occupancy has no direction, and the argument's type carries the axis |
| `.Below(rowLandmark)` | a relation | Starts on the row directly below the match — exactly one beyond, which is the matched row's own height and never a step you chose |
| `.RightOf(columnLandmark)` | a relation | The column twin of `.Below`. Spelled apart because the direction is part of what is being said, and it is grid-absolute (down the sheet, right along it), not "the next band along whichever way this flow runs" |
| `.AfterBlankRows()` / `.AfterBlankColumns()` | filler | Step over the blank band in front. Tolerant by nature: no filler means no movement, not a failure |
| `.Down(n)` / `.Right(n)` | a distance | Fixed movement, honestly named as one |
| `.OffsetBy(offsetStrategy)` | delegated | *My start is where that resolves to* — an assignment, and the one marked crossing from the cell-model surface into the interval-model strategy calculus |
| `.Sized(area)` | — | Not placement; the extent's own replace (see below) |

The anchors and `.OffsetBy` **REPLACE** the offset (including a default — that is how a `Table`
is told not to skip its blank rows); the movements **COMPOSE** onto whatever the projection
already had. A declared area survives all of them.

Absence semantics live in the word, not in a flag: a landmark that matches nothing is **loud**
(`Optional`/`Else` absorb it; a repeat reads it as having run out of sections), while a
filler-skip that finds no filler is **tolerant** and simply does not move.

The strategy vocabulary `.OffsetBy` takes — reached through the one door, so it is visible in a
declaration that it is being reached for:

| Operator | Meaning |
|---|---|
| `SkipRows(n)` `SkipColumns(n)` `BlankRows()` `BlankColumns()` | Fixed and blank-skipping offsets |
| `Then(a, b, ...)` | Sequence offsets; each searches only the space the previous shift left (seek the axis that discards least, first) |
| `FromRight(w)` / `FromBottom(h)` | From-end anchoring |
| `OffsetStrategies.To(m)` / `Past(m)` | The lifts `.On` / `.Below` / `.RightOf` are built on. Public in `Unrect.Strategies`, deliberately NOT re-exported on `Projection` — at projection level a landmark is placed by a modifier that names its own relation, and the raw lift is an escape hatch spelled like one |

## Placement — the six laws

The vocabulary above is what these six produce. They are the test any future placement
operator has to pass; an operator that cannot be justified by one of them does not belong.

1. **Silence is adjacency.** No modifier means "starts where the last one ended". Every
   placement operator is therefore a declared exception, and its word names the *kind* of
   reason — a relation (`.On`, `.Below`, `.RightOf`), filler (`.AfterBlankRows`), a distance
   (`.Down`, `.Right`), or a delegation (`.OffsetBy`).
2. **Grid-absolute over flow-relative, and direction appears in the word exactly when the
   concept has one.** `.Below` means down the sheet, not "next along whichever way this flow
   runs". `.On` names no direction because occupancy has none — one word, both axes, the
   argument's type carrying the axis.
3. **Positions are relations to things, never distances arrived at.** `.On(caption)` says
   *which row*; the arithmetic of reaching it is the engine's business, not the
   declaration's. This is what makes a declaration survive an inserted row — and what makes
   it runnable backward by a writer.
4. **Absence semantics are part of the word.** A landmark miss is loud and absorbable; a
   filler-skip that finds no filler is tolerant and silent. You never have to look up which
   one an operator is, or pass a flag to say.
5. **The surface vocabulary is cell-model (the A1 world of rows, columns and captions); the
   engine calculus stays interval-model (offsets and sizes over intervals).** They do not
   blend. `.OffsetBy` is the one *marked* crossing between them, which is why it is a word
   you can see in a declaration rather than an overload you fall into.
6. **The algebra never encodes one substrate's dominant axis as normal.** Spreadsheets grow
   downward; the vocabulary does not assume it. `VerticalFlow`/`HorizontalFlow`,
   `VerticalRepeat`/`HorizontalRepeat`, `.Below`/`.RightOf` — both halves marked, neither
   the default. (`.Until`/`.UntilColumn` is the one pair that is not, and deliberately: its
   argument does not have to be read to know the axis, so the row form carries no marking —
   matcher-and-caption-spec §1.6.)

## Extent — where things end

| Operator | Meaning |
|---|---|
| `.Sized(area)` | Declared extent, consumed in full (REPLACES) |
| `.Until(matcher)` / `.Until(matcher, orEnd: true)` / `.UntilColumn(...)` | Extent ends just BEFORE a forward landmark; the bound is consumed in full so the next sibling starts AT the landmark (its own `.On` finds it at distance zero). Strict by default; `orEnd` runs to the end of space and records an Info when exercised |
| `Extent(w, h)` `WholeExtent()` `NoExtent()` `RowsWhileAnyValue()` `RowsWhileAny(p)` `ColumnsWhileAnyValue()` `ColumnsWhileAny(p)` | The area vocabulary, mirrored on both axes |
| `TakeRows(n)` `TakeColumns(n)` `AllRows()` `AllColumns()` | Axis selectors, not area strategies — they return `IRowStrategy`/`IColumnStrategy`, for `Row(AllColumns(), ...)` / `Column(TakeRows(3), ...)` and for composing an extent from its two axes; not for `.Sized` (`.Sized(TakeRows(3))` does not compile) |

## Matchers — one family, four modifiers

`RowContaining(text)` · `RowWhere(spacePredicate)` · `RowWithCell(cellPredicate)` — and
the three column twins. One content rule everywhere: trimmed, case-insensitive,
whole-cell. Naming law: bare `Where` = whole-row/column predicate over the space;
`WithCell` = per-cell predicate; `Containing` = the content rule.

A matcher only *locates* and reports absence; what absence means belongs to the modifier
that takes it — `.On` (own the match), `.Below` / `.RightOf` (one beyond), `.Until` (bound by
it). All four describe a miss identically, because there is one matcher to describe. Because
a section can start at `.On(RowContaining("A"))` and end at `.Until(RowContaining("B"))`
through the same matcher, the start and the end cannot disagree about what a caption is.

Three matching rules exist in the library and deliberately never unify
(typed-leaves-and-tables-spec §3): the **content rule** above (matchers, `Caption`, and
also `TableView`/`TableRow`'s by-caption row access — `row["Caption"]` resolves trimmed
and case-insensitively, the same rule, so it has consumers beyond matchers and `Caption`
— literal ↔ cell text), **`LabelEquals`** (`Field` only — content rule plus a trailing
colon-run ignored), and **`CaptionComparer`** (typed `TableRows<T>` binding and the
`TableRows()` dictionary's keys — case- and whitespace-insensitive, bridging caption ↔
identifier). Each bridges a different pair of vocabularies; a declaration must never
start in one and end in another.

## Wrappers and boundaries

| Operator | Meaning |
|---|---|
| `.Under(params captions)` | Captions stacked above the projection, in reading order — sugar desugaring to the plain flow, so every caption is a real tree node with a real path segment |
| `.Padded(all)` / `(h, v)` / `(l, t, r, b)` | Shrink the inside; consumed includes the border |
| `.Optional()` | Tolerance boundary: absorbs a failure, yields `default`, records a Warning. Absorbed projections consume nothing — pair with content-anchored siblings |
| `.Else(fallbackProjection)` / `.Else(value)` | Fallback boundary; Warning carries the primary's failure; the fallback's identifier is captured for its own diagnostics |
| `.Select(f)` | Transform the value (single-value only) |
| `.Named(name)` | Explicit name — purely an OVERRIDE now; see the naming ladder below |

## Application

| Operator | Returns |
|---|---|
| `projection.Map(space)` | `T` (absorbed-tolerance diagnostics discarded) |
| `projection.MapWithDiagnostics(space)` | `MapResult<T>`: value + `ProjectionDiagnostic` list (incl. the unconsumed-space Info — the burn-down meter) |
| `projection.Apply(space)` | value + offset + consumed |

All three are usable as method groups — `spaces.Select(report.Map)` — and pinned so
(`MethodGroupTests`): no optional parameter may ever be added to them.

**Where the `space` comes from.** `SpreadsheetSpace.Create(path, sheet)` (`Unrect.Spreadsheets`)
reads a whole sheet eagerly, once, before any projection sees it — the simple default.
`Workbook.Open(path)` (same namespace) is the streaming door: `book.Sheet(name)` vends a lent
`ISpace` view over a windowed store instead of the whole grid — a value, not a handle, good to
slice and pass around until the workbook that vended it is disposed. Declare the projection once
and apply it to many files with the peak bounded per iteration, the idiom `Workbook` exists for:

```csharp
var report = VerticalFlow(v => ...);               // one declaration, reused

foreach (var path in monthlyCloseOfFunds)
{
  using var book = Workbook.Open(path);
  Publish(report.Map(book.Sheet("Detail")));        // bounded memory per iteration
}
```

Streaming's cost is declaration-shaped, not a flat tax — see `docs/design/streaming-spec.md` §2.7
for the full cost model and the sizing law (the window must be at least as tall as the tallest
extent a declaration holds open at once).

## Capabilities — what a backend adds to the vocabulary

A **capability** is what a class of spaces can do beyond `ISpace`: an interface the space
implements, demanded by the projections that use it, discharged by the backend at `Map`.
Nothing in `Unrect.Core` or `Unrect` names one; a backend package ships both the capability
and the vocabulary that reads it, and a declaration imports that vocabulary beside
`Projection`.

`Unrect.Spreadsheets` ships one today (`using static Unrect.Spreadsheets.SpreadsheetProjections;`):

| Operator | Meaning |
|---|---|
| `Formula()` | One cell, read as the formula behind it — the file's own expression without the `=`, null where the cell is a plain value. A cell has a value *and* a formula, so reading both is an `Overlay`, never a flow |
| `RowWithFormula()` / `RowWithFormula(containing)` and the column twins | Matchers over formulas. `containing` is a **substring, case-insensitively** — deliberately not the whole-cell content rule, because a formula is an expression and the useful question is whether it mentions something |
| `Formulas` | The demand witness, for the two places inference cannot reach: `VerticalFlow(Formulas, v => …)` and `projection.Demanding(Formulas)` |
| `IFormulaSpace` / `ISpreadsheetSpace` | The capability, and the bundle a declaration written over "a spreadsheet" demands. Library projections should demand the narrowest capability they use |
| `space.Capability<T>()` / `space.RequiredCapability<T>(demandedBy)` | The transport seam (`Unrect`), which walks `ISpaceChart` wrappers. A raw `space is IFormulaSpace` is the wrong question: through a discovered extent it answers false over a sheet that plainly has the capability |

**Where they come from.** `SpreadsheetSpace.CreateWithFormulas(path, sheet)` is a second
factory rather than a flag on the first, because the two answers differ in their *type*:
what comes back is an `ISpreadsheetSpace`, and the plain `Create` hands back a space that
does not implement the capability at all. `.xls` and the streaming door read no formulas and
say so by absence; asking a `.xls` for them throws rather than answering null.

**The three laws they obey:**

- **Slicing never changes geometry.** A capable space's subspaces are capable, with
  coordinates translated; a slice may never invent a capability its parent lacked nor shed
  one it had. `ISpaceChart` is for coordinate-*preserving* wrappers only — a wrapper that
  translates must implement the capability itself, because handing back the inner space
  would answer about the wrong cells.
- **Absence means two different things.** At a projection site it is null, an honest
  per-cell answer. At a boundary — a matcher — it is a `MissingCapabilityException`, classed
  as a fault: "I could not look" and "I looked and it is not there" never share a spelling,
  so no `.Optional()` can report a wrong backend as an absent section.
- **A capability is spelled as a leaf, never as a reach-through.** `row.FormulaAt(2)` would
  compile against any table and raise no demand — the trapped-knowledge shape. The leaf is
  what makes composition carry the requirement.

## The cross-cutting laws

- **The naming ladder.** A child's diagnostic identity is the first of: its own
  `.Named`; the bare identifier it was written as (captured at `v.Next(x)`, at
  `VerticalRepeat(x, ...)`'s item, and at `.Else(x)`'s fallback — never at `Map`, which is
  the declaration/infrastructure seam); otherwise `Description#ordinal`. Hoist projections
  into well-named locals and let the use site name them; a helper must not name what it
  returns.
- **Transparency.** Unnamed wrappers (`Select`, `Padded`, `Until`, boundaries)
  contribute no path segment; naming a wrapper makes it opaque and it claims the segment.
- **Replace vs compose.** Anchors (`.On`, `.Below`, `.RightOf`), `.OffsetBy` and extent
  (`.Sized`) replace; movements (`.Down`, `.Right`, `.AfterBlankRows`,
  `.AfterBlankColumns`) compose, and strategy-level offsets compose via `Then`; `Until`
  replaces only when applied directly to another `Until` (through a wrapper it nests, both
  bounds in force); wrappers nest.
- **Failure discipline.** Kind failures speak kind ("expected Number at B4, found
  Text" — never "expected Decimal"); conversion failures speak conversion ("the Number
  at B4 is not a whole number"); every failure carries subject, declaration path, and
  an A1 location. Tolerance is declared at the exact projection where it is acceptable, and
  a diagnostic is the record of tolerance being exercised — there is no ambient lenient
  mode.
- **The two design tests.** Does an operator let the user *say what the data looks
  like*, or *say how to walk it*? And could a writer execute the declaration —
  produce the file as well as read it? Declarations run backward; opaque code does not.
- **IO faults are not tolerance.** A disk failure, or a read against a `Workbook` view
  after its workbook is disposed, classifies as a fault (`ProjectionEngine.IsFault`) rather
  than a disagreement about the data, at every site that could otherwise absorb a
  foreign exception as "section absent" — `.Optional()`, `.Else()`, and `Choice` all let
  it through unchanged. A wrong-kind cell or a missing anchor is still absorbable; the
  environment failing underneath the read is not.
