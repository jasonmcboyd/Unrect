using System;
using System.Collections;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One row or one column of cells, indexed along its own axis and knowing where it sits, so a
  /// caller's own complaints about the data can cite a cell the way the framework's do.
  /// </summary>
  public sealed class CellStrip : IReadOnlyList<CellValue>
  {
    internal CellStrip(Plane<ICellValues> space, Orientation orientation, ProjectionContext context)
    {
      Space = space;
      Orientation = orientation;
      Context = context;
    }

    /// <summary>The strip's own extent — one cell wide or one cell tall, depending on its orientation.</summary>
    public Plane<ICellValues> Space { get; }

    private Orientation Orientation { get; }

    /// <summary>
    /// The context the strip was projected in — where it sits and, through it, how a typed read that
    /// disagrees with the data reports a failure against the declaration that named this strip.
    /// </summary>
    private ProjectionContext Context { get; }

    /// <summary>Where the strip starts, relative to the space <c>Map</c> was called with.</summary>
    private Offset Origin => Context.Origin;

    /// <summary>
    /// How many cells the strip holds. A row's length is its extent's width, which is free even
    /// where the height is still being discovered; a column's is that height, and asking settles
    /// it.
    /// </summary>
    public int Count => Orientation == Orientation.Horizontal ? Space.Width : Space.Area.Height;

    /// <summary>The cell at <paramref name="index"/> along the strip's own axis; an index outside it throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    public CellValue this[int index]
    {
      get
      {
        Validate(index);

        return Orientation == Orientation.Horizontal ? Space.CellAt(index, 0) : Space.CellAt(0, index);
      }
    }

    /// <summary>
    /// The address of the strip's first cell. It carries the extent it was found in, so on one
    /// whose height is still being discovered this settles the bound.
    /// </summary>
    public ProjectionLocation Location => ProjectionLocation.At(Origin, Space.Area.Size);

    /// <summary>The address of one cell of the strip, for citing it in a message.</summary>
    public ProjectionLocation AddressOf(int index)
    {
      Validate(index);

      return ProjectionLocation.At(Origin + Step(index), Space.Area.Size);
    }

    /// <summary>The strip's cells, in order.</summary>
    public IEnumerator<CellValue> GetEnumerator()
    {
      for (var index = 0; index < Count; index++)
        yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // The typed reads. A strip has no captions — it is positional — so a cell is named by its index
    // along the strip. Each method reads the cell through the one canonical accessor for its kind,
    // the same one the matching leaf uses, so a Decimal() leaf and strip.Decimal(i) describe a bad
    // cell with the very same sentence; the strip supplies the A1 the leaf's coordinate-less
    // CellValue could not. An index outside the strip throws as the indexer does; a kind mismatch or
    // a number that will not fit throws the shared reading diagnostic, carrying the declaration path.

    /// <summary>The <c>Text</c> at <paramref name="index"/>; a non-text cell throws the reading diagnostic.</summary>
    public string Text(int index) => Read(index, CellKind.Text, CellReading.AsString);

    /// <summary>The <c>Decimal</c> at <paramref name="index"/>; a non-number, or a number no decimal holds, throws.</summary>
    public decimal Decimal(int index) => Read(index, CellKind.Number, CellReading.AsDecimal);

    /// <summary>The whole-number <c>Integer</c> at <paramref name="index"/>; a non-number, or a number that is not a whole 32-bit one, throws.</summary>
    public int Integer(int index) => Read(index, CellKind.Number, CellReading.AsInteger);

    /// <summary>The <c>Double</c> at <paramref name="index"/>; a non-number cell throws.</summary>
    public double Double(int index) => Read(index, CellKind.Number, CellReading.AsDouble);

    /// <summary>The <c>Date</c> at <paramref name="index"/>; a non-temporal cell throws.</summary>
    public DateTime Date(int index) => Read(index, CellKind.Temporal, CellReading.AsDateTime);

    /// <summary>The <c>Boolean</c> at <paramref name="index"/>; a non-boolean cell throws.</summary>
    public bool Boolean(int index) => Read(index, CellKind.Boolean, CellReading.AsBoolean);

    // The blank-tolerant twins — the strip's spelling of the leaves' OrBlank. A blank cell reads as
    // null with no complaint; a cell of the wrong kind still throws, because a blank says something
    // about the data and a wrong kind says something about the format. They are separate methods
    // rather than an OrBlank() chained onto the reads above, because a read returns a value, not a
    // projection there is anything left to modify.

    /// <summary>The <c>Text</c> at <paramref name="index"/>, or null when the cell is blank.</summary>
    public string? TextOrBlank(int index) => this[index].IsBlank ? null : Text(index);

    /// <summary>The <c>Decimal</c> at <paramref name="index"/>, or null when the cell is blank.</summary>
    public decimal? DecimalOrBlank(int index) => ReadOrBlank(index, CellKind.Number, CellReading.AsDecimal);

    /// <summary>The whole-number <c>Integer</c> at <paramref name="index"/>, or null when the cell is blank.</summary>
    public int? IntegerOrBlank(int index) => ReadOrBlank(index, CellKind.Number, CellReading.AsInteger);

    /// <summary>The <c>Double</c> at <paramref name="index"/>, or null when the cell is blank.</summary>
    public double? DoubleOrBlank(int index) => ReadOrBlank(index, CellKind.Number, CellReading.AsDouble);

    /// <summary>The <c>Date</c> at <paramref name="index"/>, or null when the cell is blank.</summary>
    public DateTime? DateOrBlank(int index) => ReadOrBlank(index, CellKind.Temporal, CellReading.AsDateTime);

    /// <summary>The <c>Boolean</c> at <paramref name="index"/>, or null when the cell is blank.</summary>
    public bool? BooleanOrBlank(int index) => ReadOrBlank(index, CellKind.Boolean, CellReading.AsBoolean);

    private T Read<T>(int index, CellKind kind, CellReader<T> read) => Convert(this[index], index, kind, read);

    private T? ReadOrBlank<T>(int index, CellKind kind, CellReader<T> read) where T : struct
    {
      var cell = this[index];
      return cell.IsBlank ? (T?)null : Convert(cell, index, kind, read);
    }

    /// <summary>
    /// The kind assertion and the conversion, both spoken by <see cref="CellReading"/>, with the
    /// A1 built from the strip's own coordinates. The address is a thunk because it is only ever
    /// needed on the failing path.
    /// </summary>
    private T Convert<T>(CellValue cell, int index, CellKind kind, CellReader<T> read)
    {
      string At() => AddressOf(index).A1;

      if (cell.Kind != kind)
        throw Context.Failure(CellReading.WrongKind(kind, cell, At()), Space);

      if (!read(cell, At, out var value, out var conversion))
        throw Context.Failure(conversion!, Space);

      return value;
    }

    private Offset Step(int index) => Orientation == Orientation.Horizontal ? new Offset(index, 0) : new Offset(0, index);

    private void Validate(int index)
    {
      if (index < 0 || index >= Count)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The strip has {Count} cells.");
    }
  }
}
