using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Reading a cell as the text it holds — <c>row["Name"].Text()</c> — over any space at all, since
  /// held text is the one kind every space answers for.
  /// <para>
  /// A cell that holds none throws <see cref="CellReadException"/>, which is not a fault: the
  /// projection reading the cell catches it and rethrows it with the declaration path and the cell's
  /// address, and a tolerance boundary may absorb it like any other statement about the data.
  /// </para>
  /// </summary>
  public static class TextReads
  {
    /// <summary>The text the cell holds; a cell that holds none throws the reading diagnostic.</summary>
    /// <typeparam name="TSpace">The space the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string Text<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISpace
      => point.TryGetText(out var value, out var problem)
        ? value
        : throw new CellReadException(point.Erased(), problem!.Value);

    /// <summary>The text the cell holds, or null when the cell is blank. A cell that holds something else still throws.</summary>
    /// <typeparam name="TSpace">The space the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string? TextOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISpace
      => point.IsBlank() ? null : point.Text();
  }
}
