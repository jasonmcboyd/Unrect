# The refusals: what the typed projection layer will not compile, and why

**Status:** the campaign's compile-time evidence, in one place. Companion to
`projection-model-spec.md` (the design) and `typed-spaces-experiment.md` (the experiment that
produced the gauntlet). Every message below is verbatim from
`dotnet build spike/TypedSpacesGauntlet -p:DefineConstants=MUST_NOT_COMPILE`, re-run against the
tree at phase 7 — Roslyn, net8.0 SDK.

This is documentation as much as it is a record. The typed layer's whole product is a set of
*refusals*, and a refusal cannot be a test: a test project must compile. The fourteen spellings
below are therefore the only form the evidence can take, and they are re-runnable in exactly one
place — `spike/TypedSpacesGauntlet/MustNotCompile.cs`, where each is real code behind a define.
The positive halves (the same declarations where they DO compile, and the variance that makes both
true) are asserted in `src/Unrect.Tests/Projections/ProjectionModelAcceptanceTests.cs`.

## The three laws being enforced

1. **A demand is an input, so it is contravariant.** `IProjection<T>` derives from
   `IProjection<ISpace, T>`, so a plain declaration converts *up* to any demanding one and never
   back down. Every refusal below is a consequence of that one direction.
2. **Composition unifies to the most demanding child.** A flow, a repeat, a table's row, a choice's
   alternative — whatever it is, one demanding part makes the whole demanding, with nothing
   annotated.
3. **The demand is discharged at the file, not at the declaration.** `Map`/`MapWorkbook` is where a
   declaration meets a space, so it is where the mismatch is reported.

## The ledger

`Report()` below is a formula-demanding declaration:
`VerticalFlow(Formulas, v => new AuditedReport(v.Next(title), v.Next(rows), v.Next(totalFormula)))`,
where `totalFormula` is `Formula().On(RowContaining("Total")).Right(2)`.

| # | Attempted spelling | Compiler's answer (verbatim) | The law that refuses it |
|---|---|---|---|
| a | `Report().Map(Sheets.Plain())` | `CS0411: The type arguments for method 'ProjectionExtensions.Map<TSpace, TResult>(IProjection<TSpace, TResult>, TSpace)' cannot be inferred from the usage. Try specifying the type arguments explicitly.` | 3 — the demand is discharged at the file, and a `GridSpace` cannot discharge it. |
| b | `Report().Map<IFormulaSpace, AuditedReport>(Sheets.Plain())` | `CS1503: Argument 2: cannot convert from 'Unrect.GridSpace' to 'Unrect.Spreadsheets.IFormulaSpace'` | 3 — the same refusal, stated, and the message names the capability. |
| c | `ISpace plain = …; Report().Map(plain)` | `CS0411` (identical to (a)) | 3 — laundering the space through `ISpace` does not launder the demand. |
| d | `IProjection<AuditedReport> laundered = Report();` | `CS0266: Cannot implicitly convert type 'IProjection<IFormulaSpace, AuditedReport>' to 'IProjection<AuditedReport>'. An explicit conversion exists (are you missing a cast?)` | 1 — the conversion runs one way. |
| e | a demanding child inside a **plain** flow: `VerticalFlow(v => … v.Next(totalFormula) …)` | `CS0411: The type arguments for method 'LayoutCursor.Next<T>(IProjection<T>, string?)' cannot be inferred from the usage. Try specifying the type arguments explicitly.` | 2 — but see "the message taxonomy" below: this is the worst message in the set, and the scope (m) is its cure. |
| f | a demanding alternative in a plain `Choice`: `Choice(Text().Select(t => (string?)t), Formula())` assigned to `IProjection<string?>` | `CS0266: … 'IProjection<IFormulaSpace, string>' … to 'IProjection<string?>' …` | 2, then 1 — **the `Choice` itself compiled**: inference unified the two alternatives to the more demanding one, unprompted. Only the plain annotation failed. |
| g | `VerticalRepeat(Formula())` assigned to `IProjection<IReadOnlyList<string?>>` | `CS0266: … 'IProjection<IFormulaSpace, IReadOnlyList<string>>' …` | 2, then 1 — the repeat unified too. |
| h | `Row(cells => cells[0].GetString()).On(RowWithFormula())` assigned to `IProjection<string>` | `CS0266: … 'IProjection<IFormulaSpace, string>' … to 'IProjection<string>' …` | 2, then 1 — the *lift* carried the demand, with nothing annotated. |
| i | `Formula().Map(Sheets.Plain())` | `CS0411` (identical to (a)) | 3 — the leaf alone is enough to demand. |
| j | the **witnessed** flow applied to a plain sheet: `VerticalFlow(Formulas, …).Map(Sheets.Plain())` | `CS0411` (identical to (a)) | 3 — a witness states the demand; it does not weaken it. |
| k | a **scoped** declaration applied to a plain grid: `Over<ISpreadsheetSpace>()…​.Map(Sheets.Plain())` | `CS1503: Argument 1: cannot convert from 'Unrect.GridSpace' to 'Unrect.Spreadsheets.ISpreadsheetSpace'` | 3 — and note the code: inside a scope the receiver type is already fixed, so the refusal is a conversion rather than a failed inference. |
| l | the same at `Map`, stated: `scoped.Map<ISpreadsheetSpace, …>(plain)` | `CS1503: Argument 2: cannot convert from 'Unrect.Core.ISpace' to 'Unrect.Spreadsheets.ISpreadsheetSpace'` | 3 |
| m | a demanding child in a **plain scope**: `Over<ISpace>().VerticalFlow(v => v.Next(Formula()))` | `CS1503: Argument 1: cannot convert from 'IProjection<IFormulaSpace, string?>' to 'IProjection<Unrect.Core.ISpace, string>'` | 2 — the scoped twin of (e), and the campaign's sharpest ergonomic result. |
| n | the streaming sugar over a formula-reading declaration: `Formula().MapWorkbook(path, "Data")` | `CS1061: 'IProjection<IFormulaSpace, string?>' does not contain a definition for 'MapWorkbook' and no accessible extension method 'MapWorkbook' accepting a first argument of type 'IProjection<IFormulaSpace, string?>' could be found (are you missing a using directive or an assembly reference?)` | 3, by absence — a streamed sheet carries no formulas, so the demanding overload is not written. The tail of the message is misleading (no import would help) and is worn openly against the alternatives: a run-time fault, or a file's formulas silently read as absent. |

## The message taxonomy — the campaign's sharpest ergonomic finding

Read the codes down the table and the pattern is exact:

- Where the refusal falls on an **assignment or an argument** (CS0266, CS1503), the message is
  excellent: both types are named, the capability is spelled, and the fix is at the cursor.
- Where it falls on **generic inference** (CS0411), it is boilerplate: "the type arguments cannot be
  inferred", naming neither the capability nor the fix. (a), (c), (i), (j) are all the same sentence
  about `Map`, and (e) is the same sentence about `Next` — which points two lines away from the flow
  that needs the witness.

Entry B was specified for brevity and prose ordering. Its real payoff is (m): fixing the space on
the **cursor** turns (e), the worst message in the set, into one of the best, because the refusal
lands on the argument instead of on inference. *Scoping a declaration buys diagnosis, not just
brevity* — the strongest single argument for the scoped entry, and it was not why it was built.

## What the typed layer does NOT refuse — the three holes, named

1. **The cast escapes.** `(IProjection<string?>)demandingProjection` compiles and succeeds at run
   time, because the demand never existed at run time — it is a phantom parameter. Nothing warns.
   This is the price of a phantom, it is explicit at the site, and the runtime-fault design (§5 of
   the spec) is what catches the consequences.
2. **A lambda's body is invisible.** Anything a projection lambda *does* with the space it is handed
   is beyond the type system: `Table(row => … row.Space.Capability<IFormulaSpace>() …)` acquires no
   demand and reads null over a plain grid. This is the finding that made the original experiment
   say "do not adopt", and it is why the model moved rows onto *projections* (spec §6) and why
   `Formula()` ships as a leaf rather than as a reach-through extension (§5).
3. **`Landmark` drops the demand at the seam.** The untyped lift is still reachable, deliberately,
   so the runtime fault has to stay correct: a boundary that cannot look FAULTS, and no tolerance
   absorbs it (`src/Unrect.Tests/CapabilityFaultTests.cs`). The typed layer makes that fault
   unreachable in well-typed code; it does not delete it.
