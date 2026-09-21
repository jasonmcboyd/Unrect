using System;
using System.IO;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// The rulings every machine and the placement machine apply alike: what counts as a fault, how
  /// a strategy's own exception is worded, and how a size renders in a message. One place, so a leaf and a composite cannot describe the same
  /// event two ways.
  /// </summary>
  internal static class EngineRules
  {
    internal static ProjectionException AreaFailure<TSpace, TOther>(ProjectorScope<TSpace> scope, IProjectionDefinition projection, Plane<TOther> inner, Exception exception)
      where TSpace : class, ISpace
      where TOther : class, ISpace
      => exception is OutOfBoundsException
        ? scope.Failure(projection, "its area ran past the space available here", inner, null, exception)
        : scope.Failure(projection, Threw("area", exception), inner, null, exception, IsFault(exception));

    /// <summary>
    /// An exception that says something about this library or the machine, never about the data:
    /// no tolerance boundary absorbs one. Under a genuine out-of-memory condition the wrap itself
    /// may fail to allocate and the original escapes unwrapped, which is fine: an unwrapped
    /// OutOfMemoryException is not a ProjectionException, so nothing catches it either.
    /// </summary>
    internal static bool IsFault(Exception exception)
      => exception is NullReferenceException
        or IndexOutOfRangeException
        or ArgumentOutOfRangeException
        or ArgumentNullException
        or IOException                 // the disk, the network share, the workbook replaced mid-read
        or ObjectDisposedException     // a view outliving its Workbook
        or InvalidCastException        // an invariant this library owes itself; never the data
        or OutOfMemoryException        // never a statement about the data
        or CellReadException { IsFault: true };  // a read the SOURCE could not serve, not a cell that disagreed

    /// <summary>Whether <paramref name="size"/> is wider or taller than <paramref name="space"/>.</summary>
    internal static bool Exceeds<TSpace>(Size size, Plane<TSpace> space)
      where TSpace : class, ISpace
      => size.Width > space.Width || size.Height > space.Area.Height;

    internal static string Describe(Size size) => $"{size.Width}x{size.Height}";

    internal static string Threw(string what, Exception exception)
      => $"its {what} strategy threw {exception.GetType().Name}: {exception.Message}";

    /// <summary>A matcher that found nothing says what it was looking for; anything else just ran out of room.</summary>
    internal static string Missing(OutOfBoundsException exception)
      => exception is AnchorNotFoundException anchor
        ? $"{anchor.Description} exists in the available space"
        : "its offset ran past the available space";
  }
}
