using System;

namespace Unrect.Strategies
{
  internal static class PredicateExtensions
  {
    public static Func<T1, bool> Not<T1>(this Func<T1, bool> predicate) => t1 => !predicate(t1);
    public static Func<T1, T2, bool> Not<T1, T2>(this Func<T1, T2, bool> predicate) => (t1, t2) => !predicate(t1, t2);
    public static Func<T1, T2, T3, bool> Not<T1, T2, T3>(this Func<T1, T2, T3, bool> predicate) => (t1, t2, t3) => !predicate(t1, t2, t3);
  }
}
