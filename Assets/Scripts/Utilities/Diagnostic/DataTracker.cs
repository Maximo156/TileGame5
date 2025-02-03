using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Diagnostic
{
    public class DataTracker
    {
        int outputAfter;
        int sinceLastOutput = 0;

        List<float> points = new();
        public DataTracker(int outputAfter)
        {
            this.outputAfter = outputAfter;
        }

        /// <summary>
        /// Add point
        /// </summary>
        /// <param name="point">data point</param>
        /// <returns>true if point is noteworthy (outlier)</returns>
        public bool AddPoint(float point)
        {
            points.Add(point);
            points.Sort();
            ++sinceLastOutput;
            if(sinceLastOutput > outputAfter)
            {
                PrintStats();
                sinceLastOutput = 0;
            }
            return IsOutlier(point);
        }

        bool IsOutlier(float point)
        {
            var Q1 = points.ElementAt(points.Count / 4);
            var Q3 = points.ElementAt(points.Count / 4 * 3);
            var iqr = Q3 - Q1;
            var Q2 = points.ElementAt(points.Count / 2);
            if(iqr == 0)
            {
                return false;
            }
            return point < Q2 - (1.5f * iqr);
        }

        void PrintStats()
        {
            Debug.Log($"Avg: {points.Average()} Std: {StandardDeviation(points)}");
        }

        public double StandardDeviation(IEnumerable<float> values)
        {
            float avg = values.Average();
            return Mathf.Sqrt(values.Average(v => Mathf.Pow(v - avg, 2)));
        }
    }
}
