using Xunit;
using Seq.Client.Log4Net;

namespace Seq.Client.Log4Net.Tests
{
    public class SeqAppenderTests
    {
        [Fact]
        public SeqAppender CanConstructAppender()
        {
            return new SeqAppender();
        }
    }
}
