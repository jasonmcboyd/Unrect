using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The scoped entry: <c>Projection.Over&lt;TSpace&gt;()</c> and the eight members of the
  /// <see cref="ProjectionScope{TSpace}"/> it opens. The claim under test is the one the type's own
  /// documentation makes — <em>the scope is sugar over the witness form, not a second mechanism</em>
  /// — so every member is pinned against its witness twin rather than against a transcript of what
  /// it happens to do.
  /// <para>
  /// Three things could regress silently and each has its own section. A member could stop
  /// forwarding and grow its own behaviour (the twin tests). A member could stop forwarding the
  /// captured <c>declared</c> text and start capturing at the forwarding site instead, which is
  /// invisible wherever the scope's parameter happens to share the caller's identifier — so the
  /// naming tests deliberately use identifiers no parameter is called (the section says which). And
  /// a guard could move or change wording, which is what the parity section reads off both spellings
  /// at once.
  /// </para>
  /// <para>
  /// Written over <see cref="IFormulaSpace"/> rather than over a bundle: the scope's job is to fix
  /// <em>a</em> space, the published witness for this one is <c>Formulas</c>, and a capability with a
  /// real backend behind it (<see cref="FormulaGridSpace"/>) makes the demand a fact rather than a
  /// notion.
  /// </para>
  /// </summary>
  public class ProjectionScopeTests
  {
    private static ProjectionScope<IFormulaSpace> Scope() => Projection.Over<IFormulaSpace>();

    /// <summary>
    /// A capable sheet, two captions over two records:
    /// <code>
    /// Fund    Amount
    /// Alpha   100
    /// Beta    250
    /// </code>
    /// Small enough that a flow, an overlay, a table and a repeat each read something different from
    /// it and every reading can be written out in the assertion.
    /// </summary>
    private static ISpreadsheetSpace Sheet()
    {
      var cells = new object?[,]
      {
        { "Fund", "Amount" },
        { "Alpha", 100m },
        { "Beta", 250m },
      };

      var values = new CellValue[cells.GetLength(0), cells.GetLength(1)];

      for (var row = 0; row < cells.GetLength(0); row++)
        for (var column = 0; column < cells.GetLength(1); column++)
          values[row, column] = Adapt(cells[row, column]);

      return new FormulaGridSpace(values, new string?[cells.GetLength(0), cells.GetLength(1)]);
    }

    /// <summary>
    /// Everything a caller can observe about two projections that are supposed to be the same one:
    /// what they read, how much they consume, and what they call themselves.
    /// </summary>
    private static void SameProjection<T>(IProjection<IFormulaSpace, T> scoped, IProjection<IFormulaSpace, T> witnessed)
    {
      var sheet = Sheet();

      Assert.Equal(witnessed.Description, scoped.Description);
      Assert.Equal(witnessed.Name, scoped.Name);
      Assert.Equal(witnessed.Children.Count, scoped.Children.Count);
      Assert.Equal(witnessed.Map(sheet), scoped.Map(sheet));
      Assert.Equal(witnessed.Apply(sheet).Consumed, scoped.Apply(sheet).Consumed);
    }

    /// <summary>
    /// The other half: two projections that are the same one fail the same way, down to the path and
    /// the cell. This is the assertion that would catch a scope member that forwarded the projection
    /// but not the name it was declared under.
    /// </summary>
    private static ProjectionException SameFailure<T>(IProjection<IFormulaSpace, T> scoped, IProjection<IFormulaSpace, T> witnessed)
    {
      var sheet = Sheet();

      var byWitness = Assert.Throws<ProjectionException>(() => witnessed.Map(sheet));
      var byScope = Assert.Throws<ProjectionException>(() => scoped.Map(sheet));

      Assert.Equal(byWitness.Message, byScope.Message);
      Assert.Equal(byWitness.Path, byScope.Path);
      Assert.Equal(byWitness.Subject, byScope.Subject);
      Assert.Equal(byWitness.Location.A1, byScope.Location.A1);

      return byScope;
    }

    /// <summary>A bind, as a method group — the spelling the table rung recommends.</summary>
    private static IProjection<IFormulaSpace, decimal> AmountColumn(CaptionMap captions)
      => Decimal().Right(captions["Amount"]).Demanding(Formulas);

    /// <summary>The same bind pointed at the column of fund names, so every record fails.</summary>
    private static IProjection<IFormulaSpace, decimal> FundColumnAsANumber(CaptionMap captions)
      => Decimal().Right(captions["Fund"]).Demanding(Formulas);

    // --- 1. The eight members, each against its witness twin ---------------------------------------
    //
    // One test per member. The two spellings are written side by side and read the same sheet, so a
    // member that stopped forwarding — a placement of its own, a lost separator, a different name —
    // fails here rather than somewhere downstream.

    [Fact]
    public void VerticalFlow_IsItsWitnessTwin()
    {
      var p = Scope();

      SameProjection(
        p.VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Text())}"),
        VerticalFlow(Formulas, v => $"{v.Next(Text())}/{v.Next(Text())}"));

      // ...and it really is a flow: the second child reads the band under the first.
      Assert.Equal("Fund/Alpha", p.VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Text())}").Map(Sheet()));

      var failure = SameFailure(
        p.VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Decimal())}"),
        VerticalFlow(Formulas, v => $"{v.Next(Text())}/{v.Next(Decimal())}"));

      Assert.Equal("VerticalFlow -> Decimal#2", failure.Path);
      Assert.Equal("expected Number at A2, found Text", Problem(failure));
    }

    [Fact]
    public void HorizontalFlow_IsItsWitnessTwin()
    {
      var p = Scope();

      SameProjection(
        p.HorizontalFlow(h => $"{h.Next(Text())}/{h.Next(Text())}"),
        HorizontalFlow(Formulas, h => $"{h.Next(Text())}/{h.Next(Text())}"));

      Assert.Equal("Fund/Amount", p.HorizontalFlow(h => $"{h.Next(Text())}/{h.Next(Text())}").Map(Sheet()));

      var failure = SameFailure(
        p.HorizontalFlow(h => $"{h.Next(Text())}/{h.Next(Decimal())}"),
        HorizontalFlow(Formulas, h => $"{h.Next(Text())}/{h.Next(Decimal())}"));

      Assert.Equal("HorizontalFlow -> Decimal#2", failure.Path);
      Assert.Equal("expected Number at B1, found Text", Problem(failure));
    }

    [Fact]
    public void Overlay_IsItsWitnessTwin()
    {
      var p = Scope();

      SameProjection(
        p.Overlay(o => $"{o.Next(Text())}/{o.Next(Text().Right(1))}"),
        Overlay(Formulas, o => $"{o.Next(Text())}/{o.Next(Text().Right(1))}"));

      // Every child is handed the whole extent, so the second one places itself rather than
      // following the first — the difference from the flow above, over the same two cells.
      Assert.Equal("Fund/Amount", p.Overlay(o => $"{o.Next(Text())}/{o.Next(Text().Right(1))}").Map(Sheet()));

      var failure = SameFailure(
        p.Overlay(o => $"{o.Next(Text())}/{o.Next(Decimal().Right(1))}"),
        Overlay(Formulas, o => $"{o.Next(Text())}/{o.Next(Decimal().Right(1))}"));

      Assert.Equal("Overlay -> Decimal#2", failure.Path);
      Assert.Equal("expected Number at B1, found Text", Problem(failure));
    }

    [Fact]
    public void TheTableSlot_IsItsWitnessTwin()
    {
      var p = Scope();
      var fundName = Text().Demanding(Formulas);

      SameProjection(
        p.Table(headerRows: 1, eachRow: fundName),
        Table(headerRows: 1, eachRow: fundName));

      Assert.Equal(new[] { "Alpha", "Beta" }, p.Table(headerRows: 1, eachRow: fundName).Map(Sheet()));

      var amount = Decimal().Demanding(Formulas);

      var failure = SameFailure(
        p.Table(headerRows: 1, eachRow: amount),
        Table(headerRows: 1, eachRow: amount));

      // The index is the table's and the label is the row's, exactly as the plain rung renders it.
      Assert.Equal("Table[0] -> 'amount' (Decimal)", failure.Path);
      Assert.Equal("expected Number at A2, found Text", Problem(failure));
    }

    [Fact]
    public void TheTableBind_IsItsWitnessTwin()
    {
      var p = Scope();

      SameProjection(
        p.Table(headerRows: 1, eachRow: AmountColumn),
        Table(headerRows: 1, eachRow: AmountColumn));

      Assert.Equal(new[] { 100m, 250m }, p.Table(headerRows: 1, eachRow: AmountColumn).Map(Sheet()));

      var failure = SameFailure(
        p.Table(headerRows: 1, eachRow: FundColumnAsANumber),
        Table(headerRows: 1, eachRow: FundColumnAsANumber));

      Assert.Equal("Table[0] -> 'FundColumnAsANumber' (Decimal)", failure.Path);
      Assert.Equal("expected Number at A2, found Text", Problem(failure));
    }

    [Fact]
    public void VerticalRepeat_IsItsWitnessTwin()
    {
      var p = Scope();
      var line = Text().Demanding(Formulas);

      SameProjection(
        p.VerticalRepeat(line),
        VerticalRepeat(line));

      Assert.Equal(new[] { "Fund", "Alpha", "Beta" }, p.VerticalRepeat(line).Map(Sheet()));

      var amountLine = Decimal().Demanding(Formulas);

      var failure = SameFailure(
        p.VerticalRepeat(amountLine),
        VerticalRepeat(amountLine));

      Assert.Equal("VerticalRepeat[0] -> 'amountLine' (Decimal)", failure.Path);
    }

    [Fact]
    public void VerticalRepeat_CarriesItsSeparatorAndItsFloorThrough()
    {
      // The two optional parameters, which a forwarding member could drop without any reading
      // changing over a gapless sheet — so the sheet here has a gap in it. The separator is checked
      // by what a blank-separated sheet reads with it and how loudly it fails without it; the floor
      // by asking for more occurrences than the sheet holds.
      var p = Scope();
      var line = Text().Demanding(Formulas);

      var sparse = new object?[,] { { "a" }, { null }, { "b" } };
      var values = new CellValue[3, 1];

      for (var row = 0; row < 3; row++)
        values[row, 0] = Adapt(sparse[row, 0]);

      var sheet = new FormulaGridSpace(values, new string?[3, 1]);

      Assert.Equal(new[] { "a", "b" }, p.VerticalRepeat(line, separatedBy: BlankRows()).Map(sheet));
      Assert.Equal(
        VerticalRepeat(line, separatedBy: BlankRows()).Map(sheet),
        p.VerticalRepeat(line, separatedBy: BlankRows()).Map(sheet));

      // Without the separator the blank row is not a gap but the next occurrence, and a blank is a
      // kind failure to Text — so a dropped separator would be loud rather than quietly short.
      Assert.Equal(
        "expected Text at A2, found Blank",
        Problem(Assert.Throws<ProjectionException>(() => p.VerticalRepeat(line).Map(sheet))));

      var floor = Assert.Throws<ProjectionException>(
        () => p.VerticalRepeat(line, separatedBy: BlankRows(), atLeast: 3).Map(sheet));

      var twin = Assert.Throws<ProjectionException>(
        () => VerticalRepeat(line, separatedBy: BlankRows(), atLeast: 3).Map(sheet));

      Assert.Equal("expected at least 3 occurrences but found 2", Problem(floor));
      Assert.Equal(twin.Message, floor.Message);
    }

    [Fact]
    public void HorizontalRepeat_IsItsWitnessTwin()
    {
      var p = Scope();
      var cell = Text().Demanding(Formulas);

      SameProjection(
        p.HorizontalRepeat(cell),
        HorizontalRepeat(cell));

      Assert.Equal(new[] { "Fund", "Amount" }, p.HorizontalRepeat(cell).Map(Sheet()));

      var amountCell = Decimal().Demanding(Formulas);

      var failure = SameFailure(
        p.HorizontalRepeat(amountCell),
        HorizontalRepeat(amountCell));

      Assert.Equal("HorizontalRepeat[0] -> 'amountCell' (Decimal)", failure.Path);
    }

    [Fact]
    public void Choice_IsItsWitnessTwin()
    {
      var p = Scope();
      var total = Caption("Total").Demanding(Formulas);
      var anything = Text().Demanding(Formulas);

      SameProjection(
        p.Choice(total, anything),
        Choice(total, anything));

      // The first alternative disagrees with the sheet, so the second one reads it.
      Assert.Equal("Fund", p.Choice(total, anything).Map(Sheet()));

      var net = Caption("Net").Demanding(Formulas);

      SameFailure(
        p.Choice(total, net),
        Choice(total, net));
    }

    // --- 2. Naming through the scope ----------------------------------------------------------------
    //
    // THE forward that would regress in silence. Every capturing member takes the caller's text in a
    // [CallerArgumentExpression] parameter and passes it on explicitly; drop that argument and the
    // inner factory captures the text at the FORWARDING site instead — which is the scope's own
    // parameter name, and reads exactly like a correct capture wherever the caller's identifier
    // happens to match it.
    //
    // So none of the identifiers below is called `item`, `eachRow`, or `projection`. A test written
    // with those names would pass through the bug.

    [Fact]
    public void ARepeatsItemIsLabelledByTheIdentifierItWasWrittenAs()
    {
      var p = Scope();
      var investorDetail = Decimal().Demanding(Formulas);

      var failure = Assert.Throws<ProjectionException>(() => p.VerticalRepeat(investorDetail).Map(Sheet()));

      Assert.Equal("'investorDetail'", failure.Subject);
      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndSoIsAHorizontalRepeatsItem()
    {
      var p = Scope();
      var quarterlyColumn = Decimal().Demanding(Formulas);

      var failure = Assert.Throws<ProjectionException>(() => p.HorizontalRepeat(quarterlyColumn).Map(Sheet()));

      Assert.Equal("HorizontalRepeat[0] -> 'quarterlyColumn' (Decimal)", failure.Path);
    }

    [Fact]
    public void ATablesRowIsLabelledByTheIdentifierItWasWrittenAs()
    {
      var p = Scope();
      var allocationRow = Decimal().Demanding(Formulas);

      var failure = Assert.Throws<ProjectionException>(() =>
        p.Table(headerRows: 1, eachRow: allocationRow).Map(Sheet()));

      Assert.Equal("Table[0] -> 'allocationRow' (Decimal)", failure.Path);
    }

    [Fact]
    public void ABindPassedAsAMethodGroupLabelsEveryRecordWithItsName()
    {
      var p = Scope();

      var failure = Assert.Throws<ProjectionException>(() =>
        p.Table(headerRows: 1, eachRow: FundColumnAsANumber).Map(Sheet()));

      Assert.Equal("Table[0] -> 'FundColumnAsANumber' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndALayoutsChildIsNamedByTheCursorAsItAlwaysWas()
    {
      // Here the capture is the typed cursor's own, so what is under test is that re-typing the
      // cursor did not cost the ladder.
      var p = Scope();
      var transactions = Decimal().Demanding(Formulas);

      var failure = Assert.Throws<ProjectionException>(() =>
        p.VerticalFlow(v => v.Next(transactions)).Map(Sheet()));

      Assert.Equal("'transactions'", failure.Subject);
      Assert.Equal("VerticalFlow -> 'transactions' (Decimal)", failure.Path);
    }

    [Fact]
    public void AnExplicitNameStillOutranksTheIdentifier()
    {
      var p = Scope();
      var investorDetail = Decimal().Named("detail").Demanding(Formulas);

      var failure = Assert.Throws<ProjectionException>(() => p.VerticalRepeat(investorDetail).Map(Sheet()));

      Assert.Equal("VerticalRepeat[0] -> 'detail' (Decimal)", failure.Path);
    }

    // --- 3. Guard parity -----------------------------------------------------------------------------
    //
    // A scope member must refuse what the plain family refuses, with the plain family's own words: a
    // declaration written through a scope is the same declaration, so a broken one has to be broken
    // in the same way. Every test here reads both spellings and compares them.

    [Fact]
    public void TheLayoutsRefuseANullLambdaByItsParameterName()
    {
      var p = Scope();

      Assert.Equal("build", Assert.Throws<ArgumentNullException>(() => p.VerticalFlow<int>(null!)).ParamName);
      Assert.Equal("build", Assert.Throws<ArgumentNullException>(() => p.HorizontalFlow<int>(null!)).ParamName);
      Assert.Equal("build", Assert.Throws<ArgumentNullException>(() => p.Overlay<int>(null!)).ParamName);

      // Word for word what the plain factory says, so a caller reading the message cannot tell — and
      // does not need to tell — which spelling raised it.
      Assert.Equal(
        Assert.Throws<ArgumentNullException>(() => VerticalFlow<int>(null!)).Message,
        Assert.Throws<ArgumentNullException>(() => p.VerticalFlow<int>(null!)).Message);
    }

    [Fact]
    public void ALayoutRefusesANullChildTheWayAPlainOneDoes()
    {
      // The typed cursor re-types the child and hands it to the same state, so the refusal is the
      // layout's own and says which position it happened at.
      var p = Scope();

      var scoped = Assert.Throws<ProjectionException>(() =>
        p.VerticalFlow(v => v.Next<int>(null!)).Map(Sheet()));

      var plain = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => v.Next<int>(null!)).Map(Sheet()));

      Assert.Equal("a null projection was declared as child 1", Problem(scoped));
      Assert.Equal(plain.Message, scoped.Message);
    }

    [Fact]
    public void ATablesBindIsRefusedWhenNullByItsParameterName()
    {
      var p = Scope();

      var scoped = Assert.Throws<ArgumentNullException>(
        () => p.Table(1, (Func<CaptionMap, IProjection<IFormulaSpace, int>>)null!));

      var plain = Assert.Throws<ArgumentNullException>(
        () => Table(1, (Func<CaptionMap, IProjection<int>>)null!));

      Assert.Equal("eachRow", scoped.ParamName);
      Assert.Equal(plain.Message, scoped.Message);
    }

    [Fact]
    public void ATablesRowIsRefusedWhenNull_InThePlainFamilysWords()
    {
      // Once a recorded divergence, closed the same day: the typed factories used to reach the
      // plain family through ProjectionExtensions.Plain, whose cast guard saw the null first and
      // answered ArgumentException("projection"). The typed factories now guard their own
      // parameter before delegating, so both spellings refuse a null row in the same words.
      var p = Scope();

      var plain = Assert.Throws<ArgumentNullException>(() => Table(1, (IProjection<int>)null!));
      var scoped = Assert.Throws<ArgumentNullException>(() => p.Table(1, (IProjection<IFormulaSpace, int>)null!));

      Assert.Equal("eachRow", plain.ParamName);
      Assert.Equal("eachRow", scoped.ParamName);
    }

    [Fact]
    public void ARepeatsItemIsRefusedWhenNull_TheSameWay()
    {
      // The repeats' half of the closed divergence above: guarded before the delegation, so the
      // typed spelling answers in the plain family's words.
      var p = Scope();
      var plain = Assert.Throws<ArgumentNullException>(() => VerticalRepeat((IProjection<int>)null!));
      var scoped = Assert.Throws<ArgumentNullException>(() => p.VerticalRepeat((IProjection<IFormulaSpace, int>)null!));

      Assert.Equal("item", plain.ParamName);
      Assert.Equal("item", scoped.ParamName);
    }

    [Fact]
    public void ARepeatRefusesANegativeFloorInThePlainFamilysWords()
    {
      var p = Scope();
      var line = Text().Demanding(Formulas);

      var scoped = Assert.Throws<ArgumentOutOfRangeException>(() => p.VerticalRepeat(line, atLeast: -1));
      var plain = Assert.Throws<ArgumentOutOfRangeException>(() => VerticalRepeat(Text(), atLeast: -1));

      Assert.Equal("atLeast", scoped.ParamName);
      Assert.StartsWith("A repeat cannot require a negative number of occurrences.", scoped.Message);
      Assert.Equal(plain.Message, scoped.Message);

      Assert.Equal(
        scoped.Message,
        Assert.Throws<ArgumentOutOfRangeException>(() => p.HorizontalRepeat(line, atLeast: -1)).Message);
    }

    [Fact]
    public void ChoiceRefusesANullArrayAndANullAlternative()
    {
      var p = Scope();
      var first = Text().Demanding(Formulas);

      Assert.Equal("alternatives", Assert.Throws<ArgumentNullException>(() => p.Choice<int>(null!)).ParamName);

      var scoped = Assert.Throws<ArgumentException>(() => p.Choice(first, null!));
      var plain = Assert.Throws<ArgumentException>(() => Choice(Text(), null!));

      Assert.Equal("alternatives", scoped.ParamName);
      Assert.StartsWith("Alternative 2 is null.", scoped.Message);
      Assert.Equal(plain.Message, scoped.Message);
    }

    [Fact]
    public void AndAChoiceOfOneIsStillNoChoiceAtAll()
    {
      var p = Scope();
      var only = Text().Demanding(Formulas);

      var scoped = Assert.Throws<ArgumentException>(() => p.Choice(only));
      var plain = Assert.Throws<ArgumentException>(() => Choice(Text()));

      Assert.StartsWith("A choice needs at least two alternatives.", scoped.Message);
      Assert.Equal(plain.Message, scoped.Message);
    }

    // --- 4. headerRows, validated where the table is written ------------------------------------------

    [Fact]
    public void ABindWithNoHeaderRowIsADeclarationError()
    {
      // A bind reads this file's captions, so a table with no header has nothing to hand it. Raised
      // at construction, not per file, and in the plain family's words.
      var p = Scope();

      var scoped = Assert.Throws<ArgumentOutOfRangeException>(() => p.Table(headerRows: 0, eachRow: AmountColumn));
      var plain = Assert.Throws<ArgumentOutOfRangeException>(
        () => Table(headerRows: 0, eachRow: (Func<CaptionMap, IProjection<decimal>>)(captions => Decimal())));

      Assert.Equal("headerRows", scoped.ParamName);
      Assert.StartsWith(
        "A table whose rows are declared from its captions needs a header row to read them from; headerRows must be 1.",
        scoped.Message);
      Assert.Equal(plain.Message, scoped.Message);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void AndAMultiRowHeaderIsRefusedOnBothRungs(int headerRows)
    {
      var p = Scope();
      var line = Text().Demanding(Formulas);

      var slot = Assert.Throws<ArgumentOutOfRangeException>(() => p.Table(headerRows, eachRow: line));
      var bind = Assert.Throws<ArgumentOutOfRangeException>(() => p.Table(headerRows, eachRow: AmountColumn));
      var plain = Assert.Throws<ArgumentOutOfRangeException>(() => Table(headerRows, eachRow: Text()));

      Assert.Equal("headerRows", slot.ParamName);
      Assert.StartsWith("A table has either 0 or 1 header rows;", slot.Message);
      Assert.Equal(plain.Message, slot.Message);
      Assert.Equal(plain.Message, bind.Message);
    }

    // --- 5. The scope carries nothing ------------------------------------------------------------------

    [Fact]
    public void DefaultIsAsGoodAsTheDoor()
    {
      // The type's own documentation says so, and it is what makes the scope free to hold in a local,
      // pass around, or leave to a field initialiser. A struct with state would break this the moment
      // it grew one, which is exactly when this test should fail.
      var door = Projection.Over<IFormulaSpace>();
      var zero = default(ProjectionScope<IFormulaSpace>);

      var line = Text().Demanding(Formulas);

      SameProjection(
        zero.VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Text())}"),
        door.VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Text())}"));

      SameProjection(zero.VerticalRepeat(line), door.VerticalRepeat(line));
      SameProjection(zero.Table(headerRows: 1, eachRow: AmountColumn), door.Table(headerRows: 1, eachRow: AmountColumn));

      // ...including where they fail, which is where a scope holding anything would show it.
      SameFailure(
        zero.Table(headerRows: 1, eachRow: FundColumnAsANumber),
        door.Table(headerRows: 1, eachRow: FundColumnAsANumber));
    }

    [Fact]
    public void AScopeRaisesEverythingBuiltThroughItToItsOwnSpace()
    {
      // The guidance in the type's remarks, made a fact: a PLAIN child built through a bundle scope
      // comes back demanding the bundle. This is the point in application code and the mistake in a
      // hoisted helper, and either way it is what the static type says.
      var bundle = Projection.Over<ISpreadsheetSpace>();

      IProjection<ISpreadsheetSpace, IReadOnlyList<string>> raised = bundle.VerticalRepeat(Text());

      Assert.Equal("VerticalRepeat", raised.Description);

      // A narrow helper still composes into it — variance carries a smaller demand inwards, never
      // outwards, which is the whole of the demand story in one line.
      IProjection<ISpreadsheetSpace, IReadOnlyList<string?>> composed =
        bundle.VerticalRepeat(Projection.Over<IFormulaSpace>().Overlay(o => o.Next(Formula())));

      Assert.Equal("VerticalRepeat", composed.Description);
    }

    [Fact]
    public void AScopedDeclarationReadsARealSheetThroughTheEagerDoor()
    {
      // End to end, over the committed formula fixture and the door that actually vends the bundle:
      // the demand is answered once in prose position, the children say nothing about it, and a cell's
      // value and its formula come back from one reading. The unit tests above are over a synthetic
      // backend on purpose; this is the one that says the entry works where it is meant to be used.
      var p = Projection.Over<ISpreadsheetSpace>();

      var cell = p.Overlay(o => (Value: o.Next(Text()), Formula: o.Next(Formula())))
        .On(RowContaining("Text"))
        .Right(1);

      var sheet = SpreadsheetSpace.CreateWithFormulas(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
        "Formulas");

      var read = cell.Map(sheet);

      Assert.Equal("42", read.Value);
      Assert.Equal(@"TEXT(6*7,""0"")", read.Formula);
    }

    // --- What must NOT compile ------------------------------------------------------------------------
    //
    // Recorded rather than asserted, because a compiler diagnostic is not this library's to keep; the
    // spike's MustNotCompile.cs holds these as real code behind a define. Codes and text below are
    // verbatim from `dotnet build spike/TypedSpacesGauntlet -p:DefineConstants=MUST_NOT_COMPILE`,
    // re-run against this tree when these tests were written.
    //
    //   (k) a scoped declaration applied to a plain grid — the demand is real, and it is checked
    //       where the declaration meets the file:
    //
    //         Projection.Over<ISpreadsheetSpace>().VerticalFlow(v => …).Map(GridSpace.Create(…))
    //         CS1503: Argument 1: cannot convert from 'Unrect.GridSpace'
    //                                              to 'Unrect.Spreadsheets.ISpreadsheetSpace'
    //
    //   (m) a child demanding more than the scope allows — and this is the phase's sharpest finding.
    //       Unscoped, the same mistake is CS0411 on `Next` ("the type arguments cannot be inferred"),
    //       which never says the word formula and points two lines from the fix. In a scope the
    //       cursor's space is already answered, so the refusal lands on the ARGUMENT and both types
    //       are named:
    //
    //         Projection.Over<ISpace>().VerticalFlow(v => v.Next(Formula()))
    //         CS1503: Argument 1: cannot convert from 'IProjection<IFormulaSpace, string?>'
    //                                              to 'IProjection<Unrect.Core.ISpace, string>'
    //
    // The positive halves of both are above: AScopeRaisesEverythingBuiltThroughItToItsOwnSpace
    // composes a narrow helper into a wide scope, which is the direction that DOES compile.
  }
}
