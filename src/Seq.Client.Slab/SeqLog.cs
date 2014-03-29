// Seq Client for SLAB - Copyright 2014 Continuous IT Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Seq.Client.Slab;

namespace Seq
{
    /// <summary>
    /// Extension methods for configuring the Seq (http://getseq.net) log event sink.
    /// </summary>
    public static class SeqLog
    {
        /// <summary>
        /// Subscribes to an <see cref="IObservable{EventEntry}" /> using a <see cref="SeqSink" />.
        /// </summary>
        /// <param name="eventStream">The event stream. Typically this is an instance of <see cref="ObservableEventListener" />.</param>
        /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
        /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
        /// <param name="bufferingInterval">The buffering interval between each batch publishing. Default value is <see cref="Buffering.DefaultBufferingInterval" />.</param>
        /// <param name="onCompletedTimeout">Time limit for flushing the entries after an <see cref="SeqSink.OnCompleted" /> call is received.</param>
        /// <param name="bufferingCount">Number of entries that will trigger batch publishing. Default is <see cref="Buffering.DefaultBufferingCount" /></param>
        /// <param name="maxBufferSize">The maximum number of entries that can be buffered before the sink starts dropping entries.
        /// This means that if the timeout period elapses, some event entries will be dropped and not sent to the store. Normally, calling <see cref="IDisposable.Dispose" /> on
        /// the <see cref="System.Diagnostics.Tracing.EventListener" /> will block until all the entries are flushed or the interval elapses.
        /// If <see langword="null" /> is specified, then the call will block indefinitely until the flush operation finishes.</param>
        /// <returns>
        /// A subscription to the sink that can be disposed to unsubscribe the sink and dispose it, or to get access to the sink instance.
        /// </returns>
        public static SinkSubscription<SeqSink> LogToSeq(
            this IObservable<EventEntry> eventStream,
            string serverUrl,
            string apiKey = null,
            TimeSpan? bufferingInterval = null,
            TimeSpan? onCompletedTimeout = null,
            int bufferingCount = Buffering.DefaultBufferingCount,
            int maxBufferSize = Buffering.DefaultMaxBufferSize)
        {
            var sink = new SeqSink(
                serverUrl,
                apiKey,
                bufferingInterval ?? Buffering.DefaultBufferingInterval,
                bufferingCount,
                maxBufferSize,
                onCompletedTimeout ?? Timeout.InfiniteTimeSpan);

            var subscription = eventStream.Subscribe(sink);
            return new SinkSubscription<SeqSink>(subscription, sink);
        }

        /// <summary>
        /// Creates an event listener that logs using a <see cref="SeqSink" />.
        /// </summary>
        /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
        /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
        /// <param name="bufferingInterval">The buffering interval between each batch publishing. Default value is <see cref="Buffering.DefaultBufferingInterval" />.</param>
        /// <param name="listenerDisposeTimeout">Time limit for flushing the entries after an <see cref="SeqSink.OnCompleted" /> call is received and before disposing the sink.</param>
        /// <param name="bufferingCount">Number of entries that will trigger batch publishing. Default is <see cref="Buffering.DefaultBufferingCount" /></param>
        /// <param name="maxBufferSize">The maximum number of entries that can be buffered before the sink starts dropping entries.
        /// This means that if the timeout period elapses, some event entries will be dropped and not sent to the store. Normally, calling <see cref="IDisposable.Dispose" /> on
        /// the <see cref="System.Diagnostics.Tracing.EventListener" /> will block until all the entries are flushed or the interval elapses.
        /// If <see langword="null" /> is specified, then the call will block indefinitely until the flush operation finishes.</param>
        /// <returns>
        /// An event listener that uses <see cref="SeqSink" /> to log events.
        /// </returns>
        public static EventListener CreateListener(
            string serverUrl,
            string apiKey = null,
            TimeSpan? bufferingInterval = null,
            TimeSpan? listenerDisposeTimeout = null,
            int bufferingCount = Buffering.DefaultBufferingCount, 
            int maxBufferSize = Buffering.DefaultMaxBufferSize)
        {
            var listener = new ObservableEventListener();
            listener.LogToSeq(serverUrl, apiKey, bufferingInterval, listenerDisposeTimeout, bufferingCount, maxBufferSize);
            return listener;
        }
    }
}
