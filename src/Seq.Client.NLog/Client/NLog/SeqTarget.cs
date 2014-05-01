// Seq Client for .NET - Copyright 2014 Continuous IT Pty Ltd
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

namespace Seq.Client.NLog
{
    /// <summary>
    /// Writes events over HTTP to a Seq server.
    /// </summary>
    [Target("Seq")]
    public sealed class SeqTarget : Target
    {
        const string BulkUploadResource = "api/events/raw";
        const string ApiKeyHeaderName = "X-Seq-ApiKey";

        /// <summary>
        /// Initializes the target.
        /// </summary>
        public SeqTarget()
        {
            Properties = new List<SeqPropertyItem>();
        }

        /// <summary>
        /// The address of the Seq server to write to.
        /// </summary>
        [Required]
        public string ServerUrl { get; set; }

        /// <summary>
        /// A Seq <i>API key</i> that authenticates the client to the Seq server.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// A list of properties that will be attached to the events.
        /// </summary>
        [ArrayParameter(typeof(SeqPropertyItem), "property")]
        public IList<SeqPropertyItem> Properties { get; private set; } 

        /// <summary>
        /// Writes an array of logging events to Seq.
        /// </summary>
        /// <param name="logEvents">Logging events to be written.</param>
        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            var events = logEvents.Select(e => e.LogEvent);
            PostBatch(events);
        }

        /// <summary>
        /// Writes logging event to Seq.
        /// </summary>
        /// <param name="logEvent">Logging event to be written.
        ///             </param>
        protected override void Write(LogEventInfo logEvent)
        {
            PostBatch(new[] { logEvent });
        }

        void PostBatch(IEnumerable<LogEventInfo> events)
        {
            if (ServerUrl == null)
                return;

            var payload = new StringWriter();
            payload.Write("{\"events\":[");
            LogEventInfoFormatter.ToJson(events, payload, Properties);
            payload.Write("]}");

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            if (!string.IsNullOrWhiteSpace(ApiKey))
                content.Headers.Add(ApiKeyHeaderName, ApiKey);

            var baseUri = ServerUrl;
            if (!baseUri.EndsWith("/"))
                baseUri += "/";

            using (var httpClient = new HttpClient { BaseAddress = new Uri(baseUri) })
            {
                var result = httpClient.PostAsync(BulkUploadResource, content).Result;
                if (!result.IsSuccessStatusCode)
                    throw new HttpRequestException(string.Format("Received failed result {0}: {1}", result.StatusCode,
                        result.Content.ReadAsStringAsync().Result));
            }
        }
    }
}
