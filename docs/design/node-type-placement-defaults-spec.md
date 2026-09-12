# Implementation spec + migration report: node-type placement defaults (piece #3)

**Status:** IMPLEMENTATION SPEC (2026-09-12), branch `experiment/record-primitive`. Third build off
`docs/design/table-extent-and-blank-rows.md`, after `SkipToFirstNonBlankCell` (commit 5add5e9) and the
`onBlank` blank-row strategy (commit 1cc6c5a). Unlike those two this is **NOT purely additive** — it
changes one shipped default offset. All citations are `file:line` against this branch. No production
code is written by this spec; it is the plan the senior-developer codes from, plus the migration
report the owner asked for before commit.

---

## 0. THE RECONCILIATION (read first) — it is Table-ONLY, and the code makes that clear

The two resolutions in the design doc read as in tension:

- **2026-09-11 ("do NOT re-scope"), `table-extent-and-blank-rows.md:140-149`:** silence = adjacency
  stays universal for `VerticalFlow`/`VerticalRepeat`; **`Table` ALONE** carries
  `SkipToFirstNonBlankCell()` as its own default.
- **2026-09-12 ("supersedes the no-baked-default"), `:122-136`:** **LEAF default →
  `SkipToFirstNonBlankCell`**, **COMPOSITE default → `Adjacent().SizedToChildren()`**.

**Decision: piece #3 changes `Table`'s offset default only** (`SkipBlankRows()` →
`SkipToFirstNonBlankCell()`), and the 2026-09-12 "LEAF default" is the *principle* that licenses a
self-contained region leaf (Table) to self-locate — not an instruction to move every leaf projection's
offset. The two resolutions are reconciled, not contradictory: 09-12 reverses the *no-baked-default*
call (it is fine for a leaf to bake a default, because leaves cannot orphan), and 09-11 remains the
*scope* (only a table region self-locates by default). This is not a judgment call left open — the code
forecloses the "all leaves" reading. The reasoning, grounded in the code:

### 0.1 What "leaf" means in the engine, and why cell leaves must keep adjacency

Every projection's offset is resolved by the engine at every level, *including inside a flow band*:
`ProjectionEngine.TryPlace` calls `projection.Placement.Offset.GetOffset(availableSpace)`
(`ProjectionEngine.cs:73`) and then slices `inner = availableSpace.GetSubspace(offset)` (`:99`) before
the child projects. So a leaf's own offset default is **observable and load-bearing** — it is *not*
moot / not "always parent-positioned." A `VerticalFlow` hands each child a full-width band starting
where the last left off (adjacency, `Placement.Default`, `Projection.Layouts.cs:49`); the child's own
offset then applies *within* that band.

That is exactly why a bare cell leaf must not skip-to-content. Consider the total-row idiom already in
the corpus, `HorizontalFlow(h => new Line(h.Next(Text()), h.Next(Decimal())))`
(`TableBlankRowStrategyTests`, commit 1cc6c5a). Each `Text()`/`Decimal()` is placed positionally in its
band. If a cell leaf's default were `SkipToFirstNonBlankCell`, an intentionally-blank cell (a null
column between fields, a spacer) would be silently skipped and the leaf would read its neighbour —
column alignment collapses. This is *the same "silence is an opinion" hazard* that keeps flows
adjacent (`:140-143`), now inside the leaf. In a `VerticalFlow` of cells it is worse: every cell would
jump to the first non-blank cell of the remaining space instead of stacking one row down. Positional
cell placement is a first-class use; a hidden column-skip breaks it.

The current cell-leaf defaults confirm they are adjacency today and must stay so:

| Leaf | Current default offset | Current default area | Cite |
|---|---|---|---|
| `Cell` | `NoOffset` (adjacency) | `ExplicitArea(1,1)` | `Projection.cs:25` |
| `Text/Decimal/Integer/Double/Date/Boolean` | `NoOffset` | `ExplicitArea(1,1)` | `Projection.cs:77` |
| `Row` / `Column` | `NoOffset` | strip area | `Projection.cs:81,89,93,101,799` |
| `Range` | `NoOffset` | `DiscoveredBlock()` | `Projection.cs:108` |
| `Field` | `NoOffset` | `ExplicitArea(2,1)` | `Projection.cs:634` |
| `Fields` | `FieldsPlacement` (self-anchors on first label) | — | `Projection.cs:649` |
| `Caption` | `To(RowContaining(text))` (locates its row) | `FullRow()` | `Projection.cs:142-144` |
| `Record` | `NoOffset` | `FullRow()` | `Projection.cs:560` |
| `ColumnLabels` | `NoOffset` | header row | `Projection.cs:525-527` |
| **`Table`** | **`SkipBlankRows()`** | `DiscoveredBlock()` / `ToEdgeBlock()` | `Projection.cs:850-851` |

`NoOffset` is `OffsetStrategies.MinOffset()` — the one canonical no-movement (`Placement.cs:19`).
`Table` is already the *only* leaf that self-locates vertically (`SkipBlankRows()`); piece #3 extends
that self-location to the column axis. That is a small, coherent widening of a decision the leaf
already made, not a new opinion imposed on leaves that never had one.

### 0.2 Why Table is orphaning-safe (the counterexample is composite-only)

`Table` is a leaf in the engine's sense: it is opaque, its body rows are internal
(`TableView`/`StreamBands`), and the engine never places a *sibling* projection relative to Table's
internal origin. Inside a `VerticalFlow`, `SkipToFirstNonBlankCell` moves Table's origin **within its
own full-width band only**; the flow keeps full-width row-bands and never moves its column origin, so
the next sibling still starts at column 0 one band down. That is precisely the doc's two-region
counterexample resolution (`:150-166`): a *global/composite* column-skip would move the flow origin
rightward and orphan a later region in earlier columns; a *leaf* column-skip cannot, because it is
confined to the region. So the leaf default is orphaning-safe, and the composite default must stay
`Adjacent` — which it already is.

### 0.3 The one genuinely-open (but out-of-scope) question — flagged, not silently decided

`Range` is the *other* region leaf (`Placement.Of(DiscoveredBlock())`, `NoOffset`,
`Projection.cs:108`). By the "region self-locates" principle it is the one leaf besides `Table` that
could defensibly adopt `SkipToFirstNonBlankCell`. **Recommendation: do NOT change `Range` in piece #3.**
Reasons: (a) the doc scopes the default to `Table` specifically ("a table region is a specific
document concept", `:144`); (b) `Range(width, height)` and `Range(area)` are frequently used
positionally, so a self-locating default on the discovered `Range` overload alone would split `Range`'s
three overloads' placement semantics; (c) keeping the footprint to one default honors "ship the
node-type defaults now, minimal change." If the owner wants `Range` to self-locate too, that is a
separate, additive decision with its own pin. **This is the only flagged item; it does not block
piece #3.**

---

## 1. The production change (exact, minimal)

### 1.1 The single line that changes

`Projection.cs:850-851`, `TablePlacement(BlankRowStrategy onBlank)`:

```csharp
// BEFORE
=> new Placement(OffsetStrategies.SkipBlankRows(), onBlank.IsStop ? DiscoveredBlock() : ToEdgeBlock());
// AFTER
=> new Placement(OffsetStrategies.SkipToFirstNonBlankCell(), onBlank.IsStop ? DiscoveredBlock() : ToEdgeBlock());
```

That is the whole behavioral change. The offset moves from "skip leading blank *rows*" (vertical only)
to "skip to the first non-blank *cell*" (down then across). `TablePlacement()` (zero-arg,
`Projection.cs:841`) delegates to this, so both header rungs and body rungs pick up the new offset with
no further edit. Update the XML doc on `TablePlacement` (`:843-849`) to say "onto the first non-blank
cell" rather than "skip leading blank rows."

`SkipToFirstNonBlankCell()` already exists and is public (`OffsetStrategies.cs`, commit 5add5e9); the
strategy scans row-major and early-returns at the first content cell
(`SkipToFirstNonBlankCellStrategy.GetOffset`, commit 5add5e9), and on an all-blank space returns
`new Offset(0, area.Height)` — the same empty-subspace answer `SkipBlankRows` gives, so an all-blank
table still yields an empty block rather than throwing.

### 1.2 What does NOT change

- **`DiscoveredBlock()` (`:853`) and `ToEdgeBlock()` (`:860`) are untouched.** The `onBlank` height
  rule and the offset are orthogonal (§3).
- **Composite default: no change — it is already `Adjacent().SizedToChildren()`.** `FlowProjection`
  (Vertical `Projection.Layouts.cs:49`, Horizontal `:57`), `OverlayProjection` (`:81`),
  `RepeatProjection` (`Projection.cs:813`) and `ChoiceProjection` (`:757`) all take `Placement.Default`
  = `NoOffset` (adjacency) + `null` area. A `null` area is "derive the extent from children"
  (`Placement.cs:11,41`), which *is* size-to-children. There is nothing to set; confirm-and-leave.
- **No public signature changes.** `TablePlacement` is private; the offset strategy it composes is an
  internal wiring detail. So no covenant/parity implication (§5.3).

---

## 2. Behavioral delta (precise, with a concrete before/after)

Let R0 = the first content row (after leading blank rows), C0 = the first non-blank column of R0.

- **`SkipBlankRows()`** lands the table origin at `(0, R0)` — column always 0.
- **`SkipToFirstNonBlankCell()`** lands at `(C0, R0)`.

They are **identical whenever C0 == 0** — i.e. any top-left-aligned table (the entire committed
corpus, §4). They **diverge only when the table's first content row starts at a column > 0.**

### 2.1 The col>0 case (the intended win)

```
        A        B          C
  1   (blank)   Investor   Amount      <- header, content starts at column B (C0 = 1)
  2   (blank)   Acme       10
  3   (blank)   Beta       20
```

- **Before (`SkipBlankRows`):** origin `(0,1)`. `DiscoveredBlock` then applies
  `TakeColumnsWhileAnyValue` from column A; column A is entirely blank, so the leading-column block is
  **0 wide** → the table gets a 0-column block and a by-name read of `"Investor"`/`"Amount"` **fails**
  (no columns). This is why the corpus's one col>0 table is written `Right(1).Of(Table(...))` to force
  the origin across (`LabeledAxisPrimitivesTests.cs:149-153`, `OffsetColumn()` sheet `:94-99`).
- **After (`SkipToFirstNonBlankCell`):** origin `(1,1)`. `DiscoveredBlock` from column B reads B,C
  (2 wide, 3 tall) → `Investor`/`Amount` bind and the table reads correctly **with no explicit
  offset.** `Table<Entity>()` on an indented region becomes a one-liner — the point of the change.

### 2.2 The ragged residual (documented, accepted, NOT fixed by this change)

`SkipToFirstNonBlankCell` finds the **first content row's** corner. If a lower body row reaches
*further left* than the header's first non-blank cell, that left content is orphaned:

```
        A        B          C
  1   (blank)   Investor   Amount      <- C0 = 1
  2   Acme      100                     <- body reaches back to column A, LEFT of the header corner
```

- **After:** origin `(1,1)`; the block spans B,C; `Acme` in column A is **lost** (the record's first
  cell reads column B = `100`, mis-binding). This is the residual the strategy's own tests already pin
  (`SkipToFirstNonBlankCell_OnARaggedRegion_ResolvesToTheFirstRowsCorner_TheDocumentedMiss`, commit
  5add5e9) and the doc calls out at `:130-134`. It is **distinct from orphaning** and the node-type
  split does not fix it; the eager escape hatch `SkipBlankRowsAndColumns()` (or explicit
  `Right(n)`/positional placement) is where a ragged table goes. Rare in practice: headered tables
  locate via the header, and headerless tables are overwhelmingly top-left-aligned. Note the residual
  is offset-level and therefore **orthogonal to `onBlank`** — it composes identically with Stop and
  with the run-to-edge policies.

---

## 3. Interaction with the two shipped pieces (both clean, orthogonal)

### 3.1 With `onBlank` (commit 1cc6c5a)

The offset is the table's **leading** placement (where its top-left corner sits); `onBlank` governs
blank rows **interior/trailing** to the body, expressed by the **height** rule
(`DiscoveredBlock` vs `ToEdgeBlock`, `Projection.cs:851`). They act on different axes at different
times: the offset resolves once at placement (`ProjectionEngine.cs:73`), the blank-row policy is
peeked per body row on the walker. Changing the offset from `SkipBlankRows` to
`SkipToFirstNonBlankCell` touches neither the height rule nor the per-row peek, so every
Stop/Skip/Fault/Tolerate/blankRecord path is unchanged in behavior. Composed example on a col>0 sheet
with an interior blank: the offset lands at `(C0, R0)`, then `Skip` runs `ToEdgeBlock` down from R0 and
omits interior blanks exactly as before — the column shift and the blank-skip are independent.

### 3.2 With `ToEdgeBlock` / run-to-edge

`ToEdgeBlock() = AllRows().TakeColumnsWhileAnyValue()` (`:860`). The offset only sets the block's
top-left corner; `AllRows` then runs down to the enclosing edge *from R0*, and
`TakeColumnsWhileAnyValue` runs across *from C0*. Moving the corner to `(C0, R0)` is exactly the corner
`TakeColumnsWhileAnyValue` should start from, so run-to-edge composes correctly and, for a col>0 table,
now reads the right columns (before, it would start at column 0 and take a 0-wide block just as in
§2.1). No new interaction, no special case.

### 3.3 With `.Until` / boundaries and pipeline overrides

A declared pipeline offset (`Right(n)`, `Below(m)`, `On(m)`, `OffsetBy(...)`) **replaces** the table's
default offset via `Placement.WithOffset` (`Steps.Offset`, `Placement.cs:56`), so any table already
placed by the pipeline is unaffected by this change (its default never runs). `.Until(landmark)` bounds
the height (`UntilProjection`) and does not touch the offset. `Sized(area)` replaces the **area** only,
via `Placement.WithArea` (`Steps.cs:172`, `Placement.cs:63`), leaving the default offset in force — so a
`Sized(...).Of(Table(...))` table *does* pick up the new offset, which is benign on all col-0 sheets
(§4).

---

## 4. Laziness — no regression when it becomes the default

`SkipToFirstNonBlankCell` was judged lazy-enough as an explicit strategy (column-cheap: reads a row at
a time, early-returns at the first content cell, never scans down a column — commit 5add5e9,
`SkipToFirstNonBlankCell_WithLeadingBlankRows_TouchesOnlyUpToTheFirstContentRow`). As Table's *default*
it runs on every table, at placement (`ProjectionEngine.cs:73`), before the area binds. Compared to the
outgoing `SkipBlankRows` (= `SkipRowsWhileAll(IsBlank)`), which reads across each leading blank row and
the first content row to decide "any value?", `SkipToFirstNonBlankCell` touches the **same rows** and
**fewer-or-equal cells** (it early-returns within R0). So it is strictly no worse than the offset it
replaces: **rows-touched is unchanged.** The engine reads `Area` for consumed size anyway; the offset
scan does not force the height. The 1M-row Stop table's ~1.6 MB peak profile (CLAUDE.md streaming
notes) is preserved — the offset still reads only the leading rows plus R0, not the sheet height, and
the downstream `DiscoveredBlock`/`ToEdgeBlock` incremental scan is unchanged. Add a cross-door /
CountingSpace pin (§5.2) proving Stop's rows-touched count is identical before/after.

---

## 5. MIGRATION-IMPACT REPORT

### 5.1 Corpus sweep — does anything shift?

**Result: the change is benign across the entire committed corpus.** Every `Table` that relies on its
default offset reads a top-left-aligned sheet (C0 == 0), where `SkipBlankRows` and
`SkipToFirstNonBlankCell` produce the identical offset `(0, R0)`. The one col>0 table in the tests is
explicitly overridden and so does not use the default. Detail:

| Site | Table(s) | Sheet col-0? | Uses default offset? | Verdict |
|---|---|---|---|---|
| `ProjectionModelAcceptanceTests` AllocationReport (`:115-123,118`) | `Table<Allocation>()` | yes — `AllocationValues()` content at col 0 (`:80`) | yes | benign (identical) |
| `ProjectionModelAcceptanceTests:322` | `Table(headerRows:0, eachRow:)` | n/a | **no** — `Below(...).Sized(...)` overrides offset (`WithOffset`) | unaffected |
| `ProjectionModelAcceptanceTests:425,438,516,555,600` | `Table<CashFlow>`, `Table<InvestorSummary>`, `Table<InvestorCashFlow>`, `Table(headerRows:1, eachRow:)` | yes — col-0 sheets | yes | benign |
| `CrossDoorDenotationTests:299-354` | `Table(...)` under `Heading` | yes — `"Quarterly Report"`, `"Client"` at col 0 (`:92-99`), deal sheets col 0 (`:118-151`) | yes (Heading/Sized do not move offset) | benign; add non-vacuity note |
| `LazyDenotationTests:238-315,491` | many `Table(...)` under `Sized(...)` | yes — `Headered()`/`LateWideningSheet()` `"Client"` at col 0 (`:80,99`) | yes (`Sized` = area only) | benign |
| `LazyForcingTests` `TallTable()` (`:377`) | `Table` | yes | yes | benign; rows-touched identical (§4) |
| `LabeledAxisPrimitivesTests` (`:136-170`) | `Table` vs primitive replica | col-0 sheets, **except** `OffsetColumn()` which is `Right(1)`-overridden on both (`:149-153`) | mixed | **replica must be updated — see 5.2** |
| LINQPad `simple-report`, `investors-by-deal`, `investor-summary`, `investor-irr` | `Table<...>` / `Table(r=>)` | top-left-aligned per corpus notes; none wrapped in `Right/Below` | yes | benign (owner should eyeball the `.xlsx` once on Windows) |
| LINQPad `scrubbed-k1` (LOCAL-ONLY, gitignored) | sections via `Heading(...).Of(...)`, `Fields`, `Below` | offsets overridden | no | unaffected; not in CI |

No example workbook is read by an automated test with a col>0 table on the default offset. `Range`
(the other `DiscoveredBlock` leaf) is unchanged (§0.3), so `edge-cases.linq`'s `Range(5,4,...)` and any
`Range` in tests are untouched.

### 5.2 Tests to UPDATE (few) and pins to ADD

**Update (1 file, hygiene — currently passes but goes stale):**

- `LabeledAxisPrimitivesTests.TablePlacementReplica()` (`:56-57`) is a hand copy of the private
  `Projection.TablePlacement()`. Change its offset `OffsetStrategies.SkipBlankRows()` →
  `OffsetStrategies.SkipToFirstNonBlankCell()`, and fix the comment at `:33` ("SkipBlankRows over a
  discovered block" → "skip-to-first-non-blank-cell over a discovered block") and the docstring at
  `:50-54`. Note: the suite stays *green even if you forget*, because the only col>0 differential
  (`AColumnOffsetTableReadsIdentically`, `:149`) overrides both sides with `Right(1)` and every other
  differential sheet is col-0 (where the two offsets agree). Update it anyway so the replica remains a
  faithful copy and the "would diverge the moment they drifted apart" claim (`:52-54`) stays true.

**Add (the col>0 divergence + residual + default-flip pins — this is where the new behavior is proven):**

1. **Default self-location (the win):** `Table(1, ReadLine).Map(OffsetColumn())` — with **no** explicit
   offset — now reads `Investor`/`Amount` correctly, where before it produced a 0-wide block / bind
   failure. This is the load-bearing new pin; put it beside `AColumnOffsetTableReadsIdentically` and
   contrast the two (with-override vs default-now-suffices).
2. **Before/after default identity on col-0:** on a top-left sheet, `Table(...)` denotes L3-identically
   before and after (implicitly held by every existing test staying green; state it once explicitly for
   the record).
3. **Ragged residual as the default:** on the §2.2 ragged sheet, the default `Table` resolves to the
   first-row corner and loses the lower-left cell — pinned as EXPECTED (mirror of the strategy-level
   ragged pin), with a comment pointing at `SkipBlankRowsAndColumns()` / explicit placement as the fix.
4. **Orthogonality with `onBlank`:** a col>0 sheet with an interior blank + `onBlank: Skip` lands at
   `(C0,R0)` and still skips the interior blank (offset shift and blank-skip independent).
5. **Laziness unchanged:** a CountingSpace / cross-door pin that Stop's rows-touched on a col-0 tall
   table is identical before and after (§4).

### 5.3 Covenant / parity implications — none

The completeness covenant and value-parity theory (`ProjectionBuildersParityTests`) match public
members by **name + parameter-count multiset** and read projection-returning members through the
`Observations` harness. This change touches **no public signature** — it rewires a private
`Placement` offset. `Projection.Table*` and `ProjectionBuilders<TSpace>.Table*` still forward to the
same factory, so their L3 readings stay identical and no reflection pin trips. The
`PlacementPipelineLawTests` refusal census counts pipeline *stage* members, not defaults — unaffected.
`SkipToFirstNonBlankCell()` was already added to every parity/refusal surface in commit 5add5e9; using
it as a default adds nothing new to enumerate.

### 5.4 The one-uniform-rule apply collapse — its effect on `Fields`/`Caption`

Collapsing `Steps.Move` and `Steps.Reoffset` into one `Steps.Offset` (§7) is behavior-preserving for
`Table`, but it also makes explicit a consequence for the other two self-locating shapes,
**`Fields` and `Caption`**: a bare declared pipeline offset (a movement such as `Down(n)`/`Right(n)`,
just like an anchor or `OffsetBy`) now **REPLACES** their self-anchor default rather than composing
onto it. This is the uniform law applied consistently — the compose-onto-anchor idiom is retired for a
bare movement on these shapes. Seek-then-adjust remains reachable via a nested pass-through projection
(wrapping the self-locating shape so its default resolves before the outer offset applies), and the
composable-anchor / forward-search feature (parked) would restore it directly.

**There is NO corpus or CI breakage from this.** No committed test or example relies on a movement
composing onto `Fields`'/`Caption`'s self-anchor. The only affected pin is
`FieldsTests.AMovementComposesOntoTheAnchor`, which is a false-green today — its assertion happens to
hold on its sheet, and it stays PASSING through this refactor. QA re-pins it to the replace semantics
in the next step; the senior-developer does not touch test files.

1. Change one strategy call: `Projection.cs:851` offset `SkipBlankRows()` →
   `SkipToFirstNonBlankCell()`; update the `TablePlacement` docstring (`:843-849`).
2. Do NOT touch composites (already `Adjacent` + derive-extent), cell/row/column/field/record leaves
   (must stay adjacency), `Range` (flagged §0.3, deferred), `DiscoveredBlock`/`ToEdgeBlock`, or any
   public signature.
3. Update `LabeledAxisPrimitivesTests.TablePlacementReplica()` (`:56-57`) + its comments to match.
4. Add the five pins in §5.2 (default self-location on `OffsetColumn`, col-0 identity, ragged residual,
   `onBlank` orthogonality, laziness-unchanged).
5. Gate: `dotnet build src/Unrect.sln -v q --no-incremental` (0 warnings) and
   `dotnet test src/Unrect.sln` green before commit.

---

## 7. Law: a declared pipeline offset REPLACES the shape's default (one uniform rule)

**An offset is an offset.** There is no "movement vs anchor" distinction in how an offset is applied —
they are all `IOffsetStrategy`, all one `Step` kind, all routed through one helper (`Steps.Offset`).
The law: a declared placement is a chain of offset steps composed from the origin, and it never
composes off a shape's invisible constructor default.

- **Declare any offset → the chain starts at the origin** (the shape's default is replaced).
- **Declare nothing → the default stands.**
- **Within the chain, each step composes onto the accumulated offset.**

`Steps.Offset` composes onto the existing offset only when it is BOTH `Placement.OffsetWasDeclared`
(a pipeline stage put it there) AND `Placement.HasDeclaredOffset` (it is a real, non-origin offset) —
otherwise it starts from the origin. This single rule applies to every offset step alike: the anchors
(`On`/`Below`/`RightOf`), the strategy door (`OffsetBy`), and the movements
(`Down`/`Right`/`AfterBlankRows`/`AfterBlankColumns`/`SkipToFirstNonBlankCell`). The earlier
`Move`/`Reoffset` split is gone; matches the documented intent on `Placement` ("a modifier is free to
replace" the shape's default).

The two-flag condition covers two start-from-origin cases that a single flag would miss:
- a **shape default** (`OffsetWasDeclared` false) — a `Table`/`Caption`/`Fields` self-locate — is
  replaced, not composed onto;
- an **explicit no-op**, `OffsetBy(MinOffset())` (`OffsetWasDeclared` true but `HasDeclaredOffset`
  false, since the value is the canonical origin), also starts from the origin — so "saying no
  movement out loud says nothing" stays intact (pinned by
  `PlacementTests.AMovementAfterMinOffset_ReplacesItRatherThanComposingOntoIt`).
A step composes only onto a base the pipeline actually *moved* to.

Anchors are structurally always the FIRST offset step — a second anchor is unspellable (the
`SecondAnchor` `[Obsolete(error)]` stubs refuse an anchor as a continuation) — so an anchor always hits
the `OffsetWasDeclared == false` branch and always starts from the origin, exactly as the former
unconditional `Reoffset` did. The collapse is therefore behavior-preserving.

The three canonical scenarios, with `Table`'s `SkipToFirstNonBlankCell` default:

1. `Table()` → the default `SkipToFirstNonBlankCell` (no pipeline offset).
2. `Right(1).Of(Table())` → REPLACES the default → offset is `Right(1)` (column 1), NOT self-locate + 1.
3. `SkipToFirstNonBlankCell().Right(1).Of(Table())` → the skip replaces the default AND marks the
   offset declared; then `Right(1)` composes onto it → skip-to-content, then right 1.

`Table`, `Caption`, and `Fields` are the three shapes with a non-trivial constructor default
(`HasDeclaredOffset` true, `OffsetWasDeclared` false — confirmed by grepping `new Placement(` across
`src/Unrect/`: `Table` via `SkipToFirstNonBlankCell`, `Caption` via `To(RowContaining(text))`, `Fields`
via `Then(To(ColumnWhere), To(RowWhere))`). The law applies to EVERY one of them uniformly: a declared
pipeline offset replaces the shape's own default, whichever shape it is. This reconciles with §0.1,
which already lists `Fields`/`Caption` among the self-locating leaves — there is no contradiction, and
no shape is special.

Anchors (`On`/`Below`/`RightOf`) set `OffsetWasDeclared`, so a movement chained after an anchor still
composes — the `AnchorMovementLaw` is unaffected.

Owner-decided 2026-09-12.
