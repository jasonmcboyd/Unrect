using System.Runtime.InteropServices;

namespace Unrect.Interactive
{
  /// <summary>
  /// What the scaffolder assumes about the compiler that will see its paste, read off the runtime
  /// it is running on: the scaffolder runs in the very process whose editor the source is pasted
  /// into, and LINQPad 5 is .NET Framework with C# 7.3, where <c>init</c> and positional records do
  /// not exist. The runtime is a default and never the truth — a .NET Framework project on the
  /// modern SDK compiles C# 9 with its own <c>IsExternalInit</c> — so every caller may say
  /// otherwise through the <see cref="Interactive.MemberStyle"/> parameter.
  /// </summary>
  internal static class RuntimeDefaults
  {
    /// <summary>Whether this process is .NET Framework, where the newest C# the in-box compilers speak is 7.3.</summary>
    internal static bool IsNetFramework
      => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", System.StringComparison.Ordinal);

    /// <summary>The member style a class is written in when the caller does not say: <see cref="MemberStyle.Set"/> on .NET Framework, <see cref="MemberStyle.Init"/> elsewhere.</summary>
    internal static MemberStyle MemberStyle => IsNetFramework ? Interactive.MemberStyle.Set : Interactive.MemberStyle.Init;
  }
}
