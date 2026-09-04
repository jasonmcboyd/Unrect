using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

using Unrect.Core;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// A real <c>.xlsx</c> file for the retention family's eager rows, generated into the temp directory
  /// at setup and never committed.
  ///
  /// <para><b>Why the rig's no-workbook rule bends here, and only here.</b> Every other family measures
  /// a layer we control, so a synthetic fixture measures the same thing a file would. Retention does
  /// not: the change it exists to judge lives IN THE ADAPTERS — at <c>SheetStore</c>'s chunk fill for
  /// the streaming door and in <c>SpreadsheetSpace.Create</c>'s fill for the eager one. A
  /// <c>GridSpace</c> built from cells this assembly made bypasses the eager adapter entirely, so
  /// those rows would read FLAT under the very change they are the floor for. A floor that cannot move
  /// is not a floor. The streaming rows need no file for the same reason in reverse: every
  /// <c>IRowSource</c> passes through the store's fill, so a synthetic source exercises the real seam
  /// (and is far faster).</para>
  ///
  /// <para><b>Two encodings, because how a file spells its text decides whether the eager door
  /// duplicates at all.</b> Measured, not assumed (ExcelDataReader 3.7):</para>
  /// <list type="bullet">
  ///   <item><b>Shared strings</b> (<c>t="s"</c>) — the reader hands back the table's own instance, so
  ///     equal cells already SHARE. A fixture of these measured 11,272 distinct values held as exactly
  ///     11,272 instances: nothing left for an interner to remove.</item>
  ///   <item><b>Inline strings</b> (<c>t="inlineStr"</c>) — a fresh instance per cell, every time. The
  ///     committed example corpus is written this way (<c>investors-by-deal.xlsx</c>: 61 text cells, 30
  ///     distinct values, 61 distinct instances), as is anything an exporter streams out.</item>
  /// </list>
  /// <para><b>A real Excel export is BOTH</b>, which is the finding that shaped this fixture. The local
  /// scrubbed K-1 — an actual fund workbook — holds 9,049 text cells over 2,876 distinct values in
  /// 4,016 instances: its 8,572 shared-string cells collapse to the table's 2,873 entries, while its
  /// 3,731 formula-result cells (<c>t="str"</c>) materialise fresh per cell exactly as an inline string
  /// does. So the eager door's duplication is real but partial, and its size is a property of how
  /// formula-heavy the sheet is.</para>
  ///
  /// <para><b>The family therefore brackets it rather than picking a side.</b> The inline fixture is the
  /// floor's primary case — one instance per cell, the most an interner can ever remove — and the shared
  /// fixture is charted beside it as the other end, where the reader has already done the job. That
  /// second row is the better control of the two: it must stay flat under interning, and its value today
  /// is approximately where the inline row should land afterwards. The target is on the same chart as
  /// the floor.</para>
  ///
  /// <para><b>No writer dependency.</b> This is a hand-rolled minimum OOXML package — five small parts
  /// and a sheet — rather than a package reference, because the only thing the rig needs to write is
  /// this one shape, and a spreadsheet-writing library in the benchmark project's graph is a
  /// dependency the library itself does not have.</para>
  /// </summary>
  internal static class RetentionWorkbooks
  {
    /// <summary>
    /// Bumped when the generated content changes, so a cached file from an older shape is never read
    /// as if it were this one. The cache is what keeps a local edit-measure loop tolerable; on a CI
    /// runner it is always a miss.
    /// </summary>
    private const string Version = "v1";

    /// <summary>The one sheet these workbooks carry, by the name the eager scenarios ask for.</summary>
    public const string SheetName = "Data";

    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    /// <summary>
    /// The workbook for a flavour, generated if it is not already in the temp directory. Written to a
    /// scratch name and moved into place, so an interrupted run leaves no half-written file for the
    /// next one to read as a cache hit.
    /// </summary>
    public static string Path(bool unique, bool sharedStrings, int rows, int columns)
    {
      var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unrect-retention");

      Directory.CreateDirectory(directory);

      var path = System.IO.Path.Combine(
        directory,
        FormattableString.Invariant(
          $"ledger-{(sharedStrings ? "shared" : "inline")}-{(unique ? "unique" : "duplicated")}-{rows}x{columns}-{Version}.xlsx"));

      if (File.Exists(path))
        return path;

      var scratch = path + ".partial-" + Guid.NewGuid().ToString("N");

      Write(scratch, unique, sharedStrings, rows, columns);

      try
      {
        File.Move(scratch, path);
      }
      catch (IOException) when (File.Exists(path))
      {
        // Another process won the race and wrote the same bytes; take theirs and drop ours.
        File.Delete(scratch);
      }

      return path;
    }

    private static void Write(string path, bool unique, bool sharedStrings, int rows, int columns)
    {
      using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20);
      using var package = new ZipArchive(file, ZipArchiveMode.Create);

      // The sheet is written first and builds the string table as it goes, so the cells are generated
      // once rather than once to collect strings and again to place them. Parts may be stored in any
      // order; nothing reads a package sequentially.
      var strings = sharedStrings ? new StringTable() : null;

      Part(package, "xl/worksheets/sheet1.xml", writer => Sheet(writer, strings, unique, rows, columns));

      if (strings != null)
        Part(package, "xl/sharedStrings.xml", writer => SharedStrings(writer, strings));

      Part(package, "[Content_Types].xml", writer => writer.Write(
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        $"<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        $"<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        $"<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        $"<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        $"<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        (sharedStrings
          ? $"<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>"
          : string.Empty) +
        $"</Types>"));

      Part(package, "_rels/.rels", writer => writer.Write(
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        $"<Relationships xmlns=\"{PackageRelationships}\">" +
        $"<Relationship Id=\"rId1\" Type=\"{OfficeRelationships}/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        $"</Relationships>"));

      Part(package, "xl/workbook.xml", writer => writer.Write(
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        $"<workbook xmlns=\"{Main}\" xmlns:r=\"{OfficeRelationships}\">" +
        $"<sheets><sheet name=\"{SheetName}\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
        $"</workbook>"));

      Part(package, "xl/_rels/workbook.xml.rels", writer => writer.Write(
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        $"<Relationships xmlns=\"{PackageRelationships}\">" +
        $"<Relationship Id=\"rId1\" Type=\"{OfficeRelationships}/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        (sharedStrings
          ? $"<Relationship Id=\"rId2\" Type=\"{OfficeRelationships}/sharedStrings\" Target=\"sharedStrings.xml\"/>"
          : string.Empty) +
        $"</Relationships>"));
    }

    /// <summary>
    /// The sheet. The <c>dimension</c> element is not decoration: it is what makes the reader report a
    /// row count, which is what sends <c>SpreadsheetSpace.Create</c> down its declared-size fill rather
    /// than the measuring one — the path a real sized export takes.
    /// </summary>
    private static void Sheet(TextWriter writer, StringTable? strings, bool unique, int rows, int columns)
    {
      writer.Write($"<?xml version=\"1.0\" encoding=\"utf-8\"?><worksheet xmlns=\"{Main}\">");
      writer.Write(FormattableString.Invariant($"<dimension ref=\"A1:{Reference(columns - 1, rows - 1)}\"/><sheetData>"));

      for (var row = 0; row < rows; row++)
      {
        writer.Write(FormattableString.Invariant($"<row r=\"{row + 1}\">"));

        for (var column = 0; column < columns; column++)
        {
          var cell = RetentionSpaces.Cell(column, row, unique);
          var reference = Reference(column, row);

          switch (cell.Kind)
          {
            case CellKind.Text when strings != null:
              writer.Write(FormattableString.Invariant(
                $"<c r=\"{reference}\" t=\"s\"><v>{strings.Index(cell.GetString())}</v></c>"));
              break;

            case CellKind.Text:
              writer.Write(FormattableString.Invariant($"<c r=\"{reference}\" t=\"inlineStr\"><is><t>"));
              Escaped(writer, cell.GetString());
              writer.Write("</t></is></c>");
              break;

            case CellKind.Number:
              writer.Write(FormattableString.Invariant(
                $"<c r=\"{reference}\"><v>{cell.GetDouble().ToString("R", CultureInfo.InvariantCulture)}</v></c>"));
              break;

            case CellKind.Boolean:
              writer.Write(FormattableString.Invariant(
                $"<c r=\"{reference}\" t=\"b\"><v>{(cell.GetBoolean() ? 1 : 0)}</v></c>"));
              break;

            case CellKind.Blank:
              break;

            default:
              throw new InvalidOperationException($"The fixture writer has no encoding for {cell.Kind}.");
          }
        }

        writer.Write("</row>");
      }

      writer.Write("</sheetData></worksheet>");
    }

    private static void SharedStrings(TextWriter writer, StringTable strings)
    {
      writer.Write(FormattableString.Invariant(
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><sst xmlns=\"{Main}\" count=\"{strings.Values.Count}\" uniqueCount=\"{strings.Values.Count}\">"));

      foreach (var value in strings.Values)
      {
        writer.Write("<si><t>");
        Escaped(writer, value);
        writer.Write("</t></si>");
      }

      writer.Write("</sst>");
    }

    private static void Part(ZipArchive package, string name, Action<TextWriter> write)
    {
      // Fastest, not Optimal: these parts are tens of megabytes of highly repetitive XML that will be
      // read once and deleted, and the difference is a minute of CPU against a few megabytes of temp.
      using var stream = package.CreateEntry(name, CompressionLevel.Fastest).Open();
      using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1 << 20);

      write(writer);
    }

    /// <summary>An A1 reference. Two letters is enough for anything this fixture will ever be.</summary>
    private static string Reference(int column, int row)
    {
      var letters = column < 26
        ? ((char)('A' + column)).ToString()
        : new string(new[] { (char)('A' + column / 26 - 1), (char)('A' + column % 26) });

      return letters + (row + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static void Escaped(TextWriter writer, string value)
    {
      foreach (var character in value)
        switch (character)
        {
          case '&': writer.Write("&amp;"); break;
          case '<': writer.Write("&lt;"); break;
          case '>': writer.Write("&gt;"); break;
          default: writer.Write(character); break;
        }
    }

    /// <summary>
    /// The shared-string table as it is discovered, cell by cell. On the unique flavour this holds
    /// every string in the sheet at once — which is exactly what a unique-heavy real export's table is,
    /// and which is transient: generation finishes before the first baseline reading is taken.
    /// </summary>
    private sealed class StringTable
    {
      private readonly Dictionary<string, int> _indices = new Dictionary<string, int>(StringComparer.Ordinal);

      internal List<string> Values { get; } = new List<string>();

      internal int Index(string value)
      {
        if (_indices.TryGetValue(value, out var index))
          return index;

        index = Values.Count;

        Values.Add(value);
        _indices.Add(value, index);

        return index;
      }
    }
  }
}
