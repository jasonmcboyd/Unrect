# Step 1 implementation spec — ambient label environment on `ProjectionContext`

**Status:** IMPLEMENTATION SPEC (2026-09-11, `experiment/algebra-foundations`). Produced by the
architecture pass for step 1 of `labeled-axes-and-context.md`. **`file:line` references are as-of
this writing and may drift** — treat them as starting points, verify before editing.

Step 1 = reroute the compute-legal binder's caption resolution from a `TableView`-local map to an
**ambient label environment on `ProjectionContext`**, translating each ordinal from its capture frame
to the reading frame and bounds-checking (departed label → clean `MissingLabel`, never a wrong-cell
read). Built-in `Table` stays byte-identical (identity translation); new paths fire only under offset
scopes (step 2) and the seam pin.

## 1. `ProjectionContext` additions
- Internal `enum LabelAxis { Column, Row }`.
- Internal `interface ILabelSource { IReadOnlyList<string> Labels { get; } IReadOnlyList<int> IndicesOf(string label); }` — `TableView` implements it now; step 2's public `LabelMap` will too (forward-compat, so the context shape never changes for step 2).
- Internal `sealed class LabelScope(LabelAxis axis, ILabelSource source, Offset captureOrigin, LabelScope? outer)` — an immutable cons-cell. `CaptureOrigin` = `context.Origin` at push.
- New copied field `LabelScope? Labels` + 9th ctor param, forwarded unchanged through `Root`(→null), `Descend`, `Advance`, `WithUseSite`, `WithIndex`.
- `PushLabels(LabelAxis, ILabelSource)` → new context with `new LabelScope(axis, source, Origin, Labels)`.
- `NearestLabels(LabelAxis)` → walk `Labels`→`Outer`, first matching axis wins (shadowing; different-axis skipped so axes coexist).

## 2. Axis by orientation (not guessed)
`TableRow` wraps a `Horizontal` `CellStrip` (a row) → single-label lookup is a **Column** label. A future vertical record passes `Row`. Column labels translate along `Offset.Width`; row along `Offset.Height`.

## 3. Push + resolve + translate
- **Push** in the `TableView` ctor, after `ColumnNames` is set: `Context = HasHeader ? context.PushLabels(LabelAxis.Column, this) : context;` (`TableView` implements `ILabelSource` via existing `ColumnNames`/`IndicesOf`). Header strip and `Location`/`Failure` keep the pre-push origin — unchanged.
- **Translate**: `local_column = ordinal + scope.CaptureOrigin.Width - Context.Origin.Width`. Identity in built-in `Table` (row band shares the table's `Origin.Width`).
- **Rewrite `TableRow.Resolvable(columnName)`** (the single funnel for typed accessors, `this[string]`, `AddressOf`, `TryGet`):
  1. `scope = Context.NearestLabels(Column)`; null → **headerless message verbatim**.
  2. `ordinals = scope.Source.IndicesOf(columnName)` — **keep `CellMatching.TextComparer`** (do NOT route through `CaptionComparer`); `>1` → existing ambiguity message; `0` → return empty (`Resolve` throws "no column named …", `TryGet` false).
  3. Translate `ordinals[0]`; if `<0 || >=Count` → `throw Failure($"column '{columnName}' is not in this region")` — a plain absorbable `ProjectionException`, **not** `OutOfBoundsException`, `IsFault` false.
  4. `Resolve`'s available-columns list reads `scope.Source.Labels` (identical values in step 1).
- **Lazy-safe**: reads `Context.Origin` (free) and `Count` (row width, settled); forces no extent the old `Table.IndicesOf` path did not; `BoundedSpace` height untouched.

## 4. Engine seam — VERIFIED
Child contexts are minted only at `ProjectionEngine` `Advance`/`Descend` (field-copying → `Labels` propagates). `Root` is called only at the `Map`/`Apply` entry points, never mid-tree. Every in-tree move (`FlowState`, `LayoutState`, `PadProjection`, `RepeatProjection`, `TableView.StreamBands`, `CellBlock`) is a field-copying factory. The `Table` band path (`TableProjection.Project` → `ProjectBands` → `Advance`/`WithIndex`/`WithUseSite` → `ProjectionEngine.Apply`) carries the pushed scope to the leaf unbroken. **No engine change required** — only the ctor field-forwarding.

## 5. Rename `CaptionMap` → `LabelMap` (breaking, alpha-fine)
Rename the public type + `eachRow` `captions =>`→`labels =>`. **Keep** the `Caption(text)` leaf; **leave `CaptionComparer`** (deferred). Production: `Views/CaptionMap.cs` (→`LabelMap.cs`, type, ctor, `Captions`→`Labels` optional), `Projection.cs` (`BoundRow` sig + `new CaptionMap`, docs), `ProjectionBuilders.cs`, `ProjectionScope.cs`, `Projection.Typed.cs` (incl. `Adapt`), `Pipeline/PlacementStage.cs`, `Pipeline/PlacementStage.Scoped.cs`. Tests: `ProjectionBuildersParityTests`, `TableProjectionTests`, `PlacementPipelineLawTests`, `ProjectionScopeTests`, `BoundRowTableTests` (incl. the `Func<CaptionMap,…>` cast), `CrossDoorDenotationTests`. `TableView` also gains `ILabelSource` (new, not a rename).

## 6. Test pins
1. **CRITICAL — translation applied (column-offset).** Push a Column scope at `Origin.Width == captureX` with `IndicesOf("Amount")==[ordinal]`, `captureX+ordinal==c`; resolve from a context `Advance`d to `captureX+Δ` (Δ≠0). Assert local col `== ordinal−Δ` reads absolute `c`; assert dropping the `CaptureOrigin.Width−Origin.Width` term reads the neighbor. The guard the model rests on.
2. **Departed label → `MissingLabel`.** Message `column 'X' is not in this region`, has path+A1, not `OutOfBoundsException`, absorbable by `.Optional()`/`.Else()`, `IsFault` false.
3. **Byte-identical sweep.** `BoundRowTableTests`, `TableProjectionTests`, `CellReadingIdentityTests` green post-rename; absent/ambiguous/headerless/`column 'Amount': …` messages identical.
4. **Engine seam.** `Table` with a nested layout in `eachRow` reading `row.Decimal("caption")` one composite deep — caption still resolves.
5. **`headerRows: 0` still headerless** — no scope pushed ≡ headerless message.

## 7. Step-1/step-2 boundary (flagged)
- Push API `PushLabels(LabelAxis, ILabelSource)` is what step-2 `WithColumnLabels(LabelMap, body)` calls once public `LabelMap : ILabelSource` exists. No context change then.
- Comparer unification deferred: step 1 keeps two rules (`TableRow`/`ILabelSource` on `TextComparer`; bind `LabelMap` on `CaptionComparer`). Settle when `ColumnLabels` mints the single shared public `LabelMap` (the `CaptionComparer`→`LabelComparer` call).
- Replace-not-merge / cross-axis coexistence is expressible via `NearestLabels` but not exercised in step 1 (nothing nests a second scope). Headerless-inner-inheriting-outer is a step-2 semantic; push-only-when-`HasHeader` preserves today's headerless message.
- `ProjectionContext`'s 4th job nudges the parked `PathRenderer` split sooner — record, not required now.
