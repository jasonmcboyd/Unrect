# Spec: One Matcher Family, and the Caption Leaf

**Status:** IMPLEMENTED (2026-09-02, branch `experiment/combined-select`). All seven steps of §6 are done and the suite is green; every §7 measurement was reproduced exactly, including the flat 92-of-2772 burn-down and the 45→44 / 32→31 / 74→74 section figures. The new test suites (§8) are QA's.
This is phase B of the invertibility-audit remediation: audit items **4** (unify the matcher) and
**5** (`Caption` leaves), which together close finding **C4** and algebra irregularity **2**.

Extends `flow-vocabulary-spec.md` (the `Until` design, use-site naming, removal-order discipline,
message templates), `wave2-shapes-spec.md` (engine rules, file layout, test style),
`panel-and-anchoring-spec.md` (seeks and anchoring), and `diagnostics-and-choice-spec.md`
(severity rationale). All of their conventions apply.

Everything in §§1–3 is owner-settled. Where a detail had to be decided to make the spec
mechanical it is marked **[decided here]**; §9 lists every one of them in one place, and §10 lists
what is deliberately deferred.

Every claim about the scripts in §7 was **measured**, not predicted: the two spellings were run
side by side against the local `examples/scrubbed-k1.xlsx` and the committed
`examples/investor-irr.xlsx` using today's primitives to simulate `Caption`/`Under`
(`Row(AllColumns(), …).After(SeekRowContaining(text))` is exactly `Caption`'s placement and
extent). The numbers below are that run's output.

---

## 0. What this pass does

| # | Change | Kind |
|---|---|---|
| 1 | `IRowLandmark`/`IColumnLandmark` become **the** matcher vocabulary; the seek trios are deleted | removal |
| 2 | Two lifts, `To(matcher)` and `Past(matcher)`, each with a row and a column overload | addition |
| 3 | The `Where` name-crossing disappears with the seeks; one meaning per name is written down | naming |
| 4 | `Caption(text)` — a leaf shape that **declares** an anchor row instead of searching for one | addition |
| 5 | `.Under(params captions)` — sugar that desugars to the plain `(n+1)`-child vertical flow | addition |
| 6 | `scrubbed-k1.linq` and `investor-irr.linq` respelled; the smuggled caption rows are described | scripts |

After this pass there is exactly **one** way to say "a row that matches", exactly **one**
implementation of what matching means, and a caption row is a node in the tree rather than a
string mentioned in two vocabularies.

**Out of scope, explicitly:** multi-row column-header bands (phase C); `Caption` matching on
formatting rather than text (a capability seam, and nothing in Core may require a capability);
typed cell leaves (C1) and declared table columns (C2), which are audit items 6–7.

---

## 1. One matcher family, two lifts

### 1.1 The matcher is the landmark, and keeps its name

`IRowLandmark` / `IColumnLandmark` already have exactly the shape the audit asked of `IRowMatch`:
a `Description` phrased as a negative noun ("no row containing 'Total'") and a **nullable** `Find`.
They become the one matcher vocabulary unchanged — no new interface, no rename. **[decided here]**

> **Why not rename to `IRowMatcher`.** The word *landmark* is already load-bearing in the XML docs,
> in `RowLandmarks`/`ColumnLandmarks`, in `Landmark` (the internal axis-erasing wrapper), and in
> every `Until` message and test. It reads correctly under all three lifts — *to* the landmark,
> *past* the landmark, *until* the landmark. A rename would touch ~40 sites and buy a synonym.

**The nullable return stays the matcher's own contract; the lift decides the policy.** That is the
whole design: one locator, and a per-use answer to "what does absence mean".

| Lift | Type | Absence |
|---|---|---|
| `To(matcher)` | `IOffsetStrategy` | throws `AnchorNotFoundException` (an `OutOfBoundsException`) |
| `Past(matcher)` | `IOffsetStrategy` | throws `AnchorNotFoundException` |
| `.Until(matcher, orEnd:)` | wrapper shape | strict failure, or run to the end with an `Info` |

### 1.2 Surface

```csharp
// Unrect.Strategies/OffsetStrategies.cs — replacing the six Seek* factories
public static IOffsetStrategy To(IRowLandmark landmark);       // land ON the matched row
public static IOffsetStrategy To(IColumnLandmark landmark);    // land ON the matched column
public static IOffsetStrategy Past(IRowLandmark landmark);     // land on the row AFTER it
public static IOffsetStrategy Past(IColumnLandmark landmark);  // land on the column AFTER it
```

Re-exported on `Shape` (the single-import rule), beside the six landmark factories that are
already there:

```csharp
public static IOffsetStrategy To(IRowLandmark landmark);
public static IOffsetStrategy To(IColumnLandmark landmark);
public static IOffsetStrategy Past(IRowLandmark landmark);
public static IOffsetStrategy Past(IColumnLandmark landmark);
```

**Overloads, not `ToColumn`/`PastColumn`. [decided here]** `.Until`/`.UntilColumn` are spelled
apart because their argument does not have to be read to know the axis; here the argument *is* the
axis, and `ToColumn(ColumnContaining("EIN:"))` stutters. The rule this adds to the axis-naming
convention (§4.6 of the audit, still unwritten): **an operation is qualified by axis only when its
arguments do not already name one.** No overload takes a lambda or a method group, so nothing here
can be made ambiguous by inference.

### 1.3 Semantics

Both lifts are `IRowStrategy`/`IColumnStrategy` lifted through the existing
`RowOffsetSizeStrategy`/`ColumnOffsetSizeStrategy` — the same path `SeekRowStrategy` took, so
placement behaviour is bit-for-bit what the seeks did:

```csharp
// Unrect.Strategies/Row/LandmarkRowStrategy.cs   (Column twin alongside)
internal sealed class LandmarkRowStrategy : IRowStrategy
{
  public LandmarkRowStrategy(IRowLandmark landmark, bool past) { … }

  public int SelectRows(ISpace space)
    => Landmark.FindRow(space) is int row
      ? row + (Past ? 1 : 0)
      : throw new AnchorNotFoundException(Landmark.Description);
}
```

- **`To` lands the shape ON the match.** `SeekRowContaining("X")` had exactly this behaviour, and
  the reason is unchanged: anchoring on presence survives junk inserted above the thing sought.
- **`Past` lands it on the next row.** A matcher is row-granular, so "past" is `+1`; this is the
  whole of `Then(Seek…, SkipRows(1))`, which was a hard-coded count standing in for the caption
  row's own height and is exactly the fragility `CLAUDE.md` warns about.
- **A miss throws**, and the message is the engine's existing one, unchanged:
  `no row containing 'Cash Flows using inception date' exists in the available space`.
- **The `Repeat` stop is preserved by construction.** The failure is an `OutOfBoundsException`
  raised from an *offset strategy*, so `ShapeEngine.TryPlace(strict: false)` returns false and
  `RepeatShape` stops. Nothing about that path changes; `RepeatShapeTests`' anchor-exhaustion pins
  are respelled and must keep passing (§6, step 1).
- **`Past` on the last row of the extent** yields an offset equal to the available height. That is
  not a misfit (`Exceeds` is a strict `>`), so the shape is handed a **zero-row** subspace and
  fails with its own "does not fit" message. Identical to today's `Then(Seek…, SkipRows(1))`;
  documented rather than special-cased.

### 1.4 The `Where` cross-defect, and the naming law

The audit's defect was that `Where` meant *space-predicate* in one trio and *cell-predicate* in the
other. **Deleting the seeks removes one side of the crossing entirely**, so no rename is needed —
the surviving trio is already the good one:

| Predicate shape | Matcher factory | Reads as |
|---|---|---|
| `Func<ISpace, int, bool>` | `RowWhere` / `ColumnWhere` | "the first row where <this is true of the row>" |
| `Func<CellValue, bool>` | `RowWithCell` / `ColumnWithCell` | "the first row with a cell that …" |
| `string` | `RowContaining` / `ColumnContaining` | "the first row containing 'X'" |

**The law, to be written into `RowLandmarks`' XML doc and this spec [decided here]:**

> A bare `Where`/`While` takes a **space** predicate `(space, index)`. A cell predicate
> `(CellValue)` is always marked in the name — `WithCell`, `WhileAll`, `WhileAny`. Text is
> `Containing`, and it means whole-cell, trimmed, case-insensitive equality.

That law already holds across `RowStrategies`, `ColumnStrategies` and `OffsetStrategies`' skips
(`TakeRowsWhile(space-predicate)`, `TakeRowsWhileAll(cell-predicate)`,
`SkipRowsWhileAny(cell-predicate)`). With the seeks gone it holds everywhere without exception.

**One observation, not a change:** `RowStrategies.TakeRowsToValue(column, value)` matches by
`CellValue.Equals` — exact value equality, not the trimmed/case-insensitive text rule. That is a
different question (value identity vs. text matching) and it is not part of the matcher family;
recorded so a later pass does not mistake it for drift.

### 1.5 Matching rules: one implementation

`CellMatching.TextEquals` (trimmed, `OrdinalIgnoreCase`, whole-cell) stays the single definition of
what "containing" means, and is now used by the matcher factories **and** by `CaptionShape`
(§2.3). It stays `internal` to `Unrect.Strategies`; `Unrect` already sees it through the existing
`InternalsVisibleTo`, whose comment is updated from "nothing else is shared" to name the two things
that are (`AnchorNotFoundException.Description`, `CellMatching`). **[decided here]** Making it
public is deferred (§10).

### 1.6 The size lift: deferred, with its name reserved

The audit named a third lift, `RowsBefore(matcher)`, to replace `RowStrategies.TakeRowsTo(pred)`.
**Decision: not in this phase. [decided here]**

Reasons, in order:

1. **It would re-create the defect this phase removes.** `.Until(matcher, orEnd:)` already says
   "bounded by content", and it is the only spelling that can emit the `orEnd` `Info` — a strategy
   has no context (`flow-vocabulary-spec.md` §5.4). A size lift would be a second, quieter "until
   X" whose miss silently runs to the end. Two vocabularies again, for the same question.
2. **Nothing needs it.** No script in the corpus uses `TakeRowsTo`, and both bounds in the corpus
   are `.Until`.
3. **Regularity costs four factories, not one:** `RowsBefore`/`ColumnsBefore` (exclusive) *and*
   `RowsThrough`/`ColumnsThrough` (inclusive, which is what `TakeRowsTo` actually is), with no
   motivating file for any of them.

Reserved for whoever lands it: the names above, the implementation is the §1.3 strategy with
`?? space.Area.Size.Height` in place of the throw, and `TakeRowsTo`/`TakeColumnsTo` should be
deprecated in the same commit so the count of vocabularies does not go up.

### 1.7 Rename map

| Deleted | Replacement |
|---|---|
| `OffsetStrategies.SeekRow(pred)` | `OffsetStrategies.To(RowLandmarks.RowWhere(pred))` |
| `OffsetStrategies.SeekRowWhere(cell)` | `To(RowLandmarks.RowWithCell(cell))` |
| `OffsetStrategies.SeekRowContaining(text)` | `To(RowLandmarks.RowContaining(text))` |
| `OffsetStrategies.SeekColumn(pred)` | `To(ColumnLandmarks.ColumnWhere(pred))` |
| `OffsetStrategies.SeekColumnWhere(cell)` | `To(ColumnLandmarks.ColumnWithCell(cell))` |
| `OffsetStrategies.SeekColumnContaining(text)` | `To(ColumnLandmarks.ColumnContaining(text))` |
| `Shape.SeekRow` / `SeekRowWhere` / `SeekRowContaining` | `Shape.To(RowWhere/RowWithCell/RowContaining(…))` |
| `Shape.SeekColumn` / `SeekColumnWhere` / `SeekColumnContaining` | `Shape.To(ColumnWhere/ColumnWithCell/ColumnContaining(…))` |
| `Then(SeekRowContaining(t), SkipRows(1))` | `Past(RowContaining(t))` |
| `Then(SeekColumnContaining(t), SkipColumns(1))` | `Past(ColumnContaining(t))` |
| `Unrect.Strategies/Row/SeekRowStrategy.cs` | `Row/LandmarkRowStrategy.cs` |
| `Unrect.Strategies/Column/SeekColumnStrategy.cs` | `Column/LandmarkColumnStrategy.cs` |

Unchanged: `AnchorNotFoundException` (still internal, still an `OutOfBoundsException`), all six
landmark factories, `.Until`/`.UntilColumn`, `CellMatching`.

**Call sites to respell (grepped 2026-09-02, complete):** `linqpad/investor-irr.linq` (2),
`linqpad/scrubbed-k1.linq` (4 + one comment), `src/Unrect.Tests/StrategyTests.cs` (~35, the seek
block plus two elsewhere), `ShapeErrorTests` (8), `RepeatShapeTests` (4), `UntilShapeTests` (4),
`BoundaryShapeTests` (2), `DiagnosticsTests` (1), `FlowCompositionTests` (1),
`ShapeExampleTests` (2), plus XML-doc prose in `Shape.cs` (3), `ShapeExtensions.cs` (1),
`RowLandmarks.cs` (1), `ColumnLandmarks.cs` (1), `CellMatching.cs` (1), `OffsetStrategies.cs` (1).
No other script uses a seek: `array.linq`, `simple-report.linq`, `investors-by-deal.linq`,
`investor-summary.linq` and `edge-cases.linq` are untouched by this phase.

---

## 2. `Caption` — the anchor row as declared content

### 2.1 Why a leaf, and not an attribute

The audit's C4 is that a landmark row is *referenced* and never *declared*: nothing owns the row,
the same literal appears in two vocabularies, and `read ∘ write` is provably unsatisfiable because
a writer emits no caption. The obvious-looking fix is an attribute on the shape below
("this section has a caption") — and it is the wrong one.

> **Nodes get the algebra free; attributes need bespoke plumbing.** A caption that is a tree node
> is placed by the one engine path, bounded by `.Until`, tolerated by `.Optional`, labelled by the
> naming ladder, rendered into every path, counted in what a flow consumed, and — when a writer
> exists — emitted by the same walk that emits everything else. A caption that is a property on
> `SectionShape` needs each of those written again, and each one is a place to disagree.

So `Caption` is a leaf shape, and `.Under` is nothing but sugar for putting it in a flow (§3).

### 2.2 Surface

```csharp
// Shape.cs
/// <summary>The row that holds this text, as declared content: the shape finds it, asserts it,
/// consumes it, and yields what the cell actually says.</summary>
public static IShape<string> Caption(string text);
```

**`Caption(string)` only. [decided here]** A `Caption(IRowLandmark)` overload is rejected for this
phase: the leaf's projection is *the matched cell's text*, which is only well defined when the
match is a cell-level one. `RowWhere((s, r) => …)` matches a row and can name no cell, so the
overload would have a return value with no meaning for a third of the family. If a later phase
wants a matcher-valued caption, the seam is a matcher that reports the matching **cell**
(`int? FindCell`), not the row — recorded in §10.

Sharing a literal between a caption and a bound is therefore done with a `const string`, which is
what `ShapeExampleTests` already does (`const string Inception = …`) and what §7's scripts do. The
literal appears once; `Caption(Inception)` and `RowContaining(Inception)` cannot disagree, because
both go through `CellMatching.TextEquals`.

### 2.3 Semantics

| Aspect | Rule |
|---|---|
| **Placement** | offset `To(RowContaining(text))` — the same seek `SeekRowContaining` did, through the same lift |
| **Extent** | exactly one row, at the **full available width** (`TakeRows(1)` × `AllColumns()`) |
| **Projection** | asserts a matching cell is present in that row and yields **that cell's text, verbatim** |
| **Consumed** | 1 row × the full available width |
| **Description** | `Caption("K-1 Lines 1-21")` — the factory the user typed, with its argument, like `Row(3)` |
| **Transparency** | opaque (`IsTransparent => false`); no children |

```csharp
// src/Unrect/Shapes/Primitives/CaptionShape.cs
internal sealed class CaptionShape : ShapeBase<string>
{
  public CaptionShape(string text, Placement placement) : base(placement)
  {
    Text = text;
    Match = CellMatching.TextEquals(text);
  }

  private string Text { get; }
  private Func<CellValue, bool> Match { get; }

  public override string Description => $"Caption(\"{Text}\")";

  public override ShapeResult<string> Project(ISpace extent, ShapeContext context)
  {
    var size = extent.Area.Size;

    if (size.Height != 1)
      throw context.Failure($"a Caption must be exactly one row tall; this one is {size.Height} rows tall", extent);

    for (var column = 0; column < size.Width; column++)
      if (Match(extent[column, 0]))
        return new ShapeResult<string>(extent[column, 0].GetString(), size);

    throw context.Failure($"expected a row containing '{Text}' here", extent);
  }
}
```

```csharp
// Shape.cs
public static IShape<string> Caption(string text)
  => new CaptionShape(
       NotEmpty(text, nameof(text)),
       new Placement(OffsetStrategies.To(RowLandmarks.RowContaining(text)), FullRow()));

private static IAreaStrategy FullRow()
  => RowsThenColumns(RowStrategies.TakeRows(1), ColumnStrategies.AllColumns());
```

Notes, each load-bearing:

- **Full width, not the matched cell. [settled by the brief]** A caption row *is* a full-width row
  of the sheet; C5's `(s, c) => true` existed because `Row(project)` stops at the first blank
  column and a caption band has gaps. Consequence to know: in a flow, consumed *across* the axis is
  the widest child, so a caption widens what the flow claims to have consumed. That is honest — the
  row belongs to the section — but it can suppress a column-axis unconsumed-space `Info` that a
  narrower declaration would have raised.
- **The value is the file's text, not the declaration's. [decided here]** `Caption("ein:")`
  matching a cell that reads `"EIN:"` yields `"EIN:"`. The literal is the *matcher*; the cell is
  the *datum*, and a projection that handed back the caller's own argument would be the only leaf
  in the library that cannot tell you what the file says. Not trimmed either — trimming is the
  matcher's business, and `.Select(s => s.Trim())` is one call away.
- **The assert is reachable, and that is why it exists.** Normally the placement guarantees the
  match, so the loop always succeeds on the first hit. It fires when the placement was replaced
  (`Caption("X").After(SkipRows(2))`, a caption inside a `.Sized` frame) — and it is also the half
  of the leaf that makes `read ∘ write` satisfiable: the writer emits the row, the reader verifies
  it.
- **The empty caption is a declaration error [decided here]:** `ArgumentNullException` for null,
  `ArgumentException` for empty or whitespace-only text. A blank cell is `Blank`, never
  `Text("")`, so an empty caption could never match anything; failing at construction beats
  failing per-file.
- **Failures are ordinary.** A miss during placement produces the engine's existing anchor message;
  both failures are absorbable (`isProjectionFault: false`), so `Optional`/`Else` catch them.

### 2.4 What this fixes, concretely

```csharp
// before — the caption is a search key, consumed as data, and filtered downstream by luck
IShape<CellValue[]> FullRow(string anchor) => Row(AllColumns(), r => r.ToArray()).After(SeekRowContaining(anchor));
var k1Lines = section.After(SeekRowContaining("K-1 Lines 1-21"));
… .Where(r => r[head.AtaxColumn].HasValue)     // silently drops the caption row

// after — the caption is a node; the section holds only line items
var k1Lines = section.Under(Caption("K-1 Lines 1-21"));
```

---

## 3. `.Under` — sugar for a flow, and nothing else

### 3.1 Surface

```csharp
// ShapeExtensions.cs
public static IShape<T> Under<T>(this IShape<T> shape, params IShape<string>[] captions);
```

**The parameter type is `IShape<string>`, not a `Caption` marker type. [decided here]** No new
public type for one factory, and it keeps the useful case open: any string-valued row shape may sit
above a section and have its value discarded. The XML doc says so, and says `Caption` is what
belongs there.

### 3.2 The desugar, exactly

```csharp
shape.Under(a, b)   ≡   VerticalFlow(v => { v.Next(a); v.Next(b); return v.Next(shape); })
```

with three deviations from that literal spelling, all invisible in the resulting tree:

```csharp
public static IShape<T> Under<T>(this IShape<T> shape, params IShape<string>[] captions)
{
  if (shape is null) throw new ArgumentNullException(nameof(shape));
  if (captions is null) throw new ArgumentNullException(nameof(captions));
  if (captions.Length == 0)
    throw new ArgumentException("A shape must sit under at least one caption.", nameof(captions));
  for (var index = 0; index < captions.Length; index++)
    if (captions[index] is null)
      throw new ArgumentException($"Caption {index + 1} is null.", nameof(captions));

  // Copied because `params` may hand us the caller's own array, and the lambda below is captured
  // for every future application of this shape. A shape that could change is not a declaration.
  var declared = (IShape<string>[])captions.Clone();

  return new FlowShape<T>(Orientation.Vertical, cursor =>
  {
    foreach (var caption in declared)
      cursor.Next(caption, declared: null);

    return cursor.Next(shape, declared: null);
  }, Placement.Default, description: "Under");
}
```

1. **`declared: null` is passed explicitly at both call sites, and this is mandatory.** Without it
   the compiler supplies `CallerArgumentExpression` text from *inside this helper*: every caption
   would be labelled `'caption'` (the loop variable) and the section itself `'shape'` (the
   parameter) — rung 2 of the naming ladder, applied to identifiers the user never wrote. This is
   the same hazard the audit recorded in §4.8 for `Map`, in its other form: **capture reads the
   immediate call site, so a helper must opt out.** Passing `null` drops both to rung 3.
2. **`FlowShape<T>` gains an optional description.**
   `FlowShape(Orientation, Layout<T>, Placement, string? description = null)`, with
   `Description => _description ?? (Orientation == Vertical ? "VerticalFlow" : "HorizontalFlow")`.
   `.Under` passes `"Under"`. **[decided here]** — the standing invariant is that a description is
   the name of the factory the user typed (`flow-vocabulary-spec.md` §1.3), and the user typed
   `.Under(…)`; a path segment reading `VerticalFlow` could not be grepped back to the line that
   produced it. Existing flows are unaffected, and `FlowShapeTests`' description pins still pass.
3. **The array is copied**, as above.

Everything else is the flow: `Placement.Default`, opaque (`IOpaqueComposite.Reason`), children
declared by running the lambda, `LayoutState`'s guards, `FlowState`'s arithmetic. **Every caption
is a real child node** and renders as its own path segment.

### 3.3 Semantics that fall out (no new rules)

| Question | Answer, and where it comes from |
|---|---|
| Result | the inner shape's `T`; caption values are discarded (the lambda ignores them) |
| Order | captions in reading order, then the shape — the order of the `Next` calls |
| Consumed | flow rules: along the axis the sum of the children's advances (**including each caption's own seek offset**), across it the widest child |
| Where a caption looks | each caption seeks from where the previous child left off, so a stacked pair reads adjacent rows, and a gap between caption and content is absorbed by the seek |
| Where the content starts | immediately below the last caption; the content's own offset, if it declared one, still applies (`section.After(BlankRows()).Under(Caption(x))`) |
| Use-site label | lands on the **flow**: `v.Next(byTransferDate)` renders `'byTransferDate'`, and `Repeat(sections)` renders `Repeat[0] -> 'sections'` |
| The children's labels | rung 3: `Caption("IRR Details")#1`, `Caption("Cash Flows Using Transfer Date")#2`, and the section by its own description and ordinal (`Range#3`, `Repeat#3`). `.Named` on either is still rung 1. |
| `.Named("x")` on the result | names the flow (`'x' (Under)` when it is the last path segment) |
| `.After(o)` / `.Down(n)` on the result | placement of the flow; the first caption then seeks from there. `.After` replaces, the movements compose — the standard laws |
| `.Until(L)` on the result | wraps the flow; **the landmark is searched before any caption is found**, in the extent the wrapper is handed. This is the `investor-irr` composition and it is why the bound still ends the section where the *next* section's caption begins |
| `.Optional()` / `.Else(…)` on the result | absorbs a caption miss — a section whose caption is absent **is** an absent section. Verified: the boundary's own placement is `Default` and resolves first, the caption's anchor failure is raised inside `Project`, and it is not a projection fault, so it is caught |
| `.Under(a).Under(b)` | nests, like `.Padded`: `b` sits above `a` sits above the shape. Reading order is preserved and no merge is attempted |
| `MapWithDiagnostics` | unchanged; the caption rows now count as described because they are children rather than offset padding |

### 3.4 The one trap, and its recipe

**`Repeat(item.Under(Caption(x)))` does not stop gracefully — it fails loudly.** A repeat stops
only when the **item's own placement** fails (`ShapeEngine.TryApply` → `TryPlace(strict: false)`).
`.Under` puts the anchor *inside* the flow, and the flow's own placement is `Default` and always
fits, so a missing caption on the last iteration is a strict failure raised one level down. Pinned
by measurement:

```
Repeat(section.Under(Caption("Detail")), separatedBy: BlankRows())
  → Caption("Detail")#1: no row containing 'Detail' exists in the available space
      in Repeat[1] -> 'section' -> Caption("Detail")#1
      at row 5, column 1 (A5); 3x4 available
```

(Post-implementation correction: the hoisted item is captured by `Repeat`'s
`CallerArgumentExpression`, so the item segment is the label `'section'`, not the flow's
`Under` description — that segment appears only when the item is written inline. Both
forms are pinned by one test.)

**The recipe, which works today and is what the XML docs and this spec must show:** hoist the
matcher and put it on the item's placement as well. The seek is idempotent — the flow lands *on*
the caption row, and the caption inside then finds it at distance zero:

```csharp
var detail  = RowContaining("Detail");                       // the literal, once
var section = lines.Under(Caption("Detail")).After(To(detail));

Repeat(section, separatedBy: BlankRows())   // stops at the first row that is not a section
```

Measured on a grid of two captioned sections followed by a totals row: **2 items, captions owned
(no caption row inside the data), a graceful stop before the totals row, and the correct
unconsumed-space `Info`.**

**Why `.Under` does not lift the first caption's offset onto the flow automatically.
[decided here]** It would make the above the default and preserve the repeat stop — but it would
also mean `.After(o)` on the result silently discards the anchor, and `.Under(c).Down(2)` composes
into "two rows below the caption, then look for the caption again", which is a surprise with no
error. Sugar that quietly owns a placement is not sugar. The explicit recipe costs one modifier and
says what it does. Recorded in §10 in case a file argues otherwise.

### 3.5 The Section pattern is documentation, not API

There is no `Section` factory. The pattern is three vocabulary items already present, and belongs
in `Under`'s XML doc, in `Caption`'s, and here:

```csharp
// A captioned section that ends where the next caption begins.
var lines   = Range(RowsWhileAnyValue(), b => b.Rows.Select(r => r.ToArray()).ToArray());
var section = lines.Under(Caption("K-1 Lines 1-21"))
                   .Until(RowContaining("Portfolio Income"), orEnd: true);
```

- the caption is **declared content**, so it is described, consumed once, and emitted by any future
  writer;
- the bound is `.Until`, so the section stops at the next caption and the sibling that anchors on
  that caption finds it at distance zero;
- `orEnd: true` is the honest reading of "the last section runs to the end of the sheet", and it
  says so with an `Info` rather than by silence.

A section that may not be there is `.Optional()` on the whole thing; a run of like-captioned
sections is §3.4's recipe.

---

## 4. Path rendering — what failures look like

All four are the real formats, taken from runs of the simulated implementation.

**A missing caption, strict** (`investor-irr`, second series, if the caption were absent):

```
Caption("Cash Flows using inception date")#1: no row containing 'Cash Flows using inception date' exists in the available space
  in VerticalFlow -> 'byInception' -> Caption("Cash Flows using inception date")#1
  at row 31, column 1 (A31); 6x15 available
```

**A missing caption, absorbed** (`scrubbed-k1`, the `Optional` portfolio section):

```
Warning: Caption("Portfolio Income")#1: no row containing 'Portfolio Income' exists in the available space
  — in VerticalFlow -> 'portfolio' -> Caption("Portfolio Income")#1 at row 60, column 1 (A60)
```

Note `'portfolio'` reaches the flow through the two transparent wrappers `Optional` builds — the
existing `Through`/`Advance` behaviour, unchanged.

**The content assert** (placement replaced, so the row under the caption is not the caption):

```
'ein': expected a row containing 'EIN:' here
  in VerticalFlow -> 'ein' (Caption)
  at row 5, column 1 (A5); 2x1 available
```

**A failure inside the section, with the caption in the path** — the reason captions are nodes:

```
'k1Lines' -> Range#2
```

is what a reader now sees instead of a section that begins one row too early with no explanation.

---

## 5. Files

```
src/Unrect.Strategies/OffsetStrategies.cs              To / Past (4 overloads); the six Seek* deleted
src/Unrect.Strategies/Row/LandmarkRowStrategy.cs       new (replaces SeekRowStrategy.cs)
src/Unrect.Strategies/Column/LandmarkColumnStrategy.cs new (replaces SeekColumnStrategy.cs)
src/Unrect.Strategies/Row/SeekRowStrategy.cs           deleted
src/Unrect.Strategies/Column/SeekColumnStrategy.cs     deleted
src/Unrect.Strategies/Unrect.Strategies.csproj         InternalsVisibleTo comment names CellMatching
src/Unrect.Strategies/RowLandmarks.cs                  the naming law (§1.4) in the class doc
src/Unrect/Shapes/Shape.cs                             To/Past re-exports; Caption; Seek* re-exports deleted
src/Unrect/Shapes/Primitives/CaptionShape.cs           new
src/Unrect/Shapes/Composites/FlowShape.cs              optional description parameter
src/Unrect/Shapes/ShapeExtensions.cs                   Under

src/Unrect.Tests/StrategyTests.cs                      seek block → lift block (§8)
src/Unrect.Tests/Shapes/CaptionShapeTests.cs           new
src/Unrect.Tests/Shapes/UnderTests.cs                  new
src/Unrect.Tests/Shapes/ShapeReExportTests.cs          To/Past forwarding + single-import spelling
src/Unrect.Tests/Shapes/ShapeErrorTests.cs             respelled (message texts unchanged)
src/Unrect.Tests/Shapes/RepeatShapeTests.cs            respelled; + the Under-in-Repeat pair (§3.4)
src/Unrect.Tests/Shapes/UntilShapeTests.cs             respelled
src/Unrect.Tests/Shapes/BoundaryShapeTests.cs          respelled
src/Unrect.Tests/Shapes/DiagnosticsTests.cs            respelled
src/Unrect.Tests/Shapes/FlowCompositionTests.cs        respelled
src/Unrect.Tests/Shapes/ShapeExampleTests.cs           respelled; InvestorIrr uses Under; + the K-1 mirror
linqpad/investor-irr.linq                              Under + Caption; Past/To
linqpad/scrubbed-k1.linq                               Under + Caption; To
CLAUDE.md, docs/design/invertibility-audit.md          status notes (§6, step 6)
```

No new dependencies. netstandard2.1, nullable enabled, `LangVersion=Latest`. No public type is
added; the public surface grows by `Caption`, `Under`, and four `To`/`Past` overloads (×2 for the
`Shape` re-exports) and shrinks by twelve seek factories.

---

## 6. Removal order — every step green

`dotnet build src/Unrect.sln -v q --no-incremental` clean and `dotnet test src/Unrect.sln` passing
after each. A step may touch production *and* the tests that pin it only when it is a rename;
every deletion is preceded by the migration of what it pinned. This is the §2.3 pattern that
worked for the flow pass. The suite is at **707** tests at `3c70c7f`.

| Step | Work | Why here |
|---|---|---|
| **0** | *(additive)* `LandmarkRowStrategy` / `LandmarkColumnStrategy`; `OffsetStrategies.To`/`Past`; the four `Shape` re-exports. Seeks untouched and still compiling. Add the lift tests to `StrategyTests` and the forwarding tests to `ShapeReExportTests`. | Nothing can be respelled onto a factory that does not exist. |
| **1** | *(respell, tests + scripts)* Every `Seek*` call site in §1.7 → `To`/`Past`. Message assertions are unchanged, so this step is pure text. Leaves **zero** references to the seek surface outside its own definitions. | Deletions must be preceded by migration. |
| **2** | *(deletion)* The six `OffsetStrategies.Seek*`, the six `Shape` re-exports, `SeekRowStrategy`, `SeekColumnStrategy`, and the seek half of `StrategyTests` (its coverage now lives on the lifts). Update the prose in the six files listed in §1.7. | Pure delete; verified by grep, then by the build. |
| **3** | *(additive)* `CaptionShape` + `Shape.Caption` + `CaptionShapeTests`. | Needs `To` from step 0; independent of `Under`. |
| **4** | *(additive)* `FlowShape`'s description parameter + `ShapeExtensions.Under` + `UnderTests`. | Needs `Caption` for its tests to say anything. |
| **5** | *(scripts + example tests)* `investor-irr.linq` and `scrubbed-k1.linq` respelled onto `.Under(Caption(…))`; `ShapeExampleTests.InvestorIrr` likewise; the K-1-mirroring synthetic test added. Check the expectations in §7. | The evidence of success, and the last thing that can move. |
| **6** | *(docs)* `CLAUDE.md` (vocabulary, status, the C4 open question), a status note on `invertibility-audit.md` §§2/6 marking items 4–5 done, and the status line at the head of this spec. | |

Steps 3–4 are independent of 1–2 after step 0 and may be done in parallel.

---

## 7. Script expectations — measured, not predicted

### 7.1 `linqpad/investor-irr.linq`

```csharp
const string Inception = "Cash Flows using inception date";      // the literal, once

var irrDetails = Repeat(investorBlock, separatedBy: BlankRows());

var byTransferDate = irrDetails
  .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
  .Until(RowContaining(Inception));

var byInception = irrDetails.Under(Caption(Inception));
```

What changes, on the 6×45 sheet: rows 12 and 13 (`IRR Details`, `Cash Flows Using Transfer Date`)
and row 30 (`Cash Flows using inception date`) stop being anonymous padding inside a seek's offset
and become three `Caption` nodes. `Then(Seek…, SkipRows(1))` disappears from the script entirely —
so does the last `SkipRows`.

Expected, unchanged: three blocks per series of 3/2/4 transaction rows each;
`SeriesAgreeWithSummary` true; **`Assert.Empty(diagnostics)`** — the whole 6×45 sheet is still
consumed, because the flow's arithmetic is the same rows, differently attributed.

**Not used here:** `Past`. After the respell, the anchor-then-skip idiom `Past` replaces is gone
from this script; `Past` remains the right lift wherever a shape starts *after* a row it does not
want to own (and it is exercised by `UntilShapeTests` and `ShapeExampleTests`).

### 7.2 `linqpad/scrubbed-k1.linq`

```csharp
IShape<CellValue[]> FullRow(string anchor) =>
  Row(AllColumns(), r => r.ToArray()).After(To(RowContaining(anchor)));

var entity = Range(2, 5, …).After(Then(To(ColumnContaining("EIN:")), To(RowContaining("EIN:"))));

var k1Lines   = section.Under(Caption("K-1 Lines 1-21"));
var portfolio = section.Under(Caption("Portfolio Income")).Optional();
```

Measured against the local fixture (63×2772, both spellings run side by side):

| | today | after |
|---|---|---|
| diagnostics | `Info: the shape consumed 92 of 2772 rows; rows 93+ were not described` at **A93** | **identical** |
| `k1Lines` rows | 45 | **44** |
| `portfolio` rows | 32 | **31** |
| coded rows after the `HasValue` filter | 74 | **74** |
| funds / Σ pct / federal line items / `AllAllocationsSumToFederal` | 15 / 0.9999999997 / 8 / true | **identical** |

**The burn-down does not move, and the reason is the point of the finding. [reasoned]** The meter
counts what was **consumed**, and the caption rows were already consumed — smuggled into the
section's own extent by `After(Seek…)`, which lands *on* the caption row. Row 14 is the
`K-1 Lines 1-21` caption and rows 15–58 are its data; the section used to take 14–58 and now takes
15–58 with a `Caption` taking 14. Same rows, same 92; what changes is that they are now
**described** rather than merely covered. So:

> **Expected figure after the respell: `the shape consumed 92 of 2772 rows; rows 93+ were not
> described`, unchanged.**

A meter that would have moved is a *description* meter — the wave-3 decomposition trace — which is
precisely the observability item this finding argues for. Worth recording in `CLAUDE.md` beside the
burn-down note, so nobody reads the flat number as "phase B did nothing".

Two secondary effects worth stating because they are the audit's claim coming true:

- `.Where(r => r[head.AtaxColumn].HasValue)` used to drop three rows (the two captions plus one
  genuinely uncoded row); it now drops one. It stays in the script — it is a consumer-side filter,
  class (d) — but it has stopped doing **structural** work by luck.
- The `FullRow` helper still exists (it serves the header band, C3/phase C), but its C4 use — a
  helper whose reason to exist was re-reading a row a seek had just found — is gone from the two
  section shapes.

### 7.3 Every other script

`array.linq`, `simple-report.linq`, `investors-by-deal.linq`, `investor-summary.linq`,
`edge-cases.linq`: no `Seek*` usage (grepped), no caption idiom, untouched.

---

## 8. Test outline (house style, synthetic grids)

### StrategyTests — the lifts (replacing the seek block)
- `To` lands **on** the matched row/column, for all three matcher shapes on both axes;
- `Past` lands **one after**, and `Past(matcher)` equals today's `Then(To(matcher), SkipRows(1))`
  arithmetic on the same grid;
- a miss throws `OutOfBoundsException` for `To` and for `Past`, on both axes, for all three matcher
  shapes;
- `Past` on the **last** row yields an offset equal to the available height (a zero-row subspace),
  and the caller — not the lift — is what fails;
- matching rules are the landmark's: trimmed, case-insensitive, whole-cell, no substring (the
  parameterised cases move over from the seek tests verbatim);
- null-argument guards land on the **matcher factories** (`To(null!)` → `ArgumentNullException`
  with `landmark`);
- `Then(To(ColumnContaining("EIN:")), To(RowContaining("EIN:")))` composes across axes — the K-1
  entity anchor, kept as a strategy-level pin.

### ShapeErrorTests — respelled, texts unchanged
The six "no … exists in the available space" messages, now produced through `To`, plus one through
`Past` proving the lift does not change the message.

### RepeatShapeTests — the stop, preserved and extended
- the existing anchor-exhaustion pins respelled to `To(RowContaining(…))` (they must pass
  unchanged: this is what proves the lift kept `Repeat`'s stop);
- **new:** an item anchored with `Past(…)` also stops rather than throwing;
- **new (§3.4), the pair:** `Repeat(section.Under(Caption(x)))` **throws** on the iteration whose
  caption is absent, with the path `Repeat[1] -> 'section' -> Caption("…")#1`
  (`Under` in place of the label when the item is inline); and
  `Repeat(section.Under(Caption(x)).After(To(m)))` **stops** with the captions owned and the
  trailing content undescribed. Both are pinned because both are documented.

### CaptionShapeTests — new
- finds its row anywhere ahead of the cursor and consumes exactly **one row at the full available
  width**;
- yields **the cell's** text, not the declaration's (`Caption("ein:")` over a cell reading `"EIN:"`
  yields `"EIN:"`), and does not trim it;
- matching rules: trimmed / case-insensitive / whole-cell, and a substring does **not** match;
- a miss throws `no row containing 'X' exists in the available space`, and `Optional` absorbs it;
- the assert fires when the placement was replaced (`Caption("X").After(SkipRows(1))` over a grid
  whose second row is not X) → `expected a row containing 'X' here`;
- a caption forced to more than one row (`.Sized(WholeExtent())`) → `a Caption must be exactly one
  row tall; this one is N rows tall`;
- in a flow, the next sibling starts on the row **below** the caption;
- inspection: `Description == "Caption(\"X\")"`, no children, not transparent, `.Named` wins for the
  subject and the path;
- guards: null / empty / whitespace text → `ArgumentException` (`ArgumentNullException` for null).

### UnderTests — new
- **the desugared tree:** `Description == "Under"`, opaque with an `IOpaqueComposite.Reason`, and a
  failure below it renders `… -> 'section' -> Caption("X")#1` — every caption a real segment;
- value = the inner shape's; caption values discarded;
- consumed = flow rules, **including each caption's seek offset**, and width = the widest child;
- two captions in reading order: adjacent rows, and with a gap between them (the second seeks from
  where the first left off);
- **labels:** `v.Next(section)` names the flow `'section'`; the children fall to rung 3
  (`Caption("X")#1`, `Range#2`); `.Named("x")` on the result wins; **and the helper-leak pin** —
  no child is ever labelled `'caption'` or `'shape'`, which is what would happen if `.Under` forgot
  to pass `declared: null` (§3.2);
- **composition:** `.After` / `.Down` on the result behave as on any flow; `.Until(L)` bounds the
  whole thing with the landmark searched before the captions (the `investor-irr` composition on a
  synthetic grid); `.Optional()` absorbs a caption miss, yields `default`, consumes nothing, and
  records exactly one `Warning` naming the caption and its A1; `.Under(a).Under(b)` nests with
  `b` outermost;
- guards: null receiver, null array, empty array, a null element (1-based ordinal in the message);
- immutability: the array passed to `params` is copied — mutating the caller's array after
  construction does not change the shape.

### ShapeExampleTests
- `InvestorIrr()` respelled onto `.Under(Caption(…))`; the existing assertions plus
  `Assert.Empty(diagnostics)` and consumed `6x45`;
- **new, mirroring `scrubbed-k1`:** a synthetic grid of two captioned sections separated by a blank
  row, read by `section.Under(Caption(…))` twice in a flow, asserting (a) neither section's rows
  contain its caption row, (b) the flow consumed exactly the rows the seek spelling consumed — the
  regression pin for "the caption stopped being smuggled and the meter did not move".

### ShapeReExportTests
`To`/`Past` forward to `OffsetStrategies` on both axes (behavioural comparison, as the file's
other tests do), and one declaration written with **no** `Unrect.Strategies` import at all —
`Caption`, `Under`, `To`, `Past`, `RowContaining` — which is where the single-import claim is made.

### MethodGroupTests
Unchanged, and deliberately so. The phase-A caution was checked against every addition here:
`To`, `Past`, `Caption` and `Under` take **no** optional or compiler-supplied parameters, so no
method group can be broken by them. `.Under` could not take a `CallerArgumentExpression` even if it
wanted one (a `params` array must be the last parameter) — which is why §3.2 opts out explicitly
instead.

---

## 9. Decisions taken here, beyond the brief

1. **The matcher keeps the name `IRowLandmark`/`IColumnLandmark`** — no `IRowMatch` rename (§1.1).
2. **`To`/`Past` are overloaded on the axis rather than qualified** (`To(ColumnContaining(…))`, not
   `ToColumn`), and the axis-naming convention gains the rule that made it so (§1.2).
3. **No size lift in this phase.** `RowsBefore`/`RowsThrough` reserved, with reasons and the
   implementation recorded (§1.6).
4. **`CellMatching` stays internal**, shared with `Unrect` through the existing `InternalsVisibleTo`
   whose comment is widened to name it (§1.5).
5. **`Caption(string)` only** — no matcher overload, because the projection needs a cell-level
   match (§2.2).
6. **A caption yields the file's text, verbatim and untrimmed** (§2.3).
7. **An empty or whitespace caption is an `ArgumentException` at construction** (§2.3).
8. **`.Under` takes `IShape<string>`**, not a marker type (§3.1).
9. **`.Under`'s flow describes itself as `"Under"`**, via a new optional description on
   `FlowShape` (§3.2).
10. **`.Under` passes `declared: null` explicitly** so the helper's own identifiers cannot leak
    into the naming ladder — with a test that pins it (§3.2, §8).
11. **`.Under` copies the `params` array** (§3.2).
12. **`.Under` does not lift the first caption's offset onto the flow**; the repeat recipe is
    explicit instead, and both halves are pinned (§3.4).
13. **The Section pattern is documentation** — no factory (§3.5).
14. **The K-1 burn-down is expected to stay at 92 of 2772**, and the reason is written down beside
    the number so a flat meter is not read as a null result (§7.2).

---

## 10. Deferred, with names reserved

- **`RowsBefore` / `ColumnsBefore` / `RowsThrough` / `ColumnsThrough`** — the size lift (§1.6).
- **`Caption(IRowLandmark)`** — wants a matcher that can report the matching *cell*
  (`int? FindCell`), not the row (§2.2).
- **A public `CellMatching`** — useful the day a user writes `RowWithCell(TextEquals("X"))`; no
  request yet (§1.5).
- **A horizontal twin of `.Under`** (`.RightOf(params captions)` over a `HorizontalFlow`, with a
  column-seeking caption) — no motivating file; it is a mirror hole the day one appears.
- **`.Until`/`.UntilColumn` collapsing into overloads**, now that §1.2 establishes the rule that an
  operation is not qualified when its argument names the axis. Correct by the rule, and pure churn
  today; recorded so the inconsistency is a decision rather than an oversight.
- **Automatic anchoring of `.Under` for repeats** (§3.4).
- **Multi-row caption bands** (a section headed by a caption *and* a column-header row) — phase C,
  where `TableColumns` and the header-band work live.
- **Caption matching on formatting** (bold, fill, merged cells) — a capability seam; nothing in
  Core may require a capability, so this is a backend-extras question, not a vocabulary one.
