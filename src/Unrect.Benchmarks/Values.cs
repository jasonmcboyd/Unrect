using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

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
    private ICellSpace _numbers = default!;
    private ICellSpace _text = default!;
    private ICellSpace _mixed = default!;
    private Plane<ICellSpace> _plane;
    private Plane<ISpace> _numericPlane;
    private IRowStrategy _erasedRule = default!;
    private IRowStrategy _typedRule = default!;

    [GlobalSetup]
    public void Setup()
    {
      _ints = CanonicalSpaces.MegaInts;
      _objects = CanonicalSpaces.MegaObjects;
      _numbers = CanonicalSpaces.MegaDenseNumeric;
      _text = CanonicalSpaces.MegaDenseText;
      _mixed = CanonicalSpaces.MegaDenseMixed;
      _plane = Plane<ICellSpace>.Of(_mixed);
      _numericPlane = Plane<ISpace>.Of(_numbers);

      // Built once: lowering happens where a declaration is written, so what the two predicate rows
      // measure is evaluation. Both rules run to the bottom of the dense numeric grid, asking every
      // one of its million cells, which is the output to check when this fixture changes.
      _erasedRule = RowStrategies.TakeRowsWhileAll(cell => !cell.IsBlank());
      _typedRule = ProjectionBuilders<ICellSpace>.TakeRowsWhileAll(cell => !cell.IsBlank()).Strategy;
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
          if (_mixed.IsBlankAt(column, row))
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
          if (_mixed.ValueAt(column, row).Kind == CellKind.Text)
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
          total += _text.AsTextAt(column, row)!.Length;

      return total;
    }

    /// <summary>
    /// A million checked numeric reads — the accessor a money column goes through, asked of the
    /// sheet as a <c>Decimal()</c> leaf asks it: the double the sheet holds, then the conversion.
    /// </summary>
    [Benchmark]
    public decimal Decimal_Million()
    {
      decimal total = 0m;

      for (var row = 0; row < CanonicalSpaces.MegaRows; row++)
        for (var column = 0; column < CanonicalSpaces.Columns; column++)
          if (_numbers.ValueAt(column, row).TryGetNumber(out var value))
            total += (decimal)value;

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
    /// A million cell predicates evaluated by a rule from the strategy calculus — the erased half of
    /// the pair below, and the baseline a typed predicate is measured against. The rule accepts
    /// every row of a grid with no blank in it, so every cell is asked.
    /// </summary>
    [Benchmark]
    public int Predicate_Million() => _erasedRule.SelectRows(_numericPlane);

    /// <summary>
    /// The same million evaluations of the same question, through the rule a declaration writes
    /// (<c>TakeRowsWhileAll(p =&gt; !p.IsBlank())</c> over a file scoped to a sheet): the predicate is
    /// lowered once at construction, and each evaluation carries a cast back to the space it named.
    ///
    /// <para>Its pair is <see cref="Predicate_Million"/>, which runs the same scan over the same
    /// cells with nothing lowered, so the difference between the two rows is the cast and nothing
    /// else. It is per point on purpose: hoisting it to the measurement would need a wrapping
    /// strategy, and a wrapper is what would drop the incremental scan the engine type-tests for.
    /// The predicate is a canonical question rather than a kinded one so that the two rules do the
    /// same work.</para>
    /// </summary>
    [Benchmark]
    public int TypedPredicate_Million() => _typedRule.SelectRows(_numericPlane);

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
