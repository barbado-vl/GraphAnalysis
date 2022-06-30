
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GraphAnalysis.DataModel
{
    public class Peak
    {
        // Создание по CalculatePeaksMethods
        public Peak(int N, string direction, List<Candle> mass)
        {
            Direction = direction;
            K = false;
            Mass = mass.Count;
            Id = "peak_" + N.ToString() + "_" + Mass.ToString();
            Np = new();
            DTP = new();

            int indexTsp;
            (Tsp, indexTsp) = FindTsp(mass);

            CandlesId.Add(mass[indexTsp].id);
            CandlesId.Add(mass[0].id);

            if (Direction == "Up")
            {
                CutOffPoint = mass[0].MinPoint;
                TextPoint = CalculateTextPoint(mass[0].MinPoint, mass[^1].MinPoint);

                mass[0].CreateEllipse(mass[0].MinPoint);
                mass[indexTsp].CreateEllipse(mass[indexTsp].MaxPoint);
            }
            else
            {
                CutOffPoint = mass[0].MaxPoint;
                TextPoint = CalculateTextPoint(mass[0].MaxPoint, mass[^1].MaxPoint);

                mass[0].CreateEllipse(mass[0].MaxPoint);
                mass[indexTsp].CreateEllipse(mass[indexTsp].MinPoint);
            }
        }

        // Создание по выделенным свечкам по команде из контекстного меню
        public Peak(string type, List<Candle> targetcandles, int minsizepeak, List<Candle> allcandles)
        {
            targetcandles = targetcandles.OrderBy(a => a.MaxPoint.X).ToList();

            List<Candle> masscandles = new();
            for (int n = allcandles.IndexOf(targetcandles[0]); n <= allcandles.IndexOf(targetcandles[^1]); n++)
            {
                masscandles.Add(allcandles[n]);
            }

            // Проверяем и определяем CutOffPoint и FallPoint
            if (type is not "K")
            {
                Direction = type;
                K = false;

                SetCutOffPointAndFallPointByHand(targetcandles, minsizepeak, allcandles);
            }
            else // type is "K"
            {
                K = true;

                if (targetcandles[0].MaxPoint.Y > targetcandles[^1].MaxPoint.Y && targetcandles[0].MinPoint.Y > targetcandles[^1].MinPoint.Y) Direction = "Dn";
                else if (targetcandles[0].MaxPoint.Y < targetcandles[^1].MaxPoint.Y && targetcandles[0].MinPoint.Y < targetcandles[^1].MinPoint.Y) Direction = "Up";
                else K = false;

                if (K) SetCutOffPointAndFallPointByHand(targetcandles, minsizepeak, allcandles); ;
            }

            // Проверяем и определяем Tsp
            if (CutOffPoint.Y != 0 && FallPoint.Y != 0)
            {
                Point temppoint;
                (temppoint, _) = FindTsp(masscandles);

                for (int n = targetcandles.Count - 1; n > 0; n--)
                {
                    if (targetcandles[n].MaxPoint == temppoint || targetcandles[n].MinPoint == temppoint)
                    {
                        Tsp = temppoint;
                        CandlesId.Add(targetcandles[n].id);

                        if (Direction is "Up") targetcandles[n].CreateEllipse(targetcandles[n].MaxPoint);
                        else targetcandles[n].CreateEllipse(targetcandles[n].MinPoint);

                        targetcandles.Remove(targetcandles[n]);
                        break;
                    }
                }
            }

            // Контрольная проверка, определение Np и DTP, другие параметры
            if (CutOffPoint.Y != 0 && FallPoint.Y != 0 && Tsp.Y != 0)
            {
                Np = new();
                DTP = new();

                // Проверить и определить НП
                SetNp(targetcandles);

                // Проверить и определить ДТП
                for (int n = targetcandles.Count - 2; n > 0; n--)
                {
                    if (AdePoint(targetcandles[n], "dtp"))
                        targetcandles.Remove(targetcandles[n]);
                }
                targetcandles.Remove(targetcandles[^1]);
                targetcandles.Remove(targetcandles[0]);

                // Расчет Mass, Textpoint и Id

                if (targetcandles.Count == 0)
                {
                    (Mass, TextPoint) = ByHandMassAndTextPoint(masscandles);
                }
            }
        }


        public string Direction { get; }
        public bool K { get; set; }

        public int Mass { get; set; }
        public Point TextPoint { get; set; }
        public string Id { get; set; }

        public Point CutOffPoint { get; set; }
        public Point Tsp { get; set; }
        public Point FallPoint { get; set; }

        public List<Point> Np;
        public List<Point> DTP;

        public List<string> CandlesId = new();


        private Point CalculateTextPoint(Point start, Point wall)
        {
            Point textpoint = new();

            double a, b;
            if (start.X > wall.X) { a = start.X; b = wall.X; }
            else { a = wall.X; b = start.X; }

            textpoint.Y = Direction == "Up" ? start.Y : start.Y - 20;
            textpoint.X = a - ((a - b) / 2);   // при "В лево" от меньшего отнимает ... назад уходим

            return textpoint;
        }

        internal (Point, int) FindTsp(List<Candle> candles)
        {
            Candle tsp;
            int x;

            if (Direction == "Up")
            {
                tsp = candles[1];
                x = 1;
                for (int n = 2; n < candles.Count - 1; n++)
                {
                    if (candles[n].MaxPoint.Y < tsp.MaxPoint.Y) { tsp = candles[n]; x = n; }
                }
                return (tsp.MaxPoint, x);
            }
            else
            {
                tsp = candles[1];
                x = 1;
                for (int n = 2; n < candles.Count - 1; n++)
                {
                    if (candles[n].MinPoint.Y > tsp.MinPoint.Y) { tsp = candles[n]; x = n; }
                }
                return (tsp.MinPoint, x);
            }
        }

        private bool CheckToBifurcationPoint(Candle candle, string location, List<Candle> allcandles, int minsizebif)
        {
            minsizebif++;
            int n;
            int left = 0;
            int right = 0;

            if (Direction is "Up")
            {
                n = allcandles.IndexOf(allcandles.First(a => a.MinPoint == candle.MinPoint));

                while (left < minsizebif && n - left >= 0 && candle.MinPoint.Y >= allcandles[n - left].MinPoint.Y) { left++; }
                while (right < minsizebif && n + right < allcandles.Count && candle.MinPoint.Y >= allcandles[n + right].MinPoint.Y) { right++; }
            }
            else // Direction is "Dn"
            {
                n = allcandles.IndexOf(allcandles.First(a => a.MaxPoint == candle.MaxPoint));

                while (left < minsizebif && n - left >= 0 && candle.MaxPoint.Y <= allcandles[n - left].MaxPoint.Y) { left++; }
                while (right < minsizebif && n + right < allcandles.Count && candle.MaxPoint.Y <= allcandles[n + right].MaxPoint.Y) { right++; }
            }

            if (location is "left") return left == minsizebif || n - left < 0;
            else return right == minsizebif || n + right == allcandles.Count;
        }

        private void SetNp(List<Candle> targetcandles)
        {
            double hill = CutOffPoint.Y;

            for (int n = 1; n < targetcandles.Count - 1; n++)
            {
                if (Direction is "Up" && targetcandles[n].MinPoint.Y > CutOffPoint.Y)
                {
                    double np_check = (targetcandles[n].MinPoint.Y - Tsp.Y) * 0.117;

                    if (targetcandles[n].MinPoint.Y - hill <= np_check)
                    {
                        Np.Add(targetcandles[n].MinPoint);
                        CandlesId.Add(targetcandles[n].id);

                        targetcandles[n].CreateEllipse(targetcandles[n].MinPoint);

                        hill = targetcandles[n].MinPoint.Y;
                    }
                }
                else if (Direction is "Dn" && targetcandles[n].MaxPoint.Y < CutOffPoint.Y)
                {
                    double np_check = (Tsp.Y - targetcandles[n].MaxPoint.Y) * 0.117;

                    if (hill - targetcandles[n].MaxPoint.Y <= np_check)
                    {
                        Np.Add(targetcandles[n].MaxPoint);
                        CandlesId.Add(targetcandles[n].id);

                        targetcandles[n].CreateEllipse(targetcandles[n].MaxPoint);

                        hill = targetcandles[n].MaxPoint.Y;
                    }
                }
            }

            for (int n = targetcandles.Count - 2; n > 0; n--)
            {
                if (Np.Contains(targetcandles[n].MaxPoint) || Np.Contains(targetcandles[n].MinPoint))
                    targetcandles.Remove(targetcandles[n]);
            }
        }

        private (int, Point) ByHandMassAndTextPoint(List<Candle> candles)
        {
            int mass = 1;
            Point textpoint;

            int n = CutOffPoint.X < FallPoint.X ? 0 : candles.Count - 1;

            if (Direction is "Up")
            {
                while (candles[n].MinPoint.Y <= CutOffPoint.Y && candles[n].MinPoint.X != FallPoint.X)
                {
                    mass++;
                    n = CutOffPoint.X < FallPoint.X ? n + 1 : n - 1;
                }

                for (int np = 0; np < Np.Count; np++)
                {
                    while (candles[n].MinPoint.Y <= Np[np].Y && candles[n].MinPoint.X != FallPoint.X)
                    {
                        mass++;
                        n = CutOffPoint.X < FallPoint.X ? n + 1 : n - 1;
                    }
                }

                textpoint = CalculateTextPoint(CutOffPoint, candles[n].MinPoint);
            }
            else // Direction is "Dn"
            {
                while (candles[n].MaxPoint.Y >= CutOffPoint.Y && candles[n].MaxPoint.X != FallPoint.X)
                {
                    mass++;
                    n = CutOffPoint.X < FallPoint.X ? n + 1 : n - 1;
                }

                for (int np = 0; np < Np.Count; np++)
                {
                    while (candles[n].MaxPoint.Y >= Np[np].Y && candles[n].MaxPoint.X != FallPoint.X)
                    {
                        mass++;
                        n = CutOffPoint.X < FallPoint.X ? n + 1 : n - 1;
                    }
                }

                textpoint = CalculateTextPoint(CutOffPoint, candles[n].MaxPoint);
            }

            return (mass, textpoint);
        }

        private void SetCutOffPointAndFallPointByHand(List<Candle> targetcandles, int minsizepeak, List<Candle> allcandles)
        {
            if (!K)
            {
                if (CheckToBifurcationPoint(targetcandles[0], "left", allcandles, minsizepeak) &&
                    CheckToBifurcationPoint(targetcandles[^1], "right", allcandles, minsizepeak))
                {
                    if (Direction is "Up")
                    {
                        if (targetcandles[0].MinPoint.Y > targetcandles[^1].MinPoint.Y)
                        {
                            CutOffPoint = targetcandles[^1].MinPoint;
                            FallPoint = targetcandles[0].MinPoint;
                        }
                        else
                        {
                            CutOffPoint = targetcandles[0].MinPoint;
                            FallPoint = targetcandles[^1].MinPoint;
                        }
                        targetcandles[0].CreateEllipse(targetcandles[0].MinPoint);
                        targetcandles[^1].CreateEllipse(targetcandles[^1].MinPoint);

                        CandlesId.Add(targetcandles[0].id);
                        CandlesId.Add(targetcandles[^1].id);
                    }
                    else // Direction is "Dn"
                    {
                        if (targetcandles[0].MaxPoint.Y < targetcandles[^1].MaxPoint.Y)
                        {
                            CutOffPoint = targetcandles[^1].MaxPoint;
                            FallPoint = targetcandles[0].MaxPoint;
                        }
                        else
                        {
                            CutOffPoint = targetcandles[0].MaxPoint;
                            FallPoint = targetcandles[^1].MaxPoint;
                        }
                        targetcandles[0].CreateEllipse(targetcandles[0].MaxPoint);
                        targetcandles[^1].CreateEllipse(targetcandles[^1].MaxPoint);

                        CandlesId.Add(targetcandles[0].id);
                        CandlesId.Add(targetcandles[^1].id);
                    }
                }
            }
            else
            {
                if (Direction is "Up")
                {
                    CutOffPoint = targetcandles[0].MinPoint;
                    targetcandles[0].CreateEllipse(targetcandles[0].MinPoint);
                    CandlesId.Add(targetcandles[0].id);

                    if (CheckToBifurcationPoint(targetcandles[^1], "right", allcandles, minsizepeak) &&
                        CheckToBifurcationPoint(targetcandles[^1], "left", allcandles, minsizepeak))
                    {
                        FallPoint = targetcandles[^1].MinPoint;
                        targetcandles[^1].CreateEllipse(targetcandles[^1].MinPoint);
                        CandlesId.Add(targetcandles[^1].id);
                    }
                }
                else // Direction is "Dn"
                {
                    CutOffPoint = targetcandles[0].MaxPoint;
                    targetcandles[0].CreateEllipse(targetcandles[0].MaxPoint);
                    CandlesId.Add(targetcandles[0].id);

                    if (CheckToBifurcationPoint(targetcandles[^1], "right", allcandles, minsizepeak) &&
                        CheckToBifurcationPoint(targetcandles[^1], "left", allcandles, minsizepeak))
                    {
                        FallPoint = targetcandles[^1].MaxPoint;
                        targetcandles[^1].CreateEllipse(targetcandles[^1].MaxPoint);
                        CandlesId.Add(targetcandles[^1].id);
                    }
                }
            }
        }


        internal bool DeletePoint(Candle candle, string extremum)
        {
            if (extremum is "max" && Direction is "Up")
            {
                if (DTP.Contains(candle.MaxPoint) || (Tsp == candle.MaxPoint && FallPoint.X != 0))
                {
                    if (DTP.Contains(candle.MaxPoint)) DTP.Remove(candle.MaxPoint);
                    if (Tsp == candle.MaxPoint) Tsp = new(0, 0);

                    CandlesId.Remove(candle.id);

                    return true;
                }
            }
            else if (extremum is "min" && Direction is "Up")
            {
                if (FallPoint == candle.MinPoint || DTP.Contains(candle.MinPoint))
                {
                    if (FallPoint == candle.MinPoint) FallPoint = new(0, 0);
                    if (DTP.Contains(candle.MinPoint)) DTP.Remove(candle.MinPoint);

                    CandlesId.Remove(candle.id);

                    return true;
                }
            }

            else if (extremum is "min" && Direction is "Dn")
            {
                if (DTP.Contains(candle.MinPoint) || (Tsp == candle.MinPoint && FallPoint.X != 0))
                {
                    if (DTP.Contains(candle.MinPoint)) DTP.Remove(candle.MinPoint);
                    if (Tsp == candle.MinPoint) Tsp = new(0, 0);

                    CandlesId.Remove(candle.id);

                    return true;
                }
            }
            else if (extremum is "max" && Direction is "Dn")
            {
                if (FallPoint == candle.MaxPoint || DTP.Contains(candle.MaxPoint))
                {
                    if (FallPoint == candle.MaxPoint) FallPoint = new(0, 0);
                    if (DTP.Contains(candle.MaxPoint)) DTP.Remove(candle.MaxPoint);

                    CandlesId.Remove(candle.id);

                    return true;
                }
            }

            return false;
        }

        internal bool AdePoint(Candle candle, string extremum)
        {
            if (extremum is "dtp" && Direction is "Up")
            {
                if ((candle.MaxPoint.X != Tsp.X && candle.MaxPoint.Y == Tsp.Y) ||
                         (candle.MaxPoint.Y < Tsp.Y && (candle.MinPoint.Y == CutOffPoint.Y || candle.MinPoint.Y == FallPoint.Y)))
                {
                    DTP.Add(candle.MaxPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMax.ellipse == null) candle.CreateEllipse(candle.MaxPoint);

                    return true;
                }
                else if (candle.MinPoint.Y == CutOffPoint.Y || candle.MinPoint.Y == FallPoint.Y)
                {
                    DTP.Add(candle.MinPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMin.ellipse == null) candle.CreateEllipse(candle.MinPoint);

                    return true;
                }
            }
            else if (extremum is "dtp" && Direction is "Dn")
            {
                if ((candle.MinPoint.X != Tsp.X && candle.MinPoint.Y == Tsp.Y) ||
                         (candle.MinPoint.Y > Tsp.Y && (candle.MaxPoint.Y == CutOffPoint.Y || candle.MaxPoint.Y == FallPoint.Y)))
                {
                    DTP.Add(candle.MinPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMin.ellipse == null) candle.CreateEllipse(candle.MinPoint);

                    return true;
                }
                else if (candle.MaxPoint.Y == CutOffPoint.Y || candle.MaxPoint.Y == FallPoint.Y)
                {
                    DTP.Add(candle.MaxPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMax.ellipse == null) candle.CreateEllipse(candle.MaxPoint);

                    return true;
                }
            }

            else if (extremum is "max" && Direction is "Up")
            {
                if (Tsp.X == 0 && candle.MaxPoint.Y < CutOffPoint.Y && candle.MaxPoint.Y < FallPoint.Y)
                {
                    Tsp = candle.MaxPoint;

                    CandlesId.Add(candle.id);

                    if (candle.ViewMax.ellipse == null) candle.CreateEllipse(candle.MaxPoint);

                    return true;
                }
                else if (!DTP.Contains(candle.MaxPoint) &&
                        ((candle.MaxPoint.X != Tsp.X && candle.MaxPoint.Y == Tsp.Y) ||
                         (candle.MaxPoint.Y < Tsp.Y && (candle.MinPoint.Y == CutOffPoint.Y || candle.MinPoint.Y == FallPoint.Y))))
                {
                    DTP.Add(candle.MaxPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMax.ellipse == null) candle.CreateEllipse(candle.MaxPoint);

                    return true;
                }
            }
            else if (extremum is "min" && Direction is "Up")
            {
                if (FallPoint.Y == 0 && candle.MinPoint.Y >= CutOffPoint.Y)
                {
                    FallPoint = candle.MinPoint;

                    CandlesId.Add(candle.id);

                    if (candle.ViewMin.ellipse == null) candle.CreateEllipse(candle.MinPoint);

                    return true;
                }
                else if (!DTP.Contains(candle.MinPoint) &&
                        ((candle.MinPoint.X != CutOffPoint.X && candle.MinPoint.Y == CutOffPoint.Y) ||
                         (candle.MinPoint.X != FallPoint.X && candle.MinPoint.Y == FallPoint.Y)))
                {
                    DTP.Add(candle.MinPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMin.ellipse == null) candle.CreateEllipse(candle.MinPoint);

                    return true;
                }
            }

            else if (extremum is "min" && Direction is "Dn")
            {
                if (Tsp.X == 0 && candle.MinPoint.Y > CutOffPoint.Y && candle.MinPoint.Y > FallPoint.Y)
                {
                    Tsp = candle.MinPoint;

                    CandlesId.Add(candle.id);

                    if (candle.ViewMin.ellipse == null) candle.CreateEllipse(candle.MinPoint);

                    return true;
                }
                else if (!DTP.Contains(candle.MinPoint) && ((candle.MinPoint.X != Tsp.X && candle.MinPoint.Y == Tsp.Y) ||
                         (candle.MinPoint.Y > Tsp.Y && (candle.MaxPoint.Y == CutOffPoint.Y || candle.MaxPoint.Y == FallPoint.Y))))
                {
                    DTP.Add(candle.MinPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMin.ellipse == null) candle.CreateEllipse(candle.MinPoint);

                    return true;
                }
            }
            else if (extremum is "max" && Direction is "Dn")
            {
                if (FallPoint.Y == 0 && candle.MaxPoint.Y <= CutOffPoint.Y)
                {
                    FallPoint = candle.MaxPoint;

                    CandlesId.Add(candle.id);

                    if (candle.ViewMax.ellipse == null) candle.CreateEllipse(candle.MaxPoint);

                    return true;
                }
                else if (!DTP.Contains(candle.MaxPoint) &&
                        ((candle.MaxPoint.X != CutOffPoint.X && candle.MaxPoint.Y == CutOffPoint.Y) ||
                         (candle.MaxPoint.X != FallPoint.X && candle.MaxPoint.Y == FallPoint.Y)))
                {
                    DTP.Add(candle.MaxPoint);
                    CandlesId.Add(candle.id);

                    if (candle.ViewMax.ellipse == null) candle.CreateEllipse(candle.MaxPoint);

                    return true;
                }
            }

            return false;
        }
    }
}
