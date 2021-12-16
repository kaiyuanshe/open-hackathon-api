using System;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public enum ComparisonOperator
    {
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        NotEqual,
    }

    public static class ComparisonOperatorExtension
    {
        public static string ToOperator(this ComparisonOperator comparisonOperators)
        {
            switch (comparisonOperators)
            {
                case ComparisonOperator.Equal:
                    return "eq";
                case ComparisonOperator.GreaterThan:
                    return "gt";
                case ComparisonOperator.GreaterThanOrEqual:
                    return "ge";
                case ComparisonOperator.LessThan:
                    return "lt";
                case ComparisonOperator.LessThanOrEqual:
                    return "le";
                case ComparisonOperator.NotEqual:
                    return "ne";
                default:
                    throw new ArgumentOutOfRangeException("unknown ComparisonOperators");
            }
        }
    }
}
