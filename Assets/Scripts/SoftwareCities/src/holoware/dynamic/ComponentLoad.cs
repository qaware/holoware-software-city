using System.Collections.Generic;
using UnityEngine;

namespace SoftwareCities.dynamic
{
    /// <summary>
    /// Class to store component's load within a bucket.
    /// For every HttpStatusCode stores the duration of all spans within one bucket.
    /// </summary>
    public class ComponentLoad
    {
        public readonly string ComponentName;

        public Dictionary<DynamicDependenciesImporter.HttpStatusCodes, float> TotalLoad =
            new Dictionary<DynamicDependenciesImporter.HttpStatusCodes, float>();

        public List<CallLoad> CallLoads;

        public ComponentLoad(string componentName)
        {
            ComponentName = componentName;
            TotalLoad = new Dictionary<DynamicDependenciesImporter.HttpStatusCodes, float>();
            CallLoads = new List<CallLoad>();
        }

        /// <summary>
        /// Adds information from a span to TotalLoad and CallLoads.
        /// </summary>
        /// <param name="span">the span to add</param>
        /// <param name="aggregateTraces">whether the loads should be summed together (true), or there should be
        /// a separate CallLoad for every span (false) in which case there may be multiple CallLoads with the same
        /// parent component.</param>
        public void AddSpan(Span span, bool aggregateTraces)
        {
            if (span.ComponentName != ComponentName)
            {
                Debug.LogWarning($"Trying to add span to a wrong component! ({span.ComponentName} vs. {ComponentName}");
            }

            if (aggregateTraces)
            {
                AddLoadAggregated(span.ParentName, span.Duration, span.StatusCode);
            }
            else
            {
                AddLoadUnaggregated(span.ParentName, span.Duration, span.StatusCode);
            }
        }

        private void AddLoadUnaggregated(string parentName, float load,
            DynamicDependenciesImporter.HttpStatusCodes statusCode)
        {
            if (TotalLoad.Count == 0)
            {
                TotalLoad.Add(statusCode, float.MaxValue);
            }

            if (parentName != null && parentName != ComponentName)
            {
                AddCallLoadUnaggregated(parentName, load, statusCode);
            }
        }

        private void AddCallLoadUnaggregated(string parentName, float load,
            DynamicDependenciesImporter.HttpStatusCodes statusCode)
        {
            CallLoads.Add(new CallLoad(parentName, float.MaxValue, statusCode));
        }

        private void AddLoadAggregated(string parentName, float load,
            DynamicDependenciesImporter.HttpStatusCodes statusCode)
        {
            if (!TotalLoad.ContainsKey(statusCode))
            {
                TotalLoad.Add(statusCode, load);
            }
            else
            {
                TotalLoad[statusCode] += load;
            }

            if (parentName != null && parentName != ComponentName)
            {
                AddCallLoadAggregated(parentName, load, statusCode);
            }
        }

        private void AddCallLoadAggregated(string parentName, float load,
            DynamicDependenciesImporter.HttpStatusCodes statusCode)
        {
            CallLoad existingLoad = CallLoads.Find(callLoad => callLoad.ParentName.Equals(parentName));
            if (existingLoad == null)
            {
                CallLoads.Add(new CallLoad(parentName, load, statusCode));
            }
            else
            {
                if (existingLoad.Load.ContainsKey(statusCode))
                {
                    existingLoad.Load[statusCode] += load;
                }
                else
                {
                    existingLoad.Load.Add(statusCode, load);
                }
            }
        }

        public ComponentLoad(string componentName, float load, DynamicDependenciesImporter.HttpStatusCodes statusCode)
        {
            ComponentName = componentName;
            TotalLoad.Add(statusCode, load);
            CallLoads = new List<CallLoad>();
        }
    }

    /// <summary>
    /// Helper class to store the status code and duration of all spans for a call from a parent class.
    /// </summary>
    public class CallLoad
    {
        public readonly string ParentName;

        public Dictionary<DynamicDependenciesImporter.HttpStatusCodes, float> Load =
            new Dictionary<DynamicDependenciesImporter.HttpStatusCodes, float>();

        public CallLoad(string parentName, float load, DynamicDependenciesImporter.HttpStatusCodes statusCode)
        {
            ParentName = parentName;
            Load.Add(statusCode, load);
        }
    }
}