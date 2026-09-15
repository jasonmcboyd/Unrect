using System;

namespace Unrect.Projections
{
  /// <summary>
  /// The engine violated one of its own invariants — never a statement about the document, so no
  /// tolerance boundary may absorb it.
  /// <para>
  /// It is the exception for a rule this library owes itself rather than one the data can break: a
  /// declaration cannot provoke it, and a reader seeing one is looking at a bug here. That is why it
  /// is on <see cref="ProjectionEngine.IsFault"/>'s list — an invariant that could be swallowed by
  /// <c>Optional</c> would report a bug in the reader as a section that is not there, which is the
  /// worst answer in the taxonomy because nothing anywhere would say so.
  /// </para>
  /// </summary>
  internal sealed class EngineInvariantException : Exception
  {
    internal EngineInvariantException(string message)
      : base(message)
    {
    }
  }
}
