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
        public TLine() 
        {
            Id = "";
        }

        public Point FirstPoint { get; set; }
        public Point SecondPoint { get; set; }

        public string MainLtype { get; set; }
        public string TouchLtype { get; set; }
        public bool CommonLtype { get; set; }
        public string VectorLtype { get; set; }

        public string Id { get; set; }

    }
}
