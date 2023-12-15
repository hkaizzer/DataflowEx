using System;
using Microsoft.Extensions.Logging;

namespace Gridsum.DataflowEx.PatternMatch
{
    public class UrlStringMatchCondition : StringMatchCondition
    {
        private readonly bool m_ignoreCase;
        private readonly ILogger<StringMatchCondition> _logger;

        public UrlStringMatchCondition(string matchPattern, MatchType matchType, bool excludeParam, bool ignoreCase = true, ILogger<StringMatchCondition> logger = null) : base(matchPattern, matchType)
        {
            this.m_ignoreCase = ignoreCase;
            _logger = logger;
            ExcludeParam = excludeParam;
        }

        public override bool Matches(string input)
        {
            if (input == null) return false;

            if (this.ExcludeParam)
            {
                input = GetUrlWithoutParam(input);
            }

            if (m_ignoreCase)
            {
                switch (MatchType)
                {
                    case MatchType.ExactMatch:
                        return string.Equals(MatchPattern, input, StringComparison.OrdinalIgnoreCase);
                    case MatchType.BeginsWith:
                        return input.StartsWith(MatchPattern, StringComparison.OrdinalIgnoreCase);
                    case MatchType.EndsWith:
                        return input.EndsWith(MatchPattern, StringComparison.OrdinalIgnoreCase);
                    case MatchType.Contains:
                        return input.IndexOf(MatchPattern, StringComparison.OrdinalIgnoreCase) >= 0;
                    case MatchType.RegexMatch:
                        return Regex.IsMatch(input);
                    default:
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Invalid given enum value MatchType {0}. Using 'Contains' instead.", MatchType);
                        }
                        return input.Contains(MatchPattern);
                }
            }
            else
            {
                return base.Matches(input);
            }
        }

        public bool ExcludeParam { get; private set; }

        private static readonly char[] s_urlParamChars = new[] { '?', '#' };

        public static string GetUrlWithoutParam(string url)
        {
            url = url.Trim();

            int index = url.IndexOfAny(s_urlParamChars);

            if (index >= 0)
            {
                url = url.Substring(0, index);
            }

            return url;
        }
    }
}
