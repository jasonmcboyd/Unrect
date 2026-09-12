# Composing primitives into streaming projections

**Status: LIVING (current direction, branch `experiment/record-primitive`).** The single source of
truth for the labeled-axes / primitive-composition thread. It replaces ten contradictory step-specs
gutted 2026-09-12 (git history keeps them); do not re-fragment it into a step-chain, and delete this
in favour of code + CLAUDE.md once the direction below is shipped.

## The plot (do not lose it again)

The primitives — `ColumnLabels`, `WithColumnLabels`, `Record` — exist so a **third party** can compose
them into their own labeled region (a right-/bottom-header table, a pivot) and have it work as well as
ours. "As well as ours" **includes streaming.** So the acceptance bar is not "the primitives can
reproduce `Table`'s *values* in a test" — it is "the primitives compose into a projection that
**streams**, and `Table` *is* that composition." If composing them forces the whole block into memory,
we have shipped primitives that don't actually compose into a production-grade projection, and the
third-party promise is hollow. A bespoke streaming `Table` beside forcing primitives is an exemption
nobody else can have — that is the trap we fell into and are reversing.

## Canonical decision (agreed 2026-09-12)

`Table` is **composed from the primitives** and **streams**:

```
VerticalFlow(flow => {
  var columns = flow.Next(ColumnLabels(headerRows));
  return flow.Next(WithColumnLabels(columns, VerticalRepeat(Record(record))));
})
```

Sufficiency is judged including the **forcing profile** (rows materialised at first record, at
completion), not only value / message / A1 / `row.Index`. The bespoke leaf is interim; it is retired
once the composition matches it on all of those.

## Next step (before the build) — catalog the primitives

Before building the GAP-A fix, inventory the primitives. For each building block a larger
projection composes from — the leaves, the layouts (`VerticalFlow`/`HorizontalFlow`/`Overlay`),
the repeats, the label/record primitives (`ColumnLabels`/`WithColumnLabels`/`Record`), the
matchers/`Caption`/`Fields` — record the properties that bear on composition: what it reads, its
default placement and extent, whether it streams or forces its bound, and how it composes with a
parent. That catalog is the map for making the primitives compose-and-stream (and for judging the
leaf/composite question below).

## The blocker and the fix — GAP A (streaming)

Composing `Table` this way makes the flow a **composite**, and the engine resolves a composite's
discovered extent at first-child placement (`Exceeds` + `GetSubspace(offset)`) — the whole block, up
front. The bespoke leaf avoids that only because `TableView.StreamRows` walks a `BoundedSpace`
row-by-row via `HasRow`.

**Fix:** give `VerticalRepeat` that same walk — re-host the discovered bound **inside the repeat** so
the repeat becomes `StreamBands`-as-a-combinator: the flow carries no composite area (it just
accumulates its children's extents), and the repeat streams the body one row past the cursor via
`HasRow`, reusing the exact `BoundedSpace` machinery. No engine change; the repeat is already a
row-by-row walker rather than a slicer.

## Open questions (decide before / during the build — do NOT pre-lock)

1. **Is "leaf" still a real distinction?** Once a composition can stream, "leaf" bundles three things
   that turn out to be separable: **streaming** (achievable by a composite via the lazy repeat),
   **opacity to tooling** (already how layout composites behave — children exist only while the lambda
   runs), and a **flat diagnostic path** (that is GAP B, below). If those come apart, `Table` may just
   be a streaming composition *presented as a named opaque unit*, and the leaf/composite engine
   category may be obsolete. Open — to discuss, not to assume either way.
2. **The repeat terminator's user-facing shape.** `VerticalRepeat(item, within: <area>)` ("fill this
   discovered block") vs a **lazy while-terminator** — a repeat that stops when the next row peeks
   blank, which is the same logic as the shipped `onBlank: Stop` lifted onto the general repeat. The
   second composes with the `onBlank` vocabulary and avoids a second way to say "discovered block".
3. **GAP B — path / subject parity.** The composition's diagnostics read `VerticalRepeat[i] -> Record`
   where the leaf says flat `Table`. If `Table` is to *become* the composition, its diagnostics must
   not regress — a path-flattening / naming decision.
4. **Shared header/body width.** The header consume (`ColumnLabels`) and the body walk must share **one**
   discovered width (one `BoundedSpace` threaded across both) or a ragged / trailing-blank-column sheet
   drifts between the composition and the leaf.

## Already shipped this thread (truth is in code + tests; do not re-describe here)

- `SkipToFirstNonBlankCell` offset strategy — commit `5add5e9`.
- `Table onBlank` blank-row strategy (Stop/Skip/Fault/Tolerate/blankRecord) — commit `1cc6c5a`.
- Uniform offset law (a declared pipeline offset replaces the shape default; one `Steps.Offset` rule) —
  commit `b4838bc`.

## Parked (elsewhere, not here)

Anchors as composable forward-searches for nth-instance addressing (`Below(X).On(X)` = the second X);
trailing/right-header reversal. Both are downstream of the primitives composing cleanly.

## The primitive catalog

Code-grounded inventory as of branch `experiment/record-primitive`. Every claim cites `file:line`.
The load-bearing column is **Bound handling** (does it defer/stream or force). The mechanism behind
it is one law, so read this first:

- **The engine binds a placement into a streaming `BoundedSpace` iff** the placement declares an area
  AND that area is an `IIncrementalAreaStrategy` AND the placement is strict (a repeat item is never
  strict) — `ProjectionEngine.Bind` (`ProjectionEngine.cs:169-196`), gated at `:171`. A derived
  extent (`Placement.Area == null`) is never bound; the projection is handed the raw available space
  (`ProjectionEngine.cs:102-105`). A declared non-incremental area (e.g. `ExplicitArea`) is measured
  up front and sliced to a fixed subspace (`:114-143`).
- **Which areas are incremental:** `RowsThenColumns(incrementalRows, rowMajorColumns)` folds to an
  `InterleavedRowAndColumnSizeStrategy` (`RowAndColumnSizeStrategy.cs:32-38`), which is
  `IIncrementalSizeStrategy`; `ToAreaStrategy` carries incrementality across
  (`SizeStrategyExtensions.cs:15-18`). `DiscoveredBlock()` =
  `TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue()` (`Projection.cs:850`) is therefore incremental;
  `ExplicitArea`, `FullRow`, and `ToEdgeBlock` (`AllRows()…`, `Projection.cs:857`) are not.
- **A bound streams forward and forces on a dimension query** — reading a cell/subspace advances the
  scan only as far as named; asking `Area`/`Height` reads it to exhaustion
  (`BoundedSpace.cs:68,71-99,134-141`). The views expose the free width and the forward `HasRow`
  probe internally (`BoundedSpace.cs:114-115,126-127`); a public `ISpace.Area` always forces.
- **The composite forcing trap (GAP A):** the moment a bound is handed to a composite, placing its
  first child forces the whole height — `Exceeds(offset.Size, availableSpace)` reads `Area`
  (`ProjectionEngine.cs:91,303-304`) and `GetSubspace(offset)` reads `Area` to compute the remainder
  (`SpaceExtensions.cs:33,36`). The engine pins this as the deliberate current limit
  (`ProjectionEngine.cs:159-167`).

Primitives come in two kinds — **value** (leaves that read cells and produce values) and
**structure** (combinators that orchestrate children and produce structure). "Engine-composite" (has
child projections) is a *different axis* from "not a primitive": a combinator has children yet is
irreducible — it cannot be built from other primitives — so it IS a primitive. Only `Table` and
`Fields` are composite building blocks assembled from other primitives.

### Value primitives (leaves)

Read cells, produce values.

| Name / factory (class) | Reads (T) | Default placement | Bound: defer/stream vs force | How a parent places it / composes | Notes |
|---|---|---|---|---|---|
| `Cell` (`CellProjection<T>`) | one cell → `T` | `NoOffset` + `ExplicitArea(1,1)` (`Projection.cs:25`) | Force — fixed 1×1, non-incremental, engine slices exact | placed as a fixed 1×1 subspace | asserts 1×1 or throws (`CellProjection.cs:22-24`) |
| six typed leaves `Text/Decimal/Integer/Double/Date/Boolean` (`TypedCellProjection<T>`) | one cell of a declared `CellKind` → `T` | `NoOffset` + `ExplicitArea(1,1)` (`Projection.cs:77`) | Force — fixed 1×1 | fixed 1×1 subspace | `OrBlank` clones as blank-tolerant `T?` (`TypedCellProjection.cs:83-100`); kind is declaration data, not lambda |
| `Row` (`StripProjection<T>`, Horizontal) | one row → `T` via `CellStrip` | `Placement.Of(TakeRows(1).TakeColumnsWhileAnyValue())` (`Projection.cs:81`) — incremental, 1 row tall | Bound created but single-band; width free, height=1 | fixed-height band | `Row(w)`=`ExplicitArea(w,1)` (`:85`); `CellStrip.Count` free for a row (`CellStrip.cs:41`) |
| `Column` (`StripProjection<T>`, Vertical) | one column → `T` via `CellStrip` | `Placement.Of(TakeColumns(1).TakeRowsWhileAnyValue())` (`Projection.cs:93`) — incremental | Streams-ish: bound over height, but `CellStrip.Count`=`Space.Area.Height` forces on use (`CellStrip.cs:41`) | fixed-width strip | `Column(h)`=`ExplicitArea(1,h)` (`:97`) |
| `Range` (`BlockProjection<T>`) | rectangle → `T` via `CellBlock` | `Placement.Of(DiscoveredBlock())` (`Projection.cs:108`) — incremental | **Streams**: engine binds a `BoundedSpace`; `BlockProjection` runs the lambda first and measures `extent.Area.Size` only after (`BlockProjection.cs:26-27`) | fixed / discovered block subspace | `Range(w,h)`=`ExplicitArea` (`:112`), `Range(area)` (`:115`); `CellBlock.Width` free, `.Height/.Rows` force (`CellBlock.cs:50,56`) |
| `Caption` (`CaptionProjection`) | matched row's own text → `string` | `To(RowContaining(text))` offset + `FullRow()` area (`Projection.cs:144`) | Force — reads `extent.Area.Size` (`CaptionProjection.cs:35`); offset scans for the row | placed at the found row, full width | asserts match, yields file's spelling untrimmed (`:44-50`) |
| `Field` (`FieldProjection`) | label+value pair → `CellValue` | `NoOffset` + `ExplicitArea(2,1)` (`Projection.cs:634`) | Force — fixed 2×1 | fixed 2×1 subspace | only used as a child of `Fields`; value blank ⇒ `Blank`, not failure (`FieldProjection.cs:31-47`) |
| `ColumnLabels` (`ColumnLabelsProjection`) | header band → `LabelMap` | `Placement.Of(RowsThenColumns(TakeRows(headerRows), AllColumns()))` (`Projection.cs:527`) | Force — builds a throwaway `TableView`, reads `extent.Area.Size` (`ColumnLabelsProjection.cs:28`); band is `headerRows` tall (small) | placed as the header band | reuses `TableView` header parse so labels match `Table` byte-for-byte (`:21-28`) |
| `Record` (`RecordProjection<T>`) | one body row → `T` via `TableRow` | `Placement.Of(FullRow())` (`Projection.cs:560`) — 1 row, full width | Force at end — reads `extent.Area.Size` after the lambda (`RecordProjection.cs:35`); band is 1 row | one full-width row band; resolves columns through ambient `LabelAxis.Column` scope | `TableRow` has no owning `TableView`; ordinal from `context.Ordinal` set by the enclosing repeat (`RecordProjection.cs:33-35`) |
| `Formula` (`Unrect.Spreadsheets`) | one cell's formula text → `string?` | `Range(1,1,…)` = `ExplicitArea(1,1)`, `.Named("Formula").Demanding(Formulas)` (`SpreadsheetProjections.cs:56-59`) | Force — fixed 1×1 | fixed 1×1 subspace | not a reach-through; demands `IFormulaSpace`; `.Named` so path reads `Formula` not `Range(1,1)` |
| `Nothing` / ε (`NothingProjection<T>`) | nothing → `default!` | `Placement.Default` (derive) (`NothingProjection.cs:26`) | Neither — consumes `Size(0,0)`, `Presence.Empty` (`:32-33`) | the layout-algebra unit; internal, not vocabulary | one shared `Instance` (`:23`) |

### Structure primitives (combinators)

Orchestrate children, produce structure. Each has child projections yet is irreducible — none can be
built from the other primitives — so each is a primitive, not a composite.

**Multi-child:**

| Name / factory (class) | Reads (T) | Default placement | Bound: defer/stream vs force | How it composes | Notes |
|---|---|---|---|---|---|
| `VerticalFlow`/`HorizontalFlow` (`FlowProjection<T>` → `FlowState`) | whatever the lambda builds → `T` | `Placement.Default` (derive; Area null) (`Projection.cs:49,57`) | Derives its extent, so the engine never binds it — handed the raw available space. **But if handed a `BoundedSpace`** it forces at first child: `FlowState.Next` slices `Extent.GetSubspace(cursor)` which reads `Area` (`FlowState.cs:64`, `SpaceExtensions.cs:33,36`) | hands each child a full-width band from the cursor, accumulating advances (`FlowState.cs:38-73`) | opaque to tooling — children exist only while the lambda runs (`LayoutProjection.cs:26-59`); "empty sibling" note (`FlowState.cs:66-88`); `Heading` desugars to a named vertical flow (`ProjectionBase.cs:147-162`) |
| `Overlay` (`OverlayProjection<T>` → `OverlayState`) | whatever the lambda builds → `T` | `Placement.Default` (derive) (`Projection.cs:81`) | Same as flow: not bound by the engine; forces a handed `BoundedSpace` at first child via `Exceeds` inside child's `TryPlace` (`OverlayState.cs:37`, `ProjectionEngine.cs:91,303`) | hands every child the whole extent + unadvanced context; children may overlap (`OverlayState.cs:30-44`) | consumed = bounding box of children (`OverlayState.cs:22-26`); opaque |
| `VerticalRepeat`/`HorizontalRepeat` (`RepeatProjection<T>`) | `IReadOnlyList<T>` | `Placement.Default` (derive) (`Projection.cs:813`) | **Forces** its extent today: each attempt slices `extent.GetSubspace(Step(cursor))` (reads `Area`, `RepeatProjection.cs:110,113`) and tests `IsEmpty(remaining)` which reads `Area` (`:115,180`). A `BoundedSpace` handed in is fully resolved on iteration 1. | walks occurrences: separate → place → `TryApply` the item (item's placement is non-strict, never deferred) → collect (`:103-146`). Stops when the item's placement fails or consumes/advances zero (`:135-140`) | item labelled from its use site; `atLeast`, `separatedBy`; per-occurrence index/ordinal stamped on context (`:122`). This is the structure primitive GAP A must convert to a `HasRow`-driven walk. |
| `Choice` (`ChoiceProjection<T>`) | first matching alternative → `T` | `Placement.Default` (derive) (`Projection.cs:757`) | Passes the same `extent` to each alternative via `Apply`; forcing is whatever the winning alternative does | tries alternatives in order against the same extent, rolling back diagnostics of losers (`ChoiceProjection.cs:40-66`) | faults pass through (`:51`); no per-alternative name capture (params array) |
| `.Else(fallback)` / `.Else(value)` (`BoundaryProjection<T>`) | `T` | `Placement.Default` (`ProjectionExtensions.cs:221`, `ProjectionBase.cs:164-174`) | forwards `extent` to inner via `Apply`; on absorbable failure runs fallback or yields value | tolerance boundary; innermost (own placement resolved first) | transparent when unnamed (`BoundaryProjection.cs:48`); faults not absorbed (`:61`) |
| `.Optional()` (`BoundaryProjection<T?>`) | `T?` | `Placement.Default` (`ProjectionExtensions.cs:237`) | as `.Else`; no fallback ⇒ consumes `Size(0,0)`, `Presence.Absorbed` (`BoundaryProjection.cs:75`) | tolerance boundary yielding null | Absorbed carries a possibly-nonzero extent under a declared area (`:69-75`) |

Both layouts fault if the lambda declares nothing (`LayoutProjection.cs:53-55`); presence = Read if any child read (`LayoutState.cs:57`).

**Single-child wrappers:**

| Name / factory (class) | Reads (T) | Default placement | Bound: defer/stream vs force | How it composes | Notes |
|---|---|---|---|---|---|
| `WithColumnLabels` (`WithLabelsProjection<T>`) | forwards body → `T` | `Placement.Default` (`Projection.cs:547`) | **Forces nothing of its own** — forwards the whole `extent` to the body via `Apply` under a pushed `LabelMap` (`WithLabelsProjection.cs:37-42`) | single-child; pushes ambient column labels, body reads at the same frame (identity translation) | transparent when unnamed (`:35`); the "provide a map to subspaces" primitive |
| `Select` (`MapProjection<TSource,TResult>`) | `f(inner)` → `TResult` | `Placement.Default` (`ProjectionExtensions.cs:292`) | forwards `extent` to inner via `Apply` (`MapProjection.cs:33`) | single-child; maps the value only | transparent when unnamed (`:29`) |
| `Padded` (`PadProjection<T>`) | forwards inner → `T` | `Placement.Default` (`ProjectionBase.cs:131`) | **Forces** — reads `extent.Area.Size` at top of `Project` (`PadProjection.cs:41`), then slices the inset | single-child; insets the extent, reports whole as consumed | transparent when unnamed (`:37`) |
| `Until`/`UntilColumn` (`UntilProjection<T>`) | forwards inner → `T` | `Placement.Default` (`ProjectionBase.cs:144`) | **Forces** — reads `extent.Area.Size` and scans for the landmark (`UntilProjection.cs:57-58`) | single-child; bounds extent at a content landmark, consumes the bound in full | transparent when unnamed (`:39`); refuses a second `Until` (`:47-53`) |
| `.Named(name)` | receiver's `T` | clone with same placement | no change; makes the node opaque/named (adds a path segment) | modifier, returns a clone of same runtime type (`ProjectionBase.cs:58-63`) | flips `IsTransparent` to false on wrappers whose transparency is `Name is null` |
| `.OrBlank()` (typed-cell clone) | `T?` | leaf's own placement (`TypedCellProjection.cs:97`) | Force — still a 1×1 leaf | not a wrapper: clones the typed leaf as blank-tolerant | tolerates blank only, not wrong kind (`:60-71`) |

### Composites (built from primitives — NOT primitives)

These are assembled from the value and structure primitives above, so neither is irreducible. `Table`
is listed as the **reference** its target composition must match — on values *and* on the
streaming/forcing profile.

| Name / factory (class) | Reads (T) | Default placement | Bound: defer/stream vs force | Composed of | Notes |
|---|---|---|---|---|---|
| **`Table`** (`TableProjection<T>`) — **INTERIM** | header + body rows → `T` via `TableView` | `TablePlacement()` = `SkipToFirstNonBlankCell` offset + `DiscoveredBlock()` (Stop) / `ToEdgeBlock()` (other onBlank) (`Projection.cs:503,841-848`) | **Streams** (Stop): engine binds a `BoundedSpace`; `TableView.StreamRows`→`StreamBands` walks one row past the cursor via `BoundedSpace.HasRow`, never asks `Area` (`TableView.cs:111-140`, `TableProjection.cs:36-44`). Non-Stop uses `ToEdgeBlock` (runs to enclosing edge). | today a bespoke leaf; TARGET is `VerticalFlow(ColumnLabels; WithColumnLabels(VerticalRepeat(Record)))` | **NOT a primitive** — an interim bespoke leaf to be retired once the target composition matches it on values and forcing profile. The only building block that streams a multi-row discovered bound today. `RowCount`/`Rows`/`Location` force (`TableView.cs:63,88,95`); `ColumnCount`+header free (`:56`) |
| `Fields` (a `FlowProjection` of `FieldProjection`s) | `IReadOnlyDictionary<string,CellValue>` | `FieldsPlacement(firstLabel)` — anchors on first field's label (`Projection.cs:649`) | Composite (vertical flow) — forcing per the `VerticalFlow` row | a `VerticalFlow` of `Field` children | opaque vertical flow; each field one 2×1 band; children built once at construction (`:631-634`); keyed by declared labels |

### Synthesis — what streams, what forces, and what it means for GAP A

- **Exactly one projection streams a multi-row discovered bound today: the leaf `Table`.** Its
  `DiscoveredBlock` placement is incremental, so the engine binds a `BoundedSpace`
  (`ProjectionEngine.cs:169-196`), and `TableView.StreamRows`→`StreamBands` walks the body one row
  past the cursor via `BoundedSpace.HasRow`, never asking `Area` (`TableView.cs:111-140`). That is the
  bespoke exemption GAP A wants to dissolve.
- **`Range`/`Row`/`Column` also get a bound**, but they are single-region leaves: they read once and
  settle it (`BlockProjection.cs:26`, `CellStrip.cs:41`). No multi-band walk, so streaming is moot.
- **Flows and overlays never create a bound** — their placement is derived (`Area == null`), so the
  engine hands them the raw available space (`ProjectionEngine.cs:102-105`). They are the natural
  home for a streaming body *provided nothing above them owns a bound*.
- **The forcing happens at two composite seams, both reading `Area`:** `FlowState.Next`'s
  `Extent.GetSubspace(cursor)` (`FlowState.cs:64` → `SpaceExtensions.cs:33,36`), and any child's
  `Exceeds` during `TryPlace` (`ProjectionEngine.cs:91,303`) — which is how an overlay
  (`OverlayState.cs:37`) forces. So the instant a discovered bound is handed *down into* a flow or
  overlay, its full height resolves at first-child placement. The engine documents this as the
  deliberate, pinned limit (`ProjectionEngine.cs:159-167`).
- **`VerticalRepeat` forces today too**, independently of the above: it slices
  `extent.GetSubspace(Step(cursor))` and tests `IsEmpty` on every attempt, both reading `Area`
  (`RepeatProjection.cs:110,113,115,180`). A repeat handed a `BoundedSpace` resolves it on iteration 1.
- **Implication for `Table` = composition (`VerticalFlow(ColumnLabels; WithColumnLabels(VerticalRepeat(Record)))`):**
  to match the leaf on values *and* forcing profile, the discovered bound cannot be declared on the
  flow (that makes the flow a composite whose first-child placement forces the whole block) and cannot
  be handed to the current repeat (which forces via `GetSubspace`/`IsEmpty`). The fix in this doc's
  §"The blocker and the fix" — re-host the bound **inside** the repeat and make it walk one row past
  the cursor via `HasRow`, reusing the exact `BoundedSpace`/`StreamBands` machinery `Table` already
  proves — is consistent with the code: the flow then carries no composite area (it only accumulates
  child advances, `FlowState.cs:90-96`), `WithColumnLabels` and `Record` already force nothing of
  their own beyond a single band, and the one seam that forces (`GetSubspace(offset)`/`IsEmpty`)
  disappears from the repeat's hot loop.
- **On the leaf/composite question (open #1):** the three things "leaf" currently bundles are already
  separable in code — streaming is a property of the *placement* (incremental area → `BoundedSpace`),
  not of being a leaf; opacity-to-tooling is exactly how layouts already behave
  (`LayoutProjection.cs:26`, `IsTransparent`/`Children`); and the flat diagnostic path is the only
  thing genuinely tied to the leaf `Table` (GAP B). Nothing in the engine privileges "leaf" for
  streaming — `Bind` keys off the strategy, not the projection kind — so a streaming composite is
  reachable without an engine change once the repeat stops forcing.
