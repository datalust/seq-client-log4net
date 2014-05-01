using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Newtonsoft.Json;

namespace Seq.Client.Slab
{
    /// <summary>
    /// Writes events over HTTP/S to a Seq server.
    /// </summary>
    public class SeqSink : IObserver<EventEntry>, IDisposable
    {
        readonly string _serverUrl;
        readonly string _apiKey;
        readonly TimeSpan _onCompletedTimeout;
        readonly BufferedEventPublisher<EventEntry> _bufferedPublisher;
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        const string BulkUploadResource = "api/events/raw";
        const string ApiKeyHeaderName = "X-Seq-ApiKey";

        /// <summary>
        /// Initializes a new instance of the Seq sink.
        /// </summary>
        /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
        /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
        /// <param name="bufferingInterval">The buffering interval between each batch publishing. Default value is <see cref="Buffering.DefaultBufferingInterval" />.</param>
        /// <param name="onCompletedTimeout">Time limit for flushing the entries after an <see cref="SeqSink.OnCompleted" /> call is received.</param>
        /// <param name="bufferingCount">Number of entries that will trigger batch publishing. Default is <see cref="Buffering.DefaultBufferingCount" /></param>
        /// <param name="maxBufferSize">The maximum number of entries that can be buffered before the sink starts dropping entries.
        /// This means that if the timeout period elapses, some event entries will be dropped and not sent to the store. Normally, calling <see cref="IDisposable.Dispose" /> on
        /// the <see cref="System.Diagnostics.Tracing.EventListener" /> will block until all the entries are flushed or the interval elapses.
        /// If <see langword="null" /> is specified, then the call will block indefinitely until the flush operation finishes.</param>

        public SeqSink(
            string serverUrl,
            string apiKey, 
            TimeSpan bufferingInterval, 
            int bufferingCount,
            int maxBufferSize, 
            TimeSpan onCompletedTimeout)
        {
            if (serverUrl == null) throw new ArgumentNullException("serverUrl");


            var baseUri = serverUrl;
            if (!baseUri.EndsWith("/"))
                baseUri += "/";

            _serverUrl = baseUri;
            _apiKey = apiKey;
            _onCompletedTimeout = onCompletedTimeout;
            _bufferedPublisher = BufferedEventPublisher<EventEntry>.CreateAndStart("Seq", PublishEventsAsync, bufferingInterval,
                bufferingCount, maxBufferSize, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Releases resources used by the sink.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Notifies the sink that the source has finished sending events.
        /// </summary>
        public void OnCompleted()
        {
            FlushSafe();
            Dispose();
        }

        /// <summary>
        /// Provides the sink with a new entry to write.
        /// </summary>
        /// <param name="value">The current entry to write.</param>
        public void OnNext(EventEntry value)
        {
            if (value == null)
                return;

            _bufferedPublisher.TryPost(value);
        }

        /// <summary>
        /// Notifies the sink that the source has experienced an error condition.
        /// </summary>
        /// <param name="error">An object with information about the error.</param>
        public void OnError(Exception error)
        {
            FlushSafe();
            Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether or not the class is disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _bufferedPublisher.Dispose();
            }
        }

        /// <summary>
        /// Causes the buffer to be written immediately.
        /// </summary>
        /// <returns>The Task that flushes the buffer.</returns>
        public Task FlushAsync()
        {
            return _bufferedPublisher.FlushAsync();
        }

        internal async Task<int> PublishEventsAsync(IEnumerable<EventEntry> collection)
        {
            try
            {
                var batch = CreateBatch(collection);

                var content = new StringContent(JsonConvert.SerializeObject(batch), Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    content.Headers.Add(ApiKeyHeaderName, _apiKey);

                using (var httpClient = new HttpClient { BaseAddress = new Uri(_serverUrl) })
                {
                    var result = await httpClient.PostAsync(BulkUploadResource, content);
                    if (!result.IsSuccessStatusCode)
                    {
                        if (result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            var error = string.Format("Received failed result from Seq {0}: {1}", result.StatusCode, result.Content.ReadAsStringAsync().Result);
                            SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(error);
                            return batch.Events.Count;
                        }

                        return 0;
                    }

                    return batch.Events.Count;
                }
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                SemanticLoggingEventSource.Log.CustomSinkUnhandledFault("Failed to write events to Seq: " + ex);
                throw;
            }
        }

        static SeqBatch CreateBatch(IEnumerable<EventEntry> eventEntries)
        {
            return new SeqBatch
            {
                Events = eventEntries.Select(CreatePayload).ToList()
            };
        }

        static readonly Dictionary<EventLevel, string> LevelMap = new Dictionary<EventLevel, string>
        {
            { EventLevel.Critical, "Fatal" },
            { EventLevel.Error, "Error" },
            { EventLevel.Warning, "Warning" },
            { EventLevel.Informational, "Information" },
            { EventLevel.Verbose, "Verbose" }
        };
        
        static SeqEventPayload CreatePayload(EventEntry eventEntry)
        {
            var escapedMessage = (eventEntry.FormattedMessage ?? "<none>").Replace("{", "{{").Replace("}", "}}");
            string level;
            if (!LevelMap.TryGetValue(eventEntry.Schema.Level, out level))
                level = "Information";

            var properties = new Dictionary<string,object>
            {
                { "EtwEventId", eventEntry.EventId },
                { "Keywords", (long)eventEntry.Schema.Keywords },
                { "ProviderId", eventEntry.ProviderId },
                { "ProviderName", eventEntry.Schema.ProviderName },
                { "Opcode", (int)eventEntry.Schema.Opcode },
                { "Task", (int)eventEntry.Schema.Task },
                { "Version", eventEntry.Schema.Version },
                { "ActivityId", eventEntry.ActivityId == Guid.Empty ? (Guid?) null : eventEntry.ActivityId },
                { "RelatedActivityId", eventEntry.RelatedActivityId  == Guid.Empty ? (Guid?) null : eventEntry.RelatedActivityId }
            };

            for (var i = 0; i < eventEntry.Payload.Count; i++)
            {
                properties[eventEntry.Schema.Payload[i]] = eventEntry.Payload[i];
            }

            return new SeqEventPayload
            {
                Level = level,
                Timestamp = eventEntry.Timestamp.ToString("o"),
                MessageTemplate = escapedMessage,
                Properties = properties
            };
        }

        private void FlushSafe()
        {
            try
            {
                FlushAsync().Wait(_onCompletedTimeout);
            }
            catch (AggregateException ex)
            {
                // Flush operation will already log errors. Never expose this exception to the observable.
                ex.Handle(e => e is FlushFailedException);
            }
        }
    }
}
