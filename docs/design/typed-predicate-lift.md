# The typed-predicate lift (spec §15)

Status: **built** (2026-09-17), on `experiment/point-follow-ups`. Closes the §15 addendum of
`docs/design/point-and-line-spec.md`; its soundness rule is binding and is restated below as built.

**Deviations from this memo, as built.** The `HeadingStage` hole in §5d is a message-quality hole,
not a bypass: `Heading("Q1").Until(RowWithFormula())` never compiled (`HeadingStage` derives from
`PlacementStage`, which declares no `Until`, so there was no base overload to reach), and what it
said was CS1503 about an `IRowLandmark<TSpace>` rather than the library's sentence; the refusal
twins are added, and the census still lands on 21. `Required` takes the caller's argument text
through `CallerArgumentExpression` rather than a hard-coded name, so one helper serves every typed
overload and each names its own parameter. `RowsThenColumns`/`ColumnsThenRows` are doubled four ways
rather than typed-only (erased/erased, typed/typed and the two mixed forms), so a one-import file
can pair a rule from this vocabulary with one from the calculus; only an untyped `null` literal is
ambiguous between them, exactly as it is for `Sized`. The null guard lives in `TypedPredicates.Lower`
rather than in each factory: a lowered null is a live delegate, so the erased factory's own guard
never sees it, and one check there covers every typed factory. UNR003 is unchanged in trigger — a
mis-scoped *strategy* is refused in argument position, where the compiler already names both types;
what the analyzer gained from §5e is the UNR002 demand door, which now offers the vocabulary that
carries a rule's demand as it already did for a child declaration.

A canonical predicate asks the four questions. Anything about **kind or value** is a typed
predicate and names its space. Today the second kind is unspellable: the strategy/scan/landmark
interfaces take `Plane<ISpace>`/`Point<ISpace>`, and `Point<ISpace>` answers only
`IsBlank`/`HasValue`/`IsText`/`AsText`. This lift makes `RowsWhileAny(p => p.Decimal() < 7)` and
`p.Value() < 7` spell with nothing annotated, and keeps the canonical seam erased.

---

## 1. The phantoms — verbatim, and where they live

`src/Unrect/Projections/DemandingStrategies.cs`, namespace `Unrect.Projections`, beside
`DemandingLandmarks.cs`.

```csharp
public interface ISizeStrategy<in TSpace>   where TSpace : class, ISpace { ISizeStrategy   Strategy { get; } }
public interface IOffsetStrategy<in TSpace> where TSpace : class, ISpace { IOffsetStrategy Strategy { get; } }
public interface IAreaStrategy<in TSpace>   where TSpace : class, ISpace { IAreaStrategy   Strategy { get; } }
public interface IRowStrategy<in TSpace>    where TSpace : class, ISpace { IRowStrategy    Strategy { get; } }
public interface IColumnStrategy<in TSpace> where TSpace : class, ISpace { IColumnStrategy Strategy { get; } }
```

**Not Core.** The charter test is "name the contract this type appears in". A phantom appears in
`ProjectionBuilders<TSpace>.Sized` and in `PlacementStage<TSpace>` — both in `Unrect`. Core's
contracts never speak one: the engine sees only the unwrapped canonical strategy (§3). **Not
`Unrect.Strategies`** either — it cannot see `ProjectionBuilders`, its factories are the erased
calculus by design, and the typed factories must close over the file's `TSpace`, which a static
class there cannot do. The landmark phantoms already live in `Unrect.Projections` for the same
reasons; this is one file beside them, not a new layer.

`in TSpace` is sound here (no `Plane<TSpace>` in any signature) and is the whole ergonomic payoff:
a strategy built at `ISpace` flows into every file (§4, scenario C).

**Unwrapper name: `Strategy`, on all five.** One word, matching `Landmark`; it names *what the
calculus takes*, not what the strategy answers, so per-kind names (`Size`, `Rows`, `Area`) would
read as the result and would make a shared `Required(x).Strategy` unwrap impossible.

---

## 2. Ambiguity — measured, not reasoned

The obvious design (double every factory, as `Until` is doubled) **does not compile**. Verified
against Roslyn 8.0.419 with a reduced model of the tree:

```csharp
public static class B<TSpace> {
  public static ISizeStrategy         RowsWhileAny(Func<Point<ISpace>, bool> p);   // erased
  public static ISizeStrategy<TSpace> RowsWhileAny(Func<Point<TSpace>, bool> p);   // typed
}
// using static B<ISheet>;
RowsWhileAny(p => p.Decimal() < 7);   // OK — only the typed body binds
RowsWhileAny(p => p.IsBlank);         // error CS0121: ambiguous
```

The "a type parameter is less specific" tiebreaker does **not** rescue this: for a member of a
*constructed* type the parameters are already substituted, so the compiler compares
`Func<Point<ISpace>,bool>` against `Func<Point<ISheet>,bool>` — two unrelated concrete delegate
types, neither better. (With `TSpace = ISpace` the substituted signatures are identical and the
erased one does win, which is why the failure only appears in a backend-scoped file — the
dangerous half.)

**Conclusion: at the vocabulary layer the typed factory REPLACES the erased one; it does not
double it.** A `Point<TSpace>` still answers the canonical four, so the typed factory subsumes the
erased form: `RowsWhileAny(p => p.IsBlank)` keeps compiling, and now returns `IAreaStrategy<TSpace>`.
The erased factories stay public in `Unrect.Strategies` as the calculus — helpers, tests,
composition, and the `ISpace`-level spelling — and the docs say so. Doubling stays where it is
already correct and unambiguous: **members that take a strategy**, whose arguments are named types
rather than lambdas (§5).

**Blast radius, measured:** three files in the tree import `using static Unrect.Strategies.*`
(`SizeStrategyTests`, `OffsetStrategyTests`, `LandmarkAndLiftTests`) and **none** of them also
imports `ProjectionBuilders`. No linqpad script imports the strategy statics. The cross-`using
static` competition the brief worried about does not exist in the corpus and is not created here.

---

## 3. Lowering — and why incrementality survives for free

A typed factory **does not introduce a strategy class**. It lowers the predicate and calls the
existing erased factory, then boxes the result:

```csharp
public static IAreaStrategy<TSpace> RowsWhileAny(Func<Point<TSpace>, bool> anyCell)
  => Demanding.Area<TSpace>(SizeStrategies.RowsWhileAny(TypedPredicates.Lower(anyCell)).ToAreaStrategy());

// internal, Unrect.Projections
internal static Func<Point<ISpace>, bool> Lower<TSpace>(Func<Point<TSpace>, bool> cell)
  where TSpace : class, ISpace
  => p => cell(new Point<TSpace>((TSpace)p.Space, p.Column, p.Row));

internal static Func<Plane<ISpace>, int, bool> Lower<TSpace>(Func<Plane<TSpace>, int, bool> line)
  where TSpace : class, ISpace
  => (plane, i) => line(plane.Retyped<TSpace>(), i);
```

- **The object the engine receives is the canonical strategy itself**, unchanged — the phantom is
  unwrapped at the lift (`Required(x).Strategy`), exactly as `Required(landmark).Landmark` is today,
  and never reaches `Placement`. `ProjectionEngine.cs:195` (`Placement.Area is not
  IIncrementalAreaStrategy`), `Bound`, `IRowScan`/`IAreaScan` and `Scans.Fold*` therefore see what
  they see today. **Nothing new implements `IIncremental*`, so nothing joins
  `IncrementalStrategyTests`' theory data.** That obligation is discharged by construction, and
  pinned instead as a transparency law (§7).
- **Per-point cast, deliberately, not once-per-`Compute`.** A `Compute`-level cast needs a wrapping
  strategy, and a wrapper is precisely what would drop `IIncrementalRowStrategy` (whose `BeginRows()`
  takes no plane at all — there is no `Compute` to hoist into). One `castclass` per predicate
  evaluation on a path that already does a virtual `ISpace` read is the right trade; add
  `Values.TypedPredicate_Million` beside `IsBlank_Million` to keep the number honest.
- **A failed cast is a fault.** `InvalidCastException` is already in `ProjectionEngine.IsFault`
  (`ProjectionEngine.cs:339`) and is already the established spelling — `FormulaLandmark.Formulas`
  casts `plane.Space` the same way (`SpreadsheetProjections.cs:197`). The spec's
  `EngineInvariantException` is **discharged as that cast**; no new exception type. Licence is
  unchanged from §15.2: the phantom's static type confines it to a pipeline closed over `TSpace` or
  a subtype, and every plane the engine hands down is over the root space `Map` type-checked.
- **One Core addition, internal:** `internal Plane<TOther> Retyped<TOther>()` on `Plane<TSpace>`,
  mirroring the existing `internal Plane<ISpace> Erased()` (`Plane.cs:228`) and carrying origin,
  declared extent **and the `IBound`** — so a retype never forces a lazily discovered bottom edge.
  A member on a struct, internal, under the IVT grants Core already makes.

---

## 4. Three scenarios, as a declaration would read

**A — a sheet file.** One space named once; nothing annotated below it.

```csharp
using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

var smallLots = Sized(RowsWhileAny(p => p.Kind() == CellKind.Number && p.Decimal() < 7))
  .VerticalFlow(v => new Lots(v.Next(Table<Lot>())));

var body = On(RowWithCell(p => p.Kind() == CellKind.Number)).Sized(RowsWhileAnyValue()).Of(section);
```

**B — an in-memory value grid.**

```csharp
using static Unrect.Projections.ProjectionBuilders<Unrect.Core.IValueCells<int>>;

var small = Sized(RowsWhileAny(p => p.Value() < 7)).Range(b => b.Sum());
```

**C — a shared helper at the least demanding space, composing into any file by contravariance.**

```csharp
static IAreaStrategy<ISpace> Populated() => ProjectionBuilders<ISpace>.RowsWhileAny(p => !p.IsBlank);

// in the ISheetCells file above — implicit, because ISizeStrategy<in TSpace>:
var header = Sized(Populated()).Row(r => r[0].Text());
```

Compiled end to end against a reduced model: the canonical-question lambda, the typed read, the
doubled `Sized`, and the contravariant narrowing all bind with nothing annotated.

---

## 5. The complete doubled/replaced inventory

### 5a. Replaced in place on `ProjectionBuilders<TSpace>` (erased → typed)

Already re-exported, six: `RowWhere(Func<Plane<TSpace>,int,bool>)`, `RowWithCell(Func<Point<TSpace>,bool>)`,
`ColumnWhere`, `ColumnWithCell`, `RowsWhileAny(Func<Point<TSpace>,bool>)`, `ColumnsWhileAny`.
Return types become `IRowLandmark<TSpace>` / `IColumnLandmark<TSpace>` / `IAreaStrategy<TSpace>`.

### 5b. Newly re-exported on `ProjectionBuilders<TSpace>`, typed only (the rest of §15.1)

`TakeRowsWhile(Func<Plane<TSpace>,int,bool>)` · `TakeRowsWhile(int column, Func<Point<TSpace>,int,bool>)` ·
`TakeRowsTo(Func<Plane<TSpace>,int,bool>)` · `TakeRowsWhileAll(Func<Point<TSpace>,bool>)` ·
`TakeRowsWhileAny(…)` · the five `TakeColumns…` twins · `SkipRowsWhileAll(Func<Point<TSpace>,bool>)` ·
`SkipRowsWhileAny` · `SkipColumnsWhileAll` · `SkipColumnsWhileAny` · `SelectSize(Func<Plane<TSpace>,Size>)` ·
`SelectArea` · `SelectOffset` — plus two combinators so two typed axes can meet without unwrapping:
`RowsThenColumns(IRowStrategy<TSpace>, IColumnStrategy<TSpace>)` and `ColumnsThenRows`.

The `IColumnStrategy`-receiving *extension* overloads (`rows.AllColumns()`, `columns.TakeRowsWhileAny(…)`)
stay erased and stay un-re-exported, for the reason already recorded at
`ProjectionBuilders.ReExports.cs:150-153`.

**Space predicates get typed twins too — recommended.** A `Plane<TSpace>` mints `Point<TSpace>`
from its indexer, so `RowWhere((s, row) => s[0, row].Decimal() < 7)` is the natural row-scoped form
and is otherwise unspellable; and the naming law in `docs/vocabulary.md` ("bare `Where`/`While` = a
space predicate, a cell predicate is always marked") only holds if both halves of the family move
together. Cost is one lowering helper and `Plane.Retyped`.

### 5c. Members that gain a doubled overload (named-type arguments — unambiguous)

| Site | Member | Twin | Unwrap |
|---|---|---|---|
| `ProjectionBuilders.cs:116` | `OffsetBy(IOffsetStrategy)` | `OffsetBy(IOffsetStrategy<TSpace>)` | `Step.OffsetBy(Required(offset).Strategy)` |
| `ProjectionBuilders.cs:180` | `Sized(IAreaStrategy)` | `Sized(IAreaStrategy<TSpace>)` | `Step.Sized(Required(area).Strategy)` |
| `Vocabulary.cs:61/73/88` | `Row<T>(IColumnStrategy, …)`, `Column<T>(IRowStrategy, …)`, `Range<T>(IAreaStrategy, …)` | the three `<TSpace>` twins | `Required(x).Strategy` |
| `Vocabulary.cs:670/682` | `VerticalRepeat<T>(…, IOffsetStrategy? separatedBy = null, …)`, `HorizontalRepeat<T>` | see note | `Required(x).Strategy` |
| `PlacementStage.cs:90/103` | the two stage repeats | see note | idem |
| `PlacementStage.cs:195/214/234` | stage `Row`/`Column`/`Range` | three twins | idem |
| `PlacementStage.cs:383` | `OffsetStage.Sized(IAreaStrategy)` | `Sized(IAreaStrategy<TSpace>)` | idem |
| `PlacementStage.cs:292/298, 310/316` | `Until`/`UntilColumn` | **already doubled** | — |

**Note on `separatedBy:` — the optional-parameter trap.** Doubling an overload that differs only in
an *optional* parameter's type makes `VerticalRepeat(item)` ambiguous (CS0121, verified). The typed
twin therefore makes `separatedBy` **required**: `VerticalRepeat<T>(IProjection<TSpace,T> item,
IOffsetStrategy<TSpace> separatedBy, int atLeast = 0, …)`. Verified to resolve cleanly for all of
`VerticalRepeat(item)`, `(item, erased)`, `(item, typed)` and `(item, separatedBy: typed, atLeast: 1)`.
Principled as well as necessary: the typed twin exists only to carry a demand, so it demands the
argument that carries it.

### 5d. Refusal twins (each `[Obsolete(same sentence, error: true)]`, same body)

A stage that refused only the canonical half of a doubled member would refuse in the library's
words down one overload and in the compiler's down the other — the rule already written at
`PlacementStage.cs:459-465`.

| Stage | Refusals gaining a typed twin | Census |
|---|---|---|
| `UnboundedStage` | `On(IRowLandmark)`, `On(IColumnLandmark)`, `Below`, `RightOf`, `OffsetBy` | 5 → 10 |
| `OffsetAndSizeStage` | `Sized` | 6 → 7 |
| `BoundStage` | `Sized`, `On`×2, `Below`, `RightOf`, `OffsetBy` (`Until`/`UntilColumn` already doubled) | 12 → 18 |
| `HeadingStage` | `On`×2, `Below`, `RightOf`, `OffsetBy`, `Until`, `UntilColumn`, `Sized` | 13 → 21 |

**Bug found, closed by this work.** `HeadingStage` does not double its `Until`/`UntilColumn`
refusals, and a derived refusal does not hide a base overload of a different signature — so today
`Heading("Q1").Until(RowWithFormula())` silently reaches `PlacementStage.Until(IRowLandmark<TSpace>)`
and **succeeds**, bypassing `GeometryComesBeforeTheHeadings`. Verified on a reduced model. It is a
one-line-per-member fix inside 5d; `BoundStage` got this right and `HeadingStage` was missed.

### 5e. Analyzers

`UnrectSymbols.TryLoad` loads the phantoms by metadata name and `DemandOf` chains them
(`UnrectSymbols.cs:76-88`). Add the five: `Unrect.Projections.ISizeStrategy\`1`, `IOffsetStrategy\`1`,
`IAreaStrategy\`1`, `IRowStrategy\`1`, `IColumnStrategy\`1`, each `?? SpaceArgumentOf(type, …)` in
`DemandOf`, all nullable-tolerant like the landmarks. Without this UNR003 cannot say *why* a
declaration scoped to `ISheetCells` refuses a strategy built over `ISpreadsheetSpace`. No other
analyzer code enumerates these members.

---

## 6. `ISheetCells` needs a kind *question* (companion change)

The typed lift makes kind predicates spellable but the sheet backend has nothing to ask with: the
six kinded reads throw on the wrong kind, the `…OrBlank` twins tolerate only blankness, and
`Describe` answers a string. Every parked pin below needs "is this a number" without throwing.
Recommended: `CellKind KindAt(int column, int row)` on `ISheetCells`, implemented once on
`SheetCellsBase` (the only implementation), plus `point.Kind()` in `PointReads`. This is a backend
*accessor*, which the leaf firewall explicitly permits ("adding a backend accessor does not add a
leaf"); it adds no failure vocabulary and no writer obligation. Alternative rejected: six
`TryDecimal(out …)` point-reads — six members where one suffices, and a second try-vocabulary
beside `CellProblem`'s.

---

## 7. Pins — the R-12 inventory

The suite has **no `Skip`ped tests**; the parking was done as markers, so the inventory is a grep
for re-expressed kind/value rules, not for `Skip =`.

| Site | What it lost | Rewrite |
|---|---|---|
| `src/Unrect.Tests/Strategies/IncrementalStrategyTests.cs:222-236` | MARKER (spec §15): `TryGetInt() < 7` became `int.TryParse(AsText()) < 7` | `RowsWhileAny(p => p.Kind() == CellKind.Number && p.Integer() < 7)` over `ISheetCells` (the file's space); theory data unchanged |
| `src/Unrect.Tests/Projections/NestedDiscoveryTests.cs:76-92` | "rows while any cell is a number", spelled `!IsBlank && !IsText`; the comment says the kind predicate returns with the typed layer | `RowsWhileAny(p => p.Kind() == CellKind.Number)`; also the `RowsThenColumns` twin below it |
| `src/Unrect.Tests/Strategies/RowAndColumnStrategyTests.cs:59, 215, 248` | `int.Parse(AsText()) < 3` — a value rule through the rendering | Rewrite typed, or state in one line why the text rule is the honest pin for that fixture |
| `src/Unrect.Tests/Projections/PlacementPipelineLawTests.cs:549-570` | the refusal census, and the §15 comment at :551 | New counts per 5d; extend the comment to the strategy phantoms |
| `src/Unrect.Tests/Projections/ProjectionReExportTests.cs:66-84` | pins the re-export forwards to `SizeStrategies.RowsWhileAny(...).ToAreaStrategy()` | Becomes the **transparency law**: `RowsWhileAny(p => …).Strategy` measures identically to the erased factory's result, **and** answers `is IIncrementalAreaStrategy` identically — one theory over every typed factory family |

Unaffected (they pin the erased calculus, which survives unchanged): `SizeStrategyTests`,
`OffsetStrategyTests`, `LandmarkAndLiftTests`, `MirrorLawTests`, `MatchingPreservationTests`.

New tests to add: the transparency law above; a `Point`/`Plane` retype law (`Retyped` preserves
origin, declared extent and bound, and does not force); a fault law (a phantom smuggled into the
wrong space by reflection faults with `InvalidCastException` and is **not** absorbed by
`.Optional()`); the `HeadingStage` refusal, now that it exists.

---

## 8. Docs

- **`docs/vocabulary.md`** — retitle §"### The landmark phantom" to **"### The typed phantoms — a
  demand that crosses the erased seam"**, extend it with the five strategy interfaces and the
  `Strategy` unwrapper, and state the rule there in bold: *a canonical predicate asks the four
  questions; anything about kind or value is a typed predicate and names its space.* Update the two
  places that promise erasure: the Extent table row at line 177 (`p` is now
  `Func<Point<TSpace>, bool>`) and the Matchers paragraph at lines 183-185.
- **`CLAUDE.md`** — the `Unrect.Strategies` row (say the erased factories are the calculus and the
  typed vocabulary lives on `ProjectionBuilders<TSpace>`); the "Matchers and their lifts" bullet
  (the landmark phantom is now the *typed* phantom family, seven interfaces); remove the §15 item
  from "Deferred out of this arc"; drop "the §15 typed-predicate lift" from the Roadmap line; add
  `ISheetCells.KindAt` to the `Unrect.Spreadsheets` row if §6 is taken.
- **`docs/design/point-and-line-spec.md`** — a one-line status note on §15 pointing here.

---

## 9. Phased implementation order

1. **Seam, invisible.** `DemandingStrategies.cs` (five interfaces), internal `Demanding` boxes,
   internal `TypedPredicates.Lower` pair, internal `Plane<TOther> Retyped<TOther>()` in Core.
   Nothing public changes; builds and stays green.
2. **The doubled members and refusal twins** (5c, 5d) + the census update, including the
   `HeadingStage` hole. Green; the new overloads are exercised by a hand-written phantom in the
   suite because no factory produces one yet.
3. **The typed factories** on `ProjectionBuilders<TSpace>` (5a replace, 5b add). Fixes the in-tree
   return types that widen (`NestedDiscoveryTests.NumericRowsOnly` and friends).
4. **`ISheetCells.KindAt` + `PointReads.Kind()`** (§6), if the owner says go.
5. **The pins** (§7) — rewrites, the transparency law, the fault law.
6. **Analyzers** (5e), **docs** (§8), **benchmark row** (`Values.TypedPredicate_Million`).

Phases 1–3 are the lift; 4–6 close it. Each builds; run
`dotnet build src/Unrect.sln -v q --no-incremental` per phase, and the streaming gate once, at 3.

---

## Decisions for the owner

1. **Replace, don't double, at the vocabulary layer.** The typed factory takes the name on
   `ProjectionBuilders<TSpace>` and the erased factory stays only in `Unrect.Strategies`.
   This is forced by CS0121 (§2), but it is also a *breaking change* to a published name: a call
   site passing an explicit `Func<Point<ISpace>, bool>` variable, or assigning the result to a bare
   `IAreaStrategy`, must add `.Strategy` or name `SizeStrategies`. Lambdas — every call site in the
   tree and in `linqpad/` — are unaffected. Recommended; the alternative is a second name
   (`RowsWhileAnyTyped`), which is a second vocabulary.
2. **`ISheetCells` gains `KindAt`** (§6). A real interface addition, cheap in-tree (one
   implementation) but breaking for an outside implementer, and the tree has no `#if`/DIM escape.
   Recommended, because without it half the parked pins cannot be rewritten honestly and the
   flagship scenario reads `p.Describe() == "Number"`.
3. **Space predicates get typed twins too** (5b). Recommended for the reason in 5b; the only cost
   is the Core `Retyped` addition. Say no, and `Where`/`WithCell` stop being one family.

Everything else in this memo is a routine call.
