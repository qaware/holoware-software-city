using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SoftwareCities.dynamic
{
    public abstract class DynamicDependenciesImporter
    {
        private int currentBucketIndex;

        /**
         * The start timestamp of the bucket as key and the list of all component loads within the bucket as value. 
         */
        protected readonly SortedDictionary<long, List<ComponentLoad>> Buckets =
            new SortedDictionary<long, List<ComponentLoad>>();

        private float minComponentLoad = float.MaxValue;
        private float maxComponentLoad = float.MinValue;

        private float minCallLoad = float.MaxValue;
        private float maxCallLoad = float.MinValue;
        
        public ComponentLoad GetActivityFor(string componentName)
        {
            ComponentLoad componentLoad = Buckets.ElementAt(currentBucketIndex).Value
                .Find(load => load.ComponentName.Equals(componentName));
            return componentLoad ?? new ComponentLoad(componentName, minComponentLoad, HttpStatusCodes.Success);
        }

        public float GetMinLoad()
        {
            return minComponentLoad;
        }

        public float GetMaxLoad()
        {
            return maxComponentLoad;
        }
        
        
        public float GetMinCallLoad()
        {
            return minCallLoad;
        }

        public float GetMaxCallLoad()
        {
            return maxCallLoad;
        }

        /// <summary>
        /// Normalize loads to generate light intensities that are somehow balanced. 
        /// </summary>
        protected void NormalizeLoads()
        {
            // Flatten
            List<Dictionary<HttpStatusCodes, float>> flatLoads = Buckets.Values
                .SelectMany(x => x)
                .ToList().Select(load => load.TotalLoad).ToList();
            List<float> flatLoadValues = flatLoads.Select(floats => floats.Sum(pair => pair.Value)).ToList();

            NormalizeLoadsList(flatLoadValues, flatLoads, out minComponentLoad, out maxComponentLoad);

            List<CallLoad> flatCallLoadDictionary = Buckets.Values
                .SelectMany(x => x)
                .ToList().SelectMany(load => load.CallLoads).ToList();
            List<float> flatCallLoadList =
                flatCallLoadDictionary.Select(loads => loads.Load.Sum(pair => pair.Value)).ToList();
            List<Dictionary<HttpStatusCodes, float>> allCallLoads =
                flatCallLoadDictionary.Select(callLoad => callLoad.Load).ToList();

            NormalizeLoadsList(flatCallLoadList, allCallLoads, out minCallLoad, out maxCallLoad);
        }

        /// <summary>
        /// Normalizes by interquartile range to not be prone to outliers.
        /// Makes the resulting values non-negative, to be able to later correctly assign weights to color hues.
        /// </summary>
        /// <param name="flatValues">Flattened list of values to normalize, to compute quartiles</param>
        /// <param name="valuesToChange">Values to normalize that are going to be changed in place</param>
        /// <param name="minValue">Minimum of normalized values</param>
        /// <param name="maxValue">Third quartile of normalized values</param>
        protected void NormalizeLoadsList(List<float> flatValues,
        IEnumerable<Dictionary<HttpStatusCodes, float>> valuesToChange, out float minValue, out float maxValue)
        {
            float firstQuartile = CalculatePercentile(flatValues.ToArray(), 0.25);
            float thirdQuartile = CalculatePercentile(flatValues.ToArray(), 0.75);
            float median = CalculatePercentile(flatValues.ToArray(), 0.5);

            if (Math.Abs(firstQuartile - thirdQuartile) < float.Epsilon)
            {
                maxValue = thirdQuartile;
                minValue = 0F;
                return;
            }

            minValue = float.MaxValue;

            foreach (var dict in valuesToChange)
            {
                foreach (HttpStatusCodes key in Enum.GetValues(typeof(HttpStatusCodes)))
                {
                    if (dict.ContainsKey(key))
                    {
                        float originalValue = dict[key];
                        float normalized = (originalValue - median) / (thirdQuartile - firstQuartile);
                        // make values non-negative
                        dict[key] = Math.Max((normalized + 1), 0) / 2;
                        if (dict[key] < minValue)
                        {
                            minValue = dict[key];
                        }
                    }
                }
            }

            // Set maxLoad for MinMaxScaling to normalized third quartile to make also shorter calls visible
            maxValue = (thirdQuartile - median) / (thirdQuartile - firstQuartile);
            maxValue = Math.Max((maxValue + 1), 0) / 2;
        }

        /// <summary>
        /// Calculates the given percentile of a given sequence. 
        /// </summary>
        /// <param name="sequence">for which the percentile should be calculated</param>
        /// <param name="percentile">the percentile that should be calculated, e.g., 0.25</param>
        /// <returns>the percentile</returns>
        private static float CalculatePercentile(float[] sequence, double percentile)
        {
            Array.Sort(sequence);
            int length = sequence.Length;
            double n = (length - 1) * percentile + 1;
            if (Math.Abs(n - 1d) < float.Epsilon) return sequence[0];
            if (Math.Abs(n - length) < float.Epsilon) return sequence[length - 1];
            int k = (int) n;
            double d = n - k;
            return (float) (sequence[k - 1] + d * (sequence[k] - sequence[k - 1]));
        }

        /// <summary>
        /// Called when the user requests the next time frame. Nothing happens when we reach the end.
        /// <returns>true if the index was incremented, false if the boundary has been reached</returns>
        /// </summary>
        public bool IncrementBucketIndex()
        {
            if (currentBucketIndex >= Buckets.Count - 1) return false;
            currentBucketIndex += 1;
            return true;
        }

        /// <summary>
        /// Called when the user requests the previous time frame. Nothing happens when we reach the beginning.
        /// <returns>true if the index was decremented, false if the boundary has been reached</returns>
        /// </summary>
        public bool DecrementBucketIndex()
        {
            if (currentBucketIndex <= 0) return false;
            currentBucketIndex -= 1;
            return true;
        }

        public enum HttpStatusCodes
        {
            Success,
            ClientError,
            ServerError
        }

        public static HttpStatusCodes StringToHttpStatusCode(string stringCode)
        {
            switch (stringCode)
            {
                case string a when a.StartsWith("4"): return HttpStatusCodes.ClientError;
                case string a when a.StartsWith("5"): return HttpStatusCodes.ServerError;
                default: return HttpStatusCodes.Success;
            }
        }

        public string GetCurrentTimestamp()
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(Buckets.ElementAt(currentBucketIndex).Key / 1000)
                .ToString("G");
        }
    }
}