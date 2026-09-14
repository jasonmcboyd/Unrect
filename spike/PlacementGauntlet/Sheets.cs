using System;
using System.IO;

using Unrect;
using Unrect.Core;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE — the spaces the scenarios read. The four committed example workbooks (linked into the
  /// output by the csproj) and the synthetic grids that stand in for documents the corpus cannot
  /// commit: <c>examples/scrubbed-k1.xlsx</c> is local-only, so scenario 5's K-1-style nesting is a
  /// distilled in-memory grid, exactly as the fixture policy prescribes.
  /// </summary>
  public static class Sheets
  {
    public static string Example(string file) => Path.Combine(AppContext.BaseDirectory, "examples", file);

    public static string TestData(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);

    /// <summary>A blank-separated repeat of integer blocks — <c>linqpad/array.linq</c>'s grid.</summary>
    public static ICellValues Numbers()
      => GridSpace.Create(
        new[,]
        {
          { 1, 2, 3, 4 },
          { 5, 6, 7, 8 },
          { 0, 0, 0, 0 },
          { 9, 10, 11, 12 },
          { 13, 14, 15, 16 },
          { 17, 18, 19, 20 },
          { 0, 0, 0, 0 },
          { 21, 22, 23, 24 },
          { 25, 26, 27, 28 },
        },
        isBlank: v => v == 0);

    /// <summary>
    /// A buying-power-style export: one header row, then records whose columns are found by caption
    /// rather than by adjacency. Scenario 2's sheet — the census hotspot.
    /// </summary>
    public static ICellValues BuyingPower()
      => Grid(new object?[,]
      {
        { "Account", "Symbol", "Quantity", "Market Value", "Buying Power" },
        { "A-1", "MSFT", 100, 41000m, 12000m },
        { "A-2", "AAPL", 250, 55000m, 15000m },
        { "A-3", "NVDA", 75, 90000m, 21000m },
      });

    /// <summary>Two regions of the same shape, each announced by its own caption. Scenario 4's sheet.</summary>
    public static ICellValues Regions()
      => Grid(new object?[,]
      {
        { "Regional Report", null },
        { null, null },
        { "Region A", null },
        { "Fund", "Amount" },
        { "Alpha", 100m },
        { "Beta", 200m },
        { null, null },
        { "Region B", null },
        { "Fund", "Amount" },
        { "Gamma", 300m },
        { "Delta", 400m },
      });

    /// <summary>
    /// A K-1-style nesting: sections announced by caption, each carrying its own captioned table,
    /// with a terminator the second section is bounded by. Scenario 5's sheet.
    /// </summary>
    public static ICellValues K1()
      => Grid(new object?[,]
      {
        { "Partner K-1", null, null },
        { null, null, null },
        { "K-1 Lines 1-21", null, null },
        { "Line", "Description", "Amount" },
        { "1", "Ordinary business income", 1000m },
        { "2", "Net rental real estate", 250m },
        { null, null, null },
        { "Portfolio Income", null, null },
        { "Line", "Description", "Amount" },
        { "5", "Interest income", 75m },
        { "6a", "Ordinary dividends", 120m },
        { null, null, null },
        { "Totals", null, 1445m },
      });

    public static ICellValues Grid(object?[,] values) => GridSpace.Create(values, Cell);

    private static CellValue Cell(object? value) => value switch
    {
      null => CellValue.Blank,
      string text => CellValue.Of(text),
      int number => CellValue.Of(number),
      double number => CellValue.Of(number),
      decimal number => CellValue.Of(number),
      DateTime moment => CellValue.Of(moment),
      bool flag => CellValue.Of(flag),
      _ => throw new ArgumentException($"No canonical cell for {value.GetType().Name}.", nameof(value)),
    };
  }
}
