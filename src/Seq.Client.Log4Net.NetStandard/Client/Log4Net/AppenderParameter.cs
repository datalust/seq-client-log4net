using log4net.Layout;
using System;
using System.Collections.Generic;
using System.Text;

namespace Seq.Client.Log4Net
{
    public class AppenderParameter
    {
        private string parameterName;
        private IRawLayout layout;

        public string ParameterName
        {
            get => parameterName;
            set => parameterName = value;
        }

        public IRawLayout Layout 
        { 
            get => layout; 
            set => layout = value; 
        }
    }
}
