# Placement Spelling — the ergonomics rounds

**Status:** ITERATING (2026-09-09). The owner's process ruling: keep stubbing developer-
ergonomics scenarios until one feels right. The owner's hard bar, recorded as the first
judgment criterion of any placement spelling:

> **Nothing may read backwards of the actual intent.**

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

## Queued rounds

- Hoisted-reuse spelling: `.Of(irrDetails)` vs alternatives for placing an existing
  projection.
- The `Under` entry's exact shape (spike trial in flight: `Under(params captions)`
  stage + `.Of`/terminal close; differential acceptance reads against today).
- Whatever round 1's reaction surfaces.
