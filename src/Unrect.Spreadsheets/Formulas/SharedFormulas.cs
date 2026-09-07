using System.Text;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The reconstruction behind a shared formula: the master's expression, as it applies to another
  /// cell of the group.
  /// <para>
  /// <b>Why this exists at all.</b> An xlsx does not store a formula per cell. A filled column is
  /// written once as a master — <c>&lt;f t="shared" ref="D2:D5" si="0"&gt;B2*C2&lt;/f&gt;</c> — and
  /// the rest of the group as empty followers carrying only the group's index,
  /// <c>&lt;f t="shared" si="0"/&gt;</c>. A follower's formula is the master's text with its
  /// <em>relative</em> references moved by the distance between the two cells; its absolute ones
  /// ($-marked) stay put. That is the file format's compression, not a fact about the document, and
  /// undoing it is this reader's job rather than a caller's.
  /// </para>
  /// <para>
  /// <b>The decision, and why the two cheaper answers were rejected.</b> Handing back the master's
  /// text for every follower would report D5 as reading <c>B2*C2</c>, which is the formula of a
  /// different cell — plausible, wrong, and impossible to notice. Answering null would say "that
  /// cell is a plain value", the one thing null already means in
  /// <see cref="IFormulaSpace.FormulaAt"/>. Both are lies at the scale real files operate at: a
  /// genuine fund workbook in this project's corpus carries 35,089 formula cells, of which 14,734
  /// are written out, 706 are shared masters and <b>19,452 are followers</b> — 55% of every formula
  /// in the file, written that way by Excel itself, in a workbook nobody edited to be difficult. A
  /// reader that misreports more than half of what it was asked for has not read the file.
  /// </para>
  /// <para>
  /// <b>The shifting is cross-checked, not merely tested.</b> Every one of those 19,452 followers
  /// was reconstructed by this code and compared against openpyxl's <c>Translator</c> — an
  /// independent implementation of the same rule, in another language — with zero differences.
  /// That is what licenses the scan below being a reference finder rather than a formula parser.
  /// The two agree on a further adversarial set (quoted <c>"A1"</c> literals, <c>LOG10</c>,
  /// <c>1.5E+10</c>, table and external-workbook brackets, whole-row and whole-column ranges,
  /// mixed <c>$</c> marks) except at the edges of the sheet, where this code follows Excel and
  /// openpyxl does not: a reference shifted off the grid becomes <c>#REF!</c> here, where
  /// <c>Translator</c> either refuses or invents the column after XFD.
  /// </para>
  /// <para>
  /// <b>What it does not attempt.</b> An <c>&lt;f t="array"&gt;</c> is spelled once, at its anchor,
  /// and the cells it spills into carry no formula element at all; those answer null, because that
  /// is the file's own spelling and the alternative would require deciding between a legacy
  /// CSE range (where every cell shows the formula) and a modern dynamic-array spill (where they do
  /// not) from bytes that do not reliably say. An <c>&lt;f t="dataTable"&gt;</c> carries no
  /// expression to shift and answers null likewise. Both are recorded in
  /// <see cref="SpreadsheetSpace"/>, where a caller will look.
  /// </para>
  /// </summary>
  internal static class SharedFormulas
  {
    /// <summary>What Excel writes in place of a reference that has been shifted off the sheet.</summary>
    private const string OutOfRange = "#REF!";

    /// <summary>
    /// <paramref name="formula"/> as it applies <paramref name="columnDelta"/> columns and
    /// <paramref name="rowDelta"/> rows away from where it was written.
    /// <para>
    /// The scan is a reference finder rather than a formula parser: it needs to know only which
    /// stretches of text are references, so it steps over the three places a reference-shaped run
    /// of characters is not one — a quoted string, a quoted sheet name, and the brackets of a
    /// structured or external reference — and then judges each remaining run on its own. A run
    /// followed by <c>(</c> is a function (which is what keeps <c>LOG10</c> from being column LOG
    /// row 10), by <c>!</c> is a sheet name, and by <c>[</c> is a table name.
    /// </para>
    /// </summary>
    internal static string Shift(string formula, int columnDelta, int rowDelta)
    {
      if (formula.Length == 0 || (columnDelta == 0 && rowDelta == 0))
        return formula;

      var shifted = new StringBuilder(formula.Length + 8);
      var index = 0;
      var previous = '\0';

      while (index < formula.Length)
      {
        var c = formula[index];

        if (c == '"' || c == '\'')
        {
          index = CopyQuoted(formula, index, shifted);
        }
        else if (c == '[')
        {
          index = CopyBracketed(formula, index, shifted);
        }
        else if (IsRunCharacter(c))
        {
          var end = index;
          while (end < formula.Length && IsRunCharacter(formula[end]))
            end++;

          shifted.Append(Translate(
            formula.Substring(index, end - index),
            previous,
            end < formula.Length ? formula[end] : '\0',
            columnDelta,
            rowDelta));

          index = end;
        }
        else
        {
          shifted.Append(c);
          index++;
        }

        previous = formula[index - 1];
      }

      return shifted.ToString();
    }

    /// <summary>
    /// One run of reference-shaped characters, shifted if it is a reference and copied if it is
    /// anything else — a function name, a defined name, a number, a boolean.
    /// </summary>
    private static string Translate(string run, char previous, char following, int columnDelta, int rowDelta)
    {
      // A reference is never followed by any of these; a name is, and shifting a name would rewrite
      // something that is not a coordinate at all.
      if (following == '(' || following == '!' || following == '[')
        return run;

      if (TryShiftCell(run, columnDelta, rowDelta, out var cell))
        return cell;

      // A whole column or a whole row is only a reference as half of a range: A:A, 3:3. On its own
      // the same run is a name or a number, and there is nothing to shift.
      if (previous != ':' && following != ':')
        return run;

      return TryShiftAxis(run, columnDelta, rowDelta, out var axis) ? axis : run;
    }

    /// <summary>A cell reference — <c>A1</c>, <c>$A1</c>, <c>A$1</c>, <c>$A$1</c> — moved.</summary>
    private static bool TryShiftCell(string run, int columnDelta, int rowDelta, out string shifted)
    {
      shifted = run;

      var index = 0;
      var columnFixed = Absolute(run, ref index);
      var letters = index;

      while (index < run.Length && A1Reference.IsLetter(run[index]))
        index++;

      var columnLength = index - letters;
      if (columnLength == 0 || columnLength > 3 || index == run.Length)
        return false;

      var rowFixed = Absolute(run, ref index);
      var digits = index;

      while (index < run.Length && A1Reference.IsDigit(run[index]))
        index++;

      // Anything left over means the run was a name that merely began like a reference.
      if (index != run.Length || index == digits || index - digits > 7)
        return false;

      var column = A1Reference.ColumnIndex(run, letters, columnLength);
      var row = int.Parse(run.Substring(digits), System.Globalization.CultureInfo.InvariantCulture) - 1;

      if (!Move(ref column, columnFixed ? 0 : columnDelta, A1Reference.MaximumColumn)
        || !Move(ref row, rowFixed ? 0 : rowDelta, A1Reference.MaximumRow))
      {
        shifted = OutOfRange;
        return true;
      }

      shifted = $"{Mark(columnFixed)}{A1Reference.ColumnName(column)}{Mark(rowFixed)}{row + 1}";

      return true;
    }

    /// <summary>One end of a whole-column or whole-row range — <c>A</c>, <c>$BC</c>, <c>3</c> — moved.</summary>
    private static bool TryShiftAxis(string run, int columnDelta, int rowDelta, out string shifted)
    {
      shifted = run;

      var index = 0;
      var isFixed = Absolute(run, ref index);
      if (index == run.Length)
        return false;

      var letters = A1Reference.IsLetter(run[index]);

      for (var scan = index; scan < run.Length; scan++)
        if (letters ? !A1Reference.IsLetter(run[scan]) : !A1Reference.IsDigit(run[scan]))
          return false;

      var length = run.Length - index;
      if (letters ? length > 3 : length > 7)
        return false;

      var position = letters
        ? A1Reference.ColumnIndex(run, index, length)
        : int.Parse(run.Substring(index), System.Globalization.CultureInfo.InvariantCulture) - 1;

      if (position < 0)
        return false;

      if (!Move(ref position, isFixed ? 0 : letters ? columnDelta : rowDelta, letters ? A1Reference.MaximumColumn : A1Reference.MaximumRow))
      {
        shifted = OutOfRange;
        return true;
      }

      shifted = letters
        ? Mark(isFixed) + A1Reference.ColumnName(position)
        : Mark(isFixed) + (position + 1);

      return true;
    }

    /// <summary>A quoted string or a quoted sheet name, copied verbatim; a doubled quote stays inside it.</summary>
    private static int CopyQuoted(string formula, int start, StringBuilder shifted)
    {
      var quote = formula[start];
      var index = start + 1;

      shifted.Append(quote);

      while (index < formula.Length)
      {
        shifted.Append(formula[index]);

        if (formula[index] == quote)
        {
          index++;

          // A doubled quote is an escaped one: the string continues.
          if (index >= formula.Length || formula[index] != quote)
            return index;

          shifted.Append(quote);
        }

        index++;
      }

      return index;
    }

    /// <summary>
    /// A bracketed reference, copied verbatim: an external workbook's index (<c>[1]Sheet1!A1</c>)
    /// or a table's column names (<c>Table1[[Qty]:[Total]]</c>), which nest and which look exactly
    /// like references without being them.
    /// </summary>
    private static int CopyBracketed(string formula, int start, StringBuilder shifted)
    {
      var depth = 0;
      var index = start;

      for (; index < formula.Length; index++)
      {
        shifted.Append(formula[index]);

        if (formula[index] == '[')
          depth++;
        else if (formula[index] == ']' && --depth == 0)
          return index + 1;
      }

      return index;
    }

    /// <summary>
    /// Whether the run's next character is a <c>$</c>, consuming it if so — the mark that says this
    /// half of the reference does not move.
    /// </summary>
    private static bool Absolute(string run, ref int index)
    {
      if (index >= run.Length || run[index] != '$')
        return false;

      index++;

      return true;
    }

    private static bool Move(ref int position, int delta, int maximum)
    {
      position += delta;

      return position >= 0 && position <= maximum;
    }

    private static string Mark(bool isFixed) => isFixed ? "$" : string.Empty;

    /// <summary>
    /// The characters a reference can be spelled from, plus the ones that make a name a name: a run
    /// is taken whole so that <c>Q1_total</c> and <c>1.5</c> are judged as themselves rather than
    /// as the reference hiding in their first few characters.
    /// </summary>
    private static bool IsRunCharacter(char c)
      => A1Reference.IsLetter(c) || A1Reference.IsDigit(c) || c == '$' || c == '_' || c == '.';
  }
}
