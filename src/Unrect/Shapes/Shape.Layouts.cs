namespace Unrect.Shapes
{
  /// <summary>
  /// The layout composites: children declared by calling <c>Next</c> on a cursor, in the order they
  /// appear on the sheet, with the result built where the parts are read. They differ in one thing
  /// only — what the space between children is. A flow divides its extent into bands, each child
  /// starting where the one before it left off; an overlay gives every child the same extent and
  /// lets each find its own place in it.
  /// </summary>
  public static partial class Shape
  {
    /// <summary>
    /// A flow downwards, whose children are declared by calling <c>Next</c> on the cursor and whose
    /// result the lambda builds from what they read:
    /// <c>VerticalFlow(v =&gt; new Report(Header: v.Next(header), Rows: v.Next(rows)))</c>. The parts
    /// are named where they are read, and there is no arity to run out of.
    /// <para>
    /// The lambda declares a <em>sequence of shapes</em>, nothing more. Alternation belongs to
    /// <c>Choice</c>, <c>Else</c>, and <c>Optional</c>; repetition to <c>Repeat</c>; gaps to the
    /// following shape's offset. Conditionals, loops, and arithmetic over positions inside the
    /// lambda are the row-walking this library exists to replace, and a lambda that picks a later
    /// shape from an earlier value can never be rendered or checked without a file.
    /// </para>
    /// <para>
    /// Capture nothing you write to. A shape is safe to apply to many spaces at once only because
    /// everything it holds is immutable, and a lambda that increments a counter or appends to a
    /// list gives that up. It also runs partially inside a losing <c>Choice</c> branch, where
    /// diagnostics roll back but side effects do not.
    /// </para>
    /// <para>
    /// Do the reading inside the leaf, not around it: <c>decimal.Parse(v.Next(raw))</c> that throws
    /// blames this flow at its own origin, while a projection inside the leaf blames the cell.
    /// </para>
    /// <para>
    /// Hoist each child into a well-named local and let the use site name it: <c>v.Next(summary)</c>
    /// makes the child <c>'summary'</c> in every path and message, at no cost. Reserve <c>Named</c>
    /// for shapes written inline, and never bake a name into a shape-returning helper — see
    /// <see cref="ShapeExtensions.Named{T}(IShape{T}, string)"/>.
    /// </para>
    /// <para>
    /// The lambda must call <c>Next</c> at least once — a flow that declares nothing would match
    /// anything and describe nothing — and what it declares can be enumerated only by running it, so
    /// a flow cannot be inspected without a space.
    /// </para>
    /// </summary>
    public static IShape<T> VerticalFlow<T>(Layout<T> build)
      => new FlowShape<T>(Orientation.Vertical, NotNull(build, nameof(build)), Placement.Default);

    /// <summary>
    /// A flow rightwards, whose children are declared by calling <c>Next</c> on the cursor; see
    /// <see cref="VerticalFlow{T}(Layout{T})"/> for what belongs in the lambda and what does not,
    /// including why children belong in well-named locals rather than written inline.
    /// </summary>
    public static IShape<T> HorizontalFlow<T>(Layout<T> build)
      => new FlowShape<T>(Orientation.Horizontal, NotNull(build, nameof(build)), Placement.Default);

    /// <summary>
    /// One extent shared by every child, each finding its own place in it — the shape for a band of
    /// the sheet whose parts are anchored to their own content rather than laid out in order:
    /// <c>Overlay(o =&gt; new Header(Entity: o.Next(entity), Funds: o.Next(fundBand)))</c>.
    /// <para>
    /// Where a flow divides the space into bands and no child sees another's, an overlay hands every
    /// child the whole of it. Children are therefore independent: each is placed by its own offset
    /// and area from the overlay's origin, they may overlap, and they may read the same cells. There
    /// is no order between them and no occlusion — they read rather than paint, so nothing a child
    /// does can hide a cell from the next one. What the overlay consumes is the box that encloses
    /// wherever its children reached, so the shape that follows it starts past all of them.
    /// </para>
    /// <para>
    /// The lambda's rules are a flow's rules: see <see cref="VerticalFlow{T}(Layout{T})"/> for what
    /// belongs in it, why it must capture nothing it writes to, and why parsing belongs in the leaf.
    /// Hoist each child into a well-named local here too — an overlay's children are anchored to
    /// their own content rather than ordered, so the name is often all a reader has to tell them
    /// apart. It must call <c>Next</c> at least once.
    /// </para>
    /// </summary>
    public static IShape<T> Overlay<T>(Layout<T> build)
      => new OverlayShape<T>(NotNull(build, nameof(build)), Placement.Default);
  }
}
