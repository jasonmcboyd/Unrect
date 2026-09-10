# The Static Boundary — a criterion for the first-order fragment

**Status:** DRAFT for owner review. This is v0.4 §14.5: replacing the heuristic
("does removing it change which cells are read?") with a definition. The criterion is
the future IR's schema — it decides which nodes are inspectable data, which are staged
builders, and which are opaque executables that static interpreters must refuse.

## 1. The four classes (what the implementation actually has)

1. **First-order** — structure fixed at construction, before any document exists:
   leaves, typed leaves, modifiers, anchors, matchers, explicit and discovered extents,
   `VerticalRepeat`/`HorizontalRepeat` of a fixed item, `Choice` of fixed alternatives,
   `Table<T>()` (reflection resolves at construction), `.Under`/`Fields` (child
   structure fixed by arguments, even though the implementation runs cursor machinery).
2. **Staged** — structure produced by a builder that runs at a *defined boundary* with a
   *defined environment*: the table bind (`Table(headerRows:, eachRow: captions => …)`)
   — dependent, but on a `CaptionMap` minted once per occurrence, after which the row
   declaration is a description again. Reifiable as "a node with a typed hole."
3. **Value-dependent** — a layout lambda (`VerticalFlow(v => …)`) whose later children
   *may* be chosen by values already read. Expressible, discouraged, and not
   preventable by the API.
4. **Consumer-only** — `Select` functions and map lambdas: they never affect which
   cells are read, only what is built from them.

## 2. The criterion (proposed): constructional, two-dimensional

**The class of a node is decided by the spelling used to build it, never by analysis of
what its lambda happens to do.** A layout lambda is class 3 *by form* — even one that
never looks at a value — because staticity of behaviour is unobservable from outside
and undecidable in general. Sound, simple, conservative.

And the boundary is **two-dimensional**, because class 4 shows geometry and value are
separate opacities:

| | geometry static? | value invertible? |
|---|---|---|
| first-order | yes | yes (per the invertibility audit's sorting) |
| staged | after the stage boundary | after the stage boundary |
| value-dependent | no | no |
| consumer-only (`Select`) | **yes** | no |

So the demand/trace/dry-run interpreters accept `Select` nodes (the geometry is fully
known) while the writer refuses their value inversion — one node, two verdicts,
depending on which axis the interpreter needs. The old heuristic collapses these axes;
the criterion keeps them apart.

A dry-run probe (run a lambda against sentinel valuations, observe whether the child
sequence is valuation-independent) is **advice, never classification**: it may report
"this class-3 node behaved statically on the probe" as a tooling hint, but no
interpreter may promote a node across the boundary on its word — a different document
may take a different branch (the caching-invalidity argument, v0.4 §4.3).

## 3. The consequence nobody has said out loud

Under this criterion, **almost every real declaration's layouts are class 3** — the
lambda form is the only flow/overlay spelling that exists. `.Under` and `Fields` are
the only first-order layouts in the language. Which means:

> The writable/reifiable fragment, as the vocabulary stands, excludes nearly all
> layouts. The IR would mark the *spine* of most declarations opaque.

This is the real decision of this spec. The options:

- **(a) Accept it.** The writer program shrinks to tables, repeats, leaves, `.Under`
  cards; layouts write only when spelled through sugar. Honest, zero new surface —
  and possibly fatal to the writer's usefulness.
- **(b) Bring back a static layout spelling publicly.** The applicative form was
  deleted for the arity explosion; any return would need a shape that avoids it.
- **(c) Slot-cursor layouts** — a sketch for discussion, not a proposal yet:

  ```csharp
  StaticFlow(v => {
      var title  = v.Next(Text());          // Slot<string>, not string
      var table  = v.Next(Table<Tx>());     // Slot<IReadOnlyList<Tx>>
      return v.Combine(title, table, (t, rows) => new Report(t, rows));
  })
  ```

  `Next` returns an opaque `Slot<T>` carrying no value, so the child sequence
  *cannot* depend on values — there are none until `Combine`. The lambda runs once at
  construction, recording children: static by construction, lambda ergonomics, no
  arity explosion (`Combine` overloads or a builder can cap it). This is the
  applicative encoded with the cursor's syntax — the thing the vocabulary deleted,
  wearing the shape that survived. Whether that history makes it wiser or forbidden
  is an owner judgment.
- **(d) Declared staticity** — a marker (`StaticFlow(v => …)` with today's cursor)
  where the user *asserts* value-independence and a construction-time dry-run checks
  it once. Lighter than (c), but the check is per-declaration, not per-document, so
  it is a promise the engine can only spot-verify.

## 4. Decision points

- **E1 — Adopt the constructional criterion as the definition?** Recommended: yes.
  Behavior-based classification is unsound; form-based is checkable at a glance and
  matches how the refusals ledger already works (spellings, not behaviors).
- **E2 — The layout gap: which of (a)–(d)?** This decides the writer's fate and is
  the spec's real question. No recommendation yet — (c) deserves a scenario sketch
  session before judgment, and (a) is livable if the writer's value concentrates in
  tables and cards, which for real financial documents it may.
- **E3 — Adopt the two-axis model (geometry × value)?** Recommended: yes; it is what
  lets `Select` stay welcome in written declarations (the writer emits the *read*
  cells; `Select` only shaped the value).
- **E4 — Names for the classes.** Working set: *first-order / staged / dynamic /
  consumer-only*. Alternatives welcome; whatever is chosen becomes IR node vocabulary.

## 5. What this deliberately does not do

- No IR is designed here — this is its schema's first page, nothing more.
- No dry-run implementation — §3's probe is sketched only to be demoted to advice.
- Nothing changes in the engine or vocabulary; class 3 remains fully supported and
  documented as the expressive tier. The criterion *names* the boundary; it moves
  nothing across it.
