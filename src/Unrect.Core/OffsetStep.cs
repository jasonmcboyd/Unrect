namespace Unrect.Core
{
  /// <summary>What an offset scan says about the newest span it was shown.</summary>
  public enum OffsetStep
  {
    /// <summary>Not yet: this span is skipped, and the next one is asked about.</summary>
    Skip,

    /// <summary>The region starts on this span.</summary>
    StartHere,

    /// <summary>The region starts on the span after this one, which is skipped too.</summary>
    StartNext,
  }
}
