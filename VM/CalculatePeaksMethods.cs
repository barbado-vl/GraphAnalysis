using GraphAnalysis.DataModel;
using System.Collections.Generic;
using System.Linq;


namespace GraphAnalysis.VM
{

    /// <summary> Calculation peaks </summary>
    /// Peak consist from two movement, vector up and vector down.
    /// There are simple peaks from line or Japanese candlesticks on the chart.
    /// There are complex peaks, the vectors of which consist of simple peaks, on the chart. 
    /// Input data is Japanese candlesticks that i get by using emguCV find contours.
    /// 
    /// (??? method) The first step is to compare candlestick extremes to get simple vectors.
    /// 
    /// 
    /// (ArrangementVectors method) The second step is consist from two thing. 
    /// Fist thing is is to separate the appearance of a new vector, which gives a new peak, and the update of the current peak parameters.
    /// Secnd thing knowing the direction of the last vector and the location of the points of the vectors get the peak and its parameters.
    /// ......... Third step ...............
    /// 
    public class CalculatePeaks
    {
        public List<Peak> Peaks = new();

        public CalculatePeaks(List<Candle> candles)
        {

            CandlesStep(candles);
        }


        private void CandlesStep(List<Candle> Candles)
        {
            List<Candle> VectorUp = new();
            List<Candle> VectorDown = new();

            string lastVector = "0";

            for (int n = 0; n + 1 < Candles.Count; n++)
            {
                /// <summary> A combination extremum of candles /// </summary>
                if (Candles[n].MaxPoint.Y < Candles[n + 1].MaxPoint.Y & Candles[n].MinPoint.Y < Candles[n + 1].MinPoint.Y)
                {
                    switch (lastVector)
                    {
                        case "0":
                            VectorDown.Add(Candles[n]); VectorDown.Add(Candles[n + 1]);
                            lastVector = "down";
                            break;
                        case "down":
                            VectorDown.Add(Candles[n + 1]);
                            break;
                        case "up":
                            VectorDown.Clear();
                            VectorDown.Add(Candles[n]); VectorDown.Add(Candles[n + 1]);
                            lastVector = "down";
                            break;
                    }
                    if (VectorUp.Any())
                    {
                        CreateOrUpdateSimplePeak("up", VectorUp, VectorDown);
                    }
                }

                /// <summary> B combination extremum of candles /// </summary>
                else if (Candles[n].MaxPoint.Y > Candles[n + 1].MaxPoint.Y & Candles[n].MinPoint.Y > Candles[n + 1].MinPoint.Y)
                {
                    switch (lastVector)
                    {
                        case "0":
                            VectorUp.Add(Candles[n]); VectorUp.Add(Candles[n + 1]);
                            lastVector = "up";
                            break;
                        case "down":
                            VectorUp.Clear();
                            VectorUp.Add(Candles[n]); VectorUp.Add(Candles[n + 1]);
                            lastVector = "up";
                            break;
                        case "up":
                            VectorUp.Add(Candles[n + 1]);
                            break;
                    }
                    if (VectorDown.Any())
                    {
                        CreateOrUpdateSimplePeak("down", VectorUp, VectorDown);
                    }
                }

                /// <summary> C-D-E-F combination extremum of candles, plus f1 f2 f3 f4 /// </summary>
                else
                {
                    switch (lastVector)
                    {
                        case "0":                                                       // 
                            VectorUp.Add(Candles[n]);
                            VectorDown.Add(Candles[n]); VectorDown.Add(Candles[n + 1]);

                            CreateOrUpdateSimplePeak("up", VectorUp, VectorDown);

                            VectorUp.Clear();
                            VectorUp.Add(Candles[n + 1]);
                            lastVector = "up";

                            CreateOrUpdateSimplePeak("down", VectorUp, VectorDown);
                            break;
                        case "down":                                                   // 
                            VectorUp.Clear();
                            VectorUp.Add(Candles[n]); VectorUp.Add(Candles[n + 1]);

                            CreateOrUpdateSimplePeak("down", VectorUp, VectorDown);

                            VectorDown.Clear();
                            VectorDown.Add(Candles[n + 1]);
                            lastVector = "down";

                            CreateOrUpdateSimplePeak("up", VectorUp, VectorDown);
                            break;
                        case "up":                                                     // 
                            VectorDown.Clear();
                            VectorDown.Add(Candles[n]); VectorDown.Add(Candles[n + 1]);

                            CreateOrUpdateSimplePeak("up", VectorUp, VectorDown);

                            VectorUp.Clear();
                            VectorUp.Add(Candles[n + 1]);
                            lastVector = "up";

                            CreateOrUpdateSimplePeak("down", VectorUp, VectorDown);
                            break;
                    }
                }
            }
        }

        private void CreateOrUpdateSimplePeak(string direction, List<Candle> vectorUp, List<Candle> vectorDown)
        {
            if (Peaks.Any() && Peaks[^1].Direction == direction)
            {
                if (direction == "up")
                {
                    Peaks[^1].Tsk = (vectorDown[^1].id, vectorDown[^1].MinPoint);
                    Peaks[^1].UpdateAge(vectorDown, 1);
                }
                else // direction == "down"
                {
                    Peaks[^1].Tsk = (vectorUp[^1].id, vectorUp[^1].MaxPoint);
                    Peaks[^1].UpdateAge(vectorUp, 1);
                }

                if (Peaks[^1].State == 2)
                {
                    Peaks[^1].UpdateMass(direction);
                }

                Peaks[^1].Id = "peak" + Peaks[^1].Mass.ToString();
            }
            else
            {
                Peaks.Add(new Peak(direction, vectorUp, vectorDown));
            }

            BypassingPeaks();
        }

        private void BypassingPeaks()
        {
            for (int N = Peaks.Count - 1; N > 0;)
            {
                N = PropogationOfChange(N);
            }
        }

        private int PropogationOfChange(int N)
        {
            /// <summary> Ищем предыдущий активный пик того же направления, пока не дойдем до последнего Peaks[0]/// </summary>
            int I = N - 1;
            while (I != 0 || (Peaks[N].Direction != Peaks[I].Direction && Peaks[I].State != 0)) { I--; }

            if (I == 0) { return 0; }

            /// <summary> Тип изменения в зависимости от направление пика, которое подсказывает как сранивать точки /// </summary>
            if (Peaks[N].Direction == "up")
            {
                /// <summary> Точки поровнялись, но пробоя нет /// </summary>
                if (Peaks[N].Tsn.ExtremPpoint.Y == Peaks[N].Tsk.ExtremPpoint.Y)
                {
                    
                    
                    //while (I != 0 || (Peaks[N].Direction != Peaks[I].Direction && Peaks[I].State != 2)) { I--; }

                    //if (I == 0) { return 0; }
                    //else { UpdateDifficultPeak(N, I); return I; }



                }

                /// <summary> Пробой пика Вверх по Tsn /// </summary>
                else if (Peaks[N].Tsn.ExtremPpoint.Y < Peaks[N].Tsk.ExtremPpoint.Y)
                {

                }

                /// <summary> Внутри /// </summary>
                else
                {

                }

                return 0; // потом убрать, когда условия заполню
            }
            else
            {
                if (Peaks[N].Tsn.ExtremPpoint.Y == Peaks[N].Tsk.ExtremPpoint.Y)
                {

                }
                else if (Peaks[N].Tsn.ExtremPpoint.Y > Peaks[N].Tsk.ExtremPpoint.Y)
                {

                }
                else // Peaks[N].Tsn.ExtremPpoint.Y < Peaks[N].Tsk.ExtremPpoint.Y
                {

                }

                return 0; // потом убрать, когда условия заполню
            }

        }

        private void UpdateDifficultPeak(int N, int I)
        {
            Peaks[I].Tsk = Peaks[N].Tsk;

            List<Candle> tempList = new();
            int n = Peaks[N].Age.Count - 1;
            while (Peaks[N].Age[n] != Peaks[I].Age[^1])
            {
                tempList.Add(Peaks[N].Age[n]);
            }
            if (tempList.Any())
            {
                Peaks[I].UpdateAge(tempList, 0);
                Peaks[I].UpdateMass(Peaks[I].Direction);
                Peaks[I].Id = "peak" + Peaks[I].Mass.ToString();
            }
        }
    }

}
