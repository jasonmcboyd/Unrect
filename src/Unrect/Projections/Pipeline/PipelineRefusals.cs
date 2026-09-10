namespace Unrect.Projections
{
  /// <summary>
  /// What a stage says when it refuses a member, written once and attached to every stub that
  /// refuses for that reason.
  /// <para>
  /// A refused stage member is <em>spelled</em>, as an <c>[Obsolete(error: true)]</c> stub, rather
  /// than left out. Omitting it does not produce the missing-member message it looks like it would:
  /// the placement modifiers in <see cref="ProjectionExtensions"/> are self-typed and in scope in
  /// every declaration file, so the compiler finds them as extension candidates on the stage and
  /// rejects them on the constraint — a sentence about type parameters and implicit reference
  /// conversions that never mentions placement. A stub makes the compiler say the library's words
  /// instead, which is the whole reason the refusals are compile-time in the first place.
  /// </para>
  /// <para>
  /// Absence is still right where no reasonable declaration attempts the spelling, and where the
  /// compiler's own sentence is already about a member that is not there.
  /// </para>
  /// </summary>
  internal static class PipelineRefusals
  {
    /// <summary>A second anchor, anywhere after the entry.</summary>
    internal const string SecondAnchor =
      "a pipeline declares where it starts once, and its anchor is its entry: On/Below/RightOf/OffsetBy "
      + "are factories, not stages. To search inside a region another landmark found, nest — place the "
      + "region, and place this projection within it. To carry on from a position, use Down or Right, "
      + "which compose onto it.";

    /// <summary>An anchor after a heading, where the heading has already located the section.</summary>
    internal const string HeadingIsTheAnchor =
      "a section announced by a heading is already located by it: Heading finds the row by content, asserts "
      + "the text and consumes it. If the section needs an anchor as well — the repeat-stop recipe, where the "
      + "anchor must sit on the heading-and-content flow — declare it as the pipeline's entry, which reads "
      + "first because it is furthest up the sheet: On(mark).Heading(\"…\").Of(section).";

    /// <summary>A movement after the extent, the bound or the headings.</summary>
    internal const string OffsetComesFirst =
      "a pipeline runs in the engine's order — where it starts, how big it is, what announces it, then what it "
      + "reads — so a movement belongs with the offset, ahead of the extent, the bound and the headings: "
      + "On(mark).Down(1).Sized(rows).Heading(\"…\").Of(section). Movements compose onto the offset, so several "
      + "in a row are one position.";

    /// <summary>A second extent.</summary>
    internal const string ExtentsDoNotStack =
      "a pipeline declares its extent once, and a second Sized would erase the first rather than narrow it — "
      + "extents do not stack. Size once: to read part of a region, declare the region's extent and place a "
      + "projection inside it.";

    /// <summary>A second end, on either axis.</summary>
    internal const string ProjectionHasOneEnd =
      "a projection has one end, and the axis comes with the landmark, so a column bound over a row bound is a "
      + "second end too — the replaced landmark would never even be sought. Bound it once: to bound both axes, "
      + "bound the section and bound the region it sits in.";

    /// <summary>An extent after a bound, which frames the search rather than narrowing it.</summary>
    internal const string BoundFramesTheExtent =
      "an extent declared after a bound becomes the frame the bound's landmark is sought in, which reads like a "
      + "narrowing and is not one. Declare the extent first — On(mark).Sized(rows).Until(next) — or, to search "
      + "inside a region, place the region and place this projection within it.";

    /// <summary>A bound or an extent after a heading.</summary>
    internal const string GeometryComesBeforeTheHeadings =
      "a pipeline runs in the engine's order — where it starts, how big it is, what announces it, then what it "
      + "reads — so a bound and an extent come before the headings: On(mark).Until(next).Heading(\"…\").Of(section). "
      + "A bound written after the SUBJECT is the other spelling of the same declaration and is unchanged: "
      + "Heading(\"…\").Of(section).Until(next).";
  }
}
