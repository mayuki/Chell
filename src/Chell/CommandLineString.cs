using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Chell
{
    /// <summary>
    /// Workaround for string/FormattableString overload issues
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerDisplay("CommandLineString: String={StringValue,nq}; FormattableString={FormattableStringValue,nq}")]
    public readonly struct CommandLineString
    {
        public string? StringValue { get; }
        public FormattableString? FormattableStringValue { get; }

        public CommandLineString(string value)
        {
            StringValue = value;
            FormattableStringValue = null;
        }

        public CommandLineString(FormattableString value)
        {
            StringValue = null;
            FormattableStringValue = value;
        }

        public static implicit operator CommandLineString(string value)
        {
            return new CommandLineString(value);
        }

        public static implicit operator CommandLineString(FormattableString value)
        {
            return new CommandLineString(value);
        }
    }
}