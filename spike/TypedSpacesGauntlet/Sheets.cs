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
  }

  public sealed record Allocation(string Account, string Symbol, decimal Weight);

  public sealed record Report(string Title, System.Collections.Generic.IReadOnlyList<Allocation> Rows);

  public sealed record SourcedAllocation(string Account, string? Formula);

  public sealed record AuditedReport(
    string Title,
    System.Collections.Generic.IReadOnlyList<Allocation> Rows,
    string? TotalFormula);

  public sealed record Probe(bool RawTypeTest, bool ThroughSeam, string? Formula);
}
