using System;
using System.IO;
using System.IO.Compression;
using System.Text;

using Unrect.Core;
using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// Elapsed-time cells, end to end through the Excel adapter.
  /// <para>
  /// A cell formatted as a duration ([h]:mm:ss and friends) is handed to us by the reader as a
  /// <c>TimeSpan</c>, not a number — the one backend type whose canonical form is not obvious. A
  /// duration is not an instant, so it cannot honestly lex to Temporal; it lexes to a Number of
  /// days, the unit the serial format already uses. Before that was handled, such a cell threw
  /// "Unsupported cell type System.TimeSpan" and took the whole workbook with it.
  /// </para>
  /// <para>
  /// The workbook is written by the test rather than committed, because the thing under test is a
  /// property of the FILE — a style whose numFmtId marks the cell as elapsed time — and in a
  /// binary fixture that property is invisible to a reviewer. Here the format id sits three lines
  /// from the assertion that depends on it.
  /// </para>
  /// </summary>
  public class SpreadsheetSpaceDurationTests : IDisposable
  {
    private readonly string _path =
      Path.Combine(Path.GetTempPath(), $"unrect-durations-{Guid.NewGuid():N}.xlsx");

    public SpreadsheetSpaceDurationTests() => WriteDurationWorkbook(_path);

    public void Dispose()
    {
      if (File.Exists(_path))
        File.Delete(_path);
    }

    private ISpace Durations() => SpreadsheetSpace.Create(_path, "Durations");

    [Fact]
    public void ADurationCellIsANumberOfDays()
    {
      // A2 holds the serial 1.5 under built-in format 46 ([h]:mm:ss) — 36 hours.
      var space = Durations();

      Assert.Equal(CellKind.Number, space[0, 1].Kind);
      Assert.Equal(1.5, space[0, 1].GetDouble());
    }

    [Fact]
    public void ACustomElapsedFormatIsReadTheSameWay()
    {
      // A3 uses a custom [h]:mm rather than a built-in id: the reader decides "this is a duration"
      // from the format code, so both routes must arrive at the same canonical value.
      var space = Durations();

      Assert.Equal(CellKind.Number, space[0, 2].Kind);
      Assert.Equal(0.25, space[0, 2].GetDouble());
    }

    [Fact]
    public void ADurationReadsTheSameAsThePlainNumberBesideIt()
    {
      // B holds the identical serials with no elapsed format. That the two columns agree is the
      // point of choosing days as the unit: the formatting a workbook happens to carry does not
      // change what the cell is worth.
      var space = Durations();

      Assert.Equal(space[1, 1], space[0, 1]);
      Assert.Equal(space[1, 2], space[0, 2]);
    }

    [Fact]
    public void ADurationCellIsNotATemporal()
    {
      // 36 hours is not a moment in time. Lexing it to Temporal would make it a date in January
      // 1900 — a real number that means nothing.
      var space = Durations();

      Assert.Null(space[0, 1].TryGetDate());
      Assert.Throws<InvalidOperationException>(() => space[0, 1].GetDate());
    }

    [Fact]
    public void ADurationCellIsNotBlank()
    {
      // Worth pinning separately: had the adapter fallen back to Blank instead of throwing, a
      // duration would end a region early and the failure would be a silently short table.
      var space = Durations();

      Assert.True(space[0, 1].HasValue);
      Assert.False(space[0, 1].IsBlank);
    }

    // --- The fixture ----------------------------------------------------------------------------

    /// <summary>
    /// The smallest .xlsx that holds an elapsed-time cell: two duration cells (one built-in
    /// format, one custom) beside the same serials with no format at all.
    /// <code>
    ///        0 (A)                  1 (B)
    ///   0    "Elapsed"              "Plain"
    ///   1    1.5   as [h]:mm:ss     1.5
    ///   2    0.25  as [h]:mm        0.25
    /// </code>
    /// </summary>
    private static void WriteDurationWorkbook(string path)
    {
      using var file = File.Create(path);
      using var package = new ZipArchive(file, ZipArchiveMode.Create);

      Add("[Content_Types].xml", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """);

      Add("_rels/.rels", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """);

      Add("xl/workbook.xml", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets><sheet name="Durations" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """);

      Add("xl/_rels/workbook.xml.rels", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """);

      // The whole point of the fixture is here. cellXfs index 0 is General; index 1 is built-in
      // number format 46, [h]:mm:ss; index 2 is a custom [h]:mm. A cell's s="..." selects one, and
      // that is the only thing telling the reader to hand back a TimeSpan.
      Add("xl/styles.xml", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <numFmts count="1"><numFmt numFmtId="164" formatCode="[h]:mm"/></numFmts>
          <fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>
          <fills count="1"><fill><patternFill patternType="none"/></fill></fills>
          <borders count="1"><border/></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="3">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="46" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
            <xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
          </cellXfs>
        </styleSheet>
        """);

      Add("xl/worksheets/sheet1.xml", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <sheetData>
            <row r="1">
              <c r="A1" t="inlineStr"><is><t>Elapsed</t></is></c>
              <c r="B1" t="inlineStr"><is><t>Plain</t></is></c>
            </row>
            <row r="2"><c r="A2" s="1"><v>1.5</v></c><c r="B2"><v>1.5</v></c></row>
            <row r="3"><c r="A3" s="2"><v>0.25</v></c><c r="B3"><v>0.25</v></c></row>
          </sheetData>
        </worksheet>
        """);

      void Add(string name, string xml)
      {
        using var entry = new StreamWriter(
          package.CreateEntry(name, CompressionLevel.Optimal).Open(),
          new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        entry.Write(xml);
      }
    }
  }
}
