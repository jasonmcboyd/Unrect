# Spec: Combined Select — the cursor-lambda flow composite (experiment)

**Status:** ADOPTED and SUPERSEDED (2026-09-01, branch `experiment/combined-select`).
The experiment was judged a success: the cursor lambda is now the *only* spelling of a layout
composite, and the applicative form this document compares against has been deleted. What shipped
differs from what is described below in three ways — the composites are named `VerticalFlow` /
`HorizontalFlow` / `Overlay` and describe themselves that way, children take names from their use
sites, and `.Until` exists — all specified in `flow-vocabulary-spec.md`, which supersedes this
document's §11a and is the accurate account of the current surface. Read this one for the
*reasoning* (why a `ref struct`, why nothing but `Next` on the cursor, what opacity costs); read
that one for what the code does.

## 0. The question this experiment answers

Today a flow composite declares its children as arguments and combines their values
afterwards:

```csharp
Vertical(header, summary, details).Select((h, s, d) => new Report(h, s, d))
```

The experiment fuses the two halves into one lambda that receives a layout cursor:

```csharp
Vertical(v => new Report(
  Header:  v.Next(header),
  Summary: v.Next(summary),
  Details: v.Next(details)))
```

**Gain:** arity disappears (no 2..8 overloads, no tuples, no positional re-reading of
`(h, s, d)`), the result type is named at the point the parts are read, and members bind by
name rather than by position.

**Cost:** the child list exists only while the lambda runs. `Children` cannot be
enumerated without a space, so static inspection, the planned dry-run renderer, and any
structural tooling see an opaque node. The experiment exists to decide whether the
ergonomic gain is worth that opacity. Everything else in this spec is arranged so the
answer is the only variable.

Scope: `Vertical` and `Horizontal` lambda forms only. No changes to existing shapes,
no `Overlay` lambda form, no LINQPad conversions (later, once the style is judged).

## 1. Surface

```csharp
namespace Unrect.Shapes
{
  /// A flow declared as a sequence of Next calls.
  public delegate TResult Layout<TResult>(LayoutCursor cursor);

  public readonly ref struct LayoutCursor
  {
    internal LayoutCursor(FlowState state);
    public T Next<T>(IShape<T> shape);
  }
}

// Shape (Shape.Stacks.cs)
public static IShape<T> Vertical<T>(Layout<T> build);
public static IShape<T> Horizontal<T>(Layout<T> build);
```

`Next` is the whole cursor. It applies `shape` to the space remaining in the flow,
advances the flow, and returns the projected value.

### 1.1 Decision: nothing else on the cursor — no introspection, no movement

Rejected, deliberately: `v.Remaining`, `v.Extent`, `v.AtEnd`, `v.Row`, `v.Skip(rows)`,
`v.Peek(...)`, `v.Next(shape, name)`.

Rationale, straight from CLAUDE.md ("does it let the user *say what the data looks like*,
or does it make them *say how to walk it*?"): every one of those members exists only to be
branched on or added to. `if (v.Remaining.Height > 3)` is the row-oriented cursor logic
this project was built to escape, re-entering through the front door. Layout alternation is
already declarable — `Choice`, `Else`, `Optional`, seek offsets — and gaps are already the
following shape's offset (`.AfterBlankRows()`, `.Down(n)`, `.After(Then(...))`), so
`v.Skip` would be a second spelling for something that has one.

The fusion being sold here is *value flow*, not *position control*. The sequence of `Next`
calls is a declaration of order, exactly as argument order was; nothing about the cursor
may make position a value the user can compute with.

`Next` needs no naming overload: `v.Next(shape.Named("summary"))` already reads better and
keeps naming a property of the shape.

## 2. Factory naming, overload resolution, inference (verified by compilation)

`Vertical`/`Horizontal` are overloaded rather than given a new name (`Flow`, `VerticalBy`):
the reading is identical, and resolution is unambiguous because the lambda form takes
**one** argument while every existing overload takes 2..8. Verified against a prototype
mirroring the real signatures (netstandard2.1, `LangVersion=Latest`):

| Case | Result |
|---|---|
| `Vertical(a, b)` … `Vertical(a..h)` (existing, 2..8) | resolve unchanged |
| `Vertical(v => new { H = v.Next(h), T = v.Next(t) })` | infers, anonymous type |
| `Vertical(v => new Report(Header: v.Next(h), Rows: v.Next(r)))` | infers, named record args |
| `Vertical(v => new { Top = v.Next(Horizontal(h => …)), … })` | nested lambda forms infer |
| statement lambda with `var`/`return` | infers |
| `Repeat(Vertical(v => …))`, `.Named`, `.Select`, `.Optional` | compose unchanged |
| `Vertical(header)` (one shape, no lambda) | `CS0411` — no silent alternative binding |

No explicit `<T>` is ever required; the cursor parameter's type is fixed by the delegate,
so the body binds first and `T` falls out of the return expression. That is what makes
anonymous types work, and it is the reason the delegate parameter must not itself be
generic.

`Layout<TResult>` is a purpose-built delegate rather than `Func<ILayoutCursor, T>` because
the cursor is a `ref struct` (§5). It is declared non-variant; variance buys nothing here.

## 3. Execution model

Internal `CursorStackShape<T> : ShapeBase<T>` (Composites/), `Placement.Default`
(MinOffset + derived area) exactly like `StackShape<T>`, `IsTransparent => false`,
`Description => "Vertical" | "Horizontal"`.

`Project(extent, context)`:

1. `state = new FlowState(Orientation, extent, context)`.
2. `value = Build(new LayoutCursor(state))` — single pass, immediate; no deferral, no
   replay, no second pass.
3. `state.Close()`.
4. `state.Count == 0` → failure (§5.2).
5. return `new ShapeResult<T>(value, state.Extent)`.

`state.Next<T>(shape)` performs, per call, exactly what `StackShape.Project`'s loop body
performs per child:

```
cursor  = Step(along)                              // (0, along) vertical; (along, 0) horizontal
applied = ShapeEngine.Apply(shape, extent.GetSubspace(cursor), context.Advance(cursor))
previous = Along(applied.Advance); along += previous; across = Max(across, Across(applied.Advance))
return applied.Value
```

including the sibling note: a `ShapeException` whose location is the current cursor, raised
by a call after the first, whose predecessor consumed nothing, is rethrown
`.WithNote("the preceding sibling consumed nothing at this position")`. `index` in that
condition is the number of completed `Next` calls.

`Consumed` of the composite = `Extent(along, across)`: along the axis, the sum of child
advances; across it, the widest child — identical to `StackShape`. A declared area
(`.Sized`) is consumed in full by the engine (wave-2 step 7) as for any shape. A following
sibling of the *composite* therefore sees exactly what it would have seen with the
applicative spelling.

**Decision: extract the arithmetic, do not copy it.** `FlowState` (internal, Composites/)
holds `Orientation/extent/context/along/across/previous/count` and the `Step/Along/Across/
Extent` helpers, and `StackShape.Project` is rewritten as a loop over `state.Next` on the
untyped path. This is a mechanical extraction with no behaviour change, protected by the
existing `StackShapeTests`; it is worth touching `StackShape` for, because the alternative
is two copies of the "preceding sibling consumed nothing" rule drifting apart. If the owner
prefers `StackShape` untouched, the fallback is duplication plus the differential tests in
§9 — but then the duplication is the thing to watch in review.

Minor win, free: because `T` is known at each call site, the cursor form calls the **typed**
`ShapeEngine.Apply`, skipping `StackShape`'s `object?[]` boxing hop and its casts.

## 4. Composition guarantees

The composite is an `IShape<TResult>` with default placement, so `.Named`, `.After`,
`.AfterBlankRows`, `.Down`, `.Right`, `.Sized`, `.Padded`, `.Optional`, `.Else`, `.Select`,
`Repeat`, `Choice`, and `Overlay` all apply unchanged. Three cases need pinning.

### 4.1 Repeat stop condition — identical

`Repeat` stops when the item's **own** placement fails (`ShapeEngine.TryApply`). The cursor
composite's placement is `Placement.Default`, the same as `StackShape`'s, so its own
placement can never fail and termination comes — exactly as today — from space exhaustion,
the separator, or an error inside. Wave-2 §2.4's post-review note carries over verbatim:
`separatedBy:` remains load-bearing for termination, and trailing content still fails from
inside the item.

A failure raised by a `Next` call is *deeper than the composite's own placement*, so it is
an error, not a stop — the same loud-drift boundary as an applicative child misfit.

### 4.2 Choice rollback — diagnostics roll back, user code does not

`ChoiceShape` rolls the `DiagnosticCollector` back to its mark when a branch loses.
Side effects in user code are not and cannot be rolled back. That is already true of the
applicative style (leaf projections run inside losing branches), with one genuine
difference of degree, which must be documented:

> In the applicative style a losing branch never runs the *combine* — `Select` runs only
> after every child succeeded. In the cursor style the combine **is** the lambda, so a
> losing branch runs it **partially**: every expression up to and including the failing
> `Next`, and no further. A half-built result object is discarded; a counter incremented,
> a line logged, a list appended to, is not.

Position: acceptable, documented, pinned by a test. It is the same rule `Choice` already
states ("alternatives are tried for real, and must not have side effects worth undoing"),
tightened to name the combine. The mitigation is the same as everywhere else in this
layer: lambdas that only read and construct.

Related, and new: an applicative shape is trivially safe to reuse across threads
(wave-2 decision 29) because everything it holds is immutable. A cursor lambda that
captures mutable state (a counter, a `List` being appended to) breaks that guarantee for
its shape. Document on the factory: capture nothing you write to.

### 4.3 Fault classification — three sources, three treatments

| Origin | Treatment |
|---|---|
| A child shape fails inside `Next` | Its `ShapeException` propagates **untouched** through the lambda and out of `Project`; `ShapeEngine.Project`'s `catch (ShapeException) { throw; }` leaves it alone. **No re-wrapping, ever** — the failure belongs to the child, with the child's path and cell. The only permitted addition is the existing sibling note. |
| User code between `Next` calls throws | Not a `ShapeException`; caught by `ShapeEngine.Project` and wrapped once as "the projection threw {Type}: {message}" at the **composite's** path and origin, with `IsFault` classification unchanged (`NullReferenceException`, `IndexOutOfRangeException`, `ArgumentOutOfRangeException`, `ArgumentNullException` are non-absorbable faults; plain `ArgumentException` stays absorbable). No new code. |
| The cursor itself is misused | §5. |

Consequence worth an XML-doc line: `new Report(Total: decimal.Parse(v.Next(rawRow)))`
that throws blames the `Vertical` at its origin, not the cell, because the throw happened
in the outer lambda. Parse inside the leaf's own projection, where the location is exact —
that is where fusion's precision lives, and the same advice already applies to `Select`.

## 5. Misuse guards

### 5.1 The escaped cursor is a compile error, not a runtime one

`LayoutCursor` is a `readonly ref struct` holding one reference to the (class) `FlowState`.
That makes every escape route a compiler diagnostic, verified:

| Attempted misuse | Compiler |
|---|---|
| `Vertical(v => Enumerable.Range(0,3).Select(i => v.Next(x)).ToList())` | `CS9108` cannot use ref-like `v` inside a lambda |
| the same, left unmaterialised (the deferred-LINQ hazard) | `CS9108` |
| capture in a local function | `CS9108` |
| `Vertical(v => v)` (return the cursor as the result) | `CS9244` `T` may not be a ref struct |
| store in a static/instance field | `CS8345` |
| store in a `List<LayoutCursor>` | `CS9244` (the generic argument fails first) |
| store in a `LayoutCursor[]` | `CS0611` (the array declaration fails first) |

So the deferred-query hazard from the brief cannot be written, and a runtime "cursor
invalidated" error is not the primary guard. Cost of the ref struct, accepted: the cursor
cannot cross an `async`/iterator boundary or be a generic argument — all uses this layer
would refuse anyway.

Residual runtime guard, reachable and therefore required: `default(LayoutCursor)` is
constructible by anyone. `Next` on a cursor with a null state throws
`InvalidOperationException("A layout cursor cannot be used outside the layout that created
it.")`. `FlowState` additionally carries a `_closed` flag set after `Build` returns and
asserts on it — one bool, and the thing that keeps the design honest if the cursor is ever
demoted to a class.

Deliberately *not* a `ShapeException`: a cursor used outside its layout is a bug in the
declaration, not a shape of data, and must never be absorbable by `Optional`/`Else`.

### 5.2 Zero `Next` calls — a failure, not a silent no-op

A lambda that never calls `Next` produces a composite consuming 0x0: it would parse
anything, describe nothing, and silently terminate an enclosing `Repeat` via the
zero-consumed guard. The applicative overloads start at arity 2 for the same reason.

Decision: `Project` throws a `ShapeException` (case C) — `"a flow must declare at least one
shape; this one called Next zero times"` — raised through `context.Failure(..., isProjectionFault: true)`
so no tolerance boundary can hide a declaration bug. A single `Next` is legal (a one-child
flow is a `Select` with placement, which is useful).

This is the one reversible decision here: if a real file wants a conditionally empty flow,
the check is one line to remove and one test to update.

### 5.3 `Next(null)`

Throws a `ShapeException` built by `context.Failure(..., isProjectionFault: true)` with
"a null shape was declared as child {n}" — raised from the context **advanced to the
cursor position** against the **remaining subspace** (implementation-corrected 2026-09-01:
an earlier sketch passed the unadvanced context and whole extent, which would blame the
composite's origin; the prose's "precise position, correct A1 location" wins). Precise
position, full path, non-absorbable. (Letting the natural `ArgumentNullException`
escape would produce a correct but vaguer "the projection threw ArgumentNullException".)
Note that `v.Next(null)` written literally does not compile (`CS0411`); this guard is for a
null-valued `IShape<T>` variable.

## 6. What is lost, precisely

**Lost:** `Children` enumeration, and with it anything structural that runs without a
space — a dry-run/`Describe` renderer, a shape-tree dump, static "does this declaration
mention column X" queries, and structural equality/diffing of declarations. Wave-2 decision
"`ShapeInspectionTests` — Children/Name/Description enumerable without a space" no longer
holds for this composite.

**Not lost:** everything diagnostic. Verified against `StackShape` as it stands today:
a stack does **not** add per-child path segments — the child's segment comes from
`ShapeEngine.Place` calling `context.Descend(child, offset)`, and the stack only calls
`context.Advance(cursor)` to move the origin (positional indices appear in paths solely via
`Repeat`'s `WithIndex`). The cursor composite does the identical thing, so paths, A1
locations, sibling notes, `Choice` infos, boundary warnings, and the unconsumed-space Info
are **byte-identical** between the two spellings. Errors read the same; only tooling can
tell the difference. This is the pin that makes the experiment cheap to judge.

One exception, found by the differential suite and pinned (`NamingTheCombineIsNotNamingTheFlow`):
the claim holds **per-shape**, but the applicative spelling has one more nameable node.
`Vertical(a, b).Select(f).Named("report")` names the *Select*, adding a path segment
(`'report' -> Vertical -> Cell`) that the cursor spelling — whose combine is the lambda, not
a node — cannot produce (`'report' -> Cell`). `Vertical(a, b).Named("report").Select(f)`
agrees with the cursor form exactly. The shipped LINQPad scripts use the
`.Select(...).Named(...)` order, so converting them changes their diagnostic paths by one
segment — not wrongly, but visibly.

**Decision on the inspection surface:**
- `Description` = `"Vertical"` / `"Horizontal"` — deliberately identical to the applicative
  form, because diagnostics must not fork on spelling.
- `Children` = empty.
- Empty children would tell a future renderer "leaf", which is a lie. So the composite also
  implements an internal marker, `internal interface IOpaqueComposite { string Reason { get; } }`
  (`"declared by a cursor lambda; children are known only while it runs"`), letting wave-3
  tooling in the same assembly render `Vertical [opaque]` instead of a leaf — without
  adding a member to the public `IShape`.
- **Rejected:** caching the children observed during the first successful `Project`
  ("learned structure"). Shapes are immutable and safe to apply to many spaces
  concurrently (wave-2 decisions 28/29); a shape that mutates itself while parsing
  forfeits both, and the learned list would be a lie for any value-dependent lambda.

## 7. Value-dependence: possible, not blessed

The cursor form makes `v.Next(shapeChosenFrom(previousValue))` expressible for the first
time — the style is monadic, not applicative, and that is a genuine semantic expansion,
not an implementation detail. Wave-2 §7 listed "monadic sugar (only if a value-dependent
format appears)"; the owner has confirmed geometry has always sufficed in real reports.

Position: **allow it because the shape of the API cannot prevent it, discourage it in
documentation, and add nothing that encourages it.** Specifically:

- No `Next` overload taking a `Func<…, IShape<T>>`; no cursor member exists to support
  branching.
- XML docs state the rule: the lambda declares a *sequence* of shapes; use `Choice`,
  `Else`, `Optional`, and seek offsets to express alternation, and keep conditionals,
  loops, and arithmetic over positions out of it.
- Smell list to review against: `if`/`switch` around a `Next`; a `for`/`foreach` containing
  `Next` (that is `Repeat`); any use of a value read earlier to pick a later shape; any
  local tracking a count of rows.
- The hard consequence, stated where users will read it: a value-dependent lambda makes a
  dry-run renderer impossible **in principle** for that shape, not merely unimplemented.
  Opacity to tooling is the price of the ergonomics (§6); value-dependence is the price of
  never getting the tooling back.

A plain `for` loop calling `Next` compiles (a ref struct may be used in a loop body, just
not captured). It is not forbidden by the compiler and must be discouraged by review:
`Repeat` is the declaration of "again".

## 8. Files

```
src/Unrect/Shapes/LayoutCursor.cs                    (public readonly ref struct + Layout<T> delegate)
src/Unrect/Shapes/Composites/FlowState.cs            (internal; the shared flow arithmetic)
src/Unrect/Shapes/Composites/CursorStackShape.cs     (internal sealed)
src/Unrect/Shapes/Composites/StackShape.cs           (rewritten over FlowState; no behaviour change)
src/Unrect/Shapes/Shape.Stacks.cs                    (+ the two lambda factories, at the top)
src/Unrect.Tests/Shapes/CursorStackShapeTests.cs     (§9, behavioural)
src/Unrect.Tests/Shapes/CursorStackDifferentialTests.cs (§9, the differential suite — split out because its job is to fail if FlowState is ever re-forked)
```

No new dependencies; netstandard2.1, nullable enabled. `IShape`, `ShapeEngine`,
`ShapeContext`, `ShapeException`, and every existing shape are untouched apart from the
`StackShape` extraction.

## 9. Test outline (house style, synthetic grids)

**Differential (the load-bearing suite).** For each of ~8 shapes covering leaves, nested
stacks, `Table`, `Repeat`, `Overlay`, `Padded`, and a seek-anchored child: build the same
declaration both ways and assert equal values, equal `AppliedResult.Offset`/`Consumed`/
`Advance`, and — for the failing variants — equal `ShapeException.Path`, `.Location`,
`.Problem`, and `.IsProjectionFault`. Identity of diagnostics is the claim of §6; test it,
do not assert it in prose.

- **Flow arithmetic:** declaration order; advance along the axis only; cross-axis = widest
  child; horizontal twin; a child with a declared area consumed in full; `.Sized` on the
  composite overrides derived consumption; sibling note fires when a predecessor consumed
  nothing at the same cell, and does not fire when the next child re-anchored elsewhere.
- **Repeat:** cursor item behaves as the applicative item — separator-driven termination,
  trailing blank band, `atLeast`, error inside the item is loud (not a stop), zero-advance
  guard.
- **Choice:** a losing cursor branch leaves no diagnostics (rollback) but *does* run its
  lambda partially — pinned with a counter incremented between `Next` calls; a winning
  later branch's Infos name the earlier ones.
- **Boundaries:** `Optional`/`Else` around a cursor composite absorb a deep failure with
  the inner path; a `NullReferenceException` thrown between `Next` calls is a fault and is
  *not* absorbed; `Else(fallbackValue)` consumes nothing.
- **Faults:** a child failure inside `Next` is not double-wrapped (assert `Path` names the
  child, `InnerException` is not itself a `ShapeException` re-wrap); user code throwing
  between `Next` calls wraps once at the composite's path; `ArgumentException` from a parse
  stays absorbable.
- **Misuse:** zero `Next` calls throws case C and is not absorbed by `Optional`;
  `Next(nullShapeVariable)` throws with position and path and is not absorbed;
  `default(LayoutCursor).Next(x)` throws `InvalidOperationException`.
- **Escape (documentation, not xunit):** the four rejected snippets from §5.1 with their
  compiler codes, kept as a commented block in the test file — they cannot be asserted
  without a compilation harness, and they are the guard.
- **Inspection:** `Children` empty, `Description` equal to the applicative form's,
  `IOpaqueComposite.Reason` present; one cursor shape reused across many spaces
  concurrently yields identical results (capture-nothing lambda).
- **Regression:** the full existing suite green and unmodified, including
  `StackShapeTests` after the `FlowState` extraction.

## 10. Alternatives considered

1. **Interface cursor + `Func<ILayoutCursor, T>`.** Simpler and more familiar; verified to
   infer identically. Rejected as primary because every escape hazard — deferred LINQ,
   stashing the cursor in a field, returning it — compiles, and the guard becomes a runtime
   `_closed` check that fires late (at consumer enumeration time, far from the declaration).
   If the ref struct proves awkward in real scripts, this is the fallback: the whole
   difference is the cursor's declaration plus a `ShapeException`-family "the layout cursor
   was used after its layout returned" thrown from `Next` when closed, and §§3–4 are
   unchanged.
2. **A new factory name (`Flow`, `VerticalBy`).** Rejected: the reading is the same thing,
   resolution is unambiguous by argument count, and two names would fork the vocabulary.
3. **`params IShape[]` applicative overload with an `object?[]` combine.** Solves arity
   without opacity but loses static typing of each child — worse than both.
4. **Source-generated arities 9..16.** Keeps inspectability, does not fix the positional
   re-reading of tuples, and postpones the question.
5. **Recording children on first execution.** Rejected in §6.

## 11a. Adoption decisions (owner, 2026-09-01)

The cursor style is **adopted**; the applicative spelling will be removed (overloads,
`StackShape`, tuple `Select` combines) once the ergonomics-first design pass below settles.
Decisions recorded from that pass so far:

- **Rename "stack" to "flow."** The composite family is a *flow*, not a stack:
  `CursorStackShape` → flow naming throughout (types, files, docs). Candidate factory
  names: `VerticalFlow` / `HorizontalFlow` (final spelling not yet settled).
- **No redundant naming (variant B).** In the cursor form the member name
  (`Transactions = v.Next(...)`) already names the child; requiring `.Named("transactions")`
  an inch away is typing the name twice. The baseline spelling drops per-child `.Named`;
  diagnostics identify children by inference (ladder under exploration: explicit `.Named` >
  bare-identifier argument expression via `CallerArgumentExpression` > description + child
  ordinal). A1 locations were never at stake — only the declaration-path half.
- **Table projection should support inference too**: mapping row cells to properties by
  header caption, in addition to today's explicit by-index and by-caption access. Same
  philosophy: the framework reads names the user already wrote. (Separate work item.)
- Dry-run/static inspection is consciously forfeited: no side effects in projections, no
  harm in failure — the owner accepts opacity as the price of the ergonomics.
- **`.Until(landmark)` — the dual of `.After`** (from the investor-irr scenario): bounds a
  shape's extent by a forward content landmark. Semantics agreed with the owner: the extent
  ends just BEFORE the landmark row and consumed = the declared extent, so the landmark is
  never consumed — the following sibling's space starts at the landmark and its own `After`
  seek finds it at distance zero. This is what lets a `Repeat` stop before trailing content
  (the open "repeat cannot stop gracefully" question) and what the K-1 campaign's
  caption-to-caption sections need. Missing-landmark strictness DECIDED (owner): strict by
  default (a landmark is an anchor; anchors are loud), with a per-shape opt-in
  `orEnd: true` for exploratory/partially-completed spreadsheets — "until X, or the end."
  When the fallback is exercised it records an **Info** diagnostic (declared alternation,
  like `Choice` — not tolerance-after-failure like `Else`), so `MapWithDiagnostics` shows
  which sections ran open-ended. No ambient mode.
- **Reusable placements confirmed as the idiom**: a base shape (`var irrDetails =
  Repeat(...)`) placed twice via different `.After`/`.Until` chains — immutability already
  guarantees this; use-site name inference gives the two placements distinct diagnostic
  identities.

## 11. Deferred — SINCE DONE (see flow-vocabulary-spec.md)

~~`Overlay(o => …)`; LINQPad script conversions; deprecating either spelling. Both
spellings ship side by side while the experiment is judged.~~ All shipped by the
flow-vocabulary pass: `Overlay` joined the cursor grammar, every script was converted,
and the applicative spelling was deleted. Still genuinely deferred: the `Describe`/dry-run
renderer that must handle opaque nodes.
