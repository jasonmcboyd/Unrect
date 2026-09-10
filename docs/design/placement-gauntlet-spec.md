# The Placement Gauntlet — judging the inverted pipeline

**Status:** SPEC for the spike (2026-09-09). The owner's standing requirement: erasure
must be UNSPELLABLE — compile time, not analyzer, not runtime. The design under
judgment is `staged-placement-note.md` §5.2 (Alternative C, the inverted pipeline),
with §2+placed-state and §5.1 (the `Placed` node) as comparison foils. Judged the way
`combined-select-experiment.md` and `typed-spaces-experiment.md` were: build enough to
compile the scenarios, read the result as a user, decide. Adoption, partial adoption,
hybrid, and "neither passes — the shipped runtime refusal stands" are all acceptable
verdicts.

## 1. The candidate, as decided so far

All decisions from the design conversation are in `staged-placement-note.md`; the spike
implements them, it does not relitigate them:

- Entries: anchor factories (`On(m)`, `Below(m)`, `RightOf(m)`, `OffsetBy(s)`,
  `SkipEmptyRowsAndColumns()`, …) and size entries; movements `IOffset → IOffset`;
  size stage `IOffset → IOffsetAndSize`; **every projection factory as a terminal** on
  the intermediates beside its bare form.
- The four-spellings grammar: both stages optional, entry anywhere, bare terminal =
  **adjacency + children-sizing** (both defaults decided; zero semantic change from
  today).
- Hoisted reuse: `Below(mark).Of(transactions)` (the `Placed`-node convergence).
- Scope: `Over<TSpace>(expression)` ascription for expression arguments; **scoped
  entries** (`scope.Offset()`, `scope.Below(m)`) for the layout-lambda inference wall.
- Unspellable by construction: double-anchor, anchor-after-movement, navigation chains.
- `SizedToChildren()` exists as optional explicitness, never ceremony.

## 2. The one design question the spike must answer

**Does `.Until` join the pipeline as a stage, or stay a post-terminal wrapper?**
- As a stage (`Below(m).Until(landmark).Vertical(...)`): the bound becomes part of
  placement, adjacent-bound contradiction becomes unspellable, and the frame semantics
  (`Sized`×`Until` at L1) get a fixed stage order that may dissolve the hazard.
- As a wrapper (today): bounds stay composable after any projection (a hoisted section
  bounded at its use site — the `investor-irr` pattern `repeat.Under(...).Until(...)`),
  and the adjacent-bound refusal stays runtime.
The spike spells the corpus's `.Until` sites both ways and the reading decides.

## 3. Scenarios (each written in full under C; the sharpest also under the foils)

1. **The five example declarations** — `simple-report`, `investors-by-deal`,
   `investor-summary` (the lambda-table script), `investor-irr` (hoisted repeat,
   placed twice, `.Under`/`.Until`), `array` — rewritten completely.
2. **The eachRow idiom** — the buying-power bind row: `Right(col).Decimal()` × many
   columns inside an `Overlay` terminal. The census hotspot; zero added ceremony is
   the pass bar.
3. **The owner's scoped sketch, verbatim** —
   `Projection.Over<IGenericSpace>(Offset().SizedToChildren().VerticalRepeat(Table<T>()))`
   plus a demanding child (`Formula()`) to test how demands climb the pipeline's
   intermediates.
4. **Hoisted reuse** — `section` declared once, placed twice via `.Of`, including one
   placement under a scope.
5. **A K-1-style nested section** — outer region anchored by caption, inner content
   anchored within it (the nesting-not-chaining spelling, post-refusal).
6. **The refusal checks** — every hazard from the congruence survey §5 attempted under
   C: each must be a COMPILE error (paste the compiler messages into the spike's
   MustNotCompile file, `TypedSpacesGauntlet` style — the refusals ledger pattern).
7. **The migration read** — one script diffed old-spelling vs new side by side, judged
   for whether a reader who knows today's vocabulary can read tomorrow's unaided.

## 4. Judgment criteria

- **Scenario 2 unchanged in ceremony, or fail** (the scenario-1-unchanged rule of the
  typed-spaces gauntlet, transplanted).
- Every explicit stage in every scenario passes the **stating-vs-appeasing test**: it
  reads as declaring a fact about the document, or the design fails there.
- The compile errors in scenario 6 must be **readable refusals** (CS-error quality was
  part of the typed-spaces verdict; same here — an incomprehensible error is a fail
  even when the refusal is correct).
- Tooltip/`var` readability of the intermediates (what does hovering
  `Below(mark).Down(1)` show? — the §14.5 tradition of reading what tooltips show).
- The foils (§2+placed-state, `Placed` node) are read for any scenario where they beat
  C; a hybrid verdict is legal.

## 5. Mechanics

`spike/PlacementGauntlet/` — a throwaway csproj (not in `src/Unrect.sln`), referencing
the packages' projects, containing: the sketched surface (enough of the
entries/intermediates/terminals to compile scenarios 1–5 — implemented as a thin façade
over the EXISTING engine, since the runtime semantics do not change), the scenario
files, and `MustNotCompile.cs` behind a define. The spike is judged, then either
becomes the renovation's blueprint or is kept as the experiment record — either way the
verdict lands in this spec's §6, `typed-spaces-experiment.md` style.

## 6. Verdict

(To be written after the reading.)
