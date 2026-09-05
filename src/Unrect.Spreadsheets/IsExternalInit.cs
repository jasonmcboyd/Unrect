using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// Neither netstandard2.0 nor netstandard2.1 has such a type, and the compiler requires one to emit
  /// an <c>init</c> accessor, so this stands in for it. Roslyn matches it by full type name in
  /// metadata, so an internal copy serves and cannot collide with the real one.
  /// <para>
  /// It exists for <see cref="Unrect.Spreadsheets.WorkbookOptions"/>, whose properties are <c>init</c>
  /// rather than constructor parameters so that a future option stays additive.
  /// </para>
  /// <para>
  /// Unconditional, and stays that way while both of this assembly's targets lack the type. A target
  /// that supplies it — anything from net5.0 on — would need <c>#if !NET5_0_OR_GREATER</c> around
  /// this, because defining it again in the same assembly is a duplicate-definition error.
  /// </para>
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  internal static class IsExternalInit
  {
  }
}
