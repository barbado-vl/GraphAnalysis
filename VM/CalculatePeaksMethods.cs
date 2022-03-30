using GraphAnalysis.DataModel;
using System.Collections.Generic;
using System.Linq;

namespace GraphAnalysis.VM
{
    public class CalculatePeaks
    {
        public List<Peak> Peaks = new();
        public int N;                                     // НАДО передавать из VM

        /// <summary> Ищем точки бифуркации
        /// Собираем массу (свечки) вокруг точки бифуркации (выбраной свечки) 
        /// Смотрим когда Max меньше чем у выбраной свечки, Min больше, в лево и в право
        /// Операторы сравнения взяты с учетом того, что сравниваем точки координат Canvas
        /// 4 цикла -- в ПРАВО по Max и Min, потом в ЛЕВО по Max и Min /// </summary>
        public CalculatePeaks(List<Candle> candles, int i)
        {
            N = i;

            List<Candle> UpRightBranch = new();
            List<Candle> UpLeftBranch = new();
            List<Candle> DownRightBranch = new();
            List<Candle> DownLeftBranch = new();

            for (int n = N - 1; n < candles.Count; n++)
            {
                UpRightBranch.Clear(); UpLeftBranch.Clear(); DownRightBranch.Clear(); DownLeftBranch.Clear();
                UpRightBranch.Add(candles[n]); UpLeftBranch.Add(candles[n]); DownRightBranch.Add(candles[n]); DownLeftBranch.Add(candles[n]);

                for (int x = n + 1; x < candles.Count; x++)
                {
                    if (candles[n].MaxPoint.Y <= candles[x].MaxPoint.Y) { UpRightBranch.Add(candles[x]); }
                    else { UpRightBranch.Add(candles[x]); break; }
                }
                for (int x = n + 1; x < candles.Count; x++)
                {
                    if (candles[n].MinPoint.Y >= candles[x].MinPoint.Y) { DownRightBranch.Add(candles[x]); }
                    else { DownRightBranch.Add(candles[x]); break; }
                }
                for (int x = n - 1; x >= 0; x--)
                {
                    if (candles[n].MaxPoint.Y <= candles[x].MaxPoint.Y) { UpLeftBranch.Add(candles[x]); }
                    else { UpLeftBranch.Add(candles[x]); break; }
                }
                for (int x = n - 1; x >= 0; x--)
                {
                    if (candles[n].MinPoint.Y >= candles[x].MinPoint.Y) { DownLeftBranch.Add(candles[x]); }
                    else { DownLeftBranch.Add(candles[x]); break; }
                }

                /// <summary> Условия фильтра для точки бифуркации UP и DOWN  /// </summary>
                if (UpRightBranch.Count >= N && UpLeftBranch.Count >= N)
                {
                    ConditionByCreateDownPeak(UpRightBranch, UpLeftBranch, candles[n]);
                    ConditionByCreateDownPeak(UpLeftBranch, UpRightBranch, candles[n]);
                }
                else if (UpLeftBranch.Count >= N && UpRightBranch[^1] == candles[^1] &&
                         (UpRightBranch.Count == 1 || UpRightBranch[^1].MaxPoint.Y > UpRightBranch[^2].MaxPoint.Y))
                {
                    UpRightBranch.Clear();
                    ConditionByCreateDownPeak(UpLeftBranch, UpRightBranch, candles[n]);
                }

                if (DownRightBranch.Count >= N && DownLeftBranch.Count >= N)
                {
                    ConditionByCreateUpPeak(DownRightBranch, DownLeftBranch, candles[n]);
                    ConditionByCreateUpPeak(DownLeftBranch, DownRightBranch, candles[n]);
                }
                else if (DownLeftBranch.Count >= N && DownRightBranch[^1] == candles[^1] &&
                         (DownRightBranch.Count == 1 || DownRightBranch[^1].MinPoint.Y < DownRightBranch[^2].MinPoint.Y))
                {
                    DownRightBranch.Clear();
                    ConditionByCreateUpPeak(DownLeftBranch, DownRightBranch, candles[n]);
                }
            }
        }


        private void ConditionByCreateDownPeak(List<Candle> firstbranch, List<Candle> secondtbranch, Candle currentcandle)
        {
            YQualsY(firstbranch, "down");

            if (firstbranch[^1].MaxPoint.Y <= currentcandle.MaxPoint.Y &&
                (firstbranch.Count < secondtbranch.Count * 2 || secondtbranch.Count == 0))
            {
                Peaks.Add(new Peak("down", firstbranch));
            }
        }
        private void ConditionByCreateUpPeak(List<Candle> firstbranch, List<Candle> secondtbranch, Candle currentcandle)
        {
            YQualsY(firstbranch, "up");

            if (firstbranch[^1].MinPoint.Y >= currentcandle.MinPoint.Y &&
                (firstbranch.Count < secondtbranch.Count * 2 || secondtbranch.Count == 0))
            {
                Peaks.Add(new Peak("up", firstbranch));
            }
        }


        /// <summary> Проверка на Y = Y так это тоже точки отсекающие массу
        /// Разделяем на направления, т.к. надо обращаться к Max или Min точкам и выбирать оператор сравнения < или >
        /// Собираем индексы у списка свечек, для которых выполняется условие "Y=Y", включая первую, как дающую 1-ый Y, плюс крайняя со свои условем Y крайней
        /// Перебираем индексы свечек и проверяем условие на >=N ... Первую часть условия вынес в условие сбора индексов свечек. Втоая часть далее.
        /// Если условие второй части >=N выполняется -- отбираем свечки внутри диапазона индексов в новый список и создаем пик. </summary>
        private void YQualsY(List<Candle> tempcandles, string direction)
        {
            List<int> numbers = new() { 0 };

            if (direction == "down") // как у пика
            {
                for (int n = N - 1; n < tempcandles.Count; n++)
                {
                    if (tempcandles[0].MaxPoint.Y == tempcandles[n].MaxPoint.Y) { numbers.Add(n); }
                }
                if (tempcandles[0].MaxPoint.Y > tempcandles[^1].MaxPoint.Y && numbers.Count > 1) { numbers.Add(tempcandles.Count - 1); }

            }
            else
            {
                for (int n = N - 1; n < tempcandles.Count; n++)
                {
                    if (tempcandles[0].MinPoint.Y == tempcandles[n].MinPoint.Y) { numbers.Add(n); }
                }
                if (tempcandles[0].MinPoint.Y < tempcandles[^1].MinPoint.Y && numbers.Count > 1) { numbers.Add(tempcandles.Count - 1); }
            }

            if (numbers.Count > 1)
            {
                for (int x = 1; x < numbers.Count - 1; x++)
                {
                    if (numbers[x + 1] - numbers[x] >= N && numbers[x] - numbers[x - 1] >= N)
                    {
                        List<Candle> candles = new();
                        for (int i = numbers[x]; i <= numbers[x + 1]; i++)
                        {
                            candles.Add(tempcandles[i]);
                        }
                        Peaks.Add(new Peak(direction, candles));
                    }
                }
            }
        }
    }

}
