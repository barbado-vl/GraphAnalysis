using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

using Emgu.CV.Util;

namespace GraphAnalysis.DataModel
{
    public class Candle
    {
        public Candle(VectorOfPoint contour)
        {
            PointCollection myPointCollection = new();
            int max = 10000;
            int min = 0;

            for (int n = 0; n < contour.Size; n++)
            {
                if (contour[n].Y < max) { max = contour[n].Y; }
                if (contour[n].Y > min) { min = contour[n].Y; }

                Point myP = new(contour[n].X + 0.5, contour[n].Y + 0.5);
                myPointCollection.Add(myP);
            }

            PContour.Stroke = Brushes.Red;
            PContour.StrokeThickness = 1;
            //PContour.Fill = Brushes.White;
            PContour.Points = myPointCollection;

            MaxPoint = IfSeveralMaxMinPoint(max, contour);
            MinPoint = IfSeveralMaxMinPoint(min, contour);
        }

        private Point _MaxPoint;
        public Point MaxPoint
        {
            get { return _MaxPoint; }
            set => _MaxPoint = value;
        }

        private Point _MinPoint;
        public Point MinPoint
        {
            get { return _MinPoint; }
            set { _MinPoint = value; }
        }

        public string id = "candle_";
        public Polygon PContour = new();

        private static Point IfSeveralMaxMinPoint(int mm, VectorOfPoint contour)
        {
            List<Point> mmPoints = new();

            for (int n = 0; n < contour.Size; n++)
            {
                if (contour[n].Y == mm) 
                {
                    Point myP = new(contour[n].X + 0.5, contour[n].Y + 0.5);
                    mmPoints.Add(myP);
                }
            }

            if (mmPoints.Count % 2 == 0) { return mmPoints[mmPoints.Count / 2]; }
            else
            {
                return mmPoints[Convert.ToInt32(Math.Round((double)mmPoints.Count / 2))];
            }
        }
    }
}
