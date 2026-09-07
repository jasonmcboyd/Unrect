using Unrect;
using Unrect.Core;

namespace TypedSpacesGauntlet
{
  /// <summary>SPIKE. One little sheet, in both flavours: with formulas and without.</summary>
  public static class Sheets
  {
    // r0  Buying Power Allocation
    // r1  (blank)
    // r2  Account | Symbol | Weight
    // r3  A-1     | SPY    | 0.25      <- =D4/$D$8
    // r4  A-2     | QQQ    | 0.75      <- =D5/$D$8
    // r5  (blank)
    // r6  Total   |        | 1.00      <- =SUM(C4:C5)
    private static CellValue[,] Values() => new[,]
    {
      { CellValue.Of("Buying Power Allocation"), CellValue.Blank,        CellValue.Blank },
      { CellValue.Blank,                         CellValue.Blank,        CellValue.Blank },
      { CellValue.Of("Account"),                 CellValue.Of("Symbol"), CellValue.Of("Weight") },
      { CellValue.Of("A-1"),                     CellValue.Of("SPY"),    CellValue.Of(0.25m) },
      { CellValue.Of("A-2"),                     CellValue.Of("QQQ"),    CellValue.Of(0.75m) },
      { CellValue.Blank,                         CellValue.Blank,        CellValue.Blank },
      { CellValue.Of("Total"),                   CellValue.Blank,        CellValue.Of(1.00m) },
    };

    private static string?[,] Formulas() => new string?[,]
    {
      { null, null, null },
      { null, null, null },
      { null, null, null },
      { null, null, "=D4/$D$8" },
      { null, null, "=D5/$D$8" },
      { null, null, null },
      { null, null, "=SUM(C4:C5)" },
    };

    /// <summary>The sheet as a workbook that carries formulas.</summary>
    public static FormulaGridSpace WithFormulas() => new FormulaGridSpace(Values(), Formulas());

    /// <summary>The same sheet as a plain grid — what a unit test builds, and what scenario 5 is about.</summary>
    public static GridSpace Plain() => new GridSpace(Values());

    // A second sheet, shaped like the buying-power export the row-projection slot was designed for:
    // no header the records can be bound by, values in columns 1, 6 and 9, and a last record that
    // carries a fund and nothing else.
    //
    // c0        c1      c6         c9
    // r0  PCTCAL2 BUYING POWER
    // r1  (blank)
    // r2  ACCOUNT   FUND    PRIMARY    FEP
    // r3            ABC     1250.75    300.00
    // r4            DEF      980.50    <fep>
    // r5            GHI     (blank)    (blank)     <- fund only
    // r6  (blank)
    // r7  TOTAL             2231.25
    private static CellValue[,] BuyingPowerValues(CellValue fepOfSecondRecord)
    {
      var cells = new CellValue[8, 11];

      cells[0, 0] = CellValue.Of("PCTCAL2 BUYING POWER");

      cells[2, 0] = CellValue.Of("ACCOUNT");
      cells[2, 1] = CellValue.Of("FUND");
      cells[2, 6] = CellValue.Of("PRIMARY");
      cells[2, 9] = CellValue.Of("FEP");

      cells[3, 1] = CellValue.Of("ABC");
      cells[3, 6] = CellValue.Of(1250.75m);
      cells[3, 9] = CellValue.Of(300.00m);

      cells[4, 1] = CellValue.Of("DEF");
      cells[4, 6] = CellValue.Of(980.50m);
      cells[4, 9] = fepOfSecondRecord;

      cells[5, 1] = CellValue.Of("GHI");

      cells[7, 0] = CellValue.Of("TOTAL");
      cells[7, 6] = CellValue.Of(2231.25m);

      return cells;
    }

    /// <summary>The buying-power sheet: sparse columns, and a last record that is a fund and two blanks.</summary>
    public static GridSpace BuyingPower() => new GridSpace(BuyingPowerValues(CellValue.Blank));

    /// <summary>
    /// The same sheet with text where a number belongs. <c>OrBlank</c> tolerates a blank and nothing
    /// else, so this must still fail — and name the record it failed in.
    /// </summary>
    public static GridSpace BuyingPowerWithText() => new GridSpace(BuyingPowerValues(CellValue.Of("n/a")));
  }

  public sealed record Allocation(string Account, string Symbol, decimal Weight);

  public sealed record Report(string Title, System.Collections.Generic.IReadOnlyList<Allocation> Rows);

  public sealed record SourcedAllocation(string Account, string? Formula);

  public sealed record AuditedReport(
    string Title,
    System.Collections.Generic.IReadOnlyList<Allocation> Rows,
    string? TotalFormula);

  public sealed record Probe(bool RawTypeTest, bool ThroughSeam, string? Formula);

  public sealed record BuyingPowerRow(string FundCode, decimal? Primary, decimal? Fep);
}
