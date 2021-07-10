using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Seq.Client.Log4Net;
using Xunit.Abstractions;

namespace Seq.Client.Log4Net.Tests
{
    public class SeqAppenderTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SeqAppenderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public SeqAppender CanConstructAppender()
        {
            return new SeqAppender();
        }

        [Fact]
        public void TestXml()
        {
            Masking.MaskType = MaskPolicy.MaskLettersAndNumbers;
            Masking.MaskProperties.Add("CustomerNumber");
            string message =
                "Request : UniqueIdentifier: 188a1812-1771-4a1c-a9fa-748b490d7f68 Operation Name: GetAccounts DataElements: <Accounts xmlns=\"\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"> <UniqueIdentifier>828ec9e8-bd99-457a-835f-00045c6056fd</UniqueIdentifier> <StatusCode>0</StatusCode> <CustomerNumber>a123456789</CustomerNumber> <AccountTypeFilter>ALL</AccountTypeFilter> <Items /> </Accounts> Testing test";
            string message2 =
                "<Accounts xmlns=\"\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"> <UniqueIdentifier>828ec9e8-bd99-457a-835f-00045c6056fd</UniqueIdentifier> <StatusCode>0</StatusCode> <CustomerNumber>a123456789</CustomerNumber> <AccountTypeFilter>ALL</AccountTypeFilter> <Items /> </Accounts>";

            _testOutputHelper.WriteLine(processXml(message));
            _testOutputHelper.WriteLine(processXml(message2));
        }

        private static string processXml(string message)
        {
            if (message.Contains("<") && message.Contains(">"))
            {
                Dictionary<string, string> xmlValues = new Dictionary<string, string>();
                string possibleXml = message.Substring(message.IndexOf("<", StringComparison.Ordinal),
                    message.LastIndexOf(">", StringComparison.Ordinal) -
                    message.IndexOf("<", StringComparison.Ordinal) + 1);
                bool isMask = false;
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
                    foreach (var element in xml.Elements())
                    {
                        foreach (var node in element.Descendants())
                        {
                            if (!node.IsEmpty)
                            {
                                string value = Masking.Mask(node.Name.LocalName, node.Value);
                                if (value != node.Value)
                                {
                                    isMask = true;
                                    node.SetValue(value);
                                }

                                xmlValues.Add(element.Name + "_" + node.Name.LocalName, value);
                            }
                        }
                    }
                }

                if (xmlValues.Count > 0 && isMask)
                {
                    StringBuilder s = new StringBuilder();
                    s.Append(message.Substring(0, message.IndexOf("<", StringComparison.Ordinal)));
                    s.Append(xml);
                    if (message.Length - 1 > message.LastIndexOf(">", StringComparison.Ordinal))
                        s.Append(message.Substring(message.LastIndexOf(">", StringComparison.Ordinal) + 1,
                            message.Length - message.LastIndexOf(">", StringComparison.Ordinal) - 1));
                    message = s.ToString();
                }
            }

            return message;
        }
    }
}
