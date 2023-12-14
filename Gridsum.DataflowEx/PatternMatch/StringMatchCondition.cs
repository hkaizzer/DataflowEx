﻿using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Gridsum.DataflowEx.PatternMatch
{
    /// <summary>
    /// A simple condition implementation for string. 
    /// </summary>
	public class StringMatchCondition : MatchConditionBase<string>
	{
        private readonly ILogger<StringMatchCondition> _logger;

        public StringMatchCondition(string matchPattern, MatchType matchType = MatchType.ExactMatch, ILogger<StringMatchCondition> logger = null)
		{
            if (matchPattern == null)
            {
                throw new ArgumentNullException("matchPattern");
            }

            _logger = logger;

            this.MatchPattern = matchPattern;
			this.MatchType = matchType;
			
            if (matchType == MatchType.RegexMatch)
            {
                this.Regex = new Regex(matchPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);    
            }            
		}

		public string MatchPattern { get; private set; }
        public MatchType MatchType { get; private set; }
        
		public Regex Regex { get; set; }

	    public override bool Matches(string input)
	    {
	        if (input == null)
	        {
	            return false;
	        }

            switch (MatchType)
            {
                case MatchType.ExactMatch:
                    // 精确匹配
                    return MatchPattern == input;
                case MatchType.BeginsWith:
                    // 处理左匹配
                    return input.StartsWith(MatchPattern, StringComparison.Ordinal);
                case MatchType.EndsWith:
                    // 处理右匹配
                    return input.EndsWith(MatchPattern, StringComparison.Ordinal);
                case MatchType.Contains:
                    // 处理包含情况
                    return input.Contains(MatchPattern);
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
	}
}