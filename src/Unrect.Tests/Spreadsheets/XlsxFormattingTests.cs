using System;
using System.IO;

using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// What a cell looks like, read from the workbook package: the style a cell names, and the font
  /// and fill that style resolves to. <c>formatting.xlsx</c> is five rows by three columns, written
  /// by hand so that every way a workbook states a colour is in it once.
  /// </summary>
  public class XlsxFormattingTests
  {
    private static string Workbook => Path.Combine(AppContext.BaseDirectory, "TestData", "formatting.xlsx");

    private static (XlsxFormatting Formats, int[,] Styles) Open()
    {
      var formats = XlsxFormatting.Open(Workbook);

      return (formats, formats.ReadSheet("Ledger", 0, width: 3, height: 5));
    }

    [Fact]
    public void TheValueReaderAndTheFormattingAddressTheSameCells()
    {
      // What lets a style read out of the package be indexed by a space's coordinates: both count
      // from the sheet's own A1. The red row is the one whose account the value reader calls 1001.
      var sheet = SpreadsheetSpace.Create(Workbook, "Ledger");
      var (formats, styles) = Open();

      using (formats)
      {
        Assert.Equal(3, sheet.Area.Width);
        Assert.Equal(5, sheet.Area.Height);
        Assert.Equal("1001", sheet.AsText(0, 2));
        Assert.Equal(CellColor.Red, formats.FontOf(styles[2, 0]).Color);
      }
    }

    [Fact]
    public void ARowSetInRedSaysSoOnEveryCell()
    {
      // The case this exists for: a record somebody marked by colouring its row.
      var (formats, styles) = Open();

      using (formats)
      {
        for (var column = 0; column < 3; column++)
        {
          Assert.Equal(CellColor.Red, formats.FontOf(styles[2, column]).Color);
          Assert.True(formats.FontOf(styles[1, column]).Color.IsAutomatic, "the row above is not coloured");
        }
      }
    }

    [Fact]
    public void AColourIsTheSameColourHoweverTheFileSaidIt()
    {
      // C4 says red as index 10 of the legacy palette; A3 says it as FFFF0000. One colour.
      var (formats, styles) = Open();

      using (formats)
      {
        Assert.Equal(CellColor.Red, formats.FontOf(styles[3, 2]).Color);
        Assert.Equal(0xFF0000, formats.FontOf(styles[3, 2]).Color.Rgb);
      }
    }

    [Fact]
    public void AThemeColourIsReportedAsOneRatherThanGuessedAt()
    {
      var (formats, styles) = Open();

      using (formats)
      {
        var color = formats.FontOf(styles[3, 1]).Color;

        Assert.Equal(4, color.Theme);
        Assert.Null(color.Rgb);
        Assert.Equal(0.4, color.Tint, 3);
        Assert.NotEqual(CellColor.Red, color);
        Assert.Equal("theme 4 (tint 0.4)", color.ToString());
      }
    }

    [Fact]
    public void TheMarksAreReadAndAnExplicitNoIsNo()
    {
      var (formats, styles) = Open();

      using (formats)
      {
        Assert.True(formats.FontOf(styles[0, 0]).Bold, "the header row is bold");

        var struck = formats.FontOf(styles[4, 0]);
        Assert.True(struck.Strikethrough);
        Assert.True(struck.Italic);
        Assert.False(struck.Bold);

        // <b val="0"/> is a font that says it is NOT bold, which is not the same as saying nothing
        // and must not be read as the presence of the element.
        Assert.False(formats.FontOf(styles[4, 1]).Bold);
      }
    }

    [Fact]
    public void AFillIsItsPatternsForegroundAndNoPatternIsNoFill()
    {
      var (formats, styles) = Open();

      using (formats)
      {
        Assert.Equal(CellColor.FromRgb(0xFFFF00), formats.FillOf(styles[4, 2]).Color);
        Assert.True(formats.FillOf(styles[1, 0]).Color.IsAutomatic);
      }
    }

    [Fact]
    public void AStyleTheWorkbookDoesNotDefineIsTheDefault()
    {
      var (formats, _) = Open();

      using (formats)
      {
        Assert.True(formats.FontOf(99).Color.IsAutomatic);
        Assert.True(formats.FillOf(-1).Color.IsAutomatic);
      }
    }

    [Fact]
    public void AFileThatIsNotAnXlsxIsRefusedByName()
    {
      var notAWorkbook = Path.Combine(AppContext.BaseDirectory, "Unrect.Tests.dll");

      var refusal = Assert.Throws<NotSupportedException>(() => XlsxFormatting.Open(notAWorkbook));

      Assert.Contains("Formatting can only be read from .xlsx files", refusal.Message, StringComparison.Ordinal);
    }
  }
}
