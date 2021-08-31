using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Chell.Tests
{
    public class CommandLineStringTest
    {
        [Fact]
        public void StaticMethodPreferString()
        {
            StaticMethodOverloadedTest.A("Foo {0}").Should().Be($"{nameof(CommandLineString)}: StringValue=Foo {{0}}; FormattableStringValue=");
        }
        [Fact]
        public void StaticMethodPreferFormattableString()
        {
            StaticMethodOverloadedTest.A($"Foo {0}").Should().Be($"{nameof(FormattableString)}: Foo {{0}}; 1");
        }

        private static class StaticMethodOverloadedTest
        {
            public static string A(CommandLineString s) => $"{nameof(CommandLineString)}: StringValue={s.StringValue}; FormattableStringValue={s.FormattableStringValue}";
            public static string A(FormattableString s) => $"{nameof(FormattableString)}: {s.Format}; {s.ArgumentCount}";
        }

        [Fact]
        public void InstanceMethodPreferString()
        {
            new InstanceMethodOverloadedTest().A("Foo {0}").Should().Be($"{nameof(CommandLineString)}: StringValue=Foo {{0}}; FormattableStringValue=");
        }
        [Fact]
        public void InstanceMethodPreferFormattableString()
        {
            new InstanceMethodOverloadedTest().A($"Foo {0}").Should().Be($"{nameof(FormattableString)}: Foo {{0}}; 1");
        }

        private class InstanceMethodOverloadedTest
        {
            public string A(CommandLineString s) => $"{nameof(CommandLineString)}: StringValue={s.StringValue}; FormattableStringValue={s.FormattableStringValue}";
            public string A(FormattableString s) => $"{nameof(FormattableString)}: {s.Format}; {s.ArgumentCount}";
        }

        [Fact]
        public void ImplicitCastConstructorPreferString()
        {
            new ConstructorOverloadedTest("Foo {0}").Result.Should().Be($"{nameof(CommandLineString)}: StringValue=Foo {{0}}; FormattableStringValue=");
        }
        [Fact]
        public void ImplicitCastConstructorPreferFormattableString()
        {
            new ConstructorOverloadedTest($"Foo {0}").Result.Should().Be($"{nameof(FormattableString)}: Foo {{0}}; 1");
        }

        private class ConstructorOverloadedTest
        {
            public string Result { get; }

            public ConstructorOverloadedTest(CommandLineString s)
            {
                Result = $"{nameof(CommandLineString)}: StringValue={s.StringValue}; FormattableStringValue={s.FormattableStringValue}";
            }

            public ConstructorOverloadedTest(FormattableString s)
            {
                Result = $"{nameof(FormattableString)}: {s.Format}; {s.ArgumentCount}";
            }
        }
    }
}
