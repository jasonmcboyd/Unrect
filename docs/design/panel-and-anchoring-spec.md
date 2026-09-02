# Spec: Panel Vocabulary and Content Anchoring (wave 2.2)

**Status:** ready to implement (2026-09-01). Driven by the scrubbed-K-1 exercise: the
header area is two-dimensional (independent blocks sharing rows), and coordinate/blankness
anchoring breaks on real-world variance (fund counts, yearly line-item changes, humans
inserting rows and proof formulas). See CLAUDE.md Design Philosophy; this spec extends
`wave2-shapes-spec.md` and follows all of its conventions (engine rules, error template,
file layout, test style).

## 1. Seek strategies (Unrect.Strategies) — presence anchoring

Skip-whiles anchor on *absence* (stop at first non-match) and are defeated by inserted
junk. Seeks anchor on *presence*: scan to the first match.

Public factories:

```csharp
// OffsetStrategies (rows; exact column twins: SeekColumn / SeekColumnWhere / SeekColumnContaining)
public static IOffsetStrategy SeekRow(Func<ISpace, int, bool> predicate);      // full-row predicate
public static IOffsetStrategy SeekRowWhere(Func<CellValue, bool> anyCell);     // first row ANY cell matches
public static IOffsetStrategy SeekRowContaining(string text);                  // any Text cell, trimmed, OrdinalIgnoreCase
```

Semantics:
- Offset = (0, index of first matching row) — the region starts AT the match. "After the
  match" is spelled by composing: `Then(SeekRowContaining("X"), SkipRows(1))`.
- **No match → `OutOfBoundsException`** ("the anchor does not exist" is a placement
  failure). Consequences, both desirable: strict shape paths → `ShapeException` case A
  with the message naming the seek (see §4); inside `Repeat`'s non-strict item placement
  → the repeat *stops* — "repeat sections until no more section labels" falls out free.
- Implementation: internal `SeekRowStrategy`/`SeekColumnStrategy` (scan, throw on miss)
  lifted via the existing `RowOffsetSizeStrategy`/`ColumnOffsetSizeStrategy` pattern.
- `SeekRowContaining` matches cells whose `TryGetString()?.Trim()` equals the trimmed
  needle, OrdinalIgnoreCase. Equality, not substring — labels are whole cell values;
  substring matching invites false anchors. (A predicate overload exists for anything
  fancier.)

## 2. From-end anchoring (Unrect.Strategies)

```csharp
public static IOffsetStrategy FromRight(int width);    // offset = (available.Width - width, 0)
public static IOffsetStrategy FromBottom(int height);  // offset = (0, available.Height - height)
```

Throws `OutOfBoundsException` when the extent exceeds the available space. Typically used
with `.After(...)` (replace) since composing movements before a from-end anchor rarely
means anything; document that, don't forbid it.

## 3. `Shape.Overlay` — placement without flow

The third layout combinator. `Vertical`/`Horizontal` flow (children consume, cursor
advances); `Overlay` places: every child is applied against the SAME parent extent, each
with its own placement, no cursor. WPF analogy: stacks are StackPanel, Overlay is
Grid/Canvas.

```csharp
public static IShape<(T1, T2)> Overlay<T1, T2>(IShape<T1> first, IShape<T2> second);
// ... arities 2..8, tuple results; the existing tuple Select extensions combine them
```

Semantics (internal `OverlayShape<T>`, mirroring `StackShape<T>` — one class, object?[]
combine):
- Each child: `ShapeEngine.ApplyUntyped(child, extent, scope)` with `extent` the
  overlay's resolved extent every time. A child that does not fit is a hard error
  (consistent with stacks).
- Children are independent: they may overlap, may read the same cells (they read, they
  don't paint). No z-order, no occlusion, deliberately no opinion.
- Consumed (when `Placement.Area` is null) = bounding box: per axis,
  max over children of (child offset + child consumed). `.Sized(...)` overrides as usual
  (and is common: a header region's footprint often exceeds its sparse content).
- Context: each child descends from the overlay's scope with its own offset — error
  locations stay absolute-correct. Path segment: `Overlay`.
- Default placement: MinOffset + derived, like stacks.

## 4. `.Padded` — inner inset (WPF padding)

```csharp
public static IShape<T> Padded<T>(this IShape<T> shape, int all);
public static IShape<T> Padded<T>(this IShape<T> shape, int horizontal, int vertical);
public static IShape<T> Padded<T>(this IShape<T> shape, int left, int top, int right, int bottom);
```

Wrapper shape (`PadShape<TResult>`): resolves its own placement normally; the INNER shape
is applied to the extent inset by the four amounts; consumed = inner advance + (left+right,
top+bottom). Negative amounts → `ArgumentOutOfRangeException` at the boundary; an inset
larger than the extent → `ShapeException` case B naming the padding. Transparent in
paths when unnamed (like `Select`). Note: `Padded` is a *modifier-shaped wrapper*, not a
Placement mutation — padding shrinks the inside, margins shift the outside; the
distinction is the point.

## 5. `Shape` re-exports

Add to the `Shape` statics (single-import rule): `SeekRow`, `SeekRowWhere`,
`SeekRowContaining`, `SeekColumn`, `SeekColumnWhere`, `SeekColumnContaining`,
`FromRight`, `FromBottom`. All delegate to `OffsetStrategies`.

## 6. Error messages

Anchor misses must say what was sought. `To(RowContaining("Taxable Income"))` that finds
nothing, applied strictly, renders:

```
'taxable income': no row containing 'Taxable Income' exists in the available space
  in Overlay -> 'taxable income' (Cell)
  at row 1, column 1 (A1); 63x2772 available
```

(Spelled `SeekRowContaining` when this spec was written; the seek factories were replaced by the
`To`/`Past` lifts in `matcher-and-caption-spec.md`, and the fixture measures 63x2772.)

Mechanism: the anchoring strategies throw a new internal `AnchorNotFoundException :
OutOfBoundsException` carrying the description ("no row containing 'Taxable Income'");
`ShapeEngine`'s case-A handler uses that description as the problem text when present.
(Public surface unchanged; substrate callers still just see `OutOfBoundsException`.)

## 7. Acceptance

1. All existing 433 tests green, untouched (except none should need touching).
2. New tests (same house style, `src/Unrect.Tests/Shapes/` + `StrategyTests` additions):
   seeks (found / not-found / strict-vs-repeat-stop / Containing trim+case rules);
   FromRight/FromBottom (fit + overflow); Overlay (independence, overlap allowed, shared
   cells, bounding-box consumed, Sized override, child-miss hard error, context/paths,
   arities, Select); Padded (inset arithmetic, consumed, too-big inset, negative args,
   transparency).
3. `linqpad/scrubbed-k1.linq` rewritten as ONE root shape: a header `Overlay` (entity
   block, fund band with DISCOVERED width, taxable-income row — every piece
   content-anchored by seeks) inside a `Vertical` whose next child seeks the first
   section; near-zero hard-coded coordinates. Verified against the local fixture.

## Deferred

WPF Grid with declared tracks / proportional sizing (no motivating file yet); alignment
beyond from-end offsets; z-order/occlusion (meaningless for reading); `Choice` (wave 3,
unchanged).
