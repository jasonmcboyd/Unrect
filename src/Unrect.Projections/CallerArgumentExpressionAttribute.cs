namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// Neither netstandard2.0 nor netstandard2.1 has such an attribute, so this stands in for it.
  /// Roslyn matches the attribute on a parameter by its full type name in metadata, so a caller
  /// compiled against a framework that does have it still gets inference here, and an internal type
  /// cannot collide with the real one.
  /// <para>
  /// Unconditional, and stays that way while both of <c>Unrect</c>'s targets lack the attribute.
  /// A target that supplies it — anything from net5.0 on — would need <c>#if !NET5_0_OR_GREATER</c>
  /// around this, because defining it again in the same assembly is a duplicate-definition error.
  /// </para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  internal sealed class CallerArgumentExpressionAttribute : Attribute
  {
    public CallerArgumentExpressionAttribute(string parameterName)
    {
      ParameterName = parameterName;
    }

    public string ParameterName { get; }
  }
}
