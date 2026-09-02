# The Unrect Vocabulary

A survey of every operator in the shape layer, grouped by role in the algebra. Everything
here is available from a single `using static Unrect.Shapes.Shape;`. For semantics in
depth, each group cites its governing spec in `docs/design/`.

Current as of 2026-09-02 (post typed-leaves-and-tables). When this file and a spec
disagree, the spec is wrong or this file is stale — fix whichever it is; do not let them
drift silently.

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
| `VerticalFlow(v => ...)` / `HorizontalFlow(v => ...)` | Stacked bands, one per child: each child's band spans the flow's full width, so no sibling ever shares it, even where the child's own content is narrower — but that is a claim on the band, not on what the flow reports consumed. Consumed across the axis is the max over children of their own consumed width (bounding box), not automatically the full width. `v.Next(shape)` declares the next child and returns its value; any arity |
| `Overlay(o => ...)` | One shared band; each child finds its own place by its own placement; no advance between children; consumed = bounding box |
| `Repeat(item, separatedBy:, atLeast:)` / `RepeatHorizontal(...)` | N items with separators (`sepBy`). A blank band is a separator, never a terminator — bound the repeat with `.Until` to end it at content |
| `Choice(a, b, ...)` | The first alternative that fits; an Info per near-miss; a losing branch's diagnostics roll back |

The composite you pick is the geometric claim you make: flows say "stacked, one after
another"; overlays say "sharing a band, each finds its place." Flows never negotiate —
a child that does not fit throws, with a path and a cell; drift is an error, not a
layout problem.

## Placement — where things start

| Operator | Meaning |
|---|---|
| `.After(offset)` | Anchor/move the shape — REPLACES the offset (a declared area survives; `.Sized` is the area's own replace) |
| `To(matcher)` / `Past(matcher)` | Move to the matched row/column, or just beyond it. A miss throws — and a miss is `Repeat`'s clean stopping condition |
| `Then(a, b, ...)` | Sequence offsets; each searches only the space the previous shift left (seek the axis that discards least, first) |
| `SkipRows(n)` `SkipColumns(n)` `BlankRows()` `BlankColumns()` `.AfterBlankRows()` `.AfterBlankColumns()` `.Down(n)` `.Right(n)` | Fixed and blank-skipping movements; movement modifiers compose |
| `FromRight(w)` / `FromBottom(h)` | From-end anchoring |

## Extent — where things end

| Operator | Meaning |
|---|---|
| `.Sized(area)` | Declared extent, consumed in full (REPLACES) |
| `.Until(matcher)` / `.Until(matcher, orEnd: true)` / `.UntilColumn(...)` | Extent ends just BEFORE a forward landmark; the bound is consumed in full so the next sibling starts AT the landmark (its own `After` finds it at distance zero). Strict by default; `orEnd` runs to the end of space and records an Info when exercised |
| `Extent(w, h)` `WholeExtent()` `NoExtent()` `RowsWhileAnyValue()` `RowsWhileAny(p)` `ColumnsWhileAnyValue()` `ColumnsWhileAny(p)` | The area vocabulary, mirrored on both axes |
| `TakeRows(n)` `TakeColumns(n)` `AllRows()` `AllColumns()` | Axis selectors, not area strategies — they return `IRowStrategy`/`IColumnStrategy`, for `Row(AllColumns(), ...)` / `Column(TakeRows(3), ...)` and for composing an extent from its two axes; not for `.Sized` (`.Sized(TakeRows(3))` does not compile) |

## Matchers — one family, three lifts

`RowContaining(text)` · `RowWhere(spacePredicate)` · `RowWithCell(cellPredicate)` — and
the three column twins. One content rule everywhere: trimmed, case-insensitive,
whole-cell. Naming law: bare `Where` = whole-row/column predicate over the space;
`WithCell` = per-cell predicate; `Containing` = the content rule. Every lift (`To`,
`Past`, `.Until`) describes a miss identically, because there is one matcher to describe.

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
| `.Under(params captions)` | Captions stacked above the shape, in reading order — sugar desugaring to the plain flow, so every caption is a real tree node with a real path segment |
| `.Padded(all)` / `(h, v)` / `(l, t, r, b)` | Shrink the inside; consumed includes the border |
| `.Optional()` | Tolerance boundary: absorbs a failure, yields `default`, records a Warning. Absorbed shapes consume nothing — pair with content-anchored siblings |
| `.Else(fallbackShape)` / `.Else(value)` | Fallback boundary; Warning carries the primary's failure; the fallback's identifier is captured for its own diagnostics |
| `.Select(f)` | Transform the value (single-value only) |
| `.Named(name)` | Explicit name — purely an OVERRIDE now; see the naming ladder below |

## Application

| Operator | Returns |
|---|---|
| `shape.Map(space)` | `T` (absorbed-tolerance diagnostics discarded) |
| `shape.MapWithDiagnostics(space)` | `MapResult<T>`: value + `ShapeDiagnostic` list (incl. the unconsumed-space Info — the burn-down meter) |
| `shape.Apply(space)` | value + offset + consumed |

All three are usable as method groups — `spaces.Select(report.Map)` — and pinned so
(`MethodGroupTests`): no optional parameter may ever be added to them.

## The cross-cutting laws

- **The naming ladder.** A child's diagnostic identity is the first of: its own
  `.Named`; the bare identifier it was written as (captured at `v.Next(x)`, at
  `Repeat(x, ...)`'s item, and at `.Else(x)`'s fallback — never at `Map`, which is the
  declaration/infrastructure seam); otherwise `Description#ordinal`. Hoist shapes into
  well-named locals and let the use site name them; a helper must not name what it
  returns.
- **Transparency.** Unnamed wrappers (`Select`, `Padded`, `Until`, boundaries)
  contribute no path segment; naming a wrapper makes it opaque and it claims the segment.
- **Replace vs compose.** Placement (`After`) and extent (`Sized`) replace; movements
  compose via `Then`; `Until` replaces only when applied directly to another `Until`
  (through a wrapper it nests, both bounds in force); wrappers nest.
- **Failure discipline.** Kind failures speak kind ("expected Number at B4, found
  Text" — never "expected Decimal"); conversion failures speak conversion ("the Number
  at B4 is not a whole number"); every failure carries subject, declaration path, and
  an A1 location. Tolerance is declared at the exact shape where it is acceptable, and
  a diagnostic is the record of tolerance being exercised — there is no ambient lenient
  mode.
- **The two design tests.** Does an operator let the user *say what the data looks
  like*, or *say how to walk it*? And could a writer execute the declaration —
  produce the file as well as read it? Declarations run backward; opaque code does not.
