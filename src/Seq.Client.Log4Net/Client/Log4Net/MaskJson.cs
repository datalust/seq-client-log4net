using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Seq.Client.Log4Net
{
    public class MaskJson
    {
        public bool IsMask;
        public List<string> MaskedProperties { get; set; } = new List<string>();
        public Dictionary<string, string> JsonValues { get; set; }= new Dictionary<string, string>();
        public ExpandoObject JsonObject { get; set; } = new ExpandoObject();
    }

}
