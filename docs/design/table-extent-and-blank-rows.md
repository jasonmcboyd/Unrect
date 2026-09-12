# Table extent & the blank-row strategy — converging design

**Status:** CONVERGING DESIGN (2026-09-11), not built. Emerged from the step-3 GAP-A discussion and
**supersedes the `within:` / lazy-repeat-terminator approach** (`labeled-axes-step3a-gap-a-spec.md`) as
the cleaner framing. Feels right to the owner but not finalized — spellings/names still open.

## The root insight
**A projection should not implicitly decide its own extent.** Today `Table()` bakes in
"I end at the first blank row" (its `DiscoveredBlock` = `RowsWhileAnyValue` placement default). That
single implicit decision is what generates all three symptoms we chased:
- **GAP A (forcing):** the implicit bound is a discovered area, and on the composite reimplementation
  it forces up front.
- **Can't express `Until("GrandTotal")`:** the internal blank-stop fires before the landmark.
- **Can't skip interior blanks** (writer emits null/0 as blank): the blank-stop stops prematurely.

Strip the implicit decision out → the symptoms dissolve or narrow to a small, honest optimization.

## The fix: Table goes extent-agnostic
`Table` becomes "a header over records" with **no imposed extent**. Its extent **emerges** from what its
children consume (a bare flow already sizes to its children — lazy by construction; no discovered bound
on the composite, so no GAP-A forcing). Termination is **declared**, not baked.

## The blank-row strategy (the named knob for the body)
How a blank row is treated as the body is read. Three orthogonal axes; the owner's five presets are
points in that space:

- **Control:** continue | stop
- **Value** (if continue): skip (no record) | project(p) (produce one)
- **Diagnostic** (independent): none | info (nonterminal, keep going) | fault (terminal, propagate)

| Preset | Control | Value | Diagnostic |
|---|---|---|---|
| skip row | continue | skip | none |
| **stop on row (default)** | stop | — | none |
| terminal error | stop | — | fault |
| nonterminal diagnostic | continue | skip | info |
| blank-row projection | continue | project | none |

Notes:
- **terminal / nonterminal are not new machinery** — they are stop+fault and continue+info over the
  existing diagnostics model (`IsFault`, Info, `.Optional`/`.Else` absorbing nonterminal).
- **blank-row projection is the escape hatch** (custom/null-record value). It does NOT subsume *skip*:
  skip *omits* the entry, project-to-null *includes* a null entry — a real difference.
- Named presets are the ergonomic surface; the axes are how we check the set is complete (and reveal
  legit combinations like "note + project" if ever needed).
- **Blankness** = fully-blank row (every cell `IsBlank`), the complement of `RowsWhileAnyValue`. A row
  with some null/0-as-blank cells but real content is *data*, never triggers the strategy. Caption-scoped
  blankness ("blank if the key column is blank") is a further, deferred knob.

## Boundary — a separate, secondary concern
`stop` is self-bounding (the blank *is* the end). `skip`/`project` are NOT — they need a **boundary**:
`Until("GrandTotal")`, count, to-edge — declared in the pipeline. So the full picture is two orthogonal
things: the **blank-row strategy** (primary; what `Table` wrongly hardcoded) and a **boundary** (only for
non-self-bounding strategies).

## Distinct from `VerticalRepeat`'s separator concern (owner flag)
`Table`'s items are *rows* (1 tall), so a blank *row* is the unit the strategy acts on — this set applies.
A general `VerticalRepeat`'s items are multi-row *blocks*, so a blank row is a *gap between items* — the
`separatedBy:` concern (blank = separator). Cousins, not twins: row-level vs gap-level. Do not force one
strategy type onto both; a blank row *interior* to a block item is a third thing again.

## How this resolves the whole thread
- **GAP A (laziness):** closed with **no engine change**. `Table` carries no composite bound (extent
  emerges); the blank-row strategy is evaluated **lazily on the walker** (peek the next row, apply). The
  reactive-composite engine change stays parked — unneeded for tables.
- **Expressiveness (GrandTotal, skip-blanks):** fixed — strategy `skip` + boundary `Until("GrandTotal")`.
- **The `within:` "new paradigm" discomfort:** dissolved — termination is a **blank-row strategy** (a
  natural, named, pluggable policy fitting the existing strategy vocabulary), not a special extent-bound
  the walker reaches up to set. And `SizedToChildren` was redundant (it *is* the bare-flow default).

## Row-wise is lazy, column-wise is eager (accepted tradeoff)

The blank-ROW strategy is lazy (row-blankness = read *across* one row, peek). Its dual — blank-COLUMN
work — is inherently **eager**: to know a column is blank you must read *down* it (full height), and a
prefix read is WRONG. A headerless table

```
   B1 C1
A2 B2 C2
```

read only at the top would conclude "columns start at B," missing column A's content in the rows below.
Same substrate-cost asymmetry as row-labeled tables forcing the height: on a row-major reader,
column-wise work buffers.

Consequences (owner: **accepted** — the cost is the substrate's, not the algebra's):
- **Width should come from the header** — read one row across, its non-blank cells *are* the columns.
  Headered tables are fully lazy and never scan down columns.
- **Headerless width-from-content is inherently eager** (must read the full height to place columns
  correctly). A headerless table read *by index* needs no width discovery; only "discover which columns
  exist" forces.
- **Offset tiers into lazy default + eager escape hatch:** `SkipToFirstNonBlankCell()` is the lazy
  default (down to the first non-blank row, across to the first non-blank cell — no full-height scan);
  `SkipBlankRowsAndColumns()` is the explicit, eager escape hatch for patchy/ragged regions where the
  corner heuristic misjudges the bounds (a column appearing only below the first content row), cost
  accepted. The names read their cost.

Principle: **row-wise boundaries are lazy (along the streaming axis); column-wise boundaries are eager
(across it).**

## Resolved call-site design (2026-09-11)

- **Blank-row policy is a `Table` parameter** (`onBlank:`), not a pipeline stage — it's a *reading*
  concern, and a stage would collide with the leading offset. Presets: `Stop` (default, self-bounding),
  `Skip`, `Fault`, `Tolerate`, `Project(p)`.
- **`Project` receives the blank row's `TableRow`** — including `.Index` (the step-2 GAP-C ordinal),
  usually all a blank row has to offer: `onBlank: Project((TableRow b) => new Spacer(b.Index))`.
- **Boundary is a pipeline stage** (`Until("GrandTotal")`), needed only for the non-self-bounding
  policies (Skip/Fault/Tolerate/Project); `Stop` needs none.
  - **No boundary declared → run to edge (RESOLVED 2026-09-12, owner).** A non-self-bounding policy
    without a boundary is NOT a misuse — the table just runs until *something outward* forces a stop:
    the parent's bound, and failing that the outermost extent (for a spreadsheet, the sheet's used
    rows). "Unbounded" = "run until forced to stop; it resolves itself." So `Table<SomeEntity>()` on a
    clean top-left sheet, no offset/size/boundary touched, parses the whole sheet in one line. (With the
    default `Stop`, a clean no-interior-blank table reaches that same end at the first fully-blank row
    past the data, so the one-liner behaves identically; the boundary only matters once interior blanks
    must be *skipped* rather than *stopped on*.) Mechanically: the non-self-bounding height is
    "all remaining rows to the enclosing edge" (not `TakeRowsWhileAnyValue`, which is `Stop`), still
    lazy on the streaming door — the walker peeks each row once and applies the policy.
- **`Table`'s defaults are sensible but OVERRIDABLE** — the resolution of "extent-agnostic vs
  convenient": the sin was *hardcoded* defaults, not defaults per se.
  - offset: **TWO node-type defaults (RESOLVED 2026-09-12 — supersedes the 2026-09-11 "no baked default"):**
    - **LEAF default → `SkipToFirstNonBlankCell`.** A leaf self-locates its content corner. **Safe re
      orphaning because a leaf has no children to orphan** — the orphaning counterexample is composite-only
      (a composite that column-skips moves its origin and places children under it). Since `Table` is a
      LEAF, it self-locates, so `VerticalFlow(v => { v.Flow(Of(t1)); v.Flow(Of(t2)); })` parses with zero
      explicit placement.
    - **COMPOSITE default → `Adjacent().SizedToChildren()`.** No column-skip → no orphaning. The flow
      assigns full-width row-bands; each leaf skips within its band.
    - **Residual (leaf, rare, overridable):** `SkipToFirstNonBlankCell` finds the *first row's* first
      non-blank cell = the region's true corner ONLY if top-left-aligned. A headerless-ragged table (lower
      rows extend left of the first row — the `A2/A3` case) starts at the wrong column and loses the left
      part. Distinct from orphaning; the node-type split does NOT fix it. Rare (headered tables locate via
      the header; most headerless are top-left-aligned) → override with explicit `adjacency`/positional.
    - This reverses the 2026-09-11 "no baked default", justifiably: that call was driven by orphaning,
      which turns out to be composite-only, so a leaf default is orphaning-safe and almost-always-wanted.
  - size: **`SizedToChildren()`** (emergent — the extent-agnostic model).
  - extent: **`onBlank: Stop`** (lazy, peeked one row ahead on the walker).
  - Common table stays the one-liner `Table(r => …)`; override any of the three when the file needs it.
- **`silence = adjacency` STAYS universal (corrected 2026-09-11 — do NOT re-scope):** `VerticalFlow` /
  `VerticalRepeat` keep `adjacency` — silently skipping blanks is an *opinion* with no place in a general
  primitive (hidden behavior; breaks tightly-packed/positional uses). Their config is separation/
  repetition (`separatedBy`/`atLeast`/termination), NOT `onBlank`. **`Table` alone** carries
  `SkipToFirstNonBlankCell()` as its own default, because "a table region" is a specific document
  concept. Zero-placement still works and is *cleaner*: the gap-absorption lives in **`Table`** (each
  table self-locates its content), not the flow — `VerticalFlow(v => { v.Next(summary); v.Next(details); })`
  parses with no explicit placement because the flow stacks the tables *adjacently* and each `Table`
  skips its own leading gap. **`Table` is composed OF `Vertical*` but is not them**; the primitives get
  their own config, distinct from `Table`'s `onBlank`/`blankRecord`.
- **Why the column-skip is Table-only, not global — the decisive counterexample:**
  ```
      xxx      region 1, cols 4-6
      xxx
               (blank)
  xxx          region 2, cols 0-2
  xxx
  ```
  A document-level `VerticalFlow` with a *global* `SkipToFirstNonBlankCell` would move its origin to
  `(0,4)` (region 1's corner). Region 2 then sits at column **−4** relative to that origin — off the
  left edge, unreachable forever. So `SkipToFirstNonBlankCell` is NOT a benign superset of adjacency:
  the *row*-skip is (keeps full width), but the *column*-skip **moves the origin rightward** and orphans
  any later region in earlier columns. It is therefore fatal at the composing/flow level and safe only
  *inside a self-contained region* (`Table`, whose data is all at/after its own corner). The flow keeps
  full-width row-bands (`adjacency`); each `Table` locates its corner within its band — region 1 finds
  `(0,4)`, region 2 finds `(3,0)`, both captured because the flow never moved its column origin. This is
  why global skip-to-content was rejected.
- **Error-detection is shape-matching's job, not the offset's** (corrected — the earlier "adjacency
  catches a missing region" claim was wrong): a region's own `SkipToFirstNonBlankCell` skips the gap
  regardless of how the flow positioned its slot, so adjacency and skip-to-content land in the same
  place and fail (or don't) via the same downstream shape-mismatch. Skip-to-content therefore has NO
  detection disadvantage vs adjacency. "Seek-found-nothing is loud" catches a missing region at the
  END; a missing MIDDLE region is caught only via shape-mismatch (see Parked below).

Example call sites:
```csharp
Table((TableRow r) => new Tx(r.Text("Investor"), r.Decimal("Amount")))                 // default
Until("GrandTotal").Table((TableRow r) => new Tx(...), onBlank: Skip)                   // skip interior blanks
Until("GrandTotal").Table((TableRow r) => new Tx(...), onBlank: Fault)                  // blank = malformed
Until("GrandTotal").Table((TableRow r) => new Tx(...), onBlank: Tolerate)               // skip + Info
Until("GrandTotal").Table((TableRow r) => new Tx(...), onBlank: Project((TableRow b) => new Spacer(b.Index)))
SkipBlankRowsAndColumns().Table((TableRow r) => new Tx(...))                            // eager, patchy region
```

## Parked: shape-mismatch detection (important, not solved now)

A mis-positioned region — a missing or misaligned section, so a projection lands on the *wrong*
content — is caught today **only** by shape-matching: the projection failing because the found content
does not fit the declared shape (missing captions, wrong kinds). That is IMPERFECT. If two adjacent
regions have *similar* shapes (both carry an "Amount" column, say), a mis-seek can pass the bind → a
**silent mis-parse**, and with size-to-content the mis-size can cascade to everything after it. This is
**offset-independent** — adjacency has the exact same hole; no placement rule closes it.

It is an important failure class worth catching deliberately — candidate directions (none chosen): a
region asserting a *distinguishing* landmark it must find; a confidence/ambiguity signal when a bind
"barely" matches; a post-parse cross-region consistency check (like the existing summary-count ==
detail-count validations). **DEFERRED** — recorded so it is not forgotten, not to be solved in this
campaign.

## Future (recorded, NOT built): inherited placement (WPF-style)

Pays back the explicit-everywhere cost *without* baking defaults into shapes. An **inherited placement
default** — WPF property-value-inheritance style (like `DataContext`/`FontSize` propagating down the
visual tree) — carried on `ProjectionContext`, the same mechanism as the label environment. The author
sets it once at a subtree; descendants inherit it:
```csharp
VerticalFlow(childrenDefaultOffsetAndSize: SkipToFirstNonBlankCell(), v => { v.Next(Table(...)); ... })
```
- Shapes stay free of baked defaults (principled); the author declares it once (ergonomic); scoped per
  subtree; overridable per child.
- Generalizes "silence is adjacency" → **"silence is the nearest inherited placement default"**
  (adjacency when unset) — parameterizes the law, doesn't break it.
- **Safety rule (mandatory):** it sets the CHILDREN's default, applied *within each child's full-width
  band* — NOT the flow's own offset. Each child column-skips within its band (finds its corner); the
  flow stays `adjacency` (full-width row-bands); the two-region counterexample stays safe (no global
  origin move, no orphaning).
- **Caveat — WPF's spooky-action-at-a-distance:** a child's placement depends on a distant ancestor's
  setting, invisible at the child's call site. Mitigate by setting it close (nearest flow, not the root)
  and surfacing the effective placement in the decomposition trace/diagnostics. Ragged-headerless
  children still override explicitly (inheritance makes the default *settable-once*, not breakage-proof).

**Status: niche, deferred (demoted 2026-09-12).** The two node-type defaults (leaf →
`SkipToFirstNonBlankCell`, composite → `Adjacent`) subsume this for the common case — you no longer
*need* to inherit the default; leaves simply have it. Inherited placement drops to a niche convenience
(e.g. overriding the leaf default across a whole subtree, or pushing a non-default placement down), not
a necessary mechanism. Ship the node-type defaults now; inheritance is a later maybe.

## Still open
- Value names: `Fault` vs `Error` vs `Reject`; `Tolerate` vs `Note` vs `Warn`. (`onBlank`, `Skip`,
  `Stop`, `Project`, `SkipToFirstNonBlankCell` settled; `SkipLeadingBlanks` a possible shorter alias.)
- Whether `VerticalRepeat` gets an `onBlank` analogue (its blank concern is the *separator* one —
  cousins, kept distinct).
- Caption-scoped blankness ("blank if the key column is blank") — deferred.
