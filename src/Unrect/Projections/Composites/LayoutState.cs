using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One layout in progress: what it has taken so far, and everything that is true of a layout
  /// whatever it does with the space. What differs between layouts is one thing only — whether a
  /// child moves the next one along — so that is what the subclasses override, and the bookkeeping
  /// around it lives here where it cannot fork.
  /// </summary>
  internal abstract class LayoutState<TSpace>
    where TSpace : class, ISpace
  {
    private bool _read;

    protected LayoutState(Plane<TSpace> extent, ProjectionContext context)
    {
      Extent = extent;
      Context = context;
    }

    protected Plane<TSpace> Extent { get; }
    protected ProjectionContext Context { get; }

    /// <summary>How many children the layout has taken.</summary>
    public int Count { get; private set; }

    /// <summary>How much of its extent the layout used.</summary>
    public abstract Size Consumed { get; }

    /// <summary>
    /// The join of the children's presences: <see cref="Presence.Read"/> if any child read content,
    /// otherwise <see cref="Presence.Empty"/> — a layout none of whose children read anything read
    /// nothing itself. <see cref="Presence.Absorbed"/> is deliberately not among the answers: only a
    /// tolerance boundary can say it did not look, and a layout did look, through every child it
    /// declared.
    /// </summary>
    public Presence Presence => _read ? Presence.Read : Presence.Empty;

    /// <summary>
    /// Takes the next child and returns what it read, labelled by <paramref name="site"/> — where
    /// the declaration wrote it.
    /// </summary>
    public abstract T Next<T>(IProjectionDefinition<TSpace, T> projection, UseSite site);

    /// <summary>
    /// Records a child the layout has just taken, and what it made of its own extent. Counting and
    /// joining happen together so a subclass cannot do one and forget the other.
    /// </summary>
    protected void Took(Presence presence)
    {
      _read |= presence == Presence.Read;
      Count++;
    }
  }
}
