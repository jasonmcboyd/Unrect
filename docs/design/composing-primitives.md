# Composing primitives into streaming projections

**Status: LIVING (current direction, branch `experiment/record-primitive`).** The single source of
truth for the labeled-axes / primitive-composition thread. It replaces ten contradictory step-specs
gutted 2026-09-12 (git history keeps them); do not re-fragment it into a step-chain, and delete this
in favour of code + CLAUDE.md once the direction below is shipped.

## The plot (do not lose it again)

The primitives — `ColumnLabels`, `WithColumnLabels`, `Record` — exist so a **third party** can compose
them into their own labeled region (a right-/bottom-header table, a pivot) and have it work as well as
ours. "As well as ours" **includes streaming.** So the acceptance bar is not "the primitives can
reproduce `Table`'s *values* in a test" — it is "the primitives compose into a projection that
**streams**, and `Table` *is* that composition." If composing them forces the whole block into memory,
we have shipped primitives that don't actually compose into a production-grade projection, and the
third-party promise is hollow. A bespoke streaming `Table` beside forcing primitives is an exemption
nobody else can have — that is the trap we fell into and are reversing.

## Canonical decision (agreed 2026-09-12)

`Table` is **composed from the primitives** and **streams**:

```
VerticalFlow(flow => {
  var columns = flow.Next(ColumnLabels(headerRows));
  return flow.Next(WithColumnLabels(columns, VerticalRepeat(Record(record))));
})
```

Sufficiency is judged including the **forcing profile** (rows materialised at first record, at
completion), not only value / message / A1 / `row.Index`. The bespoke leaf is interim; it is retired
once the composition matches it on all of those.

## Next step (before the build) — catalog the primitives

Before building the GAP-A fix, inventory the primitives. For each building block a larger
projection composes from — the leaves, the layouts (`VerticalFlow`/`HorizontalFlow`/`Overlay`),
the repeats, the label/record primitives (`ColumnLabels`/`WithColumnLabels`/`Record`), the
matchers/`Caption`/`Fields` — record the properties that bear on composition: what it reads, its
default placement and extent, whether it streams or forces its bound, and how it composes with a
parent. That catalog is the map for making the primitives compose-and-stream (and for judging the
leaf/composite question below).

## The blocker and the fix — GAP A (streaming)

Composing `Table` this way makes the flow a **composite**, and the engine resolves a composite's
discovered extent at first-child placement (`Exceeds` + `GetSubspace(offset)`) — the whole block, up
front. The bespoke leaf avoids that only because `TableView.StreamRows` walks a `BoundedSpace`
row-by-row via `HasRow`.

**Fix:** give `VerticalRepeat` that same walk — re-host the discovered bound **inside the repeat** so
the repeat becomes `StreamBands`-as-a-combinator: the flow carries no composite area (it just
accumulates its children's extents), and the repeat streams the body one row past the cursor via
`HasRow`, reusing the exact `BoundedSpace` machinery. No engine change; the repeat is already a
row-by-row walker rather than a slicer.

## Open questions (decide before / during the build — do NOT pre-lock)

1. **Is "leaf" still a real distinction?** Once a composition can stream, "leaf" bundles three things
   that turn out to be separable: **streaming** (achievable by a composite via the lazy repeat),
   **opacity to tooling** (already how layout composites behave — children exist only while the lambda
   runs), and a **flat diagnostic path** (that is GAP B, below). If those come apart, `Table` may just
   be a streaming composition *presented as a named opaque unit*, and the leaf/composite engine
   category may be obsolete. Open — to discuss, not to assume either way.
2. **The repeat terminator's user-facing shape.** `VerticalRepeat(item, within: <area>)` ("fill this
   discovered block") vs a **lazy while-terminator** — a repeat that stops when the next row peeks
   blank, which is the same logic as the shipped `onBlank: Stop` lifted onto the general repeat. The
   second composes with the `onBlank` vocabulary and avoids a second way to say "discovered block".
3. **GAP B — path / subject parity.** The composition's diagnostics read `VerticalRepeat[i] -> Record`
   where the leaf says flat `Table`. If `Table` is to *become* the composition, its diagnostics must
   not regress — a path-flattening / naming decision.
4. **Shared header/body width.** The header consume (`ColumnLabels`) and the body walk must share **one**
   discovered width (one `BoundedSpace` threaded across both) or a ragged / trailing-blank-column sheet
   drifts between the composition and the leaf.

## Already shipped this thread (truth is in code + tests; do not re-describe here)

- `SkipToFirstNonBlankCell` offset strategy — commit `5add5e9`.
- `Table onBlank` blank-row strategy (Stop/Skip/Fault/Tolerate/blankRecord) — commit `1cc6c5a`.
- Uniform offset law (a declared pipeline offset replaces the shape default; one `Steps.Offset` rule) —
  commit `b4838bc`.

## Parked (elsewhere, not here)

Anchors as composable forward-searches for nth-instance addressing (`Below(X).On(X)` = the second X);
trailing/right-header reversal. Both are downstream of the primitives composing cleanly.
