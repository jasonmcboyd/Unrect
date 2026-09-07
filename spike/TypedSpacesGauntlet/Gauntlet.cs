using System;
using System.Collections.Generic;

using Unrect;
using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.FormulaProjections;

namespace TypedSpacesGauntlet
{
  /// <summary>
  /// SPIKE. The seven scenarios of `docs/design/typed-spaces-experiment.md` §6, written the way a
  /// user would write them. Every explicit type argument in this file is part of the annotation tax
  /// and is marked TAX.
  /// </summary>
  public static class Gauntlet
  {
    // =============================================================================================
    // Scenario 1 — the plain parser. MUST be spellable with zero new annotations.
    // =============================================================================================
    //
    // Verbatim today's vocabulary. The only type arguments are the ones the vocabulary has always
    // had (TableRows<Allocation>), and nothing here knows the word "space".
    public static Report PlainParser(ISpace sheet)
    {
      var title = Text();
      var rows = TableRows<Allocation>();

      var report = VerticalFlow(v => new Report(
        Title: v.Next(title),
        Rows: v.Next(rows)));

      return report.Map(sheet);
    }

    // The same declaration applied to a space that offers MORE. Variance does the work; nothing is
    // written differently, which is the property the whole design rests on.
    public static Report PlainParserOverFormulaSheet(FormulaGridSpace sheet)
    {
      var title = Text();
      var rows = TableRows<Allocation>();

      var report = VerticalFlow(v => new Report(v.Next(title), v.Next(rows)));

      return report.Map(sheet);
    }

    // =============================================================================================
    // Scenario 2 — the same parser plus a projection that reaches through to formulas.
    // =============================================================================================
    //
    // 2a: the UNCHECKED reading. row.FormulaAt(2) compiles against any table, and the declaration's
    // type is IProjection<IReadOnlyList<SourcedAllocation>> — plain. Nothing acquired a demand, so
    // nothing stops this being applied to a grid, where every formula reads null. The typed layer
    // is blind here: a lambda's body is not part of its type.
    public static IReadOnlyList<SourcedAllocation> ProjectionUnchecked(ISpace sheet)
    {
      var rows = TableRows(row => new SourcedAllocation(
          Account: row["Account"].GetString(),
          Formula: row.FormulaAt(2)))
        .On(RowContaining("Account"));

      IProjection<IReadOnlyList<SourcedAllocation>> declared = rows;   // still plain: no demand inferred

      return declared.Map(sheet);
    }

    // 2b: the ASCRIBED reading. The demand is stated, not deduced — a promise the compiler enforces
    // downstream but never verifies upstream.
    public static IReadOnlyList<SourcedAllocation> ProjectionAscribed(FormulaGridSpace sheet)
    {
      var rows = TableRows(row => new SourcedAllocation(row["Account"].GetString(), row.FormulaAt(2)))
        .On(RowContaining("Account"))
        .Demanding(Formulas);                                     // TAX-free spelling (witness)

      IProjection<IFormulaSpace, IReadOnlyList<SourcedAllocation>> declared = rows;

      return declared.Map(sheet);
    }

    // 2c: the ascription without a witness, for comparison. Both type arguments must be written,
    // including the result type, because C# has no partial type-argument inference.
    public static IProjection<IFormulaSpace, IReadOnlyList<SourcedAllocation>> ProjectionAscribedByTypeArgs()
      => TableRows(row => new SourcedAllocation(row["Account"].GetString(), row.FormulaAt(2)))
        .On(RowContaining("Account"))
        .Demanding<IFormulaSpace, IReadOnlyList<SourcedAllocation>>();   // TAX x2

    // 2d: the CHECKED reading — a leaf that is itself formula-shaped. No ascription, no annotation:
    // the demand is in the leaf's type and travels by composition.
    public static string? ProjectionChecked(FormulaGridSpace sheet)
    {
      var totalFormula = Formula().On(RowContaining("Total")).Right(2);

      return totalFormula.Map(sheet);
    }

    // =============================================================================================
    // Scenario 3 — .On(RowWithFormula()): boundary-side capability.
    // =============================================================================================
    //
    // ZERO annotations. The receiver is a plain projection and the matcher is IRowLandmark<IFormulaSpace>;
    // inference unifies the two upper bounds to the more demanding one, so the modifier chain hands
    // back a demanding projection on its own.
    public static string FirstFormulaRow(FormulaGridSpace sheet)
    {
      var firstFormulaRow = Row(cells => cells[0].GetString()).On(RowWithFormula());

      IProjection<IFormulaSpace, string> declared = firstFormulaRow;   // the inferred type, spelled out

      return declared.Map(sheet);
    }

    // =============================================================================================
    // Scenario 4 — a mixed composite: one demanding child among plain ones.
    // =============================================================================================
    //
    // This is the known-hard inference spot: a lambda body cannot drive inference, so the flow has
    // to be TOLD what it is declared over. It is told by a WITNESS, and only by a witness — the
    // explicit-type-argument spelling (`VerticalFlow<IFormulaSpace, AuditedReport>(v => …)`, two
    // arguments of which one was pure ceremony) was pruned at the projection rename, subsumed by
    // this. Zero explicit type arguments survive: TSpace comes from `Formulas`, T from the lambda's
    // return, exactly as in the plain spelling. The scenario-4 tax is one word.
    public static AuditedReport MixedComposite(FormulaGridSpace sheet)
    {
      var title = Text();
      var rows = TableRows<Allocation>();
      var totalFormula = Formula().On(RowContaining("Total")).Right(2);

      var report = VerticalFlow(Formulas, v => new AuditedReport(
        Title: v.Next(title),
        Rows: v.Next(rows),
        TotalFormula: v.Next(totalFormula)));

      return report.Map(sheet);
    }

    // =============================================================================================
    // Scenario 5 — the test-with-GridSpace case. MUST NOT COMPILE.
    // =============================================================================================
    //
    // Nine attempts, in MustNotCompile.cs; ALL NINE fail. Messages recorded verbatim from
    // `dotnet build -p:DefineConstants=MUST_NOT_COMPILE`, 2026-09-06, Roslyn / net8.0 SDK.
    //
    // GOOD messages — they name the capability:
    //
    //   (b) report.Map<IFormulaSpace, AuditedReport>(Sheets.Plain())     [type arguments stated]
    //       CS1503: Argument 2: cannot convert from 'Unrect.GridSpace' to
    //               'Unrect.Spreadsheets.IFormulaSpace'
    //
    //   (d) IProjection<AuditedReport> laundered = report;
    //       CS0266: Cannot implicitly convert type
    //               'Unrect.Projections.IProjection<Unrect.Spreadsheets.IFormulaSpace, ...AuditedReport>' to
    //               'Unrect.Projections.IProjection<...AuditedReport>'.
    //               An explicit conversion exists (are you missing a cast?)
    //
    //   (f) Choice(Text().Select(t => (string?)t), Formula())  assigned to IProjection<string?>
    //       CS0266: ... IProjection<IFormulaSpace, string> ... to 'IProjection<string?>' ...
    //       NOTE: the Choice itself COMPILED — inference unified the two alternatives to the more
    //       demanding one, unprompted. Only the plain annotation on the return type failed.
    //
    //   (g) VerticalRepeat(Formula()) assigned to IProjection<IReadOnlyList<string?>>
    //       CS0266: ... IProjection<IFormulaSpace, IReadOnlyList<string>> ... — repeat unified too.
    //
    //   (h) Row(...).On(RowWithFormula()) assigned to IProjection<string>
    //       CS0266: ... IProjection<IFormulaSpace, string> ... to 'IProjection<string>' ...
    //       — the lift carried the demand with nothing annotated.
    //
    // BAD messages — correct refusal, useless explanation:
    //
    //   (a) report.Map(Sheets.Plain())
    //       CS0411: The type arguments for method
    //               'ProjectionExtensions.Map<TSpace, TResult>(IProjection<TSpace, TResult>, TSpace)'
    //               cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //
    //   (c) ISpace plain = Sheets.Plain(); report.Map(plain)
    //       CS0411: (identical to (a))
    //
    //   (i) Formula().Map(Sheets.Plain())
    //       CS0411: (identical to (a))
    //
    //   (e) VerticalFlow(v => ... v.Next(totalFormula) ...)   [demanding child, plain flow]
    //       CS0411: The type arguments for method 'LayoutCursor.Next<T>(IProjection<T>, string?)'
    //               cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //       — points at Next, says nothing about formulas, and the actual fix (annotate the FLOW,
    //         two lines up) is nowhere in the message.
    //
    // The pattern is exact and it is the experiment's sharpest ergonomic finding: wherever the
    // refusal falls on an ASSIGNMENT the message is excellent, and wherever it falls on generic
    // INFERENCE — which is every application site and every layout site — it is CS0411 boilerplate.

    // The run-time proof that (d) is the only hole worth naming: the cast IS available, and it
    // succeeds, because the demand never existed at run time. Nothing warns.
    public static bool DemandCanBeCastAway()
    {
      IProjection<IFormulaSpace, string?> demanding = Formula();
      var laundered = (IProjection<string?>)demanding;

      return laundered is not null;
    }

    /// <summary>
    /// The same refusal as (d), without conditional compilation: the assignability one way and not
    /// the other IS the variance direction the design rests on. Expect <c>plain -> demanding:
    /// True</c> and <c>demanding -> plain: False</c>.
    /// </summary>
    public static string VarianceDirection()
    {
      var plain = typeof(IProjection<AuditedReport>);
      var demanding = typeof(IProjection<IFormulaSpace, AuditedReport>);

      return $"plain usable as demanding: {demanding.IsAssignableFrom(plain)}; "
        + $"demanding usable as plain: {plain.IsAssignableFrom(demanding)}";
    }

    // =============================================================================================
    // Scenario 6 — the deferred-extent case: capability through a BoundedSpace chart.
    // =============================================================================================
    public static Probe DeferredExtent(FormulaGridSpace sheet)
    {
      var band = Range(
        RowsWhileAnyValue(),
        block => new Probe(
          RawTypeTest: block.Space is IFormulaSpace,                            // false through the chart
          ThroughSeam: block.Space.Capability<IFormulaSpace>() is not null,     // true
          Formula: block.Space.Capability<IFormulaSpace>()?.FormulaAt(2, 1)))
        .On(RowContaining("Account"));

      return band.Map(sheet);
    }

    // =============================================================================================
    // Scenario 7 — hoisted projection libraries: what the declared types look like now.
    // =============================================================================================
    //
    // A plain helper is EXACTLY as it was. This is the common case and it does not move.
    public static IProjection<Allocation> AllocationRow()
      => HorizontalFlow(h => new Allocation(
        Account: h.Next(Text()),
        Symbol: h.Next(Text()),
        Weight: h.Next(Decimal())));

    // A demanding helper grows one type argument in its return type — a statement of requirement,
    // and the thing a tooltip shows at every use site.
    public static IProjection<IFormulaSpace, SourcedAllocation> SourcedRow()
      => HorizontalFlow(Formulas, h => new SourcedAllocation(   // no type arguments; the witness carries it
        Account: h.Next(Text()),
        Formula: h.Next(Formula().Right(2))));

    // A GENERIC helper — one that works over whatever its caller demands — needs the parameter, the
    // constraint, and (because a layout lambda cannot infer) the type arguments again inside.
    public static IProjection<TSpace, IReadOnlyList<T>> Sections<TSpace, T>(IProjection<TSpace, T> item)   // TAX x2 + constraint
      where TSpace : class, ISpace
      => VerticalRepeat(item, separatedBy: BlankRows());

    // =============================================================================================
    // Scenario 8 — the phase-1 question: ONE modifier definition, both demands. (Added 2026-09-08.)
    // =============================================================================================
    //
    // Every modifier is now generic in the SHAPE's own type and hands that type straight back, so
    // the same six-call chain below is written once in the library and typed twice at the use site.
    // Read the two declared types: the plain chain stays IProjection<T> — which is what makes every
    // existing test and every hoisted `IProjection<T> Helper() => …` in the corpus keep compiling — and
    // the demanding chain stays IProjection<IFormulaSpace, T>, with nothing annotated on either.
    public static string ModifiersPreserveWhatTheyAreGiven()
    {
      IProjection<string> plain = Text()
        .Named("title").Down(1).Right(2).AfterBlankRows().Padded(0).On(RowContaining("Total"));

      IProjection<IFormulaSpace, string?> demanding = Formula()
        .Named("total").Down(1).Right(2).AfterBlankRows().Padded(0).On(RowContaining("Total"));

      return $"{DeclaredType(plain)} | {DeclaredType(demanding)}";
    }

    // The lift is the one place a modifier's own type is not enough, and it is not a duplicate: the
    // matcher's demand and the receiver's are unified by inference, which a fixed receiver type
    // cannot do. Written plain, read demanding, annotated nowhere.
    public static string LiftsRaiseWhatTheyTouch()
      => DeclaredType(Text().Named("cell").Until(RowWithFormula()));

    public static void Run()
    {
      var formulaSheet = Sheets.WithFormulas();
      var plainSheet = Sheets.Plain();

      Show("1  plain parser / plain sheet    ", PlainParser(plainSheet));
      Show("1  plain parser / formula sheet  ", PlainParserOverFormulaSheet(formulaSheet));
      Show("2a projection, unchecked (plain) ", Join(ProjectionUnchecked(plainSheet)));
      Show("2a projection, unchecked (formula)", Join(ProjectionUnchecked(formulaSheet)));
      Show("2b projection, ascribed          ", Join(ProjectionAscribed(formulaSheet)));
      Show("2d projection, checked leaf      ", ProjectionChecked(formulaSheet));
      Show("3  .On(RowWithFormula())         ", FirstFormulaRow(formulaSheet));
      Show("4  mixed composite (witness)     ", MixedComposite(formulaSheet));
      Show("5  demand castable away          ", DemandCanBeCastAway());
      Show("5  variance direction            ", VarianceDirection());
      Show("6  deferred extent               ", DeferredExtent(formulaSheet));
      Show("7  hoisted plain helper          ", DeclaredType(AllocationRow()));
      Show("7  hoisted demanding helper      ", DeclaredType(SourcedRow()));
      Show("7  hoisted generic helper        ", DeclaredType(Sections(SourcedRow())));
      Show("8  one modifier chain, two types ", ModifiersPreserveWhatTheyAreGiven());
      Show("8  a lift raises the demand      ", LiftsRaiseWhatTheyTouch());

      // The runtime-fault design, priced: what the typed layer makes unreachable.
      try
      {
        _ = FirstFormulaRowUntyped().Map(plainSheet);
        Show("5' boundary over a plain grid    ", "NO FAULT (bad)");
      }
      catch (ProjectionException exception)
      {
        Show("5' boundary over a plain grid    ", "faulted: " + Head(exception.Message));
      }
    }

    // The same boundary as scenario 3, with its demand deliberately thrown away — the only way to
    // reach the fault the typed layer exists to make unreachable.
    private static IProjection<string> FirstFormulaRowUntyped()
      => (IProjection<string>)Row(cells => cells[0].GetString()).On(RowWithFormula());

    private static string Join(IReadOnlyList<SourcedAllocation> rows)
      => string.Join(" | ", System.Linq.Enumerable.Select(rows, r => $"{r.Account}={r.Formula ?? "<none>"}"));

    /// <summary>The STATIC type of the expression — what a tooltip shows over a hoisted local.</summary>
    private static string DeclaredType<T>(T _) => Render(typeof(T));

    private static string Render(Type type)
    {
      if (!type.IsGenericType)
        return type.Name;

      var name = type.Name.Substring(0, type.Name.IndexOf('`'));
      var arguments = System.Linq.Enumerable.Select(type.GetGenericArguments(), Render);

      return $"{name}<{string.Join(", ", arguments)}>";
    }

    private static string Head(string message)
      => message.Length <= 90 ? message : message.Substring(0, 90) + "...";

    private static void Show(string label, object? value) => Console.WriteLine($"{label}  {value}");
  }
}
