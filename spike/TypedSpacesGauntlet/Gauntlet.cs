using System;
using System.Collections.Generic;

using Unrect;
using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;

namespace TypedSpacesGauntlet
{
  /// <summary>
  /// SPIKE. The seven scenarios of `docs/design/typed-spaces-experiment.md` §6, written the way a
  /// user would write them, and one scenario per phase of `projection-model-spec.md` since: 8 is
  /// phase 1's one-modifier-two-demands question, 9 is phase 4's row-projection slot and cells 3-4
  /// of the four-scenario matrix of its §6, 10 is phase 5's bind and cells 1-2. Every explicit type
  /// argument in this file is part of the annotation tax and is marked TAX.
  /// </summary>
  public static class Gauntlet
  {
    // =============================================================================================
    // Scenario 1 — the plain parser. MUST be spellable with zero new annotations.
    // =============================================================================================
    //
    // Verbatim today's vocabulary. The only type arguments are the ones the vocabulary has always
    // had (Table<Allocation>), and nothing here knows the word "space".
    public static Report PlainParser(ISpace sheet)
    {
      var title = Text();
      var rows = Table<Allocation>();

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
      var rows = Table<Allocation>();

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
      var rows = Table(row => new SourcedAllocation(
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
      var rows = Table(row => new SourcedAllocation(row["Account"].GetString(), row.FormulaAt(2)))
        .On(RowContaining("Account"))
        .Demanding(Formulas);                                     // TAX-free spelling (witness)

      IProjection<IFormulaSpace, IReadOnlyList<SourcedAllocation>> declared = rows;

      return declared.Map(sheet);
    }

    // 2c: the ascription without a witness, for comparison. Both type arguments must be written,
    // including the result type, because C# has no partial type-argument inference.
    public static IProjection<IFormulaSpace, IReadOnlyList<SourcedAllocation>> ProjectionAscribedByTypeArgs()
      => Table(row => new SourcedAllocation(row["Account"].GetString(), row.FormulaAt(2)))
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
      var rows = Table<Allocation>();
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

    // =============================================================================================
    // Scenario 9 — the row-projection slot, §6's four-scenario matrix. (Added 2026-09-08, phase 4.)
    // =============================================================================================
    //
    // Matrix cell 3 — NO HEADERS + DENSE. Zero coordinates: every leaf consumes its own cell and
    // the next one starts where it stopped, so adjacency does all the work. (Written here with a
    // header row, which the table consumes and the record never sees; binding to captions is the
    // bind, and that is phase 5.)
    public static IReadOnlyList<Allocation> DenseRows(ISpace sheet)
    {
      var allocation = HorizontalFlow(h => new Allocation(
        Account: h.Next(Text()),
        Symbol: h.Next(Text()),
        Weight: h.Next(Decimal())));

      var table = Table(headerRows: 1, eachRow: allocation).On(RowContaining("Account"));

      return table.Map(sheet);
    }

    // Matrix cell 4 — NO HEADERS + SPARSE/INCOMPLETE, spelled as §6 writes it. The overlay hands
    // every child the whole row, `Right(n)` says which column each one is, and `OrBlank` says which
    // of them a record may omit. The declaration IS the completeness contract, per field.
    public static IReadOnlyList<BuyingPowerRow> SparseRows(ISpace sheet)
    {
      var allocation = Overlay(o => new BuyingPowerRow(
        FundCode: o.Next(Text().Right(1)),
        Primary: o.Next(Decimal().OrBlank().Right(6)),
        Fep: o.Next(Decimal().OrBlank().Right(9))));

      var table = Table(headerRows: 0, eachRow: allocation)
        .Below(RowContaining("ACCOUNT"))
        .Sized(RowsWhileAnyValue());

      return table.Map(sheet);
    }

    // The demand flows through the slot: a row that reads a formula makes the TABLE demanding, with
    // nothing annotated but the overlay's own witness. Value and formula out of one cell is an
    // overlay's job, as it always was.
    public static IProjection<IFormulaSpace, IReadOnlyList<SourcedAllocation>> SourcedRecords()
    {
      var allocation = Overlay(Formulas, o => new SourcedAllocation(
        Account: o.Next(Text()),
        Formula: o.Next(Formula().Right(2))));

      return Table(headerRows: 1, eachRow: allocation).On(RowContaining("Account"));
    }

    /// <summary>A failure inside a record names the record it happened in, and the cell.</summary>
    public static string FailureInsideARecord()
    {
      try
      {
        _ = SparseRows(Sheets.BuyingPowerWithText());

        return "NO FAILURE (bad)";
      }
      catch (ProjectionException failure)
      {
        return $"{failure.Path} @ {failure.Location.A1} : {Head(First(failure.Message))}";
      }
    }

    /// <summary>
    /// OrBlank tolerates a blank and nothing else — a cell of the wrong kind still fails, in the
    /// document's own vocabulary.
    /// </summary>
    public static string OrBlankStillFailsOnKind()
    {
      try
      {
        _ = Decimal().OrBlank().Map(new GridSpace(new[,] { { CellValue.Of("n/a") } }));

        return "NO FAILURE (bad)";
      }
      catch (ProjectionException failure)
      {
        return First(failure.Message);
      }
    }

    /// <summary>
    /// And it belongs to a leaf that declares a kind: anywhere else the blank has no meaning to
    /// read, so it is a declaration error, raised where the projection is built rather than per file.
    /// </summary>
    public static string OrBlankIsForLeaves()
    {
      try
      {
        _ = Row(cells => cells[0].GetString()).OrBlank();

        return "NO FAILURE (bad)";
      }
      catch (System.ArgumentException problem)
      {
        return Head(problem.Message);
      }
    }

    /// <summary>
    /// The walk is monotone: the table's own bound is discovered a row at a time and each record is
    /// projected as its row is reached, so nothing reads behind the furthest row already read. The
    /// answer a windowed reader cares about is the second number.
    /// </summary>
    public static string SparseRowsAreReadForwardOnly()
    {
      var watched = new WatermarkSpace(Sheets.BuyingPower());

      var records = SparseRows(watched);

      return $"{records.Count} records; high-water row {watched.HighWaterMark}; deepest backward reach {watched.BackwardReach} rows";
    }

    // =============================================================================================
    // Scenario 10 — the bind, §6's matrix cells 1 and 2. (Added 2026-09-09, phase 5.)
    // =============================================================================================
    //
    // Matrix cell 1 — HEADERS + DENSE, and cell 2 — HEADERS + INCOMPLETE, which are one declaration:
    // an Overlay whose children say which column they are, addressed by CAPTION rather than by
    // count, with OrBlank on the field an export may omit. Caption positions are absolute, so the
    // same declaration reads a file whose columns have been reordered — which is the whole point of
    // binding to captions rather than to positions, and is asserted below by running this over two
    // sheets whose column orders differ.
    //
    // The bind runs once per Map, after the header is read and before any body row; what it returns
    // is an ordinary projection, applied to every row by the engine.
    public static IReadOnlyList<PartialAllocation> BoundRows(ISpace sheet)
    {
      var table = Table(headerRows: 1, eachRow: captions => Overlay(o => new PartialAllocation(
        Account: o.Next(Text().Right(captions["Account"])),
        Symbol: o.Next(Text().OrBlank().Right(captions["Symbol"])),
        Weight: o.Next(Decimal().Right(captions["Weight"])))))
        .On(RowContaining("Account"));

      return table.Map(sheet);
    }

    // The demanding variant: a Formula() inside a bound row makes the TABLE demand formulas, with
    // nothing annotated. This is the typing-is-intact claim made real — a lambda RETURNING a
    // projection exposes its demands in its return type, which is what a value-consuming lambda
    // (scenario 2a) could never do.
    public static IProjection<IFormulaSpace, IReadOnlyList<SourcedAllocation>> BoundSourcedRecords()
      => Table(headerRows: 1, eachRow: captions => Overlay(Formulas, o => new SourcedAllocation(
        Account: o.Next(Text().Right(captions["Account"])),
        Formula: o.Next(Formula().Right(captions["Weight"])))))
        .On(RowContaining("Account"));

    // A hoisted bound row is a FACTORY, with its dependence on the captions in its signature — and
    // passing the method group is also what names it: every record renders as 'AllocationRow'.
    private static IProjection<Allocation> AllocationRow(CaptionMap captions)
      => Overlay(o => new Allocation(
        Account: o.Next(Text().Right(captions["Account"])),
        Symbol: o.Next(Text().Right(captions["Symbol"])),
        Weight: o.Next(Decimal().Right(captions["Weight"]))));

    public static IReadOnlyList<Allocation> BoundByAHoistedFactory(ISpace sheet)
      => Table(headerRows: 1, eachRow: AllocationRow).On(RowContaining("Account")).Map(sheet);

    /// <summary>A caption the file does not carry fails through the map, listing the ones it does.</summary>
    public static string ACaptionTheFileDoesNotCarry()
    {
      try
      {
        _ = Table(headerRows: 1, eachRow: captions => Text().Right(captions["Ticker"]))
          .On(RowContaining("Account"))
          .Map(Sheets.Plain());

        return "NO FAILURE (bad)";
      }
      catch (ProjectionException failure)
      {
        return $"{failure.Path} @ {failure.Location.A1} : {First(failure.Message)}";
      }
    }

    /// <summary>And a bind with no header to read is a declaration error, raised where it is written.</summary>
    public static string ABindWithNoHeader()
    {
      try
      {
        _ = Table(headerRows: 0, eachRow: captions => Text().Right(captions["Account"]));

        return "NO FAILURE (bad)";
      }
      catch (ArgumentOutOfRangeException problem)
      {
        return Head(First(problem.Message));
      }
    }

    // The friend demo: the whole ladder's top rung inside an ordinary flow. Nothing about the table
    // is written down — the captions are the record's own member names, and the kinds are its
    // members' own types.
    public static InvestorBlock TypedTableInAFlow(ISpace sheet)
    {
      var cashFlows = Table<CashFlow>();

      var investor = VerticalFlow(v => new InvestorBlock(
        Name: v.Next(Text()),
        CashFlows: v.Next(cashFlows)));

      return investor.Map(sheet);
    }

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
      Show("9  dense row (flow, no coords)   ", JoinAll(DenseRows(plainSheet)));
      Show("9  sparse row (overlay, OrBlank) ", JoinAll(SparseRows(Sheets.BuyingPower())));
      Show("9  a record's demand is the table", DeclaredType(SourcedRecords()));
      Show("9  ... and it reads the formulas ", Join(SourcedRecords().Map(formulaSheet)));
      Show("9  failure names the record      ", FailureInsideARecord());
      Show("9  OrBlank vs a wrong kind       ", OrBlankStillFailsOnKind());
      Show("9  OrBlank vs a non-leaf         ", OrBlankIsForLeaves());
      Show("9  forward-only walk             ", SparseRowsAreReadForwardOnly());
      Show("10 bound rows (captions)         ", JoinAll(BoundRows(plainSheet)));
      Show("10 ... same bind, columns moved  ", JoinAll(BoundRows(Sheets.Reordered())));
      Show("10 a bound row's demand          ", DeclaredType(BoundSourcedRecords()));
      Show("10 ... and it reads the formulas ", Join(BoundSourcedRecords().Map(formulaSheet)));
      Show("10 hoisted bind, method group    ", JoinAll(BoundByAHoistedFactory(plainSheet)));
      Show("10 a caption the file lacks      ", ACaptionTheFileDoesNotCarry());
      Show("10 a bind with no header         ", ABindWithNoHeader());
      var investor = TypedTableInAFlow(Sheets.Investor());

      Show("10 typed table inside a flow     ", $"{investor.Name}: {JoinAll(investor.CashFlows)}");

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

    /// <summary>Records as the record type prints them — nulls included, which is the point.</summary>
    private static string JoinAll<T>(IReadOnlyList<T> records)
      => string.Join(" | ", records);

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

    /// <summary>The first line of a failure message — the subject and the problem, without the path and cell beneath them.</summary>
    private static string First(string message)
      => message.Split('\n')[0].TrimEnd('\r');

    private static string Head(string message)
      => message.Length <= 90 ? message : message.Substring(0, 90) + "...";

    private static void Show(string label, object? value) => Console.WriteLine($"{label}  {value}");
  }
}
