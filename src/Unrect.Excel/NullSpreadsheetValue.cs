using System;

namespace Unrect.Excel
{
  public class NullSpreadsheetValue : SpreadsheetValueBase
  {
    private NullSpreadsheetValue()
    {
    }

    public static NullSpreadsheetValue Instance { get; } = new NullSpreadsheetValue();

    public override bool HasValue => false;

    public override Type? GetValueType() => null;

    public override DateTime? TryGetDateTime() => null;
    public override double? TryGetDouble() => null;
    public override int? TryGetInt() => null;
    public override string? TryGetString() => null;
  }
}
