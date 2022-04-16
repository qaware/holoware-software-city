using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoftwareCities.holoware.lsm;
using UnityEngine;

namespace SoftwareCities.dynamic
{
    public class ElasticSearchImporter : DynamicDependenciesImporter
    {
        private long aggregationIntervalInUs;

        private long minTimestamp = long.MaxValue;
        private long maxTimestamp = long.MinValue;
        private bool aggregateTraces;

        private ElasticSearchTransactions elasticSearchTransactions;

        public ElasticSearchImporter(TimeSpan timeSpan, bool aggregateTraces)
        {
            // Ticks are 0.1us
            this.aggregationIntervalInUs = timeSpan.Ticks / 10;
            this.aggregateTraces = aggregateTraces;
        }

        /// <summary>
        /// Load the JSON span file extracted from elasticsearch 
        /// </summary>
        /// <param name="spanDirectoryPath">path to the span files</param>
        /// <param name="timeSpan">The time to aggregate over</param>
        /// <param name="shouldBeAggregated">whether or not the traces should be aggregated inside the buckets</param>
        /// <returns>The resulting <see cref="ElasticSearchImporter"/></returns>
        public void LoadDynamicDependencies(string spanDirectoryPath)
        {
            this.elasticSearchTransactions = ProcessTransactionFiles(spanDirectoryPath);

            string[] spanFiles =
                Directory.GetFiles(spanDirectoryPath, "spans*.json", SearchOption.TopDirectoryOnly);

            foreach (string spanFile in spanFiles)
            {
                Debug.Log("Processing span file " + spanFile + "...");
                ProcessSpanFile(spanFile);
                Debug.Log("Done!");
            }

            NormalizeLoads();
        }

        /// <summary>
        /// Reads all transaction files and returns flattened transaction.
        /// </summary>
        /// <param name="transactionsDirectoryPath">Path to the transaction files</param>
        /// <returns>One <see cref="ElasticSearchTransactions"/> that contains flattened Hits from all transaction files</returns>
        private static ElasticSearchTransactions ProcessTransactionFiles(string transactionsDirectoryPath)
        {
            string[] transactionFiles =
                Directory.GetFiles(transactionsDirectoryPath, "transaction*.json", SearchOption.TopDirectoryOnly);
            List<ElasticSearchTransactions> transactionsList = new List<ElasticSearchTransactions>();
            foreach (string transactionFile in transactionFiles)
            {
                Debug.Log("Processing transaction file " + transactionFile + "...");
                using (StreamReader reader = new StreamReader(File.OpenRead(transactionFile)))
                {
                    string jsonString = reader.ReadToEnd();
                    ElasticSearchTransactions elasticSearchTransactions =
                        JsonUtility.FromJson<ElasticSearchTransactions>(jsonString);
                    transactionsList.Add(elasticSearchTransactions);
                }

                Debug.Log("Done!");
            }

            ElasticSearchTransactions flattenedTransactions = transactionsList[0];
            for (int i = 1; i < transactionsList.Count; i++)
            {
                flattenedTransactions.hits.hits.AddRange(transactionsList[i].hits.hits);
            }

            return flattenedTransactions;
        }

        /// <summary>
        /// Reads a span file and processes all spans within it.
        /// </summary>
        /// <param name="spanFileName">Name of the span file</param>
        private void ProcessSpanFile(string spanFileName)
        {
            using (StreamReader reader = new StreamReader(File.OpenRead(spanFileName)))
            {
                string jsonString = reader.ReadToEnd();
                ElasticSearchSpans elasticSearchSpans = JsonUtility.FromJson<ElasticSearchSpans>(jsonString);
                List<Span> spans = SpanMapper.ToSpans(elasticSearchSpans);
                Debug.Log("Number of spans in file: " + spans.Count);
                long startTimestamp = spans.Min(span => span.Timestamp);
                long endTimestamp = spans.Max(span => span.Timestamp + span.Duration);

                ExtendTimeRange(startTimestamp, endTimestamp);

                foreach (Span span in spans)
                {
                    Span spanExceededBucket = ProcessSpan(span);
                    while (spanExceededBucket != null)
                    {
                        spanExceededBucket = ProcessSpan(spanExceededBucket);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a ComponentLoad from the span in the corresponding time bucket.
        /// </summary>
        /// <param name="span">Span to process</param>
        /// <returns>Remaining part of the span if its duration exceeds the bucket.</returns>
        private Span ProcessSpan(Span span)
        {
            (long bucketTimestamp, List<ComponentLoad> componentLoads) = Buckets.FirstOrDefault(pair =>
                pair.Key >= span.Timestamp && pair.Key < span.Timestamp + aggregationIntervalInUs);
            Span splitSpan = GenerateSplitSpan(span, bucketTimestamp);
            ComponentLoad existingLoad =
                componentLoads.Find(load => load.ComponentName.Equals(span.ComponentName));
            ElasticSearchTransactions.Source transaction =
                elasticSearchTransactions.hits.hits.Find(source => source._source.transaction.id.Equals(span.TransactionId));
            if (transaction != null)
            {
                span.StatusCode = StringToHttpStatusCode(transaction._source.http.response.status_code);
            }

            if (existingLoad == null)
            {
                existingLoad = new ComponentLoad(span.ComponentName);
                componentLoads.Add(existingLoad);
            }
            existingLoad.AddSpan(span, aggregateTraces);

            return splitSpan;
        }

        /// <summary>
        /// If a span spans (haha) longer than the bucket, it is split so that it also appears in the next bucket. 
        /// </summary>
        /// <param name="span">The span that potentially exceeds the bucket time range. If it does, its duration is
        /// shortened to the max duration of the bucket, and the remaining time is put into the new <see cref="Span"/>
        /// that gets returned.</param>
        /// <param name="bucketTimestamp">Where the current bucket starts</param>
        /// <returns>Remaining part of the span</returns>
        private Span GenerateSplitSpan(Span span, long bucketTimestamp)
        {
            Span splitSpan = null;
            if (span.Timestamp + span.Duration > bucketTimestamp + aggregationIntervalInUs)
            {
                splitSpan = new Span(span.SpanId, span.ParentId, span.TransactionId,
                    span.ComponentName, span.ParentName,
                    bucketTimestamp + aggregationIntervalInUs,
                    span.Timestamp + span.Duration - bucketTimestamp, HttpStatusCodes.Success);
                span.Duration = bucketTimestamp + aggregationIntervalInUs - span.Timestamp;
            }

            return splitSpan;
        }

        /// <summary>
        /// Adds new buckets to the list to cover longer timespan, if startTimestamp or endTimestamp are out of
        /// current boundaries.
        /// </summary>
        /// <param name="startTimestamp"></param>
        /// <param name="endTimestamp"></param>
        private void ExtendTimeRange(long startTimestamp, long endTimestamp)
        {
            // Fill first time range if none is in the component load list yet 
            if (Buckets.Count == 0)
            {
                minTimestamp = startTimestamp;
                maxTimestamp = endTimestamp;
                while (startTimestamp < endTimestamp)
                {
                    Buckets.Add(startTimestamp, new List<ComponentLoad>());
                    startTimestamp += aggregationIntervalInUs;
                }

                Buckets.Add(startTimestamp, new List<ComponentLoad>());
            }

            while (startTimestamp < minTimestamp)
            {
                minTimestamp -= aggregationIntervalInUs;
                Buckets.Add(minTimestamp, new List<ComponentLoad>());
            }

            while (endTimestamp > maxTimestamp)
            {
                maxTimestamp += aggregationIntervalInUs;
                Buckets.Add(maxTimestamp, new List<ComponentLoad>());
            }
        }
    }
}