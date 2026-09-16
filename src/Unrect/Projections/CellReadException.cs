using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A sentence about a cell, with the cell's address left as a hole to be filled in. A backend that
  /// reads a cell knows what went wrong and where the cell is in its own space; only the projection
  /// layer knows what to call that place, so the address arrives last.
  /// </summary>
  /// <param name="at">The cell's address, as the projection layer renders it.</param>
  /// <returns>The whole sentence, ready to be read by someone holding a spreadsheet.</returns>
  public delegate string CellProblem(string at);

  /// <summary>
  /// A read of one cell that disagreed with what the declaration asked for — the wrong kind, a
  /// number that will not fit. Thrown by whatever read the cell, caught by the projection that
  /// called it, and rethrown as a <see cref="ProjectionException"/> carrying the declaration path
  /// and the cell's A1 address.
  /// <para>
  /// <b>It is not a fault.</b> A cell of the wrong kind is a statement about the data, so a
  /// tolerance boundary may absorb it — which is why this is an ordinary exception rather than
  /// anything on the engine's fault list.
  /// </para>
  /// <para>
  /// The address travels as a <see cref="Point{TSpace}"/> over the canonical surface, because the
  /// exception cannot be generic in the space the reader was written over.
  /// </para>
  /// </summary>
  public sealed class CellReadException : Exception
  {
    /// <summary>
    /// A failed read of the cell at <paramref name="at"/>, described by
    /// <paramref name="problem"/>.
    /// </summary>
    /// <param name="at">The cell that was read.</param>
    /// <param name="problem">What was wrong with it, as a sentence awaiting an address.</param>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> is null.</exception>
    public CellReadException(Point<ISpace> at, CellProblem problem)
    {
      At = at;
      Problem = problem ?? throw new ArgumentNullException(nameof(problem));
    }

    /// <summary>The cell that was read.</summary>
    public Point<ISpace> At { get; }

    /// <summary>What was wrong with it, as a sentence awaiting an address.</summary>
    public CellProblem Problem { get; }

    /// <summary>
    /// The problem, addressed in A1. A point's coordinates are its space's own and a space is the
    /// whole sheet, so the address is already the one a reader has in front of them — the same
    /// sentence a <see cref="ProjectionException"/> carries, less the declaration path.
    /// <para>
    /// Rendering it asks the point's space for its <see cref="ISpace.Area"/>, which that contract
    /// requires to be known rather than measured: an exception's message must not fail, and a
    /// message that went and read a file to address a cell would be a second failure raised from
    /// inside the first.
    /// </para>
    /// </summary>
    public override string Message => Problem(ProjectionLocation.At(At).A1);
  }
}
