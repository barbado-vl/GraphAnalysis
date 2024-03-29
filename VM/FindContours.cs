﻿using System.Collections.Generic;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using GraphAnalysis.DataModel;

namespace GraphAnalysis.VM
{
    public static class FindContours
    {

        /// <summary> Main method of this class </summary>
        public static List<Candle> ContourToCandle(string filename)
        {
            VectorOfVectorOfPoint contours = new();
            List<Candle> candles = new();

            contours = TakeContours(filename, contours);
            contours = Filtr(contours);
            contours = SortLeftToRight(contours);

            for (int x = 0; x < contours.Size; x++)
            {
                VectorOfPoint contour = contours[x];
                Candle candle = new(contour);
                candle.id += x.ToString();
                candle.Contour.Uid = candle.id;
                candles.Add(candle);
            }

            return candles;
        }

        /// <summary> EmguCV FindContours </summary>
        private static VectorOfVectorOfPoint TakeContours(string filename, VectorOfVectorOfPoint contours)
        {
            Image<Bgr, byte> inputImage = new Image<Bgr, byte>(filename);
            Image<Gray, byte> outputImage = inputImage.Convert<Gray, byte>().ThresholdBinary(new Gray(100), new Gray(255));
            Mat hierarchy = new Mat();

            CvInvoke.FindContours(outputImage, contours, hierarchy, Emgu.CV.CvEnum.RetrType.Tree, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxNone);

            return contours;
        }

        /// <summary> Filtr </summary>
        private static VectorOfVectorOfPoint Filtr(VectorOfVectorOfPoint contours)
        {
            VectorOfVectorOfPoint temp_contours = new VectorOfVectorOfPoint();

            List<int> width = new List<int>();

            for (int x = 0; x < contours.Size; x++)
            {
                int XRight = 0;
                int XLeft = 100000;
                for (int n = 0; n < contours[x].Size; n++)
                {
                    if (XLeft > contours[x][n].X) { XLeft = contours[x][n].X; }
                    if (XRight < contours[x][n].X) { XRight = contours[x][n].X; }
                }
                if (XRight - XLeft > 0) { width.Add(XRight - XLeft); }
            }

            var widthCandle = width.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(grp => grp.Key).First();

            for (int x = 0; x < contours.Size; x++)
            {
                int XRight = 0;
                int XLeft = 100000;
                for (int n = 0; n < contours[x].Size; n++)
                {
                    if (XLeft > contours[x][n].X) { XLeft = contours[x][n].X; }
                    if (XRight < contours[x][n].X) { XRight = contours[x][n].X; }
                }
                if ((widthCandle == XRight - XLeft) || (widthCandle == XRight - XLeft - 1) || (widthCandle == XRight - XLeft + 1))
                { 
                    temp_contours.Push(contours[x]); 
                }

            }
            return temp_contours;
        }

        /// <summary> Sort left to rigth or by X-axis /// </summary>
        private static VectorOfVectorOfPoint SortLeftToRight(VectorOfVectorOfPoint contours)
        {
            List<int> firstPoints = new List<int>();
            VectorOfVectorOfPoint temp_contours = new VectorOfVectorOfPoint();

            for (int x = 0; x < contours.Size; x++)
            {
                firstPoints.Add(contours[x][0].X);
            }
            firstPoints.Sort();

            for (int n = 0; n < firstPoints.Count; n++)
            {
                if (n > 0) { if (firstPoints[n] == firstPoints[n - 1]) { continue; } }

                for (int x = 0; x < contours.Size; x++)
                {
                    if (contours[x][0].X == firstPoints[n])
                    {
                        temp_contours.Push(contours[x]);
                    }
                }
            }
            return temp_contours;
        }
    }
}
