#if MUST_NOT_COMPILE
using System.Collections.Generic;

using PlacementGauntlet.Staged;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.Place;
using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;
#endif

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario 6 — the refusals ledger. Every member here is supposed to FAIL to compile;
  /// build with <c>dotnet build -p:DefineConstants=MUST_NOT_COMPILE</c> to collect the messages. The
  /// messages below are VERBATIM from that build (2026-09-09, .NET SDK 8 / Roslyn), collected with
  /// the imports a real declaration carries.
  /// <para>
  /// <b>The headline finding is the QUALITY of the refusals, not their existence.</b> Every hazard is
  /// refused at compile time, as the design claims. But refusal by ABSENCE does not produce the
  /// missing-member message the design assumed: because <c>ProjectionExtensions</c>' modifiers are
  /// self-typed (<c>TProjection : class, IProjection</c>) and are in scope in any real declaration,
  /// the compiler FINDS <c>On</c>/<c>Sized</c>/<c>Until</c> as extension candidates and rejects them
  /// on the constraint — CS0311, a sentence about type parameters and implicit reference conversions
  /// that never mentions placement. Compare (h), where the same hazard is refused by an
  /// <c>[Obsolete(error)]</c> stub and the compiler says the library's own words. A renovation that
  /// wants readable refusals must SPELL the refused members, not merely omit them.
  /// </para>
  /// <para>
  /// <b>Entries (p) through (u) live in <c>MustNotCompileEntryC.cs</c></b> — the file-scoped
  /// vocabulary's refusals need one import block each, and file-level usings would leak across them.
  /// That file also carries the mechanic this one does not need: declaration-level refusals suppress
  /// body binding for the WHOLE compilation, so it ledgers under a second constant.
  /// </para>
  /// <para>
  /// <b>What is deliberately NOT here:</b> the navigation chain across a terminal.
  /// <c>Below(m).VerticalFlow(…).On(m2)</c> COMPILES — a terminal hands back a plain
  /// <c>IProjection</c>, whose modifiers are today's, so the second anchor is caught by the SHIPPED
  /// RUNTIME REFUSAL instead. That is §5.0's intended hybrid (types inside the pipeline, the runtime
  /// guard after it), and it is demonstrated live in scenario 4 rather than pretended about here.
  /// </para>
  /// </summary>
  public static class MustNotCompile
  {
#if MUST_NOT_COMPILE
    private static readonly IRowLandmark Mark = RowContaining("Total");
    private static readonly IRowLandmark Other = RowContaining("Subtotal");

    // (a) DOUBLE ANCHOR — the survey's hazard 1. Anchors are entry factories; an intermediate has none.
    //     CS0311: The type 'PlacementGauntlet.Staged.OffsetStage' cannot be used as type parameter
    //             'TProjection' in the generic type or method
    //             'ProjectionExtensions.On<TProjection>(TProjection, IRowLandmark)'. There is no
    //             implicit reference conversion from 'PlacementGauntlet.Staged.OffsetStage' to
    //             'Unrect.Projections.IProjection'.
    public static object A() => Below(Mark).On(Other).Text();

    // (b) ANCHOR OVER ANCHOR through the strategy door.
    //     CS0311: The type 'PlacementGauntlet.Staged.OffsetStage' cannot be used as type parameter
    //             'TProjection' in the generic type or method
    //             'ProjectionExtensions.OffsetBy<TProjection>(TProjection, IOffsetStrategy)'. There is
    //             no implicit reference conversion from 'PlacementGauntlet.Staged.OffsetStage' to
    //             'Unrect.Projections.IProjection'.
    public static object B() => On(Mark).OffsetBy(BlankRows()).Text();

    // (c) ANCHOR AFTER MOVEMENT — the survey's hazard 3. Movements never lead back to a root.
    //     CS0311: The type 'PlacementGauntlet.Staged.OffsetStage' cannot be used as type parameter
    //             'TProjection' in the generic type or method
    //             'ProjectionExtensions.Below<TProjection>(TProjection, IRowLandmark)'. There is no
    //             implicit reference conversion from 'PlacementGauntlet.Staged.OffsetStage' to
    //             'Unrect.Projections.IProjection'.
    public static object C() => Down(2).Below(Mark).Text();

    // (d) DOUBLE SIZE — extents do not stack.
    //     CS0311: The type 'PlacementGauntlet.Staged.OffsetAndSizeStage' cannot be used as type
    //             parameter 'TProjection' in the generic type or method
    //             'ProjectionExtensions.Sized<TProjection>(TProjection, IAreaStrategy)'. There is no
    //             implicit reference conversion from 'PlacementGauntlet.Staged.OffsetAndSizeStage' to
    //             'Unrect.Projections.IProjection'.
    public static object D() => Sized(Extent(2, 2)).Sized(Extent(3, 3)).Text();

    // (e) MOVEMENT AFTER SIZE — the stages run in the engine's order and do not run backwards.
    //     CS0311: The type 'PlacementGauntlet.Staged.OffsetAndSizeStage' cannot be used as type
    //             parameter 'TProjection' in the generic type or method
    //             'ProjectionExtensions.Down<TProjection>(TProjection, int)'. There is no implicit
    //             reference conversion from 'PlacementGauntlet.Staged.OffsetAndSizeStage' to
    //             'Unrect.Projections.IProjection'.
    public static object E() => Sized(Extent(2, 2)).Down(1).Text();

    // (f) DOUBLE BOUND — the survey's hazard 2. A projection has one end.
    //     CS0311: The type 'PlacementGauntlet.Staged.BoundStage' cannot be used as type parameter
    //             'TProjection' in the generic type or method
    //             'ProjectionExtensions.Until<TProjection>(TProjection, IRowLandmark, bool)'. There is
    //             no implicit reference conversion from 'PlacementGauntlet.Staged.BoundStage' to
    //             'Unrect.Projections.IProjection'.
    public static object F() => Until(Mark).Until(Other).Text();

    // (g) THE FRAME — the survey's hazard 4, which was never a contradiction but IS a trap: an extent
    //     declared outside a bound becomes the frame the landmark is sought in. Unspellable here, and
    //     the live proof of what it costs today is in scenario 5.
    //     CS0311: The type 'PlacementGauntlet.Staged.BoundStage' cannot be used as type parameter
    //             'TProjection' in the generic type or method
    //             'ProjectionExtensions.Sized<TProjection>(TProjection, IAreaStrategy)'. There is no
    //             implicit reference conversion from 'PlacementGauntlet.Staged.BoundStage' to
    //             'Unrect.Projections.IProjection'.
    public static object G() => Until(Mark).Sized(Extent(3, 4)).Text();

    // (h) THE SAME REFUSAL, TAUGHT. The scoped offset stage carries the four anchors as
    //     [Obsolete(error)] stubs, so the compiler says the library's words instead of its own. This
    //     is the same class of error as the OrBlank-after-wrapper precedent, moved to compile time.
    //     CS0619: 'OffsetStage<ISpace>.On(IRowLandmark)' is obsolete: 'a pipeline declares where it
    //             starts once, and its anchor is its entry: On/Below/RightOf/OffsetBy are factories,
    //             not stages. To search inside a region another landmark found, nest — place the
    //             region, and place this projection within it. To carry on from a position, use Down
    //             or Right, which compose onto it.'
    public static object H() => Place.Over<ISpace>().Below(Mark).On(Other).Text();

    // (i) THE OWNER'S SKETCH, VERBATIM. An ascription that states the space and infers the result
    //     cannot exist: C# infers a method's type arguments all or none. The scoped ENTRY is the
    //     answer, and it is the other half of the same sketch.
    //     CS1501: No overload for method 'Over' takes 1 arguments
    public static object I()
      => Projection.Over<ISpreadsheetSpace>(Offset().SizedToChildren().VerticalRepeat(Table<Position>()));

    // (j) A DEMANDING CHILD IN A PLAIN PIPELINE'S LAYOUT LAMBDA. The inversion does not remove the
    //     inference wall the scope exists for — a lambda body cannot drive inference — and the
    //     message is the worst one in the taxonomy, exactly as it is today. The fix is the scoped
    //     entry: Place.Over<IFormulaSpace>().Offset().VerticalFlow(v => v.Next(Formula())).
    //     CS0411: The type arguments for method 'LayoutCursor.Next<T>(IProjection<T>, string?)'
    //             cannot be inferred from the usage. Try specifying the type arguments explicitly.
    public static object J() => Offset().VerticalFlow(v => v.Next(Formula()));

    // (k) THE ALL-OR-NONE WALL, MEASURED — why the intermediates are classes with instance terminals
    //     rather than interfaces with extension terminals. A terminal whose result type must be
    //     STATED cannot be an extension on a stage generic in the space: stating one of two type
    //     arguments makes the method invisible.
    //     CS1061: 'OffsetStage<ISpace>' does not contain a definition for 'TableExt' and no accessible
    //             extension method 'TableExt' accepting a first argument of type 'OffsetStage<ISpace>'
    //             could be found (are you missing a using directive or an assembly reference?)
    public static IProjection<TSpace, IReadOnlyList<T>> TableExt<TSpace, T>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISpace
      => stage.Table<T>();

    public static object K() => Place.Over<ISpace>().Offset().TableExt<Position>();

    // --- The geography law's additions (2026-09-09) ------------------------------------------------

    // (l) AN ANCHOR AFTER A CAPTION. Refused with a teaching message rather than by absence, because
    //     the reason is worth saying: a caption already locates the section, and where an anchor is
    //     genuinely wanted as well it is the pipeline's ENTRY — which reads first because it is
    //     furthest up the sheet.
    //     CS0619: 'UnderStage.On(IRowLandmark)' is obsolete: 'a section that sits under a caption is
    //             already located by it: Under finds the caption by content and consumes it. If the
    //             section needs an anchor as well — the repeat-stop recipe, where the anchor must sit
    //             on the caption-and-content flow — declare it as the pipeline's entry, which reads
    //             first because it is furthest up the sheet: On(mark).Under(caption).Of(section).'
    public static object L() => Under(Caption("Detail")).On(Mark).Of(Table<Position>());

    // (m) A DEMANDING CAPTION. Correctly refused, and not by this façade: ProjectionExtensions.Under
    //     takes IProjection<string>[], deliberately — a caption demands nothing of its space, which is
    //     the whole of what a caption is. A scoped Under scopes the SECTION; the captions stay plain.
    //     CS1503: Argument 1: cannot convert from 'Unrect.Projections.IProjection<Unrect.Spreadsheets.IFormulaSpace, string>'
    //             to 'Unrect.Projections.IProjection<string>'
    public static object M() => Under(Caption("Detail").On(RowWithFormula())).Of(Table<Position>());

    // (n) A SECOND UNDER. A NARROWING, recorded as one: today x.Under(a).Under(b) nests two flows and
    //     means "b above a above x", which is unusual but not contradictory. The pipeline offers one
    //     caption stage taking every caption at once — Under(a, b) — and refuses the stacked form.
    //     CS0311: The type 'PlacementGauntlet.Staged.UnderStage' cannot be used as type parameter
    //             'TProjection' in the generic type or method
    //             'ProjectionExtensions.Under<TProjection>(TProjection, params IProjection<string>[])'.
    //             There is no implicit reference conversion from 'PlacementGauntlet.Staged.UnderStage'
    //             to 'Unrect.Projections.IProjection'.
    public static object N() => Under(Caption("Outer")).Under(Caption("Inner")).Of(Table<Position>());

    // (o) THE SUPERSEDED STAGE, for the record: postfix .Until still composes after a terminal, so
    //     nothing about dropping the stage removes a spelling. This one COMPILES and is here only to
    //     say so — it is commented out rather than deleted so the ledger stays a build.
    //     (Under(Caption("Detail")).Of(Table<Position>()).Until(Mark) — compiles, and is the
    //      recommended spelling.)
#endif
  }
}
