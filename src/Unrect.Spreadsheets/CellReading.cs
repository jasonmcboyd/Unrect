using System;
using System.Globalization;

using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The single definition of what a kind failure and a conversion failure say. Every door reads its
  /// cells through here, so a <c>Decimal()</c> leaf, a <c>decimal</c> column and a point read cannot
  /// describe the same cell differently.
  /// <para>
  /// The two sentences are deliberately different. A kind failure speaks the document's vocabulary —
  /// there are six kinds and one of them is <c>Number</c> — so it never mentions decimals or
  /// integers, which are the reader's business. A conversion failure speaks the reader's, on a
  /// number that is really there. Neither carries advice: a per-cell message can appear thousands of
  /// times in one sheet, and advice repeated that often is noise.
  /// </para>
  /// <para>
  /// Each sentence comes back as a <see cref="CellProblem"/> — everything but the address, which the
  /// layer that knows where the cell sits fills in last.
  /// </para>
  /// </summary>
  internal static class CellReading
  {
    internal static bool Text(Cell cell, out string value, out CellProblem? problem)
    {
      if (cell.Kind != CellKind.Text)
        return Wrong(CellKind.Text, cell, out value!, out problem);

      value = cell.GetString();
      problem = null;
      return true;
    }

    internal static bool Double(Cell cell, out double value, out CellProblem? problem)
    {
      if (cell.Kind != CellKind.Number)
        return Wrong(CellKind.Number, cell, out value, out problem);

      value = cell.GetDouble();
      problem = null;
      return true;
    }

    internal static bool DateTime(Cell cell, out DateTime value, out CellProblem? problem)
    {
      if (cell.Kind != CellKind.Temporal)
        return Wrong(CellKind.Temporal, cell, out value, out problem);

      value = cell.GetDateTime();
      problem = null;
      return true;
    }

    internal static bool Boolean(Cell cell, out bool value, out CellProblem? problem)
    {
      if (cell.Kind != CellKind.Boolean)
        return Wrong(CellKind.Boolean, cell, out value, out problem);

      value = cell.GetBoolean();
      problem = null;
      return true;
    }

    internal static bool Decimal(Cell cell, out decimal value, out CellProblem? problem)
    {
      if (cell.Kind != CellKind.Number)
        return Wrong(CellKind.Number, cell, out value, out problem);

      if (cell.TryGetDecimal() is decimal exact)
      {
        value = exact;
        problem = null;
        return true;
      }

      value = default;
      problem = new CellProblem("the Number at ", $" ({Number(cell)}) is not representable as a decimal");
      return false;
    }

    internal static bool Integer(Cell cell, out int value, out CellProblem? problem)
    {
      if (cell.Kind != CellKind.Number)
        return Wrong(CellKind.Number, cell, out value, out problem);

      if (cell.TryGetInt() is int whole)
      {
        value = whole;
        problem = null;
        return true;
      }

      var number = cell.GetDouble();

      value = default;
      problem = new CellProblem(
        "the Number at ",
        number < int.MinValue || number > int.MaxValue
          ? $" ({Number(cell)}) is outside the range of a 32-bit integer"
          : $" ({Number(cell)}) is not a whole number");
      return false;
    }

    /// <summary>
    /// What the cell holds, said the way a message says it: an error spells itself out, everything
    /// else says its kind.
    /// </summary>
    internal static string Describe(Cell cell)
      => cell.Kind == CellKind.Error ? cell.ToString() : cell.Kind.ToString();

    /// <summary>
    /// The two halves of a kind failure, said once per kind: a refused read is the common case under
    /// a predicate, and the reason for it is these same few words every time.
    /// </summary>
    private static readonly string[] Expected = Halves(kind => $"expected {kind} at ");

    private static readonly string[] Found = Halves(kind => $", found {kind}");

    private static string[] Halves(Func<CellKind, string> say)
    {
      var kinds = (CellKind[])Enum.GetValues(typeof(CellKind));
      var halves = new string[kinds.Length];

      foreach (var kind in kinds)
        halves[(int)kind] = say(kind);

      return halves;
    }

    private static bool Wrong<T>(CellKind expected, Cell found, out T value, out CellProblem? problem)
    {
      value = default!;
      problem = new CellProblem(
        Expected[(int)expected],
        found.Kind == CellKind.Error ? ", found " + Describe(found) : Found[(int)found.Kind]);
      return false;
    }

    /// <summary>
    /// Invariant, because a failure message is a diagnostic artefact that gets pasted into an issue
    /// rather than display output.
    /// </summary>
    private static string Number(Cell cell)
      => cell.TryGetDecimal() is decimal exact
        ? exact.ToString(CultureInfo.InvariantCulture)
        : cell.GetDouble().ToString(CultureInfo.InvariantCulture);
  }
}
