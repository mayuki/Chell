using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Chell.Tests
{
    public class ChellEnvironmentTest
    {
        [Fact]
        public void HomeDirectory()
        {
            var env = new ChellEnvironment();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                env.HomeDirectory.Should().Be(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }
            else
            {
                env.HomeDirectory.Should().Be(Environment.GetEnvironmentVariable("HOME"));
            }
        }
    }
}
