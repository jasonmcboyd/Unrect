using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

namespace Unrect.Fluent
{
  /// <summary>
  /// The strategy layer's rules, reachable under three names — <see cref="Rows"/>,
  /// <see cref="Columns"/> and <see cref="Extent"/> — so the one <c>using static</c> at the top of a
  /// file really is the only import a declaration needs: <c>Until(Rows.Saying("Total"))</c>,
  /// <c>Sized(Rows.WhileAny(p =&gt; …))</c>, <c>OffsetBy(Rows.Skip(1))</c>, <c>Sized(Extent.Of(2, 3))</c>.
  /// Every member forwards to the factory in <c>Unrect.Strategies</c> of the same meaning and adds
  /// nothing; the factories remain the calculus — what a helper, a test or a composition writes when
  /// it has no space to name.
  /// <para>
  /// <b>A predicate written here reads a <see cref="Point{TSpace}"/> or a
  /// <see cref="Plane{TSpace}"/> over this file's space</b>, so it can ask a backend's own question —
  /// a cell's kind, its value — as well as the questions every space answers, and what it hands back
  /// carries that demand in its type. Nothing is annotated to get it: the space is the one named in
  /// the import.
  /// </para>
  /// </summary>
  public static partial class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// The rules about rows: where a row is (<see cref="Saying"/>, <see cref="Where"/>,
    /// <see cref="WithCell"/>, <see cref="Containing"/>), which leading rows a region takes
    /// (<see cref="Take"/>, <see cref="TakeWhile(Func{Plane{TSpace}, int, bool})"/> and kin), how many
    /// to skip in front of it (<see cref="Skip"/>, <see cref="SkipBlank"/>, <see cref="SkipWhileAll"/>,
    /// <see cref="SkipWhileAny"/>), and the extents a row rule measures (<see cref="WhileAny"/>,
    /// <see cref="WhileAnyIsNotBlank"/>, <see cref="ThenColumns"/>).
    /// </summary>
    public static class Rows
    {
      // --- Landmarks --------------------------------------------------------------------------
      //
      // A landmark only locates content and reports absence; what absence means belongs to the
      // modifier that takes it — On lands a projection ON the match, Below one beyond, Until bounds
      // a projection by one. Because a section can start at On(Rows.Saying("A")) and end at
      // Until(Rows.Saying("B")) through the same landmark, the two cannot disagree about a caption.

      /// <inheritdoc cref="RowLandmarks.RowWhere{TSpace}(Func{Plane{TSpace}, int, bool})"/>
      public static ILineLandmark<TSpace> Where(Func<Plane<TSpace>, int, bool> predicate)
        => RowLandmarks.RowWhere(Required(predicate));

      /// <inheritdoc cref="RowLandmarks.RowWithCell{TSpace}"/>
      public static ILineLandmark<TSpace> WithCell(Func<Point<TSpace>, bool> anyCell)
        => RowLandmarks.RowWithCell(Required(anyCell));

      /// <inheritdoc cref="RowLandmarks.RowSaying{TSpace}"/>
      public static ILineLandmark<TSpace> Saying(string text) => RowLandmarks.RowSaying<TSpace>(text);

      /// <summary>
      /// The first row in which some cell <em>holds</em> the text <paramref name="text"/> —
      /// whole-cell, trimmed and case-insensitive, against text cells alone, so a numeric 2024 is
      /// not a row containing "2024". <see cref="Saying"/> is the same comparison against what every
      /// cell renders.
      /// </summary>
      public static ILineLandmark<TSpace> Containing(string text)
        => RowLandmarks.RowWhere(
          CellMatching.AnyCellInRow(Holding(NotNull(text))), $"no row containing '{text}'");

      // --- Which leading rows a region takes ---------------------------------------------------

      /// <inheritdoc cref="RowStrategies.TakeRows{TSpace}"/>
      public static ILineStrategy<TSpace> Take(int count) => RowStrategies.TakeRows<TSpace>(count);

      /// <inheritdoc cref="RowStrategies.AllRows{TSpace}()"/>
      public static ILineStrategy<TSpace> All() => RowStrategies.AllRows<TSpace>();

      /// <inheritdoc cref="RowStrategies.TakeRowsWhile{TSpace}(Func{Plane{TSpace}, int, bool})"/>
      public static ILineStrategy<TSpace> TakeWhile(Func<Plane<TSpace>, int, bool> predicate)
        => RowStrategies.TakeRowsWhile(Required(predicate));

      /// <inheritdoc cref="RowStrategies.TakeRowsWhile{TSpace}(int, Func{Point{TSpace}, int, bool})"/>
      public static ILineStrategy<TSpace> TakeWhile(int column, Func<Point<TSpace>, int, bool> predicate)
        => RowStrategies.TakeRowsWhile(column, Required(predicate));

      /// <inheritdoc cref="RowStrategies.TakeRowsTo{TSpace}"/>
      public static ILineStrategy<TSpace> TakeTo(Func<Plane<TSpace>, int, bool> predicate)
        => RowStrategies.TakeRowsTo(Required(predicate));

      /// <summary>
      /// Rows up to and including the first whose cell in <paramref name="column"/> holds
      /// <paramref name="text"/> — a block that ends at its own total line, say, with the match kept.
      /// </summary>
      public static ILineStrategy<TSpace> TakeToText(int column, string text)
      {
        var matches = Holding(NotNull(text));

        return RowStrategies.TakeRowsTo<TSpace>((space, row) => matches(space[column, row]));
      }

      /// <inheritdoc cref="RowStrategies.TakeRowsWhileAll{TSpace}(Func{Point{TSpace}, bool})"/>
      public static ILineStrategy<TSpace> TakeWhileAll(Func<Point<TSpace>, bool> predicate)
        => RowStrategies.TakeRowsWhileAll(Required(predicate));

      /// <inheritdoc cref="RowStrategies.TakeRowsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/>
      public static ILineStrategy<TSpace> TakeWhileAny(Func<Point<TSpace>, bool> predicate)
        => RowStrategies.TakeRowsWhileAny(Required(predicate));

      // --- How many rows to skip in front of a region ------------------------------------------

      /// <summary>Down <paramref name="count"/> rows, blank or not — a rule value, for <c>separatedBy:</c> and <see cref="Extent.Then"/>; <c>Down(n)</c> is the placement verb.</summary>
      public static IOffsetStrategy<TSpace> Skip(int count)
        => OffsetStrategies.ExplicitOffset<TSpace>(0, NotNegative(count, nameof(count)));

      /// <inheritdoc cref="OffsetStrategies.SkipBlankRows{TSpace}"/>
      public static IOffsetStrategy<TSpace> SkipBlank() => OffsetStrategies.SkipBlankRows<TSpace>();

      /// <inheritdoc cref="OffsetStrategies.SkipRowsWhileAll{TSpace}"/>
      public static IOffsetStrategy<TSpace> SkipWhileAll(Func<Point<TSpace>, bool> predicate)
        => OffsetStrategies.SkipRowsWhileAll(Required(predicate));

      /// <inheritdoc cref="OffsetStrategies.SkipRowsWhileAny{TSpace}"/>
      public static IOffsetStrategy<TSpace> SkipWhileAny(Func<Point<TSpace>, bool> predicate)
        => OffsetStrategies.SkipRowsWhileAny(Required(predicate));

      // --- The extents a row rule measures -----------------------------------------------------

      /// <inheritdoc cref="SizeStrategies.RowsWhileAny{TSpace}"/>
      public static ISizeStrategy<TSpace> WhileAny(Func<Point<TSpace>, bool> anyCell)
        => SizeStrategies.RowsWhileAny(Required(anyCell));

      /// <inheritdoc cref="SizeStrategies.RowsWhileAnyIsNotBlank{TSpace}"/>
      public static ISizeStrategy<TSpace> WhileAnyIsNotBlank() => SizeStrategies.RowsWhileAnyIsNotBlank<TSpace>();

      /// <inheritdoc cref="SizeStrategies.RowsThenColumns{TSpace}"/>
      public static ISizeStrategy<TSpace> ThenColumns(ILineStrategy<TSpace> rows, ILineStrategy<TSpace> columns)
        => SizeStrategies.RowsThenColumns(Required(rows), Required(columns));
    }

    /// <summary>The rules about columns: the mirror of <see cref="Rows"/>, member for member.</summary>
    public static class Columns
    {
      /// <inheritdoc cref="ColumnLandmarks.ColumnWhere{TSpace}(Func{Plane{TSpace}, int, bool})"/>
      public static ILineLandmark<TSpace> Where(Func<Plane<TSpace>, int, bool> predicate)
        => ColumnLandmarks.ColumnWhere(Required(predicate));

      /// <inheritdoc cref="ColumnLandmarks.ColumnWithCell{TSpace}"/>
      public static ILineLandmark<TSpace> WithCell(Func<Point<TSpace>, bool> anyCell)
        => ColumnLandmarks.ColumnWithCell(Required(anyCell));

      /// <inheritdoc cref="ColumnLandmarks.ColumnSaying{TSpace}"/>
      public static ILineLandmark<TSpace> Saying(string text) => ColumnLandmarks.ColumnSaying<TSpace>(text);

      /// <summary>The column twin of <see cref="Rows.Containing"/>, with the same rule.</summary>
      public static ILineLandmark<TSpace> Containing(string text)
        => ColumnLandmarks.ColumnWhere(
          CellMatching.AnyCellInColumn(Holding(NotNull(text))), $"no column containing '{text}'");

      /// <inheritdoc cref="ColumnStrategies.TakeColumns{TSpace}"/>
      public static ILineStrategy<TSpace> Take(int count) => ColumnStrategies.TakeColumns<TSpace>(count);

      /// <inheritdoc cref="ColumnStrategies.AllColumns{TSpace}()"/>
      public static ILineStrategy<TSpace> All() => ColumnStrategies.AllColumns<TSpace>();

      /// <inheritdoc cref="ColumnStrategies.TakeColumnsWhile{TSpace}(Func{Plane{TSpace}, int, bool})"/>
      public static ILineStrategy<TSpace> TakeWhile(Func<Plane<TSpace>, int, bool> predicate)
        => ColumnStrategies.TakeColumnsWhile(Required(predicate));

      /// <inheritdoc cref="ColumnStrategies.TakeColumnsWhile{TSpace}(int, Func{Point{TSpace}, int, bool})"/>
      public static ILineStrategy<TSpace> TakeWhile(int row, Func<Point<TSpace>, int, bool> predicate)
        => ColumnStrategies.TakeColumnsWhile(row, Required(predicate));

      /// <inheritdoc cref="ColumnStrategies.TakeColumnsTo{TSpace}"/>
      public static ILineStrategy<TSpace> TakeTo(Func<Plane<TSpace>, int, bool> predicate)
        => ColumnStrategies.TakeColumnsTo(Required(predicate));

      /// <summary>The column twin of <see cref="Rows.TakeToText"/>: columns up to and including the first whose cell in <paramref name="row"/> holds <paramref name="text"/>.</summary>
      public static ILineStrategy<TSpace> TakeToText(int row, string text)
      {
        var matches = Holding(NotNull(text));

        return ColumnStrategies.TakeColumnsTo<TSpace>((space, column) => matches(space[column, row]));
      }

      /// <inheritdoc cref="ColumnStrategies.TakeColumnsWhileAll{TSpace}(Func{Point{TSpace}, bool})"/>
      public static ILineStrategy<TSpace> TakeWhileAll(Func<Point<TSpace>, bool> predicate)
        => ColumnStrategies.TakeColumnsWhileAll(Required(predicate));

      /// <inheritdoc cref="ColumnStrategies.TakeColumnsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/>
      public static ILineStrategy<TSpace> TakeWhileAny(Func<Point<TSpace>, bool> predicate)
        => ColumnStrategies.TakeColumnsWhileAny(Required(predicate));

      /// <summary>Right <paramref name="count"/> columns, blank or not — a rule value; <c>Right(n)</c> is the placement verb.</summary>
      public static IOffsetStrategy<TSpace> Skip(int count)
        => OffsetStrategies.ExplicitOffset<TSpace>(NotNegative(count, nameof(count)), 0);

      /// <inheritdoc cref="OffsetStrategies.SkipBlankColumns{TSpace}"/>
      public static IOffsetStrategy<TSpace> SkipBlank() => OffsetStrategies.SkipBlankColumns<TSpace>();

      /// <inheritdoc cref="OffsetStrategies.SkipColumnsWhileAll{TSpace}"/>
      public static IOffsetStrategy<TSpace> SkipWhileAll(Func<Point<TSpace>, bool> predicate)
        => OffsetStrategies.SkipColumnsWhileAll(Required(predicate));

      /// <inheritdoc cref="OffsetStrategies.SkipColumnsWhileAny{TSpace}"/>
      public static IOffsetStrategy<TSpace> SkipWhileAny(Func<Point<TSpace>, bool> predicate)
        => OffsetStrategies.SkipColumnsWhileAny(Required(predicate));

      /// <inheritdoc cref="SizeStrategies.ColumnsWhileAny{TSpace}"/>
      public static ISizeStrategy<TSpace> WhileAny(Func<Point<TSpace>, bool> anyCell)
        => SizeStrategies.ColumnsWhileAny(Required(anyCell));

      /// <inheritdoc cref="SizeStrategies.ColumnsWhileAnyIsNotBlank{TSpace}"/>
      public static ISizeStrategy<TSpace> WhileAnyIsNotBlank() => SizeStrategies.ColumnsWhileAnyIsNotBlank<TSpace>();

      /// <inheritdoc cref="SizeStrategies.ColumnsThenRows{TSpace}"/>
      public static ISizeStrategy<TSpace> ThenRows(ILineStrategy<TSpace> columns, ILineStrategy<TSpace> rows)
        => SizeStrategies.ColumnsThenRows(Required(columns), Required(rows));
    }

    /// <summary>
    /// The rules about no axis in particular: a fixed extent (<see cref="Of"/>, <see cref="Whole"/>,
    /// <see cref="None"/>), one measured by hand (<see cref="Select"/>, <see cref="Offset"/>), an
    /// anchor to the far edge (<see cref="FromRight"/>, <see cref="FromBottom"/>), and offsets in
    /// sequence (<see cref="Then"/>).
    /// </summary>
    public static class Extent
    {
      /// <inheritdoc cref="SizeStrategies.ExplicitSize{TSpace}"/>
      public static ISizeStrategy<TSpace> Of(int width, int height) => SizeStrategies.ExplicitSize<TSpace>(width, height);

      /// <inheritdoc cref="SizeStrategies.MaxSize{TSpace}"/>
      public static ISizeStrategy<TSpace> Whole() => SizeStrategies.MaxSize<TSpace>();

      /// <inheritdoc cref="SizeStrategies.MinSize{TSpace}"/>
      public static ISizeStrategy<TSpace> None() => SizeStrategies.MinSize<TSpace>();

      /// <inheritdoc cref="SizeStrategies.SelectSize{TSpace}"/>
      public static ISizeStrategy<TSpace> Select(Func<Plane<TSpace>, Size> selector)
        => SizeStrategies.SelectSize(Required(selector));

      /// <inheritdoc cref="OffsetStrategies.SelectOffset{TSpace}"/>
      public static IOffsetStrategy<TSpace> Offset(Func<Plane<TSpace>, Size> selector)
        => OffsetStrategies.SelectOffset(Required(selector));

      /// <inheritdoc cref="OffsetStrategies.FromRight{TSpace}"/>
      public static IOffsetStrategy<TSpace> FromRight(int width) => OffsetStrategies.FromRight<TSpace>(width);

      /// <inheritdoc cref="OffsetStrategies.FromBottom{TSpace}"/>
      public static IOffsetStrategy<TSpace> FromBottom(int height) => OffsetStrategies.FromBottom<TSpace>(height);

      /// <inheritdoc cref="OffsetStrategies.Then{TSpace}"/>
      public static IOffsetStrategy<TSpace> Then(params IOffsetStrategy<TSpace>[] offsets) => OffsetStrategies.Then(offsets);
    }

    private static Func<Point<TSpace>, bool> Holding(string text)
      => point => point.TryGetText(out var held) && CellMatching.TextComparer.Equals(held, text);

    private static string NotNull(string text) => text ?? throw new ArgumentNullException(nameof(text));
  }
}
