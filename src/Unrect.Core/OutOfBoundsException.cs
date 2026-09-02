using System;

namespace Unrect.Core
{
  /// <summary>
  /// A strategy asked for more space than it was given, or a subspace request did not fit its
  /// parent. Carries no diagnostics at this level — the shape layer above catches this and wraps it
  /// in a <c>ShapeException</c> with a declaration path and an A1 location; a substrate caller
  /// working with <see cref="ISpace"/> directly sees it bare.
  /// </summary>
  public class OutOfBoundsException : Exception
  {
  }
}
