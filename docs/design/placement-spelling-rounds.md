# Placement Spelling — the ergonomics rounds

**Status:** ITERATING (2026-09-09). The owner's process ruling: keep stubbing developer-
ergonomics scenarios until one feels right. The owner's hard bar, recorded as the first
judgment criterion of any placement spelling:

> **Nothing may read backwards of the actual intent.**

And the second bar (owner, 2026-09-10, from the monolith reading — "the canonical form
should be dead simple. VerticalFlow, dead simple"):

> **The canonical form is the dead-simple form.** If the most-declared spelling is not
> also the simplest spelling, the vocabulary owes a rung — ceremony in the canonical
> position is a defect, not a price. (`VerticalFlow` passes; `Table`'s bind rung
> currently fails — three ceremonies per field drive users to the opaque hatch — and
> the owed rung is the SLOT-BINDER: `Table(row => new { Investor = row.Text("Investor"),
> ... })`, the owner's slot-cursor idea aimed at rows — lambda runs once, slots record
> (caption, kind), anonymous-of-slots reflected like Table<T>(), full visibility at
> hatch-or-better simplicity. Same cure for Column: `Column(c => new { Title = c.Text(),
> ... })`. Spike-sized risks: slot→property mapping via the anonymous constructor,
> expression-tree construction on netstandard2.0/net48, demand climb for backend slots.)

Method: one contested question per round; the same real declarations every time
(`investor-irr`'s bounded series, the K-1 section); minimal variants; the owner's
read-aloud reaction is the instrument. Rulings accumulate here; the staged-placement
note holds the design they feed.

---

## Round 1 — the bound's position (OPEN)

The one place the owner's execution-order model ("specify the offset, specify the
size, project the space" — `Until` is a size spec) and the geography reading (the
landmark is below, so it spells below) disagree.

### Variant A — bound up front (execution order)

```csharp
var byTransferDate =
    Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
    .Until(RowContaining(Inception))
    .Of(irrDetails);

var lines =
    On(RowContaining("K-1 Lines 1-21"))
    .Until(RowContaining("Portfolio Income"))
    .VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("K-1 Lines 1-21")),
        Lines:   v.Next(kLines)));
```

Read: "under these headings, bounded before that row — the blocks." The whole address,
then the occupant.

### Variant B — bound at the end (geography)

```csharp
var byTransferDate =
    Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
    .Of(irrDetails)
    .Until(RowContaining(Inception));

var lines =
    On(RowContaining("K-1 Lines 1-21"))
    .VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("K-1 Lines 1-21")),
        Lines:   v.Next(kLines)))
    .Until(RowContaining("Portfolio Income"));
```

Read: "under these headings — the blocks — ending before that row." The spelling walks
down the page.

Both satisfy the never-backwards bar by their own logic: A's order is the INTENT's
(state the region's whole address, then its occupant); B's is the PAGE's. The round
asks only: read aloud, which one do you stop noticing?

**Ruling (owner, 2026-09-10): VARIANT A — the bound is a stage; all geometry precedes
the subject.** The owner's reasons: (1) no chained projections — under B, postfix
`.Until` would be the sole surviving postfix geometric operator, a lone exception to
"everything about the region precedes it"; (2) the argument-form correspondence —
`Offset().Size().Projection()` linearizes exactly to `Placed(offset, size, content)`,
so the fluent surface and the canonical (IR) representation state their parts in the
same order. The geography law is DEMOTED to a special case: it survives as the reason
captions precede content (above and pre-content agree), while the bound follows
FUNCTION (extent specification) over coordinates. The resulting total grammar:
**stages, then the subject, then nothing geometric** — remaining postfix operators
(`.Named`, `.Optional`, `.OrBlank`, `.Select`) are about the value or its reading,
never geometry.

Corollary settled with the ruling — the canonical stage order (the trial's
`UnderStage`-offers-only-terminals finding): **anchors/offsets → bounds/sizes →
`Under` → subject** (locate, bound, headings, content). This section's Variant A
snippet has `Under` before `Until` — superseded by the corollary; the canonical
spelling is:

```csharp
var byTransferDate =
    Until(RowContaining(Inception))
    .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
    .Of(irrDetails);
```

Build consequences: the `Until` stage un-supersedes (built, differentially verified);
bound-only entries return for hoisted reuse; postfix `.Until` retirement joins postfix
`.Under`'s in the deferred phase-5 decisions.

---

## Migration evidence for the phase-5 bound-spelling retirement call (2026-09-10)

Phase 4b's honest report, on record for the owner's eventual decision about postfix
`.Until`'s fate: the corpus's own `investor-irr` — the script whose whole lesson is
where the series STOPS — reads slightly better under the geography spelling
(`Heading(a).Heading(b).Of(x).Until(l)`, words in sheet order) than under the canonical
ruled order (`Until(l).Heading(a).Heading(b).Of(x)`, engine order), which was used as
instructed. Both are pinned as ONE declaration, so this is purely a reading
observation — but it is real-corpus evidence, exactly what the deferred retirement
decision said it wanted. Also from the migration: `Sized` has no pipeline entry (per
the ruled entry list), so an extent-only placement (scrubbed-k1's header) is the one
corpus site the pipeline cannot spell position-first — it stays the postfix modifier,
with a comment in the script saying why.

## The Record unification and the cardinality family (owner + evaluator, 2026-09-10, ergonomics-first exploration — SYNTAX-ONLY, no code yet)

Started from the slot-binder (below) and grew into a larger consolidation. Recorded as
design exploration; nothing built.

**The slot-binder** — the "canonical is dead-simple" fix for `Table`/`Column`'s opaque
lambda hatch. Syntax: `Table(row => new { Amount = row.Decimal("Amount"), ... })` where
`row.Decimal("cap")` returns the VALUE type (not a `Slot<T>`), recording the binding
(caption/index, kind) as a side effect on `row`; the lambda's return type is the natural
anonymous value type, inference clean. Runs once at bind time to record. Open forks:
- **re-run vs materialize-once** — materialize-once (never re-run the lambda; build the
  anonymous result from recorded bindings) is favored on THREE converging arguments:
  it enforces the class-4 boundary, keeps fields visible, and is the only design where a
  computed field (`Net = C - D`) is structurally impossible in the read — which is the
  only design where the WRITER can exist (computed fields must live in a post-read
  `Select` lens, where forget-and-rederive is a clean identity: `read∘write = id` on
  consistent records; the writer stores the base columns and re-derives Net).
- **capability reach** — `row.Formula("Amount")` works iff `row : RowBinder<TSpace>` with
  `TSpace : IFormulaSpace`, via a BACKEND EXTENSION METHOD (same recipe as the `Formula()`
  leaf); the demand is DECLARED at the rung (how the scope typed `row`), not climbed. This
  is the first time capability typing reaches INSIDE row-reading — a capability EXPANSION,
  not just a simpler hatch. Third parties extend with their OWN space symmetrically IFF the
  binder's recording seam is PUBLIC (precedent: the `ISpaceChart` transport is public for
  exactly this) — cost: `RowBinder<TSpace>` becomes permanent public surface, and a
  capability field's reader needs space access (two-shape seam: `Func<CellValue,T>` plain,
  space-access for capability). THE spike's central target: "is a third party's `row.Color()`
  as first-class as `row.Formula()`?"
- **caption AND index keying** — the binder must support both (`row.Decimal("AMOUNT")` and
  `row.Text(8)`), because the founding real case (buying-power, header `,,ACCOUNT,AMOUNT,,SHARES,,,`)
  has partial headers. Caption = discovered/named position (robust, survives reorder); index =
  hard-coded count (fragile, for structurally-fixed/headerless columns only) — the SAME
  discovery-vs-explicit law as placement, one layer down. Cost: overload matrix inflates.

**The Record unification** — `Column`, `Row`, `Fields` are three corners of a 2×2
{labelled × orientation} space, all reading ONE record (a singleton); the fourth corner
(labelled-horizontal) is the eachRow projection, which exists today but is trapped inside
`Table`, only ever repeated:

|            | vertical            | horizontal          |
|------------|---------------------|---------------------|
| unlabelled | `Column`            | `Row`               |
| labelled   | `Fields`            | *(eachRow, trapped)*|

Proposed: ONE `Record` primitive (bounded region → single named-field result, fields by the
slot-binder, axis + label-source as parameters). `Table` = `Record` repeated;
`Column`/`Row`/`Fields` = `Record` once. The COUNTERWEIGHT is the mirror law's precedent:
row/column were kept as SEPARATE NAMED operators (one denotation) because diagnostics own
the axis vocabulary and transpose corrupts A1 — so the likely resolution is one Record
MECHANISM with the four faces kept as named orientation-fixed sugar, not one
`Record(orientation:, labels:)` op with generic diagnostics. Fork: collapse-to-one vs
unify-mechanism-keep-names (mirror law chose the latter).

**The cardinality family** — the owner's "or we need a First() in the algebra" resolved to
an AND, orthogonal to Record. `First`/`One`/`Only` is the cardinality CEILING companion to
the repeat's existing `atLeast` FLOOR (one family: min/max/exact bounds on a repeat) — the
algebraic, DECLARED, invertible replacement for LINQ `.First()` (which drops out of the
algebra AND leaves cardinality unstated). Invertibility-safe: cardinality is a read-side
assertion (the audit's `Repeat(atLeast:n)` writes value.Count result). NOT a substitute for
Record on singleton readers — `One(Table)` extracts the first ROW of a vertical-repeat (the
labelled-horizontal corner), but `Column` is a vertical strip that never repeated, so
forcing it through `One` fights orientation. The split: **Record = "one thing with fields"
(direct, the atom); cardinality quantifiers = "bound a genuine repeat's count" (the ceiling
to atLeast).** You'd use `Only` to grab the one summary row from a section that structurally
could have several; never to read a header block.

## Queued rounds

- **The terminology + naming sweep** (owner + evaluator, 2026-09-10, from the monolith
  reading): (a) "shape" is reserved for GEOMETRY — the extent of space; the thing that
  reads it is a projection; evict the colloquialism from prose. (b) The three-language
  rule, currently implicit, stated explicitly in vocabulary.md with the triple table:
  kinds speak the document (`Text`, `Temporal`), accessors speak what C# receives
  (`GetString`, `GetDateTime`), leaves speak the asking-language (`Text()`, `Date()`).
  Audit the leaf set against it: `Double()` is the genuine outlier (CLR-speak in
  asking-language; defend as the priced conversion exception or reconsider). Uniform
  CLR leaf names (`String()`, `DateTime()`) are REFUSED — and the owner's follow-up
  ("why doesn't Decimal() collide?") sharpened the refusal with a measurement: EVERY
  BCL-type leaf name collides in member-access position (CS0119, the imported method
  hides the type — `Decimal.MaxValue` breaks today, measured), while invocation and
  type-declaration positions stay clean. The shipped leaves are safe only because
  their types are keyword-aliased (`decimal.MaxValue` sidesteps); `DateTime` has no
  keyword, so `DateTime.Now` — ubiquitous — would break with a confusing error.
  Refined rule: a leaf may share a BCL type's name only when a C# keyword makes the
  capitalized spelling unnecessary. Docs owe one line: in declaration files, write
  keyword spellings (`decimal.MaxValue`, `bool.Parse`) — the leaves shadow the
  capitalized names in member access. (c) Style notes queued from the same reading: inline-vs-hoisted trade
  (geography visible vs failure paths named), uniform-`v` cursor naming blessed
  (C# 8 shadowing; net48-default LangVersion 7.3 floor noted), accessor-vs-leaf
  layer guidance (field-wise accessor use is the leaf layer's job).

- **`Table`'s leading-blank projection-level default, questioned** (owner, 2026-09-10,
  reading the inlined monolith): "I can't imagine a scenario where we will want to
  capture blank rows and columns by default. But perhaps I lack imagination." The
  purist alternative — tables go adjacent, the corpus gains explicit `AfterBlankRows()`
  — is writer-friendlier (one less anonymous consumption). Design discussion
  deliberately deferred by the owner; both positions recorded here so it starts warm.


- Hoisted-reuse spelling: `.Of(irrDetails)` vs alternatives for placing an existing
  projection.
- The `Under` entry's exact shape (spike trial in flight: `Under(params captions)`
  stage + `.Of`/terminal close; differential acceptance reads against today).
- Whatever round 1's reaction surfaces.
