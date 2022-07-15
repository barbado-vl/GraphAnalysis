using GraphAnalysis.DataModel;
using System.Collections.Generic;
using System.Windows;
using System.Linq;

namespace GraphAnalysis.VM
{
    internal class TLinesCalculator
    {
        private readonly string Direction;
        private const string Up = "Up";
        private const string Dn = "Dn";

        private readonly double borderX;
        private readonly double heightCanvas;
        private readonly double Breakdown;

        private readonly List<Peak> seriesPeaks;

        private readonly List<Point> maxPoints;
        private readonly List<Point> minPoints;


        internal TLinesCalculator(List<Peak> seriespeaks, string direction, double width, double height, double breakdown)
        {
            seriesPeaks = seriespeaks.OrderBy(a => a.Tsp.X).ToList();

            Direction = direction;

            borderX = width;
            heightCanvas = height;

            Breakdown = breakdown == 0 ? Direction == seriesPeaks[^1].Direction ? seriesPeaks[^1].Tsp.Y : seriesPeaks[^1].CutOffPoint.Y : breakdown;

            maxPoints = new();
            minPoints = new();
        }

        internal List<TLine> CalculateTLines(List<Candle> basecandles)
        {
            List<TLine> tLines = new();

            maxPoints.Clear();
            minPoints.Clear();

            basecandles = basecandles.OrderBy(a => a.MaxPoint.X).ToList();

            foreach (Candle candle in basecandles)
            {
                foreach (Peak peak in seriesPeaks)
                {
                    if (!maxPoints.Contains(candle.MaxPoint))
                    {
                        if (peak.Tsp == candle.MaxPoint) maxPoints.Add(candle.MaxPoint);
                        if (peak.CutOffPoint == candle.MaxPoint) maxPoints.Add(candle.MaxPoint);
                        if (peak.FallPoint == candle.MaxPoint) maxPoints.Add(candle.MaxPoint);
                        if (peak.DTP.Contains(candle.MaxPoint) || peak.Np.Contains(candle.MaxPoint)) maxPoints.Add(candle.MaxPoint);
                    }

                    if (!minPoints.Contains(candle.MinPoint))
                    {
                        if (peak.Tsp == candle.MinPoint) minPoints.Add(candle.MinPoint);
                        if (peak.CutOffPoint == candle.MinPoint) minPoints.Add(candle.MinPoint);
                        if (peak.FallPoint == candle.MinPoint) minPoints.Add(candle.MinPoint);
                        if (peak.DTP.Contains(candle.MinPoint) || peak.Np.Contains(candle.MinPoint)) minPoints.Add(candle.MinPoint);
                    }
                }
            }

            if (Direction is Up)
            {
                CreateTLine("Лн", maxPoints, maxPoints, tLines);
                CreateTLine("Ор", minPoints, minPoints, tLines);

                CreateTLine("Рэ", maxPoints, minPoints, tLines);
                CreateTLine("Рэ", minPoints, maxPoints, tLines);
            }
            else // Direction is "Dn"
            {
                CreateTLine("Лн", minPoints, minPoints, tLines);
                CreateTLine("Ор", maxPoints, maxPoints, tLines);

                CreateTLine("Рэ", maxPoints, minPoints, tLines);
                CreateTLine("Рэ", minPoints, maxPoints, tLines);
            }

            for (int n = 0; n < tLines.Count; n++)
            {
                tLines[n].Id = "line_" + n.ToString() + ":" + tLines[n].MainType + tLines[n].HistoryType;
            }

            return tLines;
        }

        private void CreateTLine(string tlinetype, List<Point> firstpoints, List<Point> secondpoints, List<TLine> tLines)
        {
            for (int n1 = 0; n1 < firstpoints.Count; n1++)
            {
                int e = firstpoints == secondpoints ? n1 + 1 : 0;

                for (int n2 = e; n2 < secondpoints.Count; n2++)
                {
                    if (secondpoints[n2].X > firstpoints[n1].X)
                    {
                        TLine tline = new(tlinetype, firstpoints[n1], secondpoints[n2]);

                        if (FilterOfLine(tline))
                        {
                            tline.HistoryType = History(tline);

                            tLines.Add(tline);
                        }
                    }
                }
            }
        }

        private bool FilterOfLine(TLine line)
        {
            double y = line.CalculateY(borderX);

            if (Direction is Up)
            {
                return y <= Breakdown && y > 1 - (heightCanvas / 2);
            }
            else // Direction is Dn
            {
                return y >= Breakdown && y < heightCanvas + (heightCanvas / 2);
            }
        }


        #region Расчет Истории касаний линии

        private string History(TLine tline)
        {
            // Параметры отслеживающие касание
            double touchUp = 0;
            double touchDown = 0;
            int touchrowUp = 0;
            int touchrowDown = 0;
            double typetouch = 0;

            Peak peakofpoint;

            string result;

            List<Peak> basepeaks = new();

            // Филтруем пики: убираем пики К
            foreach (Peak peak in seriesPeaks)
            {
                if (!peak.K) basepeaks.Add(peak);
            }

            // Первая точки
            peakofpoint = PointIntoPeak(tline.FirstPoint, seriesPeaks);

            if (!peakofpoint.K)
            {
                if (minPoints.Contains(tline.FirstPoint))
                {
                    touchDown++;
                    touchrowDown++;
                }
                else
                {
                    touchUp++;
                    touchrowUp++;
                }

                typetouch = TypeOfPoint(tline.FirstPoint, basepeaks.IndexOf(peakofpoint), basepeaks);
            }

            // Другие точки
            foreach (Point point in minPoints)
            {
                peakofpoint = PointIntoPeak(point, seriesPeaks);

                if (!peakofpoint.K && tline.FirstPoint != point)
                {
                    if (TouchCheck(tline, point, "Dn", basepeaks.IndexOf(peakofpoint), basepeaks))
                    {
                        touchrowDown++;
                        touchDown++;
                        if (typetouch == 1) typetouch = TypeOfPoint(point, basepeaks.IndexOf(peakofpoint), basepeaks);
                    }
                    else
                    {
                        if (touchrowDown < 3) touchrowDown = 0;
                    }
                }
            }

            foreach (Point point in maxPoints)
            {
                peakofpoint = PointIntoPeak(point, seriesPeaks);

                if (!peakofpoint.K && tline.FirstPoint != point)
                {
                    if (TouchCheck(tline, point, "Up", basepeaks.IndexOf(peakofpoint), basepeaks))
                    {
                        touchrowUp++;
                        touchUp++;
                        if (typetouch == 1) typetouch = TypeOfPoint(point, basepeaks.IndexOf(peakofpoint), basepeaks);
                    }
                    else
                    {
                        if (touchrowUp < 3) touchrowUp = 0;
                    }
                }
            }

            // Подсчет и вывод результата
            result = typetouch == 1 ? "С" + (touchDown + touchUp).ToString() : "С" + (touchDown + touchUp - 0.5).ToString();
            if (touchrowDown > 2 || touchrowUp > 2) result += "п";

            return result;
        }

        private Peak PointIntoPeak(Point point, List<Peak> peaks)
        {
            foreach (Peak peak in peaks)
            {
                if (point == peak.Tsp || point == peak.CutOffPoint || point == peak.FallPoint
                   || peak.Np.Contains(point) || peak.DTP.Contains(point))
                {
                    return peak;
                }
            }
            return null; // не должно -- при вызове TLineCalculator в MainWindowVM проверку на вхождение точек в пики прохожили... если что ошибка
        }

        private bool TouchCheck(TLine tline, Point point, string directionP, int n, List<Peak> peaks)
        {
            double L_Ypoint = 0;
            double R_Ypoint = 0;
            double Y_point = tline.CalculateY(point.X);
            double touchcheck;

            if (directionP is "Dn") Y_point -= 0.001;
            else Y_point += 0.001;

            // Ищем вектор двыижения

            // Tsp или DTP Tsp
            if (point.Y == peaks[n].Tsp.Y ||
                (peaks[n].DTP.Contains(point) &&
                 (peaks[n].Direction is "Up" && point.Y <= peaks[n].Tsp.Y) ||
                 (peaks[n].Direction is "Dn" && point.Y >= peaks[n].Tsp.Y)))
            {
                L_Ypoint = peaks[n].CutOffPoint.Y;
                R_Ypoint = peaks[n].FallPoint.Y;
            }
            // Np упрощенно
            else if (peaks[n].Np.Contains(point))
            {
                L_Ypoint = peaks[n].Tsp.Y;
                R_Ypoint = peaks[n].Tsp.Y;
            }
            // CutOffPoint и  FallPoint. Далее 2 условия поиска: 1) справа или слева от пика n искать, 2) есть ли пики n - 1 / n + 1
            else if (point.X < peaks[n].Tsp.X) // слева от tsp
            {
                R_Ypoint = peaks[n].Tsp.Y;

                if (n == 0)
                {
                    L_Ypoint = peaks[n].Tsp.Y;
                    R_Ypoint = peaks[n].Tsp.Y;
                }
                else
                {
                    if (peaks[n].Direction == peaks[n - 1].Direction) L_Ypoint = peaks[n - 1].Tsp.Y;
                    else
                    {
                        if (peaks[n - 1].CutOffPoint.X > peaks[n - 1].Tsp.X) L_Ypoint = peaks[n - 1].CutOffPoint.Y;
                        if (peaks[n - 1].FallPoint.X > peaks[n - 1].Tsp.X) L_Ypoint = peaks[n - 1].FallPoint.Y;
                    }
                }
            }
            else if (point.X > peaks[n].Tsp.X) // справа от tsp
            {
                L_Ypoint = peaks[n].Tsp.Y;

                if (n == peaks.Count - 1)
                {
                    L_Ypoint = peaks[n].Tsp.Y;
                    R_Ypoint = peaks[n].Tsp.Y;
                }
                else
                {
                    if (peaks[n].Direction == peaks[n + 1].Direction) R_Ypoint = peaks[n + 1].Tsp.Y;
                    else
                    {
                        if (peaks[n + 1].CutOffPoint.X > peaks[n + 1].Tsp.X) R_Ypoint = peaks[n + 1].CutOffPoint.Y;
                        if (peaks[n + 1].FallPoint.X > peaks[n + 1].Tsp.X) R_Ypoint = peaks[n + 1].FallPoint.Y;
                    }
                }
            }


            // Рассчитываем "касание"
            if (directionP is "Dn" && Y_point <= point.Y)
            {
                touchcheck = point.Y - L_Ypoint < point.Y - R_Ypoint ? (point.Y - L_Ypoint) * 0.117 : (point.Y - R_Ypoint) * 0.117;

                if (point.Y - Y_point <= touchcheck) return true;
            }
            else if (directionP is "Up" && Y_point >= point.Y)
            {
                touchcheck = L_Ypoint - point.Y < R_Ypoint - point.Y ? (L_Ypoint - point.Y) * 0.117 : (R_Ypoint - point.Y) * 0.117;

                if (Y_point - point.Y <= touchcheck) return true;
            }

            return false;
        }

        private double TypeOfPoint(Point point, int n, List<Peak> peaks)
        {
            if (point.Y == peaks[n].CutOffPoint.Y || point.Y == peaks[n].FallPoint.Y) return 1;
            if (peaks[n].Np.Contains(point)) return 0.5;
            if (peaks[n].DTP.Contains(point) && point.Y != peaks[n].Tsp.Y) return 0.5;
            else // если Tsp или DTP как Tsp при Y = Y, других вариантов не остается
            {
                if (n > 0 && n < peaks.Count - 1)
                {
                    if ((peaks[n].Direction != peaks[n - 1].Direction && (point.Y == peaks[n - 1].CutOffPoint.Y || point.Y == peaks[n - 1].FallPoint.Y))
                     || (peaks[n].Direction != peaks[n + 1].Direction && (point.Y == peaks[n + 1].CutOffPoint.Y || point.Y == peaks[n + 1].FallPoint.Y))) return 1;
                    else return 0.5;
                }
                else if (n == 0 && n < peaks.Count - 1) // первый пик
                {
                    if (peaks[n].Direction is "Up" && peaks[n].Direction == peaks[n + 1].Direction && point.Y > peaks[n + 1].Tsp.Y) return 0.5;
                    else if (peaks[n].Direction is "Dn" && peaks[n].Direction == peaks[n + 1].Direction && point.Y < peaks[n + 1].Tsp.Y) return 0.5;
                    else return 1;
                }
                else if (n > 0 && n == peaks.Count - 1) // последний пик
                {
                    if (peaks[n].Direction != peaks[n - 1].Direction && (point.Y == peaks[n - 1].CutOffPoint.Y || point.Y == peaks[n - 1].FallPoint.Y)) return 1;
                    else if (peaks[n].Direction == peaks[n - 1].Direction && peaks[n].Direction is "Up" && point.Y < peaks[n - 1].Tsp.Y) return 1;
                    else if (peaks[n].Direction == peaks[n - 1].Direction && peaks[n].Direction is "Dn" && point.Y > peaks[n - 1].Tsp.Y) return 1;
                    else return 0.5;
                }
                return 0;
            }
        }

        #endregion
    }
}
