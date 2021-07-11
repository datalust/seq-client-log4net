using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

namespace Seq.Client.Log4Net
{
    public static class Masking
    {

        public static object Mask(string name, object value)
        {
            if (!Config.MaskProperties.Contains(name)) return value;
            switch (Config.MaskType)
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