# Step 2 implementation spec — scope-introducer primitives + `Table()` reimplementation (acceptance test)

**Status:** IMPLEMENTATION SPEC (2026-09-11, `experiment/algebra-foundations`), builds on
`labeled-axes-step1-spec.md` (shipped, `698dd21`). `file:line` verified at writing; may drift.
Bespoke `Table` stays untouched — step 2 proves reimplementation is possible and characterizes
exactly where it is / isn't byte-identical. The actual bespoke→primitives refactor is step 3.

Step-1 seam intact: `ProjectionContext.PushLabels(LabelAxis, ILabelSource)` / `NearestLabels`,
`LabelScope` (captured origin), `ILabelSource`, `TableView : ILabelSource`, and
`TableRow.Resolvable` already resolving through `NearestLabels(Column)` + translate + bounds-check.
**`TableRow`'s only remaining coupling to `TableView` is one dead fallback** (unreachable — `Resolvable`
throws headerless first), so extraction is nearly free.

## 1. `LabelMap : ILabelSource` — two rules, no conflict
The two comparers live in different methods, so one type carries both:
- **Bind-rung path unchanged:** `this[string]`/`Has`/`Matches`/`Ambiguous` keep `CaptionComparer` and cite header cells via the `TableView`.
- **Primitive path (new):** implement `ILabelSource` **explicitly** — `Labels` == existing public `Labels`; `IndicesOf` delegates to the source's `TextComparer` `IndicesOf` (byte-identical with pre-step-1 `TableRow`).
- **Backing split:** replace `TableView _table` with `ILabelSource _source` (always present, TextComparer + Labels) + `IHeaderCitations? _header` (bind-rung addresses/Failure; null for literals). Existing ctor `LabelMap(TableView)` sets both; a private `LabelMap(ILabelSource)` serves `ColumnLabels`/`Of`. Byte-identical for every existing test.

## 2. `ColumnLabels(int headerRows=1) : IProjection<LabelMap>` + `LabelMap.Of`
- Placement: `headerRows` × full width, offset 0. Reuse the header parser: `new TableView(extent, headerRows, context)` → `new LabelMap(view)` — byte-identical labels/ordinals/comparer by construction. (Harmless: the throwaway `TableView` pushes a scope onto its discarded context; `ColumnLabels` never streams bands, so it's dropped.) Consumes the header band.
- Require `headerRows == 1` for now. `LabelMap.Of(params (string,int)[])` backed by an internal `LiteralLabels : ILabelSource` (TextComparer) for the headerless-known-layout case. `RowLabels` twin deferred.

## 3. `WithColumnLabels(LabelMap map, IProjection<T> body) : IProjection<T>`
Transparent single-child wrapper modeled on `MapProjection`: `Placement.Default` (forces nothing),
`IsTransparent => Name is null`, `Project` = `Engine.Apply(body, extent, context.PushLabels(Column, map))`
forwarding the body's value/advance/presence. Capture frame = the body's frame (engine `Advance`s the
transparent wrapper), so column translation is the identity in the canonical `Table` composition
(`ColumnLabels` and `WithColumnLabels` are siblings of the same `VerticalFlow` at the same column
origin). **Frame-agreement invariant** (pin it): the map's ordinals must be frame-relative to an origin
sharing the labeled axis's coordinate with the push frame — holds by construction for columns; the sharp
edge when `RowLabels` (Y-translation) arrives.

## 4. `Record(Func<TableRow,T> record) : IProjection<T>` — the extraction (crux)
- **Decouple `TableRow`:** ctor param → `TableView? table`; remove the dead fallback (`?? Table.ColumnNames` → `?? Array.Empty<string>()`, unreachable ⇒ byte-identical); `Table` property → nullable, read by nothing else.
- **`Record` projection:** placement `FullRow()` (1 row × all columns) — exactly bespoke's `StreamBands(1)` band; under `VerticalRepeat` each occupies one row, repeat stops when `TakeRows(1)` fails past the last row. `Project` builds `new CellStrip(extent, Horizontal, context)` + `new TableRow(null, 0, strip, context)` and calls `_record(row)`. **Reuse not duplication:** resolution flows through the ambient `NearestLabels(Column)` scope `WithColumnLabels` pushed — zero resolution code copied, so `Record` and bespoke share the one `Resolvable`/`Convert` path and cannot drift. Diagnostics (`column 'X': …` + A1 at the band origin) byte-identical.

## 5. Acceptance / differential test
Reimplementation via an internal test helper `TableFromPrimitives<T>` that builds the body from the
**public** primitives and attaches bespoke's `TablePlacement()` + a `"Table"` description via internal
`FlowProjection` (legitimate — precisely step 3's rebuild):
```
new FlowProjection<IReadOnlyList<T>>(Vertical, flow => {
    var cols = flow.Next(ColumnLabels(headerRows));
    return flow.Next(WithColumnLabels(cols, VerticalRepeat(Record(record))));
}, TablePlacement(), "Table");
```
Run both bespoke `Table` and `TableFromPrimitives` through `MapWithDiagnostics` over the sheet set and
**assert byte-identical: cell VALUES, failure MESSAGE (`Problem`), A1 LOCATION** (via the `Observations.cs`
L3 comparator restricted to Problem+Location). **Capture/report but do NOT assert equal: PATH/SUBJECT and
FORCING** (the documented divergences → GAP B, GAP A).
Sheet set: (1) flat table [laziness preserved], (2) absent column, (3) ambiguous column, (4) offset
placement [column origin > 0], (5) nested/repeated [per-occurrence header, scope under an outer repeat],
(6) trailing content [forces the discovered block → GAP A], (7) kind-mismatch body cell.
Pins: WithColumnLabels extent-transparency; ColumnLabels byte-identity; the LabelMap comparer split;
Record standalone reuse; frame-agreement/translation (step-1 mutation guard still fails); LabelMap.Of;
the differential itself across the set.

## 6. Completeness gaps (named missing primitives, per the discipline)
- **GAP A — lazy/streaming (the real one).** The reimplementation puts `DiscoveredBlock()` on the
  `VerticalFlow`, a **composite**, which the engine forces at first-child placement. Bespoke
  `TableProjection` is a **leaf** that streams via `StreamBands`/`BoundedSpace.HasRow` and never forces up
  front. ⇒ values/messages/A1 identical, but forcing counts / peak memory differ on sheets that need the
  bound (sheet 6); where the table fills the sheet, laziness is preserved. **Missing primitive:** a
  bound-aware (lazy) composite placement, OR a lazy repeat terminator ("repeat one-row items while the
  next row carries a value") that discovers the block row-by-row without a composite-level area. **This
  gates step 3's refactor** — bespoke `Table` can't be replaced without a streaming regression until this
  exists.

  **Fix direction (owner, 2026-09-11):** the terminator is the right form, expressed as a
  projection-native streaming primitive — "everything participates in the engine" applied to the *stop*
  rather than to discovery (`ColumnLabels` already is a projection, so discovery is fine; the composite
  *area* is the one non-projection-native move that forces). Step 3 opens by building this.

  **Laziness is geometric (the cost-model characterization the owner named).** The two axes are symmetric
  in the *algebra* but not in the *streaming cost*, because of where the labels live:
  - A **top header** (column labels) spans the *width*: reading it forces one row of columns (cheap) and
    the body streams down the *height* (the expensive dimension for the streaming door). **Streams well.**
  - A **left/right header** (row labels) spans the *height*: to collect the row-labels at all you must read
    *every row* of that column, forcing the full height before anything can be addressed. **Eager in
    height by necessity**, then streams the *width*.
  So a column-labeled table streams the expensive dimension; a row-labeled table forces it. Not a bug — a
  consequence of geometry, and the deeper reason `RowLabels` was right to defer (it is the eager-height
  case, not merely untested).

  **The cost is the substrate's, not the algebra's (owner, 2026-09-11).** A row-major reader must buffer
  to do column-wise work — the oldest fact in parsing, not a Unrect tax. The algebra marks both axes
  precisely so it "never encodes one substrate's dominant axis as normal": the mirror law makes the
  library substrate-adaptive, so whichever table matches the substrate's major read axis is the cheap one
  and its twin buffers. `VerticalTable` is the hero on the row-major streaming door; a **column-major**
  substrate (a columnar format, or a transposed/column-windowed reader) would make `HorizontalTable` the
  hero with zero algebra changes. So the `RowLabels`/`HorizontalTable` deferral is a cost-sequencing call
  for the current substrate, NOT a demotion — when built they are peers, inherent buffering and all.
- **GAP B — path/subject fidelity.** Reimpl `Path`/`Subject` reflect the primitive tree
  (`VerticalRepeat[i]`, subject `Record`) vs bespoke's flat `Table`. Message + A1 identical. Partly
  closeable (FlowProjection description = `Table`, WithColumnLabels transparent); full parity is a
  path-presentation capability — a step-3 design decision, not a pure primitive.
- **GAP C — `row.Index` — FIXED IN STEP 2 (owner, 2026-09-11).** Standalone `Record` can't recover the
  body-row ordinal because the repeat's occurrence index is consumed by the repeat's own segment and
  `context.Index` (the path-rendering index) is nulled on `Descend` into the item. **The fix — a
  repeat-scoped occurrence ordinal on the context, exactly analogous to step 1's `Labels` field:**
  - Add a copied field `int? Ordinal` to `ProjectionContext`, forwarded through every factory (`Root`→null,
    `Descend`, `Advance`, `WithUseSite`, `WithIndex`) — so it *persists* into the item's whole subtree
    (unlike the path `Index`, which nulls on `Descend`). Add `WithOrdinal(int)`.
  - `RepeatProjection` sets it via `WithOrdinal(i)` when applying occurrence `i` (both axes' repeats). A
    nested repeat overwrites it — **nearest enclosing repeat wins**. It is DISTINCT from the path `Index`,
    so path rendering (`VerticalRepeat[2]`, `Cell#2`) is **unchanged**.
  - `Record.Project` reads `context.Ordinal ?? 0` and stamps `new TableRow(null, ordinal, strip, context)`,
    so `row.Index` returns the occurrence. Bespoke is unaffected (it uses its own `StreamRows` counter).
  - **Pins:** `VerticalRepeat(Record(row => row.Index))` yields `0,1,2,…`; a nested repeat's inner
    `row.Index` is the inner occurrence; path rendering is byte-identical; and the §5 differential now
    ASSERTS `row.Index` parity between bespoke and reimplemented `Table` (the "keep `row.Index` out of
    acceptance lambdas" caveat is lifted).

Of the three, **GAP C is closed in step 2**; **GAP A and GAP B remain for step 3** (GAP A is the real
blocker — the lazy streaming terminator above; GAP B is a path-presentation decision). The step-2
acceptance claim (values + message + A1 byte-identical) holds, now with `row.Index` parity added.

## 7. Build surface
New public: `Projection.ColumnLabels(int)`, `Projection.Record<T>(…)`, `Projection.WithColumnLabels<T>(…)`,
`LabelMap.Of(…)`. `LabelMap` gets `ILabelSource` (explicit) + backing split + `Of`/`LiteralLabels`/`IHeaderCitations`.
`TableRow` nullable-TableView + dead-fallback removal. New files: `ColumnLabelsProjection.cs`,
`RecordProjection.cs`, `WithLabelsProjection.cs`; test helper `TableFromPrimitives`. Coordinate the
`ProjectionBuilders`/`ProjectionScope` re-export covenant (reflection parity pin may require the new
factories). Do NOT alter bespoke `Table`/`TablePlacement`/`TableProjection`/`ProjectBands`/`CaptionComparer`.
