# Labeled axes as projection context (not a space capability)

**Status:** DESIGN, decided in discussion (2026-09-11, `experiment/record-primitive`). Supersedes
the space-capability / decorator direction for **labels** (see `capability-model-fork.md`). Does not
touch `IFormulaSpace`: the static-vs-discovered partition holds — formulas are backend-static and
stay a compile-time capability; labels are content-discovered and become context-scoped values.

## The reframe

A labeling started as "a property of the space." Walking the scenarios showed it is really **scoped
to the projection**: labels flow along the *declaration tree*, not along *space geometry*. Their
*values* are read from the space (header cells); their *scope and lifetime* are the projection's.
So the home is `ProjectionContext`, which already threads exactly this: tree position, sheet
position, diagnostics — and now a **label environment** (a stack of `(axis, LabelMap)` scopes).

Putting labels in the context deletes, for labels: `ILabeledSpace`, the decorator, promotion, the
interface-vs-value fork, the subspace translate+filter *law on the space*, the additive-chart
doctrine, and the "a generic projection can't lift `TSpace`" problem. Labels stop touching the
space's type entirely — which is what makes third-party spaces usable with `Table()` for free (it
needs only the bare `ISpace` indexer to read a header).

## Why context, not geometry (the defining condition is lineage)

Scope is **manufacturing lineage**, not spatial containment. The labeling projection manufactures
subspaces (`Table()` manufactures rows); a label is in scope for the *subtree* it manufactures — the
rows AND everything nested in them — via context inheritance. Geometry is only a post-resolution
safety check.

**Worked example that pins it — two stacked tables.** T1 above T2, both full-width, both
column-labeled, with *different* headers (T1: Amount@3, T2: Amount@5). A row in T2 must resolve
"Amount" → 5. Spatial containment cannot choose: both maps' columns fit inside a full-width band.
Only "this row was manufactured by T2" picks T2's map. **Resolution (`label → ordinal`) requires
lineage before geometry can even run** — you cannot bounds-check until lineage has said which map
answers. A space decorator scoping by geometry gets this wrong; the context gets it right because
"manufactured-by" *is* "inherited-the-context," and only the decomposition knows it.

## The two-part rule

- **Scope (which labels are visible, which map answers):** the labeling projection's declaration
  subtree, via context inheritance. Nothing else sees them.
- **Safety (can a visible label deref *here*):** at the leaf, translate the ordinal from the capture
  frame to the current frame (using the origins the context already tracks) and bounds-check. A
  departed label (a descendant that narrowed the labeled axis) → clean `MissingLabel`
  ("column 'Date' is not in this region"), absorbable, never a wrong-cell read. **Translation is the
  safety**: without it, an offset subspace reads the *neighbor* silently; with it, an out-of-scope
  label overruns loudly.

## Subspace with its own labels — scoping by stacking

Falls out of the scope stack + first-hit-wins resolution:
- **Different axis** → add a scope (row-labeled parent, subspace pushes column-labels) → coexist.
  This is "promote either-to-both" / pivots.
- **Same axis** → push a new scope → **shadows** the inherited one (nearest wins). Lexical scoping.
- Default is **replace, not merge**: a subspace reading its own header declares *its* columns;
  inherited labels it did not redeclare are shadowed (addressing them → `MissingLabel`). Merge, if
  ever wanted, is an explicit combinator building a combined `LabelMap`.

## Vocabulary (decided 2026-09-11)

Standardized on **"Labels"**, not "Captions". `Record` (dimension-agnostic) for the binder. The
value is a **`LabelMap`** — the shipped `CaptionMap` renamed (same concept; alpha, so rename now);
the `eachRow` lambda param `captions =>` becomes `labels =>`. Kept distinct: the `Caption(text)`
**leaf** (asserts an anchor row's text — a heading, not an axis label). Deferred as a small
consistency call: `CaptionComparer` (the shared text-matching rule) → possibly `LabelComparer`.

## The primitives (and the acceptance test)

**Acceptance test: `Table()` must be reimplementable from public primitives.** If it is, third parties
have everything to manufacture labeled axes and provide them to their subspaces. Three primitives,
one per verb:

1. **Manufacture a map** — `ColumnLabels(headerRows)` / `RowLabels(headerCols)` :
   `IProjection<LabelMap>` (reads and consumes the header strip); plus literal `LabelMap.Of(...)`.
   This is the public `LabelMap`, no longer locked inside `Table` (closes the `internal`-ctor gap).
2. **Provide it to subspaces** — the scope-introducer `WithColumnLabels(LabelMap, body)` /
   `WithRowLabels(LabelMap, body)`: pushes `(axis, map)` for `body`'s subtree, maps, pops. The
   **parent** builds the map at its own frame and pushes it; because the parent owns the manufacturing
   of the subtree, the frame relationship to every descendant is known, so leaf-time translation is
   correct by construction. (Not eager per-child copies — see below.)
3. **Read it** — the compute-legal binder `Record(row => …)`, `row.Decimal("Amount")` resolving the
   ambient scope. Translation to the leaf's frame happens **at resolution**, using the context's
   origins — one mechanism, composes to any depth.

`VerticalFlow` / `VerticalRepeat` already ship. The reimplementation:

```csharp
static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<Row, T> record) =>
    VerticalFlow(flow =>
    {
        LabelMap columns = flow.Next(ColumnLabels(headerRows));   // read + consume header
        return flow.Next(
            WithColumnLabels(columns,                              // provide to the body's subtree
                VerticalRepeat(Record(record))));                  // each row reads the ambient columns
    });
```

Faithful: 1×W band per row (repeat over a one-row record); absolute label positions (frame-relative
ordinals + translate); per-occurrence nested headers (a nested `Table` is this same composition
re-evaluated per row). `Table<T>()` reflection rung stays convenience over this. DIY variants
(left/right header, pivot) are compositions of the same three primitives — the right-headered table
that had no public field-binding is now a five-liner in the consumer's dialect.

## Beyond the test: the refactor is the endgame (and the completeness proof)

The acceptance test proves the primitives are *sufficient*. The refactor makes them *load-bearing*:
`Table()`, and the `Column`/`Row`/`Fields` faces of `Record`, are to be **rebuilt entirely from the
primitives, the bespoke internals deleted** — no privileged path, one implementation, the vocabulary
building its own conveniences (this campaign's real subtraction).

Lands only when both guardrails hold:
- **Diagnostics byte-identical.** Some rungs were bespoke precisely to keep their subject/path/
  aggregation (`projection-model-spec.md` §6.1: rung 1 says `column 'Amount': …`, aggregates unbound
  members, keeps the table's path). The rebuild must reproduce these exactly — pinned by
  `CellReadingIdentityTests` + the acceptance tests. Desugaring-to-primitives is what historically
  *lost* this; that is the risk to watch.
- **Lazy/streaming behavior preserved.** The primitives must be lazy-aware so the rebuilt `Table`
  keeps its forcing counts and streaming cost — pinned by `LazyForcingTests` / the cross-door sweep.
  A real constraint on how `ColumnLabels`/`WithColumnLabels`/`Record` are built.
- The **full suite green** is the gate; the reflection rungs (`Table<T>()`, `Table<T>(bind => …)`)
  stay a thin layer *on top* generating the `Record` lambda, not part of what dissolves.

**The reframe:** any bespoke code that cannot be dissolved without a regression is a documented GAP in
the primitive vocabulary — close it by extending the primitives, never by keeping the bespoke path.
The refactor is thus the completeness proof for the algebra: the built-ins earn their place only as
compositions, and what resists composition names the missing primitive.

## Costs and implementation notes

- **One honest cost:** labels are a runtime context lookup — no compile-time "this reader needs
  labels in scope"; a decoupled reader used outside a labeling scope fails at map time with
  `MissingLabel`. Acceptable: label *existence* was always runtime, and the capability's only
  compile-time win was axis-presence for exactly this niche — traded for deleting all the machinery.
- `ProjectionContext` gains a fourth job (the label environment) — the right home conceptually, but
  it nudges the parked `PathRenderer`/`ProjectionContext` split sooner.
- **Pin at build time:** the capture→current-frame translation is *actually applied* — the guarding
  test is a **column-offset** labeled table (the silent-wrong-read case if translation is skipped).
- **Verify at build time:** a projection can hand its child a context with a pushed scope through
  `ProjectionEngine` (the same seam noted for promotion; expected fine, worth a pin).
