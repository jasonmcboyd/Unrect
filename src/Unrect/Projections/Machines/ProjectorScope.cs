using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a machine holds of the run it belongs to, in one object: where it is in the declaration
  /// tree, the diagnostics of the run, the labels in scope, and the engine's services for starting
  /// a child behind the placement machine that feeds it. Abstract with a <c>private protected</c>
  /// constructor: only the engine derives one, so it can grow a member without breaking anyone —
  /// the same reason <c>Plane</c> and <c>Point</c> are structs. Immutable: a move in the tree is a
  /// new scope over the same run.
  /// </summary>
  /// <typeparam name="TSpace">The space the run is over.</typeparam>
  public abstract class ProjectorScope<TSpace>
    where TSpace : class, ISpace
  {
    private protected ProjectorScope(TreePosition position, DiagnosticCollector diagnostics)
    {
      Position = position ?? throw new ArgumentNullException(nameof(position));
      Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    // --- Position -----------------------------------------------------------------------------

    internal TreePosition Position { get; }

    /// <summary>Shared by reference across the whole tree of one run — the single piece of mutable state in it.</summary>
    internal DiagnosticCollector Diagnostics { get; }

    /// <summary>The definition this scope is inside, or null at the root.</summary>
    internal IProjectionDefinition? Projection => Position.Projection;

    /// <summary>The occurrence number the nearest enclosing repeat is on, or null outside one.</summary>
    internal int? Ordinal => Position.Ordinal;

    /// <summary>This scope inside <paramref name="projection"/>, which claims whatever use site was waiting for it.</summary>
    internal ProjectorScope<TSpace> Descend(IProjectionDefinition projection) => Derive(Position.Descend(projection));

    /// <summary>The root, told which definition it is applying; the identity anywhere else.</summary>
    internal ProjectorScope<TSpace> Blaming(IProjectionDefinition projection) => Derive(Position.Blaming(projection));

    /// <summary>Declares where the next child was written, for it to claim on the way in.</summary>
    internal ProjectorScope<TSpace> WithUseSite(UseSite site) => Derive(Position.WithUseSite(site));

    internal ProjectorScope<TSpace> WithIndex(int index) => Derive(Position.WithIndex(index));

    internal ProjectorScope<TSpace> WithOrdinal(int ordinal) => Derive(Position.WithOrdinal(ordinal));

    /// <summary>This scope with <paramref name="source"/> as the nearest column labels, captured at <paramref name="captureOrigin"/>.</summary>
    internal ProjectorScope<TSpace> PushLabels(ILabelSource source, Offset captureOrigin)
      => Derive(Position.PushLabels(source, captureOrigin));

    /// <summary>The nearest column labels, or null when none is in scope.</summary>
    internal LabelScope? NearestLabels() => Position.NearestLabels();

    /// <summary>This scope at <paramref name="position"/>: the engine's own type, over the same run.</summary>
    private protected abstract ProjectorScope<TSpace> Derive(TreePosition position);

    // --- Diagnostics --------------------------------------------------------------------------

    /// <summary>Where <paramref name="extent"/> starts, as an A1-style address. The region says so itself.</summary>
    internal ProjectionLocation Locate<TOther>(Plane<TOther> extent)
      where TOther : class, ISpace
      => ProjectionLocation.At(extent);

    /// <summary>A failure blaming this scope's own definition, for a machine that finds the data is not what was declared.</summary>
    internal ProjectionException Failure<TOther>(string problem, Plane<TOther> extent, Exception? inner = null)
      where TOther : class, ISpace
      => Failure(Position.Blamed(), problem, extent, null, inner);

    /// <summary>The same, carrying the fault flag.</summary>
    internal ProjectionException Failure<TOther>(string problem, Plane<TOther> extent, Exception? inner, bool isFault)
      where TOther : class, ISpace
      => Failure(Position.Blamed(), problem, extent, null, inner, isFault);

    /// <summary>
    /// A failed cell read, rethrown as this scope's own failure: the reader's sentence, addressed
    /// the way this layer addresses a cell, carrying the declaration path and the original as its
    /// inner exception. One place decides this wording, and every lambda a machine calls funnels
    /// through it.
    /// </summary>
    internal ProjectionException Reading<TOther>(CellReadException failure, Plane<TOther> extent)
      where TOther : class, ISpace
      => Failure(failure.Problem(ProjectionLocation.At(failure.At).A1), extent, failure);

    /// <summary>A failure about <paramref name="projection"/>, raised from here.</summary>
    internal ProjectionException Failure<TOther>(
      IProjectionDefinition projection,
      string problem,
      Plane<TOther> extent,
      Size? requested,
      Exception? inner,
      bool isFault = false)
      where TOther : class, ISpace
    {
      var chain = Position.Chain(projection);
      var (path, subject) = PathRenderer.Collapse(chain);

      return new ProjectionException(subject, problem, path, PathRenderer.RenderFull(chain), Locate(extent), requested, projection, inner, isFault);
    }

    /// <summary>Records something about <paramref name="projection"/> that happened here.</summary>
    internal void Report<TOther>(DiagnosticSeverity severity, IProjectionDefinition projection, string message, Plane<TOther> extent)
      where TOther : class, ISpace
    {
      var chain = Position.Chain(PathRenderer.Through(projection));
      var (path, subject) = PathRenderer.Collapse(chain);

      Diagnostics.Add(new ProjectionDiagnostic(severity, subject, message, path, PathRenderer.RenderFull(chain), Locate(extent)));
    }

    /// <summary>
    /// Records something a failure caused, keeping the failure's own path and location so the
    /// diagnostic points at what went wrong rather than at whatever tolerated it.
    /// </summary>
    internal void Report(DiagnosticSeverity severity, ProjectionException failure, string? subject = null, string? message = null)
      => Diagnostics.Add(new ProjectionDiagnostic(
        severity,
        subject ?? failure.Subject,
        message ?? failure.Problem,
        failure.Path,
        failure.FullPath,
        failure.Location));

    // --- The engine ---------------------------------------------------------------------------

    /// <summary>The axis the driver offers spans along.</summary>
    internal abstract Orientation Driver { get; }

    /// <summary>Where a machine started under this scope would begin — used only when it is never offered a span.</summary>
    internal abstract Plane<TSpace> Anchor { get; }

    /// <summary>This scope at <paramref name="anchor"/>, driving along <paramref name="driver"/>, under the same handle.</summary>
    internal abstract ProjectorScope<TSpace> At(Plane<TSpace> anchor, Orientation driver);

    /// <summary>
    /// The scope a handle hands the machine it wraps: children started through it report to
    /// <paramref name="owner"/>, which is how the tree of open machines is kept by the engine
    /// alone, with nothing asked of a node.
    /// </summary>
    internal abstract ProjectorScope<TSpace> Within(IChildRegistry<TSpace> owner, Plane<TSpace> anchor, Orientation driver);

    /// <summary>
    /// Starts <paramref name="definition"/> as a child at <paramref name="edge"/>, behind the
    /// placement machine that resolves its offset and area and feeds it. <paramref name="anchor"/> is
    /// where the child would begin — the parent's current position — used when no span is ever
    /// offered. <paramref name="occurrence"/> stamps a repeat's or tiler's index and ordinal.
    /// <paramref name="strict"/> false makes a placement failure a refusal the parent reads off the
    /// handle rather than a thrown failure — a repeat's stopping condition.
    /// </summary>
    internal abstract IChildHandle<TSpace, T> Start<T>(Child edge, IProjectionDefinition<TSpace, T> definition, Plane<TSpace> anchor, int? occurrence = null, bool strict = true, bool inheritSite = false);

    /// <summary>Drives <paramref name="machine"/> over <paramref name="region"/> along <paramref name="along"/> (or as one span when null) and closes it.</summary>
    internal abstract Settlement<T> Drive<T>(IProjector<TSpace, T> machine, Plane<TSpace> region, Orientation? along);
  }
}
