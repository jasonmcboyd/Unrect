using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// Neither netstandard2.0 nor netstandard2.1 has such a type, and the compiler requires one to emit
  /// an <c>init</c> accessor, so this stands in for it. Roslyn matches it by full type name in
  /// metadata, so an internal copy serves and cannot collide with the real one.
  /// <para>
  /// A compiler polyfill is per assembly by nature: it must stay internal (a public one in this
  /// namespace would collide with the real type in every .NET 5+ consumer), and an internal type
  /// cannot be shared through a reference. So this is one source file, LINKED into every project
  /// that declares an <c>init</c> property (<c>Compile Include</c> in its csproj) rather than
  /// referenced from one of them.
  /// </para>
  /// <para>
  /// Unconditional, and stays that way while both targets lack the type. A target that supplies it
  /// — anything from net5.0 on — would need <c>#if !NET5_0_OR_GREATER</c> around this, because
  /// defining it again in the same assembly is a duplicate-definition error.
  /// </para>
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  internal static class IsExternalInit
  {
  }
}
