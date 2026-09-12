# Step 3a implementation spec — the lazy streaming terminator (closes GAP A)

**Status:** SUPERSEDED (2026-09-12) by `table-extent-and-blank-rows.md`. The `within:`/lazy-terminator
approach here was outgrown: **`Table` stays a LEAF** (it streams, so GAP A is moot — GAP A was an
artifact of turning `Table` into a *composite*), and the extent/blank/offset design moved to node-type
placement defaults + `onBlank`. Kept as the record of the GAP-A *analysis* (why composites force their
bound at first-child placement; `StreamBands`-as-a-combinator). Original status: IMPLEMENTATION SPEC
(2026-09-11), closes GAP A from `labeled-axes-step2-spec.md §6` — the
last blocker for the bespoke→primitives `Table` refactor. `file:line` verified at writing; may drift.

## Problem (precise)
Bespoke `Table` is a streaming leaf: `TablePlacement()` = `SkipBlankRows()` offset + `DiscoveredBlock()`
area (`TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue()`); the engine hands it a `BoundedSpace`, and
`TableView.StreamBands` walks it via `BoundedSpace.HasRow(space, cursor)` — one row past the cursor,
never measuring the block (QA: 2 rows at first record). The naive reimplementation puts
`DiscoveredBlock()` on the **VerticalFlow** (a composite), which the engine resolves at first-child
placement (`Exceeds` + `GetSubspace(offset)`), running the area scan across all block rows up front
(QA: 4 rows). Same extent, wrong timing — GAP A. `VerticalRepeat` can't substitute today: it stops on
`IsEmpty(remaining)` which reads `Area` (forces a `BoundedSpace`), and with no bound it walks *into*
blank rows (a blank row is a valid 1×W band).

## Recommendation: a repeat-terminator, NO engine change
Add a `VerticalRepeat` mode that **re-hosts the block bound inside the repeat** — the repeat becomes
the algebra-combinator equivalent of `TableView.StreamBands`, walking a `BoundedSpace` via `HasRow`.
Lightest thing that works, declarative (the stop *is* the `TakeRowsWhileAnyValue` strategy), reuses the
exact machinery bespoke uses, zero engine code.

### Signature
```csharp
public static IProjection<IReadOnlyList<T>> VerticalRepeat<T>(
    IProjection<T> item,
    IAreaStrategy within,                    // the block the items fill, discovered lazily
    int atLeast = 0,
    [CallerArgumentExpression("item")] string? declared = null);
```
(+ `<TSpace>` re-exports per the completeness covenant; `HorizontalRepeat` twin deferred with `RowLabels`.)
Canonical spelling: `VerticalRepeat(Record(record), within: RowStrategies.TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue())`.
The rebuilt `Table` keeps `SkipBlankRows()` as the flow's leading offset (flow forces nothing — no
discovered area on the composite) and moves `DiscoveredBlock()` off the flow into the repeat's `within:`.

### Lazy continuation (streaming door) — mirrors StreamBands
In `within:` mode `RepeatProjection`: (1) begin the bound once — `new BoundedSpace(extent, ((IIncrementalAreaStrategy)within).BeginArea(extent), failure)`, `failure` mirroring `ProjectionEngine.AreaFailure` so a broken scan is classified correctly; (2) width free via `BoundedSpace.WidthOf`; (3) per occurrence at `cursor`, continuation = `BoundedSpace.HasRow(bounded, cursor)` (reads only row `cursor`'s cells through the windowed reader — one row past, first `false` = first blank = stop); (4) place the item over `bounded.GetSubspace(new Offset(0, cursor), new Area(width, 1))` (validates+slices, no forcing) — NOT the offset-only `GetSubspace(Step(cursor))` which reads `Area` and forces; (5) advance cursor by consumed height (1 for `Record`); keep `atLeast`/`WithIndex`/`WithOrdinal`/`WithUseSite`/diagnostics/presence unchanged. Reads the contiguous non-blank rows + one (the blank it stops on).

### Separator vs within — mutually exclusive overloads, no conflict
`separatedBy:` mode (existing): items separated by gaps, blank band is a SEPARATOR, stops off the
*measured* extent. `within:` mode (new): items CONTIGUOUS, fill a lazily-discovered block, stops off a
`BoundedSpace` via `HasRow`. A blank row in `within:` mode is the *edge of the discovered extent*, not a
terminator signal — so "a blank band is a separator, never a terminator" (which governs the separated
mode) is untouched. Guard so both cannot be supplied together.

### Agreement by construction
`within`'s per-row predicate is `TakeRowsWhileAnyValue()` = `v => v.HasValue` (complement of `IsBlank`) —
the SAME strategy object `DiscoveredBlock()` is built from, consumed through the same `IAreaScan.IncludesRow`
and `BoundedSpace`. So the reimplemented table stops at exactly the row bespoke's `DiscoveredBlock` stops
at — it *is* the same scan, not a matching predicate.

### Why not make the composite lazy
A flow slices its extent per child and resolves a composite's declared area at first-child placement;
making that lazy means threading a bound through `FlowState`/`LayoutState` band arithmetic and deferring
the flow's area across all children (and `Overlay`) — a broad engine change, explicitly parked in
CLAUDE.md. The repeat is already a row-by-row walker, not a slicer, so re-hosting the bound is a local
`RepeatProjection` change reusing `BoundedSpace`/`IAreaScan`/`HasRow`/`WidthOf`/`GetSubspace` — StreamBands
as a combinator. GAP A is closable cleanly without engine surgery.

### Rebuild detail (not a blocker): share one width
Bespoke discovers ONE width for header + body (its `DiscoveredBlock` scan spans the header row). The
split composition should thread ONE `BoundedSpace` over the whole placed extent, shared by the
header-consume (`ColumnLabels`) and the body walk — a structural choice in the step-3 rebuild, not a new
primitive — to be byte-identical on ragged/trailing-blank-column sheets and to avoid a header ordinal
exceeding a narrower body width (a spurious step-1 `MissingLabel`). Pin: a trailing-all-blank-column
sheet reads byte-identical `row.Count` / `LabelMap.Labels.Count` between bespoke and reimplemented Table.

## Acceptance pins
1. GAP A closed: rows materialised at first-record projection == bespoke == 2 (naive == 4 is the removed regression); rows at completion == 4 both (timing fix, not extent).
2. WHAT is read unchanged: the step-2 differential still byte-identical (values + message + A1 + row.Index).
3. Cross-door + forcing counts (`LazyForcingTests`-style) match bespoke; `CrossDoorDenotationTests` L3 identical; eager-vs-lazy identity for the `within:` repeat.
4. Mode isolation: `separatedBy:` suite unchanged; `within:`+`separatedBy:` cannot combine; interior-blank stops at the blank; extent-edge stops via `HasRow` without forcing.

## Builds on
`RepeatProjection.cs` (termination `:103-144`, ctor `:14-29`); `BoundedSpace.cs` (ctor `:39`, `GetSubspace :85`, `WidthOf :114`, `HasRow :126`); `IRowScan.IncludesRow`, `IAreaScan.Width`, `IIncrementalAreaStrategy.BeginArea`; `TableView.StreamBands :117-125`; `RowStrategies.TakeRowsWhileAnyValue :39-40`; `DiscoveredBlock()`/`TablePlacement()` `Projection.cs:693-695`; `ProjectionEngine.AreaFailure :256`.
