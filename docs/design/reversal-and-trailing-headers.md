# Reversal and trailing headers — early design discussion

**Status:** EARLY DESIGN DISCUSSION (2026-09-11), exploratory and PARKED. Nothing here is decided or
built. It arose while exploring how the labeled-axes primitives (`docs/design/labeled-axes-and-context.md`)
would extend to right/bottom-labeled tables. The shipped surface covers **leading** headers only
(`VerticalTable`/`HorizontalTable` — top/left); trailing headers stay "roll your own" until a real need
justifies building any of this. Recorded so the reasoning survives rather than being re-derived.

## The problem

A trailing header (labels in the LAST row / LAST column) breaks the natural assumption a normal flow
relies on: **read-order = spatial-order.** The header must be *read first* (to build the label map)
but sits *spatially last*. Any construct that supports it must decouple those two orderings, so it
will inherently "point two ways" — that is the defining feature, not a wart to design away.

## Three tiers (only the first ships)

1. **Leading headers** — `VerticalTable`/`HorizontalTable`. Shipped surface.
2. **Trailing headers today** — a consumer's own `Overlay` dialect: locate the header by position,
   read it into a `LabelMap`, bound the body to exclude it. Fiddly + eager, but expressible now.
3. **Trailing headers cleanly** — the two mechanisms below, at two layers. Not built.

## Two layers (the key framing)

A trailing header is "a leading header on a reversed axis." The reversal can live at either of two
layers, and they are NOT rivals — they answer different questions, so both could exist:

- **Declaration combinators** — `ReverseVerticalFlow`/`ReverseVerticalRepeat` (+ horizontal twins).
  Change one construct's placement/read direction; coordinates untouched. For *declaring* a structure
  where a section sits at the bottom or a list runs backwards. Fine-grained.
- **Space transform** — `FlipVerticalOrientation`/`FlipHorizontalOrientation`. A coordinate reflection
  on the substrate (`r ↔ H−1−r`); everything read through it sees the sheet flipped. For *reusing an
  existing* top-header declaration, unchanged, on bottom-header data. Coarse, whole-view.

Use-case split: "I have an existing declaration and flipped data" → flip the space. "I'm declaring a
structure with a trailing section" → the combinators. This dissolves the intuition split (below).

## The non-propagation rule (the sharpest decision-lean)

A poll on "does reversing the flow also reverse the repeat inside it?" would **divide people** — and a
divided poll is a warning that whatever you pick bakes in a surprise for half your users. The escape
is to remove the thing being polled: make the combinators **non-propagating**. `ReverseVerticalFlow`
reverses only *its own band allocation*; it does NOT flip the world its children see. Then the two
directions are **orthogonal knobs** and all four combinations are reachable:

| Flow | Repeat | Header | Rows out |
|---|---|---|---|
| `VerticalFlow` | `VerticalRepeat` | top | `[Row1, Row2]` |
| `ReverseVerticalFlow` | `VerticalRepeat` | **bottom** | `[Row1, Row2]` ← the common trailing-header case |
| `ReverseVerticalFlow` | `ReverseVerticalRepeat` | **bottom** | `[Row2, Row1]` |
| `VerticalFlow` | `ReverseVerticalRepeat` | top | `[Row2, Row1]` |

A *propagating* model welds the two, making rows 2 and 4 unreachable without a re-reverse fight — and
row 2 ("header at bottom, rows in file order") is the shape most bottom-labeled sheets actually have.
So the rule: **reversal is local; nothing you didn't name reverses.** Replaces "which way does it
propagate?" with a single trap-free rule.

Under this model, `ReverseVerticalFlow` runs its children in *declaration* order (so a labeling header
declared first is still *read* first — labels ready) while *placing* them bottom-up (header lands at
the bottom). Read-order and spatial-order are decoupled deliberately; content within each band reads
top-down as normal.

## Composition (nesting the two, to any depth)

Surmountable, and it's the same coordinate-translation discipline the capability/slicing framework
already runs — a flip is another translating wrapper (a reflection, not a slice). Hold one line:
**every piece is strictly frame-relative.**

- **Flips** transform the *presented* frame and compose as coordinate functions: `Flip ∘ Flip = id`,
  and the parity of flips is the current orientation relative to the file.
- **Combinators** act in *whatever frame they are handed*, frame-agnostic (a `ReverseVerticalFlow`
  inside a flipped space places at the *presented* bottom = the true top, and neither knows nor needs
  to know a flip happened).

So a deep nest never needs global reasoning — the net is the composition of local rules.

**The one localized challenge is A1 under flips.** Combinators are honest by construction (coordinates
never move; A1 via context origins works). Flips are where it bites: A1 is computed from context
origins, which sit in the *flipped* frame, so a naive flip reports true row 12 as `H−12`, and nested
flips stack the error. The clean fix: **A1 resolution walks the space's transform stack down to the
true file coordinate** (exactly like the capability walk finds a capability through the chart chain),
instead of assuming context-origin == file-coordinate. Do that once and flips compose honestly to any
depth. So the flip layer carries exactly *one* obligation (the A1-walk) to earn its whole-declaration
reuse; the combinators carry none — which is the sharpest reason the combinators are the *cheaper* tool.

## How `ReverseVerticalFlow` determines boundaries

It must **reach the end first** — the reverse cursor starts at the bottom, and you can't place the
first child until you know where the bottom is (force `H`, or find the last content row). So it gives
up the normal flow's stream-from-the-top laziness. "Reading up" splits:

- **Reaching the end (force `H`)** — always required. Inherent, the trailing cost.
- **Scanning upward to discover a child's size** — only if a bottom child has a *discovered* size. A
  **fixed-size** header (`ColumnLabels(1)` = 1 row) needs none — place at `[H−1, H)`, body reads
  `[0, H−1)` top-down. A **discovered-size** bottom-anchored section would need *reverse scans*, which
  the forward-only incremental machinery (`IRowScan`) does not have — a real gap, sidestepped by the
  fixed-size header case.

## Cost is the substrate's, not the algebra's

The two axes are symmetric in the *algebra* but not in the *streaming cost*, because of where labels
live: a **top header** spans the width (forces one cheap row, streams the expensive height) → streams
well on the row-major door; a **left/right header** spans the height (must read every row to collect
row-labels) → eager in height by necessity. A row-major reader buffering for column-wise work is the
oldest fact in parsing, not a Unrect tax. The mirror law keeps the algebra neutral so the *substrate*
decides the cost: `VerticalTable` is the hero on the row-major streaming door; a **column-major**
substrate (columnar format, or a transposed/column-windowed reader) would make `HorizontalTable` the
hero with zero algebra changes. So `RowLabels`/`HorizontalTable`/trailing-header deferrals are
cost-sequencing calls for the current substrate, never demotions.

## Open / undecided

- Whether to build *any* of this (leading headers cover the common need).
- Names (`ReverseVerticalFlow` / `FlipVerticalOrientation` are working names).
- The A1-walk (route A1 through the space's true-coordinate resolution) is the flip layer's ship
  requirement if flips are ever built.
- Reverse-scan machinery for discovered bottom-anchored sizes (only if a discovered-footer case ever
  demands it).
