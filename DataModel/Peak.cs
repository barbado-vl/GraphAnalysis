
using System.Collections.Generic;
using System.Windows;

namespace GraphAnalysis.DataModel
{
    public class Peak
    {
        /// <summary> Peaks made from candles (simple peak) /// </summary>
        public Peak(string direction, List<Candle> vectorUp, List<Candle> vectorDown)
        {
            if (direction == "up")
            {
                Tsn = (vectorUp[0].id, vectorUp[0].MinPoint);
                Tsp = (vectorDown[0].id, vectorDown[0].MaxPoint);
                Tsk = (vectorDown[^1].id, vectorDown[^1].MinPoint);

                UpdateAge(vectorUp, 0);
                UpdateAge(vectorDown, 1);
            }
            else // direction == "down"
            {
                Tsn = (vectorDown[0].id, vectorDown[0].MaxPoint);
                Tsp = (vectorUp[0].id, vectorUp[0].MinPoint);
                Tsk = (vectorUp[^1].id, vectorUp[^1].MaxPoint);

                UpdateAge(vectorDown, 0);
                UpdateAge(vectorUp, 1);
            }

            UpdateMass(Direction);
            State = 2;
            Direction = direction;
            Id = "peak_" + Mass.ToString();
        }

        /// <summary> Peaks made from peaks /// </summary>
        public Peak(string direction)
        {
            UpdateMass(Direction);
            Id = "peak_" + Mass.ToString();
        }


        public string Direction { get; }

        public List<Candle> Age = new();
        public int Mass { get; set; }
        public string Id { get; set; }

        public (string CandleId, Point ExtremPpoint) Tsn { get; }
        public (string CandleId, Point ExtremPpoint) Tsp { get; set; }
        public (string CandleId, Point ExtremPpoint) Tsk { get; set; }


        public int State { get; set; }
        public List<(string CandleId, Point ExtremPpoint)> TslAndDtp { get; set; }


        public void UpdateAge(List<Candle> candles, int start)
        {
            for (int n = start; n < candles.Count; n++)
            {
                Age.Add(candles[n]);
            }
        }

        public void UpdateMass(string direction)
        {
            Mass = 0;

            if (direction == "up")
            {
                if (Tsn.ExtremPpoint.Y >= Tsk.ExtremPpoint.Y)
                {
                    for (int n = Age.Count - 1; n >= 0; n--)
                    {
                        if (Age[n].MinPoint.Y <= Tsk.ExtremPpoint.Y) { Mass++; }
                        else { break; }
                    }
                }
                else // Tsn.ExtremPpoint.Y < Tsk.ExtremPpoint.Y
                {
                    State = 1;
                    for (int n = 0; n < Age.Count; n++)
                    {
                        if (Age[n].MinPoint.Y <= Tsn.ExtremPpoint.Y) { Mass++; }
                        else { break; }
                    }
                }
            }
            else // direction == "down"
            {
                if (Tsn.ExtremPpoint.Y <= Tsk.ExtremPpoint.Y)
                {
                    for (int n = Age.Count - 1; n >= 0; n--)
                    {
                        if (Age[n].MaxPoint.Y >= Tsk.ExtremPpoint.Y) { Mass++; }
                        else { break; }
                    }
                }
                else // Tsn.ExtremPpoint.Y < Tsk.ExtremPpoint.Y
                {
                    State = 1;
                    for (int n = 0; n < Age.Count; n++)
                    {
                        if (Age[n].MaxPoint.Y >= Tsn.ExtremPpoint.Y) { Mass++; }
                        else { break; }
                    }
                }
            }
        }

    }
}
