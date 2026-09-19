using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The layout composites: children declared by calling <c>Next</c> on a cursor, in the order they
  /// appear on the sheet, and the result built by the combiner handed to <c>Build</c> from what
  /// they read. They differ in one thing only — what the space between children is. A flow
  /// divides its extent into bands, each child starting where the one before it left off; an
  /// overlay gives every child the same extent and lets each find its own place in it.
  /// </summary>
  public static partial class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A flow downwards. The lambda declares each child with <c>Next</c>, which hands back what the
    /// child read, and returns the result assembled from them:
    /// <code>
    /// var report = VerticalFlow(v =&gt; new Report(
    ///     Header: v.Next(reportHeader),
    ///     Rows:   v.Next(transactions)));
    /// </code>
    /// The parts are named where they are declared, and there is no arity to run out of.
    /// <para>
    /// The lambda runs <em>once when the flow is declared</em>, with each child reading as the empty
    /// value of its type, so the flow learns its children before any space exists — <c>Children</c>
    /// lists every one, in order, with the identifier it was written as — and <em>once per
    /// application</em>, with the values, to assemble the result. So a lambda may assemble, count,
    /// join and format what its children read, but may not read a cell in it or reach into an object
    /// nothing has built yet: that is refused where it is written, pointing at the leaf and at
    /// <c>Select</c>. And a lambda whose shape depends on a value — a different child asked for on
    /// the second run — is a fault when it runs.
    /// Alternation belongs to <c>Choice</c>, <c>Else</c>, and <c>Optional</c>; repetition to
    /// <c>VerticalRepeat</c>/<c>HorizontalRepeat</c>; gaps to the following projection's offset.
    /// </para>
    /// <para>
    /// Capture nothing you write to: a projection is safe to apply to many spaces at once only
    /// because everything it holds is immutable, and a lambda that increments a counter or appends
    /// to a list gives that up. It also runs inside a losing <c>Choice</c> branch, where diagnostics
    /// roll back but side effects do not.
    /// </para>
    /// <para>
    /// Hoist each child into a well-named local and let the use site name it:
    /// <c>v.Next(summary)</c> makes the child <c>'summary'</c> in every path and message, at no
    /// cost. Reserve <c>Named</c> for projections written inline, and never bake a name into a
    /// projection-returning helper — see <see
    /// cref="ProjectionExtensions.Named{TSpace, TResult}(IProjectionDefinition{TSpace, TResult}, string)"/>.
    /// </para>
    /// <para>
    /// The lambda must call <c>Next</c> at least once — a flow that declares nothing would match
    /// anything and describe nothing, and it is refused where it is written.
    /// </para>
    /// </summary>
    public static IProjectionDefinition<TSpace, T> VerticalFlow<T>(LayoutDeclaration<TSpace, T> declare)
      => new FlowDefinition<TSpace, T>(Orientation.Vertical, Layout<TSpace, T>.Declare(declare, "a flow", nameof(declare)), Placement.Default);

    /// <summary>
    /// A flow rightwards, whose children are declared with <c>Next</c>; see
    /// <see cref="VerticalFlow{T}(LayoutDeclaration{TSpace, T})"/> for what belongs in the lambda
    /// and what does not, including why children belong in well-named locals rather than written
    /// inline.
    /// </summary>
    public static IProjectionDefinition<TSpace, T> HorizontalFlow<T>(LayoutDeclaration<TSpace, T> declare)
      => new FlowDefinition<TSpace, T>(Orientation.Horizontal, Layout<TSpace, T>.Declare(declare, "a flow", nameof(declare)), Placement.Default);

    /// <summary>
    /// One extent shared by every child, each finding its own place in it — the projection for a
    /// band of the sheet whose parts are anchored to their own content rather than laid out in
    /// order:
    /// <code>
    /// var header = Overlay(o =&gt; new Header(
    ///     Entity: o.Next(entityCard),
    ///     Funds:  o.Next(fundBand)));
    /// </code>
    /// <para>
    /// Where a flow divides the space into bands and no child sees another's, an overlay hands
    /// every child the whole of it. Children are therefore independent: each is placed by its own
    /// offset and area from the overlay's origin, they may overlap, and they may read the same
    /// cells. There is no order between them and no occlusion — they read rather than paint, so
    /// nothing a child does can hide a cell from the next one. What the overlay consumes is the box
    /// that encloses wherever its children reached, so the projection that follows it starts past
    /// all of them.
    /// </para>
    /// <para>
    /// The lambda's rules are a flow's rules: see
    /// <see cref="VerticalFlow{T}(LayoutDeclaration{TSpace, T})"/> for what belongs in it, why it
    /// must capture nothing it writes to, and why computation belongs in <c>Select</c>. Hoist each
    /// child into a well-named local here too — an overlay's children are anchored to their own
    /// content rather than ordered, so the name is often all a reader has to tell them apart. It
    /// must call <c>Next</c> at least once.
    /// </para>
    /// </summary>
    public static IProjectionDefinition<TSpace, T> Overlay<T>(LayoutDeclaration<TSpace, T> declare)
      => new OverlayDefinition<TSpace, T>(Layout<TSpace, T>.Declare(declare, "an overlay", nameof(declare)), Placement.Default);
  }
}
