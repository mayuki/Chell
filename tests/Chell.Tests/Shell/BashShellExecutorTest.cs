using System;
using Chell.Shell;
using FluentAssertions;
using Xunit;

namespace Chell.Tests.Shell
{
    public class BashShellExecutorTest
    {
        [Fact]
        public void Escape()
        {
            var executor = new BashShellExecutor();
            executor.Escape(@"").Should().Be(@"");
            executor.Escape(@"foo").Should().Be(@"foo");
            executor.Escape(@"123").Should().Be(@"123");
            executor.Escape(@"f oo").Should().Be(@"$'f oo'");
            executor.Escape(@"b'ar").Should().Be(@"$'b\'ar'");
            executor.Escape(@"b""ar").Should().Be(@"$'b\""ar'");
            executor.Escape(@"a\b").Should().Be(@"$'a\\b'");
        }

        [Fact]
        public void GetCommandAndArguments()
        {
            {
                var executor = new BashShellExecutor();
                executor.Path = "/bin/bash";
                executor.Prefix = "set -euo pipefail;";
                executor.GetCommandAndArguments("foo bar").Should().Be(("/bin/bash", "-c \"set -euo pipefail;foo bar\""));
            }
            {
                var executor = new BashShellExecutor();
                executor.Path = "/usr/local/bin/bash";
                executor.Prefix = "";
                executor.GetCommandAndArguments("foo bar").Should().Be(("/usr/local/bin/bash", "-c \"foo bar\""));
            }
        }
    }
}
