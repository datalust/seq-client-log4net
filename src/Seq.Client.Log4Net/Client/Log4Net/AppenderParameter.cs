using log4net.Layout;
using System;
using System.Collections.Generic;
using System.Text;

namespace Seq.Client.Log4Net
{
    public class AppenderParameter
    {
        public string ParameterName { get; set; }

        public IRawLayout Layout { get; set; }
    }
}
