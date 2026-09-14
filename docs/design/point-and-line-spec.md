# The Point Substrate — design spec, revision 4

**Status:** for owner review. Revision 4 rebases the whole spec on the **self‑typed slicing contract** (two tiers, no erased space) and the **single generic vocabulary**. Nothing built.
**Base:** `master` @ `d06708d`, branch `experiment/point-and-line`. **Inputs:** `docs/design/substrate-inventory.md`, the compiling three‑tier prototype (session scratchpad `crtp/Program.cs`), `docs/design/spreadsheet-reader-survey.md`.
**Scope:** `Line` deferred. `IProjection` → Core deferred.

Rulings applied (owner, 2026-09-14): the strict shape (kinds live in the package that owns them; views hand back points and host no reads; the reflective binder is a backend record leaf composed through the `Table` sugar); `IsText` + `AsText` at all three levels with `Text()` kinded and relocated; `Cell<T>` retired for `Point()`; the struct named `Cell`; **`GetSubspace` returns `TSpace` — "a subspace should just be a lens into the same space"**; **no erased space and no non‑generic vocabulary**; contract first, then Point; placement pipeline and analyzers frozen; `ICells` as the canonical base.

---

## 0. Facts this spec stands on

| Fact | Where |
|---|---|
| `ISpace` has 3 members; only the indexer speaks `CellValue`. No strategy/scan/landmark interface speaks it | `ISpace.cs:11,27,34`; `ISizeStrategy.cs:7`, `IRowStrategy.cs:7`, `IRowScan.cs:27`, `IAreaScan.cs:17`, `Scans.cs:21-43` |
| `CellValue` is a 24‑byte struct; `default` is Blank | `CellValue.cs:30-46,74` |
| 9 `ISpace` implementers; **every real one already slices to its own type** (`GridSpace`→`GridSpace`, `SpreadsheetGridSpace`→itself, `WindowedSpace`→itself); the test doubles wrap and slice to themselves | `GridSpace.cs:80-86`, `SpreadsheetGridSpace.cs:39-40`, `WindowedSpace.cs:70-76`; inventory T2‑11 |
| **`BoundedSpace`/`TailSpace` are the only decorators** and the only `ISpaceChart`s; `TailSpace`'s `view` is already a real translated subspace sliced **to the inner's full measured height**, with the bound hiding rows | `BoundedSpace.cs:31,54`; `TailSpace.cs:27,44,53,105` |
| `Table(headerRows, eachRow)` is already composed: `VerticalBands(1, eachRow).AsScaffolding()` under `ColumnLabels(1)` inside `UnitProjection` | `Projection.cs:354-361`, `UnitProjection.cs:14-36` |
| The bind rung hands a `LabelMap` to a lambda **once per table application** and its typed twin flows the demand | `Projection.cs:267-286`, `Projection.Typed.cs:96-103` |
| The reflective binder resolves columns once per table and aggregates unbound members into one message; member‑type checks run at **construction** | `Projection.Binding.cs:17-56`; `RowBinding.cs:209-234` |
| `column 'Amount': ` is a prefix on the **problem text** | `Projection.Binding.cs:99,102`, `TableRow.cs:267` |
| Matching only ever sees **text** cells; a non‑text header cell becomes the empty label | `CellMatching.cs:66,88`; `LabelMap.cs:194` |
| **`ProjectionBase<TResult>` is `public`**; only its ctor is `private protected`. `Unrect.csproj` carries IVT for `Unrect.Tests` only. IVT **does** unlock a `private protected` ctor for a friend subclass (verified two‑assembly netstandard2.0 build) | `ProjectionBase.cs:141,151-154`; `Unrect.csproj:17`; inventory T2‑2 |
| Views host **36** typed accessors; `CellStrip : IReadOnlyList<CellValue>`, `TableRow.Cells`/`TryGet` and the `Table()` dictionary rung leak the struct. `CellStrip.Count` calls `BoundedSpace.WidthOf` — a view can never live in a backend | `CellStrip.cs:13,41,85-124`; `TableRow.cs:30,86-170`; inventory T2‑6 |
| `MapProjection` and `CellProjection` invoke their lambda **bare**; the engine wraps anything a `Project` throws as `the projection threw {Type}: {Message}` located at the **extent**; **`IsFault` does not list `InvalidOperationException`** | `MapProjection.cs:34`, `CellProjection.cs:26`, `ProjectionEngine.cs:197-217,288-296` |
| `PlacementStage.Of<T>` is public, `Close<T>` private; the six typed leaves have pipeline terminals, `Formula()` does not | `PlacementStage.cs:167-188,267,279` |
| **No type anywhere is named `Cell`**; the simple name occurs only as a vocabulary member, in 4 places | `Projection.cs:24`, `ProjectionBuilders.cs:291`, `PlacementStage.cs:170`, `PlacementStage.Scoped.cs:165` |
| **85 files import `using static …Projection;`** — 63 of them tests | repo grep |
| `UnrectSymbols.TryLoad` **hard‑requires** `Unrect.Core.ISpace`, and `IsSpace` is "the demand that is no demand" | `UnrectSymbols.cs:79-99,112-116` |
| `MissingCapabilityException : InvalidOperationException` and is on the fault list | `MissingCapabilityException.cs:22`, `ProjectionEngine.cs:295` |
| **The elapsed‑time Known Bug in `CLAUDE.md` is stale** — the adapter lexes `TimeSpan` to a Number of days | `ExcelDataReaderExtensions.cs:33-40` |
| **Prototype‑verified** (netstandard2.0): self‑typed slicing compiles; canonical members must sit on the **non‑generic** base or go ambiguous (CS0229/CS0121) for any type implementing two instantiations; a generic engine constrains `where TSpace : class, ISpace<TSpace>` only; a level with two parents redeclares `new`; a slice keeps its static type with no cast and no walk. Also verified: `p.Value()` infers `T` from a `Point<IValueCells<int>>`; `p.Decimal()` infers on `Point<ISheetCells>`; an erased point cannot read a kind (CS0311) | session scratchpad `crtp/Program.cs`, `infer/Program.cs` |
| Inventory sizing: engine+context+pipeline 4,042 LOC / composites+primitives+vocabulary 2,142 / typed layer 1,072 / views 1,079 / binder 1,005 / lazy extents 334 | inventory T1 b,c,d,e,g,m |

Census: 94 test files, 1,594 methods; `Mixed(object?[,])` in 47 files; six typed leaves in 35; `CellValue` in 40; `IntCell()` at 228 sites through one helper; 104 `static ISpace` fixture signatures; 102 kind pins + 26 conversion pins.

---

## 1. The contract, and what it costs

```
public interface ICells                                  // canonical, non-generic, declared ONCE
{
  Area Area { get; }
  bool IsBlank(int column, int row);
  bool IsText (int column, int row);
  string? AsText(int column, int row);
}

public interface ISpace<TSpace> : ICells where TSpace : ISpace<TSpace>
{
  TSpace GetSubspace(Offset offset, Area area);
  Point<TSpace> this[int column, int row] { get; }
}

public readonly struct Point<TSpace> where TSpace : class, ICells   // a point never slices
```

**Two tiers. There is no erased `ISpace`.** Every hop is statically typed; erasure is not one hop away, it is unspellable.

The canonical four sit on `ICells` because the prototype says they must: a type implementing two instantiations of a generic interface that declared them would see them ambiguous (CS0229/CS0121). That constraint is also the design — *the canonical surface is one surface, not one per capability.*

```
ISheetCells   : ISpace<ISheetCells>    + 6 reads, Describe, IsErrorAt, ErrorTextAt
IFormulaSpace : ISpace<IFormulaSpace>  + FormulaAt
ISpreadsheetSpace : ISheetCells, IFormulaSpace, ISpace<ISpreadsheetSpace>
                                        { new GetSubspace; new this[] }     // two parents ⇒ redeclare
IValueCells<T> : ISpace<IValueCells<T>> + ValueAt
```

**Implementation cost, counted.** One private `Slice` plus one forward per distinct return type:

| implementer | `GetSubspace` bodies | indexer bodies |
|---|---|---|
| `WindowedSpace : ISheetCells`, `GridSpace<T> : IValueCells<T>`, each test double | 1 | 1 |
| `SpreadsheetGridSpace : ISpreadsheetSpace` | 3 (1 public + 2 explicit) | 3 |

All one‑liners. C# has no covariant interface implementation, which is why the explicit forwards exist; the prototype needed 7 bodies for a three‑tier ladder, the two‑tier shape needs 6 for the richest space and 2 for everything else.

**What the contract buys** (inventory T2‑1b, now cashed):

- `SpaceCapabilities`, `ISpaceChart`, the "coordinates must not move" law, `MissingCapabilityException` and its `IsFault` entry — **all delete**, ~120 LOC and a whole fault class.
- `RequiredCapability` and the runtime mint walk — **delete**. A point is minted from a statically typed slice, always.
- `Formula()`'s and `RowWithFormula`'s runtime asks (`SpreadsheetProjections.cs:57,127`) — **delete**; they receive a real `TSpace`.
- The two‑vocabulary parity surface — **deletes with the second vocabulary** (§2).
- Rev 3 §5.4's chart‑chain frame check — **collapses to `ReferenceEquals`** (§7).

**What it costs:** the engine, 16 composites, ~11 primitives, the modifier surface and `PlacementStage<TSpace>` all go generic in `TSpace`; ~4,000–4,500 LOC rewritten in place. Plus the one true collision, §4.

---

## 2. One vocabulary

`Projection` (the static class) and `IProjection<T>` **retire**. `ProjectionBuilders<TSpace>` is the vocabulary; `PlacementStage<TSpace>` is the pipeline; the non‑generic `PlacementStage` and `ProjectionScope<TSpace>` (with `Projection.Over`) retire with them.

```
public interface IProjection                                   // tooling only; never slices
public interface IProjection<in TSpace, TResult> : IProjection
  where TSpace : class, ISpace<TSpace>
{
  ProjectionResult<TResult> Project(TSpace extent, ProjectionContext context);   // REAL, not phantom
  IProjection<TSpace, TResult> WithName(string name);
  IProjection<TSpace, TResult> WithPlacement(Placement placement);
}
```

**`in TSpace` stays, and it replaces `Demand<TSpace>`.** A helper written over the narrowest interface it reads — `IProjection<ISheetCells, decimal>` — converts contravariantly into any file whose space is more capable, so it composes into an `ISpreadsheetSpace` declaration with no ceremony. That is the whole job the witness did, done by the type system.

**`Demand<TSpace>` retires, and so does UNR001.** The witness existed because `TSpace` had to be *inferred* and a lambda body's demands were invisible to inference (inventory T2‑3b). Under `ProjectionBuilders<TSpace>` there is nothing to infer: `TSpace` is fixed lexically by the file's one import, and a leaf built there is `IProjection<TSpace, T>` whatever it reads. The cost is stated plainly: **the type now records the space the file was written over, not the narrowest space the declaration needs.** UNR001 ("this scope carries a demand nobody makes") cannot be computed from that and retires — which also disposes of inventory T2‑3's false positive by deletion rather than by a symbol patch. A shared helper still states its minimum, in its constraint: `static IProjection<TSpace,T> Helper<TSpace>(…) where TSpace : class, ISpace<TSpace>, ISheetCells`.

A helper indifferent to the space is a generic method under `where TSpace : class, ISpace<TSpace>`; it can then use the canonical four and nothing else, which is the right default.

**Every file names its space once.** 85 import sites re‑scope (63 in tests, 9 scripts). Fixtures return `IValueCells<int>` / `IValueCells<object?>` / `ISheetCells`.

---

## 3. `Unrect.Core`

```
ICells.cs    NEW      the canonical four
ISpace.cs    REWRITTEN ISpace<TSpace> : ICells — GetSubspace + the indexer
Point.cs     NEW
Area / Offset / Size / OutOfBoundsException / the 11 strategy, scan and landmark interfaces / Scans   kept
  — every one of them retyped ISpace -> ICells (inventory T2-12: none needs TSpace)
CellValue.cs, CellKind.cs, CellError.cs   DELETED -> Unrect.Spreadsheets
Line, Orientation   not in this arc
```

**Contract laws**, tested at every door:

1. `AsText(c,r) is null` **⟺** `IsBlank(c,r)`.
2. `IsText(c,r)` is true **iff the cell's canonical text is its own value**. False for blank, false where `AsText` renders. `IsText ⇒ !IsBlank`.
3. Every member throws `OutOfBoundsException` off the edge, **including the indexer, eagerly** (`SpaceContractTests.cs:82-85` pins it; a lazily‑minted locator would move the overrun out of the strategy discovering it into a user lambda).
4. `GetSubspace` returns `TSpace`. A subspace is a lens into the same space, never a weaker one.
5. Nothing in Core requires a capability.

**`Point<TSpace>`** — `Space`, `Column`, `Row`; `IsBlank`, `IsText` (properties, a field test); **`AsText()` a method** (it may allocate). No indexer, no `GetSubspace`, no `As<>`/`Cast<>`. 16 bytes on x64. Equality is **address equality**, `IEquatable<Point<TSpace>>`, documented in as many words — the type it replaces had value equality (`CellValue.cs:226`) and that is the one place a reader can be misled.

`Point` earns Core through the indexer; the charter is unamended.

---

## 4. The bound, re‑homed

`BoundedSpace`/`TailSpace`/`ILazyExtent` cannot be self‑typed: a decorator's `GetSubspace` must return the *inner's* type. This is the one true collision, and it resolves by the split the inventory found already latent in `TailSpace`'s fields.

```
Unrect/Projections/Bound.cs   NEW (replaces BoundedSpace.cs + TailSpace.cs + ILazyExtent, 334 -> ~150 LOC)

internal sealed class Bound
{
  internal int Width { get; }                    // Scan.Width — free, settled before any row is read
  internal bool HasRow(int row);                 // advances the scan only as far as it takes to say
  internal Size ForceResolved();                 // reads the scan to exhaustion
  internal Bound Shifted(int rows);              // TailSpace's rowShift, as a value
}
```

The extent handed to `Project` is a **real `TSpace` slice**, cut to the inner's full measured height and the scan's width — exactly what `TailSpace.cs:105` already does. The bound *hides* rows above the discovered boundary. No new contract member is needed: every real space has a measured height.

`Placed` carries `(extent, bound)`; `ProjectionEngine.Exceeds`/`Bind`, `FlowState.Next`, `RepeatProjection.ItemExtent`, `BandsProjection` take the pair. `ProjectionContext` gains `Bound? Bound` so the views can see it. The scan's mutable position still belongs to one `Map` call, as it does today.

### The sharp edge: enforcement moves off the space

Today a read past a discovered bound overruns because the *space* refuses. With the bound off the space, the views must refuse instead.

| today | after |
|---|---|
| `BoundedSpace.WidthOf(space)` | `context.Bound?.Width ?? extent.Area.Width` |
| `BoundedSpace.HasRow(space, r)` | `context.Bound?.HasRow(r) ?? r < extent.Area.Height` |
| `space.Area` forces the scan | `context.Bound?.ForceResolved() ?? extent.Area.Size` |

`CellStrip<TSpace>.Count`, `CellBlock<TSpace>.Width/Height/Validate/Row/Rows`, `TableView<TSpace>.ColumnCount/RowCount/StreamRows` all read the bound from their context — which they already carry. So `Range(b => b[0, 500])` still throws `ArgumentOutOfRangeException("the block is N rows tall")`, `b.Rows` still settles the bound, and a walk still streams.

**The one behavioural change: `View.Space` forces.** Today `CellBlock.Space` hands out the `BoundedSpace` itself, so a raw `b.Space[0, 500]` overruns. With no bounded space to hand out, `Space` must return a slice of exactly the settled extent — which means **asking for `Space` settles the bound**, where today `Space.Area` settles it. That is a strictly earlier force by one member access, it preserves enforcement completely, and it is documented on the property ("asking for the extent settles the bound — the same question `Area` has always been"). `LazyForcingTests` gains one moved expectation with that comment.

The alternative — hand out the unforced full‑height slice and document that raw reads are unbounded — is cheaper and silently wrong, and is rejected.

**Outside any projection**, `space[c,r]` has no bound: the space is measured and throws at its own edge. Unchanged.

**Streaming is untouched.** `WindowedSpace` slices to itself and the locus pair rides the slice (`WindowedSpace.cs:65`). `RowsMaterialised` / `ChunkReloads` / `WindowOverruns` on the 1M‑row walk must be **identical** — that is the phase gate.

---

## 5. `Unrect` — the algebra, de‑typed

### 5.1 `GridSpace<T>`

```
GridSpace<T> : IValueCells<T>          storage T[,] + isBlank, isText, asText given at construction
static GridSpace                       the factories, names and signatures unchanged
IValueCells<T> : ISpace<IValueCells<T>> { T ValueAt(int column, int row); }
Point<IValueCells<T>>.Value()          one extension; T infers from the receiver (verified)
```

| factory | `T` | `IsBlank` | `IsText` | `AsText` |
|---|---|---|---|---|
| `Create(string?[,])` | `string?` | null/empty | `!IsBlank` | the string |
| `Create(int[,], isBlank)` / `Create(double[,], isBlank)` | `int`/`double` | predicate | **false** | invariant `ToString()` |
| `Create(object?[,])` — **the new home of `Mixed`** | `object?` | null/`""` | `v is string` | invariant `Convert.ToString` |
| `Create<T>(T[,], isBlank, isText, asText)` | `T` | given | given | given |

The `object?` row is load‑bearing: it reproduces today's matcher semantics exactly (a numeric cell is not a text cell) for the 47 files that use `Mixed`. A `string?[,]` grid would not.

**One member, one word.** `Value()` *is* `GridSpace<int>`'s `Integer()`; there is no copy of the spreadsheet's six. `Value()` is total and returns the stored `T` even where the grid calls the cell blank; blankness is asked separately.

`IntCell()` becomes one line (§5.3), and the 104 fixture signatures become `IValueCells<int>` / `IValueCells<object?>`.

### 5.2 Views

`CellStrip<TSpace>`, `CellBlock<TSpace>`, `TableRow<TSpace>`, `TableView<TSpace>`, all `where TSpace : class, ISpace<TSpace>`.

- **Deleted:** the 36 typed accessors, `CellStrip : IReadOnlyList<CellValue>` (→ `IReadOnlyList<Point<TSpace>>`), `TableRow.Cells`, `TableRow.TryGet(out CellValue)`. ≈430 of 1,079 lines.
- **Kept:** `Space`, `Count`/`Width`/`Height`, `Location`, **`AddressOf`**, `Row`/`Column`/`Rows`/`Columns`, `StreamRows`, `Resolvable` (pure ordinal arithmetic), the bounds messages, failure composition through `Context.Failure`. `AddressOf` matters more than before: it is how a caller's own complaint still cites a cell the way the framework's do.
- **Reads become extensions on the point:** `row["Amount"].Decimal()`.
- **`MintOrigin`**: each view carries its offset within the rung's extent and mints points in the extent's frame (§7).

Rung parameter types are now unambiguous — one vocabulary, one answer: `Row(r => …)` gives `CellStrip<TSpace>`, `Range(b => …)` gives `CellBlock<TSpace>`, `Record(row => …)` and `Table(row => …)` give `TableRow<TSpace>`, `Table(view => …)` gives `TableView<TSpace>`, `Fields(…)` yields `Point<TSpace>`. Inventory T2‑9's doubled parity surface vanishes.

### 5.3 `Point()` replaces `Cell<T>`; `AsText()` is the canonical leaf

```
Point()   -> IProjection<TSpace, Point<TSpace>>     asserts 1x1: "a Point must be exactly one cell; this one is 2x1"
AsText()  -> IProjection<TSpace, string>            total; fails only on blank:
                                                    "expected a value at B4, found a blank cell"
```

`CellProjection.cs` → `PointProjection.cs`. Custom conversion is `Point().Select(f)` — diagnostically identical to today's `Cell(f)` (both invoke the lambda bare, `CellProjection.cs:26`, `MapProjection.cs:34`), and better after §7. `IntCell()` = `Point().Select(p => p.Value())`; the 56 funneled `GetString`/`GetInt` lambdas likewise; `array.linq` changes one word per read.

**`Choice(AsText(), …)` is degenerate** and is pinned as `ChoiceProjectionTests.AsTextFirstIsDegenerate`, with *narrow leaf first* in `docs/vocabulary.md`. `Text()` keeps its kind assertion and simply relocates (§6), so **none of the ~12 `Text()` pins change** and `Choice(Text(), Decimal())` still discriminates.

**Name check:** after the retirement no member is named `Cell` and no type anywhere is (§0), so a file importing the vocabulary and `using Unrect.Spreadsheets;` sees `Cell` as a **type only**. `Point()` invoked with zero type arguments cannot bind to the generic struct `Point<TSpace>`.

### 5.4 Matchers

Predicates become `Func<Point<ICells>, bool>`; strategies and landmarks take `ICells`. Every text rule gains the `IsText` guard, so behaviour is **identical**:

```
point => point.IsText && Comparison.Equals(Trimmed(point.AsText()!), needle)
```

`RowContaining`, `RowWithCell`, the column twins, `Caption`, `Field` and `LabelMap`'s header parse all behave as today; a numeric header stays unlabeled; no scan renders, so no scan allocates. Phase 1's work is **preservation pins**.

**`RowSaying(text)` / `ColumnSaying(text)`** — the same whole‑cell, trimmed, case‑insensitive comparison against **every** cell's `AsText`. Obeys the naming law: not a bare `Where`/`While`, not `Containing`. No numeric overload: that would force the declaration to know the backend's rendering rule. `Caption` and `Field` are not widened — they assert a *label*, and a rendered number is not one.

`TakeRowsToValue`/`TakeColumnsToValue` → `…ToText` (no production callers; 8 test sites).

---

## 6. `Unrect.Spreadsheets` — where the kinds live

```
Cell.cs / CellKind.cs / CellError.cs   MOVED from Core, verbatim; Get*/TryGet* and equality -> internal
ISheetCells.cs        NEW   : ISpace<ISheetCells>; six reads + Describe + IsErrorAt/ErrorTextAt
SheetGrid.cs          NEW   Cell[,] as an ISpreadsheetSpace, constructible from arrays
CellReading.cs        MOVED from Unrect
PointReads.cs         NEW   Point<TSpace>.Text()/Decimal()/Integer()/Double()/Date()/Boolean()
TypedCellProjection   MOVED — a REAL primitive here (see below)
SpreadsheetProjections / SpreadsheetProjectionBuilders<TSpace>   + six leaves, Record<T>, Table<T> rungs
SpreadsheetPipeline.cs NEW  the six pipeline terminals as extensions over PlacementStage<TSpace>.Of
Binding/RowBinding, MemberPlan, TableBinding   MOVED from Unrect
SpreadsheetGridSpace / WindowedSpace / SheetStore / IRowSource / Workbook / MapWorkbook   retyped
```

### 6.1 `ISheetCells`

```
bool TextAt    (int c, int r, out string   value, out CellProblem? problem);
bool DecimalAt (…out decimal…);  IntegerAt;  DoubleAt;  DateTimeAt;  BooleanAt;
string Describe(int c, int r);                 // "Text", "Number", "Error(#VALUE!)"
bool IsErrorAt(int c, int r);   string? ErrorTextAt(int c, int r);      // D-D
```

Today's `CellReader<T>` (`TypedCellProjection.cs:17`) promoted to interface members, with the address turned from an input thunk into a **parameter of the sentence** (`CellProblem`, §7.1): the backend builds the words, whoever knows the address renders them. That is what keeps `expected Number at B4, found Text` byte‑identical at a leaf, at a view read and at a bare read.

`ISpreadsheetSpace : ISheetCells, IFormulaSpace, ISpace<ISpreadsheetSpace>` with the two `new` redeclarations. `Workbook.Sheet` returns `ISheetCells` (D‑9) — forced now, since a declaration over a lesser interface could read nothing but text. The honest‑absence rule is preserved exactly: formulas stay absent *from the type*.

### 6.2 The six kinded leaves are real primitives

`ProjectionBase<TResult>` is public with a `private protected` ctor, and IVT opens it for a friend assembly (verified, §0). **Add `<InternalsVisibleTo Include="Unrect.Spreadsheets" />` to `Unrect.csproj`** — one line — and `TypedCellProjection<T>` moves to the backend as a real primitive.

This reverses rev 3 §4.3: there is no `Select` tax, the leaf's description is its own (`Decimal`, not a named `Select`), `OrBlank`'s `Tolerating` mechanism survives intact, and the R‑8 benchmark becomes a check rather than a veto. "Only we derive" is preserved — the ctor stays closed to everyone outside the two friend assemblies, which is what the closure was for (`ProjectionBase.cs:144-149`). Publishing a `Leaf<T>` factory is still refused: it would re‑import a reading vocabulary into `Unrect`.

**Pipeline terminals.** The six leave `PlacementStage<TSpace>`, so `OffsetBy(SkipRows(1)).Decimal()` (54 use sites, one pinned parity law) is restored by extensions over the public `Of`:

```
public static IProjection<TSpace, decimal> Decimal<TSpace>(this PlacementStage<TSpace> stage)
  where TSpace : class, ISpace<TSpace>, ISheetCells
  => stage.Of(SpreadsheetProjections.Decimal<TSpace>());
```

reached by the namespace import every spreadsheet file already has. The leaf firewall survives one layer down: six readings over six kinds, no `Long()`, no `Money()`, no `Enum<T>()`.

### 6.3 `Record<T>()` and `Table<T>()`, composed

```
Table<T>() == ProjectionBuilders<TSpace>.Table(headerRows: 1, eachRow: labels => RecordRow<T>(plan, labels))
```

and the bind rung is itself `UnitProjection` over `ColumnLabels(1)` + `VerticalBands(1, row)` — so `Table<T>()` inherits the path folding, the scaffolding fold, the `onBlank` policies and the streaming behaviour with nothing re‑implemented. `RecordRow<T>` is an `Overlay` of kinded leaves at `labels["Amount"]`, built by reflection at bind time — the spelling the rung's own doc already shows (`Projection.cs:224-227`).

| today | after |
|---|---|
| construction‑time member checks (`RowBinding.cs:209-234`) | **unchanged** — the plan is built when `Table<T>()` is called and captured in the bind closure |
| `Table<T>(bind => bind.Column(…).Ignore(…))` | **unchanged** — same `TableBinding<T>`, same moment; only what the plan compiles to changes |
| aggregated `no column binds Money.Amount or Money.Fee; …` | **preserved** — the bind lambda runs once per table application, the same moment the old binder resolved columns; it loops the members, collects the unbound and throws one failure through `LabelMap`'s header citations |
| ambiguity citation naming two header cells | **preserved** (`LabelMap.cs:150-155`) |
| subject `Table<Money>` | unchanged — the composed unit is named for it |
| `column 'Amount': ` prefix | **becomes a path segment** (D‑A): `Table<Money> -> column 'Amount' @ B2: expected Number at B2, found Text` |

D‑A applies identically to a view read, where the extension never sees the caption. `CellReadingIdentityTests`' stated law (`:26-31`) is rewritten to say the column is named by the declaration, not by the problem.

### 6.4 `AsText` rendering — the backend's documented choice

Observable only through the `AsText()` leaf and the `Saying` family, so it is a default, not a contract:

blank → `null`; text → verbatim (`IsText` true); number → exact decimal if kept else the double, invariant; temporal → `yyyy-MM-dd`, or `…THH:mm:ss` when not midnight; boolean → `TRUE`/`FALSE`; error → the canonical literal `#DIV/0!` (`CellValue.cs:296-315`).

An error cell is therefore: not blank, not text, says `#DIV/0!`, fails every kinded read with today's sentence, findable by `RowSaying` and not by `RowContaining`, and answerable by `IsErrorAt`.

Displayed text stays out of reach — ExcelDataReader has no formatter. The reader survey's conclusion stands: **keep ExcelDataReader**, nothing here depends on a reader change, and `ExcelNumberFormat` is the path to `IFormattingSpace` later.

---

## 7. Lambda‑read diagnostics

`row["Amount"].Decimal()` is an extension with no context. Left bare it reaches the engine as `the projection threw InvalidOperationException: …` located at the **extent** with a meaningless local address. It is already *absorbable* (`IsFault` omits `InvalidOperationException`) — what is lost is the sentence and the address.

### 7.1 The protocol — two small public types in `Unrect`

```
public delegate string CellProblem(string at);

public sealed class CellReadException : Exception
{
  public CellReadException(Point<ICells> at, CellProblem problem);
  public Point<ICells> At { get; }
  public CellProblem Problem { get; }
  public override string Message => Problem(At.ToString());     // the LOCAL address
}
```

In `Unrect`, not Core: Core's contracts do not speak it, and every backend publishing projection‑level reads already references `Unrect`. It derives from `Exception`, not `InvalidOperationException`, so it can never be confused with the retiring `MissingCapabilityException`. It is **not** a fault: a cell that says the wrong thing is a statement about the document, absorbable exactly as a kind failure is today. An IO failure inside a read still surfaces as `IOException` and stays a fault.

`CellProblem` is why `ISheetCells`'s reads return a sentence‑builder rather than a string: only the catching site knows the real A1, only the backend knows the words, and nothing formats unless someone asks.

### 7.2 Translation

Every site that invokes a user lambda catches exactly `CellReadException` and rethrows it as a projection failure at the enclosing path:

```
internal static T Reading<TSpace, T>(Func<T> body, TSpace extent, ProjectionContext context)
{
  try { return body(); }
  catch (CellReadException failure)
  {
    var at = ReferenceEquals(failure.At.Space, extent)
      ? ProjectionLocation.At(context.Origin + new Offset(failure.At.Column, failure.At.Row), context.Extent).A1
      : failure.At.ToString();                                   // a foreign point keeps its local address
    throw context.Failure(failure.Problem(at), extent, failure);
  }
}
```

Sites: `MapProjection` (Select), `StripProjection` (`Row`/`Column`), `BlockProjection` (`Range`), `RecordProjection`, `TableProjection`'s two lambda rungs, the layout lambda bodies, and `Fields`' consumer. Everything other than `CellReadException` keeps today's behaviour: it escapes to `ProjectionEngine.cs:197-217` and `IsFault` decides. A user bug stays a fault; a cell saying the wrong thing is a failure.

Result: `Table<Money> -> column 'Amount' @ B2: expected Number at B2, found Text` — path, real A1, backend's words, absorbable.

### 7.3 Locating the A1

**The minting law.** *A point is minted in the frame of the extent its projection was handed.* A leaf mints at `(0,0)` of its 1×1 extent; a view mints against the rung's extent with coordinates translated by its own `MintOrigin` — the same arithmetic `AddressOf` already does through `Context.Origin` (`CellStrip.cs:58,65`, `CellBlock.cs:78,90`).

Rev 3's chart‑chain frame check **collapses to `ReferenceEquals(point.Space, extent)`**: under the self‑typed contract there is no unwrapping between them, so the point's space either *is* the extent or is foreign. A foreign point — captured from elsewhere, or from a nested `Map` — still produces a projection failure at the enclosing path, carrying its local address.

A point read **outside any projection** throws a bare `CellReadException` whose `Message` renders the local address. Correct: there is no declaration to name.

A point **handed out of `Map`** (D‑5: `Fields`, `Table()`, records holding points) has no context left. `AsText()`, `IsBlank`, `IsText` and the kinded reads work while the space is alive; a failing read throws bare. Through the streaming door the lifetime rule is unchanged: a point outliving its `Workbook` throws `ObjectDisposedException`, a fault.

**Still open (parked, not settled):** absolute position lives in the context, not in the space or the point. Whether a future run‑record trace pays for provenance with a field on `Point`, an `Origin` on `ICells`, or by recording addresses as it goes is untouched by this arc.

---

## 8. Analyzers, benchmarks, tests, docs

**Analyzers — frozen, but three things change.** `UnrectSymbols.TryLoad` drops `Unrect.Core.ISpace` (no erased space exists), `Demand\`1` and `ProjectionScope\`1`; keeps `IProjection\`2` (now real), `ProjectionBuilders\`1`, `PlacementStage\`1`, `IRowLandmark\`1`/`IColumnLandmark\`1`, both cursors. **UNR001 and its analyzer/code‑fix retire** with `Demand` (§2) — which is also how inventory T2‑3's false positive dies, without the `Point\`1`/`CellStrip\`1`/… symbol additions rev 3 planned. **UNR002** (a fix on CS1503) and **UNR003** (demands exceed offer — now naming both `TSpace`s where `Map`'s inference fails with CS0411) survive and matter more. No new analyzer features.

**Placement pipeline — frozen, but it goes generic.** `PlacementStage` (non‑generic) retires; `PlacementStage<TSpace>` absorbs its members; the six kinded terminals move out (§6.2); `Of<T>` stays public. No new spellings, no new operators.

**Benchmarks.** `Values` keeps its class name (CI leg + Bencher series). `Create_FromInts`/`Create_FromObjects`/`Blankness` keep their names over the new types; `Sweep_*` and `Equality` are replaced by `IsBlank_Million`, `IsText_Million`, `AsText_Million_Text`, `Decimal_Million`. New: **`Point_Mint_Million`** (does the indexer returning a struct cost anything measurable) and **`Slice_Million`** (the self‑typed slice against today's, since slicing is now on every hot path and the walk it replaces is gone). `Leaf_Via_Select_Million` is dropped — §6.2 removes the Select tax. `Retention` is structurally and numerically unchanged under relocation; **the pin is that its three floor rows and the `_Unique` controls do not move.**

**Tests.** Ten files move wholesale to `Spreadsheets/` (~4,150 lines): `CellValueTests`→`CellTests` (794), `TypedLeafTests` (292), `OrBlankTests` (307), `TypedTableTests` (662), `BinderAccessorTests` (444), `CellReadingIdentityTests` (254), `BoundRowTableTests` (609), `RowProjectionTableTests` (448), `DictionaryTableTests` (~200), `FieldsTests` (398). Seven split. The rest stay, rewritten onto `AsText()`/`Value()` over `GridSpace<object?>` — **the payoff of keeping a total canonical leaf in `Unrect`**. `ProjectionBuildersParityTests` (938 lines) **shrinks**: with one vocabulary there is no parity to assert; what survives is the spreadsheet‑side parity (`stage.Decimal() == stage.Of(Decimal())`) and a disjoint‑simple‑names assertion over the two `using static` imports.

**Docs.** `CLAUDE.md` (charter table, data‑flow, the `CellValue` bullet, the leaf firewall, the `Table` rungs, the matcher paragraph, **strike the stale elapsed‑time Known Bug**), `docs/vocabulary.md` (one vocabulary; the `AsText`/`Text` distinction; narrow‑leaf‑first), `docs/streaming.md` (`Sheet` → `ISheetCells`), `docs/benchmarking.md`.

---

## 9. Phases — contract first, every commit green

| # | Phase | What | Blast |
|---|---|---|---|
| **1** | **Preservation pins and funnels** | Pin that a numeric/temporal/boolean/error cell is *not* matched and a numeric header is unlabeled; funnel the 56 inline `Cell(v => …)` lambdas; pin the exploratory rungs' element types and the `column 'Amount': ` sentences so D‑A is a visible diff | ~25 test files, additive |
| **2** | **The contract** | `ICells` + `ISpace<TSpace>` + `Point<TSpace>` in Core; the canonical four alongside the old indexer (temporary `At`); every implementer self‑types (1–3 `GetSubspace`/indexer bodies each); strategies/scans/landmarks retyped to `ICells`; `SpaceContractTests` gains §3's five laws | Core 3 files; 9 implementers; 11 interfaces retyped; 1 test file |
| **3** | **The bound** | `Bound` replaces `BoundedSpace`/`TailSpace`/`ILazyExtent`; `Placed`/`Exceeds`/`Bind`/`FlowState`/`RepeatProjection`/`BandsProjection` take `(extent, bound)`; `ProjectionContext.Bound`; views read the bound; `View.Space` forces | ~10 files, 334 → ~150 LOC; `LazyForcingTests` one moved expectation; **gate: the streaming statistics are identical** |
| **4** | **Generic to the root** | `IProjection<in TSpace,T>.Project(TSpace, …)` real; engine, 16 composites, 11 primitives, `ProjectionBase<T>`'s clone surface, `PlacementStage<TSpace>`, views all generic; `Projection`, `IProjection<T>`, `ProjectionScope`, non‑generic `PlacementStage`, `Demand<TSpace>`, `SpaceCapabilities`, `ISpaceChart`, `MissingCapabilityException` **deleted**; UNR001 retired; 85 import sites re‑scoped | **the cost centre: ~4,000–4,500 LOC**; 63 test files re‑scope |
| **5** | **`Point()`, `AsText()`, `IsText`** | `CellProjection`→`PointProjection`; the `Cell` retirement at all 4 sites; the total `AsText()` leaf; matchers gain the `IsText` guard; `Saying`; `…ToText` | ~20 files; 6 strategy test files; phase‑1 pins must not move |
| **6** | **Both backends learn to read** | `IValueCells<T>`/`GridSpace<T>`/`Value()`; in Spreadsheets: `Cell`/`CellKind`/`CellError` **copies**, `ISheetCells`, `SheetGrid`, `PointReads`, `CellReading` copy, the IVT line, the six leaves as real primitives, their terminals, `Record<T>`/`Table<T>` as **delegating copies**; `Workbook.Sheet` → `ISheetCells` | `Unrect` +4, `Spreadsheets` +10/6 changed; **0 test changes** |
| **7** | **The read‑failure protocol** | `CellProblem`, `CellReadException`, `Reading`, the seven catch sites, `MintOrigin` | `Unrect` +2, 7 changed; 1 new test file |
| **8** | **`Unrect`'s surface de‑types** | 36 accessors and `Cells`/`TryGet` deleted; views over points; `LabelMap` over `IsText`/`AsText`; `Fields`/`Table()` element type (D‑5) | ~24 files in `Unrect`; suite still compiles — the originals are still present |
| **9** | **Test relocation** | 10 files move, 7 split, ~40 rewritten; fixtures return `IValueCells<…>`; the parity suite shrinks | ~60 test files |
| **10** | **Move‑out** | Delete from `Unrect`: the six leaves and terminals, `CellReading`, `RowBinding`/`MemberPlan`/`TableBinding`, `BindRows`/`ReadCell`, `Table<T>()`/`Table<T>(bind)` — nothing references them | ~10 files, deletions only |
| **11** | **Flip the indexer, drop `CellValue`** | `this[]` returns `Point<TSpace>`; delete `At`; delete `CellValue`/`CellKind`/`CellError` from Core | Core is then `Area`, `Offset`, `Size`, `ICells`, `ISpace<TSpace>`, `Point`, `OutOfBoundsException`, 11 interfaces, `Scans` |
| **12** | **Surfaces** | 9 scripts, `docs/vocabulary.md`, `docs/streaming.md`, `docs/benchmarking.md`, `CLAUDE.md` (incl. striking the stale elapsed‑time bug), the `Values` rows, the `Retention` no‑move check | — |

Delegating copies (6) before relocation (9) before deletion (10): **no red interval.** Phase 4 is one large but mechanical commit; it can be split by subsystem (engine → composites → primitives → views → pipeline) with the old vocabulary kept compiling until the last of them.

---

## 10. Risks

| # | Risk | Catch |
|---|---|---|
| **R‑1** | **Phase 4 is 4,000+ LOC of mechanical rewrite.** The suite is the only thing standing between it and a subtle behavioural change. | Split by subsystem; the suite must be green at each split, and the acceptance scripts (`investor-irr`, `scrubbed-k1`) must consume their whole sheet with unchanged diagnostics at the end of it. |
| **R‑2** | **The bound's enforcement moves off the space** (§4). A view that forgets to consult `context.Bound` silently reads past a discovered boundary. | A test per view member that used to call `BoundedSpace.HasRow`/`WidthOf`; a `Range(b => b[0, 500])` pin per rung; the `View.Space` forcing pin. |
| **R‑3** | **Streaming behaviour drifts** while the bound and the extent are re‑cut. | `RowsMaterialised == 1,000,001`, `ChunkReloads`, `WindowOverruns` identical on the 1M‑row walk — the phase‑3 gate. `CrossDoorDenotationTests`, `StreamingIdentityTests`. |
| **R‑4** | **The type no longer records the narrowest demand** (§2) — a helper hoisted out of a file silently keeps the file's space unless its author writes the constraint. | `in TSpace` makes the narrow version *compose*, so the incentive is right; `docs/vocabulary.md` states the helper idiom; UNR003 catches the mismatch at `Map`. |
| **R‑5** | **`IValueCells<T>`'s `Value()` must infer** from `Point<IValueCells<T>>`. | **Verified in the session prototype** (§0). |
| **R‑6** | **A lambda read escapes translation** (§7.2) and loses its path and A1. | An exhaustive test per rung, plus a reflection test that every `Func<…>`‑taking primitive routes through `Reading`. |
| **R‑7** | **`column 'Amount': ` moves into the path** (D‑A). Log greps for the old string find nothing. | ~15 pinned messages; `CellReadingIdentityTests`' law rewritten. |
| **R‑8** | **The aggregated unbound‑member diagnostic degrades** — fires per record or blames record 0. | The bind lambda runs once per table application. Pin: a `Table<T>` missing two columns produces **exactly one** failure naming both, subject `Table<Money>`, citing header cells. |
| **R‑9** | **`AsText()` degeneracy in `Choice`** — silent and successful. | The named pin; narrow‑leaf‑first in the docs. |
| **R‑10** | **Allocation and slice cost.** Slicing is now on every hot path and points are minted per read. | `Point_Mint_Million`, `Slice_Million`; `MemoryDiagnoser` on `Strategies`/`Engine` — any `Allocated` movement is a review item. |
| **R‑11** | **Points have a lifetime** (D‑5). A detached point costs a reload, or a fault after `Dispose`. | Document, don't engineer (inventory T2‑8). Pin `MapWorkbook` + `Table()` read after close. |
| **R‑12** | **Two vocabularies to import in every spreadsheet file** (the builders and the backend's). | The disjoint‑simple‑names assertion; the two‑line header in every script and in `docs/vocabulary.md`. |
| **R‑13** | **The test relocation loses coverage silently.** | Line‑multiset diff per moved file; the total test count must not fall. |
| **R‑14** | **net48.** Self‑typed generic interfaces, a generic capability interface, generic structs over constrained parameters, a delegate‑carrying exception. | The Windows‑only `-f net48` leg at phases 2, 4 and 11. |

---

## 11. Open decisions

| # | Decision | Status |
|---|---|---|
| **D‑C** | `Cell` keeps **internal** equality — the interner and the fixtures compare cells; nothing above does. | Written as recommended; **owner confirming**. |
| **D‑D** | `IsErrorAt`/`ErrorTextAt` ride on **`ISheetCells`**, not a separate interface. | Written as recommended; **owner confirming**. |
| **D‑9** | `Workbook.Sheet` returns **`ISheetCells`** — now forced, not merely preferred. | Confirmed by the owner (2026-09-14). |
| **D‑E** | **Where a point's absolute position will eventually live** — a field on `Point`, an `Origin` on `ICells`, or the trace recording addresses as it goes. This arc needs none of them. | **Open, deliberately.** Nothing here forecloses any of the three. |
| **D‑F** | Whether a **foreign‑point read** (`ReferenceEquals` fails, §7.3) should also record an `Info`. | **Open**; cheap either way, not needed. |
| **D‑10** | **`IProjection` → Core.** Revisited under a real `Project(TSpace, ProjectionContext)`: the charter argument is *unchanged in force and narrower in shape*. `TSpace` is now a Core type (`ISpace<TSpace>`), which removes one objection — but `Project` still speaks `ProjectionContext` (521 lines, three jobs, flagged for splitting) and `ProjectionResult<T>`, and `IProjection` still speaks `Placement` and `IReadOnlyList<IProjection>`. The charter would still drag most of the projection layer into Core, and the slim‑down still waits on the deferred context representation. | **Defer**, unchanged. Nothing in this arc forecloses it. |
