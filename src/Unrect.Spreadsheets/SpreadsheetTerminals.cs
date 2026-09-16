using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The kinded leaves as pipeline terminals — <c>Right(3).Decimal()</c>, the placement said before
  /// the subject, for a space that carries kinds.
  /// <para>
  /// Extensions rather than members, because the stage lives in <c>Unrect</c> and these leaves do
  /// not. Each one closes a pipeline through the public <c>Of</c>, so it composes exactly as a
  /// hoisted declaration placed there would.
  /// </para>
  /// </summary>
  public static class SpreadsheetTerminals
  {
    /// <inheritdoc cref="SpreadsheetProjections.Text{TSpace}()"/>
    /// <typeparam name="TSpace">The sheet the pipeline is declared over.</typeparam>
    /// <param name="stage">The pipeline this leaf closes.</param>
    public static IProjection<TSpace, string> Text<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISheetCells
      => Placed(stage).Of(SpreadsheetProjections.Text<TSpace>());

    /// <inheritdoc cref="SpreadsheetProjections.Decimal{TSpace}()"/>
    /// <typeparam name="TSpace">The sheet the pipeline is declared over.</typeparam>
    /// <param name="stage">The pipeline this leaf closes.</param>
    public static IProjection<TSpace, decimal> Decimal<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISheetCells
      => Placed(stage).Of(SpreadsheetProjections.Decimal<TSpace>());

    /// <inheritdoc cref="SpreadsheetProjections.Integer{TSpace}()"/>
    /// <typeparam name="TSpace">The sheet the pipeline is declared over.</typeparam>
    /// <param name="stage">The pipeline this leaf closes.</param>
    public static IProjection<TSpace, int> Integer<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISheetCells
      => Placed(stage).Of(SpreadsheetProjections.Integer<TSpace>());

    /// <inheritdoc cref="SpreadsheetProjections.Double{TSpace}()"/>
    /// <typeparam name="TSpace">The sheet the pipeline is declared over.</typeparam>
    /// <param name="stage">The pipeline this leaf closes.</param>
    public static IProjection<TSpace, double> Double<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISheetCells
      => Placed(stage).Of(SpreadsheetProjections.Double<TSpace>());

    /// <inheritdoc cref="SpreadsheetProjections.Date{TSpace}()"/>
    /// <typeparam name="TSpace">The sheet the pipeline is declared over.</typeparam>
    /// <param name="stage">The pipeline this leaf closes.</param>
    public static IProjection<TSpace, DateTime> Date<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISheetCells
      => Placed(stage).Of(SpreadsheetProjections.Date<TSpace>());

    /// <inheritdoc cref="SpreadsheetProjections.Boolean{TSpace}()"/>
    /// <typeparam name="TSpace">The sheet the pipeline is declared over.</typeparam>
    /// <param name="stage">The pipeline this leaf closes.</param>
    public static IProjection<TSpace, bool> Boolean<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISheetCells
      => Placed(stage).Of(SpreadsheetProjections.Boolean<TSpace>());

    /// <inheritdoc cref="SpreadsheetProjections.Formula{TSpace}()"/>
    /// <typeparam name="TSpace">The space the pipeline is declared over.</typeparam>
    /// <param name="stage">The pipeline this leaf closes.</param>
    public static IProjection<TSpace, string?> Formula<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, IFormulaSpace
      => Placed(stage).Of(SpreadsheetProjections.Formula<TSpace>());

    private static PlacementStage<TSpace> Placed<TSpace>(PlacementStage<TSpace> stage)
      where TSpace : class, ISpace
      => stage ?? throw new ArgumentNullException(nameof(stage));
  }
}
