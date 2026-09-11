# The Record Campaign — the compute-legal binder and the Record primitive

**Status:** DESIGN, decisions settled (2026-09-11), branch `experiment/record-primitive`
(off merged master, after the placement renovation). Grew from the monolith reading; the
exploration record is in `placement-spelling-rounds.md`, this spec is the scoped plan.

## Scope

1. **The compute-legal binder** — a dead-simple row/cell reading form.
2. **The Record primitive** — `Column`/`Row`/`Fields` unified as one singleton.

**Deferred to a later campaign:** the cardinality family (`Only`/`First` — the ceiling
to the repeat's `atLeast` floor). Reading-motivated, independent, not this round.

## The decisions (why compute-legal, not the machinery)

The dead-simple form is `Table(row => new { Amount = row.Decimal("Amount"), ... })` — one
line per field, name left, source right, values out. The design question was whether a
field could be a *computation* (`Net = row.Decimal("C") - row.Decimal("D")`).

- **COMPUTE-LEGAL wins.** Computation is allowed inline; the binder is a coordinate-aware
  accessor, not a recording mechanism. `row.Decimal("Amount")` reads the cell (column
  resolved from the caption, row known) and returns the value; on kind-mismatch it throws
  the shared `CellReading` sentence WITH the A1 location — better diagnostics than the
  `r["Amount"].GetDecimal()` hatch, which throws from a coordinate-less `CellValue`.
- **Rejected — materialize-once + detection.** Forbidding computation (to keep the read
  invertible) required a tracer to detect computed fields at construction, or an analyzer,
  or `Slot<T>` types that break value inference. The owner recoiled: "I don't want runtime
  failures… not a fan." The machinery existed only to police a form that inherently
  permits what we'd be forbidding — a category error.
- **The writer is DEFERRED, and self-recovering when built.** It was introduced as a
  *lens* to check the algebra, and that lens already paid off (presence, `Caption`-as-node,
  the class-4 boundary) with no artifact built. A future writer needs no reader constraint:
  it runs the row lambda with its OWN recording probe at write time to recover pure-read
  field→cell mappings, and refuses genuinely-computed fields. The recording machinery, if
  ever wanted, lives in the writer — not the reader.
- **Invertibility is honestly PARTIAL, acknowledged.** Only `read ∘ write = id` was ever
  the claim; `write ∘ read = id` is unachievable and never claimed (`SkipBlankRows` reads
  "however many" and writes one canonical blank; discovered extents write the value's own
  size — the writer picks representatives everywhere, lossy the other way). The algebra is
  a reader with a one-directional, choice-making writer-section over a fragment with lossy
  edges. Compute-legal moves one more thing into the "writer chooses or refuses" bucket
  that geometry was already in — a difference of degree.

## Diagnostics — what compute-legal gives (measured against the alternatives)

- The declaration-tree path DOWN TO the table and row index is fully logged (flow, repeat,
  table, row ordinal are real nodes): `Report -> Details -> VerticalRepeat[3] -> Table[7]`.
- WITHIN the row: the failing read is named by COLUMN + A1 + kind (`column 'Contributions':
  expected Number at D4, found Text`), via a structured `ProjectionException` the row
  handler enriches with the outer path. NOT a per-field node sub-path (`-> 'Net' ->
  Contributions#2`) — the fields are accessors, not nodes.
- For a FLAT table row this is equivalent (column name = field identity, A1 pins the cell).
  The field-node breadcrumb only adds value for records nested inside a row — and even then
  the failing cell is pinpointed. That gap is the *entire* visibility cost of dropping the
  machinery.
- The failure points at the true root cause (the bad cell), not at the output field. A
  computation error (div-by-zero) surfaces as a raw C# fault — inherent to allowing
  computation, identical to the hatch today.

## The binder's shape

- Caption keying AND index keying, coexisting: `row.Decimal("AMOUNT")` and `row.Text(8)`
  in one row projection (the founding buying-power case, `,,ACCOUNT,AMOUNT,,SHARES,,,`, has
  partial headers). Caption = discovered/named (robust, survives reorder); index =
  hard-coded (fragile, for headerless/structurally-fixed columns only) — the
  discovery-vs-explicit law, one layer down.
- Column (positional/index only, no captions) vs row (caption + index): likely two binder
  shapes (a positional cell binder for `Column`, a caption+index row binder for `Table`).
- Capability reach: `row.Formula("Amount")` is a BACKEND extension gated by
  `where TSpace : IFormulaSpace`, receiver-typed `RowBinder<TSpace>` — the demand is
  DECLARED at the rung (how the scope typed `row`), not climbed. Same recipe as the
  `Formula()` leaf. No public recording seam needed now (that was a materialize-once
  concern; compute-legal has no recording).
- The methods throw the shared `CellReading` template (the leaf firewall's one-template
  rule) — a `Decimal()` leaf and `row.Decimal(...)` describe a bad cell identically.

## The Record primitive

`Column`/`Row`/`Fields` are three corners of a 2×2 {labelled × orientation}; the fourth
(labelled-horizontal) is the eachRow record, trapped inside `Table`. Unify: one `Record`
mechanism (bounded region → single named-field result, fields by the binder, axis +
label-source as parameters); `Table` = `Record` repeated. **OPEN — the mirror-law
counterweight:** collapse to one `Record(orientation:, labels:)` operator (fewer names,
risks generic diagnostics), or one mechanism with the FOUR named faces kept as
orientation-fixed sugar (the mirror law's own answer — the machine is one, the axis
vocabulary stays specific). Burden of proof on the collapse.

## Build order

1. The compute-legal binder (coordinate-aware caption/index accessors + `CellReading`
   diagnostics) — a first implementation, not a feasibility spike (no machinery to prove).
2. The Record primitive over that binder; settle the named-faces question by the corpus
   read.
3. Migrate `Column`/`Row`/`Fields`/Table rungs; docs; acceptance.

## Out of scope

The cardinality family; the writer; anything requiring the materialize-once/detection
machinery this campaign deliberately refused.
