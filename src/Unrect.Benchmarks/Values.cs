using System;

using BenchmarkDotNet.Attributes;

using Unrect.Core;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The value model itself: adapting an array into cells, and reading a million of them back.
  /// Every row here is a tight sweep over a flat <see cref="CellValue"/> array rather than over a
  /// space, so nothing but the representation is in the measurement -- no indexer, no bounds check,
  /// no shape.
  ///
  /// <para><b>This family exists for a specific pending question.</b> <c>CellValue</c> is a sealed
  /// class today: every cell is a heap object and a grid is an array of references. Turning it into
  /// a struct trades allocation and indirection for copying, and the trade is not obviously good in
  /// either direction -- a struct with a string field, a decimal, a DateTime and a discriminator is
  /// not small. These rows are the evidence for that decision: adaptation cost (how much is
  /// allocated to build a grid), sweep cost (what a read costs once built), equality (which a struct
  /// changes from a reference-first comparison to a field-wise one), and the blankness predicates
  /// that every strategy in the library calls per cell.</para>
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
    private CellValue[] _numbers = default!;
    private CellValue[] _text = default!;
    private CellValue[] _mixed = default!;

    [GlobalSetup]
    public void Setup()
    {
      _ints = CanonicalSpaces.MegaInts;
      _objects = CanonicalSpaces.MegaObjects;
      _numbers = CanonicalSpaces.MegaNumberCells;
      _text = CanonicalSpaces.MegaTextCells;
      _mixed = CanonicalSpaces.MegaMixedCells;
    }

    /// <summary>Adapting a million numbers: the allocation floor for a grid this size.</summary>
    [Benchmark]
    public int Create_FromInts() => GridSpace.Create(_ints, isBlank: v => v == 0).Area.Height;

    /// <summary>The same, from a mixed object array: one cell at a time through a mapping lambda.</summary>
    [Benchmark]
    public int Create_FromObjects() => GridSpace.Create(_objects, Adapt).Area.Height;

    /// <summary>A million checked numeric reads -- the accessor a money column goes through.</summary>
    [Benchmark]
    public decimal Sweep_GetDecimal()
    {
      decimal total = 0m;

      foreach (var cell in _numbers)
        total += cell.GetDecimal();

      return total;
    }

    /// <summary>A million string reads.</summary>
    [Benchmark]
    public int Sweep_GetString()
    {
      var total = 0;

      foreach (var cell in _text)
        total += cell.GetString().Length;

      return total;
    }

    /// <summary>
    /// The kind-dispatched read: every cell tried as text, then as a number. Kinds cycle in the
    /// fixture, so this pays the mispredicted branch a real sheet pays.
    /// </summary>
    [Benchmark]
    public long Sweep_TryGetByKind()
    {
      long total = 0;

      foreach (var cell in _mixed)
      {
        if (cell.TryGetString() is string text)
          total += text.Length;
        else if (cell.TryGetDouble() is double number)
          total += (long)number;
      }

      return total;
    }

    /// <summary>
    /// A million equality comparisons that reach the values. Cells are compared against the one
    /// five back, not the one before: kinds cycle with period five, so adjacent cells always differ
    /// in kind and every comparison would exit on the discriminator without ever comparing a string
    /// or a number -- measuring the cheapest branch and calling it equality.
    /// </summary>
    [Benchmark]
    public int Sweep_Equality()
    {
      var equal = 0;

      for (int i = 5; i < _mixed.Length; i++)
        if (_mixed[i].Equals(_mixed[i - 5]))
          equal++;

      return equal;
    }

    /// <summary>
    /// The blankness predicates. Every size and offset strategy in the library calls one of these
    /// per cell, so this row is the multiplier on every scan the Strategies family measures.
    /// </summary>
    [Benchmark]
    public int Sweep_Blankness()
    {
      var present = 0;

      foreach (var cell in _mixed)
        if (cell.HasValue && !cell.IsBlank)
          present++;

      return present;
    }

    private static CellValue Adapt(object? value) => value switch
    {
      null => CellValue.Blank,
      string text => CellValue.Of(text),
      int number => CellValue.Of(number),
      double number => CellValue.Of(number),
      decimal number => CellValue.Of(number),
      DateTime date => CellValue.Of(date),
      bool flag => CellValue.Of(flag),
      _ => CellValue.Of(value.ToString()),
    };
  }
}
