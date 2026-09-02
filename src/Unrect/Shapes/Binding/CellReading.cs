using System;
using System.Globalization;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// The single definition of what a kind failure and a conversion failure say. The typed leaves and
  /// the table binder both read cells through here, so a <c>Decimal()</c> leaf and a
  /// <c>decimal</c> column cannot describe the same cell differently.
  /// <para>
  /// The two sentences are deliberately different. A kind failure speaks the document's vocabulary —
  /// there are six kinds and one of them is <c>Number</c> — so it never mentions decimals or
  /// integers, which are the reader's business. A conversion failure speaks the reader's, on a
  /// number that is really there. Neither carries advice: a per-cell message can appear thousands of
  /// times in one sheet, and advice repeated that often is noise.
  /// </para>
  /// </summary>
  internal static class CellReading
  {
    public static string WrongKind(CellKind expected, CellValue found, string at)
      => $"expected {expected} at {at}, found {Found(found)}";

    /// <summary>
    /// An error cell says which error it is, through Core's own rendering — nothing is duplicated
    /// here and nothing is added there.
    /// </summary>
    private static string Found(CellValue cell)
      => cell.Kind == CellKind.Error ? cell.ToString() : cell.Kind.ToString();

    public static bool ReadString(CellValue cell, Func<string> at, out string value, out string? conversion)
    {
      value = cell.GetString();
      conversion = null;
      return true;
    }

    public static bool ReadDouble(CellValue cell, Func<string> at, out double value, out string? conversion)
    {
      value = cell.GetDouble();
      conversion = null;
      return true;
    }

    public static bool ReadDateTime(CellValue cell, Func<string> at, out DateTime value, out string? conversion)
    {
      value = cell.GetDateTime();
      conversion = null;
      return true;
    }

    public static bool ReadBoolean(CellValue cell, Func<string> at, out bool value, out string? conversion)
    {
      value = cell.GetBoolean();
      conversion = null;
      return true;
    }

    public static bool ReadDecimal(CellValue cell, Func<string> at, out decimal value, out string? conversion)
    {
      if (cell.TryGetDecimal() is decimal exact)
      {
        value = exact;
        conversion = null;
        return true;
      }

      value = default;
      conversion = $"the Number at {at()} ({Number(cell)}) is not representable as a decimal";
      return false;
    }

    public static bool ReadInteger(CellValue cell, Func<string> at, out int value, out string? conversion)
    {
      if (cell.TryGetInt() is int whole)
      {
        value = whole;
        conversion = null;
        return true;
      }

      var number = cell.GetDouble();

      value = default;
      var address = at();

      conversion = number < int.MinValue || number > int.MaxValue
        ? $"the Number at {address} ({Number(cell)}) is outside the range of a 32-bit integer"
        : $"the Number at {address} ({Number(cell)}) is not a whole number";
      return false;
    }

    /// <summary>
    /// Invariant, because a failure message is a diagnostic artefact that gets pasted into an issue
    /// rather than display output.
    /// </summary>
    private static string Number(CellValue cell)
      => cell.TryGetDecimal() is decimal exact
        ? exact.ToString(CultureInfo.InvariantCulture)
        : cell.GetDouble().ToString(CultureInfo.InvariantCulture);
  }
}
