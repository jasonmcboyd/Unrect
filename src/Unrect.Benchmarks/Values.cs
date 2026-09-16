using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The substrate itself: adapting an array into cells, and reading a million of them back through
  /// the locators every declaration goes through.
  ///
  /// <para><b>This family exists for the point substrate.</b> A region is no longer an object: it is
  /// a <see cref="Plane{TSpace}"/>, three fields of arithmetic, and a cell is a
  /// <see cref="Point{TSpace}"/> minted from one. Slicing and minting are therefore on every hot
  /// path in the library, and the rows here are the evidence for that trade — what a mint costs,
  /// what a slice costs, and what each of the four canonical questions and one kinded read cost a
  /// million times over.</para>
  ///
  /// <para>The reads go through the space rather than through a flat array of cells, which is the
  /// deliberate change: the array was the representation when a grid held one, and what a strategy
  /// or a leaf actually calls is a root-coordinate read on a space.</para>
  ///
  /// <para>Sweeps accumulate into a returned value rather than discarding: BenchmarkDotNet only
  /// guarantees a benchmark's work survives dead-code elimination if the result leaves the
  /// method.</para>
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("Values")]
  public class Values
  {
    private int[,] _ints = default!;
    private object?[,] _objects = default!;
    private ISheetCells _numbers = default!;
    private ISheetCells _text = default!;
    private ISheetCells _mixed = default!;
    private Plane<ISheetCells> _plane;

    [GlobalSetup]
    public void Setup()
    {
      _ints = CanonicalSpaces.MegaInts;
      _objects = CanonicalSpaces.MegaObjects;
      _numbers = CanonicalSpaces.MegaDenseNumeric;
      _text = CanonicalSpaces.MegaDenseText;
      _mixed = CanonicalSpaces.MegaDenseMixed;
      _plane = Plane<ISheetCells>.Of(_mixed);
    }

    /// <summary>Adapting a million numbers: the allocation floor for a canonical grid this size.</summary>
    [Benchmark]
    public int Create_FromInts() => GridSpace.Create(_ints, isBlank: v => v == 0).Area.Height;

    /// <summary>
    /// The same from a mixed object array, through the kinded adapter: one cell at a time, each
    /// deciding its own kind. The floor for a sheet a script builds without a file.
    /// </summary>
    [Benchmark]
    public int Create_FromObjects() => SheetGrid.Of(_objects).Area.Height;

    /// <summary>
    /// A million blankness questions. Every size and offset strategy in the library asks one per
    /// cell, so this row is the multiplier on every scan the Strategies family measures.
    /// </summary>
    [Benchmark]
    public int IsBlank_Million()
    {
      var blank = 0;

      for (var row = 0; row < CanonicalSpaces.MegaRows; row++)
        for (var column = 0; column < CanonicalSpaces.Columns; column++)
          if (_mixed.IsBlank(column, row))
            blank++;

      return blank;
    }

    /// <summary>
    /// A million "is this cell's text its own value" questions — what every matcher asks before it
    /// compares anything.
    /// </summary>
    [Benchmark]
    public int IsText_Million()
    {
      var text = 0;

      for (var row = 0; row < CanonicalSpaces.MegaRows; row++)
        for (var column = 0; column < CanonicalSpaces.Columns; column++)
          if (_mixed.IsText(column, row))
            text++;

      return text;
    }

    /// <summary>A million renderings, over a grid where every cell carries its own string.</summary>
    [Benchmark]
    public int AsText_Million_Text()
    {
      var total = 0;

      for (var row = 0; row < CanonicalSpaces.MegaRows; row++)
        for (var column = 0; column < CanonicalSpaces.Columns; column++)
          total += _text.AsText(column, row)!.Length;

      return total;
    }

    /// <summary>
    /// A million checked numeric reads — the accessor a money column goes through, asked of the
    /// sheet exactly as a <c>Decimal()</c> leaf asks it.
    /// </summary>
    [Benchmark]
    public decimal Decimal_Million()
    {
      decimal total = 0m;

      for (var row = 0; row < CanonicalSpaces.MegaRows; row++)
        for (var column = 0; column < CanonicalSpaces.Columns; column++)
          if (_numbers.DecimalAt(column, row, out var value, out _))
            total += value;

      return total;
    }

    /// <summary>
    /// A million points minted from one plane: the bounds check and the three-field copy that every
    /// cell a view hands out now costs.
    /// </summary>
    [Benchmark]
    public int Point_Mint_Million()
    {
      var total = 0;

      for (var row = 0; row < CanonicalSpaces.MegaRows; row++)
        for (var column = 0; column < CanonicalSpaces.Columns; column++)
          total += _plane[column, row].Column;

      return total;
    }

    /// <summary>
    /// A million slices: the arithmetic a composite does where it used to allocate a subspace. One
    /// row's worth of the plane, cut a million times.
    /// </summary>
    [Benchmark]
    public int Slice_Million()
    {
      var total = 0;
      var row = new Area(CanonicalSpaces.Columns, 1);

      for (var i = 0; i < CanonicalSpaces.MegaCells; i++)
        total += _plane.Slice(new Offset(0, i % CanonicalSpaces.MegaRows), row).Width;

      return total;
    }
  }
}
