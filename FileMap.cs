using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace shitfixer
{
    public class FileMap
    {
        public string Text { get; set; }
        public string Path { get; set; }
        public int CRLFCount { get; set; }
        public int LFCount { get; set; }
        public int TabCount { get; set; }
        public int SpaceCount { get; set; }
        public int AverageSpaces { get; set; }
        public List<int> Indent { get; set; }
    }
}
