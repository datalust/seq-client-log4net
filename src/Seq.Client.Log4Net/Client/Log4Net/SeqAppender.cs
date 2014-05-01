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
using System.IO;
using System.Net.Http;
using System.Text;
using log4net.Appender;
using log4net.Core;

namespace Seq.Client.Log4Net
{
    /// <summary>
    /// A log4net <see cref="IAppender"/> that writes events synchronously over
    /// HTTP to the Seq event server.
    /// </summary>
    public class SeqAppender : BufferingAppenderSkeleton
    {
        const string BulkUploadResource = "api/events/raw";
        const string ApiKeyHeaderName = "X-Seq-ApiKey";

        /// <summary>
        /// The address of the Seq server to write to. Specified in configuration
        /// like &lt;serverUrl value="http://my-seq:5341" /&gt;.
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// A Seq <i>API key</i> that authenticates the client to the Seq server. Specified in configuration
        /// like &lt;apiKey value="A1A2A3A4A5A6A7A8A9A0" /&gt;.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Send events to Seq.
        /// </summary>
        /// <param name="events">The buffered events to send.</param>
        protected override void SendBuffer(LoggingEvent[] events)
        {
            if (ServerUrl == null)
                return;

            var payload = new StringWriter();
            payload.Write("{\"events\":[");
            LoggingEventFormatter.ToJson(events, payload);
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
                    ErrorHandler.Error(string.Format("Received failed result {0}: {1}", result.StatusCode, result.Content.ReadAsStringAsync().Result));
            }
        }
    }
}
