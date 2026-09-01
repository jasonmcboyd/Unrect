namespace Unrect.Shapes
{
  /// <summary>
  /// A shape whose children exist only while it runs. Tooling that walks a declaration without a
  /// space would otherwise read an empty <c>Children</c> as "leaf", which is a lie; this says so
  /// instead.
  /// </summary>
  internal interface IOpaqueComposite
  {
    /// <summary>Why the children are missing, for a renderer to show in their place.</summary>
    string Reason { get; }
  }
}
