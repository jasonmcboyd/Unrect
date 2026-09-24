namespace Unrect.Core
{
  /// <summary>
  /// The questions any point answers, whatever space it addresses a cell of — the canonical surface
  /// of <see cref="ISpace"/> asked of one cell. A point is an address and holds nothing: every
  /// question is an extension that forwards to the space, so a point reads whatever its space reads
  /// and a space that can do more adds extensions of its own beside these, never members to the point.
  /// </summary>
  public static class CanonicalReads
  {
    /// <summary>Whether the cell carries no value at all.</summary>
    /// <typeparam name="TSpace">The space the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISpace
      => point.Space.IsBlank(point.Column, point.Row);

    /// <summary>The negation of <see cref="IsBlank{TSpace}"/>.</summary>
    /// <typeparam name="TSpace">The space the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool HasValue<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISpace
      => !point.IsBlank();

    /// <summary>
    /// What the cell says, or null when it is blank — see <see cref="ISpace.AsText"/>. Rendering a
    /// cell that is not text may allocate.
    /// </summary>
    /// <typeparam name="TSpace">The space the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string? AsText<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISpace
      => point.Space.AsText(point.Column, point.Row);
  }
}
