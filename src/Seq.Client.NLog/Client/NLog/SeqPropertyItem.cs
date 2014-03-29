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

using NLog.Config;
using NLog.Layouts;

namespace Seq.Client.NLog
{
    /// <summary>
    /// Configures a property that enriches events sent to Seq.
    /// </summary>
    [NLogConfigurationItem]
    public sealed class SeqPropertyItem
    {
        /// <summary>
        /// Initialize parameter defaults.
        /// </summary>
        public SeqPropertyItem()        
        {
            As = "string";
        }

        /// <summary>
        /// The name of the property.
        /// </summary>
        [RequiredParameter]
        public string Name { get; set; }

        /// <summary>
        /// The value of the property.
        /// </summary>
        [RequiredParameter]
        public Layout Value { get; set; }

        /// <summary>
        /// Either "string", which is the default, or "number", which
        /// will cause values of this type to be converted to numbers for
        /// storage.
        /// </summary>
        [RequiredParameter]
        public string As { get; set; }
    }
}