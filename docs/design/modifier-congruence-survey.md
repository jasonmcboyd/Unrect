# Survey: The Modifier Congruence Table

**Status:** SURVEY — discovered evidence, 2026-09-09, branch `experiment/congruence-and-boundary`.
This is the evidence half of v0.4 §14.4 ("is there a modifier normal form?"). Every verdict below
was **measured**, not reasoned: a systematic ordered-pair harness ran each pair in both orders over
nine scenarios and compared the two readings at L1, L2 and L3 with the comparators in
`src/Unrect.Tests/Observations.cs`. Nothing here was known before it was run, and the two places
where analysis and measurement disagreed are marked in §7.

The harness was a throwaway (`CongruenceProbe`, four rounds: the 120-pair sweep, three targeted
spotlight rounds). It is deleted. What survives it is this table and the selective pins in
`src/Unrect.Tests/Projections/ModifierCongruenceTests.cs` — 31 tests, 55 cases — which pin every
non-commuting pair not already pinned elsewhere, one representative commuting pair per group, and
every silent-discard hazard. Reproducing a deleted probe is not the point; the pins are.

---

## 0. How to read a cell

A cell answers one question about the unordered pair `{A, B}`: how much of the reading survives
writing `x.A().B()` instead of `x.B().A()`?

| Code | Meaning |
|---|---|
| `3` | **Commutes at L3.** Value, failure, consumption, offset, advance, diagnostics, path and subject — identical. The two spellings are the same declaration. |
| `2` | **Commutes at L2, differs at L3.** Same reading and same geometry; different account of it (a path, a subject, an ordered diagnostic). |
| `1` | **Commutes at L1 and on the advance, differs at L2.** Same value, same failure, same distance a parent must step; the split of that distance between *offset* and *consumed* moves. |
| `✗` | **Fails L1.** Different value, or a failure in one order and none in the other. Two different declarations. |
| `⊘` | **Not composable in one direction.** One order builds; the other is refused — by the compiler, or at construction (OrBlank-after-wrapper, and since 2026-09-09 every declared-over-declared placement). |

The levels are v0.4 §3's. Two facets of L2 behave differently often enough to be worth naming
separately, and the table's `1` depends on the distinction:

```text
advance = offset + consumed     what a parent flow steps past
split   = (offset, consumed)    how that distance was attributed
```

A `1` means the advance held and the split moved.

---

## 1. The five implementation kinds

Read off `ProjectionExtensions.cs` and `ProjectionBase.cs`. The kind predicts most of the table and
mispredicts enough of it to be worth stating.

| Kind | Members | Mechanism |
|---|---|---|
| **clone/name** | `.Named(n)` | `Renamed` — `MemberwiseClone`, writes `Name` |
| **clone/area** | `.Sized(a)` | `Replaced(Placement.WithArea)` — writes `Area`, **replaces** |
| **clone/offset (replace)** | `.OffsetBy(o)`, `.On(m)`, `.Below(m)`, `.RightOf(m)` | `Replaced(Placement.WithOffset)` — writes `Offset`, **replaces**. The three anchors are `OffsetBy` with a lift (`To`/`Past`) |
| **clone/offset (compose)** | `.Down(n)`, `.Right(n)`, `.AfterBlankRows()`, `.AfterBlankColumns()` | `Move` → `OffsetBy(Then(existing, new))` when an offset is already declared, else the new one alone |
| **clone/leaf** | `.OrBlank()` | `TypedCellProjection.Tolerating` — a rebuilt leaf carrying the placement and the name across. Behaves as a clone; is not one structurally |
| **wrapper** | `.Optional()`, `.Else(value)`, `.Else(projection)`, `.Until(m)`, `.UntilColumn(m)`, `.Padded(…)`, `.Select(f)`, `.Under(…)` | A **new projection** holding the receiver as a child, always built with `Placement.Default` |
| **ascription** | `.Demanding(witness)` | Returns the receiver. Reference identity |

Two structural facts do most of the work below:

1. **A wrapper is built with `Placement.Default`.** So a clone written after a wrapper writes the
   *wrapper's* field, and one written before writes the *inner's*. Nothing is lost either way — the
   two placements are resolved at different levels of one tree.
2. **An unnamed wrapper is transparent** (`IsTransparent => Name is null`) and contributes no path
   segment. Naming it makes it opaque and it claims one.

---

## 2. The table

Upper triangle; the pair verdict is symmetric because the question is symmetric. Column order is the
kind order of §1. Footnote marks are resolved in §2.1.

| | Named | Sized | OffsetBy | On | Below | Down | Right | AfterBlank | OrBlank | Optional | Else(v) | Else(p) | Until | Padded | Select | Under | Demanding |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Named** | — | `3` | `3` | `3` | `3` | `3` | `3` | `3` | `3` | `2` | `2` | `2` | `2` | `2` | `2` | `2` | `3` |
| **Sized** | | — | `3` | `3` | `3`ᵃ | `3` | `3` | `3` | `3` | `✗` | `✗` | `✗` | `✗` | `✗` | `✗` | `✗` | `3` |
| **OffsetBy** | | | — | `✗` | `✗` | `✗` | `✗` | `✗`ᵇ | `3` | `1` | `1` | `1` | `✗` | `1` | `1` | `✗` | `3` |
| **On** | | | | — | `✗` | `✗` | `✗` | `3`ᶜ | `3` | `✗` | `✗` | `✗` | `✗` | `✗` | `1` | `✗` | `3` |
| **Below** | | | | | — | `✗` | `✗` | `✗` | `3` | `✗` | `✗` | `✗` | `✗` | `✗` | `1` | `2`ᵈ | `3` |
| **Down** | | | | | | — | `3` | `✗` | `3` | `1` | `1` | `1` | `✗` | `1` | `1` | `✗` | `3` |
| **Right** | | | | | | | — | `✗`ᵉ | `3` | `1` | `1` | `✗`ᶠ | `✗` | `1` | `1` | `✗` | `3` |
| **AfterBlank** | | | | | | | | — | `3` | `1` | `1` | `1` | `1` | `✗` | `1` | `1` | `3` |
| **OrBlank** | | | | | | | | | — | `⊘` | `⊘` | `⊘` | `⊘` | `⊘` | `⊘` | `⊘` | `⊘`ᵍ |
| **Optional** | | | | | | | | | | — | `✗` | `✗` | `✗` | `3` | `✗` | `✗` | `3` |
| **Else(v)** | | | | | | | | | | | — | `✗` | `✗` | `3` | `✗` | `✗` | `3` |
| **Else(p)** | | | | | | | | | | | | — | `✗` | `3` | `✗` | `✗` | `3` |
| **Until** | | | | | | | | | | | | | — | `✗` | `3` | `✗` | `3` |
| **Padded** | | | | | | | | | | | | | | — | `3` | `✗` | `3` |
| **Select** | | | | | | | | | | | | | | | `3` | `3` | `3` |
| **Under** | | | | | | | | | | | | | | | | — | `3` |
| **Demanding** | | | | | | | | | | | | | | | | | — |

Self-pairs (the diagonal) are a separate question and are answered in §2.2.

### 2.1 The footnotes

- **ᵃ `Sized`×`Below` is `3` but was measured vacuously on the sweep's sheets** — a 2×2 extent below
  a landmark on row 3 does not fit either way, so both orders threw the same refusal. The verdict is
  carried by the `Sized`×`On` case and by
  `AnchorMovementLawTests.ADeclaredAreaSurvivesEitherSpellingIdentically`, which is the same claim
  (an anchor touches only the offset half of a placement) pinned non-vacuously.
- **ᵇ `OffsetBy`×`AfterBlank` is a same-field discard** and therefore `✗` in general, but **none of
  the swept sheets could tell the two apart**: the difference needs a blank row *at the replaced
  offset's target*. It is entered `✗` on the strength of the rule, not of a measurement, and is the
  one cell in this table asserted by analysis. It is deliberately **not pinned**.
- **ᶜ `On`×`AfterBlank` is `3` by an accident of what "matched" means.** The row an anchor lands on
  carries the match, so a blank-skip written after it is always a no-op, and the discard the same
  pair shows for `Below` cannot be provoked. It is not a law: a `RowWhere` predicate that accepts an
  all-blank row would break it, and its `Below` twin breaks on any sheet with a gap under the
  landmark (measured, and pinned).
- **ᵈ `Below`×`Under` commutes at L2 and blames a different child at L3.** Both orders fail with the
  same sentence over the same missing landmark; `x.Below(m).Under(cap)` blames the section
  (`Under -> Text#2`), `x.Under(cap).Below(m)` blames the caption (`Under -> Caption("Mark")#1`).
- **ᵉ `Right`×`AfterBlank` is `✗`, and the sweep first said `3`.** See §7 — a cross-axis pair that
  looked like a law until a ragged sheet was written for it.
- **ᶠ `Right`×`Else(projection)` is `✗` where `Right`×`Else(value)` is `1`**, because a fallback
  *projection* has a placement of its own and a movement written outside the boundary moves it too.
- **ᵍ `OrBlank` and `Demanding` do not compose in either order at the type level.** `Demanding`
  requires an `IProjection<T>` receiver and hands back an `IProjection<TSpace, T>`; `OrBlank` refuses
  the latter.

### 2.2 Self-pairs

| Pair | Rule | Where pinned |
|---|---|---|
| `Named`×`Named` | last wins | `PlacementTests.RepeatedNames_KeepOnlyTheLast` |
| `Sized`×`Sized` | ⊘ refused at construction (since 2026-09-09, §5's DECIDED block) | `PlacementTests.RepeatedSizeModifiers_AreRefused` |
| `OffsetBy`/anchors × themselves | ⊘ refused at construction (since 2026-09-09) | `ModifierCongruenceTests.TwoAnchorsAreRefusedInEitherOrder`, `.AnAnchorOverAnAnchorIsRefusedSoNeitherLandmarkGoesUnsought` |
| movements × themselves | compose (`Then`) | `PlacementTests.RepeatedOffsetModifiers_Compose` |
| `Until`×`Until` | ⊘ refused **when adjacent or clone-separated** (since 2026-09-09); nests through a wrapper | `UntilProjectionTests.ALaterBoundIsRefusedRatherThanReplacingAnEarlierOne`, `ModifierCongruenceTests.ASecondBoundIsRefusedSoNeitherLandmarkGoesUnsought` |
| `Padded`×`Padded` | nests; insets add | `ModifierCongruenceTests.PaddingNestsSoTwoPadsAreTheSumOfTheirInsets` |
| `Optional`×`Optional` | idempotent at L3 | `AlternationLawProbeTests.OptionalIsIdempotentAtEveryLevelAReaderCanObserve` |
| `Else`×`Else` | associative until a second boundary is exercised | `AlternationLawProbeTests` (three tests) |
| `OrBlank`×`OrBlank` | does not compile (CS0453 / CS8620) | comment block in `OrBlankTests` |
| `Demanding`×`Demanding` | does not compile — the second one's receiver must be an `IProjection<T>` | noted in `ModifierCongruenceTests.DemandingIsTheIdentity…` |

---

## 3. What the table says, in four laws

### 3.1 The clone record commutes — on different fields only

`.Named`, `.Sized`, `.OffsetBy`/the anchors, the movements and `.OrBlank` all copy the receiver and
write one field of the copy. Two of them that write **different** fields commute at L3: the record
is unordered.

$$
\operatorname{Named}(n)\circ\operatorname{Sized}(a) \equiv_3 \operatorname{Sized}(a)\circ\operatorname{Named}(n)
$$

and likewise for every pair drawn from `{name} × {area} × {offset}`. That is the strongest
congruence in the vocabulary and the one a normal form can lean on hardest.

Two writes to the **same** field are last-wins, and §4.1 is about what that costs.

### 3.2 A clone crossing a wrapper is invisible at L2

A name written on either side of a wrapper reads the same document to the same extent — every
`Named`×wrapper cell is `2`. What moves is which node is named, and one further thing: naming a
wrapper makes it opaque, so the outer spelling *adds a level to the tree*.

```text
x.Named("n").Optional()   ->  warning subject 'n',  path "'n' (Text)"
x.Optional().Named("n")   ->  warning subject Text, path "'n' -> Text"
```

The reader called something `n` both times; it was a different something.

### 3.3 A placement crossing a wrapper keeps the advance and re-splits the offset

The one general congruence the survey found. For the wrappers that do **not** search their own
extent — `Optional`, `Else`, `Padded`, `Select`:

$$
\operatorname{W}(x.\operatorname{Down}(n)) \equiv_1 \operatorname{W}(x).\operatorname{Down}(n)
\quad\text{and}\quad
\operatorname{advance} \text{ is equal},
\quad\text{while}\quad
(\operatorname{offset}, \operatorname{consumed}) \text{ is not.}
$$

A parent flow cannot tell the two apart; `Apply` can. TEST-49 records this for `Select` alone
("value + advance"); it is true of the family, and it is exactly false for the two wrappers that do
search their extent (§3.4) and for any run in which a boundary actually absorbs (§4.3).

### 3.4 Two wrappers change the FRAME, and everything written outside them is in a different one

`.Until` searches its extent for a landmark. `.Under` is a flow whose first child searches its
extent for a caption. So for these two, a modifier written outside is not merely applied later — it
decides *where the search happens*:

```text
block.Sized(2x2).Until(m)   // bound found at row 2, extent inside it: reads 2 rows
block.Until(m).Sized(2x2)   // the wrapper's extent is rows 0-1; the landmark is at row 2; FAILS
```

Same pair of modifiers, same sheet; one order reads the document and the other reports that the
landmark does not exist. TEST-91 pins this pair at L2 (what the parent consumes); the difference
reaches L1, which is the hazard worth knowing.

---

## 4. Where the conjecture breaks

The conjecture was: *clone modifiers form a commuting record, wrappers form an order-meaningful
stack.* Both halves are right about the mechanism and wrong about the consequences, in four places.

### 4.1 Clones do not commute with themselves, and the loser is never evaluated

Two writes to one field are last-wins — expected. What is not expected is that **the discarded
modifier is not merely overruled but never run**, so a modifier that could not possibly succeed is
free:

```text
Text().On(RowContaining("Nope")).Below(mark)   // reads the row under the mark. No error.
Text().Below(mark).On(RowContaining("Nope"))   // "no row containing 'Nope' exists…"
```

The first spelling is ≡L3 to `Text().Below(mark)` — the anchor that names a row the file has never
contained leaves no trace of itself at any observation level. The same is true one layer up for
`.Until` (§4.4) and, in the mildest form, for a movement discarded by an anchor written after it
(`x.Down(2).On(m)` ≡L3 `x.On(m)`).

### 4.2 The movements are not one commuting group

`Down`×`Right` commutes at L3, and the inventory's boxed law says so. It does not generalize.
`AfterBlankRows` evaluates a predicate over the space it is handed, and *anything* that changes that
space changes its answer — including a movement on the other axis:

```text
Text().Right(1).AfterBlankRows()   // sliced to column 1, row 0 IS blank: steps over it -> "b1"
Text().AfterBlankRows().Right(1)   // unsliced, row 0 carries a value: no step -> reads a blank, FAILS
```

The correct statement is therefore narrower than "movements compose, so they commute": **explicit
steps commute; a content-sensitive movement commutes with nothing that moves it or reframes it.**

### 4.3 The tolerance boundary is a scope, and geometry outside it survives absorption

This is the deepest structural finding and it unifies six cells of the table. When a boundary
absorbs, it reports `Presence.Absorbed` and a consumed extent of `0x0` — nothing was read, so no
honest extent exists. Every geometric modifier written **outside** the boundary still applies, because
it was resolved before the boundary ran or is applied to what the boundary returned:

| Written | Absorbed reading consumes | Because |
|---|---|---|
| `x.Sized(1x1).Optional()` | `0x0` | the declared extent is inside the boundary and is erased with it |
| `x.Optional().Sized(1x1)` | `1x1` | the boundary itself has the declared extent, and a declared extent is consumed in full |
| `x.Padded(0,1,0,0).Optional()` | `0x0` | the pad is inside |
| `x.Optional().Padded(0,1,0,0)` | `0x1` | the pad insets what the boundary returned |
| `x.Down(1).Optional()` | advance `0x0` | the movement is inside |
| `x.Optional().Down(1)` | advance `0x1` | the boundary's own placement resolved first |
| `x.Until(m).Optional()` | absorbed, `0x0` | the bound is inside and its miss is a failure to absorb |
| `x.Optional().Until(m)` | **throws** | the bound is outside; a boundary never catches what wraps it |

The last row is the `Until` twin of TEST-78 (`Optional`×`On`) and is the same rule seen from the
other end: **a boundary's reach is what it lexically encloses.** `.Under` behaves as `.Until` does —
a caption sought outside the boundary is not absorbed either.

Two further consequences, both quiet:

- **A `Select` outside a boundary transforms the stand-in.** `x.Optional().Select(f)` applies `f` to
  the filler — `f(null)` — where `x.Select(f).Optional()` yields the filler untouched. A conversion
  written on the wrong side of a tolerance boundary runs on a value the document never contained.
- **A placement outside a boundary moves the fallback too.** `Else(projection)` applies its fallback
  to the *boundary's* extent, so `x.Right(1).Else(fb)` and `x.Else(fb).Right(1)` read the same
  primary cell and look in different places for the stand-in. Both succeed, with different answers.

### 4.4 `Until`'s replacement is decided by what is between the two bounds

v0.4 §6.6 records that `x.Until(A).Until(B)` replaces while `x.Until(A).Select(f).Until(B)` nests,
and calls it context-sensitive. The criterion turns out to be exactly the clone/wrapper split of
§1 — `BoundedBy` replaces when the receiver *is* an `UntilProjection`:

```text
x.Until(A).Until(B)              replaces   (A never sought)
x.Until(A).Named("n").Until(B)   replaces   (a clone is not a layer)
x.Until(A).Down(0).Until(B)      replaces   (nor is a zero movement)
x.Until(A).Sized(a).Until(B)     replaces   (…and the area then reframes the search)
x.Until(A).Padded(0).Until(B)    NESTS      (a zero-cell pad is still a layer)
x.Until(A).Select(f).Until(B)    NESTS
x.Until(A).Optional().Until(B)   NESTS
```

Whether a missing landmark `A` is an error therefore depends on whether the modifiers written
between the two bounds happen to be clones. That is the sharpest evidence for the clone-record /
wrapper-stack conjecture and, at the same time, the most surprising place for a user to meet it.

---

## 5. The silent-discard hazards, collected

Four, in descending order of how quietly they lose information. Each has a loud pin.

| # | Hazard | Reads as | Pin |
|---|---|---|---|
| 1 | `x.On(missing).Below(m)` — a replaced anchor is never sought | `x.Below(m)`, exactly (≡L3) | `AnAnchorOverAnAnchorIsRefusedSoNeitherLandmarkGoesUnsought` (flipped to a refusal pin) |
| 2 | `x.Until(missing).Until(m)` — a replaced bound is never sought | `x.Until(m)`, exactly (≡L3) | `ASecondBoundIsRefusedSoNeitherLandmarkGoesUnsought` (flipped) |
| 3 | `x.Down(2).On(m)` — an anchor discards a movement written before it | `x.On(m)`, exactly (≡L3) | `AnAnchorIsRefusedOverAMovementWrittenBeforeIt` (flipped) |
| 4 | `x.Until(m).Sized(a)` / `x.Under(c).Sized(a)` — an extent written outside becomes the search frame | a working declaration turns into "the landmark does not exist" | `AnExtentDeclaredOutsideABoundIsTheFrameTheLandmarkIsSoughtIn`, `…AnUnderIsTheFrameTheCaptionIsSoughtIn` |

Hazards 1–3 are the same mechanism (a replaced strategy is dropped, not run) and are arguably a
design question rather than a defect: a declaration that states two anchors has said something
contradictory, and the library picks one silently. The vocabulary has no way to say "these two must
agree", and nothing today warns.

**DECIDED (owner, 2026-09-09): erasure is a footgun — declared-over-declared placement becomes a
construction-time refusal** (`ArgumentException`, the `OrBlank`-after-wrapper precedent: a
contradiction has no denotation, so it has no spelling). The reasoning, from the design
conversation: every reading a reasonable person brings to `x.On(a).Below(b)` means something the
vocabulary spells differently — the *sequential* reading is nesting (an outer region narrows the
space an inner anchor searches), and the *conjunctive* reading belongs at the matcher level
(compound landmarks — future vocabulary, spelled as predicates with their own commutativity, if a
document ever demands it). With both readings served elsewhere and helpers barred from pre-placing
their returns, declared-replacing-declared has no legitimate use. Scope: a declared offset replaced
by an anchor/OffsetBy (hazards 1 and 3), a declared bound replaced by an adjacent-or-clone-separated
`.Until` (hazard 2), and — pending a legitimacy sweep — a declared area replaced by `.Sized`.
Replacing a DEFAULT stays silent and normal (`HasDeclaredOffset` is the discriminator — the
`MinOffset` canonicalization turns out to have been load-bearing for this rule). Movements continue
to compose. The three ≡L3 hazard pins flip to refusal pins; the "anchors replace" doc language
updates with the change. Hazard 4 (the frame) is unchanged — it is meaningful, documented
semantics, not a contradiction.

---

## 6. The congruence a normal form may use

A normal form for §14.4 cannot sort modifiers. It can rely on exactly this, all of it measured:

1. **Within one clone record**, the name, the area and the offset are an unordered triple
   (§3.1). Only the *last* write to each field survives, and a normal form that drops the earlier
   writes changes nothing observable — with the caveat that dropping them is what makes hazards 1–3
   unreportable, so a *diagnostic* pass must see them before a *simplifying* pass removes them.
2. **A movement sequence** may be reassociated but not reordered unless every step is explicit
   (§4.2). `Then(Down(a), Down(b)) ≡ Down(a+b)` and `Then(Down, Right) ≡ Then(Right, Down)` hold;
   nothing involving `AfterBlank*` may be moved at all.
3. **A placement may be pushed across a non-frame-shifting wrapper** (`Optional`, `Else`, `Padded`,
   `Select`) **if the observation level is L1 + advance and no boundary under it absorbs** (§3.3,
   §4.3). At full L2 it may not; across `Until` or `Under` it may not at any level.
4. **`Select` may be reordered against `Padded`, `Until` and `Under` freely** (L3), and against a
   tolerance boundary **never** (§4.3).
5. **`Padded` composes as addition** and is therefore the one wrapper that commutes with itself.
6. **`.Demanding` is the identity** and can be moved anywhere its receiver still type-checks.

Everything else in the table is a real ordering constraint that a normal form has to encode rather
than normalize away.

---

## 7. Coverage, and the two places measurement corrected analysis

**Swept:** 16 modifiers × 120 unordered pairs × 9 scenarios (four sheets — landmark in the middle,
landmark first, blank-led, ragged — crossed with two subjects: a `Text()` leaf that fails on a
number cell, and a `Range` whose value renders its own extent). Each scenario covered a success, a
subject failure and, where a landmark was involved, a missing landmark. Three further spotlight
rounds covered the absorbed-reading cases, the ragged-blankness cases and `OrBlank`.

**Not swept, and why:**

- `.RightOf`, `.AfterBlankColumns`, `.UntilColumn` — the column mirrors. `MirrorLawTests` pins the
  two families as one denotation; every verdict for the row form is the column form's.
- `.Until(m, orEnd: true)` was swept and differs from `.Until(m)` in exactly one way: it never fails,
  so pairs that are `✗` on a missing landmark become `1`. It is not given its own column.
- `.Else(projection)` where the fallback has no placement of its own — that is the `Else(value)`
  column's behaviour; footnote ᶠ is what a *placed* fallback adds.
- Repetition, `Choice` and the layout factories: those are combinators, not modifiers, and their
  algebra is `AlternationLawProbeTests` and `CompositeInvarianceLawTests`.

**Where the probe corrected the analysis:**

1. **`Right`×`AfterBlankRows` looked like a law.** The first sweep reported `3` across every sheet,
   which would have made "movements commute across axes" a plausible generalization of the boxed
   `Down`×`Right` law. It is an artefact of the sheets: all of them had rows that were blank in
   *every* column. A sheet with a row blank in one column and not the other falsifies it at L1
   (§4.2). **A cross-axis commutation cannot be measured on a sheet whose blankness is uniform
   across columns** — worth remembering the next time a movement law is proposed.
2. **`Padded`×`Optional` looked like a clean `3`.** It is, on any run where the subject succeeds.
   The pad only reveals itself once the boundary absorbs, which needs a subject that fails
   *wherever it lands* — a sheet whose whole column is the wrong kind. Half of §4.3's table is
   invisible without one, and the first sweep did not have one.

Both corrections have the same shape: an order difference that only exists on the failing path, over
a scenario set chosen for the succeeding one.

---

## 8. Evidence map

Every cell of §2 that is not `3` is covered by a pin, here or elsewhere. Cross-references rather
than duplicates:

| Region of the table | Pinned by |
|---|---|
| clone × clone, different fields (`3`) | `ModifierCongruenceTests.TheCloneRecordCommutesAtL3…` (8 pairs), `.OrBlankJoinsTheCloneRecordAtL3` (4), `.ExplicitMovementsCommuteAtL3` |
| clone × clone, same field (`✗`) | `.AnAnchorIsRefusedOverAMovementWrittenBeforeIt`, `.AnAnchorOverAnAnchorIsRefused…`, `.TwoAnchorsAreRefusedInEitherOrder`, `.ASecondBoundIsRefusedSoNeitherLandmarkGoesUnsought`; and `PlacementTests` for the two self-pairs (all refusal pins since the flip) |
| movement × movement (`✗`) | `.AContentSensitiveMovementDoesNotCommuteWithAMovementOnTheOtherAxis`, `.…WithAStepOnItsOwnAxisEither` |
| `Named` × wrapper (`2`) | `.NamingCommutesWithEveryWrapperAtL2AndNoHigher` (6), `.ANameWrittenOutsideAWrapperNamesTheWrapperAndDeepensThePath`, `.ANameWrittenOutsideABoundNamesTheBound…`, `.ANameWrittenInsideAPadNeverReachesThePaddingsOwnFailure` |
| placement × wrapper (`1`) | `.AMovementCrossingAWrapperKeepsTheAdvanceAndResplitsTheOffset` (4) |
| `Sized` × frame-shifting wrapper (`✗`) | `.AnExtentDeclaredOutsideABoundIsTheFrameTheLandmarkIsSoughtIn`, `.…AnUnderIsTheFrameTheCaptionIsSoughtIn`; TEST-91's L2 half stays in `UntilProjectionTests` |
| anything × tolerance boundary (`✗`) | `.AnExtentDeclaredOutsideABoundaryIsConsumedThoughNothingWasRead`, `.APadWrittenOutsideABoundaryOutlivesTheAbsorption`, `.AMovementWrittenOutsideABoundaryStillAdvances…`, `.ABoundWrittenOutsideABoundaryIsNotAbsorbed`, `.ACaptionSoughtOutsideABoundaryIsNotAbsorbedEither`, `.ASelectWrittenOutsideABoundaryTransformsTheStandIn`, `.APlacementWrittenOutsideABoundaryMovesTheStandInToo`, `.TwoTolerancesInEitherOrderDisagreeAboutWhichFillerWins`; `On`×`Optional` stays in `BoundaryProjectionTests` |
| wrapper × wrapper | `.AWrapperBetweenTwoBoundsIsTheDifferenceBetweenARefusalAndANesting`, `.AWrapperBetweenTwoPlacementsIsTheDifferenceBetweenARefusalAndANestingToo` (the offset/area escape-hatch pin, added at the flip), `.ABoundAndAPadDoNotCommuteWhenTheLandmarkSitsInThePadding`, `.PaddingNestsSoTwoPadsAreTheSumOfTheirInsets`, `.SelectCommutesWithEveryOtherWrapperAtL3…`, `.TheToleranceBoundaryAndThePadCommuteAtL3…` |
| composability (`⊘`) | `.OrBlankRefusesEveryWrapperAtConstructionTimeNamingTheOneItRefused` (4), `.DemandingIsTheIdentitySoItCommutesWithEverything…` |

The one cell deliberately unpinned is footnote ᵇ (`OffsetBy`×`AfterBlank`), because it is the one
entered by rule rather than by measurement.

---

## 9. What this leaves open for §14.4

1. **Is the silent discard (hazard 1–3) intended?** Two anchors on one projection is a contradiction
   the library resolves by dropping one without a word. An `Info` — "an anchor was replaced and never
   evaluated" — would cost nothing and would turn three of these into reportable declaration errors.
   That is an API decision, not a test one.
2. **`MinOffset`'s reference-equality wart (§14.6) is a congruence question too.** Whether a movement
   *composes or replaces* is decided by `ReferenceEquals(Offset, NoOffset)`, so
   `x.OffsetBy(MinOffset()).Down(1)` and `x.OffsetBy(ExplicitOffset(0,0)).Down(1)` are different
   declarations that resolve to the same row and produce different failure sentences
   (`PlacementTests.AMovementAfterMinOffset_ReplacesItRatherThanComposingOntoIt`). Any normal form
   over offsets has to decide whether that distinction is part of the algebra.
3. **`Presence` is not observable at any level.** The absorbed/empty distinction that §4.3's table
   turns on is carried internally beside the consumed extent; the harness could see the *number*
   (`0x0`) but never the *reason*. If the presence work of `presence-and-unit-spec.md` surfaces it,
   the L-levels should gain it, and several `✗` cells in §2 would gain a sharper statement than
   "the consumed extents differ".
