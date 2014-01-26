using System.Collections;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json;
using Seq.Client.FullNetFx.Tests.Support;
using Serilog.Events;
using Serilog.Parsing;
using System.Linq;

namespace Seq.Client.Serilog.Tests
{
    [TestFixture]
    public class JsonFormatterTests
    {
        [Test]
        public void PropertyTokensWithFormatStringsAreIncludedAsRenderings()
        {
            var p = new MessageTemplateParser();
            var e = new LogEvent(Some.OffsetInstant(), LogEventLevel.Information, null,
                p.Parse("{AProperty:000}"), new[] { new LogEventProperty("AProperty", new ScalarValue(12)) });

            var d = FormatEvent(e);

            var rs = ((IEnumerable)d.Renderings).Cast<dynamic>().ToArray();
            Assert.AreEqual(1, rs.Count());
            var ap = d.Renderings.AProperty;
            var fs = ((IEnumerable)ap).Cast<dynamic>().ToArray();
            Assert.AreEqual(1, fs.Count());
            Assert.AreEqual("000", (string)fs.Single().Format);
            Assert.AreEqual("012", (string)fs.Single().Rendering);
        }

        static dynamic FormatEvent(LogEvent e)
        {
            var j = new JsonFormatter();

            var f = new StringWriter();
            j.Format(e, f);

            var d = JsonConvert.DeserializeObject<dynamic>(f.ToString());
            return d;
        }

        [Test]
        public void PropertyTokensWithoutFormatStringsAreNotIncludedAsRenderings()
        {
            var p = new MessageTemplateParser();
            var e = new LogEvent(Some.OffsetInstant(), LogEventLevel.Information, null,
                p.Parse("{AProperty}"), new[] { new LogEventProperty("AProperty", new ScalarValue(12)) });

            var d = FormatEvent(e);

            var rs = ((IEnumerable)d.Renderings).Cast<dynamic>().ToArray();
            Assert.AreEqual(0, rs.Count());
        }
    }
}
