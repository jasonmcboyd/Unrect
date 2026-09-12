# Implementation spec: the `onBlank` blank-row strategy on the leaf `Table`

**Status:** IMPLEMENTATION SPEC (2026-09-12). Maps the settled design in
`docs/design/table-extent-and-blank-rows.md` onto the current code. This is not a redesign; the
user-facing surface, the presets, the run-to-edge ruling and the "lazy on the walker, no engine
change" constraint are all DECIDED there. Everything below is additive and gated on the default
(`Stop`) path staying byte-identical to today, so all 2,073 tests stay green.

All citations are `file:line` against the branch `experiment/record-primitive`.

---

## 0. The one-paragraph summary

`Table` bakes its body extent into its placement today: `TablePlacement()`
(`src/Unrect/Projections/Projection.cs:750`) pairs a `SkipBlankRows()` offset with
`DiscoveredBlock()` (`:752`) = `RowStrategies.TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue()`,
which stops the row scan at the first fully-blank row. That row-stop IS the `Stop` policy. To add
the other four policies we (a) let `onBlank` pick the body's **area strategy** — `DiscoveredBlock()`
for `Stop`, an `AllRows()`-based run-to-edge block for the rest — and (b) apply the per-row policy on
the **walker** (`TableView.StreamRows`/`StreamBands`), where a blank row is peeked one at a time.
No engine change, no composite bound; the deferral machinery already makes both area strategies
lazy because both are `IIncrementalAreaStrategy`.

---

## 1. API surface added

### 1.1 The `BlankRowStrategy` value

A public `readonly struct` in a new file `src/Unrect/Projections/BlankRowStrategy.cs`, namespace
`Unrect.Projections`. It carries **two of the three axes** — Control and Diagnostic. The third axis
(Value: skip vs project) is expressed by *which overload the caller reaches*, not by a field on the
struct (see §1.3 for why).

```csharp
namespace Unrect.Projections
{
  /// <summary>
  /// How the body of a Table treats a fully-blank row (every cell IsBlank). The five presets are
  /// named points in a three-axis space; this value carries the Control and Diagnostic axes, while
  /// the Value axis (skip a blank vs project one to a record) is the difference between the onBlank:
  /// overloads and the blankRecord: overloads.
  /// </summary>
  public readonly struct BlankRowStrategy
  {
    private BlankRowStrategy(bool continues, DiagnosticSeverity? diagnostic, bool isFault)
    { Continues = continues; Diagnostic = diagnostic; IsFault = isFault; }

    // Control axis
    internal bool Continues { get; }          // false == stop
    // Diagnostic axis
    internal DiagnosticSeverity? Diagnostic { get; }  // null == none; Info == Tolerate
    internal bool IsFault { get; }            // terminal error

    /// <summary>Stop at the first blank row (the default, self-bounding — today's behaviour exactly).</summary>
    public static BlankRowStrategy Stop => default;   // MUST be default(struct): the optional-param default is Stop

    /// <summary>Skip a blank row and keep reading; produce no record for it.</summary>
    public static BlankRowStrategy Skip => new BlankRowStrategy(true, null, false);

    /// <summary>Stop at a blank row and fail terminally — a blank row is malformed data.</summary>
    public static BlankRowStrategy Fault => new BlankRowStrategy(false, null, true);

    /// <summary>Skip a blank row, keep reading, and record a nonterminal Info for it.</summary>
    public static BlankRowStrategy Tolerate => new BlankRowStrategy(true, DiagnosticSeverity.Info, false);

    /// <summary>The default: stop, no record, no diagnostic. default(struct) is this.</summary>
    internal bool IsStop => !Continues && Diagnostic is null && !IsFault;
  }
}
```

The single most important property: **`default(BlankRowStrategy) == Stop`**. Every new overload takes
`BlankRowStrategy onBlank = default`, so a caller who omits it gets `Stop`, and `Stop` routes to the
existing, untouched code path (§4). `DiagnosticSeverity` already lives in `Unrect.Projections`
(used at `ProjectionContext.cs:199`).

### 1.2 New `Table` overloads on `Projection` (`src/Unrect/Projections/Projection.cs`)

Add **six** new public static overloads. Do **not** modify the signatures or bodies of the existing
`Table` overloads — add new ones so existing call sites bind exactly as they do now.

The `onBlank:` overloads (Stop/Skip/Fault/Tolerate), one per row-projecting rung named in the task:

```csharp
// 1. beside Projection.cs:381  (Func<TableRow,T> rung, 1 header row)
public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project, BlankRowStrategy onBlank)
    => Table(1, project, onBlank);

// 2. beside Projection.cs:387  (Func<TableRow,T> rung, explicit header rows)
public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project, BlankRowStrategy onBlank);

// 3. beside Projection.cs:179  (Table<T>() reflection bind)
public static IProjection<IReadOnlyList<T>> Table<T>(BlankRowStrategy onBlank) => TypedRows<T>(null, onBlank);

// 4. beside Projection.cs:190  (Table<T>(bind) reflection bind)
public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind, BlankRowStrategy onBlank);
```

The `blankRecord:` overloads (the **Project** preset), only on the `Func<TableRow,T>` rungs — see
§1.3 for why Project is confined to these:

```csharp
// 5. beside Projection.cs:381
public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project, Func<TableRow, T> blankRecord)
    => Table(1, project, blankRecord);

// 6. beside Projection.cs:387
public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project, Func<TableRow, T> blankRecord);
```

Overload-collision check (done against the current set at `Projection.cs:179–412`): items 5/6 have a
second `Func<TableRow,T>` parameter, distinct from item 1/2's `BlankRowStrategy`, so `Table/2` and
`Table/3` each gain two members that differ by parameter type — legal, and unambiguous at call sites
because the argument is either a `BlankRowStrategy` value or a lambda. Item 3 (`Table<T>(BlankRowStrategy)`,
one param) does not collide with `Table<T>()` (zero) or `Table<T>(Func<TableRow,T>)` (a delegate).
Item 4 differs from every other two-param `Table` by its first parameter type.

**Call sites read exactly as the design doc's examples:**
```csharp
Table((TableRow r) => new Tx(r.Text("Investor"), r.Decimal("Amount")))                     // Stop, unchanged
Until("GrandTotal").Table((TableRow r) => new Tx(...), onBlank: Skip)                       // item 1
Until("GrandTotal").Table((TableRow r) => new Tx(...), onBlank: Fault)                      // item 1
Until("GrandTotal").Table((TableRow r) => new Tx(...), onBlank: Tolerate)                   // item 1
Until("GrandTotal").Table((TableRow r) => new Tx(...), blankRecord: b => new Tx.Spacer(b.Index)) // item 1 + item 5
Table<Allocation>(onBlank: Skip)                                                            // item 3
```

### 1.3 The Project overload decision: `blankRecord:` separate overload, NOT `onBlank: Project(...)`

The task asks which of the two spellings the code actually supports. **Recommend `blankRecord:`, a
separate overload.** Concrete reasoning against `onBlank: Project((TableRow b) => …)`:

- The blank record has the **record type `T`**: skip *omits* an entry, project *includes a `T`* (the
  doc is explicit these differ). So Project must carry a `Func<TableRow, T>`.
- To assign `Project(lambda)` to a single `onBlank` parameter, that parameter's type would have to be
  generic — `BlankRowStrategy<T>` — and then `onBlank: Skip` needs `Skip` to be a `BlankRowStrategy<T>`,
  which a bare identifier cannot infer. Rescuing it needs a non-generic marker type plus an implicit
  conversion to `BlankRowStrategy<T>` (the `Option.None` trick) — machinery this vocabulary does not
  otherwise carry.
- A separate `blankRecord: Func<TableRow, T>` parameter infers `T` naturally (from `blankRecord` and,
  where present, from `project`), and the compiler then *enforces* that the blank record has the same
  type as a normal record — which is precisely the semantic we want.
- The doc itself already writes `onBlank`/`blankRecord` as two distinct knobs
  (`table-extent-and-blank-rows.md:149`), so this matches the settled naming.

Project is offered **only on the `Func<TableRow,T>` rungs** (items 5/6). It is deliberately NOT on the
`Table<T>()`/`Table<T>(bind)` reflection rungs: a Project overload there would be
`Table<T>(Func<TableRow,T> blankRecord)`, byte-identical in signature to the existing
`Table<T>(Func<TableRow,T> project)` at `Projection.cs:381` — a hard compile collision — and a
"blank record" for a reflected data type built from just `b.Index` is an odd need. A user who wants a
projected blank in a reflected table drops to the `(TableRow r) =>` rung, which is where blank records
read naturally. This matches every example in the design doc.

**Extensibility to "note + project"** (continue/project/info, mentioned at
`table-extent-and-blank-rows.md:45`) without an arity explosion: add, later, a
`Table<T>(… , Func<TableRow,T> blankRecord, BlankRowStrategy onBlank)` overload where `onBlank`
contributes only the Diagnostic axis. Additive; nothing built now blocks it.

### 1.4 `<TSpace>` re-exports (the covenant — mandatory, or CI fails)

`ProjectionBuildersParityTests.AndEveryOtherProjectionFactoryIsReachableFromTheBuilders`
(`src/Unrect.Tests/Projections/ProjectionBuildersParityTests.cs:607`) is a self-enforcing reflection
pin: every public static member of `Projection`, matched by **name + parameter count as a multiset**
(`Shape`, `:853`), must be re-exported on `ProjectionBuilders<TSpace>` or listed in
`TheDeliberateExclusions` (`:574`). Adding six `Projection.Table` overloads therefore **requires six
matching re-exports** on `ProjectionBuilders<TSpace>` (`src/Unrect/Projections/ProjectionBuilders.cs`,
beside the existing Table block at `:120–184`). They forward straight to `Projection`, exactly as the
existing row-lambda and reflection rungs do (`ProjectionBuilders.cs:129, 134, 165, 171`) — these rungs
are NOT raised to `TSpace` (the row is space-indifferent), so no scope routing:

```csharp
public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project, BlankRowStrategy onBlank) => Projection.Table(project, onBlank);
public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project, BlankRowStrategy onBlank) => Projection.Table(headerRows, project, onBlank);
public static IProjection<IReadOnlyList<T>> Table<T>(BlankRowStrategy onBlank) => Projection.Table<T>(onBlank);
public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind, BlankRowStrategy onBlank) => Projection.Table(bind, onBlank);
public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project, Func<TableRow, T> blankRecord) => Projection.Table(project, blankRecord);
public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project, Func<TableRow, T> blankRecord) => Projection.Table(headerRows, project, blankRecord);
```

`ProjectionScope<TSpace>` needs **no** change: it carries the projection-taking rungs (the
`IProjection`/`LabelMap` eachRow forms), which are out of scope here, and the covenant is
`Projection` vs `ProjectionBuilders`, not the scope. `SpreadsheetProjectionBuilders` is unaffected
(`Table` is not a formula rung).

**Out of scope for this pass** (Stop-only, unchanged): the dictionary rung `Table()`
(`Projection.cs:368`), the `Table(view => …)` escape rungs (`:404, :411`), and the `eachRow`
rungs (`:243, :319`, which walk `ProjectBands`→`StreamBands` at `:676`). The `eachRow` rungs *can*
adopt the identical pattern later via a policy-aware `StreamBands`; deliberately deferred to keep the
footprint minimal. None of these change behaviour.

---

## 2. The `BlankRowStrategy` type shape — recommendation and rationale

Recommended concrete shape: **the `readonly struct` in §1.1**, carrying Control (`Continues`) +
Diagnostic (`Diagnostic`, `IsFault`), with the four preset statics and an internal `IsStop`. The
Value axis is carried by the overload split (`onBlank:` = skip, `blankRecord:` = project), not by the
struct.

Why this over an enum: an enum cannot represent the three independent axes without either enumerating
the cross-product (arity explosion, and it forecloses "note + project") or losing the ability to add a
projected value. The struct keeps the axes independent, so the future note+project combination is one
additive overload (§1.3) rather than a new enum member plus a parallel value channel.

Why the Value axis is not in the struct: a projected blank carries `Func<TableRow, T>`, which cannot
live in a non-generic struct without boxing to `Func<TableRow, object>` and losing the type link to
the record list `IReadOnlyList<T>`. Keeping Value in the overload dodges the generics collision the
task flagged, and lets `Stop`/`Skip`/`Fault`/`Tolerate` stay non-generic values that infer `T` from
`project`.

`default(BlankRowStrategy)` is `Stop` by construction (all-false/all-null), which is what makes
`onBlank = default` byte-identical to the current behaviour and is the linchpin of §4.

---

## 3. The walker change

### 3.1 The area strategy: `onBlank` selects the body's height rule

`TablePlacement()` (`Projection.cs:750`) becomes parameterised. Keep a zero-arg overload that
delegates with `Stop` so the untouched rungs compile unchanged:

```csharp
private static Placement TablePlacement() => TablePlacement(BlankRowStrategy.Stop);

private static Placement TablePlacement(BlankRowStrategy onBlank)
    => new Placement(
         OffsetStrategies.SkipBlankRows(),
         onBlank.IsStop ? DiscoveredBlock() : ToEdgeBlock());

private static IAreaStrategy DiscoveredBlock()  // unchanged, Projection.cs:752
    => RowStrategies.TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue();

// NEW: same width rule as DiscoveredBlock (TakeColumnsWhileAnyValue), but the height runs to the
// enclosing edge instead of stopping at the first blank. Both are IIncrementalAreaStrategy, so the
// engine defers both lazily (ProjectionEngine.Bind, :169) — this stays a peeked, row-at-a-time walk.
private static IAreaStrategy ToEdgeBlock()
    => RowStrategies.AllRows().TakeColumnsWhileAnyValue();
```

`RowStrategies.AllRows()` (`src/Unrect.Strategies/RowStrategies.cs:46`) is `TakeToRowStrategy`, which
`is IIncrementalRowStrategy` (`src/Unrect.Strategies/Row/TakeToRowStrategy.cs:6`); composed with the
row-major `TakeColumnsWhileAnyValue` via `RowsThenColumns` it yields
`InterleavedRowAndColumnSizeStrategy` (incremental,
`src/Unrect.Strategies/Size/RowAndColumnSizeStrategy.cs:32–35`). So `ToEdgeBlock()` defers exactly as
`DiscoveredBlock()` does, and its scan's `IncludesRow` returns true without reading cells to decide
inclusion — run-to-edge peeks one row at a time and never measures the sheet up front (the same
profile that gives the 1M-row `Stop` table its 1.6 MB peak).

Why this is where the extent decision must live: the extent handed to `TableProjection.Project`
(`TableProjection.cs:31`) is resolved by placement **before** `Project` runs, and the walker
(`TableView.StreamBands`, `TableView.cs:132`) can only walk `HasRow(Space, …)` **within** that extent
(`TableView.cs:134`). Under `DiscoveredBlock`, `HasRow` is already false at the first blank row, so the
walker never sees a blank band — that is `Stop`, and it is why the other policies need a wider extent.

### 3.2 Blank detection

A fully-blank row = every cell `IsBlank` — the complement of `RowsWhileAnyValue`
(`table-extent-and-blank-rows.md:46–48`). Reuse `CellValue.IsBlank` on the row's cells. Add a private
helper to `TableView`:

```csharp
private static bool IsBlankRow(TableRow row)
{
  for (var column = 0; column < row.Count; column++)
    if (!row.Cells[column].IsBlank) return false;
  return true;
}
```

This reads the row's full width — the accepted row-wise (streaming-axis) cost noted in
`table-extent-and-blank-rows.md:71–99`. It is the one-row peek, evaluated lazily as the walk advances.

### 3.3 The policy-aware iterators on `TableView`

The public `StreamRows()` (`TableView.cs:111`) and `StreamBands()` (`:132`) stay **byte-identical** —
they are the reading a user `Table(view => …)` projection and every existing rung use, and nothing about
them should change. Add two internal iterators beside them:

```csharp
/// The body rows that become records under the four preset policies (Stop/Skip/Fault/Tolerate).
/// Project is not here — it injects records and is handled by the rung (StreamClassifiedRows).
internal IEnumerable<TableRow> StreamBodyRows(BlankRowStrategy onBlank)
{
  // Stop: the extent (DiscoveredBlock) already excludes blank rows, so there is nothing to test and
  // nothing to filter — delegate verbatim. This is what keeps the Stop path byte-identical AND free
  // of the extra full-width blank test.
  if (onBlank.IsStop) { foreach (var row in StreamRows()) yield return row; yield break; }

  foreach (var row in StreamRows())            // over the ToEdgeBlock extent -> to the enclosing edge
  {
    if (!IsBlankRow(row)) { yield return row; continue; }

    if (onBlank.IsFault) throw Fault(BlankMessage(row));       // Fault: terminal (TableView.Fault, :161)
    if (onBlank.Diagnostic is DiagnosticSeverity severity)     // Tolerate: nonterminal Info
      Context.Report(severity, Failure(ToleratedMessage(row))); // reuse Report(severity, ProjectionException), :219
    // Skip and Tolerate both omit the record and keep reading.
  }
}

/// Every body row to the edge, each tagged blank/not — the source the Project (blankRecord) rung maps.
internal IEnumerable<(TableRow Row, bool IsBlank)> StreamClassifiedRows()
{
  foreach (var row in StreamRows()) yield return (row, IsBlankRow(row));
}
```

Notes:
- `Fault(...)` already exists (`TableView.cs:161`) and produces an `IsFault: true`
  `ProjectionException` no tolerance boundary can absorb — the terminal behaviour Fault needs, using
  the existing model (task requirement "not new machinery").
- Tolerate's Info reuses the `Report(DiagnosticSeverity, ProjectionException, …)` overload
  (`ProjectionContext.cs:219`), fed a non-throwing `Failure(msg)` (`TableView.cs:155`) so the Info
  carries the table's own path and location — the same shape `UntilProjection` uses for its `orEnd`
  Info (`UntilProjection.cs:69`). No projection reference is needed, which is why this overload (not
  the `Report(severity, IProjection, …)` one at `:199`) is the right seam.
- `BlankMessage`/`ToleratedMessage(row)` are one-line message helpers citing `row.Location`/`row.Index`.

### 3.4 Dispatch per rung — wiring the producers

Only the **new** overloads route to the policy walk; the existing overload bodies are untouched.

`Func<TableRow,T>` rung with `onBlank` (items 1/2). Item 2 is the real body; item 1 delegates:
```csharp
public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project, BlankRowStrategy onBlank)
{
  if (project is null) throw new ArgumentNullException(nameof(project));
  if (onBlank.IsStop) return Table(headerRows, project);        // -> existing overload, byte-identical

  return new TableProjection<IReadOnlyList<T>>(
    ValidateHeaderRows(headerRows),
    table => (IReadOnlyList<T>)table.StreamBodyRows(onBlank).Select(project).ToList(),
    TablePlacement(onBlank),                                     // ToEdgeBlock area
    "Table");
}
```

`Func<TableRow,T>` rung with `blankRecord` (Project, items 5/6). Item 6 is the body:
```csharp
public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project, Func<TableRow, T> blankRecord)
{
  if (project is null) throw new ArgumentNullException(nameof(project));
  if (blankRecord is null) throw new ArgumentNullException(nameof(blankRecord));

  return new TableProjection<IReadOnlyList<T>>(
    ValidateHeaderRows(headerRows),
    table => (IReadOnlyList<T>)table.StreamClassifiedRows()
                 .Select(r => r.IsBlank ? blankRecord(r.Row) : project(r.Row)).ToList(),
    TablePlacement(BlankRowStrategy.Skip),                       // Project continues past blanks -> ToEdgeBlock
    "Table");
}
```
(`Project` is continue/project/none, so it needs the run-to-edge extent — pass any non-Stop strategy
to `TablePlacement` to select `ToEdgeBlock()`; `Skip` reads clearest.)

`Table<T>()` / `Table<T>(bind)` with `onBlank` (items 3/4). Thread `onBlank` through `TypedRows`:
```csharp
private static IProjection<IReadOnlyList<T>> TypedRows<T>(TableBinding<T>? binding, BlankRowStrategy onBlank = default)
{
  var plan = RowBinding<T>.Create(binding);
  return new TableProjection<IReadOnlyList<T>>(
    1,
    table => BindRows(table, plan, onBlank),
    TablePlacement(onBlank),
    $"Table<{typeof(T).Name}>");
}
```
`BindRows` (`src/Unrect/Projections/Binding/Projection.Binding.cs:17`) gains a `BlankRowStrategy
onBlank` parameter and changes only its row source (`:64`) from `table.StreamRows()` to
`onBlank.IsStop ? table.StreamRows() : table.StreamBodyRows(onBlank)`. Under `Stop` the source is
literally `StreamRows()` — byte-identical, including the `TrimExcess()` at `:74`. Reflection binding,
the unbound-member and ambiguous-column failures (`:30–56`) all resolve at construction / on the first
non-blank rows exactly as now. (Project is not supported here, §1.3, so `StreamClassifiedRows` is not
needed in `BindRows`.)

`TableProjection<T>` (`TableProjection.cs`) itself needs **no change**: it already runs the
producer lambda (`:39`) and returns `extent.Area.Size` (`:44`). For `ToEdgeBlock`, `extent.Area.Size`
forces the (already-walked) scan to the edge — the correct consumed size. For `Fault`, the producer
throws before `:44` is reached, so the fault propagates and the engine classifies it (`:198–218`,
`IsFault` true).

---

## 4. Boundary interaction (`.Until(...)` / count / run-to-edge)

`.Until(landmark)` is a pipeline `UntilRow` step (`Steps.cs:133, 175`) that wraps the `Table` in an
`UntilProjection` via `subject.BoundedBy(...)`. `UntilProjection.Project`
(`UntilProjection.cs:55–78`) finds the landmark, slices its extent to `[0, limit)`, and applies the
inner `Table` to that bounded subspace. Composition with a non-self-bounding `onBlank`:

- **`Until` + Skip/Fault/Tolerate/Project.** The `Table`'s own placement area is `ToEdgeBlock()` =
  `AllRows`, so within the bounded subspace the "edge" *is* the landmark: the body runs to the landmark
  and the walker applies the policy to every row up to it. `UntilProjection` consumes the bound in full
  (`:71–77`), so the next sibling lands on the landmark — unchanged from how `Until` composes with any
  leaf today. `Until` inherently forces the landmark search (reads down to it, `:58`); within that
  already-read span the bounded `AllRows` walk is not additionally lazy, which is acceptable and
  expected for a content boundary.

- **No boundary declared -> run to edge.** With no `Until`/count, the `Table`'s `ToEdgeBlock`
  `AllRows` extent runs to the **enclosing extent's edge**: the parent flow's band, or for the
  outermost table the sheet's used rows (whatever `inner.Area.Height` reports — cheap, from the reader
  dimension, not a full cell scan). This is the RESOLVED-2026-09-12 ruling
  (`table-extent-and-blank-rows.md:110–119`) expressed with no new object: `AllRows()` is exactly
  "all remaining rows to the enclosing edge", and the walker peeks each one once. A non-self-bounding
  policy with content *below* a blank gap and no boundary will read that content as rows — the
  documented, accepted consequence; declare `Until`/count or use `Stop` to bound it.

- **`Until` + Stop.** Legal but usually redundant: `Stop` already halts at the first blank before the
  landmark. Left to the user; no special-casing.

---

## 5. Additive-safety argument and the exact test pins to add

### 5.1 Why every existing test stays byte-identical

- Every existing `Table` overload's **signature and body is untouched**. Callers that omit `onBlank`
  bind the same overloads they bind today.
- The new overloads with `onBlank == Stop` (the default) **delegate to the existing overloads**
  (items 1/2) or take `TypedRows(..., Stop)` whose `TablePlacement(Stop)` returns the unchanged
  `DiscoveredBlock()` and whose `BindRows` source is the unchanged `StreamRows()`.
- `TablePlacement(Stop)` is `new Placement(SkipBlankRows(), DiscoveredBlock())` — the exact expression
  at `Projection.cs:750` today.
- `StreamBodyRows(Stop)` delegates to `StreamRows()` with no blank test and no filtering.
- `TableProjection`, `TableView.StreamRows`/`StreamBands`, `CaptionComparer`, `ProjectBands` are not
  modified on the Stop path.
- `default(BlankRowStrategy) == Stop`, so `= default` on every new parameter is Stop.

Net: no existing decomposition changes shape, order, consumed size, presence, or diagnostics.

### 5.2 Test-side pins the senior-dev MUST close (or CI breaks)

1. **The completeness covenant** —
   `ProjectionBuildersParityTests.AndEveryOtherProjectionFactoryIsReachableFromTheBuilders`
   (`ProjectionBuildersParityTests.cs:607`) will fail the moment the six `Projection.Table` overloads
   exist without the six `ProjectionBuilders<TSpace>` re-exports (§1.4). Add the six forwarders. Also
   check `AndNoNameIsMissingAltogether` (`:630`) — still passes since `Table` is already a reachable
   name — and `EveryExclusionIsARealMemberOfTheVocabulary` (`:597`) — the exclusion list (`:574`) is
   NOT touched (the new overloads are re-exported, not excluded).
2. **Value-parity theory** — the parity suite reads all projection-returning members through the
   `Observations` harness (`ProjectionBuildersParityTests.cs:42–51`). The six new re-exports must
   produce L3-identical readings to their `Projection` twins over the three grids; since they forward
   one-to-one this holds by construction, but the theory's member enumeration must include them.
3. **New behavioural coverage** (the +N regression tests, mirroring how the 2,073 grew): pin each
   preset on a synthetic sheet with an interior blank row and a trailing blank — Stop (identical to a
   no-`onBlank` control), Skip (blank omitted, run-to-edge), Fault (terminal `IsFault`, unabsorbed by
   `.Optional()`), Tolerate (blank omitted + one Info per blank, path = the table's), Project (a blank
   yields `blankRecord(row)` with correct `row.Index`). Add an `Until(landmark)` + Skip pin proving the
   body runs to the landmark and the next sibling lands on it. Add a lazy pin (eager-vs-streaming L3,
   in the spirit of `LazyDenotationTests`/`CrossDoorDenotationTests`) proving run-to-edge stays
   row-at-a-time and Stop's rows-touched count is unchanged.
4. **`default == Stop`** — a one-line pin that `default(BlankRowStrategy).IsStop` and that
   `Table((TableRow r) => …, onBlank: Stop)` denotes L3-identically to `Table((TableRow r) => …)`.

---

## 6. Risks and sharp edges found in the code

1. **`StreamBands` does not peek a row past its band.** It calls `HasRow(Space, row + bandHeight - 1)`
   (`TableView.cs:134`); at `bandHeight == 1` that is the row itself. Under `DiscoveredBlock` the blank
   row is never yielded (Stop); under `ToEdgeBlock` the blank row IS yielded and its cells are read by
   the blank test — that read is the intended one-row peek, not an extra one.
2. **Run-to-edge needs a different area-strategy object, and it exists.** `ToEdgeBlock()` =
   `RowStrategies.AllRows().TakeColumnsWhileAnyValue()` is a distinct `IIncrementalAreaStrategy`
   (verified incremental in §3.1); the width rule is identical to `DiscoveredBlock`, only the height
   rule changes. Do not reuse `DiscoveredBlock()` for non-Stop — its `TakeRowsWhileAnyValue` height is
   exactly the stop we are removing.
3. **Interaction with the leading offset.** `TablePlacement`'s offset is `SkipBlankRows()`
   (`Projection.cs:750`) today. It governs **leading** blanks (positioning the table's top-left);
   `onBlank` governs blanks **after** content starts. They are orthogonal — with `Skip` and a leading
   blank run, the offset consumes the leading blanks and the walker skips interior ones. NOTE: the
   design doc separately RESOLVED (2026-09-12) that the leaf offset default should become
   `SkipToFirstNonBlankCell` (`table-extent-and-blank-rows.md:122–136`). That is a **separate** ruling
   and NOT part of this `onBlank` spec; keep `SkipBlankRows()` here so this change stays orthogonal and
   additive. If both land in one branch, land the offset change first and independently.
4. **Fault ordering.** `TableProjection.Project` runs the producer (`:39`) before computing
   `extent.Area.Size` (`:44`), so a Fault thrown mid-walk propagates cleanly as a fault; the engine
   classifies it `IsFault` (`ProjectionEngine.cs:198–218, 288`). No consumed/presence is computed on
   the faulting path — correct.
5. **Presence at the edge.** `TableProjection` returns `extent.Area.Size`; the engine's `Settled`
   (`ProjectionEngine.cs:241`) stamps `Empty` only when a declared area settles at width/height 0. A
   run-to-edge table with a header always consumes height >= 1, and Presence is read off the settled
   extent, so the two doors cannot disagree. No Stop-path change.
6. **`Record` primitive is unrelated.** `RecordProjection` (new on this branch,
   `src/Unrect/Projections/Primitives/RecordProjection.cs`) is a single-row primitive; `onBlank` is a
   Table body concern and does not touch it. `Record` builds a `TableRow` with a null owning view
   (`TableView.cs:372–374`) — the `StreamBodyRows` path never runs for it.
7. **Do not route the public `StreamRows()` through the policy.** The `Table(view => …)` escape hatch
   hands the whole view; blank policy there is the user's business (design doc scopes `onBlank` to the
   built-in row rungs). Adding two internal iterators rather than mutating `StreamRows()` keeps that
   line clean and keeps the public API byte-identical.
8. **Covenant matches by param COUNT, not type.** Because `Shape` (`ParityTests:853`) reduces to
   name/count, the two `Table/2` additions (item 1 `BlankRowStrategy`, item 5 `Func<TableRow,T>`) and
   the two `Table/3` additions must ALL be mirrored on `ProjectionBuilders<TSpace>` as a multiset — six
   re-exports for six overloads, no fewer, or the multiset comparison at `:626` fails.
