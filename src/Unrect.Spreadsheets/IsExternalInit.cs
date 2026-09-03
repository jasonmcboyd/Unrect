using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// netstandard2.1 has no such type, and the compiler requires one to emit an <c>init</c> accessor,
  /// so this stands in for it. Roslyn matches it by full type name in metadata, so an internal copy
  /// serves and cannot collide with the real one.
  /// <para>
  /// It exists for <see cref="Unrect.Spreadsheets.WorkbookOptions"/>, whose properties are <c>init</c>
  /// rather than constructor parameters so that a future option stays additive.
  /// </para>
  /// <para>
  /// Unconditional because this assembly targets netstandard2.1 alone. Should it ever multi-target,
  /// this needs <c>#if !NET5_0_OR_GREATER</c> around it: on a target that supplies the type itself,
  /// defining it again in the same assembly is a duplicate-definition error.
  /// </para>
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  internal static class IsExternalInit
  {
  }
}
