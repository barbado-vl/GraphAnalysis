using System.Windows;
using System.Windows.Shapes;

namespace GraphAnalysis.DataModel
{
    public class Candle
    {
        public Point max { get; set; }
        public Point min { get; set; }
        string id;
        public Polygon polygon { get; set; }
    }
}
