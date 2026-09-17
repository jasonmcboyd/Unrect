using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

namespace Unrect.Interactive
{
  /// <summary>
  /// A first record declaration, read off a sheet and handed back as C# source to paste.
  /// <para>
  /// Exploratory, like everything in <c>Unrect.Interactive</c>: nothing in the library calls it, and
  /// what it returns is text, never a projection. It exists for the first five minutes with an
  /// unfamiliar file, where the question is what the columns even are.
  /// </para>
  /// <para>
  /// <b>It is a starting point, not a truth.</b> Every type it writes is a guess from a handful of
  /// rows: a column of whole numbers in the sample reads as <c>int</c> however wide the file's
  /// numbers get further down, a column that happens not to be blank in the first five rows reads as
  /// non-nullable, and a column whose caption needed characters removed to make an identifier will
  /// not bind at all until it is given an explicit caption. Read what it writes, then edit it.
  /// </para>
  /// </summary>
  public static class Scaffolding
  {
    /// <summary>
    /// The C# source for a record matching <paramref name="sheet"/>'s header row, plus the
    /// declaration that would bind it — two lines, ready to paste:
    /// <code>
    /// public sealed record Transaction(DateTime Date, string Description, decimal Amount);
    /// var transaction = Table&lt;Transaction&gt;();
    /// </code>
    /// The second line reads as written wherever the file has already imported a sheet vocabulary
    /// (<c>using static Unrect.Spreadsheets.SheetProjectionBuilders&lt;ISheetCells&gt;;</c>).
    /// <para>
    /// <b>The columns</b> are the text cells of row <paramref name="headerRow"/>; a blank cell, and a
    /// header that is not text, are skipped rather than named. A caption becomes a member name by
    /// dropping everything that is not a letter, digit or underscore and capitalising each run that
    /// survives, which is what <see cref="CaptionComparer"/> binds back to — so <c>"Contribution
    /// ITD"</c> becomes <c>ContributionITD</c> and binds with nothing declared, while <c>"Net
    /// (USD)"</c> becomes <c>NetUSD</c> and does not, because the parentheses that were dropped are
    /// characters the comparer counts. A name that would start with a digit is prefixed with
    /// <c>_</c>, a caption with no usable characters becomes <c>Column</c> and its 1-based position
    /// among the columns named, and a name already taken gains a number.
    /// </para>
    /// <para>
    /// <b>The types</b> are read from up to <paramref name="sampleRows"/> rows beneath the header: a
    /// column whose samples are all numbers is <c>decimal</c>, or <c>int</c> where every one of them
    /// is whole and fits one; all dates is <c>DateTime</c>, all booleans <c>bool</c>, and anything
    /// else — text, or a mix — is <c>string</c>. A blank sample, or an error cell (which is a
    /// condition rather than a value, so it is read as one), makes the member nullable; a column with
    /// no samples at all is <c>string?</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The sheet being read.</typeparam>
    /// <param name="sheet">The sheet to read a header row and some samples from.</param>
    /// <param name="typeName">The record's name; the binding line camel-cases it for the variable.</param>
    /// <param name="headerRow">The 0-based row the captions are on.</param>
    /// <param name="sampleRows">How many rows beneath the header to type the columns from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sheet"/> or <paramref name="typeName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="typeName"/> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="headerRow"/> or <paramref name="sampleRows"/> is negative.</exception>
    /// <exception cref="OutOfBoundsException"><paramref name="headerRow"/> is past the end of the sheet.</exception>
    public static string Scaffold<TSpace>(this TSpace sheet, string typeName, int headerRow = 0, int sampleRows = 5)
      where TSpace : class, ISheetCells
    {
      if (sheet is null)
        throw new ArgumentNullException(nameof(sheet));

      if (typeName is null)
        throw new ArgumentNullException(nameof(typeName));

      if (typeName.Trim().Length == 0)
        throw new ArgumentException("A scaffolded record needs a name.", nameof(typeName));

      if (headerRow < 0)
        throw new ArgumentOutOfRangeException(nameof(headerRow));

      if (sampleRows < 0)
        throw new ArgumentOutOfRangeException(nameof(sampleRows));

      // Checked here rather than left to the first cell read: a sheet with no columns reads no
      // cell at all, and would otherwise scaffold an empty record from a row that does not exist.
      if (headerRow >= sheet.Area.Height)
        throw new OutOfBoundsException();

      var captions = Captions(sheet, headerRow);
      var taken = new HashSet<string>(CaptionComparer.Default);
      var parameters = new List<string>(captions.Count);

      for (var index = 0; index < captions.Count; index++)
      {
        var (column, caption) = captions[index];
        var member = Distinct(Identifier(caption, index), taken);

        parameters.Add($"{Type(sheet, column, headerRow, sampleRows)} {member}");
      }

      return $"public sealed record {typeName}({string.Join(", ", parameters)});"
        + Environment.NewLine
        + $"var {Camel(typeName)} = Table<{typeName}>();";
    }

    /// <summary>The header row's text cells, with the columns they sit in.</summary>
    private static List<(int Column, string Caption)> Captions(ISheetCells sheet, int headerRow)
    {
      var captions = new List<(int, string)>();
      var width = sheet.Area.Width;

      for (var column = 0; column < width; column++)
        if (sheet.TextAt(column, headerRow, out var caption, out _) && caption.Trim().Length > 0)
          captions.Add((column, caption));

      return captions;
    }

    /// <summary>
    /// The C# type the sample cells argue for: what each of them reads as, folded together until
    /// they either agree or stop agreeing. A column is typed by all of its samples rather than by
    /// its first, because "all of them agree" is the only claim a handful of rows can support.
    /// </summary>
    private static string Type(ISheetCells sheet, int column, int headerRow, int sampleRows)
    {
      string? agreed = null;
      var blanks = false;
      var height = sheet.Area.Height;

      for (var offset = 1; offset <= sampleRows && headerRow + offset < height; offset++)
      {
        var row = headerRow + offset;

        // An error is a condition, not a value — the same cell a nullable member tolerates.
        if (sheet.IsBlank(column, row) || sheet.IsErrorAt(column, row))
        {
          blanks = true;
          continue;
        }

        var sample = Reads(sheet, column, row);

        agreed = agreed is null ? sample : Agree(agreed, sample);
      }

      // No samples at all is the same answer as samples that agreed on nothing, except that it is
      // also a reason to be nullable: nothing was seen, so nothing rules a blank out.
      return agreed is null ? "string?" : blanks ? agreed + "?" : agreed;
    }

    /// <summary>The type one cell reads as, narrowest first — a whole number is an <c>int</c> until another sample says otherwise.</summary>
    private static string Reads(ISheetCells sheet, int column, int row)
      => sheet.IntegerAt(column, row, out _, out _) ? "int"
        : sheet.DoubleAt(column, row, out _, out _) ? "decimal"
        : sheet.DateTimeAt(column, row, out _, out _) ? "DateTime"
        : sheet.BooleanAt(column, row, out _, out _) ? "bool"
        : "string";

    /// <summary>
    /// The type that covers both samples. Only one pair genuinely meets: a whole number and a
    /// fractional one are both numbers, so they widen to <c>decimal</c> rather than disagreeing.
    /// Everything else that differs is a column this cannot type, which is <c>string</c> — what
    /// every cell can say.
    /// </summary>
    private static string Agree(string first, string second)
      => first == second ? first
        : (first == "int" || first == "decimal") && (second == "int" || second == "decimal") ? "decimal"
        : "string";

    /// <summary>
    /// <paramref name="caption"/> as a C# identifier: every run of identifier characters
    /// capitalised, everything else dropped. What survives is spelled as the file spelled it, so an
    /// acronym stays one.
    /// </summary>
    private static string Identifier(string caption, int index)
    {
      var identifier = new StringBuilder(caption.Length);
      var starting = true;

      foreach (var character in caption)
      {
        if (!char.IsLetterOrDigit(character) && character != '_')
        {
          starting = true;
          continue;
        }

        identifier.Append(starting ? char.ToUpperInvariant(character) : character);
        starting = false;
      }

      if (identifier.Length == 0)
        return "Column" + (index + 1).ToString(CultureInfo.InvariantCulture);

      return char.IsDigit(identifier[0])
        ? "_" + identifier.ToString()
        : identifier.ToString();
    }

    /// <summary>
    /// <paramref name="name"/>, numbered until nothing else has it. Numbered under
    /// <see cref="CaptionComparer"/> rather than by string equality, because two members differing
    /// only in case or spacing would bind to the same column — which is the collision worth avoiding,
    /// not the one the compiler would have caught.
    /// </summary>
    private static string Distinct(string name, HashSet<string> taken)
    {
      var candidate = name;

      for (var suffix = 2; taken.Contains(candidate); suffix++)
        candidate = name + suffix.ToString(CultureInfo.InvariantCulture);

      taken.Add(candidate);

      return candidate;
    }

    /// <summary>The type's name as a variable's.</summary>
    private static string Camel(string typeName)
      => char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
  }
}
