using System;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <c>Column(t =&gt; t.Member, 3)</c>: a member bound to a column by position, for the column a
  /// caption cannot reach — one whose header cell is blank, or one of two that say the same thing.
  /// The position is counted from the table's left edge, as <c>row[3]</c> counts it.
  /// </summary>
  public class TableBindingByPositionTests
  {
    public sealed record Transaction(DateTime Date, string Type, decimal Amount);

    public sealed record Transfer(int FromId, string FromCode, int ToId, string ToCode);

    /// <summary>Three columns, the third with no caption over it.</summary>
    private static ICellSpace UnlabeledAmount() => SheetGrid.Of(new object?[,]
    {
      { "Transaction Date", "Transaction Type", null },
      { new DateTime(2026, 1, 5), "Buy", 100m },
      { new DateTime(2026, 1, 6), "Sell", 250m },
    });

    [Fact]
    public void AMemberBindsToAColumnThatHasNoCaption()
    {
      var transactions = Table<Transaction>(bind => bind
        .Column(t => t.Date, "Transaction Date")
        .Column(t => t.Type, "Transaction Type")
        .Column(t => t.Amount, 2));

      Assert.Equal(new[] { 100m, 250m }, transactions.Map(UnlabeledAmount()).Select(t => t.Amount));
    }

    [Fact]
    public void AMemberBoundByPositionIsNotAskedForACaption()
    {
      // The pin the feature rests on. Strictness reports every member that found no caption, and
      // Amount has none to find — the header cell over it is blank and nothing is named "Amount".
      // Bound by position, it is not among the members that look for one.
      var byName = Assert.Throws<ProjectionException>(() =>
        Table<Transaction>(bind => bind
          .Column(t => t.Date, "Transaction Date")
          .Column(t => t.Type, "Transaction Type")).Map(UnlabeledAmount()));

      Assert.Contains("no column binds Transaction.Amount", byName.Message, StringComparison.Ordinal);

      var byPosition = Table<Transaction>(bind => bind
        .Column(t => t.Date, "Transaction Date")
        .Column(t => t.Type, "Transaction Type")
        .Column(t => t.Amount, 2));

      Assert.Equal(2, byPosition.Map(UnlabeledAmount()).Count);
    }

    [Fact]
    public void TwoColumnsThatSayTheSameThingAreToldApartByPosition()
    {
      // The duplicate-caption table, which no reading by name can bind — and the message that
      // refuses it says which position to write.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Id", "Code", "Id", "Code" },
        { 1, "FEP", 2, "FCP" },
      });

      var ambiguous = Assert.Throws<ProjectionException>(() =>
        Table<Transfer>(bind => bind
          .Column(t => t.FromId, "Id")
          .Column(t => t.FromCode, 1)
          .Column(t => t.ToId, 2)
          .Column(t => t.ToCode, 3)).Map(sheet));

      Assert.Contains("Bind it by position with Column(t => t.FromId, 0)", ambiguous.Message, StringComparison.Ordinal);

      var transfers = Table<Transfer>(bind => bind
        .Column(t => t.FromId, 0)
        .Column(t => t.FromCode, 1)
        .Column(t => t.ToId, 2)
        .Column(t => t.ToCode, 3)).Map(sheet);

      Assert.Equal(new Transfer(1, "FEP", 2, "FCP"), Assert.Single(transfers));
    }

    [Fact]
    public void APositionCountsFromTheTablesLeftEdgeAsARowsIndexDoes()
    {
      // An indented table: the blank column in front of the first caption is position 0, exactly as
      // row[0] reads it, so Amount is at 3 and not at 2.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { null, "Transaction Date", "Transaction Type", null },
        { null, new DateTime(2026, 1, 5), "Buy", 100m },
      });

      var transactions = Table<Transaction>(bind => bind
        .Column(t => t.Date, "Transaction Date")
        .Column(t => t.Type, "Transaction Type")
        .Column(t => t.Amount, 3));

      Assert.Equal(100m, Assert.Single(transactions.Map(sheet)).Amount);
    }

    [Fact]
    public void APositionTheTableDoesNotHaveIsAFailureThatCitesTheHeader()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Table<Transaction>(bind => bind
          .Column(t => t.Date, "Transaction Date")
          .Column(t => t.Type, "Transaction Type")
          .Column(t => t.Amount, 7)).Map(UnlabeledAmount()));

      Assert.Contains("Transaction.Amount is bound to column 7, and the table has 3 columns (0 to 2)", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AFailureInAColumnBoundByPositionNamesTheColumn()
    {
      // By its caption where it has one, by its position where it has none: either way the path says
      // which column, as it does for a column bound by name.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Transaction Date", "Transaction Type", null },
        { new DateTime(2026, 1, 5), "Buy", "n/a" },
      });

      var failure = Assert.Throws<ProjectionException>(() =>
        Table<Transaction>(bind => bind
          .Column(t => t.Date, "Transaction Date")
          .Column(t => t.Type, "Transaction Type")
          .Column(t => t.Amount, 2)).Map(sheet));

      Assert.Equal("Table<Transaction>[0] -> column 2", failure.Path);
      Assert.Contains("expected Number at C2, found Text", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ABindingIsRefusedWhereItIsWritten()
    {
      Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() =>
        Table<Transaction>(bind => bind.Column(t => t.Amount, -1))).ParamName);

      // Twice is twice, whichever two ways it was said.
      Assert.Contains("bound twice", Assert.Throws<ArgumentException>(() =>
        Table<Transaction>(bind => bind.Column(t => t.Amount, 2).Column(t => t.Amount, "Amount"))).Message, StringComparison.Ordinal);
      Assert.Contains("bound twice", Assert.Throws<ArgumentException>(() =>
        Table<Transaction>(bind => bind.Column(t => t.Amount, "Amount").Column(t => t.Amount, 2))).Message, StringComparison.Ordinal);

      Assert.Contains("both bound and ignored", Assert.Throws<ArgumentException>(() =>
        Table<Transaction>(bind => bind.Column(t => t.Amount, 2).Ignore(t => t.Amount))).Message, StringComparison.Ordinal);
    }
  }
}
