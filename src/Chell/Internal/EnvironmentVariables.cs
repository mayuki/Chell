using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Chell.Internal
{
    internal class EnvironmentVariables : IDictionary<string, string>
    {
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Environment.GetEnvironmentVariables()
                .OfType<DictionaryEntry>()
                .Select(x => KeyValuePair.Create((string)x.Key, (string)(x.Value ?? string.Empty)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(item.Key));
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            if (Contains(item))
            {
                Environment.SetEnvironmentVariable(item.Key, null);
                return true;
            }

            return false;
        }

        public int Count => Environment.GetEnvironmentVariables().Count;
        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        public bool ContainsKey(string key)
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key));
        }

        public bool Remove(string key)
        {
            if (ContainsKey(key))
            {
                Environment.SetEnvironmentVariable(key, null);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out string value)
        {
            var tmpValue = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(tmpValue))
            {
                value = string.Empty;
                return false;
            }

            value = tmpValue;
            return true;
        }

        public string this[string key]
        {
            get => TryGetValue(key, out var value) ? value : string.Empty;
            set => Add(key, value);
        }

        public ICollection<string> Keys
            => Environment.GetEnvironmentVariables().OfType<DictionaryEntry>().Select(x => (string)x.Key).ToArray();
        public ICollection<string> Values
            => Environment.GetEnvironmentVariables().OfType<DictionaryEntry>().Select(x => (string)(x.Value ?? string.Empty)).ToArray();
    }
}