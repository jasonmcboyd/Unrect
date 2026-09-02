# Spec: Diagnostics, Tolerance Boundaries, and Choice (wave 3, part 1)

**Status:** IMPLEMENTED (`d47e33a`). Driven by the production-import parity gap: real
imports warn-and-continue with cell locations; ours only fail fast. Extends
`wave2-shapes-spec.md` and `panel-and-anchoring-spec.md`; all their conventions apply.
(The leaf this spec calls `Cells` is `Range` as shipped — renamed after this spec was
written; see `CLAUDE.md`'s open design questions.)

## The governing principle

> **Tolerance is declared at the exact shape where it is acceptable; a diagnostic is the
> record of tolerance being exercised. There is no ambient lenient mode.** A tolerance
> boundary behaves like a catch block: descendants fail exactly as loudly as ever, and
> the failure propagates to the nearest enclosing boundary, which absorbs it, records a
> diagnostic carrying the INNER failure's full path and location, and supplies the
> declared hole-filler. "Global" tolerance is just a boundary placed at the root
> (`shape.Optional().MapWithDiagnostics(space)`) — no mode, no flag.

## 1. Diagnostic model (namespace Unrect.Shapes)

```csharp
public enum DiagnosticSeverity { Info, Warning }

public sealed class ShapeDiagnostic
{
  public DiagnosticSeverity Severity { get; }
  public string Message { get; }        // the problem text, ShapeException-style
  public string Subject { get; }        // quoted name or description of the shape involved
  public string Path { get; }           // full declaration path of the ORIGIN of the event
  public ShapeLocation Location { get; }
  public override string ToString();    // "{Severity}: {Subject}: {Message} — in {Path} at {Location}"
}
```

Collection: `ShapeContext` gains an internal per-`Map` `DiagnosticCollector` (a mutable
list created in `Root(...)`, shared by reference through `Descend`/`Advance`; contexts
stay otherwise immutable, parallel `Map` stays safe). The collector supports
checkpoint/rollback (`int Mark()`, `Rollback(int)`) — required by Choice (§3).
Only the framework emits diagnostics in this wave.

## 2. Result surface

```csharp
public readonly struct MapResult<T>
{
  public T Value { get; }
  public IReadOnlyList<ShapeDiagnostic> Diagnostics { get; }
}

// ShapeExtensions
public static MapResult<TResult> MapWithDiagnostics<TResult>(this IShape<TResult> shape, ISpace space);
```

`Map` is unchanged: absorbed-tolerance diagnostics are simply discarded (XML-doc this).
Unabsorbed failures throw from both entry points — declared tolerance is the only
softening, ever.

**Unconsumed-space diagnostic:** `MapWithDiagnostics` appends one `Info` diagnostic when
the root's advance is smaller than the space on either axis: e.g. `"the shape consumed
2650 of 2772 rows; rows 2651+ were not described"` (both axes reported when both fall
short; location = the first unconsumed cell). This is the "expected EOF" warning from
the observability roadmap, landing as data instead of a mode.

## 3. `Choice` — ordered alternatives (the tolerance primitive)

```csharp
public static IShape<T> Choice<T>(params IShape<T>[] alternatives);   // 2+ required
```

Semantics (internal `ChoiceShape<T>`):
- Alternatives are tried in declaration order against the choice's resolved extent. An
  alternative "fails" when anything in its subtree would raise `ShapeException`
  (placement or projection). On failure: record an `Info` diagnostic
  (`"alternative 1 ('vendor A layout') did not match: {problem}"` with the inner path
  and location), roll the collector back to the pre-attempt checkpoint (a failed
  branch's own absorbed diagnostics must not leak), and try the next.
- First success wins; its diagnostics (from any inner tolerance) survive, plus the
  "did not match" infos for the alternatives before it. Consumed = the winner's advance.
- All fail → `ShapeException` at the Choice's own path aggregating each alternative's
  subject + problem + location (one line each), so the error reads like a diff of
  near-misses. InnerException = the last failure. (Note, pinned: the inner exception's
  `Path` includes the Choice's own segment — `Choice -> 'second try' -> Cell` — because
  that is genuinely where it occurred; the aggregate's per-alternative lines use the
  shorter `alternative 2 ('second try')` form. Both are correct; they are different views.)
- Heterogeneous variants are unified by the caller via `Select` to a common result type.
- Placement: `Placement.Default` like other composites; `Description` = "Choice".
- Argument validation at the factory: null array/element, fewer than 2 alternatives.

## 4. `.Else` and `.Optional` — boundary sugar over Choice semantics

```csharp
public static IShape<T> Else<T>(this IShape<T> shape, IShape<T> fallback);  // Warning when fallback taken
public static IShape<T> Else<T>(this IShape<T> shape, T fallbackValue);     // Warning; consumes nothing
public static IShape<T?> Optional<T>(this IShape<T> shape);                 // Warning; yields default(T?)
```

- `Else(fallback)` = Choice semantics but the fallback-taken diagnostic is **Warning**
  (a fallback is tolerance; a variant is not). `Else(value)` and `Optional()` absorb the
  failure, emit the Warning (message = the inner failure's problem, path, location), and
  yield the constant / `default`.
- **Absorbed shapes consume nothing.** When a boundary absorbs, the honest extent is
  unknown, so `Consumed` and the offset are zero — a following sibling in a flow starts
  where the failed shape began. XML-doc this prominently: pair absorbing boundaries with
  seek-anchored siblings so downstream placement recovers by content, not arithmetic.
- `Optional` on a struct `T` yields `default(T)` (unconstrained `T?`); document, and
  point value-type users at `Else(value)` for an explicit filler.
- Severity rationale in docs: `Choice` = Info (alternation is expected), `Else`/`Optional`
  = Warning (tolerance was exercised).
- Filtering hazard (pinned, deliberate): a `Choice` that *chose* contributes Infos, but a
  `Choice` that failed entirely and was absorbed by a boundary surfaces its aggregate at
  the BOUNDARY's Warning severity. Severity belongs to the absorber, not the origin — a
  diagnostics UI filtering `Info` to find "alternation happened" misses total failures.
- Faults-are-not-tolerance list: `NullReferenceException`, `IndexOutOfRangeException`,
  `ArgumentOutOfRangeException`, `ArgumentNullException` (the last two cover wrong view
  indexes/names — code bugs, not absent sections). Plain `ArgumentException` stays
  absorbable: parse-style APIs throw it for data reasons.

## 5. Repeat recovery — a documented recipe, not a parameter

"One malformed section among 169" recovers by re-anchoring: the item is
`goodSection.Else(junkConsumer)` where the junk consumer is a shape that consumes up to
the next anchor and yields a marker (e.g. `Cells(area: rows-until-next-section-label,
b => (Section?)null)`), and the caller filters nulls. The Warning from `Else` carries
where and why the real shape failed.

**Implementation-corrected spelling (2026-09-01):** the seek anchor goes on the ITEM,
outside the boundary — `Repeat(good.Else(junk).After(seek))`-style, not
`Repeat(good.After(seek).Else(junk.After(seek)))`. With the seek inside the boundary,
anchor exhaustion is absorbed as a tolerance event and the fallback's own seek then
fails and propagates — the repeat never sees the stop signal and the recipe does not
terminate. With the seek on the item, an anchor miss remains the repeat's clean stop
condition while failures *within* an anchored section are what the boundary absorbs. Ship this as an XML-doc example on `Repeat` and a
test proving it end-to-end; do NOT add a `Repeat` recovery parameter in this wave —
if the campaign shows the composition is too clumsy, that's the evidence for one.

Anchor-fallback sugar (`seek else fixed position`, the legacy Find-with-fallback) is
likewise expressible as `shape.After(seek).Else(shape.After(fixed))`; dedicated
offset-level fallback is DEFERRED (a strategy-layer version couldn't emit the warning,
and silent fallback is guessing).

## 6. Location-bearing views (user warnings stay user-side)

Views gain absolute locations so post-parse validation can cite cells (framework
diagnostics stay structural; data-quality rules remain the caller's):

```csharp
// CellStrip
public ShapeLocation Location { get; }               // first cell
public ShapeLocation AddressOf(int index);
// CellBlock
public ShapeLocation Location { get; }
public ShapeLocation AddressOf(int column, int row);
// TableView
public ShapeLocation Location { get; }
// TableRow
public ShapeLocation Location { get; }               // first cell of the row
public ShapeLocation AddressOf(int column);
public ShapeLocation AddressOf(string columnName);   // same resolution rules as the indexer
```

Implementation: the shapes that build views already hold the `ShapeContext`; views store
their absolute origin (an `Offset`) and derive `ShapeLocation` on demand. Locations must
be correct through nesting, padding, overlay, and repeat (the `PadShape` context-advance
fix pattern applies — add locations to views where the context is already right).

A designed-but-deferred alternative for row streams (`RowOutcome.Keep/Skip`) is
documented in this spec for the record and NOT built.

## 7. Files

```
src/Unrect/Shapes/DiagnosticSeverity.cs
src/Unrect/Shapes/ShapeDiagnostic.cs
src/Unrect/Shapes/MapResult.cs
src/Unrect/Shapes/Composites/ChoiceShape.cs
src/Unrect/Shapes/ShapeContext.cs        (collector; internal surface only)
src/Unrect/Shapes/Shape.cs               (Choice factory)
src/Unrect/Shapes/ShapeExtensions.cs     (MapWithDiagnostics, Else x2, Optional)
src/Unrect/Shapes/Views/*.cs             (locations)
```

## 8. Test outline (house style, synthetic grids; example workbooks only where noted)

- Choice: first match wins (no diagnostics beyond none), later match wins (Info per
  skipped alternative, correct inner path/location in the Info), all-fail aggregate
  message, rollback (a failed branch's inner Optional warning does not leak), consumed =
  winner's advance, factory validation.
- Else/Optional: Warning content (inner problem + path + location), fallback shape /
  constant / default paths, absorbed-consumes-nothing pinned via a following sibling,
  boundary absorbs a DEEP failure (three levels down) with full inner path.
- MapResult: Map discards / MapWithDiagnostics surfaces the same parse; unconsumed-space
  Info on both axes; fully-consumed space emits none.
- Views: AddressOf correctness at root, under Padded, inside Overlay children, inside
  Repeat items (index-bearing paths not required, locations absolute); TableRow
  AddressOf(name) follows indexer resolution incl. errors.
- Recovery recipe: grid with sections A, junk, B → two good sections + one Warning; the
  Warning's location points into the junk.
- scrubbed-k1 (LOCAL-ONLY, harness/manual only — not committed tests): wrap a
  questionnaire block in Optional and confirm a clean parse with warnings.
