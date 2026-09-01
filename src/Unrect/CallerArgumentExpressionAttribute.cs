namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// netstandard2.1 has no such attribute, so this stands in for it. Roslyn matches the attribute on
  /// a parameter by its full type name in metadata, so a caller compiled against a framework that
  /// does have it still gets inference here, and an internal type cannot collide with the real one.
  /// <para>
  /// Unconditional because <c>Unrect</c> targets netstandard2.1 alone. Should it ever multi-target,
  /// this needs <c>#if !NET5_0_OR_GREATER</c> around it: on a target that supplies the attribute
  /// itself, defining it again in the same assembly is a duplicate-definition error.
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
