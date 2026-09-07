using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The formula half of an xlsx, read from the file's own bytes.
  /// <para>
  /// It exists because ExcelDataReader does not expose formulas: its xlsx reader parses the
  /// <c>&lt;f&gt;</c> element and drops it, and there is no public seam to reach the text through
  /// (verified by reflection probe over 3.7.0). So the values come from the reader, the formulas
  /// come from here, and the two are laid over each other in one grid.
  /// </para>
  /// <para>
  /// <b>What lines them up.</b> Both address the sheet in its own absolute coordinates: the value
  /// reader emits a row per sheet row including the empty ones it never saw, so grid row 0 is sheet
  /// row 1 and grid column 0 is column A, whether or not the sheet declares a dimension and whether
  /// or not it starts at A1. That is pinned by the eager door's own tests, and it is the only
  /// reason a formula read out of the zip can be indexed by the coordinates a space uses.
  /// </para>
  /// <para>
  /// The grid is clipped to the extent the value reader measured. A formula outside it addresses a
  /// cell the space does not have, and there is nowhere to put it.
  /// </para>
  /// </summary>
  internal sealed class XlsxFormulas : IDisposable
  {
    private const string OfficeDocument = "/officeDocument";
    private const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private readonly ZipArchive _archive;
    private readonly List<KeyValuePair<string, string>> _sheets;

    private XlsxFormulas(ZipArchive archive, List<KeyValuePair<string, string>> sheets)
    {
      _archive = archive;
      _sheets = sheets;
    }

    /// <summary>
    /// The workbook at <paramref name="path"/>, opened for its formulas: the zip, plus the sheet
    /// name to sheet part map its workbook part declares.
    /// </summary>
    /// <exception cref="NotSupportedException">The file is not an xlsx.</exception>
    internal static XlsxFormulas Open(string path)
    {
      // The same share flags the value reader opens with: the workbook may be open in Excel, and a
      // save there replaces the file rather than writing into it.
      var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      ZipArchive archive;

      try
      {
        archive = new ZipArchive(stream, ZipArchiveMode.Read);
      }
      catch (InvalidDataException)
      {
        stream.Dispose();

        // A .xls is a compound binary file, not a zip, and its formulas are BIFF RPN token streams
        // rather than text — a different reader and a different decompiler, not a missing branch.
        throw new NotSupportedException(
          $"Formulas can only be read from .xlsx files, and '{path}' is not one. " +
          "A .xls stores formulas as BIFF token streams, which this package does not decompile; " +
          "read it without formulas, or convert it.");
      }

      try
      {
        return new XlsxFormulas(archive, ReadSheetMap(archive));
      }
      catch
      {
        archive.Dispose();
        throw;
      }
    }

    public void Dispose() => _archive.Dispose();

    /// <summary>
    /// The formulas of one sheet, as a grid of the same shape as the value grid — null wherever the
    /// cell is a plain value.
    /// </summary>
    /// <param name="sheetName">The sheet's name, as the value reader reported it.</param>
    /// <param name="sheetIndex">Its position in the workbook, used when the name does not resolve.</param>
    /// <param name="width">The value grid's width.</param>
    /// <param name="height">The value grid's height.</param>
    internal string?[,] ReadSheet(string sheetName, int sheetIndex, int width, int height)
    {
      var formulas = new string?[height, width];
      var part = _archive.GetEntry(PartOf(sheetName, sheetIndex));

      if (part is null)
        return formulas;

      var masters = new Dictionary<int, Shared>();
      var followers = new List<Shared>();

      using (var content = part.Open())
      using (var reader = XmlReader.Create(content, Settings()))
        Scan(reader, formulas, masters, followers);

      foreach (var follower in followers)
      {
        if (!masters.TryGetValue(follower.Index, out var master))
          throw new InvalidDataException(
            $"The cell at column {follower.Column + 1}, row {follower.Row + 1} of '{sheetName}' shares formula {follower.Index}, " +
            "and the sheet does not define it. The file is malformed: a shared formula's text lives on its master cell, " +
            "so there is no formula to report for this one.");

        formulas[follower.Row, follower.Column] =
          SharedFormulas.Shift(master.Text!, follower.Column - master.Column, follower.Row - master.Row);
      }

      return formulas;
    }

    /// <summary>
    /// One pass over the sheet, filling in every formula whose text is written where it is used and
    /// collecting the shared groups, whose followers cannot be resolved until their master has been
    /// seen. Nothing here assumes the file writes a master before its followers.
    /// </summary>
    private static void Scan(XmlReader reader, string?[,] formulas, Dictionary<int, Shared> masters, List<Shared> followers)
    {
      var height = formulas.GetLength(0);
      var width = formulas.GetLength(1);
      var row = -1;
      var column = -1;

      while (reader.Read())
      {
        if (reader.NodeType != XmlNodeType.Element)
          continue;

        switch (reader.LocalName)
        {
          // The r attributes are optional in the format. Where a writer omits them, position is
          // where the reading has got to, which is what these two counters are.
          case "row":
            row = Index(reader.GetAttribute("r"), row);
            column = -1;
            break;

          case "c":
            if (A1Reference.TryParse(reader.GetAttribute("r") ?? string.Empty, out var at, out var atRow))
            {
              column = at;
              row = atRow;
            }
            else
            {
              column++;
            }

            break;

          case "f":
            var kind = reader.GetAttribute("t");
            var index = reader.GetAttribute("si");
            var text = reader.IsEmptyElement ? null : reader.ReadElementContentAsString();
            var inside = row >= 0 && row < height && column >= 0 && column < width;

            // A follower carries its group's index and nothing else; a master carries the text and
            // the range it covers. Everything else — an ordinary formula, an array anchor — is
            // written where it is used.
            var written = !string.IsNullOrEmpty(text);

            if (kind == "shared" && int.TryParse(index, out var group))
            {
              var shared = new Shared(group, column, row, text);

              if (!written)
              {
                if (inside)
                  followers.Add(shared);
              }
              else
              {
                masters[group] = shared;

                if (inside)
                  formulas[row, column] = text;
              }
            }
            else if (inside && written)
            {
              formulas[row, column] = text;
            }

            break;
        }
      }
    }

    /// <summary>The part holding <paramref name="sheetName"/>, by name and then by position.</summary>
    private string PartOf(string sheetName, int sheetIndex)
    {
      foreach (var sheet in _sheets)
        if (string.Equals(sheet.Key, sheetName, StringComparison.Ordinal))
          return sheet.Value;

      return sheetIndex >= 0 && sheetIndex < _sheets.Count ? _sheets[sheetIndex].Value : string.Empty;
    }

    /// <summary>
    /// The workbook's sheets in the order it declares them, each paired with the part that holds
    /// it. Found by following the package's own relationships rather than by assuming
    /// <c>xl/workbook.xml</c>: the layout is a convention of one writer, not a rule of the format.
    /// </summary>
    private static List<KeyValuePair<string, string>> ReadSheetMap(ZipArchive archive)
    {
      var workbookPart = WorkbookPart(archive)
        ?? throw new InvalidDataException("The file declares no workbook part; it is not a readable xlsx.");

      // The refusal is named here rather than left to the XmlException the binary bytes would
      // produce two calls later: the caller did nothing malformed, they asked the wrong format.
      if (workbookPart.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
        throw new NotSupportedException(
          "Formulas can only be read from .xlsx files, and this workbook's parts are binary (.xlsb). " +
          "An .xlsb stores its workbook and sheets as BIFF12 records rather than XML; read it without formulas, or convert it.");

      var targets = new Dictionary<string, string>(StringComparer.Ordinal);
      var folder = FolderOf(workbookPart);

      foreach (var relationship in ReadRelationships(archive, $"{folder}_rels/{NameOf(workbookPart)}.rels"))
        targets[relationship.Id] = Resolve(folder, relationship.Target);

      var sheets = new List<KeyValuePair<string, string>>();
      var entry = archive.GetEntry(workbookPart);

      if (entry is null)
        return sheets;

      using var content = entry.Open();
      using var reader = XmlReader.Create(content, Settings());

      while (reader.Read())
      {
        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "sheet")
          continue;

        var name = reader.GetAttribute("name");
        var id = reader.GetAttribute("id", Relationships);

        if (name is not null && id is not null && targets.TryGetValue(id, out var target))
          sheets.Add(new KeyValuePair<string, string>(name, target));
      }

      return sheets;
    }

    private static string? WorkbookPart(ZipArchive archive)
    {
      foreach (var relationship in ReadRelationships(archive, "_rels/.rels"))
        if (relationship.Type.EndsWith(OfficeDocument, StringComparison.Ordinal))
          return Resolve(string.Empty, relationship.Target);

      return null;
    }

    private static IEnumerable<Relationship> ReadRelationships(ZipArchive archive, string part)
    {
      var entry = archive.GetEntry(part);

      if (entry is null)
        yield break;

      using var content = entry.Open();
      using var reader = XmlReader.Create(content, Settings());

      while (reader.Read())
      {
        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "Relationship")
          continue;

        var id = reader.GetAttribute("Id");
        var type = reader.GetAttribute("Type");
        var target = reader.GetAttribute("Target");

        if (id is not null && type is not null && target is not null)
          yield return new Relationship(id, type, target);
      }
    }

    /// <summary>A relationship target as a part name: absolute as written, relative to its source's folder.</summary>
    private static string Resolve(string folder, string target)
    {
      if (target.StartsWith("/", StringComparison.Ordinal))
        return target.Substring(1);

      var resolved = folder + target;

      // "../" appears in targets written by tooling that keeps sheets outside xl/.
      for (var up = resolved.IndexOf("../", StringComparison.Ordinal); up > 0; up = resolved.IndexOf("../", StringComparison.Ordinal))
      {
        var parent = resolved.LastIndexOf('/', up - 2);

        resolved = resolved.Substring(0, parent + 1) + resolved.Substring(up + 3);
      }

      return resolved;
    }

    private static string FolderOf(string part)
    {
      var slash = part.LastIndexOf('/');

      return slash < 0 ? string.Empty : part.Substring(0, slash + 1);
    }

    private static string NameOf(string part) => part.Substring(FolderOf(part).Length);

    private static int Index(string? reference, int previous)
      => int.TryParse(reference, out var number) && number > 0 ? number - 1 : previous + 1;

    // XmlResolver and DtdProcessing: a workbook is untrusted input, and neither an external entity
    // nor a DTD has any business in an xlsx part.
    private static XmlReaderSettings Settings()
      => new XmlReaderSettings
      {
        IgnoreComments = true,
        IgnoreWhitespace = true,
        IgnoreProcessingInstructions = true,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
      };

    private readonly struct Relationship
    {
      internal Relationship(string id, string type, string target)
      {
        Id = id;
        Type = type;
        Target = target;
      }

      internal string Id { get; }

      internal string Type { get; }

      internal string Target { get; }
    }

    /// <summary>One cell of a shared group: the master carries the text, a follower carries null.</summary>
    private readonly struct Shared
    {
      internal Shared(int index, int column, int row, string? text)
      {
        Index = index;
        Column = column;
        Row = row;
        Text = text;
      }

      internal int Index { get; }

      internal int Column { get; }

      internal int Row { get; }

      internal string? Text { get; }
    }
  }
}
