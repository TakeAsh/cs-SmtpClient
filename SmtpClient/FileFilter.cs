using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TakeAsh {

    public struct FileFilterItem {

        public const string Separator = ";";

        public string Description { get; private set; }
        public IEnumerable<string> Patterns { get; private set; }

        public FileFilterItem(string description, IEnumerable<string> patterns)
            : this() {

            Description = description;
            Patterns = patterns.Select(pattern => Normalize(pattern));
        }

        public FileFilterItem(string description, string patterns)
            : this(description, patterns.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)) { }

        public override string ToString() {
            return Description + "|" + String.Join(Separator, Patterns);
        }

        public static string Normalize(string pattern) {
            if (String.IsNullOrEmpty(pattern)) {
                return null;
            }
            if (pattern.IndexOf('.') < 0) {
                pattern = "." + pattern;
            }
            if (pattern[0] != '*') {
                pattern = "*" + pattern;
            }
            return pattern;
        }
    }

    public static class IEnumerableFileFilterItemExtensionMethods {

        public static string ToFileFilter(this IEnumerable<FileFilterItem> list) {
            return String.Join("|", list);
        }

        public static int GetIndex(this IEnumerable<FileFilterItem> list, string pattern) {
            if (String.IsNullOrEmpty(pattern)) {
                return -1;
            }
            pattern = FileFilterItem.Normalize(pattern).ToLower();
            return list
                .ToList()
                .FindIndex(
                    item => item
                        .Patterns
                        .Select(p => p.ToLower())
                        .Contains(pattern)
                );
        }
    }
}
