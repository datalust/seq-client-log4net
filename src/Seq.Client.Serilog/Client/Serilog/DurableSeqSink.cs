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
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.RollingFile;

namespace Seq.Client.Serilog
{
    class DurableSeqSink : ILogEventSink, IDisposable
    {
        readonly HttpLogShipper _shipper;
        readonly RollingFileSink _sink;

        public DurableSeqSink(string serverUrl, string bufferBaseFilename)
        {
            if (serverUrl == null) throw new ArgumentNullException("serverUrl");
            if (bufferBaseFilename == null) throw new ArgumentNullException("bufferBaseFilename");

            _shipper = new HttpLogShipper(serverUrl, bufferBaseFilename);
            _sink = new RollingFileSink(
                bufferBaseFilename + "-{Date}.json",
                new SeqJsonFormatter(trailingNewline: true),
                null,
                null);
        }

        public void Dispose()
        {
            _shipper.Dispose();
            _sink.Dispose();
        }

        public void Emit(LogEvent logEvent)
        {
            _sink.Emit(logEvent);
            _shipper.EventWritten();
        }
    }
}
