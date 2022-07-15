using System.Collections.Generic;
using System.Linq;
using System.Windows;


namespace GraphAnalysis.DataModel
{
    public class TLine
    {
        // TLinesCalculator
        public TLine(string maintype, Point firstpoint, Point secondpoint)
        {
            MainType = maintype;
            FirstPoint = firstpoint;

            k = (firstpoint.Y - secondpoint.Y) / (firstpoint.X - secondpoint.X);
            b = firstpoint.Y - k * firstpoint.X;

            CommonType = false;
            VectorType = "";

            NextProximity = new string[4];
            PreviousProximity = new string[4];
        }

        // copy cell
        public TLine(TLine pretline)
        {
            FirstPoint = pretline.FirstPoint;
            SecondPoint = pretline.SecondPoint;

            k = pretline.k;
            b = pretline.b;

            MainType = pretline.MainType;
            CommonType = pretline.CommonType;
            HistoryType = pretline.HistoryType;
            VectorType = pretline.VectorType;

            Id = pretline.Id;

            Way = 2;

            NextProximity = pretline.NextProximity;
            PreviousProximity = pretline.PreviousProximity;
        }

        // handmade
        public TLine(string direction, string[] inputdata, Candle firstC, Candle secondC)
        {
            Point firstpoint = inputdata[0] is "max" ? firstC.MaxPoint : firstC.MinPoint;
            Point secondpoint = inputdata[1] is "max" ? secondC.MaxPoint : secondC.MinPoint;

            FirstPoint = firstpoint;
            k = (firstpoint.Y - secondpoint.Y) / (firstpoint.X - secondpoint.X);
            b = firstpoint.Y - k * firstpoint.X;

            if ((direction is "Up" && inputdata[0] is "max" && inputdata[1] is "max")
             || (direction is "Dn" && inputdata[0] is "min" && inputdata[1] is "min"))
                MainType = "Лн";
            else if ((direction is "Up" && inputdata[0] is "min" && inputdata[1] is "min")
                  || (direction is "Dn" && inputdata[0] is "max" && inputdata[1] is "max"))
                MainType = "Ор";
            else if ((inputdata[0] is "max" && inputdata[1] is "min")
                  || (inputdata[0] is "min" && inputdata[1] is "max"))
                MainType = "Рэ";

            HistoryType = inputdata.Length > 2 ? inputdata[2] : "";
            CommonType = false;
            VectorType = "";

            NextProximity = new string[4];
            PreviousProximity = new string[4];
        }

        public Point FirstPoint { get; set; }
        public Point SecondPoint { get; set; }

        // коэффициенты линейной функции Y = kX + b
        public double k;
        public double b;

        public string MainType { get; set; }
        public string HistoryType { get; set; }
        public string VectorType { get; set; }

        public bool CommonType { get; set; }

        public string Id { get; set; }

        public int Way;

        public string[] NextProximity;
        public string[] PreviousProximity;


        public double CalculateY(double x)
        {
            return (k * x) + b;
        }

        public void VectorTypeMethod(string direction, List<Point> maxpoints, List<Point> minpoints)
        {
            int touch;

            if (direction is "Up")
            {
                for (int n = maxpoints.Count - 1; n >= 0; n--)
                {
                    double touchcheck = 0.117 * (minpoints[n].Y - maxpoints[n].Y);
                    double pointY = CalculateY(maxpoints[n].X) + 0.001;

                    touch = pointY >= maxpoints[n].Y && pointY - maxpoints[n].Y <= touchcheck ? 1 : 0;

                    if (n == maxpoints.Count - 1 && touch == 1)
                        VectorType = "С";

                    if (VectorType != "" && VectorType[0].ToString() is "С")
                    {
                        VectorType += touch == 1 ? "С" : "ч";
                    }
                    else break;
                }
            }
            else // direction is "Dn"
            {
                for (int n = 0; n < minpoints.Count; n++)
                {
                    double touchcheck = 0.117 * (maxpoints[n].Y - minpoints[n].Y);
                    double pointY = CalculateY(minpoints[n].X) + 0.001;

                    touch = pointY <= minpoints[n].Y && minpoints[n].Y - pointY <= touchcheck ? 1 : 0;

                    if (n == maxpoints.Count - 1 && touch == 1)
                        VectorType = "С";

                    if (VectorType != "" && VectorType[0].ToString() is "С")
                    {
                        VectorType += touch == 1 ? "С" : "ч";
                    }
                    else break;
                }
            }
            if (VectorType.Split("C").Length < 3) VectorType = "С";
        }

    }
}
