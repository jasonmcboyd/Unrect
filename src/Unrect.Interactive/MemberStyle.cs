namespace Unrect.Interactive
{
  /// <summary>
  /// How <see cref="Scaffolding.ScaffoldClass{TSpace}"/> writes a member: the setter it gets, or
  /// none. Every style is one <c>Table&lt;T&gt;()</c> binds — through the properties for the three
  /// settable shapes, through the constructor for <see cref="ReadOnly"/>.
  /// </summary>
  public enum MemberStyle
  {
    /// <summary><c>{ get; init; }</c> — settable once, at construction; C# 9 and later.</summary>
    Init,

    /// <summary><c>{ get; set; }</c> — the plain property every C# can compile.</summary>
    Set,

    /// <summary><c>{ get; private set; }</c> — read-only from outside; the binder reaches the private setter.</summary>
    PrivateSet,

    /// <summary>
    /// A get-only property per label and one public constructor assigning every member in label
    /// order — the shape the binder fills through the constructor, as it fills a positional record.
    /// </summary>
    ReadOnly,
  }
}
