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
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using log4net.Core;
using Newtonsoft.Json;

namespace Seq.Client.Log4Net
{
    static class LoggingEventFormatter
    {
        static readonly IDictionary<Type, Action<object, TextWriter>> LiteralWriters;
        const uint Log4NetEventType = 0x00010649;

        static LoggingEventFormatter()
        {
            LiteralWriters = new Dictionary<Type, Action<object, TextWriter>>
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

        public static void ToJson(LoggingEvent[] events, StringWriter payload, List<AppenderParameter> mParameters, string appName = null, string appVersion = null)
        {
            var delim = "";
            foreach (var loggingEvent in events)
            {
                payload.Write(delim);
                delim = "\n";
                ToJson(loggingEvent, payload, mParameters, appName, appVersion);
            }
        }

        static void ToJson(LoggingEvent loggingEvent, StringWriter payload, IEnumerable<AppenderParameter> parameters, string appName = null, string appVersion = null)
        {
            payload.Write("{");

            var delim = "";

            WriteJsonProperty("@t", loggingEvent.TimeStamp, ref delim, payload);
            WriteJsonProperty("@l", loggingEvent.Level.Name, ref delim, payload);
            WriteJsonProperty("@i", Log4NetEventType, ref delim, payload);
            var message = loggingEvent.RenderedMessage;
            if (loggingEvent.ExceptionObject != null)
                WriteJsonProperty("@x", loggingEvent.ExceptionObject, ref delim, payload);

            if (Config.Destructure)
            {
                message = DestructureRegex(loggingEvent.ThreadName, payload, message, ref delim);
                message = DestructureXml(loggingEvent.ThreadName, payload, message, ref delim);
                message = DestructureJson(loggingEvent.ThreadName, payload, message, ref delim);
            }

            WriteJsonProperty("@m", message, ref delim, payload);

            var seenKeys = new HashSet<string>();

            foreach (var property in parameters)
            {
                var stringValue = property.Layout.Format(loggingEvent);
                WriteJsonProperty(property.ParameterName, stringValue, ref delim, payload);
            }

            if (!string.IsNullOrEmpty(appName))
                WriteJsonProperty("AppName", appName, ref delim, payload);
            if (!string.IsNullOrEmpty(appVersion))
                WriteJsonProperty("AppVersion", appVersion, ref delim, payload);

            var correlationId = "";

            if (CorrelationCache.Contains(loggingEvent.ThreadName))
                correlationId = CorrelationCache.Get(loggingEvent.ThreadName);
            else
            {
                correlationId = Guid.NewGuid().ToString();
                CorrelationCache.Replace(loggingEvent.ThreadName, correlationId);
            }

            WriteJsonProperty("CorrelationId", correlationId, ref delim, payload);
            WriteJsonProperty("MachineName", Environment.MachineName, ref delim, payload);
            WriteJsonProperty("MethodName", loggingEvent.LocationInformation.MethodName, ref delim, payload);
            WriteJsonProperty("SourceFile", loggingEvent.LocationInformation.FileName, ref delim, payload);
            WriteJsonProperty("LineNumber", loggingEvent.LocationInformation.LineNumber, ref delim, payload);
            WriteJsonProperty("ThreadId", loggingEvent.ThreadName, ref delim, payload);
            WriteJsonProperty("EnvironmentUserName", loggingEvent.UserName, ref delim, payload);
            WriteJsonProperty(SanitizeKey("log4net:Logger"), loggingEvent.LoggerName, ref delim, payload);

            foreach (DictionaryEntry property in loggingEvent.GetProperties())
            {
                var sanitizedKey = SanitizeKey(property.Key.ToString());
                if (seenKeys.Contains(sanitizedKey))
                    continue;

                seenKeys.Add(sanitizedKey);
                WriteJsonProperty(sanitizedKey, property.Value, ref delim, payload);
            }
            payload.Write("}");
        }

        private static string DestructureRegex(string threadId, StringWriter payload, string message, ref string delim)
        {
            if (string.IsNullOrEmpty(Config.PropertyRegex)) return message;
            try
            {
                if (Regex.IsMatch(message, Config.PropertyRegex))
                {
                    foreach (Match match in Regex.Matches(message, Config.PropertyRegex, RegexOptions.IgnoreCase))
                    {
                        var mask = Masking.Mask(match.Groups[1].Value, match.Groups[2].Value);
                        
                        if (match.Groups[2].Value != mask.ToString())
                            message = message.Replace(match.Groups[2].Value, mask.ToString());
                        WriteJsonProperty(match.Groups[1].Value, mask, ref delim, payload);

                        if (!string.IsNullOrEmpty(Config.CorrelationProperty) &&
                            match.Groups[1].Value.Equals(Config.CorrelationProperty,
                                StringComparison.OrdinalIgnoreCase))
                            CorrelationCache.Replace(threadId, mask.ToString());
                    }
                }
            }
            catch (Exception)
            {
                return message;
            }

            return message;
        }

        private static string DestructureJson(string threadId, StringWriter payload, string message, ref string delim)
        {
            if (!message.Contains("{") || !message.Contains("}")) return message;
            Dictionary<string, string> jsonValues = new Dictionary<string, string>();
            string possibleJson = message.Substring(message.IndexOf("{", StringComparison.Ordinal),
                message.LastIndexOf("}", StringComparison.Ordinal) -
                message.IndexOf("{", StringComparison.Ordinal) + 1);

            var json = new ExpandoObject();
            try
            {
                json = JsonConvert.DeserializeObject<ExpandoObject>(possibleJson, new JsonSerializerSettings());
            }
            catch (Exception)
            {
                json = new ExpandoObject();
            }

            if (!json.Any()) return message;
            var mask = EvaluateJson(threadId, string.Empty, new MaskJson() {JsonObject = json});

            foreach (var p in mask.JsonValues)
            {
                WriteJsonProperty(p.Key, p.Value, ref delim, payload);
            }

            if (!mask.IsMask) return message;
            var outputJson = JsonConvert.SerializeObject(mask.JsonObject);
            var s = new StringBuilder();
            s.Append(message.Substring(0, message.IndexOf("{", StringComparison.Ordinal)));
            s.Append(outputJson);
            if (message.Length - 1 > message.LastIndexOf("}", StringComparison.Ordinal))
                s.Append(message.Substring(message.LastIndexOf("}", StringComparison.Ordinal) + 1,
                    message.Length - message.LastIndexOf("}", StringComparison.Ordinal) - 1));
            message = s.ToString();

            return message;
        }


        private static MaskJson EvaluateJson(string threadId, string key = "", MaskJson json = null)
        {
            var updateJson = new ExpandoObject() as IDictionary<string, object>;
            var maskJson = new MaskJson();
            
            foreach (var x in json.JsonObject)
            {
                if (x.Value.GetType() != typeof(ExpandoObject))
                {
                    var mask = Masking.Mask(x.Key, x.Value);

                    if (string.IsNullOrEmpty(key))
                    {
                        if (!maskJson.JsonValues.ContainsKey(key))
                            maskJson.JsonValues.Add(x.Key, mask.ToString());
                    }
                    else
                    {
                        if (!maskJson.JsonValues.ContainsKey(key))
                            maskJson.JsonValues.Add(key + "_" + x.Key, mask.ToString());
                    }

                    if (!string.IsNullOrEmpty(Config.CorrelationProperty) &&
                        x.Key.Equals(Config.CorrelationProperty, StringComparison.OrdinalIgnoreCase))
                        CorrelationCache.Replace(threadId, mask.ToString());

                    if (mask != x.Value)
                    {
                        maskJson.MaskedProperties.Add(x.Key);
                        maskJson.IsMask = true;
                    }

                    if (!updateJson.ContainsKey(x.Key))
                        updateJson.Add(new KeyValuePair<string, object>(x.Key, mask));
                }
                else
                {
                    var subJson = new MaskJson() { JsonObject = (ExpandoObject) x.Value };
                    subJson = string.IsNullOrEmpty(key) ? EvaluateJson(threadId, x.Key, subJson) : EvaluateJson(threadId, key + "_" + x.Key, subJson);

                    foreach (var y in subJson.JsonValues)
                    {
                        if (!maskJson.JsonValues.ContainsKey(y.Key))
                            maskJson.JsonValues.Add(y.Key, y.Value);
                    }

                    if (!updateJson.ContainsKey(x.Key))
                        updateJson.Add(x.Key, subJson.JsonObject);
                }

                maskJson.JsonObject = (ExpandoObject)updateJson;
            }

            return maskJson;
        }

        private static string DestructureXml(string threadId, StringWriter payload, string message, ref string delim)
        {
            //Attempt to parse XML properties if they exist
            var hasXml = false;
            if (!message.Contains("<") || !message.Contains(">")) return message;
            var xmlValues = new Dictionary<string, string>();
            var possibleXml = message.Substring(message.IndexOf("<", StringComparison.Ordinal),
                message.LastIndexOf(">", StringComparison.Ordinal) -
                message.IndexOf("<", StringComparison.Ordinal) + 1);
            var isMask = false;
            var xml = new XDocument();
            try
            {
                xml = XDocument.Parse(possibleXml);
            }
            catch (Exception)
            {
                xml = new XDocument();
            }

            if (xml.Elements().Any())
            {
                hasXml = true;
                foreach (var element in xml.Elements())
                {
                    foreach (var node in element.Descendants())
                    {
                        if (node.IsEmpty) continue;
                        var value = Masking.Mask(node.Name.LocalName, node.Value);
                        if (value.ToString() != node.Value)
                        {
                            isMask = true;
                            node.SetValue(value);
                        }

                        if (!xmlValues.ContainsKey(element.Name + "_" + node.Name.LocalName))
                            xmlValues.Add(element.Name + "_" + node.Name.LocalName, value.ToString());
                            
                        if (!string.IsNullOrEmpty(Config.CorrelationProperty) &&
                            node.Name.LocalName.Equals(Config.CorrelationProperty,
                                StringComparison.OrdinalIgnoreCase))
                            CorrelationCache.Replace(threadId, value.ToString());

                        WriteJsonProperty(element.Name + "_" + node.Name.LocalName, value, ref delim, payload);
                    }
                }
            }

            //Replace the XML component with masked properties if appropriate
            if (!hasXml || !isMask) return message;
            var s = new StringBuilder();
            s.Append(message.Substring(0, message.IndexOf("<", StringComparison.Ordinal)));
            s.Append(xml);
            if (message.Length - 1 > message.LastIndexOf(">", StringComparison.Ordinal))
                s.Append(message.Substring(message.LastIndexOf(">", StringComparison.Ordinal) + 1,
                    message.Length - message.LastIndexOf(">", StringComparison.Ordinal) - 1));
            message = s.ToString();

            return message;
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

            // Attempt to convert the object (if a string) to it's literal type (int/decimal/date)
            value = GetValueAsLiteral(value);

            if (LiteralWriters.TryGetValue(value.GetType(), out var writer))
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

        /// <summary>
        /// GetValueAsLiteral attempts to transform the (string) object into a literal type prior to json serialization.
        /// </summary>
        /// <param name="value">The value to be transformed/parsed.</param>
        /// <returns>A translated representation of the literal object type instead of a string.</returns>
        static object GetValueAsLiteral(object value)
        {
            if (value is string str)
            {
                // All number literals are serialized as a decimal so ignore other number types.
                if (decimal.TryParse(str, out var decimalBuffer))
                    return decimalBuffer;

                // Standardize on dates if/when possible.
                if (DateTime.TryParse(str, out var dateBuffer))
                    return dateBuffer;
            }

            return value;
        }
    }
}
