using System;

namespace Unrect
{
  /// <summary>
  /// A declaration asked a space for something <see cref="Unrect.Core.ISpace"/> does not promise,
  /// at a site where "I could not look" is not an answer.
  /// <para>
  /// This is the <em>boundary</em> half of the absence rule. At a projection site a missing
  /// capability is null — an honest per-cell answer, and a cell that cannot carry a formula plainly
  /// has none. At a boundary — a matcher deciding where a region starts or ends — null would say
  /// "I looked and it is not there", which is a claim about the document rather than about the
  /// reader. The two must never share a spelling, so a boundary that cannot look throws this.
  /// </para>
  /// <para>
  /// <b>It is a fault, and therefore unabsorbable.</b> <c>Optional</c>, <c>Else</c> and
  /// <c>Choice</c> exist to tolerate a section that is genuinely absent; a wrong backend is not an
  /// absent section, and tolerating it would turn a mis-wired declaration into a quietly empty
  /// result. <c>ProjectionEngine</c> classifies this alongside the IO failures for that reason.
  /// </para>
  /// </summary>
  public sealed class MissingCapabilityException : InvalidOperationException
  {
    /// <summary>
    /// States that <paramref name="demandedBy"/> needed <paramref name="capability"/> and did not
    /// get it.
    /// </summary>
    /// <param name="capability">The capability interface that was asked for.</param>
    /// <param name="demandedBy">What asked — a matcher's name, or a declaration's.</param>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    public MissingCapabilityException(Type capability, string demandedBy)
      : base(Describe(
        capability ?? throw new ArgumentNullException(nameof(capability)),
        demandedBy ?? throw new ArgumentNullException(nameof(demandedBy))))
    {
      Capability = capability;
      DemandedBy = demandedBy;
    }

    /// <summary>The capability interface the space did not offer.</summary>
    public Type Capability { get; }

    /// <summary>What asked for it.</summary>
    public string DemandedBy { get; }

    private static string Describe(Type capability, string demandedBy)
      => $"{demandedBy} needs a space that offers {capability.Name}, and this one does not. " +
        "A boundary that cannot look must fault: 'I could not look' and 'I looked and it is not there' are different answers.";
  }
}
