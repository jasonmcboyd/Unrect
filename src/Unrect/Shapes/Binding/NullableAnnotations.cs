using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unrect.Shapes
{
  /// <summary>
  /// Whether a reference-typed member was declared <c>string?</c>. <c>Nullable&lt;T&gt;</c> is a CLR
  /// type and reflection sees it; an annotated reference type is not — it is the plain type plus
  /// metadata the consumer's compiler emitted, and reading that metadata is what makes the declared
  /// type mean what it says.
  /// <para>
  /// Both attributes are generated per-assembly and are <c>internal</c> to the consumer's assembly,
  /// so they are matched by full name through <c>GetCustomAttributesData()</c> and never by type.
  /// </para>
  /// </summary>
  internal static class NullableAnnotations
  {
    private const string Nullable = "System.Runtime.CompilerServices.NullableAttribute";
    private const string NullableContext = "System.Runtime.CompilerServices.NullableContextAttribute";

    /// <summary>Annotated nullable — the flag value that means "may be null".</summary>
    private const byte Annotated = 2;

    public static bool IsAnnotatedNullable(PropertyInfo property)
      => Member(property.GetCustomAttributesData())
        ?? Context(property.DeclaringType)
        ?? false;

    public static bool IsAnnotatedNullable(ParameterInfo parameter)
      => Member(parameter.GetCustomAttributesData())
        // The constructor's own scope, which the walk must not skip: Roslyn puts a
        // NullableContextAttribute on a method when most of its signature agrees, and then omits
        // the per-parameter attributes that agree with it. A record whose constructor context is 2
        // under a type context of 1 would otherwise read every string? parameter as strict.
        ?? Scope(parameter.Member.GetCustomAttributesData())
        ?? Context(parameter.Member.DeclaringType)
        ?? false;

    /// <summary>The nearest enclosing context: the declaring type, its enclosing types, the module.</summary>
    private static bool? Context(Type? type)
    {
      for (var scope = type; scope is not null; scope = scope.DeclaringType)
        if (Scope(scope.GetCustomAttributesData()) is bool annotated)
          return annotated;

      return type is null ? null : Scope(type.Module.GetCustomAttributesData());
    }

    /// <summary>
    /// A member's or parameter's own annotation. Only <c>NullableAttribute</c> is read here: a
    /// context attribute at this scope would be describing something else.
    /// </summary>
    private static bool? Member(IList<CustomAttributeData> attributes) => Flag(attributes, Nullable);

    /// <summary>
    /// An enclosing scope's default. Only <c>NullableContextAttribute</c> is read here — a
    /// <c>NullableAttribute</c> on a type describes its base type's and interfaces' nullability, and
    /// reading it as the scope default would let that shadow the type's real context.
    /// </summary>
    private static bool? Scope(IList<CustomAttributeData> attributes) => Flag(attributes, NullableContext);

    private static bool? Flag(IList<CustomAttributeData> attributes, string wanted)
    {
      foreach (var attribute in attributes)
      {
        if (attribute.AttributeType.FullName != wanted)
          continue;

        if (attribute.ConstructorArguments.Count != 1)
          continue;

        var argument = attribute.ConstructorArguments[0];

        // Our supported member types are all non-generic, so only the single-byte form can occur;
        // the array form is handled defensively.
        if (argument.Value is byte flag)
          return flag == Annotated;

        if (argument.Value is IReadOnlyCollection<CustomAttributeTypedArgument> flags && flags.Count > 0)
          return flags.First().Value is byte first && first == Annotated;
      }

      return null;
    }
  }
}
