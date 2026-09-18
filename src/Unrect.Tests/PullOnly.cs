using System;

using Unrect.Projections;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// A fact about the pull interpreter's own mechanics — the lazy bound, rows touched, the window
  /// and the pool, the sweep announcement — that the push interpreter has no counterpart of. Runs
  /// under the pull interpreter and is skipped, with its reason, when the suite runs under push
  /// (<c>UNRECT_PUSH=1</c>). Retires with the pull interpreter.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public sealed class PullOnlyFactAttribute : FactAttribute
  {
    public PullOnlyFactAttribute(string reason)
    {
      if (ProjectionEngine.Pushing)
        Skip = $"pull interpreter only: {reason}";
    }
  }

  /// <inheritdoc cref="PullOnlyFactAttribute"/>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public sealed class PullOnlyTheoryAttribute : TheoryAttribute
  {
    public PullOnlyTheoryAttribute(string reason)
    {
      if (ProjectionEngine.Pushing)
        Skip = $"pull interpreter only: {reason}";
    }
  }
}
