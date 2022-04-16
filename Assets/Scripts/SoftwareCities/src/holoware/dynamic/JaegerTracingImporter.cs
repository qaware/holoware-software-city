using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoftwareCities.holoware.lsm;
using UnityEngine;

namespace SoftwareCities.dynamic
{
    public class JaegerTracingImporter : DynamicDependenciesImporter
    {
        private const string ClassTagName = "class";

        private long aggregationIntervalInUs;

        /// <summary>
        /// Load the JSON span file extracted from elasticsearch 
        /// </summary>
        /// <param name="spanDirectoryPath">path to the span file</param>
        /// <param name="timeSpan">The time to aggregate over</param>
        /// <returns>The resulting <see cref="JaegerTracingImporter"/></returns>
        public static JaegerTracingImporter LoadDynamicDependencies(string spanDirectoryPath, TimeSpan timeSpan)
        {
            JaegerTracingImporter importer = new JaegerTracingImporter {aggregationIntervalInUs = timeSpan.Ticks / 10};

            // get all trace file names in directory 
            List<string> filenames = Directory
                .EnumerateFiles(spanDirectoryPath, "*", SearchOption.AllDirectories)
                .Select(Path.GetFileName).ToList();

            // no trace files, we're done 
            if (filenames.Count <= 0) return importer;

            // get start and end timestamp from filenames by convention traces_start_end.json -> min. start and max. end
            long startTimestamp = long.Parse(filenames[0].Split('_')[0]);
            long endTimestamp = long.Parse(filenames[filenames.Count - 1].Split('_')[filenames.Count - 1]);

            // generate buckets from start to end timestamp
            GenerateBucketTimespans( startTimestamp, endTimestamp, importer);

            ImportComponentLoads(spanDirectoryPath, importer);

            return importer;
        }

        /// <summary>
        /// Iterate over all trace files, drop when no class name found and sort spans with class name and load in right bucket 
        /// </summary>
        /// <param name="spanFilePath">Where the trace files are stored</param>
        /// <param name="timeSpan"></param>
        /// <param name="result"></param>
        private static void ImportComponentLoads(string spanFilePath, JaegerTracingImporter result)
        {
            foreach (string file in Directory.EnumerateFiles(spanFilePath, "*.json"))
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(file)))
                {
                    string jsonString = reader.ReadToEnd();
                    Traces traces = JsonUtility.FromJson<Traces>(jsonString);
                    foreach (Trace trace in traces.data)
                    {
                        Trace spansExceededBucket = ProcessTrace(trace, result);
                        while (spansExceededBucket.spans.Count > 0)
                        {
                            spansExceededBucket = ProcessTrace(spansExceededBucket, result);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate buckets from start to end timestamp
        /// </summary>
        /// <param name="startTimestamp">start</param>
        /// <param name="endTimestamp">end</param>
        /// <param name="importer">to generate the buckets for</param>
        private static void GenerateBucketTimespans(long startTimestamp, long endTimestamp,
            JaegerTracingImporter importer)
        {
            while (startTimestamp < endTimestamp)
            {
                importer.Buckets.Add(startTimestamp, new List<ComponentLoad>());
                // Ticks are 0.1us, we have us
                startTimestamp += importer.aggregationIntervalInUs;
            }
        }

        private static Trace ProcessTrace(Trace trace, JaegerTracingImporter importer)
        {
            Trace addedSpans = new Trace();
            foreach (Span span in trace.spans)
            {
                Tag classTag = span.tags.Find(tag => tag.key.Contains(ClassTagName));
                if (classTag == null) break;
                (long bucketTimestamp, List<ComponentLoad> componentLoads) = importer.Buckets.FirstOrDefault(pair =>
                    pair.Key >= span.startTime && pair.Key < span.startTime + importer.aggregationIntervalInUs);
                GenerateSplitSpan(span, bucketTimestamp + importer.aggregationIntervalInUs, addedSpans);
                ComponentLoad existingLoad = componentLoads.Find(load => load.ComponentName.Contains(classTag.value));
                if (existingLoad == null)
                {
                    componentLoads.Add(new ComponentLoad(classTag.value, span.duration, HttpStatusCodes.Success));
                }
                else
                {
                    existingLoad.TotalLoad[HttpStatusCodes.Success] = +span.duration;
                }
            }

            return addedSpans;
        }

        /// <summary>
        /// If a span spans (haha) longer than the bucket, it is split so that it also appears in the next bucket. 
        /// </summary>
        /// <param name="spanInBucket">The spans in the bucket that potentially exceed the bucket time range</param>
        /// <param name="newStartTimestamp">Where the new bucket starts</param>
        /// <param name="trace">For the next bucket</param>
        private static void GenerateSplitSpan(Span spanInBucket, long newStartTimestamp, Trace trace)
        {
            if (spanInBucket.startTime + spanInBucket.duration < newStartTimestamp) return;
            Span splitSpan = new Span(spanInBucket.operationName,
                spanInBucket.startTime + spanInBucket.duration - newStartTimestamp,
                newStartTimestamp, spanInBucket.tags);
            trace.spans.Add(splitSpan);
            spanInBucket.duration = newStartTimestamp - spanInBucket.startTime;
        }

        /// <summary>
        /// Helper classes for JSON deserialization of Jaeger traces.
        /// </summary>
        [Serializable]
        internal class Traces
        {
            public List<Trace> data;
        }

        [Serializable]
        internal class Trace
        {
            public List<Span> spans;
        }

        [Serializable]
        internal class Span
        {
            public string operationName;
            public long duration;
            public long startTime;
            public List<Tag> tags;

            public Span(string operationName, long duration, long startTime, List<Tag> tags)
            {
                this.operationName = operationName;
                this.duration = duration;
                this.startTime = startTime;
                this.tags = tags;
            }
        }

        [Serializable]
        internal class Tag
        {
            public string key;
            public string value;
        }
    }
}