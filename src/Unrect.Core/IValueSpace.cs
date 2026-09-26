namespace Unrect.Core
{
  /// <summary>
  /// The value facet: a space whose cells hold a value of one type, beside the text every space
  /// says. What the type is, is the space's to choose — a CLR primitive for an in-memory grid, a
  /// backend's own sum type for a store with several kinds of cell — and the questions a value
  /// answers live on that type, never here.
  /// <para>
  /// One member, because one is all the facet is: the value itself, unrendered. A declaration
  /// written over the canonical surface reads through this unchanged; one that wants the value
  /// says so in its own type, and then <c>Point&lt;IValueSpace&lt;T&gt;&gt;.Value()</c> is there.
  /// </para>
  /// </summary>
  /// <typeparam name="TValue">What every cell of the space holds.</typeparam>
  public interface IValueSpace<out TValue> : ISpace
  {
    /// <summary>
    /// The value in the cell at <paramref name="column"/>, <paramref name="row"/>, 0-based from this
    /// space's own origin. A blank cell answers with whatever the source calls empty — blankness is
    /// <see cref="ISpace.IsBlankAt"/>'s question, not this one's.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    TValue ValueAt(int column, int row);
  }
}
