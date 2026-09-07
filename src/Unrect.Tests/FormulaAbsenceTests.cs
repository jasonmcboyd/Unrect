using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Tests.Streaming;

using Xunit;

using static Unrect.Projections.Projection;

namespace Unrect.Tests
{
  /// <summary>
  /// Where formulas cannot be read, and what the reader says instead. The honest-absence rule has
  /// three edges and this is all of them: a format whose formulas are not text, a door that reads
  /// values only, and a package whose parts are not XML.
  /// <para>
  /// The rule under all three: <em>a reader that did not look must not answer as though it had.</em>
  /// Null from <c>FormulaAt</c> means "that cell is a plain value" — a statement about the file — so a
  /// space full of nulls produced by a reader that never read any formulas is a lie about the
  /// document, told one cell at a time and impossible to notice. Every absence here is therefore
  /// either a type that does not carry the capability at all, or a loud failure.
  /// </para>
  /// </summary>
  public class FormulaAbsenceTests
  {
    private static string TestData(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);

    // --- .xls: the formulas are there and they are not text -----------------------------------------

    [Fact]
    public void AskingALegacyWorkbookForItsFormulasFailsAndSaysWhy()
    {
      // A .xls stores a formula as a BIFF RPN token stream, not as an expression, so reading one is
      // a decompiler rather than a missing branch. The refusal names the format, because the caller's
      // next move is to convert the file, and "not supported" alone would not tell them that.
      var refused = Assert.Throws<NotSupportedException>(
        () => SpreadsheetSpace.CreateWithFormulas(TestData("legacy.xls"), "Legacy"));

      Assert.Contains("BIFF", refused.Message, StringComparison.Ordinal);
      Assert.Contains(".xlsx", refused.Message, StringComparison.Ordinal);
      Assert.Contains("legacy.xls", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheEnumeratingDoorRefusesTheSameFileTheSameWay()
    {
      // The sheet-predicate overload builds its spaces lazily, so the refusal arrives when the
      // enumeration is walked rather than when it is described. It is the same refusal: a door that
      // yielded a first sheet and then threw would be worse than one that never started.
      Assert.Throws<NotSupportedException>(
        () => SpreadsheetSpace.CreateWithFormulas(TestData("legacy.xls"), _ => true).ToList());
    }

    [Fact]
    public void TheSameLegacyWorkbookReadsPerfectlyWellWithoutFormulas()
    {
      // The other half of the pin, and the reason the refusal above is narrow rather than a rejection
      // of the format. Nothing about .xls is unreadable; one capability over it is.
      var sheet = SpreadsheetSpace.Create(TestData("legacy.xls"), "Legacy");

      Assert.Equal("Widget", sheet[0, 0].GetString());
      Assert.Equal(4, sheet[1, 0].GetInt());
      Assert.Equal("Gadget", sheet[0, 1].GetString());
      Assert.Equal(6, sheet[1, 1].GetInt());
      Assert.Equal("Total", sheet[0, 2].GetString());

      Assert.Null(sheet.Capability<IFormulaSpace>());
    }

    // --- .xlsb: a package whose parts are binary ----------------------------------------------------

    [Fact]
    public void AWorkbookWhoseWorkbookPartIsBinaryFailsRatherThanReadingNoFormulas()
    {
      // WART, pinned as found. An .xlsb is a zip like an .xlsx, and its relationship parts are XML
      // like an .xlsx's — so the formula reader gets as far as opening the package and following its
      // relationships, and then meets a workbook part that is BIFF12 records rather than markup. What
      // it does at that point is throw XmlException from inside itself: loud, which is what the
      // honest-absence rule demands, but not named, and not the NotSupportedException its .xls
      // sibling above throws for the same class of reason.
      //
      // The pin is deliberately on the LOUDNESS and not on the type. If the reader learns to
      // recognise a .bin workbook part and refuse it by name, this test should keep passing
      // unchanged; the day it starts answering a grid of nulls instead is the day it has begun
      // telling every caller that an .xlsb contains no formulas.
      //
      // The package is built here rather than committed because no tool in this repository writes a
      // real .xlsb, and a committed file that merely LOOKED like one would be a fixture lying about
      // what it is. What is asserted is exactly what is arranged: a well-formed OPC package whose
      // workbook and worksheet parts are binary, which is the one structural fact about an .xlsb that
      // this reader meets.
      var path = BinaryPartWorkbook();

      try
      {
        Assert.ThrowsAny<Exception>(() => SpreadsheetSpace.CreateWithFormulas(path, _ => true).ToList());
      }
      finally
      {
        File.Delete(path);
      }
    }

    /// <summary>
    /// An OPC package shaped like an <c>.xlsb</c>: XML relationships pointing at a workbook part and
    /// a worksheet part that are binary. Returns the path; the caller deletes it.
    /// </summary>
    private static string BinaryPartWorkbook()
    {
      const string ContentTypes =
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"bin\" ContentType=\"application/vnd.ms-excel.sheet.binary.macroEnabled.main\"/>" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "</Types>";

      const string PackageRelationships =
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" " +
        "Target=\"xl/workbook.bin\"/></Relationships>";

      const string WorkbookRelationships =
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" " +
        "Target=\"worksheets/sheet1.bin\"/></Relationships>";

      // A BIFF12 record header and a few bytes of payload: enough to be plainly not markup, which is
      // the only property of the binary parts that matters here.
      var records = new byte[] { 0x85, 0x01, 0x14, 0x00, 0x03, 0x00, 0x00, 0x00, 0x83, 0x01, 0x00 };

      var path = Path.Combine(Path.GetTempPath(), $"unrect-binary-parts-{Guid.NewGuid():N}.xlsb");

      using (var file = File.Create(path))
      using (var package = new ZipArchive(file, ZipArchiveMode.Create))
      {
        Write(package, "[Content_Types].xml", Encoding.UTF8.GetBytes(ContentTypes));
        Write(package, "_rels/.rels", Encoding.UTF8.GetBytes(PackageRelationships));
        Write(package, "xl/_rels/workbook.bin.rels", Encoding.UTF8.GetBytes(WorkbookRelationships));
        Write(package, "xl/workbook.bin", records);
        Write(package, "xl/worksheets/sheet1.bin", records);
      }

      return path;
    }

    private static void Write(ZipArchive package, string part, byte[] content)
    {
      using var entry = package.CreateEntry(part).Open();

      entry.Write(content, 0, content.Length);
    }

    // --- Streaming: a door that reads values only ---------------------------------------------------

    [Fact]
    public void TheStreamingDoorOffersNoFormulaCapabilityAtAll()
    {
      // The streaming reader walks rows for their values and never sees a formula, so its sheets are
      // plain spaces. Absence by TYPE rather than by answer, exactly as the plain eager door is: this
      // is the same decision the second eager factory exists to make, kept by a door that has no
      // second factory to offer.
      using var book = Workbook.Over(FakeRowSource.Of(rows: 8, columns: 3), new WorkbookOptions { WarmReaders = false });

      var sheet = book.Sheet("Data");

      Assert.False(sheet is IFormulaSpace);
      Assert.Null(sheet.Capability<IFormulaSpace>());
      Assert.Null(sheet.GetSubspace(new Offset(1, 2), new Area(2, 4)).Capability<IFormulaSpace>());
    }

    [Fact]
    public void ADiscoveredExtentOverAStreamedSheetInventsNoCapabilityEither()
    {
      // The seam's other direction. Capability<T>() walks charts precisely so a bound whose height is
      // still being discovered does not HIDE a capability the sheet has — and the walk must not
      // manufacture one for a sheet that has none. Over the streaming door it finds nothing to find,
      // through as many wrappers as the engine cares to build.
      using var book = Workbook.Over(FakeRowSource.Of(rows: 8, columns: 3), new WorkbookOptions { WarmReaders = false });

      var probe = Range(RowsWhileAnyValue(), block => block.Space.Capability<IFormulaSpace>());

      Assert.Null(probe.Map(book.Sheet("Data")));
    }
  }
}
