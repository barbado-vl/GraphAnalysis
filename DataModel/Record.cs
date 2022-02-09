using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphAnalysis.DataModel
{
    public class Record
    {
        public List<TLine> TLines { get; set; }

        public List<double> DistanceByLine { get; set; }

        public List<double> DistanceByTypeLine { get; set; }
    }
}
