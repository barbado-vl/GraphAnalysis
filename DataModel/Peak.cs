
using System;
using System.Collections.Generic;
using System.Windows;

namespace GraphAnalysis.DataModel
{
    public class Peak
    {
        /// <summary>  /// </summary>
        public Peak(string direction, List<Candle> mass)
        {
            Direction = direction;
            Mass = mass.Count;
            Id = "peak_" + Mass.ToString();

            if (direction == "up")
            {
                TextPoint = CalculateTextPoint(mass[0].MinPoint, mass[^1].MinPoint, Direction);
                CutOffPoint = mass[0].MinPoint;
            }
            else
            {
                TextPoint = CalculateTextPoint(mass[0].MaxPoint, mass[^1].MaxPoint, Direction);
                CutOffPoint = mass[0].MaxPoint;
            }

            int indexTsp;
            (Tsp, indexTsp) = FindTsp(Direction, mass);

            CandlesId.Add(mass[0].id);
            CandlesId.Add(mass[indexTsp].id);
        }

        /// <summary> НА ПОТОМ      Ручное добавление /// </summary>
        public Peak()
        {

        }


        public string Direction { get; }
        public int Mass { get; set; }
        public string Id { get; set; }

        public Point TextPoint { get; set; }
        public Point CutOffPoint { get; set; }
        public Point Tsp { get; set; }

        public List<string> CandlesId = new();


        private Point CalculateTextPoint(Point start, Point wall, string direction)
        {
            Point textpoint = new();

            double a, b;
            if (start.X > wall.X) { a = start.X; b = wall.X; }
            else { a = wall.X; b = start.X; }

            textpoint.Y = direction == "up" ? start.Y : start.Y - 20;
            textpoint.X = a - ((a - b) / 2);   //     при В лево от меньшего отнимает ... назад уходим

            return textpoint;
        }

        private (Point, int) FindTsp(string direction, List<Candle> candles)
        {
            Point tsp;
            int x = 0;

            if (direction == "up")
            {
                tsp = candles[0].MaxPoint;
                for (int n = 1; n < candles.Count; n++)
                {
                    if (candles[n].MaxPoint.Y < tsp.Y) { tsp = candles[n].MaxPoint; x = n; }
                }
            }
            else
            {
                tsp = candles[0].MinPoint;
                for (int n = 1; n < candles.Count; n++)
                {
                    if (candles[n].MinPoint.Y > tsp.Y) { tsp = candles[n].MinPoint; x = n; }
                }
            }

            return (tsp, x);
        }
    }
}
