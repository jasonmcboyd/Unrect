using System;
using System.Globalization;

namespace Unrect.Core
{
  /// <summary>
  /// Reading a number, a date or a boolean out of what a cell says — the default a space may use
  /// when its store is text: a CSV, a grid of strings, a cell of a typed grid holding a word. The
  /// culture is invariant, a thousands separator is allowed in a number, and a failure says what the
  /// cell said: <c>expected Number at B4; the cell says 'abc'</c>.
  /// </summary>
  public static class TextReads
  {
    /// <summary>The number <paramref name="says"/> spells, or why it spells none.</summary>
    public static bool TryDouble(string says, out double value, out CellProblem? problem)
      => Read(double.TryParse(says, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value), "Number", says, out problem);

    /// <summary>The date <paramref name="says"/> spells, or why it spells none.</summary>
    public static bool TryDate(string says, out DateTime value, out CellProblem? problem)
      => Read(DateTime.TryParse(says, CultureInfo.InvariantCulture, DateTimeStyles.None, out value), "Date", says, out problem);

    /// <summary>The boolean <paramref name="says"/> spells, or why it spells none.</summary>
    public static bool TryBoolean(string says, out bool value, out CellProblem? problem)
      => Read(bool.TryParse(says, out value), "Boolean", says, out problem);

    /// <summary>The reason a text could not be read as <paramref name="kind"/>: it says what it says.</summary>
    public static CellProblem Unreadable(string kind, string says)
      => new CellProblem("expected " + kind + " at ", "; the cell says '" + says + "'");

    private static bool Read(bool parsed, string kind, string says, out CellProblem? problem)
    {
      problem = parsed ? (CellProblem?)null : Unreadable(kind, says);
      return parsed;
    }
  }
}
