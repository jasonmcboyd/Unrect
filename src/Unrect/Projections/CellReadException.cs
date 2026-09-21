using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A read of one cell that disagreed with what the declaration asked for — the wrong kind, a
  /// number that will not fit. Thrown by whatever read the cell, caught by the projection that
  /// called it, and rethrown as a <see cref="ProjectionException"/> carrying the declaration path
  /// and the cell's A1 address.
  /// <para>
  /// <b>It is not a fault, unless it says it is.</b> A cell of the wrong kind is a statement about
  /// the data, so a tolerance boundary may absorb it. A read that failed because of the SOURCE —
  /// a streamed row that has already been released — says nothing about the data at all, and is
  /// raised with <see cref="IsFault"/> set: <c>Optional</c>, <c>Else</c> and <c>Choice</c> let it
  /// through, because absorbing it would report a read that could not be made as a section that
  /// is not there.
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
    public CellReadException(Point<ISpace> at, CellProblem problem)
      : this(at, problem, isFault: false)
    {
    }

    /// <summary>
    /// A failed read of the cell at <paramref name="at"/>, which is a fault when
    /// <paramref name="isFault"/> says the failure is the source's and not the cell's.
    /// </summary>
    /// <param name="at">The cell that was read.</param>
    /// <param name="problem">What was wrong, as a sentence awaiting an address.</param>
    /// <param name="isFault">Whether the read failed for a reason that says nothing about the data.</param>
    public CellReadException(Point<ISpace> at, CellProblem problem, bool isFault)
    {
      At = at;
      Problem = problem;
      IsFault = isFault;
    }

    /// <summary>The cell that was read.</summary>
    public Point<ISpace> At { get; }

    /// <summary>What was wrong with it, as a sentence awaiting an address.</summary>
    public CellProblem Problem { get; }

    /// <summary>
    /// Whether the read failed for a reason that says nothing about the data — the source could no
    /// longer answer. No tolerance boundary absorbs such a failure.
    /// </summary>
    public bool IsFault { get; }

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
    public override string Message => Problem.Render(ProjectionLocation.At(At).A1);
  }
}
