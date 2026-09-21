using Unrect.Core;

namespace Unrect.Tests
{
  /// <summary>
  /// The derived question, asked of a space at a coordinate: a test pins what a space answers, and
  /// says it the way the point does.
  /// </summary>
  internal static class SpaceQuestions
  {
    /// <summary>Whether the cell holds text of its own — true exactly when the read would hand it back.</summary>
    public static bool IsText(this ISpace space, int column, int row)
      => space.TryGetTextAt(column, row, out _, out _);
  }
}
