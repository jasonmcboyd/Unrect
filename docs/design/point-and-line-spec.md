# The Point Substrate — design spec, revision 5

**Status:** for owner review. Revision 5 is a *reduction* of rev 4: the CRTP contract is gone, replaced by **three locator structs over one interface**. Rev 4's conclusions stand except where restated.
**Base:** branch `experiment/point-and-line` @ `e6e1d10` + the uncommitted phase‑2 build (2,322 green).
**Deferred:** `Line` (shape stated, not built); `IProjection` → Core.

Rulings applied (owner, 2026-09-14): the strict shape; `IsText`/`AsText` at all three levels with `Text()` kinded and relocated; `Cell<T>` retired for `Point()`; the struct named `Cell`; **the ladder is three locator structs — `Plane`, `Line`, `Point` — over one `ISpace`**; `Slice` is the verb; no erased vocabulary; contract first; placement pipeline and analyzers frozen.

---

## 0. Facts this revision stands on

| Fact | Where |
|---|---|
| Phase 2 as built: `ICells` (canonical four), `ISpace<TSpace>` (self‑typed slice + indexer), the old non‑generic `ISpace : ICells` kept as the bridge, `Point<TSpace>`, `CellValue.AsText()/IsText`, canonical four on all 9 spaces, `SpaceContractTests` +173, `PointTests` (11 facts), `MatchingPreservationTests` +37 | working tree, 17 files / +640 lines |
| Every real space already slices to its own type; `BoundedSpace`/`TailSpace` are the only decorators, and `TailSpace`'s `view` is a real translated subspace cut to the inner's **full measured height**, with the bound hiding rows | `GridSpace.cs:80-86`, `SpreadsheetGridSpace.cs:39-40`, `WindowedSpace.cs:70-76`, `TailSpace.cs:44,105` |
| **The streaming locus**: `WindowedSpace` passes `(Offset.Height, Area.Height)` on **every cell read**; `Anchor` unions it into `[_locusFrom,_locusTo)` while the union fits `WindowChunks × ChunkRows`, else re‑anchors, else counts a `WindowOverrun`; `Evict` refuses locus chunks as victims and falls back to LRU | `WindowedSpace.cs:65`; `SheetStore.cs:191,227-228,316-364,376-430` |
| The reader pool is keyed on `(sheetIndex, startRow)` — **chunk‑load driven, locus‑independent**. The locus affects eviction only, hence `ChunkReloads`/`WindowOverruns`/`Evictions`, and through them `RowsMaterialised` | `ReaderPool.cs:161-203`; `SheetStore.cs:260-283` |
| `ProjectionContext` accumulates `Origin` and `Advance`; `Locate(space) = ProjectionLocation.At(Origin, space.Area.Size)`; `CellStrip.AddressOf = Origin + Step(i)`; `TableRow.Resolvable` translates by `scope.CaptureOrigin.Width − Context.Origin.Width` | `ProjectionContext.cs:77,148,176`; `CellStrip.cs:58,65`; `TableRow.cs:224` |
| `ProjectionBase<TResult>` is **public** with a `private protected` ctor; IVT unlocks it for a friend subclass (verified) | `ProjectionBase.cs:141,151-154`; `Unrect.csproj:17` |
| Views host **36** typed accessors; `CellStrip : IReadOnlyList<CellValue>`, `TableRow.Cells`/`TryGet` and the `Table()` rung leak the struct; `CellStrip.Count` needs `Unrect`‑internal access | `CellStrip.cs:13,41,85-124`; `TableRow.cs:30,86-170` |
| `MapProjection`/`CellProjection` invoke their lambda bare; the engine wraps as `the projection threw {Type}: {Message}` at the **extent**; `IsFault` omits `InvalidOperationException` | `MapProjection.cs:34`, `CellProjection.cs:26`, `ProjectionEngine.cs:197-217,288-296` |
| `PlacementStage.Of<T>` public, `Close<T>` private; the six typed leaves have terminals, `Formula()` does not | `PlacementStage.cs:167-188,267,279` |
| `UnrectSymbols.TryLoad` hard‑requires `Unrect.Core.ISpace`; `IsSpace` is "the demand that is no demand" | `UnrectSymbols.cs:79-99,112-116` |
| No type anywhere is named `Cell`; the simple name occurs only as a vocabulary member, 4 places | `Projection.cs:24`, `ProjectionBuilders.cs:291`, `PlacementStage.cs:170`, `PlacementStage.Scoped.cs:165` |
| **`ISpace` occurs ~641 times across ~200 source files**; `using static …Projection;` in 85 files (63 tests) | repo grep |
| The `CLAUDE.md` elapsed‑time Known Bug is **stale** — the adapter lexes `TimeSpan` to a Number of days | `ExcelDataReaderExtensions.cs:33-40` |

---

## 1. The ladder: three locator structs over one interface

```
public interface ISpace                          // ONE interface. Root coordinates. No slicing.
{
  Area Area { get; }
  bool IsBlank(int column, int row);
  bool IsText (int column, int row);
  string? AsText(int column, int row);
}

public readonly struct Plane<TSpace> where TSpace : class, ISpace     // 2-D locator
{
  public Plane(TSpace space, Offset origin, Area area);
  public TSpace Space { get; }  public Offset Origin { get; }  public Area Area { get; }
  public Plane<TSpace> Slice(Offset offset, Area area);       // eager check vs own Area; pure arithmetic
  public Point<TSpace> this[int column, int row] { get; }     // eager check; mints ROOT = Origin + local
}

public readonly struct Point<TSpace> : IEquatable<Point<TSpace>>      // 0-D locator, as built
  where TSpace : class, ISpace
{ TSpace Space; int Column; int Row;   bool IsBlank, HasValue, IsText;   string? AsText(); }
```

Capability interfaces are **plain**, root‑coordinate, no slicing and no redeclarations:

```
ISheetCells   : ISpace   // six reads + Describe + IsErrorAt/ErrorTextAt
IFormulaSpace : ISpace   // FormulaAt
ISpreadsheetSpace : ISheetCells, IFormulaSpace
IValueCells<T>    : ISpace   // ValueAt
```

**What this deletes outright**, relative to rev 4: `ISpace<TSpace>`, every `new` redeclaration, every per‑backend slice body (2–6 per implementer, 9 implementers, and every future one), the recursive constraint `where TSpace : class, ISpace<TSpace>` threaded through the whole engine, `SpaceCapabilities`, `ISpaceChart` and its law, `MissingCapabilityException` and its `IsFault` entry, `RequiredCapability` and the mint walk.

**`Slice` is the uniform verb.** `GetSubspace` is renamed everywhere it survives — which, after this arc, is only on `Plane` (and later `Line`).

**`Line<TSpace>`, stated once so `Plane` can grow toward it** (next arc, not built): `(TSpace Space, Point<TSpace> Start, Orientation Orientation, int Length)` with `Slice(start, count)` and `this[i] → Point<TSpace>`. The owner's "starting point and length" is exactly that; `plane.Row(i)` is full‑width sugar over it. No further concept.

**Why structs matter beyond allocation:** adding a member to a struct later is **not a breaking change**. The no‑DIM rule (`CLAUDE.md`: a DIM on a published Core interface compiles everywhere and fails at run time on .NET Framework) makes every interface member a one‑way door. `Plane` and `Line` can grow; `ISpace` cannot. That is why the surface is four interface members and everything else is a locator.

**Contract laws** (tested at every door, as built):
1. `AsText(c,r) is null` ⟺ `IsBlank(c,r)`.
2. `IsText(c,r)` is true iff the cell's canonical text is its own value — false for blank, false where `AsText` renders.
3. Every `ISpace` member, and `Plane.Slice`/`Plane.this[]`, throws `OutOfBoundsException` eagerly. The checking entry is the plane; a `Point` constructed directly is unchecked and its read throws from the space.
4. Nothing in Core requires a capability.

---

## 2. The transitional rename

The old non‑generic `ISpace` (the `CellValue` indexer + `GetSubspace`) must survive until the kinded leaves and the binder leave `Unrect` — but it must vacate the name **now**, so the base can be `ISpace` from the next commit.

**Proposal: `ICellValues`.** It names exactly what it is — the interface whose indexer yields a `CellValue` — it sorts next to `CellValue` for a reader, and it dies in phase 12 with the struct.

One commit, two mechanical renames in order:

1. `ISpace` → `ICellValues` (~641 occurrences, ~200 files)
2. `ICells` → `ISpace` (14 occurrences)

Both are compiler‑verified, and **doc `cref`s are warnings‑as‑errors**, so a stale reference in a summary fails the build rather than rotting. Do them as two passes in one commit; the reverse order collides on the name.

---

## 3. What of the built phase 2 stays

| Built | Verdict |
|---|---|
| `ICells.cs` — the canonical four and their laws | **KEEP**, renamed `ISpace.cs`. Strike the paragraph explaining why the members are not on `ISpace<TSpace>` — the reason is gone; the surviving reason is simpler ("one canonical surface, not one per capability"). |
| `Point<TSpace>` (`Point.cs`) — address equality, `IsBlank`/`IsText`/`AsText()`, `ToString()` | **KEEP** verbatim; the constraint reads `where TSpace : class, ISpace` after the rename. Coordinates are now **root**, which is a documentation change only. |
| `CellValue.AsText()` / `IsText` (+47) | **KEEP** — the backend's rendering, and it travels with the struct to Spreadsheets. |
| The canonical four on all 9 spaces (`GridSpace`, `SpreadsheetGridSpace`, `WindowedSpace`, `BoundedSpace`, `TailSpace`, the 4 doubles) | **KEEP** — root‑coordinate on a root space; on the two decorators they stay as the bridge until phase 5 retires them. |
| `SpaceContractTests` (+173), the rendering pins, `MatchingPreservationTests` (+37) | **KEEP** |
| `PointTests` — 10 of 11 facts | **KEEP** |
| `ISpace<TSpace>` and its doc | **REMOVE** |
| Every self‑typed `Slice`/indexer body and explicit forward across the 9 implementers | **REMOVE** (~150 LOC, and the obligation on every future backend) |
| `PointTests.ASliceIsAnotherSpaceAndSoMintsAnotherAddress` | **REWRITE over `Plane`** in phase 3: a *slice of a plane* is the same space and mints the same root address — the opposite fact, and the better one. |

---

## 4. The engine, and where the canonical layer sits

```
IProjection<in TSpace, TResult> : IProjection  where TSpace : class, ISpace
{
  ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context);
  IProjection<TSpace, TResult> WithName(string name);
  IProjection<TSpace, TResult> WithPlacement(Placement placement);
}

projection.Map(TSpace space)  ==>  Project(new Plane<TSpace>(space, default, space.Area), …)
```

Composites slice planes (`extent.Slice(offset, area)`), which is pure struct arithmetic: no allocation, no cast, no walk. `in TSpace` stays and does `Demand<TSpace>`'s old job — a helper written over `ISheetCells` composes contravariantly into an `ISpreadsheetSpace` file. `Demand<TSpace>` and **UNR001** retire together (rev 4 §2: with `TSpace` fixed lexically there is nothing to infer, and the type records the file's space rather than the narrowest demand).

**(A) The canonical layer takes erased planes — option (ii).**

```
int SelectRows(Plane<ISpace> plane);        bool IncludesRow(Plane<ISpace> plane, int row);
int? FindRow(Plane<ISpace> plane);          Func<Point<ISpace>, bool> predicates
```

`Plane<ISheetCells>` is not a `Plane<ISpace>` (structs are invariant), so the engine re‑mints — `new Plane<ISpace>(plane.Space, plane.Origin, plane.Area)`, a three‑field struct copy, **once per strategy invocation, never per cell**. This `ISpace` is the real base contract, not the rejected erased vocabulary.

Chosen over generic strategy methods (`Size Compute<TSpace>(Plane<TSpace>)`) because (a) predicates are `Func<Point<ISpace>,bool>` either way, so a generic strategy would re‑mint anyway and buy nothing; (b) it keeps **42 strategy files non‑generic**, which is most of why phase 6 shrinks; (c) nothing leaks upward — a strategy returns `int`, `Size`, `Offset` or `bool`, never a locator. The re‑mint is the only erasure in the system and it is one‑way by construction.

---

## 5. Absolute position, answered (rev 4 §7 reduced)

Points and planes carry **root coordinates**. Everything built to compensate for their absence collapses:

| rev 4 machinery | now |
|---|---|
| `ProjectionContext.Origin`, `Advance` and their accumulation | **gone** — the context is paired with a plane that knows where it is. One of the context's three jobs (sheet position) dissolves; ~80 LOC and the `CLAUDE.md` "three jobs" note with it |
| `Locate(space) = At(Origin, space.Area.Size)` | `ProjectionLocation.At(plane)` |
| `CellStrip.AddressOf = At(Origin + Step(i))`, `CellBlock.AddressOf` | `ProjectionLocation.At(point)` |
| the minting law, `MintOrigin` on every view | **gone** — a view mints from its own plane; the coordinates are already root |
| the frame check (`ReferenceEquals`, chart chain) | **gone** |
| the foreign‑point rule | **reduced to a fact**: a point over the *same* root space is always locatable; one over another space cites its own coordinates and names its space |

§7's read‑failure protocol reduces accordingly:

```
public delegate string CellProblem(string at);

public sealed class CellReadException : Exception          // in Unrect, beside the engine
{ Point<ISpace> At { get; }  CellProblem Problem { get; }  Message => Problem(At.ToString()); }
```

and the catch, at the seven lambda sites (`MapProjection`, `StripProjection`, `BlockProjection`, `RecordProjection`, the two `Table` lambda rungs, the layout bodies):

```
catch (CellReadException failure)
{
  throw context.Failure(failure.Problem(ProjectionLocation.At(failure.At).A1), extent, failure);
}
```

No arithmetic, no check, no conditional. Anything other than `CellReadException` keeps today's behaviour and `IsFault` decides. `Point<TSpace>` gains one member — `Erased()`, a struct copy — so the exception can carry `Point<ISpace>`; a struct addition, non‑breaking.

**D‑E (where absolute position lives) is CLOSED:** in the locator. **D‑F (an `Info` for a foreign point) is MOOT:** there is no frame to mismatch, only a legitimate cross‑space read that cites itself.

---

## 6. The bound (rev 4 §4, simplified by planes)

A lazily discovered height is not a property of a locator, so the bound stays engine‑carried — and planes make it smaller:

```
internal sealed class Bound            // replaces BoundedSpace + TailSpace + ILazyExtent, 334 -> ~130 LOC
{
  internal int Width { get; }                  // the scan's width, settled before any row is read
  internal bool HasRootRow(int rootRow);       // advances the scan only as far as it takes to say
  internal Size ForceResolved();
}
```

**`Shifted`/`rowShift` disappear**: because a plane's origin is root, a bound expressed in root rows needs no re‑basing when a plane is sliced. `Placed` becomes `(Plane<TSpace> Extent, Bound? Bound)`; `Exceeds`, `Bind`, `FlowState.Next`, `RepeatProjection.ItemExtent`, `BandsProjection` take the pair. The extent is a real plane cut to the space's full measured height; the bound hides rows above the discovered boundary. The scan's mutable position still belongs to one `Map` call.

**Enforcement is the views', via `ProjectionContext.Bound`** — `CellStrip.Count`, `CellBlock.Width/Height/Validate/Row/Rows`, `TableView.ColumnCount/RowCount/StreamRows` consult it, so `Range(b => b[0,500])` still throws `ArgumentOutOfRangeException("the block is N rows tall")` and `b.Rows` still settles the bound.

**Restated over planes:** a view's `Plane` property is cut to the settled height — `_plane.Slice(default, new Area(Width, Height))` — so **asking for it settles the bound**, where today `Space.Area` settles it. One member access earlier, enforcement complete, documented on the property; `LazyForcingTests` gains one moved expectation. The alternative (hand out the full‑height plane, document that raw reads are unbounded) is cheaper and silently wrong.

---

## 7. (D) The streaming locus — the one open risk, closed

Today the hint rides on the slice: `WindowedSpace` passes `(Offset.Height, Area.Height)` on **every cell read** (`WindowedSpace.cs:65`), `Anchor` unions it into the locus, and `Evict` refuses locus chunks. With root‑coordinate reads on the root space there are no slices, so the hint is gone.

**What actually depends on it.** The pool is keyed on `(sheetIndex, startRow)` and is locus‑independent, so the locus affects **eviction only** → `ChunkReloads`, `WindowOverruns`, `Evictions`, and through them `RowsMaterialised`.

- **The 1M‑row monotone walk:** recency order equals row order, so plain LRU always evicts the chunk furthest behind — exactly what the locus would have chosen. **Statistics identical with no hint.**
- **A band sweep that fits the window:** eviction only runs at the residency cap; a band of k < `WindowChunks` chunks is never a victim while nothing else competes. **Identical with no hint.**
- **Where it diverges:** reads interleaved between a band and somewhere else — a row projection reaching back to a header chunk. The locus pins the band; LRU would evict its top. That is the case the residency law was written for (`SheetStore.cs:20-24`), and dropping it would silently change the cost model rather than fail a test.

**Design: the engine announces the band once per extent.** One optional interface in `Unrect`, detected by the engine, never required by Core:

```
namespace Unrect;   public interface ISweepAware { void Sweeping(Offset origin, Area area); }
```

`SheetStore` exposes its existing private `Anchor` through it (`WindowedSpace` — now the root space — implements it by forwarding); `ProjectionEngine` calls it where `Placed` is built, once per placement. `SheetStore.GetCell` loses its two locus parameters.

This is **less** machinery than today, not more: one announcement per rung instead of one per cell, one gate acquisition per rung instead of one per resident read (`SheetStore.cs:227-228`), and `Anchor`'s per‑cell dedup (`_oversizedFrom`/`_oversizedTo`) can go because the call is already deduplicated. Reads outside a projection simply do not announce and get LRU.

**Gate:** `RowsMaterialised == 1,000,001`, `ChunkReloads`, `WindowOverruns` and `Evictions` identical on the 1M‑row walk **and** on a `HorizontalFlow` band sweep, before and after.

---

## 8. (E) Views — minimal re‑host only

Each view is a `Plane<TSpace>` plus a context. No `MintOrigin`, no `Space` field, no origin arithmetic. `CellStrip<TSpace>` is a plane of height 1 (or width 1) until `Line` arrives next arc. The 36 typed accessors, `CellStrip : IReadOnlyList<CellValue>`, `TableRow.Cells` and `TableRow.TryGet` are deleted (≈430 of 1,079 lines); `Space`, `Count`/`Width`/`Height`, `Location`, `AddressOf`, `Row`/`Column`/`Rows`/`Columns`, `StreamRows` and `Resolvable` stay. Reads become `row["Amount"].Decimal()`. Views are dealt with categorically in a later arc.

`TableRow.Resolvable`'s column translation stays the same formula with simpler provenance: both the label scope's capture origin and the row's origin are root, so it is one subtraction with nothing accumulated.

---

## 9. Everything rev 4 settled that is unchanged

Stated by name only; the reasoning stands.

`Text()` keeps its kind assertion and relocates to Spreadsheets with the other five kinded leaves; `AsText()` is the total canonical leaf, and `Choice(AsText(), …)` is degenerate and pinned. `Cell<T>` retires for `Point()`; the struct is named `Cell` (no collision, §0). Matchers keep today's semantics via the `IsText` guard; `RowSaying`/`ColumnSaying` is the opt‑in widening; `…ToText` replaces `…ToValue`. `GridSpace<T>` + `IValueCells<T>` + one‑member `Value()`; `Create(object?[,])` is the new home of `Mixed`. `Cell`/`CellKind`/`CellError` relocate verbatim, accessors and equality internal. `ISheetCells` carries the six reads returning a `CellProblem`, plus `Describe`, `IsErrorAt`, `ErrorTextAt` (D‑D). `Workbook.Sheet → ISheetCells` (D‑9). **IVT for `Unrect.Spreadsheets`** makes the six kinded leaves real primitives — no `Select` tax; their pipeline terminals return as extensions over the public `Of`. `Record<T>()`/`Table<T>()` compose through the existing bind rung, preserving construction‑time checks, the aggregated unbound‑member message and the header citations; the caption becomes a **path segment** (D‑A). `Fields`/`Table()` yield points (D‑5). `Cell` keeps internal equality (D‑C). The analyzers are frozen: UNR001 retires, UNR002/UNR003 survive with symbol‑set edits. The reader survey stands — keep ExcelDataReader, `ExcelNumberFormat` is the later `IFormattingSpace` path. The `OfError(Other)`‑with‑no‑literal rendering (`Other`) is left for phase 7, where the error vocabulary lives.

---

## 10. Phases — every commit green

| # | Phase | Blast |
|---|---|---|
| **1 ✔** | Preservation pins and test funnels | committed `e6e1d10` |
| **2** | **Reduce and rename.** Drop `ISpace<TSpace>` and every self‑typed body; rename `ISpace`→`ICellValues` then `ICells`→`ISpace`; keep everything in §3's first block | ~200 files touched by the rename (mechanical, compiler‑verified); ~150 LOC deleted; 1 test rewritten in phase 3 |
| **3** | **`Plane<TSpace>`** + its laws (slice arithmetic, eager checks, root minting); `ProjectionLocation.At(Point)` / `At(Plane)`; the rewritten slice law | Core +1 file; 1 test file; no callers yet |
| **4** | **The bound, as a bounded plane** *(addendum §14; engine still non‑generic)*. `Bound` (root rows; `Width`, `HasRootRow`, `ForceResolved`) replaces `BoundedSpace`/`TailSpace`/`ILazyExtent`; `Plane<TSpace>` gains an optional `Bound?` field with `Width`/`HasRow` (never force) and `Area` (settles); `Placed.Extent` becomes a `Plane<ISpace>` wrapped over the engine's subspace objects; the 23 `HasRow`/`WidthOf` sites become `extent.HasRow`/`extent.Width`; `Slice(Offset)`/`Slice(Area)` convenience overloads with the check before the subtraction | ~10 engine/composite/view files, 334 → ~130 LOC; one moved `LazyForcingTests` expectation; **gate: streaming statistics identical** |
| **5** | **Canonical layer to locators.** Strategy/scan/landmark signatures take `Plane<ISpace>`; predicates `Func<Point<ISpace>,bool>`; `Scans.Fold*` read `plane.HasRow`; `IsText` guard; `Saying`; `…ToText` | 42 strategy files; 7 test files; phase‑1 pins must not move; fold‑identity suite is the pin |
| **6** | **Generic to the root.** `Project(Plane<TSpace>, …)` real; `Map` mints `Plane<TSpace>.Of(space)` at root; composites slice planes so `ICellValues.GetSubspace` and `SpaceExtensions` go; one vocabulary; delete `Projection`, `IProjection<T>`, `ProjectionScope`, non‑generic `PlacementStage`, `Demand`, `SpaceCapabilities`, `ISpaceChart`, `MissingCapabilityException`; UNR001 retires; `ProjectionContext.Origin`/`Advance` dissolve; locations move to the plane; **`ISweepAware`** lands here (the slice‑borne locus disappears in this commit); 85 import sites re‑scope | **the cost centre — see below**; 63 test files re‑scope; **gate: streaming statistics identical** |
| **7** | **Backends learn to read.** `IValueCells<T>`/`GridSpace<T>`/`Value()`; in Spreadsheets: `Cell` copies, `ISheetCells`, `SheetGrid`, `PointReads`, `CellReading` copy, the IVT line, six leaves as real primitives + terminals, `Record<T>`/`Table<T>` **delegating copies**, `Workbook.Sheet → ISheetCells` | `Unrect` +4, `Spreadsheets` +10 / 6 changed; **0 test changes** |
| **8** | **Read‑failure protocol.** `CellProblem`, `CellReadException`, `Point.Erased()`, the seven catch sites | `Unrect` +2, 7 changed; 1 new test file |
| **9** | **Surface de‑types.** `Point()` and `AsText()` leaves; the `Cell` retirement at all 4 sites; 36 accessors and `Cells`/`TryGet` deleted; views over planes; `LabelMap` over `IsText`/`AsText`; `Fields`/`Table()` element type | ~24 files; suite still compiles — the originals are still present |
| **10** | **Test relocation.** 10 files move to `Spreadsheets/`, 7 split, ~40 rewritten onto `AsText()`/`Value()`; the parity suite shrinks to one vocabulary | ~60 test files |
| **11** | **Move‑out.** Delete from `Unrect`: the six leaves and terminals, `CellReading`, `RowBinding`/`MemberPlan`/`TableBinding`, `BindRows`/`ReadCell`, `Table<T>()`/`Table<T>(bind)` — nothing references them | ~10 files, deletions only |
| **12** | **Drop the bridge.** Delete `ICellValues` and `CellValue`/`CellKind`/`CellError` from Core. Core is then `Area`, `Offset`, `Size`, `ISpace`, `Plane`, `Point`, `OutOfBoundsException`, 11 canonical interfaces, `Scans` | Core final |
| **13** | **Surfaces.** 9 scripts, `docs/vocabulary.md`, `docs/streaming.md`, `docs/benchmarking.md`, `CLAUDE.md` (incl. **striking the stale elapsed‑time bug**), the `Values` rows, the `Retention` no‑move check | — |

**Phase 6 shrinks against rev 4's ~4,000–4,500 LOC to roughly 2,800–3,200 — about 30%** — for four reasons, in order of size:

1. **No recursive constraint.** `where TSpace : class, ISpace` instead of `where TSpace : class, ISpace<TSpace>` on every composite, primitive, modifier, cursor and stage. The recursive form is where a generic rewrite fights the compiler (inference through `Layout<TSpace,T>`, `params` arrays, the clone surface on `ProjectionBase`); the plain form is a find‑and‑replace.
2. **Nothing slices through an interface.** `extent.Slice(…)` is struct arithmetic, so no call site needs the self type, and the 9 implementers + 3 doubles contribute **zero** generic ceremony instead of 2–6 bodies each.
3. **`ProjectionContext.Origin`/`Advance` dissolve** (§5) rather than being re‑parameterized, taking ~80 LOC and their call sites with them.
4. **No `MintOrigin`, no frame check, no foreign‑point branch** in the views or the read protocol (~120 LOC never written).

Delegating copies (7) before relocation (10) before deletion (11): **no red interval.** Phase 6 may be split by subsystem — engine → composites → primitives → views → pipeline — with the old vocabulary compiling until the last split.

---

## 11. Risks

| # | Risk | Catch |
|---|---|---|
| **R‑1** | **The streaming locus** (§7). The one place behaviour can change invisibly. | `RowsMaterialised`/`ChunkReloads`/`WindowOverruns`/`Evictions` identical on the monotone walk **and** a band sweep — the phase‑5 and phase‑7 gate. |
| **R‑2** | **Phase 6 is ~3,000 LOC of mechanical rewrite.** | Split by subsystem, green at each; the acceptance scripts must consume their whole sheet with unchanged diagnostics at the end. |
| **R‑3** | **The bound's enforcement is the views'** (§6). A view that forgets `context.Bound` reads past the boundary silently. | A test per view member that used to call `BoundedSpace.HasRow`/`WidthOf`; a `Range(b => b[0,500])` pin per rung; the `View.Plane` forcing pin. |
| **R‑4** | **The rename sweep** touches ~641 occurrences. | Compiler‑verified both ways; doc `cref`s are warnings‑as‑errors, so a stale reference fails the build. |
| **R‑5** | **The type no longer records the narrowest demand** (rev 4 §2); UNR001 is gone. | `in TSpace` makes the narrow helper compose, so the incentive is right; UNR003 catches the mismatch at `Map`; the helper idiom goes in `docs/vocabulary.md`. |
| **R‑6** | **A lambda read escapes translation** (§5) and loses its path and A1. | A test per rung, plus a reflection test that every `Func<…>`‑taking primitive routes through the catch. |
| **R‑7** | **`column 'Amount': ` moves into the path** (D‑A). | ~15 pinned messages; `CellReadingIdentityTests`' law rewritten. |
| **R‑8** | **Plane/point cost.** Slicing and minting are now on every hot path. | `Point_Mint_Million`, `Slice_Million`, `IsBlank_Million`, `AsText_Million_Text`, `Decimal_Million` in the `Values` family (which keeps its name and CI leg); `MemoryDiagnoser` on `Strategies`/`Engine` — any `Allocated` movement is a review item. `Retention`'s three floor rows and `_Unique` controls must not move. |
| **R‑9** | **Points have a lifetime** (D‑5) — a detached point costs a reload, or a fault after `Dispose`. | Document, don't engineer. Pin `MapWorkbook` + `Table()` read after close. |
| **R‑10** | **Test relocation loses coverage silently.** | Line‑multiset diff per moved file; the total test count must not fall. |
| **R‑11** | **net48.** Generic structs over constrained parameters, a generic capability interface, a delegate‑carrying exception. | The Windows‑only `-f net48` leg at phases 3, 6 and 12. |

---

## 12. Open decisions

| # | Decision | Status |
|---|---|---|
| **D‑G** | **The transitional name** for the old `CellValue`‑indexer interface — `ICellValues` proposed (§2). It lives from phase 2 to phase 12. | Owner's call; one word, one sweep. |
| **D‑H** | **`ISweepAware`** (§7): the name, and whether the engine may carry one optional `is` test per placement for a backend concern. The alternative is no hint and plain LRU, which is provably identical on both pinned scenarios and silently different on an interleaved read. | Recommended as written; owner's call. |
| **D‑C / D‑D / D‑9** | `Cell` keeps internal equality; error queries on `ISheetCells`; `Workbook.Sheet → ISheetCells`. | Written as recommended; **owner confirming**. |
| **D‑10** | **`IProjection` → Core.** With `Project(Plane<TSpace>, ProjectionContext)`, `Plane` and `ISpace` are Core types — but `Project` still speaks `ProjectionContext` and `ProjectionResult<T>`, and `IProjection` still speaks `Placement` and `IReadOnlyList<IProjection>`. The charter would still drag the projection layer into Core, and the slim‑down still waits on the context representation — which §5 has just made *smaller*, so the question is worth re‑asking after this arc, not during it. | **Defer**, unchanged. |

*Closed since rev 4: D‑E (absolute position lives in the locator), D‑F (moot — no frame to mismatch).*

---

## 14. Addendum — the bound rides inside the plane (supersedes §6 and R‑3)

**The premise corrected by the call sites.** No strategy calls `BoundedSpace.HasRow` or `WidthOf`: all 9 `HasRow` sites and all 14 `WidthOf` sites are in `Unrect` — `ProjectionEngine.cs:192,306,307`, `BandsProjection.cs:59,101,102`, `RepeatProjection.cs:59,193,194,205,206`, `TableProjection.cs:27`, `ColumnLabelsProjection.cs:29,35`, `WithLabelsProjection.cs:43`, `CellStrip.cs:41`, `CellBlock.cs:50,101,142`, `TableView.cs:55,123`. Strategies read `space.Area` freely (`PredicateRowLandmark.cs:25`, `TakeWhileAllColumnStrategy.cs:28`, `ColumnAccumulators.cs:19`, …), so a strategy handed a bounded space forces today and always has; on the incremental path the engine never hands one the bound — `BoundedSpace.Advance` asks `Scan.IncludesRow(Inner, row)` one row at a time against the **measured** inner (`BoundedSpace.cs:199-216`). The scan is asked one row at a time against a measured space, and the bound is the asker. `BoundedSpace` is `Bound` wearing a space's clothes; the knot is the 23 `Unrect` sites that ask **the extent** whether it has a row.

**The plane carries the bound.**

```
public readonly struct Plane<TSpace> where TSpace : class, ISpace     // + one reference field
{
  internal Plane(TSpace space, Offset origin, Area area, Bound? bound);   // unchecked, engine-only
  public int  Width   { get; }          // bound?.Width ?? Area.Width          — never forces
  public bool HasRow(int row);          // bound?.HasRootRow(Origin.Height + row) ?? row < Area.Height — never forces
  public Area Area    { get; }          // SETTLES the bound; documented as the forcing question
  public Plane<TSpace> Slice(Offset offset, Area area);   // carries the bound; checks via HasRow, not Area
  public Plane<TSpace> Slice(Offset offset);              // the rest — check BEFORE the subtraction; never forces
  public Plane<TSpace> Slice(Area area);                  // from the corner
  public Point<TSpace> this[int c, int r] { get; }        // refuses a row past the bound
}
```

A region whose bottom is not yet known is a legitimate locator state. Cost: 24 → 32 bytes, still allocation‑free; the ctor's `space.Area` dispatch goes away for engine‑made slices via the internal unchecked ctor, which also removes the phase‑3 hazard. **Struck from §6:** `ProjectionContext.Bound`, the views' `context.Bound` enforcement, the `View.Plane` forcing rule, and risk R‑3 — a view holds a plane and the plane refuses (`Range(b => b[0,500])` overruns at the plane). `Bound` keeps `int Width`, `bool HasRootRow(int)`, `Size ForceResolved()` (~130 LOC).

**The transitional plane (phase 4–5) is a RELATIVE frame.** Until phase 6 the engine still makes subspace objects with `ICellValues.GetSubspace` and wraps each as `Plane<ISpace>.Of(subspace)` — `Space` is the subspace object, `Origin` is `(0,0)` in its frame, not root. Acceptable for strategies (relative reads only); **not** for addressing: `ProjectionLocation.At(plane/point)` stays unused by the engine and `context.Origin` remains the locator of record until phase 6. `CountingSpace`/`WatermarkSpace` translate by their own origin; their assertions re‑base in phase 6, not earlier.

**Streaming gate at two commits:** phase 4 (replacing the decorators changes which rows a bound admits and when) and phase 6 (root‑coordinate reads; the slice‑borne locus at `WindowedSpace.cs:65` is gone and `ISweepAware` replaces it). Phase 5 is signature‑only and exempt. D‑H (`ISweepAware`) is a phase‑6 concern.

**Phase‑4 execution notes (2026‑09‑15).** (i) Strategies *are* handed bounded spaces today — `ProjectionEngine.cs:73` (`Placement.Offset.GetOffset(availableSpace)`) and `:117` (`Placement.Area.GetArea(inner)`) receive a `BoundedSpace`/`TailSpace` and the strategy reads `space.Area` — so until phase 5 retypes the strategies, phase 4 keeps a ~60‑LOC `BoundedView : ICellValues` adapter at that seam (Area forces exactly as `BoundedSpace.Area` did); it retires in phase 5. (ii) **Rule:** in phase 4 every plane has `Origin == (0,0)` over a real subspace object; a plane is never sliced arithmetically over a parent object, because the streaming locus rides on the subspace object's extent (`WindowedSpace.cs:65`) and an arithmetic slice would widen it and move the gate. Arithmetic slicing arrives in phase 6 with `ISweepAware`. (iii) As built: `IBound` is `HasRow(int)` / `int Force()` / `IBound Shift(int)` — the width lives on the plane (`Bounded(bound, width)`), a second copy would disagree the moment `WithLabels` narrows; the shift lives in the bound so `Plane.Origin` keeps one meaning across phases; `Slice(Offset, Area)` **drops** the bound (a request for part of the extent is not a question about the whole of it — today's `BoundedSpace.GetSubspace` behaviour), `Slice(Offset)` carries it shifted. (iv) The in‑suite streaming gate is the exact counters in `WorkbookTests` (`ChunkLoads 19 / ChunkReloads 0 / Evictions 15 / ResidentChunks 4 / PeakResidentChunks 4 / RowsMaterialised 1201 / WindowOverruns 1`) and `StreamingIdentityTests` (`0 / 1201 / 2`); the 1M‑row figure is a benchmark (`Unrect.Benchmarks/Streaming.cs`), not a test.

---

## 15. Addendum — typed predicates (amends §4; found in phase 5)

With the strategy/scan/landmark interfaces taking erased `Plane<ISpace>` and predicates `Func<Point<ISpace>,bool>`, a **kind or value predicate cannot be written**: `RowsWhileAny(v => v.Kind == CellKind.Number)`, `RowsWhileAny(v => v.TryGetInt() < 7)`, `RowWithCell(v => v.Kind == Number)` are public API today and are unspellable over a four‑member canonical point. §4's "a generic strategy would buy nothing, predicates are `Func<Point<ISpace>,bool>` either way" was right about *text* predicates and wrong about these. Phase 5 accepts the gap (pins re‑expressed canonically where honest for the fixture, else `Skip`ped naming §15); this section closes it.

**15.1 What needs a typed twin.** Cell predicates — 15 factories in 5 classes: `RowLandmarks.RowWithCell`, `ColumnLandmarks.ColumnWithCell`, `SizeStrategies.RowsWhileAny`/`ColumnsWhileAny`, `RowStrategies.TakeRowsWhile(int,…)`/`TakeRowsWhileAll`/`TakeRowsWhileAny` and the two combining overloads, the `ColumnStrategies` twins, `OffsetStrategies.SkipRowsWhileAll/Any`, `SkipColumnsWhileAll/Any`. Space predicates (`Func<Plane<ISpace>,int,bool>` / `Func<Plane<ISpace>,Size>`) — 10 factories: `RowWhere`, `ColumnWhere`, `TakeRowsWhile`, `TakeRowsTo`, `TakeColumnsWhile`, `TakeColumnsTo`, `SelectSize`, `SelectArea`, `SelectOffset`. **No twin needed** — closed over the canonical four or pure geometry: `RowContaining`/`ColumnContaining`, `RowSaying`/`ColumnSaying`, `Caption`, `Field`/`Fields`, `RowsWhileAnyValue`/`ColumnsWhileAnyValue`, `TakeRowsWhileAnyValue`/`AllValue`, `TakeRows(n)`/`TakeColumns(n)`, `SkipBlankRows`/`SkipBlankColumns`, `SkipToFirstNonBlankCell`, `WholeExtent`, `ExplicitArea`, `MaxArea`, `RowsThenColumns`/`ColumnsThenRows`, `To`/`Past`/`Then` — the majority of the surface, which is why the canonical layer stays non‑generic.

**15.2 How a typed predicate reaches the erased seam — the landmark phantom, already in the tree.** A bare cast at `Compute` is unsound because `IRowStrategy` is non‑generic: a strategy built in an `ISheetCells` file could be hoisted into an `IValueCells<int>` declaration, compile, and fault. Generic strategy interfaces cost the 42‑file layer and, because `Plane<TSpace>` is an invariant struct parameter, forbid `in TSpace` variance. The tree already solves this for landmarks: `IRowLandmark<in TSpace> where TSpace : class, ICellValues { IRowLandmark Landmark { get; } }` (`DemandingLandmarks.cs:23-38`), consumed by the doubled `PlacementStage<TSpace>.Until(IRowLandmark)` / `Until(IRowLandmark<TSpace>)` (`PlacementStage.Scoped.cs:282-302`). Extend it: `ISizeStrategy<in TSpace>`, `IOffsetStrategy<in TSpace>`, `IAreaStrategy<in TSpace>`, `IRowStrategy<in TSpace>`, `IColumnStrategy<in TSpace>` — each a single‑member unwrapper over its non‑generic form. The typed factories live on `ProjectionBuilders<TSpace>` / `SpreadsheetProjectionBuilders<TSpace>` and return the generic form; `PlacementStage<TSpace>`, `Sized`, `OffsetBy`, `Row(IColumnStrategy,…)`, `Column(IRowStrategy,…)`, `Range(IAreaStrategy,…)` gain the doubled overload, as `Until` already has.

> **The soundness rule.** A typed strategy may recover `TSpace` from the canonical plane it is handed by a **type test on `plane.Space`** — never a chart walk, never a search. It is licensed by two facts and nothing else: the strategy's static type names the `TSpace` it was created for, so the only pipeline that can consume it is closed over a `TSpace` or a subtype (contravariance); and from phase 6 every plane the engine hands down is over the **root** space, which `Map` type‑checked. A failed test is an `EngineInvariantException` — a fault, never absorbed. Erasure stays one‑way *by default*: the canonical seam speaks `Plane<ISpace>`, and the only thing that crosses back is a strategy minted with its `TSpace` in hand and statically prevented from arriving anywhere else.

**15.3 Phase.** The lift lands in **phase 7**, with the backends — a typed predicate is worth writing only when the point has typed reads (`p.Decimal()`, `p.Value()`). **Phase‑6 obligation:** keep the strategy‑taking members shaped so the doubled overload can be added without re‑opening them (do not collapse the two `Until`s; do not make `Sized`/`OffsetBy` extension methods). The cast is moot before phase 7 (no typed strategy exists to cast). The parked phase‑5 pins un‑skip in phase 7, rewritten against the typed lift in the space they belong to (`Kind == Number` → `p.IsNumber()`/`p.DecimalOrNull() is not null` over `ISheetCells` in `Unrect.Tests.Spreadsheets`; `TryGetInt() < 7` → `p.Value() < 7` over `IValueCells<int>`); each `Skip` names §15 and the phase, so the un‑skip is a grep.

**15.4 Risk R‑12 — a predicate that compiles and quietly means less.** `RowsWhileAny(p => p.AsText() == "42")` compiles over the erased point and matches a *rendered* number where the old `v.Kind == Number` meant something else. Catch: the phase‑5 parked pins are the inventory of every predicate that lost meaning; each must un‑skip in phase 7 or be deleted with a stated reason. `docs/vocabulary.md` states the rule: **a canonical predicate asks the four questions; anything about kind or value is a typed predicate and names its space.**

**Phase‑5 execution notes (2026‑09‑15).** (i) The strategies now slice planes arithmetically (`CompositeOffsetSizeStrategy`, `RowAndColumnSizeStrategy`) — a canonical `ISpace` cannot cut a subspace object. This amends §14(ii): the *read* through a translated plane is unchanged, but the *band announced* to the streaming store is the parent subspace object's, which is observable only where the first strategy of a composition reads nothing (`RowsThenColumns(TakeRows(2), TakeColumnsWhileAnyValue())` — `TakeRows(2)` announces no band, so the column scan announces the parent's). That is exactly what the engine announces from phase 6 (`ISweepAware`, once per placement), arriving one phase early at one seam; the counters on that composition over the windowed door are pinned so the number is chosen, not inherited. Phase 5 is therefore *not* exempt from the streaming gate: the four gate tests were run and are identical. `Extents` still refuses to cut a translated plane — that rule is about cutting new subspace objects, not about reading through a translated plane. (ii) `Scans.Fold*` now read `plane.HasRow(count)`: a fold over a bounded region advances the bound only as far as the fold reaches (pinned); it moved no counter because the strategy that asked for the bound had already settled it. (iii) `SpaceCapabilities`/`ISpaceChart.Underlying` retyped to `ISpace` (pure navigation; deleted in phase 6); `ISpaceChart` has no production implementer after `BoundedView`. (iv) The two formula landmarks ask `FormulaAt` at the point's coordinates — the first production code outside Core depending on root coordinates; reachable only through a hand‑sliced region until phase 6, since `Extents` gives every placed region origin `(0,0)` (a tripwire test records the phase‑5 frame and must re‑base in phase 6). (v) `TakeRowsToValue`/`TakeColumnsToValue` are removed, not renamed: `…ToText` is a different rule (whole‑cell text, trimmed, case‑insensitive, text cells only). (vi) `WholeSpace.cs` (tests) forwards the strategy entry points from a space to `Plane<ISpace>.Of(space)`, pinned per entry point.
