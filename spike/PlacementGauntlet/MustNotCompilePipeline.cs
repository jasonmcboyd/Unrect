// SPIKE, scenario 6 continued — the refusals ledger for the SHIPPED placement pipeline
// (`Unrect.Projections`, phase 2 of the placement renovation). Entries (a) through (x) ledger the
// spike's own façade in MustNotCompile.cs and stay there as the record of what was measured before
// the port; everything below is the library's own surface, refusing the same hazards in its own
// words.
//
//   dotnet build -p:DefineConstants=MUST_NOT_COMPILE        collects every entry below
//
// Messages are VERBATIM from that build (2026-09-10, .NET SDK 8 / Roslyn).
//
// THE IMPORTS ARE NAMESPACE-SCOPED, and must be: this file names the library's stage types and its
// entries, while MustNotCompile.cs imports the spike's `Place` and `PlacementGauntlet.Staged`. The
// two sets share every name the renovation promoted, so file-level usings would collide — which is
// itself the headline finding recorded at the foot of this file.

#if MUST_NOT_COMPILE

namespace PlacementGauntlet.PipelineLedger
{
  using Unrect.Core;
  using Unrect.Projections;
  using Unrect.Spreadsheets;

  using static Unrect.Projections.Projection;
  using static Unrect.Spreadsheets.SpreadsheetProjections;

  /// <summary>
  /// The shipped pipeline's refusals. Every member here is supposed to FAIL to compile.
  /// <para>
  /// <b>What the port changed.</b> In the spike, seven of these hazards were refused by ABSENCE and
  /// got CS0311 — a sentence about type parameters and implicit reference conversions that never
  /// mentions placement, produced because <c>ProjectionExtensions</c>' modifiers are self-typed and
  /// in scope in any declaration file, so the compiler finds them as extension candidates on the
  /// stage and rejects them on the constraint. The library spells them instead, as
  /// <c>[Obsolete(error: true)]</c> stubs, and every one of them now says what to write instead.
  /// </para>
  /// </summary>
  public static class MustNotCompilePipeline
  {
    private static readonly IRowLandmark Mark = RowContaining("Total");
    private static readonly IRowLandmark Other = RowContaining("Subtotal");
    private static readonly IColumnLandmark Column = ColumnContaining("Amount");

    // (y) DOUBLE ANCHOR — the survey's hazard 1, and the spike's (a). Was CS0311; is now the
    //     library's own sentence.
    //     CS0619: 'UnboundedStage.Below(IRowLandmark)' is obsolete: 'a pipeline declares where it
    //             starts once, and its anchor is its entry: On/Below/RightOf/OffsetBy are factories,
    //             not stages. To search inside a region another landmark found, nest — place the
    //             region, and place this projection within it. To carry on from a position, use Down
    //             or Right, which compose onto it.'
    public static object Y() => On(Mark).Below(Other).Text();

    // (z) ANCHOR AFTER MOVEMENT — hazard 3, the spike's (c). Movements never lead back to a root.
    //     CS0619: 'UnboundedStage.On(IRowLandmark)' is obsolete: 'a pipeline declares where it starts
    //             once, and its anchor is its entry: …'
    public static object Z() => Down(2).On(Mark).Text();

    // (aa) ANCHOR OVER ANCHOR THROUGH THE STRATEGY DOOR — the spike's (b).
    //     CS0619: 'UnboundedStage.OffsetBy(IOffsetStrategy)' is obsolete: 'a pipeline declares where
    //             it starts once, and its anchor is its entry: …'
    public static object AA() => On(Mark).OffsetBy(BlankRows()).Text();

    // (ab) DOUBLE SIZE — extents do not stack; the spike's (d).
    //     CS0619: 'OffsetAndSizeStage.Sized(IAreaStrategy)' is obsolete: 'a pipeline declares its
    //             extent once, and a second Sized would erase the first rather than narrow it —
    //             extents do not stack. Size once: to read part of a region, declare the region's
    //             extent and place a projection inside it.'
    public static object AB() => On(Mark).Sized(Extent(2, 2)).Sized(Extent(3, 3)).Text();

    // (ac) MOVEMENT AFTER SIZE — the stages run in the engine's order and do not run backwards; the
    //      spike's (e).
    //     CS0619: 'OffsetAndSizeStage.Down(int)' is obsolete: 'a pipeline runs in the engine's order —
    //             where it starts, how big it is, what announces it, then what it reads — so a
    //             movement belongs with the offset, ahead of the extent, the bound and the headings:
    //             On(mark).Down(1).Sized(rows).Heading("…").Of(section). Movements compose onto the
    //             offset, so several in a row are one position.'
    public static object AC() => On(Mark).Sized(Extent(2, 2)).Down(1).Text();

    // (ad) DOUBLE BOUND — hazard 2, the spike's (f). A projection has one end.
    //     CS0619: 'BoundStage.Until(IRowLandmark, bool)' is obsolete: 'a projection has one end, and
    //             the axis comes with the landmark, so a column bound over a row bound is a second end
    //             too — the replaced landmark would never even be sought. Bound it once: to bound both
    //             axes, bound the section and bound the region it sits in.'
    public static object AD() => Until(Mark).Until(Other).Text();

    // (ae) THE AXIS SWITCH. The same refusal reached the other way — the spelling that looks like
    //      bounding two axes at once and is a second end, which is why the message says so.
    //     CS0619: 'BoundStage.UntilColumn(IColumnLandmark, bool)' is obsolete: 'a projection has one
    //             end, and the axis comes with the landmark, …'
    public static object AE() => Until(Mark).UntilColumn(Column).Text();

    // (af) THE FRAME — hazard 4, the spike's (g). Never a contradiction, always a trap: an extent
    //      declared after a bound becomes the frame the landmark is sought in.
    //     CS0619: 'BoundStage.Sized(IAreaStrategy)' is obsolete: 'an extent declared after a bound
    //             becomes the frame the bound's landmark is sought in, which reads like a narrowing
    //             and is not one. Declare the extent first — On(mark).Sized(rows).Until(next) — or, to
    //             search inside a region, place the region and place this projection within it.'
    public static object AF() => Until(Mark).Sized(Extent(3, 4)).Text();

    // (ag) AN ANCHOR AFTER A HEADING — the spike's (v), inherited word for word: a heading already
    //      locates the section, so a second locator to its right is the same statement twice.
    //     CS0619: 'HeadingStage.On(IRowLandmark)' is obsolete: 'a section announced by a heading is
    //             already located by it: Heading finds the row by content, asserts the text and
    //             consumes it. If the section needs an anchor as well — the repeat-stop recipe, where
    //             the anchor must sit on the heading-and-content flow — declare it as the pipeline's
    //             entry, which reads first because it is furthest up the sheet:
    //             On(mark).Heading("…").Of(section).'
    public static object AG() => Heading("Detail").On(Mark).Of(Table<Position>());

    // (ah) A BOUND AFTER A HEADING — the spike's (w), which was refused by absence and got the
    //      constraint-babble. The canonical order enforced in the one direction it runs, now with the
    //      other spelling named in the message.
    //     CS0619: 'HeadingStage.Until(IRowLandmark, bool)' is obsolete: 'a pipeline runs in the
    //             engine's order — where it starts, how big it is, what announces it, then what it
    //             reads — so a bound and an extent come before the headings:
    //             On(mark).Until(next).Heading("…").Of(section). A bound written after the SUBJECT is
    //             the other spelling of the same declaration and is unchanged:
    //             Heading("…").Of(section).Until(next).'
    public static object AH() => Heading("Detail").Until(Mark).Of(Table<Position>());

    // (ai) A DISCOVERED HEADING — the spike's (x), and the ruled refusal: Heading takes the TEXT, and
    //      there is no overload taking a projection, ever. A row whose text varies per file is a
    //      landmark (RowWhere over the row's shape), not a heading.
    //     CS1503: Argument 1: cannot convert from 'Unrect.Projections.IProjection<string>' to 'string'
    public static object AI() => Heading(Row(cells => cells[0].GetString())).Of(Table<Position>());

    // (aj) THE SIZE STAGE, TWICE, THE OTHER WAY. Refused BY ABSENCE and correctly so: no extension
    //      method is named SizedToChildren, so the compiler's own sentence is already about a member
    //      that is not there rather than about type parameters. This is the line the ledger draws —
    //      a stub where the reason is worth saying, absence where the compiler already says it.
    //     CS1061: 'OffsetAndSizeStage' does not contain a definition for 'SizedToChildren' and no
    //             accessible extension method 'SizedToChildren' accepting a first argument of type
    //             'OffsetAndSizeStage' could be found (are you missing a using directive or an
    //             assembly reference?)
    public static object AJ() => On(Mark).Sized(Extent(2, 2)).SizedToChildren().Text();

    // (ak) A DEMANDING CHILD IN A PLAIN PIPELINE'S LAYOUT LAMBDA. The pipeline does not remove the
    //      inference wall the scope exists for — a lambda body cannot drive inference — and the
    //      message is the worst one in the taxonomy, exactly as it is without a pipeline. The fix is
    //      the scoped entry: Over<IFormulaSpace>().Down(1).VerticalFlow(v => v.Next(Formula())).
    //     CS0411: The type arguments for method 'LayoutCursor.Next<T>(IProjection<T>, string?)' cannot
    //             be inferred from the usage. Try specifying the type arguments explicitly.
    public static object AK() => Down(1).VerticalFlow(v => v.Next(Formula()));

    // (al) THE SCOPED TWIN OF (y). Every refusal above has one, spelled on the <TSpace> hierarchy with
    //      the same message — a demanding pipeline is refused for the same reasons and told the same
    //      thing.
    //     CS0619: 'UnboundedStage<ISpreadsheetSpace>.Below(IRowLandmark)' is obsolete: 'a pipeline
    //             declares where it starts once, and its anchor is its entry: …'
    public static object AL() => Over<ISpreadsheetSpace>().On(Mark).Below(Other).Formula();

    // (am) NOT HERE, DELIBERATELY: the navigation chain across a terminal.
    //      On(mark).VerticalFlow(…).Below(other) COMPILES — a terminal hands back a plain IProjection,
    //      whose modifiers are today's, so the second anchor is caught by the SHIPPED RUNTIME REFUSAL
    //      instead. That is the intended hybrid: types inside the pipeline, the runtime guard after it.
  }
}

#endif
