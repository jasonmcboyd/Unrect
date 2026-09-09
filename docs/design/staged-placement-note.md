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

- **`SizedToChildren` survives as OPTIONAL explicitness, not ceremony** — silence
  before the terminal means default sizing, and the word exists for declarations that
  want to state it. The placement laws' own pattern (silence works; exceptions are
  declared words) applied to the size stage.
- **Scope threading, two coexisting shapes:** `Over<TSpace>(expression)` as ascription
  for expression arguments (inference + contravariant conversion at the boundary, as
  Entry A works today), and — for the layout-lambda case, whose CS0411 wall the
  inversion does not remove — the scope VENDS THE ENTRIES (`scope.Offset()`,
  `scope.Below(m)`), so intermediates carry `TSpace` from birth and every terminal's
  lambda receives its cursor type. Today's eight scoped members reborn as scoped
  entries; likely fewer members than the current scope.

The owner's sketch is gauntlet scenario material verbatim (scoped + repeat + typed
table).

**Standing owner requirement (2026-09-09): erasure must become UNSPELLABLE — compile
time, not analyzer, not runtime.** That retires option (1) as a destination (it remains
the shipped interim guard) and makes the gauntlet a choice among §2 (+placed-state), B,
and C — with C currently leading on structural coverage and ceremony.

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
