using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjections;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario C — <b>boundary (d), the partial-class edge</b>, half one. One class, two
  /// files, two scopes. It COMPILES, and the reason is worth stating precisely: a
  /// <c>using static</c> is a property of the FILE, not of the type, so a partial class is bound
  /// once per part with whatever that part's file imported. The file is the scope even when the
  /// type is not.
  /// <para>
  /// This half is scoped to <c>ISpreadsheetSpace</c> and reads a formula, so its demand is honest.
  /// The other half is next door.
  /// </para>
  /// </summary>
  public static partial class ScenarioCPartial
  {
    /// <summary>
    /// The pin: character for character what the other part declares. Only the file differs, so the
    /// type that comes back isolates exactly one thing — the demand.
    /// </summary>
    public static string SharedBodyType()
    {
      var part = Overlay(o => new Line(
        Fund: o.Next(Text()),
        Amount: o.Next(Right(1).Decimal())));

      return Reveal(part);
    }

    /// <summary>
    /// And one member that genuinely reads the capability, so this part's scope is honest rather
    /// than merely asserted — which is what a real file's other declarations would be doing.
    /// </summary>
    public static IProjection<ISpreadsheetSpace, SourcedAllocation> AuditedPart()
      => Overlay(o => new SourcedAllocation(
        Account: o.Next(Text()),
        Formula: o.Next(Right(3).Of(Formula()))));
  }
}
