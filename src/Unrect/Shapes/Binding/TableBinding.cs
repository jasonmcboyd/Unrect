using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Unrect.Shapes
{
  /// <summary>
  /// Declares how one type's members bind to a table's captions, where the caption comparer would
  /// not have found them by itself. Immutable: every method returns a new instance, so a binding
  /// handed to two factories cannot be changed by either.
  /// </summary>
  public sealed class TableBinding<T>
  {
    internal TableBinding()
      : this(new Dictionary<string, string>(StringComparer.Ordinal), new List<string>())
    {
    }

    private TableBinding(IReadOnlyDictionary<string, string> captions, IReadOnlyCollection<string> ignored)
    {
      Captions = captions;
      Ignored = ignored;
    }

    internal IReadOnlyDictionary<string, string> Captions { get; }
    internal IReadOnlyCollection<string> Ignored { get; }

    /// <summary>
    /// Binds one member to a caption the comparer would not have found — a plural caption, a
    /// shorter member name, a heading with punctuation in it. The caption is still resolved through
    /// <see cref="CaptionComparer"/>, so this declares a <em>different caption</em>, not a different
    /// rule: <c>Column(t =&gt; t.Date, "Transaction Date")</c> still matches a header reading
    /// <c>"Transaction  Date"</c>.
    /// </summary>
    public TableBinding<T> Column<TMember>(Expression<Func<T, TMember>> member, string caption)
    {
      var name = MemberName(member, nameof(member), nameof(Column));

      if (string.IsNullOrWhiteSpace(caption))
        throw new ArgumentException("A column caption cannot be empty or whitespace.", nameof(caption));

      if (Captions.ContainsKey(name))
        throw new ArgumentException($"{typeof(T).Name}.{name} is bound twice.", nameof(member));

      var captions = new Dictionary<string, string>(Captions, StringComparer.Ordinal) { [name] = caption };

      return new TableBinding<T>(captions, Ignored);
    }

    /// <summary>
    /// Declares that one member is not read from the table. The opt-out is per member and by name,
    /// deliberately: a blanket "non-strict" flag would tolerate the <em>next</em> member somebody
    /// adds too, silently, which is the failure mode strictness exists to prevent.
    /// </summary>
    public TableBinding<T> Ignore<TMember>(Expression<Func<T, TMember>> member)
    {
      var name = MemberName(member, nameof(member), nameof(Ignore));
      var ignored = new List<string>(Ignored);

      // Ignoring twice is idempotent and harmless, so it is absorbed; binding twice is a
      // contradiction — two captions for one member — and stays an error.

      if (!ignored.Contains(name, StringComparer.Ordinal))
        ignored.Add(name);

      return new TableBinding<T>(Captions, ignored);
    }

    /// <summary>
    /// A selector must be a direct property access on the lambda parameter — <c>t =&gt; t.Date</c>,
    /// optionally wrapped in the compiler's boxing conversion. Anything deeper names something the
    /// binder cannot fill.
    /// </summary>
    private static string MemberName<TMember>(Expression<Func<T, TMember>> member, string parameter, string caller)
    {
      if (member is null)
        throw new ArgumentNullException(parameter);

      var body = member.Body is UnaryExpression conversion && conversion.NodeType == ExpressionType.Convert
        ? conversion.Operand
        : member.Body;

      if (body is MemberExpression access
        && access.Expression is ParameterExpression
        && access.Member is PropertyInfo property)
        return property.Name;

      throw new ArgumentException(
        $"{caller}({member}) does not select a property of {typeof(T).Name}; select a property directly.",
        parameter);
    }
  }
}
