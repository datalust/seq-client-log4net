using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Seq.Client.Log4Net
{
    public static class Masking
    {
        public static List<string> MaskProperties { get; set; }
        public static MaskPolicy MaskType { get; set; } 

        static Masking()
        {
            MaskProperties = new List<string>();
            MaskType = MaskPolicy.None;
        }

        public static string Mask(string name, string value)
        {
            if (!MaskProperties.Contains(name)) return value;
            switch (MaskType)
            {
                case MaskPolicy.MaskWithString:
                    return "XXXXXX";
                case MaskPolicy.MaskLettersAndNumbers:
                    var replaceValue = Regex.Replace(value.ToString(), "[A-Z]", "X",
                        RegexOptions.IgnoreCase);
                    return Regex.Replace(replaceValue, "\\d", "*");
                default:
                    return value;
            }
        }

    }
}