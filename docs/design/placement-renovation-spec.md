# The Placement Renovation — Entry C, the pipeline, and Heading

**Status:** SPEC for owner review (2026-09-10). Every design decision herein is already
ruled and recorded — this document only phases the build. Sources of truth:
`staged-placement-note.md` (the rulings, §§5.0–5.7), `placement-spelling-rounds.md`
(round 1), `spike/PlacementGauntlet/` (the verified mechanics: 64+ differentials,
20+ ledgered refusals, MIGRATION-READ.md). Strategy: **ADDITIVE-FIRST** — phases 1–4
break nothing; every existing declaration compiles untouched until the phase-5
retirement decisions.

## 0. What ships, in one paragraph

`ProjectionBuilders<TSpace>` — the file-scoped vocabulary (`using static`), zero-prefix
declaration files, capability safety by variance. The placement pipeline — entries
(anchors/offsets/movements as free factories), stages (offset → bound/size →
`Heading` → subject, the ruled canonical order), terminals per the servant principle
(full spread), `.Of` for hoisted reuse. `Heading(text)` — the fossil pair dissolved:
asserts, consumes, attributes a title row; `Caption` remains only as the
value-capturing leaf. Teaching-stub refusals wherever a stage member is refused
(never CS0311 babble). The three-diagnostic scope-hygiene analyzer. The taxonomy:
**`Heading` asserts · `Caption` captures · geometry skips · matchers locate.**

## Phase 1 — the vocabulary class and its covenant

- `src/Unrect/Projections/ProjectionBuilders.cs`: `public static class
  ProjectionBuilders<TSpace> where TSpace : class, ISpace`, re-exporting the complete
  projection vocabulary closed over `TSpace` (the spike's 76-forwarder shape, adjusted
  to the real surface). Every forwarder passes `CallerArgumentExpression` through.
- **The parity suite** (`ProjectionBuildersParityTests`): theory-driven — every member
  ≡ its `Projection`/scope twin at L3 via the Observations harness, plus the
  name-capture forwarding pin per member. This is the covenant made CI (the Entry C
  completeness obligation is ongoing: an operator that moves, moves here too).
- **The backend pattern**: `Unrect` cannot name `Formula()`, so backends ship their own
  closed classes with DISJOINT member names (`SpreadsheetProjectionBuilders<TSpace>` in
  `Unrect.Spreadsheets`: `Formula`, `RowWithFormula`, twins). Disjoint names make the
  two `using static` imports coexist (CS0104 fires only on shared names) — state this
  as the backend rule and pin one coexistence test.

## Phase 2 — the pipeline: entries, stages, Heading, terminals

- The stage types (plain + `<TSpace>`): offset stage, bound/size stage, heading stage —
  classes with instance terminals (the C# inference constraint), replay-based over the
  existing modifiers exactly as the spike proved (each slot written once; the shipped
  declared-over-declared refusal can never fire from inside).
- Entries: `On/Below/RightOf/OffsetBy/Down/Right/AfterBlankRows/AfterBlankColumns/
  SkipEmptyRowsAndColumns/Until/UntilColumn/Heading` as free factories on `Projection`,
  as scope members, and re-exported on `ProjectionBuilders<TSpace>`.
- Canonical stage order enforced by the types: anchors/offsets → bounds/sizes →
  `Heading`(s, chained, accumulated into ONE replayed call) → subject. Refused
  transitions are `[Obsolete(error)]` teaching stubs speaking the library's words
  (the spike's (h)/(l)/(v) messages are the models); refusal-by-absence is acceptable
  only where no reasonable user would attempt the spelling.
- Terminals: full spread per the servant ruling (~41 per hierarchy), `.Of` for hoisted
  reuse and the exotic tail. Defaults: adjacency offset, children-sizing — the bare
  terminal is exactly today's semantics.
- `Heading(string)`: locates by the content rule, asserts loudly, consumes the row at
  full width, contributes NO node (mints `Caption` leaves internally — the L3-by-
  construction property). No `Heading(IProjection<string>)` overload, ever (the ruled
  fossil refusal).
- Pins: port the spike's differential suites into committed law tests
  (`PlacementPipelineLawTests` + additions to existing suites), and the refusal ledger
  into the re-runnable spike (`TypedSpacesGauntlet`-style `MUST_NOT_COMPILE` file —
  extend the existing gauntlet or add `PlacementRefusals` beside it).

## Phase 3 — the scope-hygiene analyzer

New `Unrect.Analyzers` (netstandard2.0 analyzer assembly, packed into the `Unrect`
package). Three diagnostics, all warnings with code-fixes where stated:
1. **Unnecessary scope** — a scoped factory/`p.` whose children are space-indifferent
   ("nothing here demands `X`; use the plain spelling"), the IDE0005 of scopes.
2. **The demand door** — the CS1503-at-`Next` case gets a code-fix ("child demands
   `X`; use the scope's factory here").
3. **Demands-exceed-offer** — the CS0411-at-`Map` case rewritten into the sentence the
   compiler refuses to say ("this projection demands `ISpreadsheetSpace`; this space
   offers `ISpace`").
Ship-requirement per the ruling: phase 3 completes before any release containing
phase 1–2.

## Phase 4 — corpus and docs

- LINQPad scripts respelled to Entry C zero-prefix form (each script's header names its
  space via the `using static` — the file-is-the-scope teaching made visible);
  acceptance tests gain Entry C spellings beside existing ones (both live — additive).
- `docs/vocabulary.md`: the placement sections rewritten around the taxonomy and the
  pipeline; the entries doc gains Entry C as the recommended declaration-file style
  with the two boundaries (helper files demand narrowly; one space per file is the
  dichotomy theorem, not a limitation).
- CLAUDE.md updated by the orchestrator (off-limits to agents, per standing rule).

## Phase 5 — retirements (the breaking phase, each its own owner decision)

1. Postfix `.Under` and the `Under` entry: RETIRE (ruled — the fossil dissolution;
   nothing left behind: `Heading` asserts, capture is a flow child, shape-location is
   `RowWhere`).
2. Discard-case `Caption` uses: gone by migration; `Caption` the leaf remains, doc'd
   as the capture word.
3. Postfix `.Until` vs the bound stage: ruling 1 chose the stage as the canonical
   spelling; whether postfix retires or remains as an accepted second spelling is the
   ONE retirement left genuinely open (the trial proved both are the same declaration
   — the one-spelling discipline argues retire; the migration experience decides).
4. Version: the retirements are breaking → **v0.4.0-alpha.1** target; phases 1–4 alone
   are additive and could ship earlier as a minor if desired.

## Acceptance (the campaign's phase-7 analog)

- The gauntlet re-read: MIGRATION-READ.md regenerated against the REAL implementation,
  read cold by the owner; the never-backwards bar applied to every spelling.
- The refusals ledger extended and re-verified; the parity suite green; the analyzer's
  three diagnostics demonstrated on the corpus; both TFMs 0 warnings; net48 on the
  owner's Windows; the full suite green with the differential obligation intact
  (existing declarations byte-identical through phases 1–4).

## Out of scope

The writer, the IR, presence follow-ups, matcher combinators (compound landmarks —
the conjunctive reading's future home), the two-axis bound vocabulary, `MapResult`
presence exposure — all parked with their own records.
