using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Unrect.Analyzers
{
  /// <summary>
  /// The handful of Unrect types a declaration's space is written in, resolved once per compilation
  /// — and the one question asked of them: <em>what space does this type ask for?</em>
  /// <para>
  /// <see cref="TryLoad"/> answers null when the compilation does not reference Unrect at all,
  /// which is every analyzer's first exit.
  /// </para>
  /// </summary>
  internal sealed class UnrectSymbols
  {
    /// <summary>
    /// The generic types that carry a demand in their first type argument, in the order
    /// <see cref="DemandOf"/> asks them: the projection itself, then the phantoms — the two matchers
    /// and the five strategies.
    /// </summary>
    private static readonly string[] PhantomNames =
    {
      "Unrect.Projections.IRowLandmark`1",
      "Unrect.Projections.IColumnLandmark`1",
      "Unrect.Projections.ISizeStrategy`1",
      "Unrect.Projections.IOffsetStrategy`1",
      "Unrect.Projections.IAreaStrategy`1",
      "Unrect.Projections.IRowStrategy`1",
      "Unrect.Projections.IColumnStrategy`1",
    };

    private readonly IReadOnlyList<INamedTypeSymbol> _demanding;

    private UnrectSymbols(
      INamedTypeSymbol space,
      INamedTypeSymbol projection,
      INamedTypeSymbol builders,
      INamedTypeSymbol stage,
      IReadOnlyList<INamedTypeSymbol> demanding,
      INamedTypeSymbol? cursor)
    {
      Space = space;
      Projection = projection;
      Builders = builders;
      Stage = stage;
      _demanding = demanding;
      Cursor = cursor;
    }

    /// <summary><c>Unrect.Core.ISpace</c> — the space a declaration asks for when it asks for nothing.</summary>
    public INamedTypeSymbol Space { get; }

    /// <summary><c>IProjection&lt;TSpace, TResult&gt;</c>, where a declaration's space is written.</summary>
    public INamedTypeSymbol Projection { get; }

    /// <summary><c>ProjectionBuilders&lt;TSpace&gt;</c>, the file-scoped vocabulary.</summary>
    public INamedTypeSymbol Builders { get; }

    /// <summary><c>PlacementStage&lt;TSpace&gt;</c>, the base of the pipeline stages.</summary>
    public INamedTypeSymbol Stage { get; }

    /// <summary><c>LayoutCursor&lt;TSpace&gt;</c>, the receiver a layout's child is declared on.</summary>
    public INamedTypeSymbol? Cursor { get; }

    /// <summary>True for the layout cursor — the receiver a child is declared on.</summary>
    public bool IsCursor(ITypeSymbol type)
      => SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, Cursor);

    /// <summary>The Unrect types in <paramref name="compilation"/>, or null if it has none.</summary>
    public static UnrectSymbols? TryLoad(Compilation compilation)
    {
      var space = compilation.GetTypeByMetadataName("Unrect.Core.ISpace");
      var projection = compilation.GetTypeByMetadataName("Unrect.Projections.IProjection`2");
      var builders = compilation.GetTypeByMetadataName("Unrect.Projections.ProjectionBuilders`1");
      var stage = compilation.GetTypeByMetadataName("Unrect.Projections.PlacementStage`1");

      if (space is null || projection is null || builders is null || stage is null)
        return null;

      // A phantom the referenced Unrect predates is simply absent: the list is what this
      // compilation has, and a demand nothing can express is a demand nothing can fail.
      var demanding = new List<INamedTypeSymbol> { projection };

      foreach (var name in PhantomNames)
      {
        if (compilation.GetTypeByMetadataName(name) is INamedTypeSymbol phantom)
          demanding.Add(phantom);
      }

      return new UnrectSymbols(
        space,
        projection,
        builders,
        stage,
        demanding,
        compilation.GetTypeByMetadataName("Unrect.Projections.LayoutCursor`1"));
    }

    /// <summary>
    /// The space <paramref name="type"/> asks for — the <c>TSpace</c> of the projection or matcher
    /// it is — or null where the type says nothing about a space at all.
    /// </summary>
    public ITypeSymbol? DemandOf(ITypeSymbol? type)
    {
      foreach (var definition in _demanding)
      {
        if (SpaceArgumentOf(type, definition) is ITypeSymbol demanded)
          return demanded;
      }

      return null;
    }

    /// <summary>True for <c>ISpace</c> itself — the space that asks for nothing.</summary>
    public bool IsSpace(ITypeSymbol type) => SymbolEqualityComparer.Default.Equals(type, Space);

    /// <summary>
    /// True when a space of type <paramref name="offered"/> satisfies a demand for
    /// <paramref name="demanded"/>.
    /// </summary>
    public static bool Satisfies(ITypeSymbol offered, ITypeSymbol demanded)
    {
      if (SymbolEqualityComparer.Default.Equals(offered, demanded))
        return true;

      foreach (var implemented in offered.AllInterfaces)
      {
        if (SymbolEqualityComparer.Default.Equals(implemented, demanded))
          return true;
      }

      for (var current = offered.BaseType; current is object; current = current.BaseType)
      {
        if (SymbolEqualityComparer.Default.Equals(current, demanded))
          return true;
      }

      return false;
    }

    /// <summary>
    /// The space argument of <paramref name="definition"/> as <paramref name="type"/> constructs it
    /// — first in every one of these types — preferring the most demanding construction where the
    /// type reaches the definition more than once.
    /// </summary>
    private ITypeSymbol? SpaceArgumentOf(ITypeSymbol? type, INamedTypeSymbol? definition)
    {
      if (type is null || definition is null)
        return null;

      ITypeSymbol? found = null;

      foreach (var candidate in Constructions(type, definition))
      {
        var argument = candidate.TypeArguments[0];

        // A matcher that names a capability may reach its definition through a base construction
        // over ISpace as well; the demanding one is the answer.
        if (found is null || (IsSpace(found) && !IsSpace(argument)))
          found = argument;
      }

      return found;
    }

    private static IEnumerable<INamedTypeSymbol> Constructions(
      ITypeSymbol type,
      INamedTypeSymbol definition)
    {
      if (type is INamedTypeSymbol named
        && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, definition))
      {
        yield return named;
      }

      foreach (var implemented in type.AllInterfaces)
      {
        if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, definition))
          yield return implemented;
      }
    }
  }
}
