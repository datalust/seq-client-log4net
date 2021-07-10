// Seq Client for log4net - Copyright 2014-2019 Datalust and Contributors
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using log4net.Appender;
using log4net.Core;
using System.Threading;
// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedMember.Global

namespace Seq.Client.Log4Net
{
    /// <summary>
    /// A log4net <see cref="IAppender"/> that writes events synchronously over
    /// HTTP to the Seq event server.
    /// </summary>
    public class SeqAppender : BufferingAppenderSkeleton
    {
        readonly HttpClient _httpClient = new HttpClient();
        readonly List<AppenderParameter> _parameters = new List<AppenderParameter>();
        const string BulkUploadResource = "api/events/raw";
        const string ApiKeyHeaderName = "X-Seq-ApiKey";
        private string _appName = "";
        private string _appVersion = "";

        public SeqAppender()
        {
            bool isSuccess = true;

            try
            {
                if (string.IsNullOrEmpty(_appName))
                    _appName = Assembly.GetEntryAssembly()?.GetName().Name;

                if (string.IsNullOrEmpty(_appVersion))
                    _appVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
            }
            catch
            {
                isSuccess = false;
            }

            if (!isSuccess)
                try
                {
                    if (string.IsNullOrEmpty(_appName))
                        _appName = Assembly.GetExecutingAssembly().GetName().Name;

                    if (string.IsNullOrEmpty(_appVersion))
                        _appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
                catch
                {
                    //We surrender ...
                    _appName = string.Empty;
                    _appVersion = string.Empty;
                }

        }

        /// <summary>
        /// The address of the Seq server to write to. Specified in configuration
        /// like &lt;serverUrl value="http://my-seq:5341" /&gt;.
        /// </summary>
        public string ServerUrl
        {
            get
            {
                if (_httpClient.BaseAddress != null)
                    return _httpClient.BaseAddress.OriginalString;

                return null;
            }
            set
            {
                if (!value.EndsWith("/"))
                    value += "/";

                _httpClient.BaseAddress = new Uri(value);
            }
        }

        /// <summary>
        /// A Seq <i>API key</i> that authenticates the client to the Seq server. Specified in configuration
        /// like &lt;apiKey value="A1A2A3A4A5A6A7A8A9A0" /&gt;.
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// Gets or sets HttpClient timeout.
        /// Specified in configuration like &lt;timeout value="00:00:01" /&gt; which corresponds to 1 second.
        /// </summary>
        public string Timeout
        {
            get => _httpClient.Timeout.ToString();
            set => _httpClient.Timeout = TimeSpan.Parse(value);
        }

        public string MaskProperties
        {
            get => string.Join(",",Masking.MaskProperties);
            set => Masking.MaskProperties.AddRange((value ?? "")
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList());
        }

        public MaskPolicy MaskType
        {
            get => Masking.MaskType;
            set => Masking.MaskType = value;
        }

        /// <summary>
        /// Adds a parameter to the command.
        /// </summary>
        /// <param name="parameter">The parameter to add to the command.</param>
        /// <remarks>
        /// <para>
        /// Adds a parameter to the ordered list of command parameters.
        /// </para>
        /// </remarks>
        public void AddParameter(AppenderParameter parameter)
        {
            _parameters.Add(parameter);
        }

        /// <summary>
        /// Send events to Seq.
        /// </summary>
        /// <param name="events">The buffered events to send.</param>
        protected override void SendBuffer(LoggingEvent[] events)
        {
            if (ServerUrl == null)
                return;

            var payload = new StringWriter();
            LoggingEventFormatter.ToJson(events, payload, _parameters, _appName, _appVersion);

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/vnd.serilog.clef");
            if (!string.IsNullOrWhiteSpace(ApiKey))
                content.Headers.Add(ApiKeyHeaderName, ApiKey);

            using (var result = _httpClient.PostAsync(BulkUploadResource, content).Result)
            {
                if (!result.IsSuccessStatusCode)
                    ErrorHandler.Error($"Received failed result {result.StatusCode}: {result.Content.ReadAsStringAsync().Result}");
            }
        }
    }
}
