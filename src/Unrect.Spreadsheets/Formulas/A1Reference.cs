using System.Text;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A1 cell references, both ways: the <c>r="AB12"</c> attribute the sheet XML puts on every cell,
  /// and the column letters a shifted formula has to be spelled back into.
  /// <para>
  /// Coordinates here are 0-based, the way a space addresses cells; the file's are 1-based. The
  /// conversion happens at this boundary and nowhere else, so nothing above it has to remember
  /// which convention it is holding.
  /// </para>
  /// </summary>
  internal static class A1Reference
  {
    /// <summary>The last column a sheet has, 0-based: XFD.</summary>
    internal const int MaximumColumn = 16383;

    /// <summary>The last row a sheet has, 0-based.</summary>
    internal const int MaximumRow = 1048575;

    /// <summary>
    /// The 0-based cell <paramref name="reference"/> names, or false when it does not name one —
    /// which covers the whole-column and whole-row forms a cell attribute never uses.
    /// </summary>
    internal static bool TryParse(string reference, out int column, out int row)
    {
      column = 0;
      row = 0;

      var letters = 0;
      while (letters < reference.Length && IsLetter(reference[letters]))
        letters++;

      if (letters == 0 || letters > 3 || letters == reference.Length || reference.Length - letters > 7)
        return false;

      var value = 0;
      for (var index = letters; index < reference.Length; index++)
      {
        if (!IsDigit(reference[index]))
          return false;

        value = (value * 10) + (reference[index] - '0');
      }

      if (value == 0)
        return false;

      column = ColumnIndex(reference, 0, letters);
      row = value - 1;

      if (column <= MaximumColumn && row <= MaximumRow)
        return true;

      // The TryParse contract: a refusal leaves nothing behind. A well-formed reference off the
      // end of the sheet used to return false with its numbers still set — harmless to the one
      // caller that read the outs only on true, but a trap for the next one.
      column = 0;
      row = 0;

      return false;
    }

    /// <summary>The 0-based column the <paramref name="length"/> letters at <paramref name="start"/> spell.</summary>
    internal static int ColumnIndex(string reference, int start, int length)
    {
      var column = 0;

      for (var index = start; index < start + length; index++)
        column = (column * 26) + (Upper(reference[index]) - 'A' + 1);

      return column - 1;
    }

    /// <summary>The letters for a 0-based <paramref name="column"/> — 0 is A, 26 is AA.</summary>
    internal static string ColumnName(int column)
    {
      var letters = new StringBuilder(3);

      for (var remaining = column + 1; remaining > 0; remaining = (remaining - 1) / 26)
        letters.Insert(0, (char)('A' + ((remaining - 1) % 26)));

      return letters.ToString();
    }

    internal static bool IsLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    internal static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static char Upper(char c) => c >= 'a' ? (char)(c - 32) : c;
  }
}
