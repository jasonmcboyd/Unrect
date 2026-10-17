using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Fluent
{
  /// <summary>
  /// The next cell of a layout, read: <c>v.Decimal()</c> is <c>v.Next(Point().Decimal())</c>, the
  /// line that dominates a real declaration spelled as short as it can be. These seven read over
  /// any <see cref="ISpace"/>; a backend's own (<c>v.Formula()</c>) sits beside them, constrained to
  /// the space that carries it, so what a cursor can read is exactly what its space answers.
  /// </summary>
  public static class LayoutCursorReads
  {
    /// <summary>The next cell, read as the text it holds; see <see cref="PointProjectionReads.Text{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static string Text<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, ISpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().Text(), declared: null);

    /// <summary>The next cell, read as a number; see <see cref="PointProjectionReads.Double{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static double Double<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, ISpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().Double(), declared: null);

    /// <summary>The next cell, read as a decimal; see <see cref="PointProjectionReads.Decimal{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static decimal Decimal<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, ISpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().Decimal(), declared: null);

    /// <summary>The next cell, read as a whole number; see <see cref="PointProjectionReads.Integer{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static int Integer<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, ISpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().Integer(), declared: null);

    /// <summary>The next cell, read as a date or time; see <see cref="PointProjectionReads.Date{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static DateTime Date<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, ISpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().Date(), declared: null);

    /// <summary>The next cell, read as a boolean; see <see cref="PointProjectionReads.Boolean{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static bool Boolean<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, ISpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().Boolean(), declared: null);

    /// <summary>The next cell, read as what it says; see <see cref="PointProjectionReads.AsText{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static string AsText<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, ISpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().AsText(), declared: null);
  }
}
