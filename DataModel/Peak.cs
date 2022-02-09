
using System.Collections.Generic;
using System.Windows;

namespace GraphAnalysis.DataModel
{
    public class Peak
    {
        public int Age { get; set; }
        public int mass { get; set; }

        public List<Point> mainPoints { get; set; }
        public List<Point> extraPoints { get; set; }

        public string id { get; set; }
    }
}
