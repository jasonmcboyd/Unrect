# Spec: The Projection Model — spaces are the shapes; projections read them

**Status:** DRAFT for owner review, on branch `experiment/typed-spaces`. Nothing here is
implemented except what the typed-spaces spike proved (see
`typed-spaces-experiment.md`, whose gauntlet and recorded compiler behaviour this spec
builds on). This is the deepest cut since wave 2; it ships in phases, each leaving a
coherent system, hardest last and droppable.

Owner direction (2026-09-06/07): "What we've been calling Shape is a projection. The
space and its subspaces are the shapes; we're just projecting those spaces." The model
follows that sentence everywhere it leads.

## 1. The model

- A **space** is the geometric thing: a bounded 2D region of canonical values. Subspaces
  are spaces. Geometry — extent, and any capabilities — is invariant under slicing (the
  slicing law, §5). The space is the *shape*, in the geometric sense.
- A **projection** is a declarative function from a space to a value:
  `Projection<TSpace, T> ≈ TSpace → (T, consumed extent, diagnostics)`, carrying its
  placement. It is a parser combinator lifted to 2D; `Consumed` is the "rest".
- **Capabilities** are what a class of spaces can do beyond `ISpace` — interfaces a
  space implements (`IFormulaSpace`), discovered through a transport seam (§5), demanded
  by the projections that use them, discharged by the backend at `Map`. Per-file context
  a composite derives (a table's caption map) is NOT a capability — it is a projected
  value delivered through a bind (§7).
- The layering is unchanged and now better named: Core is the provider-facing contract
  (what a backend implements to join); the projection algebra is consumer-facing, built
  on it. `IProjection` does not move to Core for the same reason `IShape` never did:
  no provider implements it.

## 2. The rename — **DONE (2026-09-06, phase 2)**

Landed as specified, plus the namespace: `Unrect.Shapes` → `Unrect.Projections`, directory
`src/Unrect/Shapes/` → `src/Unrect/Projections/` (and the test suite's folder with it). The
namespace moved because it is the one place the old word would have kept contradicting the
model — `Unrect.Shapes.Projection` would have said the shapes namespace contains no shapes —
and because the churn is identical either way: every consumer file respells that one `using
static` line regardless. `shape` survives ONLY in its geometric sense ("the shape of the data",
"the shape of their cost", "a different shape of grid"), which is now precisely the model's
meaning of the word. Rode along, per owner decision: `Choice` stays doubled (the judgment is
now recorded on the typed overload); the three explicit-type-argument layout overloads are
pruned, subsumed by the witness form; `ProjectionBase<T>`'s constructor is `private protected`.

`IShape<T>` → `IProjection<T>`; the `Shape` static class → `Projection`;
`ShapeEngine`/`ShapeException`/`ShapeContext`/`ShapeDiagnostic`/`ShapeLocation` →
`Projection*` equivalents; `docs/vocabulary.md` and CLAUDE.md respelled. `MapResult`,
the views, strategies, and matchers keep their names (they never said "shape").
Clean break, no aliases — same discipline as the placement renovation, and the compiler
finds every site. This is the largest mechanical phase and the least interesting;
everything else in this spec is semantics.

## 3. The typed layer, adopted — because the model change dissolves its fatal flaw

The typed-spaces spike verdict ("do not adopt") rested on one finding: a projection
lambda's body is invisible to the type system, so the site that motivated typing —
capability use inside a `TableRows` lambda — stayed unprotected. The projection model
removes that site from the main path: **rows are projected by projections, not lambdas**
(§6), and a projection's demands live in its type. The spike's positive results carry
over unchanged:

- `IProjection<in TSpace, T>` with `TSpace : class, ISpace` — the phantom-marker
  mechanic, engine untouched, scenario 1 (a plain declaration) spelled verbatim with
  zero annotations.
- Contravariance does composition: a plain projection runs on any richer space;
  composites unify to their most demanding child; `Map(gridSpace)` on a
  formula-demanding tree does not compile.
- `.On(RowWithFormula())` and capability leaves raise demands through inference with
  nothing written.

**The one unsolved problem, stated as this spec's first implementation risk: the spike's
43 doubled public members.** A contravariant parameter cannot appear in a return type,
so the spike duplicated every modifier. That cost is unacceptable and must die before
the typed layer ships. Candidate answer: a class-based fluent surface — projections are
classes already; classes are invariant; modifiers defined once on the class (or on an
invariant builder the class exposes) return the class, and variance applies only at
interface conversion sites (`Next`, `Map`, factory parameters). Phase 1 exists to prove
or kill this. If it dies, the fallback is the runtime-fault design alone (§5 ships
regardless) and the typed layer waits.

## 4. Entry ergonomics **[owner-decided]**

Two entries, not rivals — B is sugar over A's machinery:

- **A. Imports only.** `using static Unrect.Projections.Projection;` plus the backend's
  vocabulary (`using static Unrect.Spreadsheets.SpreadsheetProjections;` brings
  `Formula()`, `RowWithFormula()`, …). Demands climb from leaves by inference; the space
  is named nowhere until `Map`.
- **B. The scoped entry.** `Projection.Over<ISpreadsheetSpace>()` returns a scope whose
  members are generic only in `TResult`, dodging C#'s no-partial-inference wall:
  `Projection.Over<ISpreadsheetSpace>().Table(row)`. Reads as "a projection over
  spreadsheet space" — the requirement in prose position. The witness-value form
  (`Over(Formulas, …)`) is the recorded alternative spelling.

**Demands root in capabilities, never vendors.** `Over<SpreadsheetSpace>` (the class)
is wrong twice over: the streaming door does not vend that class even today, and
neither does a test double. The domain's face becomes an interface bundle:

```csharp
public interface ISpreadsheetSpace : ISpace, IFormulaSpace, IFormattingSpace { }
```

Guidance: hoisted library projections demand the narrowest capability they use
(`IFormulaSpace`); application code may demand the bundle. The bundle is a versioning
commitment — adding a capability to it breaks backend implementors; acceptable pre-1.0,
recorded here.

`SpreadsheetSpace`-the-class (the delegation shell) retires into this: adapters
implement `ISpreadsheetSpace`; the concrete type goes internal or dies. This resolves
the 2026-09-06 shell finding.

## 5. The capability stack (ships in every outcome — independent of the typed layer)

Everything in this section was designed in the formula conversation and validated by
the spike; it is correct under runtime faults alone and under the typed layer alike.

- **`IFormulaSpace : ISpace`** — `string? FormulaAt(int column, int row)`, in
  `Unrect.Spreadsheets`. The file's own spelling, null for a plain value. Implemented
  by the eager door via a hand-rolled xlsx `<f>` reader (ExcelDataReader exposes no
  formulas publicly — verified by reflection probe); opt-in at creation so the default
  read pays nothing. `.xls` (BIFF RPN) and the streaming door are honest absences with
  triggers. `IFormattingSpace` follows by the same recipe (EDR's
  `GetCellStyle`/`GetNumberFormatString` are public — nearly free).
- **The slicing law:** slicing never changes the geometry. A capable space's subspaces
  are capable, coordinates translated; a slice may never invent capability its parent
  lacked nor shed what it had. Forgetting is always safe (contravariance licenses it);
  inventing is the sin. Pinned as a doors-style conformance theory in
  `SpaceContractTests` for every capable backend.
- **Transport:** `space.Capability<TCapability>()` in `Unrect`, walking the public
  `ISpaceChart { ISpace Underlying { get; } }` unwrap protocol. `BoundedSpace`
  implements it (spike-proven: a raw type-test answers false over a formula-bearing
  sheet through a deferred extent; the seam recovers it). `ISpaceChart` is
  coordinate-preserving only — a translating wrapper must implement the capability
  itself, because handing back the inner would answer about the wrong cells.
- **Absence semantics:** at a projection site, a missing capability is null (an honest
  per-cell answer). At a boundary site (a matcher, a landmark), it is a FAULT, never a
  no-match — "I couldn't look" and "I looked and it isn't there" never share a
  spelling, and `.Optional()` can never absorb a wrong backend as an absent section.
  Under the typed layer the boundary fault becomes unreachable, which is the point.
- **The capability law:** capability use stays declarative, and the generic layer never
  requires one. Nothing in Core or `Unrect` names a backend capability.
- **`Formula()` is a leaf, not an extension.** The reach-through spelling
  (`aRow.FormulaAt(…)`) is exactly what no analysis can see — the audit's
  trapped-knowledge category, the trace's opacity, the spike's blind spot, all the same
  wall. The backend ships the leaf; the extension is not built.

## 6. Row projections — the table ladder becomes one mechanism

`Table(headerRows:, eachRow:)` takes a **projection** for its row slot, applied by the
table's decomposition to each body band. The parameter is named `eachRow:`
**[owner-decided]** (`rowProjection` considered; the parameter-name family is
relational lowercase — `separatedBy:`, `orEnd:` — and the name is where the application
semantics get to speak, since a space never appears in construction syntax anywhere:
the two coherent slot forms are a description value in, or the bottom-rung escape
lambda `space => value`; there is deliberately nothing between).

**Layout states column geometry; `OrBlank` states per-field tolerance.** The
four-scenario matrix (owner-walked 2026-09-07):

1. *Headers + dense* — the bind + an `Overlay` with `.Right(captions[…])` (caption
   positions are absolute; robust to column reorder). `Table<T>()` is this cell's sugar.
2. *Headers + incomplete* — same, with `.OrBlank()` on the fields a row may omit; the
   declaration is the completeness contract, per leaf.
3. *No headers + dense* — `HorizontalFlow` of leaves, ZERO coordinates:
   silence-is-adjacency does everything. The model's best spelling, and the common case.
4. *No headers + sparse/incomplete* — `Overlay` with `.Right(n)` (structurally-fixed
   positions, worn openly; subsumes the parked positional-binder idea) + `.OrBlank()`:

```csharp
var table = Table(headerRows: 0,
  eachRow: Overlay(o => new BuyingPowerAllocationRow(
    FundCode: o.Next(Text().Right(1)),
    Primary:  o.Next(Decimal().OrBlank().Right(6)),
    Fep:      o.Next(Decimal().OrBlank().Right(9)))))
  .Below(RowContaining("ACCOUNT"))
  .Sized(RowsWhileAnyValue());
```

A flow full of `.Right(n)` or an overlay with none should look wrong on sight — flows
are relative (adjacency stories), overlays are grid-absolute (positioned stories), and
overlap (a cell's value AND its formula) is overlay-only by the layouts' own contracts.

**`OrBlank()` — a required new leaf modifier.** `Decimal()` on a blank is a kind
failure and `.Optional()` converts it to null *plus a Warning* — wrong for expected
blanks. `OrBlank()` yields null on Blank with no diagnostic and still fails loudly on
wrong kind. It is the table binding's nullable-member tolerance promoted to a leaf —
`Table<T>()`'s desugarer emits it for `Nullable<>`/`string?` members, so the two
tolerance mechanisms become one.

**The overlay is extent-agnostic; the 1×W is what `Table` hands it.** The row-ness
comes from the slicer, not the layout (the K-1 header is an overlay over a multi-row
band). Corollary kept deliberately: **multi-row records need nothing new** — if `Table`
ever slices taller bands (`rowHeight:`, or a per-record size rule), the same overlay
grammar reads the second line with `.Down(1)`. The slot was never a row projector; it
is a record projector whose records are usually one row tall.

The ladder — one mechanism at five degrees of declaredness (`TableRows*` names retire
into the `Table` family):

1. `Table<T>()` — reflection writes the bind: member name → caption (CaptionComparer),
   member type → leaf, `Nullable<>`/`string?` → `.OrBlank()`; strict one-way binding as
   today (a member's `captions[…]` miss is the found-no-column failure); construction
   validates members once, captions resolve per file.
2. `Table<T>(bind => …)` — adjust what reflection writes (`.Column`, `.Ignore`).
3. `Table(1, eachRow: captions => …)` — write the bind yourself (§7).
4. `Table(0, eachRow: …)` — headerless, plain projection slot; dictionary rung: bare
   `Table()` keeps the exploration role.
5. `Table(0, eachRow: space => …)` — the space-receiving lambda, the true bottom rung,
   opaque to every analysis and documented as such.

`Table` itself is `VerticalRepeat(eachRow)` specialized with header knowledge (caption
resolution, sizing defaults, `StreamRows`, table diagnostics) — the unification that
work Claude's original `Repeat(Row(…))` parser was reaching for.

## 7. Captions: the bind and the `CaptionMap` **[owner-decided, after due diligence]**

A caption→column map is not geometry — it is **the result of the table projecting its
own header**, per-file data needed by a declaration written once. The headered row slot
is therefore a *bind*: `Table(headerRows: 1, eachRow: captions => …)`, where

```csharp
public sealed class CaptionMap
{
  internal CaptionMap(...);                   // only Table mints one
  public int this[string caption] { get; }    // CaptionComparer rules; a miss fails with the
                                              //   header's location (the found-no-column error)
  public bool Has(string caption);
  public IReadOnlyList<string> Captions { get; }
}
```

Two arrows, two moments — the space never appears in construction syntax:

```
bind (once per file):      CaptionMap  →  IProjection<T>        // builds a DESCRIPTION
the description (per row): row band    →  (T, consumed)         // engine-applied
```

Why the bind wins, on the record:

- **Safety by arity, stronger than any capability check**: the bind cannot be invoked
  without a `CaptionMap`, and only `Table` mints one. A caption-dependent row projection
  outside a headered table is not a fault or a compile error — it is unwritable.
- **Typing is intact** (a due-diligence correction, 2026-09-07): a lambda *returning a
  projection* exposes its demands in its return type — `Func<CaptionMap,
  IProjection<IFormulaSpace, T>>` flows the formula demand into the table's type. (The
  spike's lambda-invisibility applies to value-consuming projection lambdas, whose
  *bodies* act; a description-returning lambda's result is fully visible.)
- **It is how the machine works anyway**: captions locate columns once per file; rows
  then read positionally. The bind makes the two-phase truth visible.
- Costs, stated: value-dependent structure becomes expressible
  (`captions.Has(…) ? a : b`) — the cursor-lambda clause applies verbatim (expressible
  because the API cannot prevent it, discouraged, nothing added to encourage it);
  construction moves to first-map time per file (cheap); the bound row exists only
  after the lambda runs — the standing status of every layout composite, no new
  category of darkness. Hoisted reusable rows become factories
  (`static IProjection<T> Row(CaptionMap c) => …`), the dependence in the signature.

**Rejected alternatives, with reasons:**
1. `ICaptionedSpace` as a table-provided space capability + demand discharge in
   `Table`’s signature — rejected because it mints geometry from interpretation (a
   caption map is a projection’s result, not a property of the document), and because
   its safety story is weaker than the bind’s arity. Its one advantage (a typed
   “context capability” channel) was purchased with a chart wrapper per row, transport
   obligations, and a taxonomy split. Recorded partly because the argument that first
   rejected the bind — “everything inside the lambda is invisible to typing” — was
   **false**, and the correction reversed the design.
2. Header-included row bands (each row sees header + body, ordinary `ColumnContaining`
   matching, zero new machinery) — rejected for silent mis-anchoring: without a header
   the landmark searches *data* for the caption text and can quietly bind the wrong
   column — the worst failure mode in the taxonomy.

## 8. Phases

| # | Phase | Value if stopped here |
|---|---|---|
| 1 | Kill the doubling: class-based fluent surface for `IProjection<in TSpace, T>`; scenario-1 gauntlet green | the typed layer is viable (or dead, with §5 as the remainder) |
| 2 | The rename (§2), whole corpus | the model's language everywhere; no semantics change |
| 3 | Capability stack (§5): seam, transport, slicing-law conformance theory, `Formula()` leaf + xlsx reader + fixture, `ISpreadsheetSpace`, shell retirement | capabilities ship even if everything later stops |
| 4 | `OrBlank()`; row projections: `Table(headerRows, row)` slot; positional rung | the ladder's rung 3; positional binder subsumed |
| 5 | The bind + `CaptionMap`; `Table<T>()` as the reflection desugarer | rungs 1–3; `TableRows*` retires into the `Table` family |
| 6 | Entry B (`Over<T>()` scope), backend vocabulary statics, `MapWorkbook` sugar | the announced-demand ergonomics |
| 7 | Gauntlet rerun as acceptance + the work-Claude parser in final form | the judgment evidence |

## 9. Open questions

1. Phase 1's class-based surface: does it kill the doubling completely, or only mostly —
   and is "mostly" acceptable?
2. `CaptionMap`'s home and mint: sealed, in `Unrect`, internal constructor — confirm
   nothing else ever needs to mint one (a test wanting a synthetic map argues for an
   internal factory reachable via `InternalsVisibleTo`, not a public constructor).
3. `IFormattingSpace` shape: what the formatting vocabulary is (number format string?
   style id? both?) — deferred to its own short spec when formulas land.
4. The lambda rungs' documentation: state plainly, once, that everything in a lambda is
   invisible to typing, inversion, and tooling — the standing trade, chosen at the
   cursor-lambda adoption, paid knowingly.
5. `CellValue`'s name stays (owner-reviewed 2026-09-07): Core is generic over
   *backends*, not over *domains* — the document vocabulary is the declared center, and
   the charter wording should say so.
