using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// One labelled pair: a label cell asserted against the declared label, and the value cell
  /// immediately to its right. Two wide, one tall, and nothing else — a wider value region or a gap
  /// would be a different shape, and this one says which it is.
  /// <para>
  /// The value is handed back as a <see cref="CellValue"/>: the labels are the structure, the values
  /// are data, so a blank value is <c>Blank</c> rather than a failure.
  /// </para>
  /// </summary>
  internal sealed class FieldShape : ShapeBase<CellValue>
  {
    public FieldShape(string label, Placement placement)
      : base(placement)
    {
      Label = label;
      Match = CellMatching.LabelEquals(label);
    }

    private string Label { get; }
    private Func<CellValue, bool> Match { get; }

    public override string Description => $"Field(\"{Label}\")";

    public override ShapeResult<CellValue> Project(ISpace extent, ShapeContext context)
    {
      var size = extent.Area.Size;

      if (size.Width != 2 || size.Height != 1)
        throw context.Failure(
          $"a Field must be two cells wide and one row tall; this one is {size.Width}x{size.Height}", extent);

      var label = extent[0, 0];

      if (!Match(label))
        throw context.Failure($"expected a label reading '{Label}' here, but this cell {Describe(label)}", extent);

      return new ShapeResult<CellValue>(extent[1, 0], size);
    }

    private static string Describe(CellValue cell)
      => cell.IsBlank ? "is blank"
       : cell.TryGetString() is string text ? $"reads '{text}'"
       // An error says which error it is, in Core's own spelling, rather than "holds a Error".
       : cell.Kind == CellKind.Error ? $"holds {cell}"
       : $"holds a {cell.Kind}";
  }
}
