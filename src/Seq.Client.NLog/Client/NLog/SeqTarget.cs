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
using System.Net;
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
        /// </param>
        protected override void Write(LogEventInfo logEvent)
        {
            PostBatch(new[] { logEvent });
        }

        void PostBatch(IEnumerable<LogEventInfo> events)
        {
            if (ServerUrl == null)
                return;

            var uri = ServerUrl;
            if (!uri.EndsWith("/"))
                uri += "/";
            uri += BulkUploadResource;

            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            if (!string.IsNullOrWhiteSpace(ApiKey))
                request.Headers.Add(ApiKeyHeaderName, ApiKey);

            using (var requestStream = request.GetRequestStream())
            using (var payload = new StreamWriter(requestStream))
            {
                payload.Write("{\"events\":[");
                LogEventInfoFormatter.ToJson(events, payload, Properties);
                payload.Write("]}");
            }

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                if (responseStream == null)
                    throw new WebException("No response was received from the Seq server");

                using (var reader = new StreamReader(responseStream))
                {
                    var data = reader.ReadToEnd();
                    if ((int) response.StatusCode > 299)
                        throw new WebException(string.Format("Received failed response {0} from Seq server: {1}",
                            response.StatusCode,
                            data));
                }
            }
        }
    }
}
