using GraphAnalysis.DataModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace GraphAnalysis.VM
{
    internal static class CalculateSeriesPeaks
    {
        public static void ThirdPointOfPeak(List<Peak> inputpeaks, List<Candle> candles)
        {
            inputpeaks = inputpeaks.OrderBy(a => a.Tsp.X).ToList();

            // Ищем пик того же направления и ищем выпадающую точку между Tsp обоих пиков
            for (int n = 0; n + 1 < inputpeaks.Count; n++)
            {
                for (int h = n + 1; h < inputpeaks.Count; h++)
                {
                    if (inputpeaks[n].Direction == inputpeaks[h].Direction)
                    {
                        Candle temP;
                        int i;

                        if (inputpeaks[n].Direction is "Up")
                        {
                            i = candles.IndexOf(candles.First(a => a.MaxPoint == inputpeaks[n].Tsp));
                            i++;
                            temP = candles[i];

                            while (candles[i].MaxPoint != inputpeaks[h].Tsp)
                            {
                                i++;
                                if (candles[i].MinPoint.Y > temP.MinPoint.Y) temP = candles[i];
                            }

                            if (inputpeaks[n].CutOffPoint.X < inputpeaks[n].Tsp.X)
                            {
                                inputpeaks[n].FallPoint = temP.MinPoint;
                                inputpeaks[n].CandlesId.Add(temP.id);
                            }
                            if (inputpeaks[h].CutOffPoint.X > inputpeaks[h].Tsp.X)
                            {
                                inputpeaks[h].FallPoint = temP.MinPoint;
                                inputpeaks[h].CandlesId.Add(temP.id);
                            }

                            if (temP.ViewMin.point.Y == 0) temP.CreateEllipse(temP.MinPoint);
                        }
                        else // inputpeaks[n].Direction is "Dn"
                        {
                            i = candles.IndexOf(candles.First(a => a.MinPoint == inputpeaks[n].Tsp));
                            i++;
                            temP = candles[i];

                            while (candles[i].MinPoint != inputpeaks[h].Tsp)
                            {
                                i++;
                                if (candles[i].MaxPoint.Y < temP.MaxPoint.Y) temP = candles[i];
                            }

                            if (inputpeaks[n].CutOffPoint.X < inputpeaks[n].Tsp.X)
                            {
                                inputpeaks[n].FallPoint = temP.MaxPoint;
                                inputpeaks[n].CandlesId.Add(temP.id);
                            }
                            if (inputpeaks[h].CutOffPoint.X > inputpeaks[h].Tsp.X)
                            {
                                inputpeaks[h].FallPoint = temP.MaxPoint;
                                inputpeaks[h].CandlesId.Add(temP.id);
                            }

                            if (temP.ViewMax.point.Y == 0) temP.CreateEllipse(temP.MaxPoint);
                        }

                        break;
                    }
                }
            }

            // Для последнего пика
            if (inputpeaks[^1].CutOffPoint.X < inputpeaks[^1].Tsp.X && inputpeaks[^1].FallPoint.X == 0)
            {
                int i;
                Candle temP;

                if (inputpeaks[^1].Direction is "Up")
                {
                    i = candles.IndexOf(candles.First(a => a.MaxPoint == inputpeaks[^1].Tsp));
                    temP = candles[i];

                    for (int h = i + 1; h < candles.Count; h++)
                    {
                        if (candles[h].MinPoint.Y > temP.MinPoint.Y) temP = candles[h];
                    }
                    inputpeaks[^1].FallPoint = temP.MinPoint;
                    inputpeaks[^1].CandlesId.Add(temP.id);
                    if (temP.ViewMin.point.Y == 0) temP.CreateEllipse(temP.MinPoint);

                }
                else // inputpeaks[n].Direction is "Dn"
                {
                    i = candles.IndexOf(candles.First(a => a.MinPoint == inputpeaks[^1].Tsp));
                    temP = candles[i];

                    for (int h = i + 1; h < candles.Count; h++)
                    {
                        if (candles[h].MaxPoint.Y < temP.MaxPoint.Y) temP = candles[h];
                    }
                    inputpeaks[^1].FallPoint = temP.MaxPoint;
                    inputpeaks[^1].CandlesId.Add(temP.id);
                    if (temP.ViewMax.point.Y == 0) temP.CreateEllipse(temP.MaxPoint);
                }
            }

            // Перепроверка FallPoint на случай ситуаций, когда далее нет пика того же направления
            for (int n = inputpeaks.Count - 1; n > 0; n--)
            {
                if (inputpeaks[n].FallPoint.X == 0)
                {
                    int i = inputpeaks[n].Direction is "Up"
                        ? candles.IndexOf(candles.First(a => a.MaxPoint == inputpeaks[n].Tsp))
                        : candles.IndexOf(candles.First(a => a.MinPoint == inputpeaks[n].Tsp));
                    int end = inputpeaks[n - 1].Direction is "Up"
                        ? candles.IndexOf(candles.First(a => a.MaxPoint == inputpeaks[n - 1].Tsp))
                        : candles.IndexOf(candles.First(a => a.MinPoint == inputpeaks[n - 1].Tsp));
                    Candle maxP = candles[i];
                    Candle minP = candles[i];

                    do
                    {
                        i--;
                        if (candles[i].MinPoint.Y > minP.MinPoint.Y) minP = candles[i];
                        if (candles[i].MaxPoint.Y < maxP.MaxPoint.Y) maxP = candles[i];
                    }
                    while (i > end);

                    if (inputpeaks[n].Direction is "Up")
                    {
                        if (inputpeaks[n].CutOffPoint != minP.MinPoint)
                        {
                            inputpeaks[n].FallPoint = minP.MinPoint;
                            inputpeaks[n].CandlesId.Add(minP.id);
                            if (minP.ViewMin.point.Y == 0) minP.CreateEllipse(minP.MinPoint);
                        }
                    }
                    else // inputpeaks[n].Direction is "Dn"
                    {
                        if (inputpeaks[n].CutOffPoint != maxP.MaxPoint)
                        {
                            inputpeaks[n].FallPoint = maxP.MaxPoint;
                            inputpeaks[n].CandlesId.Add(maxP.id);
                            if (maxP.ViewMax.point.Y == 0) maxP.CreateEllipse(maxP.MaxPoint);
                        }
                    }
                }
            }

            // Проверка на Tsp выпадающие из "массы"... косяк простоты алгоритма расчета пиков
            for (int n = 0; n < inputpeaks.Count; n++)
            {
                if (inputpeaks[n].FallPoint.X != 0) CheckTsp(inputpeaks[n], candles);
            }
        }
        private static void CheckTsp(Peak peak, List<Candle> allcandles)
        {
            int left;
            int right;
            List<Candle> agepeak = new();
            Point newtsp;
            int indexofnewtsp;

            // Получаем диапазон поиска для проверки
            if (peak.Direction is "Up")
            {
                if (peak.FallPoint.X > peak.CutOffPoint.X)
                {
                    left = allcandles.IndexOf(allcandles.First(a => a.MinPoint == peak.CutOffPoint));
                    right = allcandles.IndexOf(allcandles.First(a => a.MinPoint == peak.FallPoint));
                }
                else
                {
                    left = allcandles.IndexOf(allcandles.First(a => a.MinPoint == peak.FallPoint));
                    right = allcandles.IndexOf(allcandles.First(a => a.MinPoint == peak.CutOffPoint));
                }
            }
            else
            {
                if (peak.FallPoint.X > peak.CutOffPoint.X)
                {
                    left = allcandles.IndexOf(allcandles.First(a => a.MaxPoint == peak.CutOffPoint));
                    right = allcandles.IndexOf(allcandles.First(a => a.MaxPoint == peak.FallPoint));
                }
                else
                {
                    left = allcandles.IndexOf(allcandles.First(a => a.MaxPoint == peak.FallPoint));
                    right = allcandles.IndexOf(allcandles.First(a => a.MaxPoint == peak.CutOffPoint));
                }
            }

            for (int n = left + 1; n < right; n++)
            {
                agepeak.Add(allcandles[n]);
            }

            // Новый кандидат в Tsp
            (newtsp, indexofnewtsp) = peak.FindTsp(agepeak);

            // Проверка
            if ((peak.Direction is "Up" && newtsp.Y < peak.Tsp.Y) ||
                (peak.Direction is "Dn" && newtsp.Y > peak.Tsp.Y))
            {
                if (peak.Direction is "Up")
                {
                    peak.CandlesId.Remove(allcandles.First(a => a.MaxPoint == peak.Tsp).id);

                    agepeak[indexofnewtsp].CreateEllipse(agepeak[indexofnewtsp].MaxPoint);
                }
                else
                {
                    peak.CandlesId.Remove(allcandles.First(a => a.MinPoint == peak.Tsp).id);

                    agepeak[indexofnewtsp].CreateEllipse(agepeak[indexofnewtsp].MinPoint);
                }
                peak.Tsp = newtsp;
                peak.CandlesId.Add(agepeak[indexofnewtsp].id);
            }

        }

        public static void ViewPointOfSeriesPeaks(bool seriespeaks, List<Peak> inputpeaks, List<Candle> candles)
        {
            if (seriespeaks)
            {
                foreach (Peak peak in inputpeaks)
                {
                    foreach (string s in peak.CandlesId)
                    {
                        Candle candle = candles.First(a => a.id == s);

                        if (candle.ViewMax.point.X != 0)
                        {
                            candle.ViewMax.ellipse.Stroke = Brushes.DarkBlue;
                            candle.ViewMax.ellipse.Fill = Brushes.DarkBlue;
                        }
                        if (candle.ViewMin.point.X != 0)
                        {
                            candle.ViewMin.ellipse.Stroke = Brushes.DarkBlue;
                            candle.ViewMin.ellipse.Fill = Brushes.DarkBlue;
                        }
                    }
                }
            }
            else
            {
                foreach (Peak peak in inputpeaks)
                {
                    foreach (string s in peak.CandlesId)
                    {
                        Candle candle = candles.First(a => a.id == s);

                        if (candle.ViewMax.point.X != 0)
                        {
                            candle.ViewMax.ellipse.Stroke = Brushes.LightGray;
                            candle.ViewMax.ellipse.Fill = Brushes.LightGray;
                        }
                        if (candle.ViewMin.point.X != 0)
                        {
                            candle.ViewMin.ellipse.Stroke = Brushes.LightGray;
                            candle.ViewMin.ellipse.Fill = Brushes.LightGray;
                        }
                    }
                }
            }
        }
    }
}
