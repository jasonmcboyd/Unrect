# Spec: The Projection Model — spaces are the shapes; projections read them

**Status:** COMPLETE. All seven phases (§8) shipped and are recorded as built (§2, §4.1,
§5.1, §6.1) and accepted (§10); the acceptance suite and the refusals ledger
(`projection-model-refusals.md`) are both re-verified against the finished tree. What
began as the typed-spaces spike (see `typed-spaces-experiment.md`, whose gauntlet and
recorded compiler behaviour this spec builds on) is now the shipped model. See the closing
note at the end of §10 for the pre-merge documentation pass that followed acceptance.

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

### 4.1 As built (phase 6, 2026-09-10)

Both entries ship, and the audit that was supposed to be a formality produced the phase's
sharpest finding.

**The scope is `ProjectionScope<TSpace>`, and it holds eight members.** `Projection.Over<TSpace>()`
is the door and the struct is what it opens — a `readonly struct` with no state, so `default` is as
good as the factory and no member of it can fail on the scope itself. The census was decided by a
survey rather than by symmetry (probe recorded below): **only the three layouts genuinely need a
scope for inference.** `Table`'s two composing rungs, both repeats and `Choice` all infer their
demand from their arguments today and need neither witness nor scope. They are in the scope anyway,
under a rule that can be stated in one sentence — *the scope carries the factories that take
projections and build one* — because the alternative is a door that opens onto three members and
leaves a scoped declaration switching spellings halfway through. Everything else in the vocabulary
(every leaf, matcher, extent, offset, and all of `ProjectionExtensions`) is indifferent to the space
and composes in by variance, so it has no scoped spelling and needs none.

**The scope fixes the worst message in the taxonomy.** §3 recorded that a demanding child inside a
plain flow reports `CS0411` on `Next`, says nothing about capabilities, and points nowhere near the
fix — "the experiment's sharpest ergonomic finding". Inside a scope the cursor's space is already
fixed, so the same mistake is a failed *argument conversion* instead of a failed inference:

```
CS1503: Argument 1: cannot convert from 'IProjection<IFormulaSpace, string?>'
                                     to 'IProjection<Unrect.Core.ISpace, string>'
```

Both types named, at the child that raised the demand. That was not why entry B was specified, and
it is now the strongest argument for it: **scoping a declaration buys diagnosis, not just brevity.**

**A scope raises the demand of everything built through it**, whether the children needed it or not,
which is exactly the split §4 already drew: application code says `Over<ISpreadsheetSpace>()` because
"this parser is for spreadsheets" is the honest requirement, and a hoisted library projection must
*not* be written through a bundle scope, because it would demand more than it reads. The guidance is
in the type's own documentation rather than only here.

**`MapWorkbook` (in `Unrect.Spreadsheets`) is the streaming loop's body as one expression**, with
`MapWorkbookWithDiagnostics` beside it — the pairing `Map` has everywhere else, and a run over a
directory is precisely where nobody is watching. Four decisions, each recorded because each could
have gone the other way:

1. **Options are an optional parameter, not a second overload** (two methods, not four), matching the
   package's own `Create(path, sheet, caseSensitive:, isBlank:)` style.
2. **The demanding variant does not exist.** A streamed sheet reads values only, so a
   formula-demanding declaration has no capable space to be applied to; the receiver type is
   `IProjection<TResult>` and the compiler refuses `formulaProjection.MapWorkbook(…)` outright. The
   message is CS1061 with a misleading tail ("are you missing a using directive?") — recorded as the
   cost of the honest spelling, against alternatives that were a run-time fault or a file's formulas
   silently read as absent.
3. **No `MapSpreadsheet` sibling over the eager door.** The sugar exists to hide a *lifetime*; the
   eager door has none, and `projection.Map(SpreadsheetSpace.Create(path, sheet))` is already one
   expression with nothing to dispose. A sibling would only import the blankness and formula
   parameters into an overload set that had no reason for them.
4. **It lives in `SpreadsheetProjectionExtensions`, not in `SpreadsheetProjections`.** The projection
   layer already splits what a declaration *says* from what is *done* to it, and this is the second
   half of that split for spreadsheets. The cost is one namespace import — the same rule that makes
   `Map` itself need `using Unrect.Projections;`.

**The import audit passes, with one correction to how §4 says it.** Compiled with *exactly* the two
`using static` lines and nothing else, every vocabulary member is reachable: both layers' leaves,
matchers, extents, offsets, the `Formulas` witness, the layouts (witnessed and scoped), the repeats,
`Choice` and the whole `Table` family — including lambdas over `CellValue` and `CellBlock`, whose
types are inferred and so never named. What is *not* reachable that way is every modifier and every
application (`.Named`, `.On`, `.Demanding`, `.Map`), because they are extension methods: a
declaration file needs `using Unrect.Projections;` as well, and that is the standing rule at both
layers rather than a gap in the backend's story. Naming a capability — which entry B does by
construction (`Over<ISpreadsheetSpace>()`) — needs `using Unrect.Spreadsheets;` too, which is what
saying the requirement out loud costs.

**Inference survey, recorded** (probe: a demanding leaf composed by each factory with no witness and
no scope, assigned to the demanding type). Fails: `VerticalFlow`/`HorizontalFlow`/`Overlay` (CS0411 at
`Next`, whether the layout is all-demanding or mixed). Succeeds: `Table(headerRows:, eachRow:
projection)`, `Table(headerRows:, eachRow: bind)` — including a bind returning a *plain* projection
through a scope — `VerticalRepeat`, `HorizontalRepeat`, `Choice` (all-demanding and mixed alike), and
every plain composite assigned to a demanding type.

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

### 5.1 As built (phase 3, 2026-09-07)

Everything in §5 shipped. Four decisions were taken while building it; each is recorded here
because each could reasonably have gone the other way.

**Shared formulas: reconstructed, not approximated.** An xlsx writes a filled column once, as
a master carrying the text and a `ref` range, with the rest of the group as empty followers
carrying only `si`. The three candidate answers for a follower were the master's text, null,
and the shifted text. The evidence decided it: the corpus's one real Excel workbook
(`examples/scrubbed-k1.xlsx`, local-only) carries **35,089 formula cells — 14,734 written out,
706 masters, 19,452 followers, 197 array anchors**. Followers are 55% of every formula in a
file nobody edited to be difficult, so the master's text would misreport nearly all of them
and null would report them as plain values, which is what null already means. The shifter is a
reference *finder* rather than a formula parser — it steps over quoted strings, quoted sheet
names and bracketed structured/external references, then judges each remaining run, rejecting
one followed by `(`, `!` or `[` (which is what keeps `LOG10` from being column LOG row 10) —
and all 19,452 followers were cross-checked against openpyxl's `Translator`, an independent
implementation in another language, **with zero differences**. Stated boundaries: an
`<f t="array">` is spelled at its anchor and the cells it spills into carry no formula in the
file and answer null (the alternative needs a legacy-CSE-vs-dynamic-array distinction the bytes
do not reliably carry); `<f t="dataTable">` carries no expression and answers null; a follower
whose master is absent is a malformed file and throws.

**The eager door: a second factory, not a flag.** `CreateWithFormulas` rather than
`Create(..., withFormulas: true)`, because the two answers differ in their *type* — the
honest-absence rule forbids returning a space that implements `IFormulaSpace` and answers null
everywhere, and a bool cannot vary a return type. `Create` returns `ISpace`,
`CreateWithFormulas` returns `ISpreadsheetSpace`, which is also the statically-typed door the
typed layer wants.

**The shell retired completely.** `SpreadsheetSpace` is now a static factory class; the
delegation-shell instance type is gone, and the plain door hands back the `GridSpace` it always
built. Alpha break, source-compatible with everything in the tree (every call site was `var`).

**The boundary fault has a type.** `MissingCapabilityException` (in `Unrect`, beside the seam)
and `space.RequiredCapability<T>(demandedBy)` as its throwing door, added to
`ProjectionEngine.IsFault` alongside the IO failures. Without it the matcher's
`InvalidOperationException` would have been *absorbable* by `.Optional()` — the exact swap §5
forbids. Verified through the runtime path (`.On(RowWithFormula().Landmark)` over a grid).

One cost is worn openly rather than fixed: `Formula()` uses `.Named("Formula")`, because
`ProjectionBase`'s constructor is `private protected` and a backend package therefore cannot
author a projection class — its leaf inherits the description of whatever public factory built
it (`Range(1, 1)`). A description seam for backend-authored leaves is the fix if this recurs;
it was not worth expanding `Unrect`'s public surface inside a capability phase.

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

### 6.1 As built (phase 5, 2026-09-09)

The ladder is one `Table` family, clean break, no aliases; the five rungs are spelled as
above, plus `Table()` (the dictionary) and the two rung-5 lambdas
(`Table(row => …)`, `Table(view => …)`), each with its `headerRows` overload.

**Rung 1 is sugar CONCEPTUALLY, and is implemented directly — for diagnostic identity.**
The literal desugar was attempted on paper against the regression bar (the existing typed
table pins) and rejected, on four counts, each of which is a message a user reads today:

1. *The subject.* A bound member's failure says `column 'Amount': expected Number at B4,
   found Text` — the caption is the subject, because that is what a reader looks for in the
   file. A desugared `Decimal()` leaf says only `expected Number at B4, found Text`; the
   column is nowhere in it.
2. *The path.* Every binding failure is the TABLE's (`Table<Transaction>` at the table's
   own origin). Through the desugar it becomes `Table[7] -> Overlay -> Decimal#3`, which is
   a truthful description of a machine nobody declared.
3. *Aggregation.* One message lists every member that found no column — `no column binds
   Transaction.Date, Transaction.Type or Transaction.Amount` — because binding resolves all
   members before reading anything. A per-member `captions[…]` lookup reports the first and
   hides the rest, and the advice (`Bind one with Column(t => t.Date, "…")`) has no member
   to name once it is a bare caption lookup.
4. *Cost.* The most-used rung would go from one compiled materializer per row to an engine
   `Apply` per member per row (placement resolution, subspace slice, context descend), on
   the path the `Tables` benchmark family measures.

So `RowBinding` stays the implementation of rungs 1–2, and the bind (rung 3) is added
beside it. What the two share is the comparer (`CaptionComparer`) and the voice: the
`CaptionMap`'s miss and duplicate messages are the binder's own, minus the member advice a
bare caption lookup cannot give. Honesty over forced elegance — the ladder is one
mechanism in the model and two in the code, and the seam is where the diagnostics live.

**Named by the same capture as everything else.** `CallerArgumentExpression` on the bind
argument keeps the naming ladder intact without a new rule: a method group
(`Table(1, AllocationRow)`) is a bare identifier and labels every record
`'AllocationRow'`; an inline `captions => …` is not, so the record falls back to the
description of what the bind *returned* (`Overlay`), or to its own `.Named`. A hoisted
bound row is a factory anyway (§7), so the spelling that is already recommended is the one
that carries a name.

**One overload cost, recorded.** The two rung-5 lambdas take `TableRow` and `TableView`,
and a lambda body that touches nothing distinctive (`Table(x => 0)`,
`Table(x => x.Location.A1)` — both views have a `Location`) is ambiguous between them.
The compiler names both candidates (CS0121, not the useless CS0411), and typing the
parameter (`Table((TableRow r) => 0)`) resolves it. Every other shape in the family
resolves unaided, including the bind against both rung-5 lambdas — the caption map's
members exist on neither view, so the wrong candidates fail to bind and drop out.

**Rung-5 ambiguity — DECIDED (owner, 2026-09-07): the typed parameter is the answer.**
A `WholeTable(view => …)` split was proposed to dissolve the CS0121 collision on
view-agnostic lambda bodies and declined: the collision is rare, the compiler's message is
good, `Table((TableView t) => t)` self-documents at the site, and a second factory name
for the same family is the two-spellings wart the vocabulary prunes everywhere else.

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
| 1 | Kill the doubling: class-based fluent surface for `IProjection<in TSpace, T>`; scenario-1 gauntlet green — **DONE**, the doubling census (43 → 6) is recorded in §10 ("The doubling") | the typed layer is viable (or dead, with §5 as the remainder) |
| 2 | The rename (§2), whole corpus — **DONE (2026-09-06)**, as recorded in §2 | the model's language everywhere; no semantics change |
| 3 | Capability stack (§5): seam, transport, slicing-law conformance theory, `Formula()` leaf + xlsx reader + fixture, `ISpreadsheetSpace`, shell retirement — **DONE (2026-09-07)**, as built in §5.1 | capabilities ship even if everything later stops |
| 4 | `OrBlank()`; row projections: `Table(headerRows, row)` slot; positional rung — **DONE**, folded into the ladder recorded in §6.1 | the ladder's rung 3; positional binder subsumed |
| 5 | The bind + `CaptionMap`; `Table<T>()` as the reflection desugarer — **DONE (2026-09-09)**, desugar recorded as conceptual only (§6.1) | rungs 1–3; `TableRows*` retired into the `Table` family |
| 6 | Entry B (`Over<T>()` scope), backend vocabulary statics, `MapWorkbook` sugar — **DONE (2026-09-10)**, as built in §4.1 | the announced-demand ergonomics |
| 7 | Gauntlet rerun as acceptance + the work-Claude parser in final form — **DONE**, judged in §10 | the judgment evidence |

All seven phases are DONE. The campaign's numbers table (end of §10) is the receipt: the
suite grew from the 1,439-test checkpoint to 1,700 at phase 7 acceptance, both TFMs at
0 warnings under `--no-incremental` at every phase including the last.

## 9. Open questions

1. Phase 1's class-based surface: does it kill the doubling completely, or only mostly —
   and is "mostly" acceptable?
2. ~~`CaptionMap`'s home and mint~~ — **resolved (phase 5).** Sealed, in
   `Unrect.Projections` beside the views it is one of, internal constructor taking the
   `TableView` whose header it reads; `Table` is the only mint. No synthetic-map factory was
   needed: the bottom rung already vends a real view (`Table(1, table => table)`), so a test
   mints one from that — a real header, a real context, real failures.
3. `IFormattingSpace` shape: what the formatting vocabulary is (number format string?
   style id? both?) — deferred to its own short spec when formulas land.
4. The lambda rungs' documentation: state plainly, once, that everything in a lambda is
   invisible to typing, inversion, and tooling — the standing trade, chosen at the
   cursor-lambda adoption, paid knowingly.
5. `CellValue`'s name stays (owner-reviewed 2026-09-07): Core is generic over
   *backends*, not over *domains* — the document vocabulary is the declared center, and
   the charter wording should say so.

## 10. Acceptance (phase 7) — the judgment record

The evidence left the spike. `src/Unrect.Tests/Projections/ProjectionModelAcceptanceTests.cs`
(16 tests) runs the gauntlet's *runnable* scenarios that no phase suite already pinned, and its
distinctive content is whole declarations rather than mechanisms: a pre-campaign declaration
applied to two spaces, the work-Claude buying-power export read as a document, the IRR report at
the top of the table ladder, and the audited ledger through the scope. The fourteen refusals — the
half that cannot be a test, because a test project must compile — are documented verbatim in
`projection-model-refusals.md` and stay re-runnable in `spike/TypedSpacesGauntlet/MustNotCompile.cs`.
All fourteen still refuse, with the recorded messages unchanged, re-run against this tree.

The experiment's §7 criteria, revisited item by item. Honest verdicts, including the two costs that
survived.

### 1. "Scenario 1 unchanged, or fail" — **PASS, verified one last time**

A plain declaration is spelled with no type argument the vocabulary did not always have, no witness,
no scope, and the word *space* nowhere:

```csharp
IProjection<Report> report = VerticalFlow(v => new Report(
  Title: v.Next(Text()),
  Rows:  v.Next(Table<Allocation>())));
```

That is the acceptance suite's first test, and it also runs, unchanged, over a space that
implements a capability — variance, not an overload. The corpus-scale proof is stronger than any
one test and was collected as the phases landed: phases 1 and 2 changed **zero** plain declarations
(1,439 tests untouched and green through both), and the 72 explicitly-typed `IProjection<T>` sites
in the suite were never edited.

### 2. The annotation tax — **final census: two sites pay, and each states a requirement**

Everything the gauntlet marked TAX in scenarios 2–7 is gone except two entries, and neither is a
type argument written to appease inference:

| Site | Final cost | Reads as |
|---|---|---|
| A capability leaf (`Formula()`), a capability matcher (`.On(RowWithFormula())`), any chain over either | **zero** | the demand climbs by inference; the declaration is written as if untyped |
| A *mixed* layout — one demanding child among plain ones | **one word**: `VerticalFlow(Formulas, v => …)` or `Over<ISpreadsheetSpace>()` once for the declaration | a requirement, in prose position. The explicit-type-argument spelling (two arguments, one of them ceremony) was pruned in phase 2 |
| A hoisted demanding helper | one type argument **in the return type** | a statement of what the helper needs — the thing a tooltip shows at every use site |
| A hoisted *generic* helper (`Sections<TSpace, T>`) | the parameter, its `where TSpace : class, ISpace` constraint, and — recorded at phase 7 — its plain instantiation is `IProjection<ISpace, T>`, which does **not** convert to `IProjection<T>` (the latter derives from the former; the conversion runs one way) | appeasing the compiler. This is the tax's whole remaining balance |

Scenario 2's original tax (`.Demanding<IFormulaSpace, IReadOnlyList<T>>()`, two type arguments of
which one was pure ceremony) cannot be paid by anyone: the reach-through spelling it existed for was
never shipped — `Formula()` is a leaf (§5) — and the witness form `.Demanding(Formulas)` writes no
type argument at all, so the type-argument overload was deleted with gauntlet scenario 2c in the
post-campaign cleanup. `Demanding` has one spelling.

Verdict: **PASS.** The one site that reads as appeasement is the generic helper, it costs a
constraint and a base-form return type, and it is the rarest shape in the vocabulary.

### 3. The doubling — **43 → 6, and the six are not duplicates of each other**

Phase 1's third design won: a modifier is generic in the *projection's* own type and hands it
straight back, so one definition serves plain and demanding receivers without knowing demands
exist. The final census: **21 single-definition modifiers; 16 demand-raising or demand-declaring
members** (a distinct set — the lifts unify the receiver's demand with the matcher's, which a fixed
receiver type cannot do); **6 residual doubles**, five of them the type-function limit (C# cannot
say "this same projection with `T` replaced by `T?`" — `Optional`, `Select`, `Else`) and one
(`Choice`) kept doubled by owner judgment for its named type errors.

Engine damage, the other half of criterion 3: **none.** `TSpace` is a phantom — it appears in no
member — so `ProjectionEngine`, `Placement`, the views and every composite are written against the
untyped form exactly as before, and the typed layer costs one internal cast at the seam.

### 4. Error-message quality — **PASS, with a documented worst case and its cure**

The taxonomy is exact and is tabulated in the refusals ledger: refusals landing on an **assignment
or an argument** produce CS0266/CS1503 messages that name both types and the capability; refusals
landing on **generic inference** produce CS0411 boilerplate that names neither. The worst message in
the set is (e) — a demanding child in a plain flow, reported as `CS0411` on `Next`, pointing two
lines from the fix.

The phase-6 finding stands as the campaign's sharpest ergonomic result: inside a scope the cursor's
space is already fixed, so the same mistake is a failed *argument conversion* —

```
CS1503: Argument 1: cannot convert from 'IProjection<IFormulaSpace, string?>'
                                     to 'IProjection<Unrect.Core.ISpace, string>'
```

— both types named, at the child that raised the demand. **Scoping a declaration buys diagnosis,
not just brevity.** That was not why entry B was specified and it is now the strongest argument for
it.

### 5. What the fallback (§5) turned out to be — **the floor, not the alternative**

The experiment priced the runtime-fault design as the remainder if the typed layer failed. The typed
layer did not fail, and §5 shipped anyway and is not redundant, because the types close three doors
short of all of them:

1. **The cast escapes** — a phantom parameter has nothing to check at run time, and
   `(IProjection<T>)demanding` compiles and succeeds. Explicit at the site; nothing warns.
2. **A lambda's body is invisible** — the original "do not adopt" finding, dissolved for the main
   path by moving records onto projections (§6) and by shipping `Formula()` as a leaf rather than a
   reach-through (§5), not by the type system growing eyes.
3. **`Landmark` drops the demand at the seam** — deliberately, so the runtime fault stays reachable
   and therefore has to stay correct.

So the boundary rule ("I could not look" is a fault, never a no-match, and no tolerance absorbs it)
is load-bearing under the typed layer rather than instead of it. It is pinned in
`CapabilityFaultTests` and `FormulaCapabilityTests`, and it is what makes the three holes survivable
rather than silent.

### The campaign's numbers

| Phase | Suite | Note |
|---|---|---|
| checkpoint | 1,439 | the spec, the spike, the gauntlet |
| 1 — the doubling dies | 1,439 | zero delta: nothing user-visible changed |
| 2 — the rename | 1,439 | zero delta, literal pins respelled in the same pass |
| 3 — the capability stack | 1,545 | +106: formulas, shared-formula reconstruction, the slicing law as a theory |
| 4 — `OrBlank`, the `eachRow` slot | 1,610 | +65 |
| 5 — the bind and the `CaptionMap` | 1,641 | +31 |
| 6 — the scoped entry, `MapWorkbook` | 1,684 | +43 |
| 7 — acceptance | **1,700** | +16, plus the refusals ledger (14 recorded refusals, re-verified) |

Both TFMs 0 warnings, `--no-incremental`, at every phase including this one.

### Closing note: the pre-merge documentation pass

After phase 7 acceptance, a three-sweep cleanup (plus a QA round) went over the whole
tree on `experiment/typed-spaces` before merge: sweep 1 deleted what the campaign left
dead — unused usings and the vestigial members this spec already recorded as gone (among
them `Demanding<TSpace, T>()`'s type-argument spelling), with `EnforceCodeStyleInBuild`
turned on to keep them out; sweep 2 was structural — the `git mv` renames that foldered
`Unrect.Spreadsheets` and the test tree and split the strategy tests, under the
conventions "libraries folder with flat namespaces; tests folder = namespace"; sweep 3
(this pass) brought every status header, including this one, into agreement with the
finished campaign, verified every doc path and operator claim against the settled tree,
and re-verified the refusals ledger and the acceptance suite. The suite stood at 1,709 tests green, both TFMs at 0 warnings, when sweep 3 closed —
the 9-test difference from the 1,700 recorded above is the QA round's regression coverage
added after phase 7, not a phase this spec tracks.
