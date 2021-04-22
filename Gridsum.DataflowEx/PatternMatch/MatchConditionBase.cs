using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gridsum.DataflowEx.PatternMatch
{
    /// <summary>
    /// IMatchCondition with a default implementation of MatchesExact
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MatchConditionBase<T> : IMatchCondition<T>
    {
        protected static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public abstract bool Matches(T input);

        public IMatchCondition<T> MatchesExact(T input)
        {
            if (this.Matches(input)) return this;
            else return null;
        }
    }
}
