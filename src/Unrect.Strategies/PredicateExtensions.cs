using System;

namespace Unrect.Strategies
{
  internal static class PredicateExtensions
  {
    /// <summary>The negation of a space predicate — how a take-while is expressed as a take-to.</summary>
    public static Func<T1, T2, bool> Not<T1, T2>(this Func<T1, T2, bool> predicate) => (t1, t2) => !predicate(t1, t2);
  }
}
