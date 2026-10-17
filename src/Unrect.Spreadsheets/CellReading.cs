using System;

using Unrect.Core;

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
    // --- Conversions ---------------------------------------------------------------------------
    //
    // A sheet holds doubles. A decimal or a whole number is something a READER wants, so these sit
    // above the space's contract and take the double it handed back: the point reads, the two
    // convenience leaves and the table binder all convert here, which is why none of them can word
    // the same number differently.

    /// <summary>
    /// The cell's number as a <see cref="decimal"/>. The conversion rounds to the fifteen significant
    /// digits a double carries, which is what takes the binary noise off an amount: a cell that
    /// stores 0.30000000000000004 reads as 0.3.
    /// </summary>
    internal static bool Decimal<TSpace>(Point<TSpace> cell, out decimal value, out CellProblem? problem)
      where TSpace : class, ICellSpace
    {
      value = default;

      if (!cell.TryGetDouble(out var number, out problem))
        return false;

      if (number > (double)decimal.MinValue && number < (double)decimal.MaxValue)
      {
        value = (decimal)number;
        return true;
      }

      problem = new CellProblem("the Number at ", $" ({Said(cell, number)}) is not representable as a decimal");
      return false;
    }

    internal static bool Integer<TSpace>(Point<TSpace> cell, out int value, out CellProblem? problem)
      where TSpace : class, ICellSpace
    {
      value = default;

      if (!cell.TryGetDouble(out var number, out problem))
        return false;

      if (number >= int.MinValue && number <= int.MaxValue && Math.Floor(number) == number)
      {
        value = (int)number;
        return true;
      }

      problem = new CellProblem(
        "the Number at ",
        number < int.MinValue || number > int.MaxValue
          ? $" ({Said(cell, number)}) is outside the range of a 32-bit integer"
          : $" ({Said(cell, number)}) is not a whole number");
      return false;
    }

    /// <summary>
    /// What the cell holds, said the way a message says it: an error spells itself out, everything
    /// else says its kind.
    /// </summary>
    private static string Said<TSpace>(Point<TSpace> cell, double number)
      where TSpace : class, ICellSpace
      => cell.AsText();
  }
}
