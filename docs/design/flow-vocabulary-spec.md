# Spec: The Flow Vocabulary (rename, removal, overlay cursor, use-site names, `Until`)

**Status:** IMPLEMENTED (2026-09-01, branch `experiment/combined-select`). All seven steps of
§2.3 are done; the suite is green at 707 tests and the vocabulary below is what ships.

Extends `combined-select-experiment.md` and **supersedes its §11a "Adoption decisions" in
detail** — §11a records what the owner decided; this document says exactly what to build.
It also extends `wave2-shapes-spec.md` (engine rules, error-message template, file layout,
test style), `panel-and-anchoring-spec.md` (Overlay, seeks), and
`diagnostics-and-choice-spec.md` (severity rationale). All of their conventions apply.

Everything here is settled. This is a consolidation pass, not a design pass: where a
detail had to be decided to make the spec mechanical, it is marked **[decided here]** so
the owner can see what was added.

## 0. What this pass does

| # | Change | Kind |
|---|---|---|
| 1 | "Stack" becomes "flow" everywhere: `VerticalFlow` / `HorizontalFlow` / `FlowShape<T>` | rename |
| 2 | The applicative spelling is deleted: `Vertical`/`Horizontal`/`Overlay` arities 2–8, `StackShape<T>`, the tuple `Select` combines | removal |
| 3 | `Overlay(o => …)` joins the cursor form, on the same `LayoutCursor` | addition |
| 4 | Children get names from the use site: explicit `.Named` > bare-identifier argument > description + ordinal | addition |
| 5 | `.Until(landmark)` — the dual of `.After`: bound a shape's extent by a forward content landmark | addition |
| 6 | Documentation: helpers stop naming; the WPF correspondence; flows never negotiate | docs |

After this pass there is exactly **one** spelling of a layout composite. Nothing in the
vocabulary is overloaded on "with a lambda or with arguments" any more, and every
composite is declared by a `Layout<T>`.

---

## 1. Rename: stack → flow

A stack is what the children make; a flow is what the composite *does*. The composite
family is renamed accordingly.

### 1.1 Surface

```csharp
// Shape.Layouts.cs  (replaces Shape.Stacks.cs and Shape.Overlays.cs)
public static IShape<T> VerticalFlow<T>(Layout<T> build);
public static IShape<T> HorizontalFlow<T>(Layout<T> build);
public static IShape<T> Overlay<T>(Layout<T> build);        // §3
```

`Layout<TResult>` and `LayoutCursor` keep their names: they were named for this shape of
the API in the first place, and a cursor that serves flows *and* overlays must not be
called a flow cursor.

### 1.2 Types and files

| Before | After |
|---|---|
| `Composites/CursorStackShape.cs` → `CursorStackShape<T>` | `Composites/FlowShape.cs` → `FlowShape<T>` |
| `Composites/FlowState.cs` → `FlowState` | `Composites/LayoutState.cs` → `LayoutState` (abstract) + `Composites/FlowState.cs` → `FlowState` + `Composites/OverlayState.cs` → `OverlayState` (§3) |
| `Composites/OverlayShape.cs` → applicative `OverlayShape<T>` | `Composites/OverlayShape.cs` → cursor `OverlayShape<T>` |
| `Composites/StackShape.cs` | deleted |
| `Shape.Stacks.cs`, `Shape.Overlays.cs` | `Shape.Layouts.cs` |
| `IOpaqueComposite` (inside CursorStackShape.cs) | `Composites/IOpaqueComposite.cs`, unchanged text |

### 1.3 `Description` — decision: the description is the factory name

`Description` becomes **`"VerticalFlow"`**, **`"HorizontalFlow"`**, **`"Overlay"`**.

The wave-2 reason for keeping `"Vertical"` was that diagnostics must not fork on
spelling — with two spellings alive, an identical description was the pin. That reason
retires with the applicative form.

What replaces it is an existing invariant, which every other shape already obeys:
**`Description` is the name of the factory the user typed.** `Cell` → `"Cell"`,
`Row(3)` → `"Row(3)"`, `Repeat` → `"Repeat"`, `Choice` → `"Choice"`, `Table` →
`"Table"`, `Padded` → `"Padded"`. Leaving a `VerticalFlow(…)` declaration to render as
`Vertical` would make it the only member of the vocabulary whose path segment cannot be
grepped back to the line that produced it. Six characters of path is a cheap price for
that, and the failure text stays readable:

```
'transactions': expected Text but found Number
  in VerticalFlow -> 'investor detail' (VerticalFlow) -> 'transactions' (TableRows)
  at row 14, column 2 (B14); 4x9 available
```

**Cost, listed so it is not a surprise:** every pinned path string containing `Vertical`
or `Horizontal` changes. That is a mechanical test respell, done in step 1 of §6.

---

## 2. Removal: what dies

### 2.1 The inventory (grepped, 2026-09-01)

**Production**

| File | What goes |
|---|---|
| `src/Unrect/Shapes/Shape.Stacks.cs` | the 14 tuple factories `Vertical<T1..T8>` / `Horizontal<T1..T8>`; the file is deleted, the two lambda factories move to `Shape.Layouts.cs` |
| `src/Unrect/Shapes/Shape.Overlays.cs` | the 7 tuple factories `Overlay<T1..T8>`; file deleted |
| `src/Unrect/Shapes/ShapeExtensions.Select.cs` | the 7 tuple `Select` combines; file deleted (the single-value `Select` in `ShapeExtensions.cs` survives) |
| `src/Unrect/Shapes/Composites/StackShape.cs` | `StackShape<T>`; file deleted |
| `src/Unrect/Shapes/Composites/OverlayShape.cs` | the applicative `OverlayShape<T>` body, replaced by the cursor form (§3) |
| `src/Unrect/Shapes/Shape.cs` | the private helpers `Stack<T>(…)`, `Overlay<T>(IShape[], Func<object?[],T>)`, and `NotNull(IShape child, string parameter)` — all reachable only from the tuple factories |
| `src/Unrect/Shapes/Composites/FlowState.cs` | `NextUntyped(IShape)` — `StackShape` was its only caller |

**Verified to have no other consumers.** `Repeat`, `Choice`, `Table`/`TableRows`,
`BoundaryShape`, `MapShape`, `PadShape` and every strategy are untouched: nothing in
`src/Unrect` calls a tuple `Select` (the only `.Select((…` occurrences in production are
LINQ over `IEnumerable` in `Shape.TableRows`), and `StackShape`/`OverlayShape` are
constructed only by the deleted factories.

**Tests and scripts** (all listed here are the *only* consumers):

- Tests using the applicative form: `StackShapeTests`, `OverlayShapeTests`,
  `CursorStackDifferentialTests`, `BoundaryShapeTests`, `ChoiceShapeTests`,
  `DiagnosticsTests`, `PadShapeTests`, `PlacementTests`, `RepeatShapeTests`,
  `ShapeErrorTests`, `ShapeExampleTests`, `ShapeInspectionTests`, `ViewLocationTests`,
  `CursorStackShapeTests` (its two "identical to the fixed-arity spelling" tests).
- Scripts: `linqpad/array.linq`, `simple-report.linq`, `investors-by-deal.linq`,
  `investor-summary.linq`, `scrubbed-k1.linq`. `investor-summary-cursor.linq` is folded
  into `investor-summary.linq` and deleted — with one spelling left there is nothing to
  put side by side.

### 2.2 Consequences that need a decision

**`ShapeEngine.ApplyUntyped`, `IShape.ProjectUntyped`, `ShapeBase.ProjectUntyped` become
unreferenced.** The untyped path existed for exactly one purpose: erasing child result
types behind an `object?[]` combine. With `StackShape` and the applicative overlay gone,
nothing in the library or the tests calls it, and the cursor form never will (it knows
`T` at every call site — that is the boxing hop wave 2 accepted and this pass reclaims).

**Recommendation [decided here]: delete all three, in step 6**, as `RegionMapper` and
friends were deleted in wave 2. `IShape` is a young, pre-1.0, in-repo interface; carrying
a member no implementation needs makes every future shape implement a lie. If the owner
would rather keep the seam for a future untyped tool, the whole pass still works with the
three members left in place and unreferenced — that is the only reversible piece here.

**`ShapeInspectionTests` loses two claims.** "A stack exposes its children in declaration
order" and the whole-tree walk cannot survive, because every layout composite is now
opaque (`combined-select-experiment.md` §6). They are replaced, not dropped: the walk
test keeps walking `Repeat` → `Select` → `Table` → `Until` and asserts that it meets
`VerticalFlow [opaque]` and stops there, reading `IOpaqueComposite.Reason`.

### 2.3 Removal order

Every step ends green: `dotnet build src/Unrect.sln -v q --no-incremental` clean and
`dotnet test src/Unrect.sln` passing. A step may touch production *and* the tests that
pin it when it is a **rename** (a test cannot be respelled to a factory that does not
exist yet); every **deletion** is preceded by the migration of what it pinned.

| Step | Work | Why here |
|---|---|---|
| **0** ✅ | *(tests only)* Make `CursorStackDifferentialTests` independent: replace `AssertIndistinguishable(applicative, cursor, space)` with fixed expected values for the cursor spelling; delete the description-identity and `TheFixedAritySpellingIsNotOpaque` tests. Rename the file `FlowCompositionTests.cs` — its job is now "a flow composes with every other shape, and here is exactly what that produces". | The suite compares two spellings; step 1 forks their descriptions. This must go first. |
| **1** ✅ | *(rename)* `CursorStackShape<T>` → `FlowShape<T>`; `Vertical(Layout<T>)`/`Horizontal(Layout<T>)` → `VerticalFlow`/`HorizontalFlow` in `Shape.Layouts.cs`; descriptions per §1.3. Respell `CursorStackShapeTests` → `FlowShapeTests`, `FlowCompositionTests`, and `investor-summary-cursor.linq`. | Rename before anything is written against the new names. |
| **2** ✅ | *(additive)* `LayoutState` / `FlowState` / `OverlayState`; internal `CursorOverlayShape<T>`; `Overlay<T>(Layout<T>)` factory (§3). The applicative overlay is untouched and still compiles. | The cursor overlay must exist before overlay tests can be respelled onto it. |
| **3** ✅ | *(additive + respell)* Use-site name inference (§4): the polyfill, `Next`'s `CallerArgumentExpression` parameter, the `ShapeContext` use-site plumbing. Respell the path assertions in `FlowShapeTests`/`FlowCompositionTests`/overlay tests; add `NameInferenceTests`. | Changes path segments; do it while only the flow tests own those strings. |
| **4** ✅ | *(additive)* `Until` (§5): landmark interfaces, factories, `UntilShape<T>`, `Until`/`UntilColumn`, `Shape` re-exports, `UntilShapeTests`. | Independent of 2–3; may be done in parallel. |
| **5** ✅ | *(tests + scripts only)* Respell every remaining applicative consumer into the cursor form. Migrate the uniquely-pinned behaviours out of `StackShapeTests` and the applicative half of `OverlayShapeTests` (§2.4), then delete `StackShapeTests.cs`. Rewrite `ShapeInspectionTests` per §2.2. Convert the five LINQPad scripts; delete `investor-summary-cursor.linq`. | Leaves **zero** references to the applicative surface. |
| **6** ✅ | *(deletion)* Delete everything in §2.1; rename `CursorOverlayShape<T>` → `OverlayShape<T>` into the freed file; delete `FlowState.NextUntyped`; delete the untyped path if the owner accepts §2.2. | Pure delete: verified by grep, then by the build. |
| **7** ✅ | *(docs)* `CLAUDE.md` (vocabulary, status, the open questions `Until` closes), status notes on this spec and the experiment spec. | |

### 2.4 Behaviours to migrate before deleting (QA checklist)

Uniquely pinned by `StackShapeTests`, and not by any cursor test — each must exist in
`FlowShapeTests` after step 5, respelled:

- a child's offset is consumed on the flow's own axis only (both orientations);
- a flow includes a child's offset in what it consumes (`IntCell().Down(1)` first child);
- a child **without** a declared area consumes only what its content used (the pair with
  the `.Sized` case that the cursor tests already have);
- a child that does not fit throws (`an extent of 1x2 does not fit here`), and a flow that
  runs out of space entirely throws;
- "beyond eight children" becomes **"a flow has no arity limit"**: one flow with 12 `Next`
  calls, replacing both the eight-children tests and the nesting test.

Uniquely pinned by `OverlayShapeTests` (keep the file, respell to `Overlay(o => …)`):
independence, overlap and shared cells, bounding-box consumed, widest-child-not-last,
following sibling starts after the bounding box, `.Sized` override, child misfit is a hard
error, offset run-off, absolute child locations at root and under a placed overlay,
overlays and flows nest, name/placement, derived area. The arity tests are replaced by a
single "no arity limit" test; the null-child construction guard moves to §3.4.

---

## 3. `Overlay(o => …)` — the cursor form of placement without flow

### 3.1 Surface

```csharp
public static IShape<T> Overlay<T>(Layout<T> build);
```

The **same** `LayoutCursor` and the **same** `Layout<T>`. A second public cursor type
would fork the grammar for no gain: the call reads `o.Next(shape)` either way, and what
differs is what the composite does between calls — which is the composite's business, not
the cursor's. The cursor stays what `combined-select-experiment.md` §1.1 made it: `Next`
and nothing else.

```csharp
var header = Overlay(o => new Header(
  Entity: o.Next(entityBlock),
  Funds:  o.Next(fundBand),
  Total:  o.Next(taxableIncome)));
```

### 3.2 Semantics — identical to the applicative overlay it replaces

Verified against the current `OverlayShape<T>.Project`:

- Each `Next` applies its shape to the overlay's **whole resolved extent**, through
  `ShapeEngine.Apply(shape, Extent, Context.WithUseSite(site))` — the overlay's own
  context, unadvanced, so the engine's `Descend` records each child's own offset and
  locations stay absolutely correct.
- **No cursor and no advance between children.** Children are independent, may overlap and
  may read the same cells; there is no z-order and no occlusion, because they read rather
  than paint.
- **Consumed = the union of the children's footprints**: per axis, the maximum over
  children of `applied.Advance` (child offset + child consumed) — the bounding box
  measured from the overlay's origin. This is exactly today's
  `Math.Max(width, applied.Advance.Width)` accumulation; the differential behaviour that
  `OverlayShapeTests` pins (`SizesItselfToTheWidestChildNotTheLast`) must not move.
- A child that does not fit is a hard error, as in a flow.
- `Placement.Default`; `Description => "Overlay"`; `IsTransparent => false`;
  `Children` empty and `IOpaqueComposite.Reason` present, like every cursor composite.

### 3.3 State: a sibling class, not a mode flag

`FlowState` splits into an abstract base plus two states. A mode flag would put
`if (overlay)` inside `Next`, the one method whose arithmetic this whole design exists to
keep un-forked; two subclasses put each arithmetic in one place and let the base own
everything that is genuinely shared.

```csharp
// Composites/LayoutState.cs
internal abstract class LayoutState
{
  protected LayoutState(IShape owner, ISpace extent, ShapeContext context);

  protected IShape Owner { get; }
  protected ISpace Extent { get; }
  protected ShapeContext Context { get; }

  public int Count { get; protected set; }
  public abstract Size Consumed { get; }

  /// The message for a layout that never called Next; each state supplies its own noun.
  public abstract string DeclaredNothing { get; }

  public void Close();                                   // sets _closed
  public abstract T Next<T>(IShape<T> shape, string? declared);

  /// Refuses a closed layout and a null child; `at` is where the child would have gone.
  protected void Admit(IShape? shape, Offset at);
  /// The ladder's lower two rungs (§4.2).
  protected static UseSite SiteOf(string? declared, int ordinal);

  internal const string NoLayout = …;         // unchanged text
  internal const string LayoutReturned = …;   // unchanged text
}
```

`FlowState : LayoutState` keeps `Orientation`, `_along`/`_across`/`_previous`, `Step`,
`Along`, `Across`, `RemainingAt`, `FollowsAnEmptySibling` and the sibling note, all
unchanged. `OverlayState : LayoutState` keeps `_width`/`_height`.

`LayoutCursor` holds a `LayoutState?` instead of a `FlowState?`; its public surface is
unchanged apart from §4's compiler-supplied parameter.

### 3.4 Which guards apply

| Guard | Flow | Overlay |
|---|---|---|
| `default(LayoutCursor).Next(x)` → `InvalidOperationException` "…never had a layout." | yes | yes |
| cursor used after the layout returned → `InvalidOperationException` "…after its layout returned." | yes | yes |
| zero `Next` calls → `ShapeException`, `isProjectionFault: true` | yes | yes |
| null child → `ShapeException` "a null shape was declared as child *n*", `isProjectionFault: true` | at the cursor position | at the overlay's origin |
| the sibling note ("the preceding sibling consumed nothing at this position") | yes | **no** |

The sibling note is flow-only by construction: it explains a child failing on the cells a
predecessor declined to consume, and an overlay has no predecessor-consumed relation at
all — every child starts from the same origin whatever its neighbours did.

**The zero-`Next` message names what it is [decided here]:** `"a flow must declare at
least one shape; this one called Next zero times"` and `"an overlay must declare at least
one shape; this one called Next zero times"`. Same rule, same severity, same
non-absorbability; the noun comes from `LayoutState.DeclaredNothing` so the two cannot
drift. (§11a asks for "the same error"; a message that calls an overlay a flow would be
the same error told wrong.)

Null-child ordinals are 1-based in both modes, matching the existing pinned text
("a null shape was declared as child 2").

---

## 4. Use-site name inference

### 4.1 The ladder

For each `Next` call, the path segment for that child is decided by the first rung that
applies:

1. **The shape's own name.** `v.Next(summary.Named("summary"))` → `'summary'`. An explicit
   name always wins; it is the only rung that survives being passed around.
2. **A bare-identifier argument expression.** If the `CallerArgumentExpression` text of the
   argument matches `^[A-Za-z_][A-Za-z0-9_]*$`, that text is the segment, rendered exactly
   as a name is: `v.Next(transactions)` → `'transactions'`.
3. **Description plus child ordinal.** Anything else — an inline factory call, a member
   access, a method call, a conditional — renders as `Cell#2`, `TableRows#3`: the shape's
   own description, `#`, and the child's 1-based position in the declaration.

**Rendered verbatim [decided here].** `investorName` renders `'investorName'`, not
"investor name". The point of the segment is to lead a reader back to the line that
produced it; humanising the identifier would break the grep and invent a name the user
never wrote.

**`#` and 1-based, deliberately [decided here].** `Repeat` already renders its iteration
index as `[0]`, `[1]` … — 0-based, because it is a coordinate into data, like
`TableRow.Index`. A child ordinal is a position in a declaration a human wrote ("the third
child"), which is the same kind of number as `ShapeLocation`'s 1-based row and column, and
as the existing "a null shape was declared as child 2". Different meanings, different
sigils, different bases: `Repeat[2] -> Cell#3` is unambiguous, and each half is consistent
with the rest of the library.

Ordinals count **every** child, named ones included, so naming one child never renumbers
the others.

### 4.2 What is inferred, and where it attaches

The inferred label belongs to the **use site**, not to the shape:

```csharp
var amount = Cell(c => c.GetDecimal());

var row = HorizontalFlow(h => new Line(
  Gross: h.Next(amount),          // 'Gross'?  No — 'amount'
  Net:   h.Next(amount)));        // 'amount' as well; distinguish with locals or .Named
```

- The same shape used in two flows gets two segments, and `shape.Name` stays `null`.
- **The member name is not read.** `Gross: h.Next(amount)` infers from the *argument*
  (`amount`), because that is all the compiler can hand us. §11a's "the member name
  already names the child" is the *motivation* — the fix is that a name the user already
  wrote is reused, and the one available to the compiler is the argument expression. In
  practice this means: hoist a shape into a well-named local, or name it inline with
  `.Named`. Both spellings read the same at the call site.
- The label passes through **transparent** wrappers (an unnamed `Select`, `Padded`,
  `Until`) to the first shape that actually contributes a segment — the same substitution
  `ShapeContext.Through` already makes for subjects.
- It applies to `Next` **only**. Capturing factory arguments (`Repeat(item)`,
  `Choice(a, b)`, `Vertical`-style children) is **deferred**; those factories keep their
  descriptions and, for `Repeat`, its index.

### 4.3 Mechanics

**`LayoutCursor`:**

```csharp
public T Next<T>(IShape<T> shape, [CallerArgumentExpression("shape")] string? declared = null)
{
  if (_state is null)
    throw new InvalidOperationException(LayoutState.NoLayout);

  return _state.Next(shape, declared);
}
```

The parameter is named `declared`, XML-documented as compiler-supplied, and is not a
naming API: pass `.Named(…)` when you want to choose a name. A caller *can* pass it
explicitly; anything that is not a bare identifier falls to rung 3 anyway, so the worst
case is a caller writing the name they could have written with `.Named`.

**The polyfill** (`src/Unrect/CallerArgumentExpressionAttribute.cs`, namespace
`System.Runtime.CompilerServices`, `internal sealed`): netstandard2.1 has no such
attribute. **Verified experimentally on this machine (SDK 8.0.419):** a netstandard2.1
library with an *internal* polyfill, consumed by a **net8.0** project that has the real
attribute in its framework, still gets inference at the call site and produces no
conflict — Roslyn matches the attribute on the parameter by full type name in metadata,
and an internal type in another assembly cannot collide with the framework's. `Unrect`
targets netstandard2.1 only, so no `#if` is needed; add
`#if !NET5_0_OR_GREATER` around it if the project ever multi-targets.

**The identifier test** is a hand-rolled ASCII scan (no `System.Text.RegularExpressions`
dependency, no allocation): first char `A–Z a–z _`, rest `A–Z a–z 0–9 _`, empty string
false. ASCII only, per the rule as stated; widening to `char.IsLetter` is a one-line
change if a declaration ever needs it.

**`ShapeContext`** gains the use-site plumbing (all internal):

```csharp
internal readonly struct UseSite
{
  public UseSite(string? name, int? ordinal);
  public string? Name { get; }        // rung 2
  public int? Ordinal { get; }        // rung 3
}

// on ShapeContext
private UseSite Site { get; }         // the site this context's own shape was used at
private UseSite Pending { get; }      // the site for the NEXT Descend

internal ShapeContext WithUseSite(UseSite site);   // same node, sets Pending
```

- `Descend(shape, offset, index)` → the new context takes `Site = Pending`,
  `Pending = default`.
- `Advance(offset)` and `WithIndex(index)` preserve **both** — `Advance` is the same node
  moved, and it is also what the engine uses for transparent shapes, which is how a label
  reaches the shape a wrapper wraps.
- Segment and subject rendering go through one overload:

```csharp
private static string Describe(IShape shape, UseSite site)
  => shape.Name is not null   ? $"'{shape.Name}'"
   : site.Name is not null    ? $"'{site.Name}'"
   : site.Ordinal is int n    ? $"{shape.Description}#{n}"
   :                            shape.Description;
```

- `Render` uses `Describe(context.Shape, context.Site)` per segment and
  `Describe(failing, Pending)` for the failing child appended at the end. The existing
  "a name hides what the shape is, so the last segment says so" rule (`… (Cell)`) fires
  when the last segment rendered as a **quoted name**, from either rung 1 or rung 2.
- `Failure(shape, …)` and `Report(severity, shape, …)` resolve the site the same way the
  renderer does: `ReferenceEquals(Shape, shape) ? Site : Pending`. This is the existing
  discrimination between "the context's own shape" and "a child being placed but not yet
  descended into", so offset failures (raised from the parent context), area failures and
  projection wraps (raised from the descended scope) all pick the right label.

**Consequence, stated because it is the point:** the inferred label acts as a name for
both the path **and** the subject, so `v.Next(total)` failing reads `'total': expected
Number …` exactly as `v.Next(x.Named("total"))` would. Anything less would make the two
rungs disagree about what the child is called in the same message. **[decided here]**

**Where the ladder runs:** `LayoutState.SiteOf(declared, ordinal)` returns
`new UseSite(IsIdentifier(declared) ? declared : null, ordinal)`; `FlowState.Next` and
`OverlayState.Next` pass it via `WithUseSite` on the context they hand the engine. Rung 1
needs no code: `Describe` prefers `shape.Name`.

---

## 5. `.Until(landmark)` — bounding by a forward landmark

### 5.1 What it is for

`.After` says where a shape starts by content. `.Until` says where it **ends** by content:

```csharp
var irrDetails = Repeat(investorBlock, separatedBy: BlankRows());   // declared once, placed twice

var report = VerticalFlow(v => new Report(
  Header:      v.Next(header),
  Summary:     v.Next(summary),
  ByTransferDate: v.Next(irrDetails
                    .After(Then(SeekRowContaining("Cash Flows Using Transfer Date"), SkipRows(1)))
                    .Until(RowContaining("Cash Flows using inception date"))),
  ByInception:    v.Next(irrDetails
                    .After(Then(SeekRowContaining("Cash Flows using inception date"), SkipRows(1))))));
```

This is `examples/investor-irr.xlsx`: two series of per-investor blocks, the first ending
where the second's caption begins. Without `Until`, the first `Repeat` runs into the
caption row and fails from inside its item — the open question "a repeat cannot stop
gracefully before trailing content" (`CLAUDE.md`). `Until` is that answer, and it is also
what the K-1 campaign's caption-to-caption sections need.

### 5.2 Semantics

- The extent ends **just before** the landmark row: the landmark itself is never inside
  what the shape may read.
- **Consumed = the bounded extent**, along the axis, whether the inner shape read all of
  it or not — exactly as a declared area is consumed in full. So the following sibling's
  space **starts at the landmark row**, and its own `.After(SeekRow…)` finds it at
  distance zero. That is the whole reason `Until` exists rather than a "stop before"
  option on `Repeat`.
- Across the axis, consumed is what the inner shape actually reached
  (`applied.Advance.Width` for a row landmark) — bounding rows must not claim columns.
- **Strict by default.** A missing landmark throws, like a missed seek, naming what it was
  looking for. A landmark is an anchor, and anchors are loud.
- **`orEnd: true`** opts one shape into "until X, or the end": when the landmark is
  absent, the extent runs to the end of the available space and an **`Info`** diagnostic
  records that it did. Info, not Warning, per `diagnostics-and-choice-spec.md` §4: this is
  declared alternation like `Choice`, not tolerance exercised after a failure. No ambient
  mode; the opt-in is per shape.
- A landmark on the very first row leaves a zero-row extent. That is not an error in
  itself: a `Repeat` there yields an empty list, a `Cell` fails because a 1×1 extent does
  not fit. Both are correct; document it, do not special-case it.

### 5.3 Landmarks

A landmark is a first-match content locator — the same idea as the seek strategies, minus
the offset lifting, plus the ability to say "not found" without throwing (so `orEnd` can
be implemented without exceptions-as-control-flow).

```csharp
// Unrect.Core, beside IRowStrategy / IColumnStrategy
public interface IRowLandmark
{
  /// What is being looked for, phrased as the seek descriptions are: "no row containing 'Total'".
  string Description { get; }

  /// The index of the first row that is the landmark, or null when there is none.
  int? FindRow(ISpace space);
}

public interface IColumnLandmark
{
  string Description { get; }
  int? FindColumn(ISpace space);
}
```

```csharp
// Unrect.Strategies, mirroring OffsetStrategies' seek trio exactly
public static class RowLandmarks
{
  public static IRowLandmark RowWhere(Func<ISpace, int, bool> predicate);   // "no matching row"
  public static IRowLandmark RowWithCell(Func<CellValue, bool> anyCell);    // "no row with a matching cell"
  public static IRowLandmark RowContaining(string text);                    // "no row containing 'X'"
}

public static class ColumnLandmarks
{
  public static IColumnLandmark ColumnWhere(Func<ISpace, int, bool> predicate);
  public static IColumnLandmark ColumnWithCell(Func<CellValue, bool> anyCell);
  public static IColumnLandmark ColumnContaining(string text);
}
```

`RowContaining` matches on the same rules as `SeekRowContaining`: whole cell value,
trimmed, `OrdinalIgnoreCase` — the predicate helpers (`AnyCellInRow`, `TextEquals`) are
lifted out of `OffsetStrategies` into an internal shared helper so the two vocabularies
cannot drift on what "containing" means.

Re-exported on `Shape` (the single-import rule): `RowWhere`, `RowWithCell`,
`RowContaining`, `ColumnWhere`, `ColumnWithCell`, `ColumnContaining`.

### 5.4 `Until` is a wrapper shape, not an area strategy

`.Until` reads like `.Sized` sugar — "rows while the landmark is not seen × full width" —
and under the hood it is exactly that arithmetic. It is nonetheless implemented as a
**wrapper shape**, `UntilShape<T>`, on the `PadShape` pattern, for one decisive reason:
**a strategy has no context and therefore cannot emit the `orEnd` `Info`.** The
alternatives were considered and rejected:

- a strategy that records the fallback in itself — shapes and strategies are immutable and
  applied to many spaces at once (wave-2 decisions 28/29); a strategy that remembers
  anything forfeits both;
- an `ILandmarkArea` special case inside `ShapeEngine.TryPlace` — a branch for one
  strategy kind in the one code path that must stay the one code path.

```csharp
// ShapeExtensions
public static IShape<T> Until<T>(this IShape<T> shape, IRowLandmark landmark, bool orEnd = false);
public static IShape<T> UntilColumn<T>(this IShape<T> shape, IColumnLandmark landmark, bool orEnd = false);
```

Distinct names rather than an overload pair, matching `AfterBlankRows` /
`AfterBlankColumns`: the row form is the common one and should not have to be
disambiguated by the reader.

```csharp
// Composites/UntilShape.cs
internal sealed class UntilShape<T> : ShapeBase<T>
{
  // Orientation decides row vs column; Landmark is the matching interface.
  public override string Description => Orientation == Orientation.Vertical ? "Until" : "UntilColumn";
  public override IReadOnlyList<IShape> Children { get; }   // { Inner }, stored in the constructor
  public override bool IsTransparent => Name is null;      // like Padded: no extra path segment

  public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
  {
    var size    = extent.Area.Size;
    var found   = FindLandmark(extent);                       // int? , 0-based within extent
    var limit   = found ?? Limit(size);                       // the whole extent when absent

    if (found is null && !OrEnd)
      throw context.Failure(ShapeContext.Through(this), $"{Landmark.Description} exists to end this shape", extent, null, null);

    if (found is null)
      context.Report(DiagnosticSeverity.Info, this, $"{Landmark.Description} exists to end this shape, so it ran to the end of the space", extent);

    var bounded = extent.GetSubspace(Bound(limit, size));      // (width, limit) rows, or (limit, height) columns
    var applied = ShapeEngine.Apply(Inner, bounded, context);

    return new ShapeResult<T>(applied.Value, Consumed(limit, applied.Advance));
  }
}
```

Notes on that body, each load-bearing:

- **The miss is reported against `ShapeContext.Through(this)`** — the shape being bounded
  when the wrapper is unnamed, the wrapper itself when it has been `.Named`. Blaming
  "Until" for a bound the user wrote as part of the inner shape's declaration would be
  technically true and useless. `Report(…, this, …)` already applies `Through` itself.
- **Message shapes follow the seek template.** `Description` is the negative noun phrase
  the seeks already use, so the strict failure renders
  `no row containing 'Cash Flows using inception date' exists to end this shape` beside
  the existing `no row containing 'Total' exists in the available space`.
- **The `Info`'s location** is the bounded shape's own origin, with the available extent —
  it answers "which section ran open-ended, and from where".
- **The miss is absorbable** (`isProjectionFault: false`): a landmark that is not there is
  a disagreement about the shape of the data, exactly what `Optional`/`Else` are for.
- **Inside a `Repeat`, a miss is loud, not a stop.** The wrapper's own placement is
  `Placement.Default` and always fits, so the failure comes from `Project` — deeper than
  the item's own placement, therefore drift. That is the right side of the loud-drift
  boundary: a missing *start* is exhaustion, a missing *end* is drift.

### 5.5 Composition

**With `.After`.** The landmark is searched in the extent the wrapper is handed, so the
two orders differ, and both are useful:

- `shape.After(seek).Until(L)` — the landmark is measured from where the flow's cursor is,
  and the seek anchors *inside* what the landmark left. This is the reading order ("start
  here, stop there") and the recommended spelling. A landmark that occurs before the
  anchor makes the anchor unreachable, and that fails loudly — correct: the section the
  user described is not there.
- `shape.Until(L).After(seek)` — `.After` applies to the wrapper, so the shift happens
  first and the landmark is measured from the shifted origin.

Document both; recommend the first.

**With `.Sized`, and with itself — one rule: the modifier written last is what the parent
sees.**

| Spelling | What the parent consumes |
|---|---|
| `x.Sized(a).Until(L)` | rows up to `L` — `Until` is outermost, **provided `a` fits inside the bound**; a declared extent taller than the landmark is a contradiction and fails as any misfit does (`an extent of 1x4 does not fit here`) |
| `x.Until(L).Sized(a)` | `a` — the wrapper has a declared area, consumed in full by the engine (wave-2 step 7), and the landmark search happens inside it |
| `x.Until(A).Until(B)` | `B` — **`Until` replaces a landmark already declared on the same shape** rather than nesting: `Until` on an `UntilShape<T>` clones it with the new landmark (`MemberwiseClone`, preserving `Name` and `Placement`), exactly as `Sized` replaces an area |

That single rule reproduces the "later replaces earlier" behaviour `.Sized` documents,
without `Sized` needing to know that `UntilShape` exists. Only the `Until`-over-`Until`
case needs the explicit replace, and it is three lines.

**With `Repeat`.** `Repeat(item, separatedBy: BlankRows()).Until(RowContaining("…"))` is
the intended spelling for "sections until the next caption". It does not change any of
`Repeat`'s rules — the repeat simply runs out of space at the bound — and it does not
change the "a blank band is a separator, never a terminator" semantics; it gives the user
the terminator that semantics deliberately withholds.

---

## 6. Documentation guidance (XML docs and this spec)

### 6.1 Helpers stop naming

A shape-returning helper must **not** call `.Named` on what it returns:

```csharp
// wrong: every use site is called 'full row', wherever it appears
static IShape<CellStrip> FullRow() => Row(s => s).Named("full row");

// right: the use site names it
static IShape<CellStrip> FullRow() => Row(s => s);
var captions = FullRow();
… v.Next(captions)          // 'captions'
```

A name baked into a helper wins rung 1 at every call site and defeats the whole ladder;
the helper cannot know which of a dozen rows it is producing, and the use site always
does. `.Named` remains exactly right for **inline** shapes (which have no identifier to
infer from) and for **overriding** an inferred name that reads badly. Put this on
`Named`'s XML doc, on the layout factories, and here.

### 6.2 The WPF correspondence

Unrect's layout vocabulary is WPF's panel vocabulary applied to reading rather than
drawing. The correspondence is exact enough to be worth stating:

| WPF | Unrect | |
|---|---|---|
| `StackPanel` (`Orientation="Vertical"`) | `VerticalFlow(v => …)` | children in declaration order, each consuming along the axis |
| `StackPanel` (`Orientation="Horizontal"`) | `HorizontalFlow(h => …)` | |
| `Grid` / `Canvas` | `Overlay(o => …)` | one extent shared by all children, each placing itself; overlap allowed, no z-order |
| `ItemsControl` | `Repeat(item, separatedBy:, atLeast:)` | |
| `Margin` | `.After` / `.Down` / `.Right` / `.AfterBlankRows` | shifts the outside |
| `Padding` | `.Padded` | shrinks the inside |
| `Width` / `Height` | `.Sized` | declares the extent |
| *(no analogue)* | `.Until(landmark)` | bounds by content instead of by measurement |
| `ContentPresenter` / `DataTemplate` | `.Select` | |
| *(no analogue)* | `Choice` / `.Else` / `.Optional` | alternation and declared tolerance |
| `Measure` / `Arrange` negotiation | *(none — deliberately)* | see below |

> **Flows never negotiate — a child that doesn't fit throws.** WPF panels measure their
> children and settle on a size everyone can live with; a flow does no such thing. A
> child that does not fit the space it is handed is the declaration disagreeing with the
> file, and that is the one thing this library will not paper over: it fails, loudly, with
> the path and the cell. Tolerance exists — `Optional`, `Else`, `Choice` — but it is
> declared at the shape where it is acceptable, never negotiated at layout time.

### 6.3 The cursor lambda's rules, unchanged and re-stated

Carried forward from `combined-select-experiment.md` §§4.2, 5, 7, now also on `Overlay`:
the lambda declares a *sequence* of shapes and nothing more; capture nothing you write to;
a losing `Choice` branch runs the lambda partially; parse inside the leaf, not around it;
conditionals, loops and position arithmetic in the lambda are the row-walking this library
exists to replace.

---

## 7. Files

```
src/Unrect/CallerArgumentExpressionAttribute.cs          new (internal polyfill)
src/Unrect/Shapes/LayoutCursor.cs                        Next gains the compiler-supplied parameter
src/Unrect/Shapes/ShapeContext.cs                        UseSite plumbing (§4.3)
src/Unrect/Shapes/Shape.Layouts.cs                       new — VerticalFlow / HorizontalFlow / Overlay
src/Unrect/Shapes/Shape.Stacks.cs                        deleted
src/Unrect/Shapes/Shape.Overlays.cs                      deleted
src/Unrect/Shapes/Shape.cs                               landmark re-exports; tuple helpers deleted
src/Unrect/Shapes/ShapeExtensions.cs                     Until / UntilColumn
src/Unrect/Shapes/ShapeExtensions.Select.cs              deleted
src/Unrect/Shapes/Composites/LayoutState.cs              new (abstract base)
src/Unrect/Shapes/Composites/FlowState.cs                rebased on LayoutState; NextUntyped deleted
src/Unrect/Shapes/Composites/OverlayState.cs             new
src/Unrect/Shapes/Composites/FlowShape.cs                renamed from CursorStackShape.cs
src/Unrect/Shapes/Composites/OverlayShape.cs             rewritten in cursor form
src/Unrect/Shapes/Composites/StackShape.cs               deleted
src/Unrect/Shapes/Composites/UntilShape.cs               new
src/Unrect/Shapes/Composites/IOpaqueComposite.cs         moved out of CursorStackShape.cs
src/Unrect.Core/IRowLandmark.cs                          new
src/Unrect.Core/IColumnLandmark.cs                       new
src/Unrect.Strategies/RowLandmarks.cs                    new
src/Unrect.Strategies/ColumnLandmarks.cs                 new
src/Unrect.Strategies/Row/…, Column/…                    landmark implementations; shared predicate helpers

src/Unrect.Tests/Shapes/FlowShapeTests.cs                renamed from CursorStackShapeTests.cs (+ §2.4)
src/Unrect.Tests/Shapes/FlowCompositionTests.cs          renamed from CursorStackDifferentialTests.cs
src/Unrect.Tests/Shapes/OverlayShapeTests.cs             respelled to the cursor form
src/Unrect.Tests/Shapes/NameInferenceTests.cs            new
src/Unrect.Tests/Shapes/UntilShapeTests.cs               new
src/Unrect.Tests/Shapes/StackShapeTests.cs               deleted (after §2.4 migration)
src/Unrect.Tests/Shapes/ShapeInspectionTests.cs          rewritten around opacity (§2.2)
src/Unrect.Tests/StrategyTests.cs                        landmark factories
linqpad/*.linq                                           converted; investor-summary-cursor.linq deleted
```

No new dependencies. netstandard2.1, nullable enabled, `LangVersion=Latest`.

---

## 8. Test outline (house style, synthetic grids)

### FlowShapeTests (renamed; absorbs §2.4)
Everything `CursorStackShapeTests` pins today, respelled to `VerticalFlow`/`HorizontalFlow`
and the new descriptions, plus the migrated stack behaviours: axis-only consumption both
orientations, a child's offset counted in what the flow consumed, derived vs declared child
extents, child misfit and space exhaustion both throw, **a flow has no arity limit** (one
lambda with 12 `Next` calls, replacing the eight-children and nesting tests).
The `Description` tests now assert `"VerticalFlow"` / `"HorizontalFlow"`; the
"identical to the fixed-arity spelling" and "the fixed-arity spelling is not opaque" tests
are deleted.

### OverlayShapeTests (respelled to `Overlay(o => …)`)
The whole existing list (§2.4), plus the cursor-specific ones:
- **no advance between children** — `o.Next(cell)` twice reads the same cell, while the
  flow spelling of the same two calls reads two rows (the distinguishing test, kept);
- **consumed is the union of footprints**, including the widest-child-not-last case, and a
  following sibling starting after the bounding box;
- **guards:** zero `Next` throws with the overlay wording and is not absorbed by
  `Optional`; a null child is reported at the overlay's **origin** with its 1-based
  ordinal and is not absorbed; **no sibling note** — an overlay child failing after a
  child that consumed nothing carries no note (contrast with the flow test of the same
  shape);
- overlays and flows nest; one overlay reused across many spaces concurrently;
- opacity: `Children` empty, `IOpaqueComposite.Reason` present, `Description == "Overlay"`.

### NameInferenceTests
- **rung 1:** `.Named` wins over a bare identifier and over an ordinal;
- **rung 2:** `v.Next(transactions)` → path segment `'transactions'`, subject
  `'transactions'`, and `transactions.Name` is still `null`;
- **rung 3:** `v.Next(Cell(c => c.GetInt()))` → `Cell#2`; `v.Next(shapes.Total)` (member
  access) → ordinal, not `'Total'`; `v.Next(Pick(x))` (call) → ordinal;
- **same shape, two flows, two names** — one instance assigned to two differently named
  locals yields two different path segments in the same `Map`;
- **ordinals number every child**, named ones included (child 2 named ⇒ child 3 is still
  `#3`);
- **both modes:** the ladder in a flow and in an overlay produce the same segments;
- **through transparents:** `v.Next(summary)` where `summary` is a `Select`/`Padded`/
  `Until` wrapper labels the shape that actually renders a segment;
- **the helper pattern:** a `FullRow()` helper that calls `.Named` internally reports
  `'full row'` at two different use sites; the same helper without `.Named` reports the two
  use-site identifiers — the test that makes §6.1 a rule rather than advice;
- `Repeat` and `Choice` are unaffected (deferred): `Repeat[0] -> Cell` still renders that
  way, and a `Repeat` inside a `Next` shows `'items'[0]`.

### UntilShapeTests
- **exclusive extent:** the bounded shape reads the rows before the landmark and not the
  landmark row;
- **the sibling starts AT the landmark:** the following `Next`'s `.After(SeekRow…)` finds
  it at distance zero, and the flow's consumed height equals the landmark index;
- **strict miss:** subject is the bounded shape (not "Until"), message
  `no row containing 'X' exists to end this shape`, path and A1 pinned; absorbed by
  `Optional` (it is not a projection fault);
- **`orEnd: true`:** value equals the run-to-the-end parse, exactly one `Info`, its
  message, path and A1 (the bounded shape's origin) pinned; nothing when the landmark
  *is* found;
- **compose with `.After`:** both orders, with the landmark measured from the right origin
  in each;
- **replace semantics:** `Until(A).Until(B)` uses B only; `Until(L).Sized(a)` consumes `a`;
  `Sized(a).Until(L)` consumes to `L`; `.Until(L).Named("x")` blames `'x'` on a miss;
- **column twin:** `UntilColumn(ColumnContaining("Total"))` in a `HorizontalFlow`;
- **inside a `Repeat` item:** a missing landmark is loud, not a stop (the item was found,
  its end was not);
- **zero rows before the landmark:** a `Repeat` yields empty and consumes nothing; a `Cell`
  fails;
- **`Repeat(...).Until(...)` stops before trailing content** — the case the open question
  in `CLAUDE.md` describes;
- factory guards: null landmark → `ArgumentNullException`;
- `StrategyTests` additions: each landmark factory found / not found / `RowContaining`
  trim-and-case rules, mirroring the seek tests.

### ShapeExampleTests
- **investor-irr, synthetic:** a grid mirroring `examples/investor-irr.xlsx` — header,
  summary table, `Cash Flows Using Transfer Date` + three per-investor blocks, then
  `Cash Flows using inception date` + three more — parsed by ONE shape: two placements of
  the same `Repeat`, the first `.Until(RowContaining("Cash Flows using inception date"))`,
  the second `.After(Then(SeekRowContaining(…), SkipRows(1)))`. Assert both series' block
  counts and row counts, and that the whole space is consumed (no unconsumed-space `Info`).
- The three committed workbooks keep their end-to-end tests, respelled.
- **`examples/investor-irr.xlsx` should be committed** (it is currently untracked): it is
  synthetic, not client data, so the K-1 fixture policy does not apply. Copy it to
  `src/Unrect.Tests/TestData/`, add the `Content`/`PreserveNewest` item, and add
  `linqpad/investor-irr.linq`. The synthetic-grid test above stands on its own either way.

### Regression
The full suite green after each step of §2.3; `ShapeInspectionTests` rewritten to walk a
tree that stops at an opaque composite and reads its `Reason`.

---

## 9. Decisions taken here, beyond §11a

1. **`Description` = the factory name** (`"VerticalFlow"`, `"HorizontalFlow"`), §1.3 — the
   only decision that changes existing diagnostics for reasons unrelated to this pass.
2. **The zero-`Next` message names the composite** ("a flow …" / "an overlay …"), §3.4.
3. **A sibling state class rather than a mode flag**, with a shared `LayoutState` base,
   §3.3.
4. **The inferred label is a name for the subject as well as the path**, §4.3 — so the two
   naming rungs cannot disagree inside one message.
5. **Identifiers render verbatim**, no humanising, §4.1.
6. **1-based ordinals with `#`**, justified against `Repeat`'s 0-based `[i]`, §4.1.
7. **`Until` is a wrapper shape**, and the strict miss is reported against the shape it
   bounds via `Through`, §5.4.
8. **"The modifier written last is what the parent sees"** as the single statement of
   `Sized`/`Until` interaction, with an explicit replace only for `Until`-over-`Until`,
   §5.5.
9. **Recommended deletion of the untyped path** (`ApplyUntyped`, `ProjectUntyped`), §2.2 —
   the one item in this spec the owner may want to veto; everything else works either way.

## 10. Deferred

- ~~`Cells` → `Range`~~ — **landed after review, with owner approval (2026-09-01).** All three
  factory overloads and their descriptions renamed; the `CellBlock` view keeps its name. This was
  §10's first deferred item and is the only part of that list now closed.
- ~~Name inference for `Repeat`/`Choice`/factory arguments~~ — see the addendum below.
- `Table` projection by header caption into properties (§11a records it as a separate
  work item).
- A blank-row/blank-column landmark (`Until(BlankRow())`) — no motivating file yet; the
  discovered-area defaults already cover the common case.
- A dry-run/`Describe` renderer over opaque nodes (unchanged from
  `combined-select-experiment.md` §11); the `IOpaqueComposite` marker is what it will need.

---

## 12. Addendum: name capture beyond `Next` (2026-09-01, owner-approved)

`Repeat<T>` and `RepeatHorizontal<T>` gained a
`[CallerArgumentExpression("item")] string? declared` parameter, so a repeat's item is labelled by
the local it was hoisted into and the last four `.Named` calls left the example scripts. The ladder
is unchanged and now lives on `UseSite.From(declared, ordinal)`, shared by `LayoutState` and
`RepeatShape` so the two cannot drift on what counts as an identifier.

Two details worth recording, because both were discovered rather than designed:

- **The index stays on the repeat's segment, and the label lands on the item's.** A repeat renders
  `Repeat[2] -> 'investorDetail'`, not `'investorDetail'[2]` — `[i]` has always decorated the
  repeat's own segment (`ShapeErrorTests.ARepeatDecoratesItsOwnSegmentWithTheItemIndex`), and a
  named item has always rendered as the segment after it. Capture changes which of the two rungs
  supplies that segment's text, nothing else.
- **A repeat's item has no ordinal.** It is *the* item rather than the nth child, so rung 3 has
  nothing to count and falls straight through to the description — an inline item still renders
  `Repeat[2] -> Row`, exactly as before.

**`Choice` cannot be extended, and this is a constraint rather than an omission.**
`CallerArgumentExpression` names a single parameter; `Choice(params IShape<T>[] alternatives)`
collapses every alternative into one, so the only text the compiler could supply is that of the
implicit array — nothing per-alternative. Recovering per-argument capture would mean replacing
`params` with fixed arities 2..8, which is precisely the arity explosion §2 removed. `Choice`
therefore keeps `.Named` as its naming mechanism. `Else(fallbackShape)` is capturable and was left
alone as out of scope.
