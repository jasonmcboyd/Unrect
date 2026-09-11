# Capability model fork — interfaces vs. decorator/values

**Status:** PARTIALLY RESOLVED (2026-09-11, `experiment/record-primitive`). Seeded mid-Record-campaign
when the labeled-space idea surfaced the interface-explosion worry. **The labels half is decided:**
labels are content-discovered and become **context-scoped values**, not a space capability — see
`labeled-axes-and-context.md`, which supersedes the decorator/capability direction for labels. What
remains open here is only the *general* question for any *future* discovered capability; the
static-vs-discovered partition (below) is the working answer, with `IFormulaSpace` on the static side.

## The fork

Today a capability is an **interface** (`IFormulaSpace`), implemented statically, demanded by a
type constraint, found by `is IFormulaSpace` in the `ISpaceChart` walk. Jason's proposal: make a
capability a **value/object** (`FormulaCapability`) carried in a per-space bag, composed by
decoration, queried at runtime — "this could eliminate the other spaces."

## What the value model buys

- **The interface explosion vanishes.** No capability interfaces to combine, no bundles
  (`ISpreadsheetSpace`), no crossings (`IRowLabeledSpreadsheetSpace × every backend`). You compose
  *values*, and values compose freely (a keyed bag).
- **Promotion is trivial and uniform** — add one more capability to the bag. No additive-chart
  doctrine, no per-instance type juggling (see `capability model was already per-instance` in
  `SpaceCapabilities` / `SpreadsheetGridSpace` / `BoundedSpace`).
- **It matches runtime discovery** — capabilities read from content exist only at map time; values
  compose at runtime, interfaces are static.

## What the value model costs

- **The compile-time demand layer** — Entry C (`using static ProjectionBuilders<TSpace>`), the
  scoped `Over<TSpace>()`, the CS1503 diagnosis win, `MapWorkbook`'s refusal-by-overload. With a
  value bag, "does this space have formulas" becomes a *runtime* query → null → fault at map time:
  exactly the runtime-failure surface the projection-model campaign fought to eliminate. A wholesale
  switch reverses shipped, valued design.

## The synthesis to test (not yet decided)

The two models may **partition by when the capability is known**, not compete:

- **Statically known** (backend-intrinsic, chosen in code — `CreateWithFormulas` vs `Create`):
  stays an **interface** (`IFormulaSpace`). The type reflects a decision the author made; the
  compile-time check is real and earns its keep.
- **Discovered at map time** (read from content — labels/captions): becomes a **value**
  (`CaptionMap`). It can't be a compile-time type — it doesn't exist until the header is read — so an
  interface for it is a fiction. This is exactly the `CaptionMap`-as-value the Record campaign
  already needs (see `record-campaign-spec.md`).

Under that line the interface explosion lives **entirely on the discovered side**, which the value
model handles at zero cost — so we may keep `IFormulaSpace`'s compile-time check *and* dodge the
explosion, without reversing the shipped layer. The open question is whether the partition holds for
every capability we can foresee (formatting? — backend-static, so interface) or whether something
sits awkwardly across the line.

## Relation to the labeled-space work

If the synthesis holds, labels never become a space capability at all in the Record campaign — they
are `CaptionMap` values handed to lambdas (mechanism 1). The `IRowLabeledSpace`/`IColumnLabeledSpace`
capability + promotion + additive-chart doctrine (last turn's design) would only be needed for the
narrow "labels travel with the space across a boundary" case, and this fork may retire even that in
favour of a `LabelCapability` value if the value model wins for discovered capabilities.
