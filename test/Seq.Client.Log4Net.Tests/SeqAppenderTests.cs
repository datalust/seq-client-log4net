using System;
using Xunit;

namespace Seq.Client.Log4Net.Tests
{
    public class SeqAppenderTests
    {
        [Fact]
        public void CanConstructAppender()
        {
            GC.KeepAlive(new SeqAppender());
        }
    }
}
