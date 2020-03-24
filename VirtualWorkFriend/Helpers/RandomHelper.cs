namespace MyAssistant_1.Helpers
{
    using System;
    using System.Collections.Generic;

    public static class RandomHelper
    {
        private static Random _random = new Random((int)DateTime.UtcNow.Ticks);

        public static int GetRandom(int left, int right)
        {
            return _random.Next(left, right);
        }

        /// <summary>
        /// select randomly a subset of size k from online data
        /// </summary>
        public static IEnumerable<T> GetRandomSubset<T>(IEnumerable<T> data, int k)
        {
            var result = new List<T>();
            int step = 0;
            foreach (var el in data)
            {
                step++;
                if (step <= k)
                {
                    // initially store in the result the first k elements in the collection
                    result.Add(el);
                    continue;
                }

                // at step i, probability to be selected is k/i
                int pos = _random.Next(step);
                if (pos < k)
                {
                    // store element in the result at random position
                    result[pos] = el;
                }
            }
            return result;
        }
    }
}
