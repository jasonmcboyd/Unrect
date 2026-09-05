# Capability Seam — pre-tag verification (2026-09-02)

**Status:** verification note, written before the first NuGet tag. The capability seam
(canonical-model-and-shapes.md's deferred roadmap item: backend extras like formatting and
native value types, under the rule that nothing in Core may require a capability) is NOT
implemented — this note verifies that the v1 contract leaves it fully implementable
**additively**, and records the recipe so future capability work follows one pattern.

## The question

Can a concrete space (e.g. `SpreadsheetSpace`) later expose features other spaces lack —
cell formatting, richer/native primitive values — without breaking the published contract?

## Verdict: yes, on both axes. Nothing needs to change before the tag.

Verified against the shipped code, not asserted:

1. **`ISpace` flows everywhere a capability would be consumed.** Strategies and matchers
   receive `ISpace` (`ISizeStrategy.GetSize(ISpace)`, `IRowLandmark.FindRow(ISpace)`), and
   every view exposes `public ISpace Space` (`CellBlock`, `CellStrip`, `TableView`) — so
   both boundary logic and projections can type-test: `if (b.Space is IFormattedSpace f)`.
2. **Capabilities survive slicing.** `SpreadsheetSpace.GetSubspace` already re-wraps its
   slices as `SpreadsheetSpace` — the concrete type's identity propagates through
   decomposition today. This is the one pattern that could have been foreclosed and was
   not. (`GridSpace` returns plain slices, correctly — it has nothing extra to carry.)
3. **Adding an interface to a sealed public class is non-breaking**, and new interfaces in
   Core are additive. `sealed` blocks inheritance, not capability.
4. ~~**The safety net**: netstandard2.1 supports default interface members, so even `ISpace`
   itself could gain a default-implemented capability accessor later without breaking any
   implementor. Reach for this only if type-testing proves insufficient.~~
   **Withdrawn 2026-09-05.** The libraries now multi-target `netstandard2.0;netstandard2.1`
   for .NET Framework consumers, and .NET Framework's runtime cannot dispatch a default
   interface member — so a DIM on `ISpace` would compile and then fail at run time on half
   the supported surface. There is no safety net; adding a member to a published Core
   interface is a breaking change, full stop. (This is not hypothetical: the incremental
   strategy calculus *had* DIMs and gave them up in the same change — its folds moved to the
   static `Unrect.Core.Scans`, see `streaming-spec.md` §11.2.) Items 1–3 are untouched, and
   they were always the real argument: type-testing on `ISpace` is what makes a capability
   additive, and it needs nothing of the target framework.

## The recipe (for whoever builds the first capability)

- Define the capability as an interface in Core (so any space, including test fakes over
  `GridSpace`, can implement it): e.g. `ICellFormats { CellFormat? GetFormat(int column,
  int row); }`. Core defines; Core never *requires* — no engine code may depend on it.
- The concrete space implements it AND keeps implementing it on the subspaces it returns,
  translating coordinates as it goes (the subspace wrapper is constructed at slice time
  and can capture the accumulated offset — an internal change to the adapter only).
- Consumers reach it by type-test at the surfaces that already exist: `view.Space is T`
  in projections, the `ISpace` parameter in custom strategies/matchers. Vocabulary sugar
  (e.g. `RowWhereFormat(f => f.Bold)`) comes later, as data shows what reads well.
- The adapter must start *retaining* what the capability serves (today `SpreadsheetSpace`
  discards formatting at read time). Retention is an implementation change, not contract.

## Additional primitive types, specifically

The vendor survey (vendor-type-survey.md) settled that the six `CellKind`s absorb every
vendor primitive — durations lex to `Number`-in-days, entities to `Error(Value)`, etc. —
so "more primitives" never means new kinds. What a richer backend offers instead is a
**native payload beside the canonical value**, and two additive paths exist:

- **Space-level (preferred, zero Core churn):** a capability like
  `INativeValues { bool TryGetNative<T>(int column, int row, out T value); }` on the
  concrete space — the canonical `CellValue` stays the vocabulary; the native decimal128 /
  `TimeSpan` / entity object rides the side channel, position-keyed, exactly like formats.
- **Value-level (if ever needed):** `CellValue` could gain an opaque payload slot via a
  NEW factory OVERLOAD plus a `TryGet`-style accessor. Discipline learned from
  `OfError(error, literal)`: additive members and overloads are safe post-publish; adding
  optional parameters to existing methods is binary-breaking — never do that.

Precedent already inside the model: the dual double+exact-decimal storage on `Number` is a
native-fidelity side-channel avant la lettre, and the survey's finding that a first-party
OOXML reader would light `ExactNumber` up requires no contract change at all.

## What this note deliberately does not do

Design `CellFormat`, pick the first capability, or add vocabulary sugar. Those are
ergonomics-first design work for the session that builds them, against real scenarios
(the K-1's bold section captions are the standing candidate).
