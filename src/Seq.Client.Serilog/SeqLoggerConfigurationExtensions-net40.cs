// Seq Client for .NET - Copyright 2013 Continuous IT Pty Ltd
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
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Seq(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string serverUrl,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = SeqSink.DefaultBatchPostingLimit,
            TimeSpan? period = null)
        {
            if (loggerSinkConfiguration == null) throw new ArgumentNullException("loggerSinkConfiguration");
            if (serverUrl == null) throw new ArgumentNullException("serverUrl");

            var defaultedPeriod = period ?? SeqSink.DefaultPeriod;
            return loggerSinkConfiguration.Sink(new SeqSink(serverUrl, batchPostingLimit, defaultedPeriod), restrictedToMinimumLevel);
        }
    }
}
