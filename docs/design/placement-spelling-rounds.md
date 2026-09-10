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

**Ruling:** (awaiting owner)

---

## Queued rounds

- Hoisted-reuse spelling: `.Of(irrDetails)` vs alternatives for placing an existing
  projection.
- The `Under` entry's exact shape (spike trial in flight: `Under(params captions)`
  stage + `.Of`/terminal close; differential acceptance reads against today).
- Whatever round 1's reaction surfaces.
