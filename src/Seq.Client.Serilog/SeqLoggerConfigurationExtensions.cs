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
using Seq.Client.Serilog;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Seq
{
    /// <summary>
    /// Extends Serilog configuration to write events to Seq.
    /// </summary>
    public static class SeqLoggerConfigurationExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events to a http://getseq.net Seq event server.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger configuration.</param>
        /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        /// in order to write an event to the sink.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="bufferBaseFilename">Path for a set of files that will be used to buffer events until they
        /// can be successfully transmitted across the network. Individual files will be created using the
        /// pattern <paramref name="bufferBaseFilename"/>-{Date}.json.</param>
        /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Seq(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string serverUrl,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = SeqSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            string apiKey = null,
            string bufferBaseFilename = null)
        {
            if (loggerSinkConfiguration == null) throw new ArgumentNullException("loggerSinkConfiguration");
            if (serverUrl == null) throw new ArgumentNullException("serverUrl");

            var defaultedPeriod = period ?? SeqSink.DefaultPeriod;

            ILogEventSink sink;
            if (bufferBaseFilename == null)
                sink = new SeqSink(serverUrl, apiKey, batchPostingLimit, defaultedPeriod);
            else
                sink = new DurableSeqSink(serverUrl, bufferBaseFilename, apiKey, batchPostingLimit, defaultedPeriod);

            return loggerSinkConfiguration.Sink(sink, restrictedToMinimumLevel);
        }
    }
}
