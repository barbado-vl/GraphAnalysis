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
            id = "candle_";

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

            MaxPoint = IfSeveralMaxMinPoint(max, contour);
            MinPoint = IfSeveralMaxMinPoint(min, contour);

            CreatePolygon(myPointCollection);
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
            set => _MinPoint = value;
        }

        public string id;

        public Polygon Contour { get; set; }

        public (Ellipse ellipse, Point point) ViewMax;
        public (Ellipse ellipse, Point point) ViewMin;


        private void CreatePolygon(PointCollection pointCollection)
        {
            // прозрачный фон -- чтобы не видеть, но можно было взаимодействовать
            SolidColorBrush myBrush = new();
            myBrush.Opacity = 0;

            Contour = new();
            Contour.Fill = myBrush;
            Contour.Stroke = Brushes.Red;
            Contour.StrokeThickness = 1;
            Contour.Points = pointCollection;
        }

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

            //if (mmPoints.Count % 2 == 0) { return mmPoints[mmPoints.Count / 2]; }
            //else
            //{
            //    return mmPoints[Convert.ToInt32(Math.Round((double)mmPoints.Count / 2))];
            //}
            return mmPoints.Count % 2 == 0 ? mmPoints[mmPoints.Count / 2] : mmPoints[Convert.ToInt32(Math.Round((double)mmPoints.Count / 2))];
        }

        // вызывается, когда экстремум свечки(Max/Min) присваивается как точка пика
        // в классе Peak
        // в классе CalculatedSeriesPeaks
        public void CreateEllipse(Point center)
        {
            if (center == MaxPoint && ViewMax.ellipse == null)
            {
                ViewMax = new();
                ViewMax.point = MaxPoint;

                ViewMax.ellipse = new();
                ViewMax.ellipse.Stroke = Brushes.LightGray;
                ViewMax.ellipse.StrokeThickness = 1;
                ViewMax.ellipse.Fill = Brushes.LightGray;
                ViewMax.ellipse.Width = 5;
                ViewMax.ellipse.Height = 5;
                ViewMax.ellipse.Uid = id;

            }
            else if (center == MinPoint && ViewMin.ellipse == null)
            {
                ViewMin = new();
                ViewMin.point = MinPoint;

                ViewMin.ellipse = new();
                ViewMin.ellipse.Stroke = Brushes.LightGray;
                ViewMin.ellipse.StrokeThickness = 1;
                ViewMin.ellipse.Fill = Brushes.LightGray;
                ViewMin.ellipse.Width = 5;
                ViewMin.ellipse.Height = 5;
                ViewMin.ellipse.Uid = id;
            }
        }
    }
}
