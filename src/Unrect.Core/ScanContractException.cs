using System;

namespace Unrect.Core
{
  /// <summary>
  /// A scan broke the contract its interface states — asked how far across it reaches once the
  /// spans had run out, it did not say. A fault: it speaks of the strategy's code and never of the
  /// data, so no tolerance boundary absorbs it, where a scan that <em>throws</em> is the data
  /// source's rule failing over the data and may be.
  /// </summary>
  public sealed class ScanContractException : Exception
  {
    /// <summary>A scan that broke its contract, said how.</summary>
    public ScanContractException(string message)
      : base(message)
    {
    }
  }
}
