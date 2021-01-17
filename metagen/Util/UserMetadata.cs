using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metagen
{
    public class UserMetadata
    {
        public string userRefId { get; set; }
        public string userId { get; set; }
        public bool isPublic { get; set; }
        public bool isRecording { get; set; }
        public string headDevice { get; set; }
        public string platform { get; set; }
        public string bodyNodes { get; set; }
        public string devices { get; set; }
    }
}
