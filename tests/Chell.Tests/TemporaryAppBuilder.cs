﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chell.Tests
{
    public class TemporaryAppBuilder : IDisposable
    {
        private bool _disposed;

        public string ProjectName { get; }

        public string BaseDirectory { get; }
        public string SourceDirectory { get; }
        public string OutputDirectory { get; }

        private TemporaryAppBuilder(string projectName)
        {
            ProjectName = projectName;
            BaseDirectory = Path.Combine(Path.GetTempPath(), $"Chell.Tests-{Guid.NewGuid()}");
            SourceDirectory = Path.Combine(BaseDirectory, $"src");
            OutputDirectory = Path.Combine(BaseDirectory, $"out");
        }

        public static TemporaryAppBuilder Create(string projectName)
        {
            var builder = new TemporaryAppBuilder(projectName);
            builder.Initialize();
            return builder;
        }

        private void Initialize()
        {
            Directory.CreateDirectory(BaseDirectory);
            Directory.CreateDirectory(SourceDirectory);
            Directory.CreateDirectory(OutputDirectory);

            // Create .NET Console App project.
            RunInSourceDirectory("dotnet", $"new console -f net5.0 -n {ProjectName} -o .");
            // Explicitly use .NET 5 SDK. (AppHost is required for macOS with .NET 5 SDK)
            WriteSourceFile("global.json",
                @"{
                    ""sdk"": {
                        ""version"": ""5.0.100"",
                        ""rollForward"": ""latestFeature""
                    }
                }");
            WriteSourceFile("Directory.Build.props",
                @"<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
                    <PropertyGroup>
                        <UseAppHost>true</UseAppHost>
                    </PropertyGroup>
                </Project>");
        }

        public TemporaryAppBuilder WriteSourceFile(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(SourceDirectory, fileName), content, Encoding.UTF8);
            return this;
        }

        public Compilation Build()
        {
            RunInSourceDirectory("dotnet", $"publish -o \"{OutputDirectory}\"");

            return new Compilation(this, Path.Combine(OutputDirectory, ProjectName));
        }

        public class Compilation : IDisposable
        {
            private readonly TemporaryAppBuilder _builder;
            public string ExecutablePath { get; }

            public Compilation(TemporaryAppBuilder builder, string executablePath)
            {
                _builder = builder;
                ExecutablePath = executablePath;
            }

            public void Dispose()
            {
                _builder.Dispose();
            }
        }

        public TemporaryAppBuilder RunInSourceDirectory(string fileName, string arguments)
        {
            var procStartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments =  arguments,
                WorkingDirectory = SourceDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var proc = Process.Start(procStartInfo)!;
            proc.WaitForExit();
            var standardOutput = proc.StandardOutput.ReadToEnd();
            var standardError = proc.StandardError.ReadToEnd();


            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, $"The process has been exited with code {proc.ExitCode}. (FileName={fileName}, Arguments={arguments}", "Output:", standardOutput, "Error:", standardError));
            }

            return this;
        }

        public void Dispose()
        {
            if (_disposed) return;

            Directory.Delete(BaseDirectory, recursive:true);

            _disposed = true;
        }
    }
}
