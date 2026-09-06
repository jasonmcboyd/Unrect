# Experiment: Typed Spaces — capability requirements checked by the compiler

**Status:** EXPERIMENT, on branch `experiment/typed-spaces`. Not adopted, not a decision.
Sparked by the formula-capability design conversation of 2026-09-06: a declaration that
probes formulas applied to a space that cannot carry them should fail as early as
possible — and the owner's instinct is *compile time*: "If we create a projection in
Euclidean space that creates a triangle whose angles sum to 180 degrees, that projection
wouldn't work in a non-Euclidean space and we should prevent the user from doing it at
compile time if at all possible."

Judged the way `combined-select-experiment.md` was: build enough to run the scenario
gauntlet (§6), read the resulting code as a user, and decide. Adoption, partial adoption
(runtime-fault fallback with the typed layer dropped), and full revert are all
acceptable outcomes.

## 1. The idea

Shapes gain a contravariant space parameter:

```csharp
public interface IShape<in TSpace, out T> where TSpace : ISpace
{
  // Map(TSpace space) at the application site; everything else as today.
}
```

Contravariance is the load-bearing choice: a space parameter is an *input*, so a shape
demanding less runs on a space offering more — `IShape<ISpace, T>` **is** an
`IShape<IFormulaSpace, T>` with no ceremony. Composition therefore unifies to the most
demanding child: a flow with one formula-requiring child and four plain ones is a
formula-requiring flow, and `Map(gridSpace)` on it does not compile.

## 2. The two halves, and where each is checked

**Composition (user-visible): checked by the compiler.** Variance propagates requirements
up the declaration tree. This is the half the experiment must prove ergonomic.

**Slicing (engine-internal): checked by law.** `ISpace.GetSubspace` returns `ISpace`; the
static type dies at the first slice. Rather than self-typing the substrate
(`ISpace<TSelf>`, which drags a parameter into every strategy, matcher, and view
contract — the "every contract grows a parameter" cost, rejected), the engine casts at
slice boundaries under a stated law:

> **The slicing law: slicing never changes the geometry.** A capable space's subspaces
> are capable, with translated coordinates. A slice may never invent capability its
> parent lacked nor shed what its parent had. (Forgetting is always safe and is what
> contravariance licenses; inventing is the sin.)

Pinned as a doors-style conformance theory in `SpaceContractTests` for every capable
backend. A backend that breaks the law fails its contract suite before it fails a user.

## 3. Capability transport — wrapper spaces are coordinate charts

`BoundedSpace` (the lazy-extent wrapper) wraps arbitrary inner spaces and cannot
statically implement a capability on behalf of its inner. A raw `space is IFormulaSpace`
type-test through a deferred extent would report `false` over a formula-bearing sheet —
the capability present in the geometry, invisible through the chart.

So the recipe is an unwrapping seam rather than a raw type-test:

```csharp
space.Capability<IFormulaSpace>()   // walks wrapper spaces; null when absent
```

Design questions the spike must answer: where the seam lives (extension over `ISpace` in
`Unrect` with an internal unwrap protocol wrappers implement?), whether Core needs
anything (charter says it must not), and whether the typed layer can use the seam's
answer to justify its casts.

## 4. The capability under test

`IFormulaSpace : ISpace` with `string? FormulaAt(int column, int row)` — in
`Unrect.Spreadsheets` when real; in the spike, a stub `FormulaGridSpace` test double so
the verdict is about typing, not parsing. (The real xlsx `<f>` reader is deliberately out
of scope: ExcelDataReader exposes no formulas publicly — verified by reflection probe —
so the reader is hand-rolled XML work that only happens if this experiment's design, or
the runtime-fault fallback, survives.)

Consumption sites, both spellings under test:
- Projection: `(aRow.Space as IFormulaSpace)` → becomes `aRow.Space.Capability<IFormulaSpace>()`;
  sugar `aRow.FormulaAt(column)` in the backend package. Absent capability = null (an
  honest per-cell answer).
- Boundary: `RowWithFormula()` matcher (the `WithCell` naming lineage). Absent capability
  at a boundary = FAULT, never no-match — "I couldn't look" and "I looked and it isn't
  there" must never share a spelling. In the typed design the fault becomes unreachable
  (the pairing doesn't compile), which is the point.

## 5. The law this experiment does NOT change

Capability use must stay declarative, and the generic layer never requires a capability:
nothing in Core or `Unrect` names `IFormulaSpace`. Strategies keep deciding extents from
content; a capability-aware matcher is a declaration like any other, shipped by the
backend package that owns the vocabulary.

## 6. The scenario gauntlet (the judgment evidence)

Each written end to end on the branch, then read as a user would read it:

1. **The plain parser** — the work-Claude BuyingPowerAllocation shape, exactly as today.
   MUST be spellable with zero new annotations: `Text()` and friends are
   `IShape<ISpace, T>` and run everywhere via variance. If this scenario grows a type
   argument, the experiment fails regardless of soundness.
2. **The same parser + `aRow.FormulaAt(...)`** — projection-side capability. What type
   does the declaration acquire, and does the caller annotate anything?
3. **`.On(RowWithFormula())`** — boundary-side capability. Does the modifier chain
   infer, or does the anchor site need a type argument?
4. **A mixed composite** — `VerticalFlow` with one formula-requiring child among plain
   ones. Where does the annotation land, and how bad does it read? (Known C# limit:
   lambda-internal demands don't drive inference; expect
   `VerticalFlow<IFormulaSpace, Report>(v => ...)` or an equivalent spelling. The
   gauntlet measures how often and how ugly.)
5. **The test-with-GridSpace case** — scenario 2's shape applied to a `GridSpace`:
   MUST NOT compile. The error message quality is part of the verdict.
6. **The deferred-extent case** — scenario 2 over a `.Sized(RowsWhileAnyValue())` leaf
   (a `BoundedSpace` in the engine): the capability must survive the chart via the
   transport seam.
7. **Hoisted shape libraries** — `section`, `investorDetail` locals: what do their
   declared types look like now? `var` hides it; the doc reads what tooltips show.

## 7. Judgment criteria

- Scenario 1 unchanged, or fail.
- Annotation tax: count the sites across scenarios 2–7 that need explicit type
  arguments; judge whether each reads as *stating a requirement* (acceptable — it is
  one) or as *appeasing the compiler* (failure).
- Engine damage: the retrofit's diff size and whether `ShapeEngine`/`Placed`/the views
  survive with their current shapes.
- The fallback stays priced: if the typed layer fails judgment, the runtime-fault design
  (capability-absence faults at boundary sites, nulls at projection sites, transport
  seam unchanged) is the remainder and is worth keeping regardless — §3–§5 are
  experiment-independent.

## 8. Out of scope

The xlsx formula reader; ODS; streaming-door capabilities; the formatting capability
(`GetCellStyle`/`GetNumberFormatString` are public on ExcelDataReader — noted as
nearly-free for later); `MapWorkbook` sugar and the `SpreadsheetSpace` shell decision
(paused pending this experiment, since its outcome moves both).
