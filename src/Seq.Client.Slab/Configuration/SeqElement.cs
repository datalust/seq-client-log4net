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
using System.Threading;
using System.Xml.Linq;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Seq.Client.Slab;

namespace Seq.Configuration
{
    internal class SeqElement : ISinkElement
    {
        private readonly XName sinkName = XName.Get("seqSink", Constants.Namespace);

        public bool CanCreateSink(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            return element.Name == sinkName;
        }

        public IObserver<EventEntry> CreateSink(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");

            return new SeqSink(
                (string)element.Attribute("serverUrl"),
                (string)element.Attribute("apiKey"),
                element.Attribute("bufferingIntervalInSeconds").ToTimeSpan() ?? Buffering.DefaultBufferingInterval,
                (int?)element.Attribute("bufferingCount") ?? Buffering.DefaultBufferingCount,
                (int?)element.Attribute("maxBufferSize") ?? Buffering.DefaultMaxBufferSize,
                element.Attribute("bufferingFlushAllTimeoutInSeconds").ToTimeSpan() ?? Timeout.InfiniteTimeSpan);
        }
    }
}