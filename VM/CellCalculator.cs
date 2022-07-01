using GraphAnalysis.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;


namespace GraphAnalysis.VM
{
    internal static class CellCalculator
    {
        internal static List<TLine> CellMethod(string direction, List<TLine> tlines, List<Candle> candleway, double breakdown, double zone1314, double startpoint)
        {
            List<TLine> touchcells = new();

            double border = breakdown;


            // Обнулим близость, нужно для повторного пересчета
            foreach (TLine tline in tlines)
            {
                Array.Clear(tline.NextProximity, 0, 4);
                Array.Clear(tline.PreviousProximity, 0, 4);
            }


            for (int ind = 0; ind < candleway.Count; ind++)
            {
                // Считаем вторую точку и сортируем, считаем border

                if (direction is "Up")
                {
                    foreach (TLine tline in tlines)
                    {
                        tline.SecondPoint = new(candleway[ind].MaxPoint.X, tline.CalculateY(candleway[ind].MaxPoint.X));
                    }
                    tlines = tlines.OrderByDescending(a => a.SecondPoint.Y).ToList();

                    border = ind > 0 && candleway[ind - 1].MaxPoint.Y < border ? candleway[ind - 1].MaxPoint.Y : border;
                }
                else // direction is "Dn"
                {
                    foreach (TLine tline in tlines)
                    {
                        tline.SecondPoint = new(candleway[ind].MinPoint.X, tline.CalculateY(candleway[ind].MinPoint.X));
                    }
                    tlines = tlines.OrderBy(a => a.SecondPoint.Y).ToList();

                    border = ind > 0 && candleway[ind - 1].MinPoint.Y > border ? candleway[ind - 1].MinPoint.Y : border;
                }

                // Смотрим касание и считаем близость

                foreach (TLine tline in tlines)
                {
                    int nL = tlines.IndexOf(tline);

                    if (tline != tlines[^1] && Touch(direction, tline, candleway[ind], border, ind, candleway.Count - 1))
                    {
                        if (!touchcells.Contains(tline))
                        {
                            ProximityMethod(tline, tlines, nL, startpoint);

                            DynamicsPrevious(tline, tlines, nL, startpoint); // Ошибка 29.06.22 

                            touchcells.Add(tline);
                        }
                        else
                        {
                            TLine newtline = new(tline);

                            for (int x = touchcells.Count - 1; x >= 0; x--)
                            {
                                if (newtline.Id == touchcells[x].Id)
                                {
                                    touchcells[x].Way = touchcells[x].Way == 0 ? 1 : 3;
                                    break;
                                }
                            }

                            ProximityMethod(newtline, tlines, nL, startpoint);

                            touchcells.Add(newtline);
                        }
                    }
                }
            }
            touchcells.Add(tlines[^1]);

            // Проблема 1-ой линии (есть только следующая, а надо учесть "-" близость и "б/д" близость)

            foreach (TLine cell in touchcells)
            {
                if (cell.PreviousProximity[1] == null)
                    FirstLinePreviousProximity(1, cell, touchcells);

                if (cell.CommonType && cell.PreviousProximity[2] == null)
                    FirstLinePreviousProximity(2, cell, touchcells);

                double cellhis = cell.HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(cell.HistoryType.Split("С").Last());

                if (cellhis > 2 && cell.PreviousProximity[3] == null)
                    FirstLinePreviousProximity(3, cell, touchcells);
            }


            return touchcells;
        }


        private static bool Touch(string direction, TLine tline, Candle candle, double border, int indC, int indlastC)
        {
            if (direction is "Up" && tline.SecondPoint.Y < border && tline.SecondPoint.Y >= candle.MaxPoint.Y) return true;
            else if (direction is "Dn" && tline.SecondPoint.Y > border && tline.SecondPoint.Y <= candle.MinPoint.Y) return true;

            else if (direction is "Up" && indC == indlastC && tline.SecondPoint.Y < candle.MaxPoint.Y) return true;
            else if (direction is "Dn" && indC == indlastC && tline.SecondPoint.Y > candle.MinPoint.Y) return true;

            return false;
        }

        private static void ProximityMethod(TLine tline, List<TLine> tlines, int nL, double startpoint)
        {
            double proximity;

            // base
            proximity = ProximityCalculate(startpoint, tline.SecondPoint.Y, tlines[nL + 1].SecondPoint.Y);

            tline.NextProximity[0] = ProximityRepresentResult(proximity, tline.NextProximity[0]);
            tlines[nL + 1].PreviousProximity[0] = ProximityRepresentResult(proximity, tlines[nL + 1].PreviousProximity[0]);

            // MainType
            for (int n = nL + 1; n < tlines.Count; n++)
            {
                if (tline.MainType == tlines[n].MainType)
                {
                    proximity = ProximityCalculate(startpoint, tline.SecondPoint.Y, tlines[n].SecondPoint.Y);

                    tline.NextProximity[1] = ProximityRepresentResult(proximity, tline.NextProximity[1]);
                    tlines[n].PreviousProximity[1] = ProximityRepresentResult(proximity, tlines[n].PreviousProximity[1]);
                    break;
                }
            }

            // CommonType
            if (tline.CommonType)
            {
                for (int n = nL + 1; n < tlines.Count; n++)
                {
                    if (tlines[n].CommonType)
                    {
                        proximity = ProximityCalculate(startpoint, tline.SecondPoint.Y, tlines[n].SecondPoint.Y);

                        tline.NextProximity[2] = ProximityRepresentResult(proximity, tline.NextProximity[1]);
                        tlines[n].PreviousProximity[2] = ProximityRepresentResult(proximity, tlines[n].PreviousProximity[1]);
                        break;
                    }
                }
            }

            // HistoryType
            double tlhis = tline.HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(tline.HistoryType.Split("С").Last());

            if (tlhis > 2)
            {
                for (int n = nL + 1; n < tlines.Count; n++)
                {
                    double nexthis = tlines[n].HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(tlines[n].HistoryType.Split("С").Last());

                    if (nexthis > 2)
                    {
                        proximity = ProximityCalculate(startpoint, tline.SecondPoint.Y, tlines[n].SecondPoint.Y);

                        tline.NextProximity[3] = ProximityRepresentResult(proximity, tline.NextProximity[3]);
                        tlines[n].PreviousProximity[3] = ProximityRepresentResult(proximity, tlines[n].PreviousProximity[3]);
                        break;
                    }
                }
            }
        }

        private static void DynamicsPrevious(TLine tline, List<TLine> tlines, int nL, double startpoint)
        {
            double proximity;

            // base
            if (nL > 0 && tline.PreviousProximity[0] == null)
            {
                proximity = ProximityCalculate(startpoint, tlines[nL - 1].SecondPoint.Y, tline.SecondPoint.Y);

                tline.PreviousProximity[0] = ProximityRepresentResult(proximity, tline.PreviousProximity[0]);
            }

            // MainType
            if (tline.PreviousProximity[1] == null)
            {
                for (int n = nL - 1; n >= 0; n--)
                {
                    if (tline.MainType == tlines[n].MainType)
                    {
                        proximity = ProximityCalculate(startpoint, tlines[n].SecondPoint.Y, tline.SecondPoint.Y);

                        tline.PreviousProximity[1] = ProximityRepresentResult(proximity, tline.PreviousProximity[1]);
                        break;
                    }
                }
            }

            // CommonType
            if (tline.CommonType && tline.PreviousProximity[2] == null)
            {
                for (int n = nL - 1; n >= 0; n--)
                {
                    if (tlines[n].CommonType)
                    {
                        proximity = ProximityCalculate(startpoint, tlines[n].SecondPoint.Y, tline.SecondPoint.Y);

                        tline.PreviousProximity[2] = ProximityRepresentResult(proximity, tline.PreviousProximity[2]);
                        break;
                    }
                }
            }

            // HistoryType
            double cellhis = tline.HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(tline.HistoryType.Split("С").Last());

            if (cellhis > 2 && tline.PreviousProximity[3] == null)
            {
                for (int n = nL - 1; n >= 0; n--)
                {
                    double nexthis = tlines[n].HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(tlines[n].HistoryType.Split("С").Last());

                    if (nexthis > 2)
                    {
                        proximity = ProximityCalculate(startpoint, tlines[n].SecondPoint.Y, tline.SecondPoint.Y);

                        tline.PreviousProximity[3] = ProximityRepresentResult(proximity, tline.PreviousProximity[3]);
                        break;
                    }
                }
            }

        }

        private static double ProximityCalculate(double startpoint, double firsthPoint, double secondPoint)
        {
            return Math.Round(Math.Abs(firsthPoint - secondPoint) / Math.Abs(startpoint - firsthPoint) * 100, 2);
        }

        private static string ProximityRepresentResult(double proximity, string current)
        {
            string s = "";

            if (proximity <= 5 && current is null or "") s = "=";
            else if (proximity is > 5 and <= 11.7 && current is null or "") s = "-";
            else if (proximity is > 11.7 and <= 30 && current is null or "") s = "б";
            else if (proximity is > 30 && current is null or "") s = "д";

            else if (proximity is <= 11.7 && current is "б" or "д") s += ">-";
            else if (proximity is > 11.7 and <= 30 && current is "=" or "-" or "д") s += ">б";
            else if (proximity is > 30 && current is "=" or "-" or "б") s += ">д";

            return s;
        }

        private static void FirstLinePreviousProximity(int operation, TLine cell, List<TLine> cells)
        {
            int ind = cells.IndexOf(cell);

            if (operation is 1)
            {
                for (int n = ind + 1; n < cells.Count; n++)
                {
                    if (cells[n].MainType == cell.MainType && (cells[n].PreviousProximity[1].Contains('б') || cells[n].PreviousProximity[1].Contains('д')))
                    {
                        cell.PreviousProximity[1] = cells[n].PreviousProximity[1].Contains('б') ? "б" : "д";
                        break;
                    }
                }
            }
            else if (operation is 2)
            {
                for (int n = ind + 1; n < cells.Count; n++)
                {
                    if (cells[n].CommonType && (cells[n].PreviousProximity[2].Contains('б') || cells[n].PreviousProximity[2].Contains('д')))
                    {
                        cell.PreviousProximity[2] = cells[n].PreviousProximity[2].Contains('б') ? "б" : "д";
                        break;
                    }
                }
            }
            else if (operation is 3)
            {
                for (int n = ind + 1; n < cells.Count; n++)
                {
                    double his = cells[n].HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(cells[n].HistoryType.Split("С").Last());

                    if (his > 2 && (cells[n].PreviousProximity[3].Contains('б') || cells[n].PreviousProximity[3].Contains('д')))
                    {
                        cell.PreviousProximity[3] = cells[n].PreviousProximity[3].Contains('б') ? "б" : "д";
                        break;
                    }
                }
            }
        }

    }
}
