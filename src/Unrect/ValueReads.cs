using Unrect.Core;

namespace Unrect
{
  /// <summary>
  /// Reading a cell of a grid of values as the value it holds, rather than as the text it says.
  /// <para>
  /// It is an extension on the <em>point</em> and not a leaf, because there is nothing about the
  /// reading to declare: the value is simply there, the type is the space's own, and no conversion
  /// can fail. Declare the region with <c>Range</c>, <c>Row</c> or <c>Record</c> and ask each cell.
  /// </para>
  /// </summary>
  public static class ValueReads
  {
    /// <summary>
    /// The value in the cell this point addresses.
    /// <para>
    /// The receiver is a point over <see cref="IValueSpace{T}"/> itself, which is what a declaration
    /// written over a grid of values is handed: name the space as
    /// <c>IValueSpace&lt;int&gt;</c> at the top of the file and every point below it answers.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the grid's cells hold.</typeparam>
    /// <param name="point">The cell.</param>
    public static T Value<T>(this Point<IValueSpace<T>> point) => point.Space.ValueAt(point.Column, point.Row);
  }
}
