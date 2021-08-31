using System;
using Chell.Shell;
using FluentAssertions;
using Xunit;

namespace Chell.Tests.Shell
{
    public class CmdShellExecutorTest
    {
        [Fact]
        public void Escape()
        {
            var executor = new CmdShellExecutor();
            executor.Escape(@"foo").Should().Be(@"foo");
            executor.Escape(@"123").Should().Be(@"123");
            executor.Escape(@"Program Files").Should().Be(@"""Program Files""");
            executor.Escape(@"b'ar").Should().Be(@"""b'ar""");
            executor.Escape(@"b""ar").Should().Be(@"""b\""ar""");
            executor.Escape(@"a\b").Should().Be(@"a\b");
            executor.Escape(@"^").Should().Be(@"""^^""");
            executor.Escape(@"<").Should().Be(@"""^<""");
            executor.Escape(@">").Should().Be(@"""^>""");
            executor.Escape(@"|").Should().Be(@"""^|""");
            executor.Escape(@"&").Should().Be(@"""^&""");
            executor.Escape(@"&&").Should().Be(@"""^&^&""");

            // \  --> \
            // "  --> "\""
            // \" --> "\\\""
            executor.Escape(@"\").Should().Be(@"\");
            executor.Escape(@"""").Should().Be(@"""\""""");
            executor.Escape(@"\""").Should().Be(@"""\\\""""");

            // "\" --> "\"\\\""
            executor.Escape(@"""\""").Should().Be("\"\\\"\\\\\\\"\"");

            // "\"'|<>[] --> "\"\\\"'^|^<^>[]""
            executor.Escape("\"\\\"'|<>[]").Should().Be("\"\\\"\\\\\\\"'^|^<^>[]\"");

            // "\'|<> --> "\"\'^|^<^>"
            executor.Escape("\"\\'|<>").Should().Be("\"\\\"\\'^|^<^>\"");

            // A B\C D --> "A B\C D"
            // A "B\C D --> "A \"B\C D"
            executor.Escape(@"A B\C D").Should().Be(@"""A B\C D""");
            executor.Escape(@"A ""B\C D").Should().Be(@"""A \""B\C D""");
        }

        [Fact]
        public void GetCommandAndArguments()
        {
            var executor = new CmdShellExecutor();
            executor.GetCommandAndArguments("foo bar").Should().Be(("cmd", "/c \"foo bar\""));
        }
    }
}
