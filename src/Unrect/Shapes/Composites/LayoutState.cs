using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// One layout in progress: what it has taken so far, and everything that is true of a layout
  /// whatever it does with the space. What differs between layouts is one thing only — whether a
  /// child moves the next one along — so that is what the subclasses override, and the guards, the
  /// wording, and the bookkeeping around it live here where they cannot fork.
  /// </summary>
  internal abstract class LayoutState
  {
    private const string Outside = "A layout cursor cannot be used outside the layout that created it";

    /// <summary>
    /// Being outside a layout covers two different bugs, so the messages name which one:
    /// <see cref="LayoutCursor"/> refuses a cursor that never had a layout, and this class refuses
    /// one whose layout has already returned.
    /// </summary>
    internal const string NoLayout = Outside + "; this one never had a layout.";

    /// <inheritdoc cref="NoLayout"/>
    internal const string LayoutReturned = Outside + "; this one was used after its layout returned.";

    private bool _closed;

    protected LayoutState(IShape owner, ISpace extent, ShapeContext context)
    {
      Owner = owner;
      Extent = extent;
      Context = context;
    }

    protected IShape Owner { get; }
    protected ISpace Extent { get; }
    protected ShapeContext Context { get; }

    /// <summary>How many children the layout has taken.</summary>
    public int Count { get; protected set; }

    /// <summary>How much of its extent the layout used.</summary>
    public abstract Size Consumed { get; }

    /// <summary>
    /// What to say when the lambda never called <c>Next</c>; each layout supplies its own noun.
    /// </summary>
    public abstract string DeclaredNothing { get; }

    /// <summary>
    /// Takes the next child and returns what it read. <paramref name="declared"/> is the text the
    /// compiler saw at the call site, from which the child's label is inferred.
    /// </summary>
    public abstract T Next<T>(IShape<T> shape, string? declared);

    /// <summary>Ends the layout, after which no cursor may add to it.</summary>
    public void Close() => _closed = true;

    /// <summary>
    /// Lets a child into the layout, <paramref name="at"/> being where it is about to go. Both
    /// refusals are declaration bugs rather than shapes of data, so neither is absorbable.
    /// </summary>
    protected void Admit(IShape? shape, Offset at)
    {
      if (_closed)
        throw new InvalidOperationException(LayoutReturned);

      // A null shape is a hole in the declaration: it is reported where the child would have gone,
      // and no tolerance boundary may absorb it.
      if (shape is null)
        throw Context.Advance(at).Failure(
          Owner,
          $"a null shape was declared as child {Count + 1}",
          RemainingAt(at),
          null,
          null,
          isProjectionFault: true);
    }

    /// <summary>
    /// The space left at <paramref name="at"/>, or the whole extent when there is no room there to
    /// slice. A hole in the declaration has to be reportable from any position the layout reached,
    /// so the message and the location outrank an exact availability figure.
    /// </summary>
    protected ISpace RemainingAt(Offset at)
      => at.Width > Extent.Area.Width || at.Height > Extent.Area.Height
        ? Extent
        : Extent.GetSubspace(at);

    /// <summary>The one wording, so a flow and an overlay cannot drift apart on it.</summary>
    protected static string NothingDeclared(string noun)
      => $"{noun} must declare at least one shape; this one called Next zero times";
  }
}
