namespace Unrect.Core
{
  /// <summary>
  /// A space whose cells are values of one CLR type: the in-memory grid's own surface, alongside the
  /// canonical four every space answers.
  /// <para>
  /// One member, because one is all a homogeneous grid knows that <see cref="ISpace"/> does not —
  /// the value itself, unrendered. A declaration written over the canonical surface reads through
  /// this unchanged; a declaration that wants the value says so in its own type, and then
  /// <c>Point&lt;IValueCells&lt;T&gt;&gt;.Value()</c> is there.
  /// </para>
  /// </summary>
  /// <typeparam name="T">What every cell of the space holds.</typeparam>
  public interface IValueCells<out T> : ISpace
  {
    /// <summary>
    /// The value in the cell at <paramref name="column"/>, <paramref name="row"/>, 0-based from this
    /// space's own origin. A blank cell answers with whatever the source calls empty — blankness is
    /// <see cref="ISpace.IsBlank"/>'s question, not this one's.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    T ValueAt(int column, int row);
  }
}
