using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seq.Client.Log4Net
{
    public static class Config
    {
        public static List<string> MaskProperties { get; set; }
        public static MaskPolicy MaskType { get; set; } 
        public static bool LogMethodName { get; set; }
        public static bool LogSourceFile { get; set;  }
        public static bool LogLineNumber { get; set;  }
        public static int CacheTime { get; set; }
        public static bool Destructure { get; set; }
        public static string PropertyRegex { get; set; }
        public static string CorrelationProperty { get; set; }

        static Config()
        {
            MaskProperties = new List<string>();
            MaskType = MaskPolicy.None;
            CacheTime = 0;
        }
    }
}
