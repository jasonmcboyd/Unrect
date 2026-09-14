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

`Table` is **composed from the primitives** and **streams**. Shipped for the projection-`eachRow`
rung, `Table(headerRows, eachRow)` (`Projection.cs:343-361`):

```
UnitProjection(
  headerRows == 0
    ? VerticalBands(1, eachRow).AsScaffolding()
    : UnderColumnLabels(ColumnLabels(1).AsScaffolding(), VerticalBands(1, eachRow).AsScaffolding())
        .AsScaffolding(),
  children: { eachRow }, "Table", TablePlacement())
```

where `UnderColumnLabels` is `VerticalFlow(flow => { var columns = flow.Next(header); return
flow.Next(WithColumnLabels(columns, body)); })` (`Projection.cs:368-381`) — the tiler, not
`VerticalRepeat(Record(record))`, is the body: nothing is searched for, a band is cut. The unit
owns the placement (`TablePlacement()` — the discovered block); the tiler underneath declares no
extent of its own, so the bound lives on exactly one node and streams through
`BoundedSpace.Tail`/`HasRow` rather than a repeat's discovered-occurrence search.

Sufficiency is judged including the **forcing profile** (rows materialised at first record, at
completion), not only value / message / A1 / `row.Index`. The other four rungs (`Table<T>()`, the
`LabelMap` bind, `Table()`, and the two lambda escape hatches) stay on the bespoke leaf
(`TableProjection`/`TableView`) — see the catalog's `Table` row, below.

## Next step (before the build) — catalog the primitives

Before building the GAP-A fix, inventory the primitives. For each building block a larger
projection composes from — the leaves, the layouts (`VerticalFlow`/`HorizontalFlow`/`Overlay`),
the repeats, the label/record primitives (`ColumnLabels`/`WithColumnLabels`/`Record`), the
matchers/`Caption`/`Fields` — record the properties that bear on composition: what it reads, its
default placement and extent, whether it streams or forces its bound, and how it composes with a
parent. That catalog is the map for making the primitives compose-and-stream (and for judging the
leaf/composite question below).

## The blocker and the fix — GAP A (streaming)

CLOSED (2026-09-13, commit `e561f36`) by the fix described below — then **SUPERSEDED (2026-09-14,
commit `6d64326`)** by a second fix, which is what `Table`'s composed rung uses today. The
2026-09-13 text stays as the record of what was tried first.

The blocker was: composing `Table` this way makes the flow a **composite**, and the engine resolved a
composite's discovered extent at first-child placement (`Exceeds` + `GetSubspace(offset)`) — the whole
block, up front. The bespoke leaf avoided that only because `TableView.StreamRows` walks a
`BoundedSpace` row-by-row via `HasRow`.

**2026-09-13 fix (fully superseded; the machinery is gone):** `VerticalRepeat` re-hosted the
discovered bound **inside the repeat** (`ProjectionEngine.BindArea`) and walked the body one band
past the cursor via `BoundedSpace.HasRow`, with an `onBlank` blank-band terminator over that bound.
The second fix put the bound on the composite's own placement instead, leaving the repeat's copy
with nothing to do, so `onBlank`, the self-bound walk and `BindArea` were all deleted (2026-09-14).
`VerticalRepeat` now simply walks the extent it is handed.

**2026-09-14 fix (what `Table`'s composed rung uses):** the bound moved off the repeat entirely and
onto the COMPOSITE's own placement — exactly where a `.Sized`/`TablePlacement()` already puts it for
a leaf. Two engine seams became bound-aware instead of forcing:

- `ProjectionEngine.Exceeds` (`ProjectionEngine.cs:335-337`) asks `BoundedSpace.HasRow`/`WidthOf`
  instead of reading `Area`.
- `FlowState.Next` (`FlowState.cs:64`) slices with `BoundedSpace.Tail(Extent, cursor)` — a lazy tail
  (`TailSpace.cs`) — instead of the `Area`-forcing `GetSubspace(offset)` extension.
- `RepeatProjection.TryCollect` (`RepeatProjection.cs:137`) does the same: it hands each attempt
  `BoundedSpace.Tail(extent, Step(cursor))` rather than a `GetSubspace`.

`ProjectionEngine.Bind` (`ProjectionEngine.cs:169-196`) is otherwise unchanged — it still binds a
`BoundedSpace` exactly when a placement declares a strict, incremental area — but that `BoundedSpace`
can now travel all the way down through a flow (or an overlay, or a repeat) to a leaf without anything
along the way forcing `Area`. The "composite forcing trap" this doc's catalog used to pin as the
deliberate limit is LIFTED; `ProjectionEngine.cs:159-167`'s rewritten paragraph states the new law: a
composite streams over a bound, and only a strategy that itself reads `ISpace.Area` inside a DECLARED
child area still settles it (Open question 5, below).

`BandsProjection` — `VerticalBands`/`HorizontalBands` — is the shape built to exploit this: it cuts a
real n-row (or n-column) band via `HasRow` + two-argument `GetSubspace` (`BandsProjection.cs:65-115`),
so its item is always placed in a measured space and never runs a strategy against a lazy tail. It is
what lets `Table`'s composed rung stream without a repeat's occurrence search at all: `UnitProjection`
carries `TablePlacement()`, wrapping a `VerticalFlow` of `ColumnLabels` + `WithColumnLabels(...,
VerticalBands(1, eachRow))` (`Projection.cs:343-361`; `headerRows == 0` drops the header flow and uses
the tiler alone).

Truth is in code + tests (`BandsProjection`, `UnitProjection`, `TailSpace`, `ProjectionEngine`,
`LabeledAxisPrimitivesTests.GapA_TheCompositeStreamsInStepWithTheLeaf`,
`LazyDenotationTests.ASizedLayoutCompositeStreamsItsBound`); this section is kept as the record of
what the blocker was and how it was actually closed.

## Open questions (decide before / during the build — do NOT pre-lock)

1. **Is "leaf" still a real distinction?** CLOSED (2026-09-14, uncommitted). `UnitProjection`
   (`src/Unrect/Projections/Composites/UnitProjection.cs`) is the generic wrapper: one node carrying a
   name, a declared child list and a placement over a composed body — the body reads, the unit
   supplies `Description`/`Children`/`Placement`/`Project`. `Table(headerRows, eachRow)` is built from
   it (`Projection.cs:343-361`). The other four rungs stay on the bespoke leaf, deferred rather than
   blocked: retiring them needs the same `UnitProjection` treatment applied one rung at a time.
2. **The repeat terminator's user-facing shape.** SUPERSEDED (2026-09-14). The 2026-09-12 answer was
   `onBlank: BlankRowStrategy` on the repeat, shipped in `e561f36` only so `Table` could be built on
   the repeat. `Table` is built on the tiler instead, and the tiler owns `onBlank` natively
   (`VerticalBands(1, item, onBlank:)`) — so the knob was removed from `VerticalRepeat`/
   `HorizontalRepeat`. A pattern-repeat's terminators are the ones it already had: the item stops
   placing, `.Until(landmark)` bounds the run (including `.Until(RowWhere(...))` for "stop at a blank
   row"), and `separatedBy: BlankRows()` skips blank bands between occurrences.
3. **GAP B — path / subject parity.** CLOSED (2026-09-14, uncommitted). `AsScaffolding()`
   (`ProjectionExtensions.cs:183-185`, backed by `ProjectionBase.IsUnitScaffolding`) marks a unit's
   internal chrome — the wrapping flow, `ColumnLabels`, the tiler (`WithColumnLabels` carries no
   mark: it is transparent when unnamed, which drops its segment the same way).
   `ProjectionContext.Collapse` (`ProjectionContext.cs:314-349`) folds only scaffolding-marked nodes
   into the nearest surviving segment, hoisting their occurrence index onto it, and applies the kind
   suffix unconditionally — so `Table(headerRows, eachRow)`'s failure paths are byte-identical to the
   leaf's: `Table[1] -> 'allocation' -> Decimal?#3 @ J5`. The naming law for a unit: a named use site
   renders `'name' (Table)`; an unnamed nth child renders `Table#n`; the unit itself carries no
   `.AsUnit` mark, so it climbs the ordinary naming ladder rather than the `.AsUnit` label ladder.
   Pinned by `ProjectionBuildersParityTests` (e.g. `Table[0] -> 'allocationRow' (Decimal)`) and
   `LabeledAxisPrimitivesTests`' rung-4 parity fixtures.
4. **Shared header/body width.** REVERSED (2026-09-14, uncommitted). The 2026-09-13 answer
   ("`ColumnLabels` discovers width via `TakeColumnsWhileAnyValue`") no longer holds:
   `ColumnLabelsProjection.Project` (`ColumnLabelsProjection.cs:27-34`) now takes
   `BoundedSpace.WidthOf(extent)` — the width of the band it is HANDED — and slices its own header
   rows out of that; it does not discover width itself. Width is the composite's job: the unit's
   placement (`TablePlacement()`) discovers the block once, and `WithColumnLabels`
   (`WithLabelsProjection.cs:37-50`) narrows the body to `Map.Labels.Count` only when the extent is
   wider, so header and body share one width because they are handed the same bound, not because two
   strategies agree on one. Ragged sheets (a body row wider than the header) remain out of scope, as
   before.
5. **The lazy seam into `Unrect.Strategies` (NEW, 2026-09-14 — OPEN / DEFERRED).** `ProjectionEngine`
   only avoids forcing a bound when the CHILD's own placement is derived (`Placement.Area is null`) or
   is itself bound; a child that DECLARES its own area still asks that area's strategy to answer, and a
   strategy in `Unrect.Strategies` answers by reading `ISpace.Area` — which forces a lazy tail. The
   engine says this plainly where the bound is built: "a shape that knows its own shape slices before
   it declares" (`ProjectionEngine.cs:162-166`). Concretely: a bare `VerticalRepeat(Record(record))`
   inside a bound still forces at the first occurrence, because `Record`'s placement is
   `Placement.Of(FullRow())` (`Projection.cs:582`) — a declared area — and the strategy behind
   `FullRow()` reads `Area.Width`. Closing this needs either an incremental form of that strategy
   family that a lazy tail can answer without forcing, or a rule letting a strategy handed a
   `BoundedSpace` ask `HasRow`/`WidthOf` instead of `Area`. Deferred: no real declaration has paid this
   cost yet, and the tiler sidesteps it entirely — a fixed stride never asks the extent anything.

## Already shipped this thread (truth is in code + tests; do not re-describe here)

- `SkipToFirstNonBlankCell` offset strategy — commit `5add5e9`.
- `Table onBlank` blank-row strategy (Stop/Skip/Fault/Tolerate/blankRecord) — commit `1cc6c5a`.
- Uniform offset law (a declared pipeline offset replaces the shape default; one `Steps.Offset` rule) —
  commit `b4838bc`.
- GAP A closed (first pass): streaming `VerticalRepeat` (HasRow walk) + `onBlank` terminator +
  `ColumnLabels`/`WithColumnLabels` shared-width thread; the primitives compose-and-stream —
  commit `e561f36`.
- GAP A closed (second pass, superseding the first for `Table`): the tiler
  (`VerticalBands`/`HorizontalBands`, `BandsProjection`), bound-aware engine seams (`Exceeds`,
  `FlowState.Next`, `RepeatProjection.ItemExtent`, `TailSpace`) so a discovered bound streams through
  a flow/overlay/repeat instead of forcing at first-child placement, `UnitProjection` (the generic
  named-wrapper-over-a-composed-body primitive), `AsScaffolding()` + the `ProjectionContext.Collapse`
  fold (GAP B), and the `Table(headerRows, eachRow)` repoint onto this composition — commit
  `6d64326`.
- `onBlank` removed from `VerticalRepeat`/`HorizontalRepeat`, with the repeat's self-bound walk
  (`ProjectionEngine.BindArea`, the blank-band branches, `RepeatProjection.ItemExtent`) — the first
  GAP-A pass's machinery, dead once the second pass put the bound on the composite's placement.
  `BlankRowStrategy` stays: it is the tiler's and the leaf `Table`'s.

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
  `TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue()` (`Projection.cs:968`) is therefore incremental;
  `ExplicitArea`, `FullRow`, and `ToEdgeBlock` (`AllRows()…`, `Projection.cs:975`) are not.
- **A bound streams forward and forces on a dimension query** — reading a cell/subspace advances the
  scan only as far as named; asking `Area`/`Height` reads it to exhaustion
  (`BoundedSpace.cs:68,71-99,134-141`). The views expose the free width and the forward `HasRow`
  probe internally (`BoundedSpace.cs:114-115,126-127`); a public `ISpace.Area` always forces.
- **The composite forcing trap (GAP A) is LIFTED (2026-09-14, uncommitted).** `Exceeds`
  (`ProjectionEngine.cs:335-337`) now asks `BoundedSpace.HasRow`/`WidthOf` instead of reading `Area`,
  and the composites that used to slice with the `Area`-forcing `GetSubspace(offset)` extension now
  slice with the lazy `BoundedSpace.Tail` (`FlowState.cs:64`; `RepeatProjection.ItemExtent`,
  `RepeatProjection.cs:271-274`) or hand the extent through unsliced (`OverlayState.cs:37`). A bound
  handed to a composite now streams as far down as nothing along the way reads `Area` — see the "What
  this buys, and where it stops" paragraph the engine itself carries (`ProjectionEngine.cs:159-167`),
  and Open question 5, above, for where it still doesn't.

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
| `Record` (`RecordProjection<T>`) | one body row → `T` via `TableRow` | `Placement.Of(FullRow())` (`Projection.cs:582`) — 1 row, full width | Force at end — reads `extent.Area.Size` after the lambda (`RecordProjection.cs:35`); band is 1 row | one full-width row band; resolves columns through ambient `LabelAxis.Column` scope | `TableRow` has no owning `TableView`; ordinal from `context.Ordinal` set by the enclosing repeat (`RecordProjection.cs:33-35`) |
| `Formula` (`Unrect.Spreadsheets`) | one cell's formula text → `string?` | `Range(1,1,…)` = `ExplicitArea(1,1)`, `.Named("Formula").Demanding(Formulas)` (`SpreadsheetProjections.cs:56-59`) | Force — fixed 1×1 | fixed 1×1 subspace | not a reach-through; demands `IFormulaSpace`; `.Named` so path reads `Formula` not `Range(1,1)` |
| `Nothing` / ε (`NothingProjection<T>`) | nothing → `default!` | `Placement.Default` (derive) (`NothingProjection.cs:26`) | Neither — consumes `Size(0,0)`, `Presence.Empty` (`:32-33`) | the layout-algebra unit; internal, not vocabulary | one shared `Instance` (`:23`) |

### Structure primitives (combinators)

Orchestrate children, produce structure. Each has child projections yet is irreducible — none can be
built from the other primitives — so each is a primitive, not a composite.

**Multi-child:**

| Name / factory (class) | Reads (T) | Default placement | Bound: defer/stream vs force | How it composes | Notes |
|---|---|---|---|---|---|
| `VerticalFlow`/`HorizontalFlow` (`FlowProjection<T>` → `FlowState`) | whatever the lambda builds → `T` | `Placement.Default` (derive; Area null) (`Projection.cs:49,57`) | Derives its extent, so the engine never binds it — handed the raw available space. **Streams a handed `BoundedSpace`** (2026-09-14): `FlowState.Next` slices with `BoundedSpace.Tail(Extent, cursor)` (`FlowState.cs:64`), a lazy tail (`TailSpace.cs`) that keeps an unsettled height unsettled — a child placed here forces only if its OWN placement does | hands each child a full-width band from the cursor, accumulating advances (`FlowState.cs:38-73`) | opaque to tooling — children exist only while the lambda runs (`LayoutProjection.cs:26-59`); "empty sibling" note (`FlowState.cs:66-88`); `Heading` desugars to a named vertical flow (`ProjectionBase.cs:147-162`) |
| `Overlay` (`OverlayProjection<T>` → `OverlayState`) | whatever the lambda builds → `T` | `Placement.Default` (derive) (`Projection.cs:81`) | Same as flow: not bound by the engine; hands every child the same extent unsliced (`OverlayState.cs:37`). `Exceeds` no longer reads `Area` (`ProjectionEngine.cs:335-337`), so a handed `BoundedSpace` streams here too — a child forces only if its own placement does | hands every child the whole extent + unadvanced context; children may overlap (`OverlayState.cs:30-44`) | consumed = bounding box of children (`OverlayState.cs:22-26`); opaque |
| `VerticalRepeat`/`HorizontalRepeat` (`RepeatProjection<T>`) | `IReadOnlyList<T>` | `Placement.Default` (derive) (`Projection.cs:746,758`) | **Streams** (2026-09-14): the walk holds no bound of its own. It hands each attempt a lazy `BoundedSpace.Tail(extent, Step(cursor))` (`RepeatProjection.cs:137`) and probes via `HasRow`/`WidthOf` (`:202-206`) rather than reading `Area`; the only `Area` read is a horizontal repeat's height (`:58-60`), which is its across axis. **Honest limit:** an item with its OWN declared, non-incremental area (e.g. `Record`'s `FullRow()`) is measured up front regardless — a repeat item's placement is never strict, so `ProjectionEngine.Bind` (which requires strict) never applies to it, and the item's area strategy runs and may read `Area` itself (Open question 5 in this doc's own questions section) | walks occurrences: separate → place → `TryApply` the item (item's placement is non-strict, never deferred) → collect (`:120-167`). Stops when the item's placement fails or consumes/advances zero (`:156-161`) | item labelled from its use site; `atLeast`, `separatedBy`; per-occurrence index/ordinal stamped on context (`:143`). No `onBlank` — the run is bounded by `.Until(landmark)` or by the item ceasing to place; a blank band between occurrences is `separatedBy: BlankRows()` |
| `VerticalBands`/`HorizontalBands` (`BandsProjection<T>`) | `IReadOnlyList<T>` | `Placement.Default` (derive) (`Projection.cs:926`) | **Streams**: cuts a REAL n-row/column band via `HasBand` (`BoundedSpace.HasRow`/`WidthOf`) then a two-argument `GetSubspace` (`BandsProjection.cs:65-115`) — the band handed to `each` is always a measured subspace, never a lazy tail, so `each` may declare its own area and read `Area` freely without forcing anything upstream | cuts the extent into fixed-stride bands top-to-bottom (or left-to-right), applying `each` to every whole band until a part-band is left; `onBlank` (vertical only) treats a fully-blank band as Stop/Skip/Fault/Tolerate (`:70-83,117-127`) | declares NO extent of its own — how far it runs is whoever places it (`.Sized`, a discovered block, or the raw handed space); no `separatedBy`/`atLeast`, no productivity guard (the stride is imposed, not discovered); this is the tiler underlying `Table(headerRows, eachRow)`'s body (`Projection.cs:354`) |
| `Choice` (`ChoiceProjection<T>`) | first matching alternative → `T` | `Placement.Default` (derive) (`Projection.cs:852`) | Passes the same `extent` to each alternative via `Apply`; forcing is whatever the winning alternative does | tries alternatives in order against the same extent, rolling back diagnostics of losers (`ChoiceProjection.cs:40-66`) | faults pass through (`:51`); no per-alternative name capture (params array) |
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
| **`Table`** — mixed: `TableProjection<T>` for four rungs, `UnitProjection<T>` for the fifth | header + body rows → `T` (via `TableView` on the leaf; via the composed rung's own `IProjection<T>`) | `TablePlacement()` = `SkipToFirstNonBlankCell` offset + `DiscoveredBlock()` (Stop) / `ToEdgeBlock()` (other onBlank) (`Projection.cs:959-975`) | **Streams**, by two mechanisms now. The leaf: `TableView.StreamRows`→`StreamBands` walks one row past the cursor via `BoundedSpace.HasRow`, never asks `Area` (`TableView.cs:111-140`, `TableProjection.cs:22-36`). The composed rung: its placement lives on the `UnitProjection`, and the `VerticalFlow`/`VerticalBands` beneath it are bound-aware (see their own rows above), so nothing forces the bound on the way down | `Table(headerRows, eachRow)` (`Projection.cs:343-361`) is **SHIPPED** as `UnitProjection` over `VerticalFlow(ColumnLabels; WithColumnLabels(VerticalBands(1, eachRow)))`, matching the leaf on values and forcing profile (`LabeledAxisPrimitivesTests.GapA_TheCompositeStreamsInStepWithTheLeaf`). The other four rungs (`Table<T>()`, the `LabelMap` bind, `Table()`, both lambda escape hatches) remain the bespoke leaf `TableProjection<T>` | The composed rung is **NOT a primitive** — it is `UnitProjection` + `VerticalFlow` + `ColumnLabels` + `WithColumnLabels` + `VerticalBands`, all primitives listed above. The leaf's `RowCount`/`Rows`/`Location` force (`TableView.cs:63,88,95`); `ColumnCount`+header free (`:56`) |
| `Fields` (a `FlowProjection` of `FieldProjection`s) | `IReadOnlyDictionary<string,CellValue>` | `FieldsPlacement(firstLabel)` — anchors on first field's label (`Projection.cs:649`) | Composite (vertical flow) — forcing per the `VerticalFlow` row | a `VerticalFlow` of `Field` children | opaque vertical flow; each field one 2×1 band; children built once at construction (`:631-634`); keyed by declared labels |

### Synthesis — what streams, what forces, and what it means for GAP A

> Pre-fix analysis, kept as the reasoning that led to the 2026-09-13 fix (commit `e561f36`). GAP A was
> closed again, differently, on 2026-09-14 (uncommitted — commit: pending): the fix that shipped is
> not "the repeat re-hosts its own bound," it is "the engine's placement seams (`Exceeds`,
> `FlowState.Next`, `RepeatProjection.ItemExtent`) stopped reading `Area`," so a discovered bound now
> streams through a flow, an overlay, or a bare repeat alike, and the tiler
> (`VerticalBands`/`HorizontalBands`) is the shape that carries the bound for `Table`'s composed rung.
> The bullets below describe the PRIOR state and the reasoning that made the 2026-09-13 fix look
> sufficient; they are kept because the reasoning is still sound, not because the mechanism they
> describe is still current. Read "The blocker and the fix," above, for what actually shipped.
>
> Measured parity on the trailing-content fixture (`LabeledAxisPrimitivesTests.Trailing`): composed
> and leaf touch the same row count at the first record and the same total at completion
> (`GapA_TheCompositeStreamsInStepWithTheLeaf`). A sparse `.Sized(RowsWhileAnyValue())` fixture yields
> `11x1` bands from the tiler — the leaf's own width, discovered once and handed down rather than
> rediscovered. `StreamingIdentityTests` (window of 4 chunks) reports zero chunk reloads over the
> composed reading.


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
