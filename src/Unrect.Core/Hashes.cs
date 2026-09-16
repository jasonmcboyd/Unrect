namespace Unrect.Core
{
  /// <summary>
  /// Hash mixing for the value types here. Written out because <c>System.HashCode</c> is
  /// netstandard2.1 and up, and these libraries also target netstandard2.0.
  /// </summary>
  internal static class Hashes
  {
    /// <summary>
    /// Two hashes mixed into one — the multiply-and-add the compiler itself emits for a record or an
    /// anonymous type.
    /// </summary>
    internal static int Combine(int first, int second)
    {
      unchecked
      {
        return (first * -1521134295) + second;
      }
    }
  }
}
