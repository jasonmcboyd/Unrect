using System;
using System.Globalization;

using Unrect.Spreadsheets;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The canonical benchmark workloads: every grid shape at standardized size tiers, defined in
  /// exactly one place so cross-benchmark comparisons are same-scale by construction.
  ///
  /// <para><b>Why sizes are standardized (the noise floor).</b> Sub-millisecond benchmarks are
  /// noise on a shared CI runner. The rule inherited from Copse: every row must clear ~1 ms on the
  /// slowest runner CPU, with ~10 ms as the design target. Rather than tune each benchmark's size
  /// until it stopped swinging -- which fixes the noise but leaves every benchmark at a different,
  /// undocumented size -- the tiers below are fixed and shared, so any two rows measured at the
  /// same tier are directly comparable.</para>
  ///
  /// <para><b>The tiers are cell counts, because cells are what the engine touches.</b> A space is
  /// <c>rows x columns</c>; a projection's cost tracks the cells it reads, not the rows alone. Ten
  /// columns is the standard width -- wide enough that a row is not a degenerate single cell,
  /// narrow enough that a Mega grid stays under a gigabyte.</para>
  /// <list type="bullet">
  ///   <item>Mega: 100,000 x 10 = 1,000,000 cells -- the engine and value-representation tier</item>
  ///   <item>Large: 10,000 x 10 = 100,000 cells -- the table ladder's lower rung</item>
  /// </list>
  ///
  /// <para><b>Why fixtures are lazy per-property, not a static initializer.</b> BenchmarkDotNet
  /// runs each benchmark in its own process, so a class-wide static initializer would build every
  /// Mega fixture in every process to serve the one that process needs -- several hundred megabytes
  /// of <see cref="Cell"/> for nothing. Each fixture caches itself on first touch, and each
  /// benchmark class touches what it needs from a <c>[GlobalSetup]</c>, which BenchmarkDotNet
  /// excludes from measurement. The rule: <b>no benchmark may build a fixture inside a measured
  /// operation</b> -- construction is the subject of exactly one family (Values), where it is
  /// measured deliberately.</para>
  ///
  /// <para><b>Determinism.</b> The sparse and document fixtures use a fixed-seed generator, so the
  /// same commit measured twice sees byte-identical inputs. Nothing here reads a file: the CI
  /// runners get no workbooks, and a benchmark that depended on one could not run.</para>
  ///
  /// <para><b>Reading results.</b> Shared CI runners are a CPU lottery (Copse observed EPYC 9V74 /
  /// EPYC 7763 / Xeon 8573C, spanning roughly +-30%), and each matrix leg draws its own machine.
  /// Same-run comparisons -- rows within one leg, which is why a comparison group must live in one
  /// family -- are trustworthy; cross-run absolute deltas are not, until checked against
  /// HostEnvironmentInfo.ProcessorName in the run artifacts.</para>
  /// </summary>
  internal static class CanonicalSpaces
  {
    public const int MegaRows = 100_000;
    public const int LargeRows = 10_000;

    /// <summary>The standard width. Every tiered fixture is this wide unless it says otherwise.</summary>
    public const int Columns = 10;

    public const int MegaCells = MegaRows * Columns;

    /// <summary>Roughly the density of a real K-1 cross-tab: most of the grid is empty.</summary>
    public const double SparseDensity = 0.25;

    // ----- Dense numeric: every cell a number. The engine's cheapest possible content. -----

    private static ISheetCells? _megaDenseNumeric;
    public static ISheetCells MegaDenseNumeric => _megaDenseNumeric ??= SheetGrid.Of(DenseNumericCells(MegaRows));

    // ----- Dense mixed: kinds cycle by column, so a sweep sees every branch of the value model. -----

    private static ISheetCells? _megaDenseMixed;
    public static ISheetCells MegaDenseMixed => _megaDenseMixed ??= SheetGrid.Of(DenseMixedCells(MegaRows));

    // ----- Sparse: the K-1 shape. Same extent as dense, a quarter of the values. -----

    private static ISheetCells? _megaSparse;
    public static ISheetCells MegaSparse => _megaSparse ??= SheetGrid.Of(SparseCells(MegaRows));

    // ----- Raw arrays, for the adaptation benchmarks that measure GridSpace.Create itself. -----

    private static int[,]? _megaInts;
    public static int[,] MegaInts => _megaInts ??= Ints(MegaRows);

    private static object?[,]? _megaObjects;
    public static object?[,] MegaObjects => _megaObjects ??= Objects(MegaRows);

    // ----- Dense text: every cell its own string, for the rendering read. -----

    private static ISheetCells? _megaDenseText;
    public static ISheetCells MegaDenseText => _megaDenseText ??= SheetGrid.Of(DenseTextCells(MegaRows));

    // ----- Tabular: a header row over typed columns that bind to SummaryRow by caption. -----

    private static ISheetCells? _largeTabular;
    public static ISheetCells LargeTabular => _largeTabular ??= SheetGrid.Of(TabularCells(LargeRows));

    private static ISheetCells? _megaTabular;
    public static ISheetCells MegaTabular => _megaTabular ??= SheetGrid.Of(TabularCells(MegaRows));

    // ----- Documents: the investor-IRR shape, the end-to-end subject. -----

    private static ISheetCells? _smallDocument;
    public static ISheetCells SmallDocument => _smallDocument ??= SheetGrid.Of(DocumentCells(SmallDocumentInvestors));

    private static ISheetCells? _largeDocument;
    public static ISheetCells LargeDocument => _largeDocument ??= SheetGrid.Of(DocumentCells(LargeDocumentInvestors));

    /// <summary>
    /// The smaller of the two end-to-end sizes, at roughly 2 ms a parse.
    /// <para>
    /// It is not the real workbook's size. Three investors -- what
    /// <c>examples/investor-irr.xlsx</c> actually holds -- measured 18 us, fifty times under the
    /// noise floor, where a regression could never surface through the run-to-run spread. Nothing
    /// physical forced that size, so the rule wins over the anecdote: both document rows clear the
    /// floor, and the pair still answers the scaling question a single size could not.
    /// </para>
    /// </summary>
    public const int SmallDocumentInvestors = 400;

    /// <summary>The same document an order of magnitude out, at the ~10 ms design target.</summary>
    public const int LargeDocumentInvestors = 4_000;

    public const int DocumentBlockRows = 4;

    /// <summary>The caption that ends the first cash-flow series and announces the second.</summary>
    public const string InceptionCaption = "Cash Flows using inception date";

    public const string DetailsCaption = "IRR Details";
    public const string TransferDateCaption = "Cash Flows Using Transfer Date";

    // ----- Landmark rows, for the seek benchmarks. -----

    /// <summary>The text a <c>RowContaining</c> seek looks for in the landmark fixtures.</summary>
    public const string Landmark = "LANDMARK";

    private static ISheetCells? _landmarkNear;
    public static ISheetCells LandmarkNear => _landmarkNear ??= SheetGrid.Of(LandmarkCells(MegaRows, MegaRows / 10));

    private static ISheetCells? _landmarkFar;
    public static ISheetCells LandmarkFar => _landmarkFar ??= SheetGrid.Of(LandmarkCells(MegaRows, MegaRows * 9 / 10));

    /// <summary>No landmark anywhere: the seek that scans the whole grid and finds nothing.</summary>
    private static ISheetCells? _landmarkAbsent;
    public static ISheetCells LandmarkAbsent => _landmarkAbsent ??= SheetGrid.Of(LandmarkCells(MegaRows, -1));

    // ----- Blocks: many small regions separated by blank rows, the Repeat subject. -----

    /// <summary>Enough blocks that per-item engine cost dominates the per-cell cost.</summary>
    public const int BlockCount = 2_000;

    public const int BlockRows = 4;

    private static ISheetCells? _repeatBlocks;
    public static ISheetCells RepeatBlocks => _repeatBlocks ??= SheetGrid.Of(BlockCells(BlockCount, BlockRows));

    /// <summary>A leading run of blank rows, sized so skipping it is the whole measurement.</summary>
    private static ISheetCells? _blankLed;
    public static ISheetCells BlankLed => _blankLed ??= SheetGrid.Of(BlankLedCells(MegaRows, MegaRows / 2));

    // ----- Builders -----

    private static Cell[,] DenseNumericCells(int rows)
    {
      var cells = new Cell[rows, Columns];

      for (int row = 0; row < rows; row++)
        for (int column = 0; column < Columns; column++)
          cells[row, column] = Cell.Of(row * Columns + column);

      return cells;
    }

    private static Cell[,] DenseTextCells(int rows)
    {
      var cells = new Cell[rows, Columns];

      for (int row = 0; row < rows; row++)
        for (int column = 0; column < Columns; column++)
          cells[row, column] = Cell.Of(Label(row * Columns + column));

      return cells;
    }

    /// <summary>
    /// Kinds cycling across a full-width grid.
    /// <para>
    /// The row stride is <c>Columns + 1</c>, not <c>Columns</c>, and that matters: the kind cycle
    /// has period five and the grid is ten wide, so a stride of ten would put the cycle's blank
    /// entry in columns 4 and 9 of EVERY row -- two permanently empty columns, which is not what a
    /// sheet looks like and which stops width discovery at column 4. The extra step shifts the
    /// cycle one column per row, so the blanks fall on a diagonal instead.
    /// </para>
    /// </summary>
    private static Cell[,] DenseMixedCells(int rows)
    {
      var cells = new Cell[rows, Columns];

      for (int row = 0; row < rows; row++)
        for (int column = 0; column < Columns; column++)
          cells[row, column] = MixedCell(row * (Columns + 1) + column);

      return cells;
    }

    /// <summary>
    /// The K-1 cross-tab shape: a label in column 0 of every row, and amounts scattered across the
    /// fund columns at roughly <see cref="SparseDensity"/>.
    /// <para>
    /// The always-populated label column is not decoration, it is what makes the fixture usable.
    /// With blankness independent across all ten columns, a row is entirely blank about 5.6% of the
    /// time (0.75^10), so a "rows while any cell has a value" scan would stop after twenty-odd rows
    /// and measure nothing. A real cross-tab does not look like that either: the row exists because
    /// it is labelled, and the amounts against it are sparse.
    /// </para>
    /// </summary>
    private static Cell[,] SparseCells(int rows)
    {
      var cells = new Cell[rows, Columns];
      var random = new Random(20260903);

      for (int row = 0; row < rows; row++)
      {
        cells[row, 0] = Cell.Of(Label(row));

        for (int column = 1; column < Columns; column++)
          cells[row, column] = random.NextDouble() < SparseDensity
            ? Cell.Of(row * Columns + column)
            : Cell.Blank;
      }

      return cells;
    }

    private static Cell[,] LandmarkCells(int rows, int landmarkRow)
    {
      var cells = DenseNumericCells(rows);

      // The seek reads column 0 of every row until it matches, so only column 0 can carry the
      // landmark -- putting it anywhere else would make the "hit" fixtures scan to the end too.
      if (landmarkRow >= 0)
        cells[landmarkRow, 0] = Cell.Of(Landmark);

      return cells;
    }

    private static Cell[,] BlankLedCells(int rows, int blankRows)
    {
      var cells = new Cell[rows, Columns];

      for (int row = 0; row < rows; row++)
        for (int column = 0; column < Columns; column++)
          cells[row, column] = row < blankRows ? Cell.Blank : Cell.Of(row * Columns + column);

      return cells;
    }

    private static Cell[,] BlockCells(int blocks, int blockRows)
    {
      // Every block is blockRows tall and followed by one blank separator row.
      var cells = new Cell[blocks * (blockRows + 1), Columns];

      for (int block = 0; block < blocks; block++)
        for (int row = 0; row < blockRows; row++)
          for (int column = 0; column < Columns; column++)
            cells[block * (blockRows + 1) + row, column] = Cell.Of(block * blockRows + row + column);


      return cells;
    }

    private static Cell[,] TabularCells(int rows)
    {
      // One header row, then rows that bind to SummaryRow: text, four decimals, a double. The
      // captions are what Table<T>() matches members against.
      var cells = new Cell[rows + 1, 6];

      cells[0, 0] = Cell.Of("Investor");
      cells[0, 1] = Cell.Of("Contribution");
      cells[0, 2] = Cell.Of("Distribution");
      cells[0, 3] = Cell.Of("Fee");
      cells[0, 4] = Cell.Of("End Balance");
      cells[0, 5] = Cell.Of("Irr");

      for (int row = 1; row <= rows; row++)
      {
        cells[row, 0] = Cell.Of(Label(row));
        cells[row, 1] = Cell.Of(1000m + row);
        cells[row, 2] = Cell.Of(250m + row);
        cells[row, 3] = Cell.Of(15m + row % 40);
        cells[row, 4] = Cell.Of(800m + row);
        cells[row, 5] = Cell.Of(0.05 + row % 100 / 1000.0);
      }

      return cells;
    }

    /// <summary>
    /// The investor-IRR document, synthesized: a four-cell header, a summary table, then the same
    /// per-investor cash-flow blocks twice under two captions. The row-for-row layout of
    /// <c>examples/investor-irr.xlsx</c> -- gap sizes included, since the gaps are what the
    /// placement defaults and the <c>Until</c> bound actually resolve against.
    /// </summary>
    private static Cell[,] DocumentCells(int investors)
    {
      var series = investors * (1 + DocumentBlockRows + 1);   // header + rows + separator, per block
      var cells = new Cell[4 + 1 + 1 + investors + 3 + 2 + series + 1 + 1 + series, 6];
      int row = 0;

      cells[row++, 0] = Cell.Of("Investor IRR Report");
      cells[row++, 0] = Cell.Of("Growth Fund II, LP");
      cells[row++, 0] = Cell.Of(new DateTime(2026, 6, 30));
      cells[row++, 0] = Cell.Of("RPT-00214");
      row++;                                                   // the gap the table's placement absorbs

      cells[row, 0] = Cell.Of("Investors");
      cells[row, 1] = Cell.Of("Contribution ITD");
      cells[row, 2] = Cell.Of("Distribution ITD");
      cells[row, 3] = Cell.Of("Management Fee ITD");
      cells[row, 4] = Cell.Of("End Balance");
      cells[row, 5] = Cell.Of("IRR");
      row++;

      for (int investor = 0; investor < investors; investor++)
      {
        cells[row, 0] = Cell.Of(Investor(investor));
        cells[row, 1] = Cell.Of(500000m + investor);
        cells[row, 2] = Cell.Of(125000m + investor);
        cells[row, 3] = Cell.Of(15000m + investor);
        cells[row, 4] = Cell.Of(402500m + investor);
        cells[row, 5] = Cell.Of(0.08 + investor % 50 / 1000.0);
        row++;
      }

      row += 3;
      cells[row++, 0] = Cell.Of(DetailsCaption);
      cells[row++, 0] = Cell.Of(TransferDateCaption);
      row = WriteBlocks(cells, row, investors, 2024);

      // Two blank rows before the caption, as in the real workbook: the last block's own separator
      // supplies the first, so only one more is written here.
      row += 1;
      cells[row++, 0] = Cell.Of(InceptionCaption);
      WriteBlocks(cells, row, investors, 2023);


      return cells;
    }

    private static int WriteBlocks(Cell[,] cells, int row, int investors, int year)
    {
      for (int investor = 0; investor < investors; investor++)
      {
        cells[row, 0] = Cell.Of("Investor Name");
        cells[row, 1] = Cell.Of("Date");
        cells[row, 2] = Cell.Of("Transaction");
        cells[row, 3] = Cell.Of("IRR");
        row++;

        for (int flow = 0; flow < DocumentBlockRows; flow++)
        {
          cells[row, 0] = Cell.Of(Investor(investor));
          cells[row, 1] = Cell.Of(new DateTime(year + flow % 2, 1 + flow % 12, 1 + flow));
          cells[row, 2] = Cell.Of(flow % 2 == 0 ? "Contribution" : "Distribution");
          cells[row, 3] = Cell.Of(0.03 + flow / 100.0);
          row++;
        }

        row++;   // the blank separator the repeat spans
      }

      return row;
    }

    private static int[,] Ints(int rows)
    {
      var values = new int[rows, Columns];

      for (int row = 0; row < rows; row++)
        for (int column = 0; column < Columns; column++)
          values[row, column] = row * Columns + column;

      return values;
    }

    private static object?[,] Objects(int rows)
    {
      var values = new object?[rows, Columns];

      for (int row = 0; row < rows; row++)
        for (int column = 0; column < Columns; column++)
          values[row, column] = ((row * Columns + column) % 5) switch
          {
            0 => Label(row + column),
            1 => (object)(row + column),
            2 => new DateTime(2020, 1, 1).AddDays((row + column) % 3650),
            3 => (row + column) % 2 == 0,
            _ => null,
          };

      return values;
    }

    /// <summary>Kinds cycle, including blanks, so a sweep pays for every branch in proportion.</summary>
    private static Cell MixedCell(int i) => (i % 5) switch
    {
      0 => Cell.Of(Label(i)),
      1 => Cell.Of(i * 1.5),
      2 => Cell.Of(new DateTime(2020, 1, 1).AddDays(i % 3650)),
      3 => Cell.Of(i % 2 == 0),
      _ => Cell.Blank,
    };

    private static string Label(int i) => "row-" + i.ToString(CultureInfo.InvariantCulture);

    private static string Investor(int i) => "Investor " + i.ToString(CultureInfo.InvariantCulture);

  }
}
