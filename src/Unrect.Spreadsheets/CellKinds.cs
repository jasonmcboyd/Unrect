using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// How a sheet answers the kind facet from a <see cref="CellValue"/>: the one place a wrong kind
  /// is worded, in the document's vocabulary — <c>expected Number at B4, found Text</c>, an error
  /// spelled as the file spells it — so a leaf, a point read and the table binder cannot describe
  /// the same cell differently.
  /// </summary>
  internal static class CellKinds
  {
    internal static bool Text(CellValue cell, out string value, out CellProblem? problem)
    {
      if (!cell.TryGetText(out value))
        return Wrong(CellKind.Text, cell, out value!, out problem);

      problem = null;
      return true;
    }

    internal static bool Double(CellValue cell, out double value, out CellProblem? problem)
    {
      if (!cell.TryGetNumber(out value))
        return Wrong(CellKind.Number, cell, out value, out problem);

      problem = null;
      return true;
    }

    internal static bool DateTime(CellValue cell, out DateTime value, out CellProblem? problem)
    {
      if (!cell.TryGetDate(out value))
        return Wrong(CellKind.Date, cell, out value, out problem);

      problem = null;
      return true;
    }

    internal static bool Boolean(CellValue cell, out bool value, out CellProblem? problem)
    {
      if (!cell.TryGetBoolean(out value))
        return Wrong(CellKind.Boolean, cell, out value, out problem);

      problem = null;
      return true;
    }

    /// <summary>The word after "found": the kind, or an error spelled as the file spells it.</summary>
    internal static string Describe(CellValue cell)
      => cell.Kind == CellKind.Error ? cell.ToString() : cell.Kind.ToString();

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

    private static bool Wrong<T>(CellKind expected, CellValue found, out T value, out CellProblem? problem)
    {
      value = default!;
      problem = new CellProblem(
        Expected[(int)expected],
        found.Kind == CellKind.Error ? ", found " + Describe(found) : Found[(int)found.Kind]);
      return false;
    }
  }
}
