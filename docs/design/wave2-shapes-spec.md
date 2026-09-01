# Wave 2 Implementation Spec: The Fused Shape Layer

**Status:** ready to implement (authored 2026-08-31 by architect pass; prototyped: tuple-Select inference, extension-method-group `spaces.Select(map.Map)`, MemberwiseClone modifiers; default strategies validated against all three example workbooks).
**Governing docs:** `CLAUDE.md`, `docs/design/canonical-model-and-shapes.md` (§4, §5, DECIDED applicative-fusion block are hard constraints).

## 0. Summary of the approach

A **shape** is an immutable value that declares (a) where it sits in the space it is handed and (b) how to project that space into a `TResult`. `shape.Map(space)` decomposes and projects in one call. Shapes compose; the composite is itself a shape.

Three structural commitments:

1. **Placement is owned by the shape and applied by exactly one code path** (`ShapeEngine.Apply`), exactly once, at every level *including the root*. `IShape<T>.Project` receives an already-resolved extent and therefore *cannot* see or re-apply an offset. This makes the `Builder(offset, area, sub)` trap unspellable.
2. **The fused layer reimplements decomposition against the strategy calculus, not against `IRegionBuilder`.** It never constructs a `Region`. The 182 existing tests and the whole builder/region/strategy surface stay untouched and public.
3. **Arity lives in static factory overloads only.** One composite class (`StackShape<T>`) for all arities of `Vertical`/`Horizontal`; no `Region4`/`Builder4` families.

Validated end-to-end against `examples/simple-report.xlsx`, `examples/investors-by-deal.xlsx`, `examples/investor-summary.xlsx`.

## 1. Type inventory

All types in namespace `Unrect.Shapes`, in the existing `Unrect` project.

### 1.1 Core abstraction

```csharp
public interface IShape
{
  string? Name { get; }               // user label, carried into errors/diagnostics
  string Description { get; }         // structural fallback, e.g. "Column(4)"
  Placement Placement { get; }
  IReadOnlyList<IShape> Children { get; }   // declaration order; empty for leaves
  bool IsTransparent { get; }         // true only for unnamed Select wrappers; skipped in paths
  ShapeResult<object?> ProjectUntyped(ISpace extent, ShapeContext context);
}

public interface IShape<TResult> : IShape
{
  ShapeResult<TResult> Project(ISpace extent, ShapeContext context);  // receives RESOLVED extent
  IShape<TResult> WithName(string name);
  IShape<TResult> WithPlacement(Placement placement);
}

public readonly struct ShapeResult<T>
{
  public ShapeResult(T value, Size consumed);
  public T Value { get; }
  public Size Consumed { get; }
}

public readonly struct AppliedResult<T>
{
  public AppliedResult(T value, Offset offset, Size consumed);
  public T Value { get; }
  public Offset Offset { get; }
  public Size Consumed { get; }
  public Size Advance { get; }   // Offset.Size + Consumed
}

public sealed class Placement
{
  public Placement(IOffsetStrategy offset, IAreaStrategy? area);
  public static Placement Default { get; }              // MinOffset + derived area
  public static Placement Of(IAreaStrategy area);       // MinOffset + given area
  public IOffsetStrategy Offset { get; }
  public IAreaStrategy? Area { get; }                   // null = derived from children/content
  public Placement WithOffset(IOffsetStrategy offset);
  public Placement WithArea(IAreaStrategy area);
}

public abstract class ShapeBase<TResult> : IShape<TResult>
{
  protected ShapeBase(Placement placement);
  // Name/Placement private-set; WithName/WithPlacement via MemberwiseClone —
  // subclasses MUST be immutable field bags.
  public abstract string Description { get; }
  public virtual IReadOnlyList<IShape> Children { get; }  // Array.Empty default
  public virtual bool IsTransparent { get; }              // false default
  public abstract ShapeResult<TResult> Project(ISpace extent, ShapeContext context);
}
```

### 1.2 Engine, context, errors

```csharp
public static class ShapeEngine
{
  public static AppliedResult<TResult> Apply<TResult>(IShape<TResult> shape, ISpace availableSpace, ShapeContext context);
  public static AppliedResult<object?> ApplyUntyped(IShape shape, ISpace availableSpace, ShapeContext context);
  // TryApply: returns false when the shape's OWN placement does not fit (Repeat's stop
  // condition); projection exceptions and nested failures still propagate.
  public static bool TryApply<TResult>(IShape<TResult> shape, ISpace availableSpace, ShapeContext context, out AppliedResult<TResult> result);
}

public sealed class ShapeContext   // immutable; fresh tree per Map call (thread-safe reuse)
{
  public static ShapeContext Root(ISpace space);
  public ShapeContext? Parent { get; }
  public IShape? Shape { get; }
  public int? Index { get; }        // repeat index
  public Offset Origin { get; }     // accumulated from root
  public string Path { get; }       // "Vertical -> 'investor details'[2] -> 'investor name' (Cell)"
  public ShapeContext Descend(IShape shape, Offset offset, int? index = null);
  public ShapeContext Advance(Offset offset);
  public ShapeLocation Locate(ISpace space);
  public ShapeException Failure(string problem, ISpace space, Exception? inner = null);
}

public readonly struct ShapeLocation
{
  public int Row { get; }           // 1-based, relative to root space
  public int Column { get; }
  public Size Available { get; }
  public string A1 { get; }         // "A30"
  public override string ToString();  // "row 30, column 1 (A30)"
}

public sealed class ShapeException : Exception
{
  public string Subject { get; }    // quoted name or description
  public string Path { get; }
  public ShapeLocation Location { get; }
  public Size? Requested { get; }
  public IShape Shape { get; }
}
```

### 1.3 Projection views

```csharp
public sealed class CellStrip : IReadOnlyList<CellValue>
{
  public ISpace Space { get; }
  public int Count { get; }
  public CellValue this[int index] { get; }
}

public sealed class CellBlock
{
  public ISpace Space { get; }
  public int Width { get; }
  public int Height { get; }
  public CellValue this[int column, int row] { get; }
  public CellStrip Row(int index);
  public CellStrip Column(int index);
  public IReadOnlyList<CellStrip> Rows { get; }
  public IReadOnlyList<CellStrip> Columns { get; }
}

public sealed class TableView
{
  public ISpace Space { get; }              // whole extent incl. header
  public int ColumnCount { get; }
  public int RowCount { get; }              // body rows only
  public bool HasHeader { get; }
  public CellStrip Header { get; }          // empty when HasHeader false
  public IReadOnlyList<string> ColumnNames { get; }   // "" for blank/non-text header cells
  public IReadOnlyList<TableRow> Rows { get; }
}

public sealed class TableRow
{
  public int Index { get; }                 // 0-based within body
  public int Count { get; }
  public CellValue this[int column] { get; }
  public CellValue this[string columnName] { get; }   // tier 2
  public bool TryGet(string columnName, out CellValue value);
  public IReadOnlyList<CellValue> Cells { get; }
}
```

### 1.4 The vocabulary (`Shape`) — the one using-static

```csharp
public static class Shape
{
  // leaves
  public static IShape<T> Cell<T>(Func<CellValue, T> project);
  public static IShape<T> Row<T>(Func<CellStrip, T> project);                       // 1 row x columns-with-values
  public static IShape<T> Row<T>(int width, Func<CellStrip, T> project);
  public static IShape<T> Row<T>(IColumnStrategy columns, Func<CellStrip, T> project);
  public static IShape<T> Column<T>(Func<CellStrip, T> project);                    // 1 col x rows-with-values
  public static IShape<T> Column<T>(int height, Func<CellStrip, T> project);
  public static IShape<T> Column<T>(IRowStrategy rows, Func<CellStrip, T> project);
  public static IShape<T> Cells<T>(Func<CellBlock, T> project);                     // maximal value-bearing block
  public static IShape<T> Cells<T>(int width, int height, Func<CellBlock, T> project);
  public static IShape<T> Cells<T>(IAreaStrategy area, Func<CellBlock, T> project);

  // tables
  public static IShape<T> Table<T>(Func<TableView, T> project);
  public static IShape<T> Table<T>(int headerRows, Func<TableView, T> project);     // 0 or 1
  public static IShape<IReadOnlyList<T>> TableRows<T>(Func<TableRow, T> project);
  public static IShape<IReadOnlyList<T>> TableRows<T>(int headerRows, Func<TableRow, T> project);

  // stacks — arities 2..8, tuple results
  public static IShape<(T1, T2)> Vertical<T1, T2>(IShape<T1> first, IShape<T2> second);
  public static IShape<(T1, T2)> Horizontal<T1, T2>(IShape<T1> first, IShape<T2> second);
  // ... through <T1..T8>

  // repetition
  public static IShape<IReadOnlyList<T>> Repeat<T>(IShape<T> item, IOffsetStrategy? separatedBy = null, int atLeast = 0);
  public static IShape<IReadOnlyList<T>> RepeatHorizontal<T>(IShape<T> item, IOffsetStrategy? separatedBy = null, int atLeast = 0);

  // offset vocabulary re-exports (no strategy import needed for common cases)
  public static IOffsetStrategy BlankRows();          // == OffsetStrategies.SkipBlankRows()
  public static IOffsetStrategy BlankColumns();
  public static IOffsetStrategy SkipRows(int count);
  public static IOffsetStrategy SkipColumns(int count);
  public static IOffsetStrategy Then(params IOffsetStrategy[] offsets);
}
```

### 1.5 Modifiers and combinators (`ShapeExtensions`)

```csharp
public static class ShapeExtensions
{
  public static TResult Map<TResult>(this IShape<TResult> shape, ISpace space);
  public static AppliedResult<TResult> Apply<TResult>(this IShape<TResult> shape, ISpace space);

  public static IShape<T> Named<T>(this IShape<T> shape, string name);
  public static IShape<T> After<T>(this IShape<T> shape, IOffsetStrategy offset);
  public static IShape<T> AfterBlankRows<T>(this IShape<T> shape);
  public static IShape<T> AfterBlankColumns<T>(this IShape<T> shape);
  public static IShape<T> Down<T>(this IShape<T> shape, int rows);
  public static IShape<T> Right<T>(this IShape<T> shape, int columns);
  public static IShape<T> Sized<T>(this IShape<T> shape, IAreaStrategy area);

  public static IShape<TResult> Select<T, TResult>(this IShape<T> shape, Func<T, TResult> selector);
  public static IShape<TResult> Select<T1, T2, TResult>(this IShape<(T1, T2)> shape, Func<T1, T2, TResult> selector);
  // ... tuple arities 3..8
}
```

`.Named()` after `.Select()` compiles and names the wrapper; `spaces.Select(map.Map)` compiles as extension-method-group conversion (both prototyped).

### 1.6 Internal implementations (all internal sealed)

| Type | Notes |
|---|---|
| `CellShape<T>` | area `ExplicitArea(1,1)` |
| `StripShape<T>` | Orientation field; backs Row and Column |
| `BlockShape<T>` | backs Cells |
| `TableShape<T>` | backs Table / TableRows |
| `StackShape<T>` | ONE class for all arities: `IReadOnlyList<IShape>` children, Orientation, `Func<object?[], T>` combine |
| `RepeatShape<T>` | item, separator, orientation, atLeast |
| `MapShape<TSource,TResult>` | backs Select; `IsTransparent => Name is null` |

### 1.7 One additive change to Unrect.Strategies

`OffsetStrategies.Then(params IOffsetStrategy[] offsets)` — applies each offset to the space remaining after the previous one and sums. Backed by internal `CompositeOffsetSizeStrategy : ISizeStrategy`. Purely additive.

## 2. Semantics

### 2.0 The universal rule (anti-trap; implement literally)

> A shape's `Placement` describes where that shape sits inside the space it is handed. It is applied by `ShapeEngine.Apply` and by nothing else, exactly once, at every level, including the top-level `Map` call. `IShape<T>.Project` receives the resolved extent and can neither observe nor re-apply the offset.

`ShapeEngine.Apply(shape, available, context)`:
1. `offset = shape.Placement.Offset.GetOffset(available)`; strategy `OutOfBoundsException` → rethrown as `ShapeException`.
2. If offset exceeds available in either dimension → `ShapeException` (case A).
3. `inner = available.GetSubspace(offset)`.
4. `scope = context.Descend(shape, offset)` (skipped for transparent shapes).
5. If `Placement.Area is null`, `extent = inner`; else compute area on `inner`, bounds-check (case B), `extent = inner.GetSubspace(area)`.
6. `result = shape.Project(extent, scope)`; non-ShapeException exceptions wrapped (case D); ShapeException propagates unchanged.
7. `consumed = Placement.Area is null ? result.Consumed : extent.Area.Size` — **explicit area is consumed in full even if content used less.**
8. Return `AppliedResult(result.Value, offset, consumed)`.

`TryApply` differs only in steps 1/2/5 (returns false instead of throwing). `Map(space)` == `Apply(shape, space, ShapeContext.Root(space)).Value`.

Documented consequences: `Table(...).Map(sheet)` skips leading blank rows at the top (the declaration means what it reads, unlike the builder layer's silent no-op); `Vertical(a, b).AfterBlankRows()` positions the stack while `Vertical(a.AfterBlankRows(), b)` positions the first child — both legal, both read correctly.

### 2.1 Leaves

| Vocabulary | Default area | Projection input | Validation |
|---|---|---|---|
| `Cell(p)` | `ExplicitArea(1,1)` | the `CellValue` | extent 1x1 |
| `Row(p)` | `TakeRows(1).TakeColumnsWhileAnyValue()` | `CellStrip` row 0 | height 1 |
| `Row(n, p)` | `ExplicitArea(n,1)` | " | " |
| `Row(cols, p)` | rows(1) + given col strategy | " | " |
| `Column(p)` | `TakeColumns(1).TakeRowsWhileAnyValue()` | `CellStrip` col 0 | width 1 |
| `Column(n, p)` | `ExplicitArea(1,n)` | " | " |
| `Column(rows, p)` | cols(1) + given row strategy | " | " |
| `Cells(p)` | `TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue()` | `CellBlock` | none |
| `Cells(w,h,p)` | `ExplicitArea(w,h)` | " | none |
| `Cells(area,p)` | given | " | none |

Consumed = extent size. Validation failures (e.g. `Column` sized wider than 1 via `.Sized()`) → case C.

### 2.2 Vertical / Horizontal

Default placement: MinOffset + derived area (`Area = null`). Project: cursor walk in declaration order; each child applied via `ShapeEngine.ApplyUntyped` (hard error if it does not fit); stack consumes along its own axis only (cross-axis = max of children); returns `combine(values)` + derived consumed size. Arity 2..8; beyond 8, nest (a stack is a shape). 8 = ValueTuple's natural limit before TRest.

### 2.3 Select

`MapShape` with Default placement; Project = `ShapeEngine.Apply(inner, extent, context)` then selector; Consumed = inner's Advance. Therefore `X.Select(f).After(o)` ≡ `X.After(o).Select(f)` (test this). Unnamed MapShape is transparent in paths.

### 2.4 Repeat

`Repeat(item, separatedBy: null, atLeast: 0)`; default placement MinOffset + derived.

**Separator = sepBy: applied between items, never before the first.** Leading gap is the Repeat's own offset (`Repeat(...).AfterBlankRows()`).

Loop: while remaining nonempty → apply separator (any failure to fit → stop) → `TryApply(item)` (own-placement failure → stop) → zero-consumed or zero-advance → stop → collect, advance along axis. `results.Count < atLeast` → case C. Termination guards mirror the hardened `SuperStackRegionBuilder`.

**Only the item's own placement resolution is a stopping condition; failures deeper inside the item (nested misfit, projection throw) are errors** — intra-block format drift is loud, not silently truncating.

Separator is a Repeat parameter (not the item's offset) because separation is a property of the repetition; an item carrying "blank row before me" is not reusable standalone. The alternative spelling still works; documented as discouraged.

**Post-review note: `separatedBy` is load-bearing for termination, not just separation.** A Select-wrapped composite item has `Placement.Default` (MinOffset, derived area), so its own placement can never fail — meaning a separator-less Repeat only stops when the remaining space is literally empty or the item errors from inside. With trailing NON-blank content after the last item (a "Total" row, a footer), the repetition runs into it and fails loudly from inside the item — correct per the loud-drift boundary, but the error blames the item's internals rather than saying "the repetition met content it can't parse." The real fix is wave-3 `Choice`; until then, always give `Repeat` a separator when the sheet has trailing content, and know that the failure path will point into the item.

### 2.5 Table

Default placement: Offset = `SkipBlankRows()`, Area = `TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue()`.

- `headerRows` ∈ {0, 1}; else `ArgumentOutOfRangeException` at construction ("multi-row headers are not supported in this release").
- header expected but extent empty → case C.
- `ColumnNames[i]` = `Header[i].TryGetString()?.Trim() ?? ""`.
- Header-only table → empty `Rows` (legitimate). Consumed = extent size.
- **Tier 1 (index):** out of range → case C "column index 4 is out of range; the table has 4 columns."
- **Tier 2 (name):** dictionary per TableView, OrdinalIgnoreCase over trimmed non-empty names; unknown → case C listing available columns; duplicate → "appears at indices 2 and 5; use the index"; `TryGet` false for unknown, throws for ambiguous; name lookup with `headerRows: 0` → "declared without a header row; use column indices."
- **Tier 3 not built** (deferred).
- `TableRows(p)` ≡ `Table(t => t.Rows.Select(p).ToList())`, same defaults.

### 2.6 Gaps

**No `Gap` element** — it would inject a unit value into every stack's arity. Gaps are the following shape's offset (`.AfterBlankRows()`, `.Down(2)`, `.After(Then(...))`) — usually already defaulted — or a declared-but-unprojected region in the (still public) builder layer.

### 2.7 Failure behavior

Fused layer throws `ShapeException`; a bare `OutOfBoundsException` never escapes `Map` (closes the "no diagnostics" finding without touching the pinned type).

Message template:
```
{subject}: {problem}
  in {path}
  at {location}; {width}x{height} available
```
Cases: A offset-does-not-fit; B area-does-not-fit; C invariant violated (missing header row, atLeast unmet, bad column index/name, Column not 1-wide); D user projection threw (wrapped once, InnerException preserved, never double-wrapped, path + A1 location — the single biggest debugging win).

## 3. File/project layout

Namespace `Unrect.Shapes` inside the `Unrect` project (same conceptual layer, no new deps, zero reference churn; extractable later).

```
src/Unrect/Shapes/
  IShape.cs ShapeBase.cs Placement.cs ShapeResult.cs ShapeContext.cs
  ShapeLocation.cs ShapeException.cs ShapeEngine.cs
  Shape.cs Shape.Stacks.cs [mechanical] ShapeExtensions.cs ShapeExtensions.Select.cs [mechanical]
  Primitives/CellShape.cs StripShape.cs BlockShape.cs TableShape.cs
  Composites/StackShape.cs RepeatShape.cs MapShape.cs
  Views/CellStrip.cs CellBlock.cs TableView.cs
src/Unrect.Strategies/OffsetStrategies.cs            (+Then)
src/Unrect.Strategies/Offset/CompositeOffsetSizeStrategy.cs  (new, internal)
src/Unrect.Tests/Shapes/                              (§6)
src/Unrect.Tests/TestData/investor-summary.xlsx       (Content, PreserveNewest)
```

**Deletions:** `src/Unrect/RegionMapper.cs`, `src/Unrect/RegionMapperFactory.cs`, `src/Unrect.Core/IRegionMapper.cs` (verified unreferenced; 182 tests stay green). `RegionExtensions.Map` survives.

Mechanical files: write once with a header comment; do not invent abstraction to avoid repetition.

## 4. Target usage scripts

(§4.1 simple-report, §4.2 investors-by-deal rewrites and §4.3 investor-summary — see the scripts as shipped in linqpad/ after wave-2 implementation; acceptance = they match the spec's verified extents: simple-report header 1x4 + table 4x9; investors-by-deal 3 blocks heights 5/7/4 with 3/5/2 txns; investor-summary header 1x3 discovered, summary 4x4, 3 detail blocks 3/2/4 txns.)

Key excerpt (investor-summary):
```csharp
var report =
  Vertical(
    Column(c => new { Title = c[0].GetString(), ReportDate = c[1].GetDateTime(), ReportId = c[2].GetString() })
      .Named("report header"),
    TableRows(r => new { Investor = r["Investor"].GetString(), Contributions = r["Contributions"].GetDecimal(),
                         Distributions = r["Distributions"].GetDecimal(), Net = r["Net"].GetDecimal() })
      .Named("summary"),
    Repeat(investorDetail, separatedBy: BlankRows(), atLeast: 1).AfterBlankRows().Named("investor details"))
  .Select((header, summary, details) => new { ReportHeader = header, Summary = summary, Details = details });
```

## 5. Explicit decisions (abbreviated; full rationale in the architect pass)

1. Fused layer reimplements decomposition on the strategy calculus (no IRegionBuilder wrap — would cap arity and leak Region types).
2. Substrate stays intact and public; 182 tests pin it.
3. RegionMapper/RegionMapperFactory/IRegionMapper deleted; RegionExtensions.Map survives.
4. Namespace Unrect.Shapes in Unrect project, not a new assembly.
5. `using static Unrect.Shapes.Shape;` is the single import; offset helpers re-exported.
6. Placement applied by ShapeEngine alone, once, at every level incl. root.
7. No positional offset/area parameters; placement via postfix modifiers only.
8. Project receives the resolved extent (trap prevented by signature, not docs).
9. IShape<T> invariant (ShapeResult<T> struct forbids covariance; loss nil).
10. One StackShape<T> for all arities (object?[] boxing hop).
11. Max stack arity 8; nest beyond; no params overload (ambiguous with 2-arity).
12. Select as extension on tuple shapes 1..8; survives Named/After.
13. Stack extent derived from children (Area = null) — enables placement-free Repeat items.
14. Explicit area consumed in full.
15. Repeat separator = parameter, sepBy between items only; leading gap = Repeat's offset.
16. Repeat stops on item's own placement failure; errors on anything deeper.
17. atLeast converts silent empty list into a good error.
18. No Gap element.
19. Table defaults: skip blank rows / 1 header row / rows-while-any x columns-while-any.
20. Table tiers 1+2 ship; tier 3 does not.
21. TableRows beside Table (distinct names; overloads ambiguous with implicit lambdas).
22. Every primitive: discovered / explicit-count / strategy forms + After/Sized modifiers.
23. Fused layer throws ShapeException; bare OutOfBoundsException never escapes Map.
24. Projection exceptions wrapped once, InnerException preserved.
25. ShapeContext accumulates origin → "row 30, column 1 (A30)", relative to Map's space.
26. Modifiers replace (last wins); Then(...) composes explicitly.
27. Down(rows)/Right(columns), not At(x, y).
28. MemberwiseClone-based WithName/WithPlacement; subclasses are immutable field bags.
29. Shapes immutable + thread-safe; ShapeContext per-Map; parallel Map safe.
30. OffsetStrategies.Then is the only strategies addition.
31. Region tree stays public; wave-3 diagnostics hang off ShapeContext.
32. Builder-layer top-level-Build open question stays open; its pinning tests untouched.

## 6. Test plan (src/Unrect.Tests/Shapes/)

- **PlacementTests** — anti-trap: own offset/area applied at top level; applied exactly once nested; offset-on-stack vs offset-on-child; Select/modifier commutation; modifiers replace; Then composes; immutability.
- **PrimitiveShapeTests** — Cell/Row/Column/Cells discovered + explicit + strategy forms; CellBlock enumeration; Column sized >1 wide throws.
- **StackShapeTests** — order, axis-only consumption, derived extent, explicit-area consumed in full, child-misfit throws, Select order, 8 children, nesting beyond 8.
- **RepeatShapeTests** — mirror RepeatTests case-for-case: separator between only; trailing blank band; all-blank; empty; zero-area (timeout); zero-advance (timeout); next-item misfit stops; projection inside item propagates; atLeast; horizontal.
- **TableShapeTests** — defaults; header split; headerRows 0; empty-extent-with-header throws; header-only → empty rows; by-index; by-name case-insensitive/trimmed; unknown name lists columns; duplicate name; name without header; index out of range; explicit area; TableRows; >1 headerRows throws at construction.
- **ShapeErrorTests** — requested-vs-available in A/B; wrap with path+location; no double-wrap; names vs descriptions in paths; repeat index in path; transparent Select skipped; A1 location; Map never throws bare OutOfBoundsException.
- **ShapeInspectionTests** — Children/Name/Description enumerable without a space; one shape reused across many spaces.
- **ShapeExampleTests** — all three workbooks end-to-end (TestData copies); summary-count == details-count as the post-parse validation example.
- Regression guard: the 182 pre-existing tests pass unmodified.

## 7. Deliberately deferred

**Wave 3 (this design is shaped to accept it):** Choice(a, b); decomposition trace (hangs off ShapeContext); dry-run renderer (Apply minus step 6 over IShape.Children); unconsumed-space warnings (Advance subtraction); strategy prose in messages.
**Wave 4+:** Table tier 3; multi-row headers; capability declarations (IShape gains Requirements additively); homogeneous params Vertical; monadic sugar (only if a value-dependent format appears); deprecating the builder surface (and with it the top-level-Build and Region1/2/3 questions); GetSubspace(offset) exception inconsistency in the substrate; absolute coordinates for non-spreadsheet adapters.
