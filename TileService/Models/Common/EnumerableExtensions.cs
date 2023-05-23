using System.Collections.Generic;

namespace TileService.Models.Common
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<int> CumulativeSum(this IEnumerable<int> sequence)
        {
            int sum = 0;
            foreach (var item in sequence)
            {
                sum += item;
                yield return sum;
            }
        }
    }
}
