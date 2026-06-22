using System.Text;
using System.Text.RegularExpressions;

namespace SharpMemory.Core.Business.Segments.Validation;

public sealed class GitIgnoreMatcher
{
    private readonly IReadOnlyList<Rule> rules;

    private GitIgnoreMatcher(IReadOnlyList<Rule> rules)
    {
        this.rules = rules;
    }

    public static GitIgnoreMatcher Empty { get; } = new([]);

    public static GitIgnoreMatcher? Load(string rootPath)
    {
        var path = Path.Combine(rootPath, ".gitignore");
        if (!File.Exists(path))
        {
            return null;
        }

        var rules = File.ReadAllLines(path)
            .Select(Rule.TryParse)
            .OfType<Rule>()
            .ToArray();

        return new GitIgnoreMatcher(rules);
    }

    public bool IsIgnored(string normalizedRelativePath, bool isDirectory)
    {
        var ignored = false;
        foreach (var rule in rules)
        {
            if (rule.IsMatch(normalizedRelativePath, isDirectory))
            {
                ignored = !rule.IsNegated;
            }
        }

        return ignored;
    }


    private sealed class Rule
    {
        private readonly Regex regex;

        public bool IsNegated { get; }
        public bool DirectoryOnly { get; }

        private Rule(bool isNegated, bool directoryOnly, Regex regex)
        {
            IsNegated = isNegated;
            DirectoryOnly = directoryOnly;
            this.regex = regex;
        }

        public static Rule? TryParse(string raw)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                return null;
            }

            var isNegated = line.StartsWith('!');
            if (isNegated)
            {
                line = line[1..];
            }

            if (line.Length == 0)
            {
                return null;
            }

            var directoryOnly = line.EndsWith('/');
            var anchored = line.StartsWith('/');
            var hasSlash = line.Contains('/');
            var pattern = line.Trim('/');

            if (pattern.Length == 0)
            {
                return null;
            }

            return new Rule(isNegated, directoryOnly, BuildRegex(pattern, hasSlash, anchored));
        }

        public bool IsMatch(string path, bool isDirectory)
        {
            if (DirectoryOnly && !isDirectory)
            {
                return false;
            }

            return regex.IsMatch(path);
        }

        private static Regex BuildRegex(string pattern, bool hasSlash, bool anchored)
        {
            var body = TranslateGlob(pattern);
            var expr = (hasSlash && anchored)
                ? $"^{body}($|/.*)"
                : $"(^|.*/){body}($|/.*)";

            return new Regex(expr, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private static string TranslateGlob(string pattern)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < pattern.Length; i++)
            {
                var ch = pattern[i];
                if (ch == '*')
                {
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        sb.Append(".*");
                        i++;
                    }
                    else
                    {
                        sb.Append("[^/]*");
                    }
                }
                else if (ch == '?')
                {
                    sb.Append("[^/]");
                }
                else if (ch == '[')
                {
                    var close = pattern.IndexOf(']', i + 1);
                    if (close > i)
                    {
                        sb.Append(pattern[i..(close + 1)]);
                        i = close;
                    }
                    else
                    {
                        sb.Append("\\[");
                    }
                }
                else
                {
                    if ("+.()^${}|\\ ".Contains(ch))
                    {
                        sb.Append('\\');
                    }

                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
    }
}
