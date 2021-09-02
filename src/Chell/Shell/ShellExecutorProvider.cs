using System;
using System.Runtime.InteropServices;
using Chell.Shell;

namespace Chell.Shell
{
    public class ShellExecutorProvider
    {
        public IShellExecutor Executor { get; private set; } = GetPlatformPreferredExecutor();

        public void SetExecutor(IShellExecutor shellExecutor)
        {
            Executor = shellExecutor ?? throw new ArgumentNullException(nameof(shellExecutor));
        }

        internal static IShellExecutor GetPlatformPreferredExecutor()
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new CmdShellExecutor()
                : BashShellExecutor.AutoDetectedPath != null
                    ? new BashShellExecutor()
                    : new NoUseShellExecutor();
    }
}

namespace Chell
{
    public static class ShellExecutorProviderExtensions
    {
        public static void NoUseShell(this ShellExecutorProvider provider)
        {
            provider.SetExecutor(new NoUseShellExecutor());
        }
        public static void UseBash(this ShellExecutorProvider provider, string? prefix = null)
        {
            provider.SetExecutor(new BashShellExecutor(prefix));
        }
        public static void UseCmd(this ShellExecutorProvider provider)
        {
            provider.SetExecutor(new CmdShellExecutor());
        }
        public static void UseDefault(this ShellExecutorProvider provider)
        {
            provider.SetExecutor(ShellExecutorProvider.GetPlatformPreferredExecutor());
        }
    }
}
