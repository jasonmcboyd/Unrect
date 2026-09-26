using System;
using System.IO;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A declaration file that names its space once and asks its cells about their kind and their
  /// value below that line, with nothing annotated. What is under test is as much that these
  /// <em>compile</em> as what they read: a predicate about a number is unspellable over the
  /// canonical four, and the imports at the top of this file are the whole of what makes it
  /// spellable here.
  /// </summary>
  public class TypedDeclarationTests
  {
    /// <summary>Two rows of small numbers, one of large ones, then a note.</summary>
    private static ICellSpace Lots()
      => Mixed(new object?[,]
      {
        { 1m, 2m },
        { 3m, 4m },
        { 80m, 90m },
        { "note", null },
      });

    [Fact]
    public void ASheetsExtentCanBeDeclaredByWhatItsCellsAreAndWhatTheyHold()
    {
      // Both halves of a typed predicate in one rule: the kind question, which the canonical four
      // cannot ask at all, and the value question, which they could only ask through the rendering.
      var smallLots = Sized(RowsWhileAny(cell => cell.IsDouble() && cell.Decimal() < 7))
        .Of(Range(block => block.Height));

      Assert.Equal(2, smallLots.Map(Lots()));
    }

    [Fact]
    public void AndAMatcherAsksTheSameQuestionToFindWhereASectionStarts()
    {
      // The kind question locating a section rather than sizing one: the first row that holds a
      // number, which is the row below the caption however the caption is worded.
      var sheet = Mixed(new object?[,]
      {
        { "Lots", null },
        { 5m, 6m },
      });

      var body = On(RowWithCell(cell => cell.IsDouble())).Of(Row(row => row[0].Decimal()));

      Assert.Equal(5m, body.Map(sheet));
    }

    /// <summary>
    /// A rule written at the least demanding space there is, for any file at all to use. It asks a
    /// canonical question, so it names <c>ISpace</c> and demands nothing more.
    /// </summary>
    private static IAreaStrategy<ISpace> Populated()
      => ProjectionBuilders<ISpace>.RowsWhileAny(cell => !cell.IsBlank());

    [Fact]
    public void ARuleBuiltAtTheLeastDemandingSpaceFlowsIntoAFileScopedToMore()
    {
      // Contravariance, doing the work a shared helper needs: an IAreaStrategy<ISpace> IS an
      // IAreaStrategy<ICellSpace> as far as Sized is concerned, so the helper composes in as it is
      // — no unwrapping, no cast, nothing annotated at the call site.
      var sheet = Mixed(new object?[,]
      {
        { "Header", null },
        { null, null },
        { "Body", null },
      });

      var header = Sized(Populated()).Row(row => row[0].Text());

      Assert.Equal("Header", header.Map(sheet));
    }

    /// <summary>The matcher and row-rule halves of the same helper, at the same least demanding space.</summary>
    private static IRowLandmark<ISpace> FirstPopulatedRow()
      => ProjectionBuilders<ISpace>.RowWithCell(cell => !cell.IsBlank());

    private static IRowStrategy<ISpace> PopulatedRows()
      => ProjectionBuilders<ISpace>.TakeRowsWhileAny(cell => !cell.IsBlank());

    [Fact]
    public void AndSoDoAMatcherAndAnAxisRuleBuiltTheSameWay()
    {
      // Contravariance holds for the whole family or for none of it: a helper library that had to
      // hand back a matcher over the caller's space would need one copy per backend, which is the
      // duplication the variance annotation exists to prevent. Nothing is annotated below either.
      var sheet = Mixed(new object?[,]
      {
        { null, null },
        { "Body", null },
        { "More", null },
        { null, null },
      });

      Assert.Equal("Body", On(FirstPopulatedRow()).Of(Row(row => row[0].Text())).Map(sheet));
      Assert.Equal(2, On(FirstPopulatedRow()).Column(PopulatedRows(), column => column.Count).Map(sheet));
    }

    [Fact]
    public void AndTheSameDeclarationAsksItsQuestionsOfASheetReadAWindowAtATime()
    {
      // The kind and value questions over the streaming door, where a cell is materialised from a
      // window rather than held in an array. Nothing in the declaration changes — the space named
      // at the top of this file is what the streamed sheet arrives as — and that is what is being
      // said: the typed layer belongs to the vocabulary, so it cannot know which door answered.
      var path = Path.Combine(AppContext.BaseDirectory, "TestData", "multi-sheet.xlsx");

      using var book = Workbook.Open(path, new WorkbookOptions());

      // Captions, then five records whose amounts are 100, 150, 400, 500, 1500. The section is the
      // records under 200: found by the first row holding a number, sized by the amounts.
      var small = On(RowWithCell(cell => cell.IsDouble()))
        .Sized(RowsWhileAny(cell => cell.IsDouble() && cell.Decimal() < 200))
        .Of(Range(block => block.Height));

      Assert.Equal(2, small.Map(book.Sheet("Detail")));
    }
  }
}
