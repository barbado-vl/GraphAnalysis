using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace GraphAnalysis.DataModel
{
    public class TLine
    {
        public Point FirstPoint { get; set; }
        public Point SecondPoint { get; set; }

        string MainLtype { get; set; }
        string TouchLtype { get; set; }
        bool CommonLtype { get; set; }
        string VectorLtype { get; set; }

        string id { get; set; }

    }
}
