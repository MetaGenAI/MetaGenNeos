using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metagen
{
    public class MetaGenUser
    {
        public string userId { get; set; }
        public bool default_public { get; set; }
        public bool is_friend { get; set; }
        public bool default_recording { get; set; }
        public bool is_banned { get; set; }
        public float total_recorded_public { get; set; }
        public float total_recorded { get; set; }
    }
}
