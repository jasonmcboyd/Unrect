using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One definition, mid-application: a state machine fed spans in order. The engine starts one
  /// from a definition for each application and drives it; a consumer never holds one.
  /// </summary>
  /// <typeparam name="TSpace">The space the spans are over.</typeparam>
  /// <typeparam name="TResult">What the machine reads.</typeparam>
  public interface IProjector<TSpace, TResult>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// Offers the next span. True: taken — keep feeding me. False: <b>not mine, and I am finished</b>
    /// — the span is untouched and the caller re-offers it to my successor. A machine that has
    /// answered false answers false to everything after it.
    /// </summary>
    bool Next(Plane<TSpace> span);

    /// <summary>
    /// Nothing more is coming. Yields the value and how much was kept, or throws because what was
    /// declared is incomplete. The only way to a value: a machine that ended by refusing a span is
    /// still closed by its parent.
    /// </summary>
    Settlement<TResult> Close();
  }
}
