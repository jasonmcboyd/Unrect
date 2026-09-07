using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The witness a capability's package publishes so an ascription can
  /// be written without type arguments — <c>rows.Demanding(Formulas)</c>, where the result type is
  /// still inferred from the receiver. It carries no data and does nothing; its whole job is to give
  /// inference something to read the demand off.
  /// </summary>
  /// <typeparam name="TSpace">The capability being demanded.</typeparam>
  public sealed class Demand<TSpace>
    where TSpace : class, ISpace
  {
    private Demand()
    {
    }

    /// <summary>The witness. One per capability, and there is nothing to configure.</summary>
    public static Demand<TSpace> Instance { get; } = new Demand<TSpace>();
  }
}
