using Unrect.Core;
using Unrect.Projections;

using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Core.ISpace>;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario C — <b>boundary (d), the partial-class edge</b>, half two. The same class,
  /// scoped to <c>ISpace</c> in its own file. Both halves compile, and each member demands what its
  /// own file said — so a partial class is NOT a counter-example to the dichotomy theorem: it is two
  /// files, and the theorem is about files.
  /// <para>
  /// What it does cost is the one thing the theorem's file-split remedy relies on: a reader who
  /// opens the type rather than the file sees two demands on one class and no using block to
  /// explain either. The guidance the queued scope-hygiene analyzer would give is the same as for
  /// helpers — a shared type is not a declaration file.
  /// </para>
  /// </summary>
  public static partial class ScenarioCPartial
  {
    /// <inheritdoc cref="ScenarioCPartial.SharedBodyType"/>
    public static string SharedBodyTypeHere()
    {
      var part = Overlay(o => new Line(
        Fund: o.Next(Text()),
        Amount: o.Next(Right(1).Decimal())));

      return Reveal(part);
    }

    private static string Reveal<T>(T value) => Judge.TypeName(typeof(T));
  }
}
