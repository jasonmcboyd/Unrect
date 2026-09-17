namespace Unrect.Projections
{
  /// <summary>
  /// One edge of a composite: the child, and how the declaration wrote it. A composite that
  /// captured the identifier its child was written as (<c>VerticalRepeat(block)</c>, <c>Table(0,
  /// allocation)</c>) keeps it here, so a walker without a space reads the same label a failure
  /// path renders.
  /// </summary>
  public readonly struct Child
  {
    internal Child(IProjection projection, UseSite site)
    {
      Projection = projection;
      Site = site;
    }

    /// <summary>The child itself.</summary>
    public IProjection Projection { get; }

    /// <summary>Where the child was written: the identifier the declaration used for it, and which child it is.</summary>
    public UseSite Site { get; }
  }
}
