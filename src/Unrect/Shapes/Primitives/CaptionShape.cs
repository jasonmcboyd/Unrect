using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// An anchor row, declared rather than searched for. Its placement finds the row, its extent is
  /// that row at the full available width, and its projection asserts the match and yields what the
  /// cell actually says.
  /// <para>
  /// It is a leaf shape and not an attribute on the section below it, because a node gets the whole
  /// algebra for nothing: it is placed by the one engine path, bounded by <c>Until</c>, tolerated by
  /// <c>Optional</c>, labelled by the naming ladder, rendered into every path, and counted in what a
  /// flow consumed. A caption carried as a property would need each of those written again.
  /// </para>
  /// </summary>
  internal sealed class CaptionShape : ShapeBase<string>
  {
    public CaptionShape(string text, Placement placement)
      : base(placement)
    {
      Text = text;
      Match = CellMatching.TextEquals(text);
    }

    private string Text { get; }
    private Func<CellValue, bool> Match { get; }

    public override string Description => $"Caption(\"{Text}\")";

    public override ShapeResult<string> Project(ISpace extent, ShapeContext context)
    {
      var size = extent.Area.Size;

      // Reachable only when the placement was replaced — Caption("X").After(SkipRows(1)), or a
      // caption inside a declared frame. Left in because it is also the half of this leaf that a
      // writer would satisfy: the writer emits the row, the reader verifies it.
      if (size.Height != 1)
        throw context.Failure($"a Caption must be exactly one row tall; this one is {size.Height} rows tall", extent);

      for (var column = 0; column < size.Width; column++)
        if (Match(extent[column, 0]))
          // The file's text, not the declaration's: the literal is the matcher, the cell is the
          // datum, and untrimmed because trimming is the matcher's business.
          return new ShapeResult<string>(extent[column, 0].GetString(), size);

      throw context.Failure($"expected a row containing '{Text}' here", extent);
    }
  }
}
