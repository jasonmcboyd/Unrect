using System.Text;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The one-time code-page registration every reader in this assembly needs.
  /// <para>
  /// The legacy <c>.xls</c> format stores text in code pages that .NET Core does not carry by
  /// default, so without this a perfectly good workbook fails on a character. Both doors into the
  /// reader — the eager <see cref="SpreadsheetSpace"/> and the streaming cursor — must pass through
  /// here, which is why it is a named helper rather than a line repeated at each.
  /// </para>
  /// </summary>
  internal static class SpreadsheetEncodings
  {
    /// <summary>
    /// Registration happens once, in a static initializer, because that is what the runtime already
    /// guarantees to run exactly once and to publish safely. Hand-written double-checked locking
    /// would be more code for a weaker promise.
    /// </summary>
    private static readonly bool Registered = RegisterProvider();

    /// <summary>
    /// Registers the code-page provider, once per process. Cheap and safe to call on every open —
    /// callers should not have to remember whether someone else already did.
    /// </summary>
    internal static void Register()
    {
      // Touching the field is what forces the initializer; the value itself carries no information.
      _ = Registered;
    }

    private static bool RegisterProvider()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      return true;
    }
  }
}
