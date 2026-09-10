# Staged Placement — a candidate design for compile-time placement discipline

**Status:** CANDIDATE (2026-09-09) — parked, fully sketched, not scheduled. The
declared-over-declared *runtime* refusal shipped on `experiment/congruence-and-boundary`
and is the standing guard; this note records the compile-time design that would replace
it, the owner-driven conversation that produced it, and the triggers that would reprice
building it. Judged, when its time comes, by the scenario-gauntlet protocol
(`combined-select-experiment.md`'s method): rewrite the example corpus under the
sketched API, read it cold, apply the stating-a-requirement-vs-appeasing-the-compiler
test to every ceremony site.

## 1. The problem it solves

Erasure was a footgun (the congruence survey's hazards 1–3): the fluent flip made every
modifier return a complete, re-modifiable projection, so a second anchor silently
replaced a first. The runtime refusal makes the contradiction loud at construction. This
design makes it *unspellable*: the type after declaring a placement slot no longer has
that slot.

> Chaining literally becomes nesting — the only legal operation, enforced by types.

## 2. The model

Placement construction becomes a staged pipeline of intermediates that do NOT implement
`IProjection`:

```
IProjection ──.On/.Below/.OffsetBy──▶ offset-declared intermediate
                                          │
                              .Sized / .Until (natural closers)
                              .SelfSized()  (the explicit no-size closer)
                                          ▼
                                     IProjection
```

- An intermediate lacks anchor methods, so `x.On(a).Below(b)` is a compile error — the
  conjunctive reading belongs to future matcher combinators
  (`On(a.After(b))`-style compound landmarks), the sequential reading to nesting.
- `.Until(...)` and `.Sized(...)` are the *natural* closers — a section spelled
  `x.Below(m).Until(RowContaining("Total"))` closes with a bound, already the
  recommended way to end a section. This aligns with the design philosophy: the pipeline
  makes every placement say how its extent is determined.
- Clone modifiers (`.Named`, …) preserve the intermediate's state (the self-typed
  `TProjection` machinery carries this for free); wrappers close-then-wrap, consistent
  with the pinned lexical-scope semantics.

## 3. Stage-jumping operators (the owner's key move)

The intermediates are the honest calculus; **nothing obligates an operator to expose one
stage at a time**. Any operator may traverse several stages internally and return the
finished projection:

- A typed leaf's anchor closes immediately — its extent is intrinsic (1×1), there is no
  open size slot, so `Decimal().Right(captions["Amount"])` returns a closed projection
  with zero ceremony. This serves the census hotspot (15 of ~30 corpus placements are
  leaves in `eachRow` projections) entirely.
- Intrinsic-extent receivers generally (`Cell`, typed leaves, `Caption`, `Fields`) get
  closing anchors; genuinely extent-open receivers (`Table`, `Range`, sections) get the
  open intermediate.
- This mirrors the strategy calculus one level down: `IOffsetStrategy`/`ISizeStrategy`
  are stages, `IAreaStrategy` their composition, `ToAreaStrategy()` a stage jump. The
  surface pipeline is the existing substrate made visible in types.

## 4. The explicit closer

Needed only where an open intermediate is hoisted (a local, a `Choice(params ...)`
argument) or where the author declares "no size — the projection's own sizing rules."
**Naming law rulings on record:** `Unbounded()` is wrong (it says `MaxArea`), and
`SelfSized()` is wrong per the owner ("sounds like it decides on its own, when really
the child subspaces determine the size"). Owner's candidate: **`SizedToChildren()`** —
long but accurate for layouts. Accuracy nuance for gauntlet time: there are TWO
self-sizing mechanisms — a layout sizes to its *children*, while a table or discovered
extent sizes to *content* (data scans, not child declarations) — and the word must not
claim the wrong one, or the closer needs a word general over both.

A design principle from the strategy calculus's own history (the closer's older
cousin): the calculus never had a typed half-offset — axis factories return COMPLETE
strategies with a neutral component (`SkipBlankRows()` has width 0) composed via
`Then`, and the surface movements (`.Down(2).Right(3)`) are the same piecewise
construction. **Where the algebra composes, complete-values-with-identity wins (offsets
under movement are a monoid — no staging types needed); where it declares-once
(anchors, bounds), typed staging fits — two slot-writes contradict, and the type
system is the right enforcer.** The pipeline makes the complementary choice for the
slots that never composed; it does not overturn the calculus's choice for those that do.
The owner's position (2026-09-09): even a frequent closer is acceptable when it is a
real size word (`RowsWhileAnyValue()` was judged fine long ago); the leaf one-shots
remove the cases where the closer would say nothing.

Fallback option if the gauntlet finds residual ceremony: consumption sites auto-close
(`v.Next(intermediate)` ≡ `v.Next(intermediate.SelfSized())`) — one close-and-delegate
overload per site; the refusal property survives since the intermediate still lacks
anchors regardless of who closes it.

## 5. The costs (measured against the machinery, honestly)

1. **Factory return types.** Anchors must key on a static type that closed projections
   lack, and C# extension methods bind to interfaces for every implementor — so the
   factories must return something richer than bare `IProjection<T>`. A surface
   renovation on the scale of the projection-model's phase 2, interacting with the
   `TSpace` variance and `ProjectionScope`.
2. **The state lattice.** Offset and area are independent slots (pinned: an anchor does
   not erase a declared area), so intermediates must be order-free — "offset declared,
   size open" and the reverse both exist. The clone machinery carries the static type
   through `.Named` etc.; the lattice is real but small.
3. **What it buys over the shipped refusal:** compile-time for an error that is already
   deterministic at construction (data-independent — no document makes the spelling
   work). The purchase is narrow *today*; the design is recorded because the triggers
   below could widen it.

## 5.0 Settled sub-decision: no navigation chains (owner + evaluator, 2026-09-09)

Under chaining-as-nesting, `x.Below(mark).SizedToChildren().On("None").SizedToChildren()`
would become spellable with well-defined semantics — and MISLEADING ones: each later
anchor wraps the earlier, so the rightmost anchor is the OUTERMOST region and the chain
executes inside-out, the reverse of the left-to-right navigation it resembles. A
spelling that looks like a journey and means reverse-scoped containment is the
`After`/`Past` genus of confusion, inverted on every link. DECIDED: not offered.
Sequential anchoring stays spelled as visible structural nesting (and, someday, compound
landmarks); the chain form remains refused. Consequence for this design: a CLOSED
projection is a plain `IProjection` again, so post-close re-anchoring is guarded by the
shipped RUNTIME refusal rather than by growing the type lattice a "placed" state — the
hybrid (types inside the pipeline, the runtime guard after it) is the intended shape.

## 5.1 Alternative B (owner, 2026-09-09): placement as arguments — or as a node

Enforce declared-once **by grammar** instead of by types: strategies as constructor
arguments (`Vertical(OffsetStrategy(), SizeStrategy(), projection)`) — an argument list
is a write-once slot, so erasure is unspellable. The RegionBuilder's placement model
returning, deliberately.

Two versions with different costs. The *scattered* version (optional strategy args on
every factory) smears placement parameters across ~20 factories and orphans them after
multi-line layout lambdas, and hoisted declarations lose their use-site placement
spelling. The *converged* version is one explicit placing node —
`Placed(offset, area, projection)` — which is how the engine already thinks (a
`Placement` applied once per node) and follows the caption precedent (structure that
matters becomes a node): `Placed(o1, Placed(o2, x))` is honest nesting, reuse is
`v.Next(Placed(Past(m), transactions))`, and it is the most IR/writer-friendly shape of
the three options.

The costs (CORRECTED after owner pushback, 2026-09-09 — an earlier draft claimed this
"unwinds the words"; wrong, the axes are independent): the placement WORDS transplant
cleanly as free landmark-lifting factories — `Below(mark)` is `Past(mark)` under its
renovation name, same direction-in-the-word, same absence semantics —
`v.Next(Placed(Below(mark), transactions))`. What actually remains: (a) word order —
fluent reads subject-first, the node reads placement-first; gauntlet-judged style, not
semantics; (b) one-spelling discipline — the factories must REPLACE the modifiers, not
join them, so it is a full placement-surface renovation either way; (c) one wrap
instead of a chain per use site. And one credit: Alternative B handles §5.0's
navigation hazard BEST — `Placed(o2, Placed(o1, x))` forces sequential anchoring to
look like the nesting it is; the inside-out misreading is structurally impossible when
the scoping is parenthesized.

**The trade-space, complete:** (1) runtime refusal — shipped, zero surface change;
(2) staged pipeline (§2–§4) — types enforce, fluent subject-first spelling, costs a
factory renovation; (3) placement node — grammar enforces, nesting visibly spelled,
best for IR/writer, same renovation scale, placement-first spelling. All three enforce
one law at different layers and all three keep the words. Note for the IR program: even
if users keep spelling (1) or (2), `Placed`-as-node may be what placement looks like in
the IR internally — the surface question and the representation question can be
answered differently.

## 5.2 Alternative C (owner, 2026-09-09): the inverted pipeline — LEADING CANDIDATE

Run the stages in execution order, projection TERMINAL:

```csharp
Below(mark)          // entry factory : IOffset — anchors exist ONLY here
  .Down(1)           // movements: IOffset -> IOffset (composition, the legal monoid)
  .Sized(strategy)   // IOffset -> IOffsetAndSize (optional stage)
  .Vertical(v => …); // terminal: the projection closes the pipeline
```

What it buys over §2's subject-first pipeline, all structural (no runtime guard, no
extra type-states): double-anchor unspellable (IOffset has no anchor methods);
anchor-after-movement unspellable (anchors are roots, movements never lead back to
one); navigation chains unspellable (nothing chains past a terminal); and **the closer
problem of §4 DELETES** — the terminal is the close, absence of a size stage means
default sizing, so `Right(6).Decimal()` serves the eachRow hotspot with zero ceremony
and `SizedToChildren`'s naming question never arises.

The reading defends itself twice: left-to-right matches the engine's execution order
(offset → area → project — the owner's mental model), AND it reads as natural document
description ("below 'Details': a table of transactions") — placement-first is arguably
the more declarative English.

**Convergence with Alternative B:** hoisted reuse needs a terminal taking an existing
projection — `Below(mark).Of(transactions)` — which is `Placed(Below(mark), x)` in
fluent clothes. B and C are one grammar: C is the inline spelling, B's node the hoisted
spelling (and likely the IR representation either way).

Costs: the terminal spread — every projection factory (~30) gains extension forms on
the intermediates beside its bare unplaced form; a full breaking migration of every
declaration in the corpus; and one gauntlet sub-decision left open: does `.Until` join
the pipeline as a stage (structural refusal) or stay a post-terminal wrapper (runtime
refusal remains for adjacent bounds).

Two sub-decisions settled by the owner's scoped sketch
(`Projection.Over<IGenericSpace>(Offset().SizedToChildren().VerticalRepeat(Table<T>()))`):

- **`SizedToChildren` survives as OPTIONAL explicitness, not ceremony** — terminals
  build directly off `IOffset`, with children-sizing as the implicit default (owner,
  2026-09-09). The grounding is the project's founding metaphor: a WPF `StackPanel`
  sizes to its children unless an explicit Width/Height is set — auto-sizing is the
  default, explicit extent the declared exception. Three justifications converge:
  the WPF precedent, the design philosophy ("explicit dimensions are the exception"),
  and the corpus census (2 explicit sizes vs ~30 placements). The placement laws' own
  pattern (silence works; exceptions are declared words) applied to the size stage.
- **Scope threading, two coexisting shapes:** `Over<TSpace>(expression)` as ascription
  for expression arguments (inference + contravariant conversion at the boundary, as
  Entry A works today), and — for the layout-lambda case, whose CS0411 wall the
  inversion does not remove — the scope VENDS THE ENTRIES (`scope.Offset()`,
  `scope.Below(m)`), so intermediates carry `TSpace` from birth and every terminal's
  lambda receives its cursor type. Today's eight scoped members reborn as scoped
  entries; likely fewer members than the current scope.

The owner's sketch is gauntlet scenario material verbatim (scoped + repeat + typed
table).

**The four-spellings grammar (owner, 2026-09-09 — ADOPTED as the pipeline's shape):**
both stages optional, entry at any stage, all combinations legal:

```csharp
Over(OffsetEntry().SizeStage().Vertical(...))   // both explicit
Over(OffsetEntry().Vertical(...))               // size defaults
Over(SizeEntry().Vertical(...))                 // offset defaults
Over(Vertical(...))                             // both default
```

**DECIDED (owner delegated the ruling, 2026-09-09): the default offset is ADJACENCY —
today's law, unchanged.** The reasoning that decided it: the owner proposed
`SkipEmptyRowsAndColumns()` as the default; the evaluator recommends **adjacency**
(today's law) on three collisions: (1) declared separators exist because silence does
not skip — an auto-skipping default absorbs the bands `separatedBy:` and
blank-band-never-terminator reason about; (2) presence: a default skip leaps past the
region a discovered-empty extent or Optional needs to SEE, killing Empty and the
writer's absent-vs-empty; (3) invertibility: adjacency runs backward with gap = 0,
auto-skip makes every boundary an invented canonical gap. The vocabulary's existing
mechanism for the instinct is SHAPE-LEVEL defaults (`Table` skips leading blanks as
part of what a table is; flows stay adjacent). Under the recommendation the bare
spelling `Over(Vertical(...))` keeps exactly today's semantics (zero-change migration),
and `SkipEmptyRowsAndColumns()` joins as an explicit one-word entry. WPF parallel holds
both ways: size defaults to Auto, children stack adjacent unless a Margin is declared.

**Standing owner requirement (2026-09-09): erasure must become UNSPELLABLE — compile
time, not analyzer, not runtime.** That retires option (1) as a destination (it remains
the shipped interim guard) and makes the gauntlet a choice among §2 (+placed-state), B,
and C — with C currently leading on structural coverage and ceremony.

## 5.3 Post-spike rulings (owner, 2026-09-09)

- **Design value, recorded as standing law for this renovation: the hard stuff lies on
  the library — "we are servants to the end users."** This settles the terminal-spread
  dial at the USER-OPTIMAL end: full terminal spread (~41 × 2 + entries), `.Of` kept
  only for the exotic tail. The library-side covenant it accepts (41 lockstep pairs,
  CallerArgumentExpression forwarding at every delegation) is made enforceable, not
  merely borne, by a generated parity suite in the ProjectionScopeTests mold — the
  laws-as-CI pattern applied to the surface itself. Adoption requirement carried over
  from the spike: refused stage members are spelled as `[Obsolete(error)]` teaching
  stubs, never left to CS0311 constraint-babble.
- **Finding 5 dissolved:** the scope keeps its existing layout members beside the
  vended entries — a scoped declaration with no placement stays `p.Vertical(...)`;
  `Offset()` never exists as ceremony. Two situations, each with its one spelling.
- **The verdict's remaining substance is purely what users READ:** (a) mixed reading
  directions in dense declarations (`.Under` can never stage; read-modifiers stay
  postfix); (b) the guarantee boundary (unspellable inside the pipeline; runtime-guarded
  after a terminal). Both live in spike/PlacementGauntlet/MIGRATION-READ.md for the
  cold read; §6 of placement-gauntlet-spec.md awaits the owner.

## 5.4 THE GEOGRAPHY LAW (owner + evaluator, 2026-09-09) — the unifying principle

The owner rejected postfix `.Under`'s "reverse readability" (concept useful,
spelling inverted), and the rejection precipitated the law that generates every
placement-spelling instinct of this design conversation at once:

> **An operator sits on the side of the subject where its referent sits on the
> sheet.** What is above the content spells BEFORE it; what is below spells AFTER;
> reading the declaration top-to-bottom is reading the document top-to-bottom.

Consequences:
- Anchors, offsets, `Under`'s captions (referents ABOVE) → prefix: the pipeline's
  entries, plus a new **`Under(params captions).Of(content)`** entry. The postfix
  `.Under` retires under the one-spelling discipline once the entry proves out.
- Bounds — **UNSETTLED (owner, same evening), and this is the one place the two
  candidate laws part.** The owner's founding model — "specify the offset, specify
  the size, project the space" (execution order) — makes `Until` a SIZE
  specification and puts it PREFIX with the rest of the geometry
  (`On(m1).Until(m2).VerticalFlow(...)`, the spike's original stage form, built and
  differentially verified). The geography law puts it POSTFIX (landmark below).
  Both readings are true of the same operator: "about my extent" vs "about that row
  down there." An earlier revision of this section marked the stage form SUPERSEDED —
  retracted; both spellings stay live in MIGRATION-READ.md for the cold read, and
  the bound's position is an open owner call. Postfix `.Until` keeps its shipped
  adjacent-bound runtime refusal either way.
- The subject sits between its above-operators and its below-operators — where it
  sits on the page. The "mixed reading direction" finding dissolves: mixed by
  geography, uniform by principle.

The canonical read, `investor-irr`'s first series — four lines, four sheet
positions, same order:

```csharp
var byTransferDate =
    Under(Caption("IRR Details"),                        // row 12
          Caption("Cash Flows Using Transfer Date"))     // row 13
    .Of(irrDetails)                                      // rows 14–29
    .Until(RowContaining(Inception));                    // stops before row 30
```

Also recorded from the same exchange, the grammar law that resolved the
chaining-vs-nesting friction: **one dot-chain, one subject; the dot is never
"and."** Content enters only through parentheses (nesting — the declaration tree
mirrors the sheet's containment); everything on dots is about the one subject
(its geometry, its wrapping, its name). Extension syntax moves exactly one
argument left of the operator's name; `x.Under(a, b)` was always
`Under(irrDetails, a, b)` — nesting wearing a chain's clothes — and the entry
form puts even that argument back in parens. Spike trial of the `Under` entry
dispatched with differential acceptance reads.

## 5.5 Entry C (owner's stub, 2026-09-10): the file-scoped vocabulary

`using static ProjectionBuilders<ISpreadsheetSpace>;` — a closed generic static class
imported once, closing `TSpace` at the top of the file. The THIRD solution to the
type-argument split (after instance receivers and argument inference): nested static
types split the parameters across the type dot — `Table.Of<Person>()` — with the space
captured by the closed class and only the result stated. Pushed to its end, the class
re-exports the whole vocabulary and a scoped file has ZERO prefixes: the space is named
once, in the using block, where C# puts file-level bindings. The bet it rests on is
measured: application declaration files are one-document-one-space essentially by
definition (all seven corpus scripts are); only tests and shared helper libraries mix,
and both would keep explicit spellings.

Boundaries found at stub time: (a) one space per file, enforced by C# (two closed
imports = CS0104 on every member — the file IS the scope); (b) the helper-file trap
inverts its default (a helper in a scoped file silently over-demands — guidance:
scoped using static for declaration files, narrow explicit demands for shared helpers;
soft spot, unenforced); (c) completeness obligation — an Entry C file cannot also
`using static Projection` (CS0104), so the closed class must re-export everything;
(d) hierarchy discipline (owner's derived-spaces question, 2026-09-10): deep space
hierarchies amplify the over-demand trap from per-declaration to per-file — and the
standing capability philosophy is the answer, restated: **capabilities, not vendors**
(no `IExcelSpace`/`IOpenDocSpace` — a format difference is a capability interface
joining a bundle), **formats are doors, not types** (no `ICsvSpace` — a CSV door
returns plain `ISpace` and every plain declaration runs on it), and the Entry C
guidance clause: **scope the file to what the declarations READ, not to what the file
parses.** The queued scope-hygiene analyzer gains its second diagnostic: "file scoped
to X but nothing demands beyond Y — narrow the scope."
**Boundary (a) RATIFIED as a theorem (owner's dichotomy, 2026-09-10):** for any two
space types, either their difference matters to a projection (then no shared
projection can exist and separate files are the semantic reality) or it does not
(then the projection targets the shared base and one scope serves both). No third
case — a file never legitimately needs one vocabulary at two space types. A mixed
file is two parsers in one file; the resolution is the file split, with full
qualification as the deliberately-effortful fallback. Corollary: the plain
vocabulary is Entry C at its floor — every file conceptually scopes to what its
document offers, `ISpace` included.
**Status (2026-09-10): LEADING DIRECTION, ARM BUILT AND MEASURED** (49 differentials,
Entry C ≡ today at L2 and L3 on all reads). Findings: (1) the split-type trick is
UNNECESSARY — Entry C closes TSpace at the class level, so `Table<Person>()` binds
directly; the method-type-argument wall never arises (adopting the trick anyway costs
CS0102 + a family respell). (2) The ceiling: CS1106 (no extensions in a generic static
class) means the postfix operators arrive via `using Unrect.Projections;` permanently —
same four import lines as A/B, and the seam falls EXACTLY on the geography law's line:
before-the-subject from the closed class, after-the-subject as extensions. (3) The
completeness obligation is priced: 76 forwarders (and it caught two missing scoped
entries in the façade immediately). (4) Boundary (a) enforces via CS0121 rendered as
"ambiguous between Text() and Text()" — theorem held, message inarticulate; the
over-demand trap punishes as CS0411 at Map. The scope-hygiene analyzer's case is now
THREE diagnostics (narrow-the-scope; the helper trap; demands-exceed-offer) and Entry C
should not ship without it. VERDICT: Entry C carries the whole prefix half alone in
declaration files (the owner's 99%); A/B remain for helper libraries and the
theorem's illegitimate-mixed fallback.

## 5.6 The fossil diagnosis and the Heading trial (owner + evaluator, 2026-09-10)

The owner named `Under` and `Caption` outlier operators, and the history confirms the
diagnosis: **they are fossils of a missing category.** `Under` was born in phase B as
the replacement for a placement modifier (`.After(Past(...))`) and inherited its seat —
postfix, subject-first, modifier grammar — even though its type changed from geometry
to content; the reverse reading the owner hated was never chosen, it was inherited.
`Caption` was shoehorned into value-yielding-leaf because the algebra had no category
for "located, consumed, asserted structure that yields nothing" — hence its vestigial
payload, discarded by `Under` at every corpus site but one. **The stage calculus is
the missing category**, and `Heading(text)` — one stage word: locate, assert, consume,
document order — is the two refugees repatriated. Consequences if the trial holds:
`Under` retires entirely (postfix AND the just-built entry — spikes are scaffolding);
`Caption` shrinks to its one honest value-capturing use (the K-1 `KSection` case);
ruling 2 (double-`Under`) evaporates unasked — layering is `Heading(outer).Of(section)`
nesting. Trial dispatched with differential acceptance reads, the repeat-stop recipe,
and the surviving-Caption coexistence read.

**TRIAL RESULT + FINAL RULING (owner, 2026-09-10): the third act is a dissolved
category, and `Under` retires outright — both forms, nothing left behind.** The trial
held at L3 on the flagship reads (structural reason: a heading contributes no node —
it mints the same Caption leaves, so failures speak the leaf's words) but surfaced a
third use hiding in `Under`: consume-without-asserting (the varying region-title row).
The owner's resolution: that case decomposes into existing honest words — location is
by SHAPE, not text (`RowWhere` over the row's structure: first cell text, neighbor
blank — a structural landmark for titles that vary per file), the name is CAPTURED as
a flow child (ignorable by the caller), and pure don't-care is geometry. The locked
taxonomy: **`Heading` asserts · `Caption` captures · geometry skips · matchers
locate.** No `Heading(IProjection<string>)` overload (would reinstate the fossil); no
third stage word (no document has demonstrated validate-but-discard; mint it if one
ever does). Also from the trial: ruling 1 is pure reading preference (both bound
positions pinned as ONE declaration — semantics never forked); Entry C's completeness
is an ongoing covenant (the closed class inherits every vocabulary move — `Until`'s
revival broke the Entry C read until re-exported; the parity suite is the guard).

## 5.7 The IDE gate (owner, 2026-09-10): PASSED

The one risk the Linux environment could not measure — Visual Studio tooling over
`using static` of a closed generic class — checked by the owner against the spike's
zero-prefix files: completion, tooltips, go-to-definition, error presentation,
signature help. Verdict: "it seems to work incredibly well." Entry C is IDE-safe;
the known-inarticulate errors (CS0121/CS0411) remain the analyzer's assignment as
planned. ALL DESIGN GATES ARE NOW CLEARED — the renovation spec is the next artifact.

## 6. Reprice triggers

- **Matcher combinators** (compound landmarks) — they renovate the anchor surface
  anyway; doing both at once amortizes the factory-return-type cost.
- **The writer / IR program** — a placement model with explicit completion is more
  reifiable; if the IR wants placement as data, the pipeline may fall out of it.
- **Any placement-model revisit** — e.g., if two-axis bounding (`Until(row) × Until(col)`,
  the axis-switch question the refusal surfaced) earns vocabulary, the bound slot's
  design reopens and this note should be on the table.

## 7. What ships meanwhile

The runtime refusal (declared-over-declared throws at construction, teaching messages,
authorship-flagged `Placement`), pinned and documented. This note changes nothing until
a trigger fires and the gauntlet judges it.
