using System.Collections.Generic;


namespace GraphAnalysis.DataModel
{
    public class Record
    {
        public List<TLine> TLines { get; set; }

        public List<double> DistanceByLine { get; set; }

        public List<double> DistanceByTypeLine { get; set; }
    }
}
