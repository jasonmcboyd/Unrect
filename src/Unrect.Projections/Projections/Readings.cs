using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The one place a cell becomes a CLR value, for every space. Blank is decided here, first: a
  /// cell the space calls blank holds nothing a reading may assert, so every reading refuses it
  /// with <c>found Blank</c>, whatever the cell says. Everything else is the space's own answer,
  /// <see cref="ISpace.TryGetTextAt"/> and its three siblings, worded by the space. The two
  /// conversions a reader may want of a number — a decimal, a whole 32-bit integer — are made here
  /// over the double the space hands back, so the point reads, the leaves and the table binder
  /// cannot word the same number differently.
  /// </summary>
  internal static class Readings
  {
    internal static bool TryText<TSpace>(Point<TSpace> point, out string value, out CellProblem? problem)
      where TSpace : class, ISpace
      => point.IsBlank()
        ? Blank("Text", out value!, out problem)
        : point.Space.TryGetTextAt(point.Column, point.Row, out value, out problem);

    internal static bool TryDouble<TSpace>(Point<TSpace> point, out double value, out CellProblem? problem)
      where TSpace : class, ISpace
      => point.IsBlank()
        ? Blank("Number", out value, out problem)
        : point.Space.TryGetDoubleAt(point.Column, point.Row, out value, out problem);

    internal static bool TryDate<TSpace>(Point<TSpace> point, out DateTime value, out CellProblem? problem)
      where TSpace : class, ISpace
      => point.IsBlank()
        ? Blank("Date", out value, out problem)
        : point.Space.TryGetDateAt(point.Column, point.Row, out value, out problem);

    internal static bool TryBoolean<TSpace>(Point<TSpace> point, out bool value, out CellProblem? problem)
      where TSpace : class, ISpace
      => point.IsBlank()
        ? Blank("Boolean", out value, out problem)
        : point.Space.TryGetBooleanAt(point.Column, point.Row, out value, out problem);

    // --- Conversions ---------------------------------------------------------------------------
    //
    // A space hands back a double. A decimal or a whole number is something a READER wants, so
    // these sit above the space's contract and take the double it handed back.

    internal static bool TryDecimal<TSpace>(Point<TSpace> point, out decimal value, out CellProblem? problem)
      where TSpace : class, ISpace
    {
      value = default;

      if (!TryDouble(point, out var number, out problem))
        return false;

      if (number > (double)decimal.MinValue && number < (double)decimal.MaxValue)
      {
        value = (decimal)number;
        return true;
      }

      problem = new CellProblem("the Number at ", $" ({point.AsText()}) is not representable as a decimal");
      return false;
    }

    internal static bool TryInteger<TSpace>(Point<TSpace> point, out int value, out CellProblem? problem)
      where TSpace : class, ISpace
    {
      value = default;

      if (!TryDouble(point, out var number, out problem))
        return false;

      if (number >= int.MinValue && number <= int.MaxValue && Math.Floor(number) == number)
      {
        value = (int)number;
        return true;
      }

      problem = new CellProblem(
        "the Number at ",
        number < int.MinValue || number > int.MaxValue
          ? $" ({point.AsText()}) is outside the range of a 32-bit integer"
          : $" ({point.AsText()}) is not a whole number");
      return false;
    }

    /// <summary>A read that failed, as the exception a point read throws: the address and the reason.</summary>
    internal static CellReadException Failed<TSpace>(Point<TSpace> point, CellProblem? problem)
      where TSpace : class, ISpace
      => new CellReadException(
        point.Erased(),
        problem ?? throw new InvalidOperationException("a failed read must say why"));

    private static bool Blank<T>(string kind, out T value, out CellProblem? problem)
    {
      value = default!;
      problem = new CellProblem("expected " + kind + " at ", ", found Blank");
      return false;
    }
  }
}
