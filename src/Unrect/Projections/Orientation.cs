namespace Unrect.Projections
{
  /// <summary>
  /// Which way a projection runs. Internal: it is a parameter of the projection vocabulary's own
  /// machinery and appears in no public signature — the vocabulary spells the axis into the factory
  /// name instead (<c>VerticalFlow</c>, <c>Row</c>, <c>UntilColumn</c>).
  /// </summary>
  internal enum Orientation
  {
    Horizontal,
    Vertical
  }
}
