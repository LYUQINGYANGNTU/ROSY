using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceAssistantClient
{
    class VisionResult
    {
        public string id { get; set; }
        public string project { get; set; }
        public string iteration { get; set; }
        public string created { get; set; }

        public List<predictions> Predictions { get; set; }

        public class predictions 
        {
            public string probability { get; set; }
            public string tagId { get; set; }
            public string tagName { get; set; }

            public boundingbox Boundingbox { get; set; }

            public class boundingbox
            {
                public string left { get; set; }
                public string top { get; set; }
                public string width { get; set; }
                public string height { get; set; }
            }

            public string tagType { get; set; }

        }
    }
}
