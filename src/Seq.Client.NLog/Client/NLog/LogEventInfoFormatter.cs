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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NLog;

namespace Seq.Client.NLog
{
    static class LogEventInfoFormatter
    {
        static readonly IDictionary<Type, Action<object, TextWriter>> _literalWriters;

        static LogEventInfoFormatter()
        {
            _literalWriters = new Dictionary<Type, Action<object, TextWriter>>
            {
                { typeof(bool), (v, w) => WriteBoolean((bool)v, w) },
                { typeof(char), (v, w) => WriteString(((char)v).ToString(CultureInfo.InvariantCulture), w) },
                { typeof(byte), WriteToString },
                { typeof(sbyte), WriteToString },
                { typeof(short), WriteToString },
                { typeof(ushort), WriteToString },
                { typeof(int), WriteToString },
                { typeof(uint), WriteToString },
                { typeof(long), WriteToString },
                { typeof(ulong), WriteToString },
                { typeof(float), WriteToString },
                { typeof(double), WriteToString },
                { typeof(decimal), WriteToString },
                { typeof(string), (v, w) => WriteString((string)v, w) },
                { typeof(DateTime), (v, w) => WriteDateTime((DateTime)v, w) },
                { typeof(DateTimeOffset), (v, w) => WriteOffset((DateTimeOffset)v, w) },
            };
        }

        static readonly IDictionary<string, string> _levelMap = new Dictionary<string, string>
        {
            { "Trace", "Verbose" },
            { "Debug", "Debug" },
            { "Info", "Information" },
            { "Warn", "Warning" },
            { "Error", "Error" },
            { "Fatal", "Fatal" }
        };

        public static void ToJson(IEnumerable<LogEventInfo> events, StringWriter payload, IList<SeqPropertyItem> properties)
        {
            var delim = "";
            foreach (var loggingEvent in events)
            {
                payload.Write(delim);
                delim = ",";
                ToJson(loggingEvent, payload, properties);
            }
        }

        static void ToJson(LogEventInfo loggingEvent, StringWriter payload, IList<SeqPropertyItem> properties)
        {
            string level;
            if (!_levelMap.TryGetValue(loggingEvent.Level.Name, out level))
                level = "Information";

            payload.Write("{");

            var delim = "";
            var offsetTimestamp = new DateTimeOffset(loggingEvent.TimeStamp, DateTimeOffset.Now.Offset);
            WriteJsonProperty("Timestamp", offsetTimestamp, ref delim, payload);
            WriteJsonProperty("Level", level, ref delim, payload);

            WriteJsonProperty("MessageTemplate", loggingEvent.Message, ref delim, payload);

            if (loggingEvent.Exception != null)
                WriteJsonProperty("Exception", loggingEvent.Exception, ref delim, payload);

            payload.Write(",\"Properties\":{");

            var pdelim = "";

            foreach (var property in properties)
            {
                var stringValue = property.Value.Render(loggingEvent);
                if (property.As == "number")
                {
                    decimal numberValue;
                    if (decimal.TryParse(stringValue, out numberValue))
                    {
                        WriteJsonProperty(property.Name, numberValue, ref pdelim, payload);
                        continue;
                    }
                }
                
                WriteJsonProperty(property.Name, stringValue, ref pdelim, payload);
            }

            if (loggingEvent.Parameters != null)
            {
                for (var i = 0; i < loggingEvent.Parameters.Length; ++i)
                {
                    WriteJsonProperty(i.ToString(CultureInfo.InvariantCulture), loggingEvent.Parameters[i], ref pdelim, payload);
                }
            }

            if (loggingEvent.Properties != null)
            {
                var seenKeys = new HashSet<string>();
                foreach (var property in loggingEvent.Properties)
                {
                    var sanitizedKey = SanitizeKey(property.Key.ToString());
                    if (seenKeys.Contains(sanitizedKey))
                        continue;

                    seenKeys.Add(sanitizedKey);
                    WriteJsonProperty(sanitizedKey, property.Value, ref pdelim, payload);
                }
            }

            payload.Write("}");
            payload.Write("}");
        }

        static string SanitizeKey(string key)
        {
            return new string(key.Replace(":", "_").Where(c => c == '_' || char.IsLetterOrDigit(c)).ToArray());
        }


        static void WriteJsonProperty(string name, object value, ref string precedingDelimiter, TextWriter output)
        {
            output.Write(precedingDelimiter);
            WritePropertyName(name, output);
            WriteLiteral(value, output);
            precedingDelimiter = ",";
        }

        static void WritePropertyName(string name, TextWriter output)
        {
            output.Write("\"");
            output.Write(name);
            output.Write("\":");
        }

        static void WriteLiteral(object value, TextWriter output)
        {
            if (value == null)
            {
                output.Write("null");
                return;
            }

            Action<object, TextWriter> writer;
            if (_literalWriters.TryGetValue(value.GetType(), out writer))
            {
                writer(value, output);
                return;
            }

            WriteString(value.ToString(), output);
        }

        static void WriteToString(object number, TextWriter output)
        {
            output.Write(number.ToString());
        }

        static void WriteBoolean(bool value, TextWriter output)
        {
            output.Write(value ? "true" : "false");
        }

        static void WriteOffset(DateTimeOffset value, TextWriter output)
        {
            output.Write("\"");
            output.Write(value.ToString("o"));
            output.Write("\"");
        }

        static void WriteDateTime(DateTime value, TextWriter output)
        {
            output.Write("\"");
            output.Write(value.ToString("o"));
            output.Write("\"");
        }

        static void WriteString(string value, TextWriter output)
        {
            var content = Escape(value);
            output.Write("\"");
            output.Write(content);
            output.Write("\"");
        }

        static string Escape(string s)
        {
            if (s == null) return null;

            StringBuilder escapedResult = null;
            var cleanSegmentStart = 0;
            for (var i = 0; i < s.Length; ++i)
            {
                var c = s[i];
                if (c < (char)32 || c == '\\' || c == '"')
                {

                    if (escapedResult == null)
                        escapedResult = new StringBuilder();

                    escapedResult.Append(s.Substring(cleanSegmentStart, i - cleanSegmentStart));
                    cleanSegmentStart = i + 1;

                    switch (c)
                    {
                        case '"':
                            {
                                escapedResult.Append("\\\"");
                                break;
                            }
                        case '\\':
                            {
                                escapedResult.Append("\\\\");
                                break;
                            }
                        case '\n':
                            {
                                escapedResult.Append("\\n");
                                break;
                            }
                        case '\r':
                            {
                                escapedResult.Append("\\r");
                                break;
                            }
                        case '\f':
                            {
                                escapedResult.Append("\\f");
                                break;
                            }
                        case '\t':
                            {
                                escapedResult.Append("\\t");
                                break;
                            }
                        default:
                            {
                                escapedResult.Append("\\u");
                                escapedResult.Append(((int)c).ToString("X4"));
                                break;
                            }
                    }
                }
            }

            if (escapedResult != null)
            {
                if (cleanSegmentStart != s.Length)
                    escapedResult.Append(s.Substring(cleanSegmentStart));

                return escapedResult.ToString();
            }

            return s;
        }
    }
}
