# Presence and the Unit — separating semantic presence from geometric zero

**Status:** DECIDED (owner, 2026-09-09) — all five decision points resolved as
recommended; IMPLEMENTED and pinned on `experiment/algebra-foundations` (2026-09-09,
`PresenceLawTests`, 30 tests — including the compatibility-law section born of the D5
amendment below).
The calls: **D1** internal only (public exposure waits for a caller the value cannot
serve, and lands in the decomposition trace when the trace lands); **D2** yes — a
repetition ended by an Absorbed item records the teaching `Info`; **D3** the names are
`Read | Empty | Absorbed` ("absorbed" is already the project's word for the event);
**D4** no public unit combinator — an internal ε proves the flow-unit law closes;
**D5** Empty ends a repetition. Plus the composite rule: a composite's
presence is the join of its children's — Read if any child Read, else Empty; Absorbed
arises only at tolerance boundaries.

**D5 AMENDED at implementation (2026-09-09):** "continue only on Read with progress" was
falsified by QA — an Absorbed boundary under a declared area or padding carries
Consumed > 0 (`.Optional().Sized(…)` inside a repeat previously advanced and kept
collecting; the presence-based guard would have ended it, an L1/L2 change to an existing
spelling). The corrected rule: **the numeric productivity guard remains the sole
decider** (zero consumption/progress ends the run, exactly as before presence existed);
**presence supplies the reason** — D2's Info fires when the run ends via the guard AND
the ending item's presence is Absorbed. In one sentence: **presence explains a stop; the
extent decides one.** Nothing may treat presence and geometry as two spellings of one
fact — Absorbed and Consumed > 0 co-occur legitimately. Corollary now documented: Absorbed may travel
with non-zero consumption when placement declares area, so the writer's §3.2 rule must
key on presence AND consumption together (an absorbed-but-placed region emits its
declared empty area, not nothing).

This was the design conversation for v0.4's §7/§14.1 — the algebra's highest-priority
open problem — written scenarios-first so the API was judged before any code existed.

## 1. The problem, stated once

`Consumed == 0` currently means four different things:

1. a repetition has no next item (the productivity guard's exit);
2. a tolerance boundary absorbed a failure (`.Optional()`, `.Else(value)`);
3. a legitimate region is empty (a discovered extent found zero rows);
4. the trigger for the following-sibling diagnostic note.

These are different semantic events sharing one number. The consequences are all on
record: `Repeat(x.Optional())` is a documented trap (the repeat cannot tell "tolerated
absence" from "ran out"); `VerticalFlow` has no identity element and therefore no monoid
law (a zero-child flow must stay a fault to protect the repeat guard); and a writer
cannot exist, because absent-and-tolerated versus present-and-empty read back
identically, making the emit/omit decision undecidable (the invertibility audit's
recorded prerequisite).

## 2. The proposal in one line

The engine's result gains a **presence** — what kind of nothing (or something) this was:

```
Presence = Read | Empty | Absorbed
```

- **Read** — content was recognized; the region is real and non-vacuous.
- **Empty** — the declaration looked and legitimately found a zero-extent region
  (a discovered extent settling at zero rows; "the data contained zero of these").
- **Absorbed** — a tolerance boundary exercised itself; nothing was read and the
  extent is honestly unknown.

Presence is carried on the engine's internal result beside `Consumed`/`Advance`. It is
**denotation metadata, not geometry**: every existing declaration keeps its exact
L1/L2/L3 behaviour (the whole 1,820-test suite must pass unmodified — the differential
obligation), and no operator changes what it consumes. What changes is that rules which
today *infer meaning from a zero* instead *read the meaning directly*.

## 3. The scenarios (why each variant matters)

### 3.1 The repeat trap, dissolved by rule instead of accident

```csharp
VerticalRepeat(section.Optional(), separatedBy: BlankRows())
```

Today: the absorbed item consumes zero, the productivity guard fires, the repetition
ends — correct outcome, accidental reason, and silent.

**AMENDED (see D5 in the status header):** the guard stays numeric — an absorbed item
under a declared area consumes that area and always did keep the run going. What
presence adds is the *reason*: when the run ends by the guard AND the ending item was
Absorbed, the repeat says so (D2's Info). One known limit, a consequence of the join
rule: the Info reaches only an item that IS the boundary (or a transparent wrapper of
one) — a boundary buried inside a wrapping flow joins away to Empty before the repeat
sees it, so that spelling of the trap stays silent.

### 3.2 The writer's emit/omit decision, decidable

| presence | writer emits |
|---|---|
| Read | the content (a blank-tolerant leaf that took a blank is Read — it read a real 1×1 cell whose kind is Blank; Empty is reserved for zero-EXTENT regions, so presence and extent never contradict) |
| Empty | the canonical empty representation (zero rows, structure intact) |
| Absorbed | nothing where no area was declared; the declared empty area where placement claimed one (per the D5 corollary — Absorbed travels with consumption legitimately) |

Without presence, `read ∘ write = id` is provably unsatisfiable for any vocabulary
containing tolerance. With it, the writer's §13 program unblocks.

### 3.3 The unit, enabled

A unit ε — always Accept, yields nothing, consumes zero, presence **Empty** — becomes
*expressible*. That gives the flow its identity element and makes "delete an empty
child" / "flatten nested flows" candidate rewrites for the future IR. Whether ε becomes
public vocabulary is a separate decision (§5, D4).

**Qualified at implementation (2026-09-09):** the identity holds at **L3 for readings
that succeed** and for use-site-named children; it stops where ε's zero consumption
trips the following-sibling note on a child failing at the cursor (a divergence at
**L1**, since the note is appended to the failure's problem text) and where ε shifts an
inline sibling's ordinal (`Cell#1` → `Cell#2`). Both boundaries are pinned as
specific-difference negatives in `PresenceLawTests`. "Delete an empty child" is
therefore sound for values and extents but may change a failure's text — the IR's
rewrite must know this. (Note also, post-D5-amendment: ε ends a repetition via the
numeric guard, as any zero-consumer always has — the unit and the productivity
guarantee coexist without presence arbitrating.)

### 3.4 Diagnostics that say why

The following-sibling note ("preceding sibling consumed nothing at this position")
currently fires on the number. With presence it can speak: *"the preceding sibling was
absent (a tolerance boundary absorbed its failure)"* versus *"…read an empty region"* —
different advice to a reader debugging a declaration.

**DEFERRED at implementation (2026-09-09):** the note's trigger deliberately tests
consumption, not absorption — pinned by
`BoundaryProjectionTests.TheNoteIsAboutConsumptionRatherThanAboutAbsorption` — so
wording that names absorption as the cause would claim more than the trigger checks.
The D2 Info covers the confusing case; rewording the note is a separate owner decision,
recorded at `FlowState.cs`'s note const.

## 4. What each existing spelling reports (the classification table)

| today's spelling | presence |
|---|---|
| any leaf/composite that read content | Read |
| `Range(RowsWhileAnyValue(), …)` settling at 0 rows | Empty |
| `VerticalRepeat(…, atLeast: 0)` collecting zero items | Empty |
| `.Optional()` / `.Else(value)` absorbing | Absorbed |
| `.Else(projection)` whose fallback ran | the fallback's own presence |
| `Caption`, anchors, tables that matched | Read |
| a zero-child layout lambda | **stays a declaration fault** — "described nothing" is not a presence, it is an error |

The classification is itself pinnable: one law-test per row.

## 5. Decision points (the owner's calls, deliberately not pre-made)

- **D1 — Does presence surface publicly, and where?** Options: (a) internal only —
  the engine and diagnostics use it, nothing public changes (smallest step; the writer
  and IR can still see it via internals); (b) exposed on `MapResult`/diagnostics API so
  callers can ask "was that section absent or empty?" — a real user question in
  monthly-close code, but new public surface. Recommendation: (a) first; (b) is
  additive later and better decided by a real caller's need.
- **D2 — Should a repeat that ends by Absorbed say so?** An `Info` ("repetition ended
  at occurrence 3: the item's failure was absorbed — a tolerated item cannot drive a
  repetition") turns the documented trap into a guided one without changing semantics.
  Recommendation: yes — it is the cheapest possible fix to DOC-88's trap.
- **D3 — Naming.** `Read | Empty | Absorbed` is the working set. Alternatives:
  `Content/Empty/Absent` (reads better in user docs; "Absent" risks confusion with
  landmark absence), `Present/Empty/Tolerated`. The word pair that must never blur:
  *empty* (looked, found zero) vs *absorbed/absent* (did not read).
- **D4 — Ship the unit combinator now?** The domain will support ε; shipping `Nothing()`
  as public vocabulary is separate. Recommendation: not yet — no document-shaped need
  exists, and the leaf-firewall spirit says vocabulary follows documents. The unit's
  value today is that the *laws close*; the IR can mint it internally when flattening.
- **D5 — Does `Empty` end a repetition?** Proposed: yes (same behaviour as today,
  principled reason). The alternative — Empty items continue with separators — is a
  semantic change with no motivating document and would break the productivity
  guarantee's simplicity. Recommendation: keep today's behaviour everywhere; presence
  changes *reasons and future capability*, never current outcomes.

## 6. What this deliberately does not do

- No change to any existing declaration's L1/L2/L3 behaviour (differential obligation)
  — with exactly one decided exception: D2's Info is a new L3-visible diagnostic for
  `VerticalRepeat(x.Optional())`-shaped declarations.
- No change to consumption arithmetic, the unconsumed-space meter, or `atLeast:`.
- No public vocabulary (per D1(a)/D4 recommendations).
- No writer implementation — this only makes one possible.
- Core charter untouched: presence lives on the engine's result in `Unrect`, not in
  `ISpace` or any Core contract.

## 7. Implementation sketch (sized after the decisions)

`ProjectionResult` gains `Presence` (internal enum); tolerance boundaries stamp
Absorbed; discovered-zero extents stamp Empty; everything else defaults Read.
`RepeatProjection`'s guard is unchanged (the D5 amendment) — only the exit Info reads
presence; the sibling-note and (per D2) the
repeat-exit Info consume it for wording. New law-tests: the classification table, the
repeat-reason pins, and the flow-unit law stated over an internal ε to prove the domain
closes — even if no public combinator ships.
