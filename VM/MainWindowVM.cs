using GraphAnalysis.DataModel;
using GraphAnalysis.Infrastructure.Commands;

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GraphAnalysis.VM
{
    internal class MainWindowVM : BaseViewModel
    {
        #region ПАРАМЕТРЫ окна

        /// <summary> Заголовок окна </summary>
        private string _Title = "GraphAnalysis";
        public string Title
        {
            get => _Title;
            //set                                       развернутый вариант реализации метода Set из базового класса
            //{
            //    //if (Equals(_Title, value)) return;
            //    //_Title = value;
            //    //OnPropertyChanged();
            //    Set(ref _Title, value);              свернутый вариант
            //}
            set => Set(ref _Title, value);             // самый сокращенный -- через анонимный метод
        }

        private string _StatusMessage;
        public string StatusMessage
        {
            get => _StatusMessage;
            set
            {
                _StatusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
        #endregion

        #region ПАРАМЕТРЫ VM

        private readonly MainWindow MW;

        /// <summary> Имя/путь загруженного изображения по OpenNewCommand /// </summary>
        private string ImgFilename;

        /// <summary> Переменная изображения по OpenNewCommand /// </summary>
        private ImageSource _BGImage;
        public ImageSource BGImage
        {
            get => _BGImage;
            set
            {
                _BGImage = value;
                OnPropertyChanged(nameof(BGImage));
            }
        }

        /// <summary> Ширина и Высота для Холста по OpenNewCommand /// </summary>
        private double _WidthCanvas;
        public double WidthCanvas
        {
            get => _WidthCanvas;
            set
            {
                _WidthCanvas = value;
                OnPropertyChanged(nameof(WidthCanvas));
            }
        }

        private double _HeightCanvas;
        public double HeightCanvas
        {
            get { return _HeightCanvas; }
            set
            {
                _HeightCanvas = value;
                OnPropertyChanged(nameof(HeightCanvas));
            }
        }

        /// <summary> Параметры настраиваемые в ручную /// </summary>
        private int _MinSizePeak = 9;
        public int MinSizePeak
        {
            get => _MinSizePeak;
            set
            {
                _MinSizePeak = value;
                OnPropertyChanged(nameof(MinSizePeak));
            }
        }

        private string _Direction = "--";
        public string Direction
        {
            get => _Direction;
            set
            {
                _Direction = value;
                OnPropertyChanged(nameof(Direction));
            }
        }

        private string _InputLine = "--------";
        public string InputLine
        {
            get => _InputLine;
            set
            {
                _InputLine = value;
                OnPropertyChanged(nameof(InputLine));
            }
        }

        /// <summary> Параметры обеспечивающие функцию выделения объектов /// </summary>
        public List<UIElement> SelectedObject;

        internal Point PointStart = new();

        #endregion

        #region ПАРАМЕТРЫ логической модели

        /// <summary> Списки данных /// </summary>
        public List<Candle> Candles = new();
        public List<Peak> Peaks = new();
        public List<Peak> SeriesPeaks = new();
        public List<TLine> TLines = new();

        public (Point BD, Peak P) Breakdown;
        public double Zone1314 = 0;

        #endregion


        #region КОМАНДЫ и Методы основной логики + VM

        private void ClearData(string sender)
        {
            if (sender is "newimage" or "newcontours")
            {
                MW.myCanvas.Children.Clear();

                Candles.Clear();
                Peaks.Clear();
                TLines.Clear();
                SeriesPeaks.Clear();
                SelectedObject.Clear();

                Breakdown = new(new(0, 0), null);
            }
            else if (sender is "newpeaks")
            {
                if (SelectedObject.Any())
                {
                    for (int n = SelectedObject.Count - 1; n >= 0; n--)
                    {
                        if (SelectedObject[n].GetType() == typeof(TextBlock))
                        {
                            TextBlock text = (TextBlock)SelectedObject[n];
                            if (text.Uid.Split("_").First() is "peak") SelectedObject.Remove(SelectedObject[n]);
                        }
                        else if (SelectedObject[n].GetType() == typeof(Line))
                        {
                            Line line = (Line)SelectedObject[n];
                            if (line.Uid.Split("_").First() is "line") SelectedObject.Remove(SelectedObject[n]);
                        }
                    }
                }

                for (int n = MW.myCanvas.Children.Count - 1; n >= 0; n--)
                {
                    if (MW.myCanvas.Children[n].GetType() == typeof(TextBlock) &&
                        MW.myCanvas.Children[n].Uid.Split("_").First() is "peak")
                    {
                        MW.myCanvas.Children.Remove(MW.myCanvas.Children[n]);
                    }
                    else if (MW.myCanvas.Children[n].GetType() == typeof(Line) &&
                        MW.myCanvas.Children[n].Uid.Split("_").First() is "line")
                    {
                        MW.myCanvas.Children.Remove(MW.myCanvas.Children[n]);
                    }
                }

                Peaks.Clear();
                SeriesPeaks.Clear();
                TLines.Clear();
            }
            else if (sender is "newlines")
            {
                if (SelectedObject.Any())
                {
                    for (int n = SelectedObject.Count - 1; n >= 0; n--)
                    {
                        if (SelectedObject[n].GetType() == typeof(Line))
                        {
                            Line line = (Line)SelectedObject[n];
                            if (line.Uid.Split("_").First() is "line") SelectedObject.Remove(SelectedObject[n]);
                        }
                    }
                }

                for (int n = MW.myCanvas.Children.Count - 1; n >= 0; n--)
                {
                    if (MW.myCanvas.Children[n].GetType() == typeof(Line) &&
                        MW.myCanvas.Children[n].Uid.Split("_").First() is "line")
                    {
                        MW.myCanvas.Children.Remove(MW.myCanvas.Children[n]);
                    }
                }

                TLines.Clear();
            }

            StatusMessage = "";

            MW.brdrOne.Reset();
        }

        public Command ResetZoomBorderCommand { get; }
        private bool CanResetZoomBorderCommandExecute(object p) => true;
        private void OnResetZoomBorderCommandExecuted(object p)
        {
            MW.brdrOne.Reset();
        }

        /// <summary> Новое изображение </summary>

        public Command OpenNewImageCommand { get; }
        private bool CanOpenNewImageCommandExecute(object p) => true;
        private void OnOpenNewImageCommandExecuted(object p)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image files (*.png;*.jpg; *.bmp)|*.png;*.jpg;*.bmp|All files (*.*)|*.*";
                openFileDialog.InitialDirectory = @"c:\temp\";

                if (openFileDialog.ShowDialog() == true)
                {
                    ClearData("newimage");

                    ImgFilename = openFileDialog.FileName;
                    BGImage = new BitmapImage(new Uri(openFileDialog.FileName));

                    WidthCanvas = BGImage.Width;
                    HeightCanvas = BGImage.Height;
                }
                else
                {
                    MessageBox.Show("Incorrect action, try again please", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Command PasteCtrlVCommand { get; }
        private bool CanPasteCtrlVCommandExecute(object p) => true;
        private void OnPasteCtrlVCommandExecuted(object p)
        {
            if (Clipboard.ContainsImage())
            {
                ClearData("newimage");

                BitmapSource image = Clipboard.GetImage();
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(image));
                using FileStream filestream = new FileStream("temp.jpg", FileMode.Create);
                encoder.Save(filestream);
                filestream.Close();

                BitmapImage bi = new BitmapImage();
                using (var stream = new FileStream("temp.jpg", FileMode.Open))
                {
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = stream;
                    bi.EndInit();
                }

                ImgFilename = "temp.jpg";
                BGImage = bi;

                WidthCanvas = BGImage.Width;
                HeightCanvas = BGImage.Height;
            }

        }


        /// <summary> Расчеты </summary>

        public Command FindContoursCommand { get; }
        private bool CanFindContoursCommandExecute(object p) => true;
        private void OnFindContoursCommandExecuted(object p)
        {
            if (ImgFilename != null)
            {
                ClearData("newcontours");

                Candles = FindContours.ContourToCandle(ImgFilename);

                for (int x = 0; x < Candles.Count; x++)
                {
                    Candles[x].Contour.MouseLeftButtonDown += SelectChild;
                    MW.myCanvas.Children.Add(Candles[x].Contour);
                }
                MW.checkBox_CandleAll.IsChecked = true;
            }
            else
            {
                MessageBox.Show("Open image file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Command CalculatePeaksCommand { get; }
        private bool CanCalculatePeaksCommandExecute(object p) => true;
        private void OnCalculatePeaksCommandExecuted(object p)
        {
            if (Candles.Any())
            {
                ClearData("newpeaks");

                CalculatePeaks calculatePeaks = new(Peaks, Candles, MinSizePeak);

                MW.checkBox_Peaks.IsChecked = true;


                // Скрываем свечки

                MW.checkBox_CandleAll.IsChecked = false;

                for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                {
                    if (MW.myCanvas.Children[x].GetType() == typeof(Polygon))
                    {
                        Polygon polygon = (Polygon)MW.myCanvas.Children[x];

                        polygon.StrokeThickness = 0;
                    }
                }


                // Выводим пики

                foreach (Peak peak in Peaks)
                {
                    TextBlock Text = new();
                    Text.Uid = peak.Id;
                    Text.Text = peak.Mass.ToString();
                    Text.Foreground = Brushes.Red;
                    Text.MouseLeftButtonDown += SelectChild;

                    Canvas.SetTop(Text, peak.TextPoint.Y);
                    Canvas.SetLeft(Text, peak.TextPoint.X);

                    MW.myCanvas.Children.Add(Text);
                }
            }
            else
            {
                MessageBox.Show("Use Find Contours to get Candles befor calculate Peaks", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        public Command SeriesPeaksCommand { get; }
        private bool CanSeriesPeaksCommandExecute(object p) => true;
        private void OnSeriesPeaksCommandExecuted(object p)
        {
            if (SelectedObject.Any() && SelectedObject[0].GetType() == typeof(TextBlock))
            {
                TextBlock textblock = (TextBlock)SelectedObject[0];

                if (textblock.Uid.Split("_").First() == "peak")
                {
                    List<Peak> listpeaks = new();

                    foreach (TextBlock item in SelectedObject)
                    {
                        listpeaks.Add(Peaks.First(a => a.Id == item.Uid));
                    }

                    // Проверка
                    bool checkpeak = true;
                    foreach (Peak target in listpeaks)
                    {
                        if (checkpeak)
                        {
                            foreach (Peak peak in listpeaks)
                            {
                                if (target != peak && target.Tsp == peak.Tsp) checkpeak = false; break;
                            }
                            if (target.Tsp.X == 0) checkpeak = false; break;
                        }
                        else break;
                    }

                    if (checkpeak)
                    {
                        SeriesPeaks.Clear();
                        SeriesPeaks = listpeaks.OrderBy(a => a.Tsp.X).ToList();

                        // Расчет третьей точки пика

                        CalculateSeriesPeaks.ThirdPointOfPeak(SeriesPeaks, Candles);
                        CalculateSeriesPeaks.ViewPointOfSeriesPeaks(true, SeriesPeaks, Candles);

                        // Визуализация и снятие выделения с пиков и выделение свечек

                        foreach (object item in MW.myCanvas.Children.OfType<TextBlock>())
                        {
                            TextBlock itemtb = (TextBlock)item;

                            if (itemtb.Uid.Split("_").First() == "peak") itemtb.Foreground = Brushes.Gray;
                        }

                        foreach (TextBlock item in SelectedObject)
                        {
                            item.Foreground = Brushes.Blue;
                            item.FontWeight = FontWeights.UltraBold;
                        }

                        SelectPointFromPeak(SeriesPeaks, new RoutedEventArgs());
                    }
                    else StatusMessage = "ВНИМАНИЕ: выбраны пики из разных рядов или у одного из пиков нет Tsp";
                }
                else StatusMessage = "ВНИМАНИЕ: в качестве выделенных объектов Не Пики";
            }
            else StatusMessage = "ВНИМАНИЕ: в качестве выделенных объектов Не Пики";

        }

        public Command CalculateTLinesCommand { get; }
        private bool CanCalculateTLinesCommandExecute(object p) => true;
        private void OnCalculateTLinesCommandExecuted(object p)
        {
            if (SelectedObject.Any() && SelectedObject[0].GetType() == typeof(Polygon))
            {
                if (Direction is "Up" or "Dn")
                {
                    List<Candle> basecandles = new();
                    foreach (Polygon item in SelectedObject)
                    {
                        basecandles.Add(Candles.First(a => a.id == item.Uid));
                    }
                    basecandles = basecandles.OrderBy(a => a.MaxPoint.X).ToList();

                    List<Candle> rowcandles = new();
                    foreach (Peak peak in SeriesPeaks)
                    {
                        foreach (string s in peak.CandlesId)
                        {
                            rowcandles.Add(Candles.First(a => a.id == s));
                        }
                    }
                    rowcandles = rowcandles.Distinct().ToList();
                    rowcandles = rowcandles.OrderBy(a => a.MaxPoint.X).ToList();

                    bool coincidenceofpoint = true;
                    foreach (Candle candle in basecandles)
                    {
                        if (rowcandles.Contains(candle)) continue;
                        else coincidenceofpoint = false; break;
                    }

                    if (coincidenceofpoint)
                    {
                        if (rowcandles[0].MaxPoint.X < basecandles[0].MaxPoint.X)
                        {
                            ClearData("newlines");

                            TLinesCalculator calculate = new(SeriesPeaks, Direction, WidthCanvas, HeightCanvas, Breakdown.BD.Y);
                            TLines = calculate.CalculateTLines(basecandles);

                            foreach (TLine tline in TLines)
                            {
                                Line line = new();
                                line.X1 = tline.FirstPoint.X;
                                line.Y1 = tline.FirstPoint.Y;
                                line.X2 = WidthCanvas;
                                line.Y2 = tline.CalculateY(WidthCanvas);
                                line.StrokeThickness = 0.5;
                                line.Uid = tline.Id;

                                line.MouseLeftButtonDown += SelectChild;

                                if (tline.MainType is "Лн")
                                {
                                    line.Stroke = Brushes.LightCoral;
                                }
                                else if (tline.MainType is "Ор")
                                {
                                    line.Stroke = Brushes.LightCoral;
                                    line.StrokeDashArray = new() { 8, 4 };
                                }
                                else if (tline.MainType is "Рэ")
                                {
                                    line.Stroke = Brushes.Black;
                                }

                                MW.myCanvas.Children.Add(line);
                            }
                        }
                        else StatusMessage = "ВНИМАНИЕ: для последней точки расчета линий не хватает точки ряда для расчета истории, ИЛИ, выделена лишняя точка!";
                    }
                    else StatusMessage = "ВНИМАНИЕ: выделенные точки не совпадают с точками выделенного ряда пиков";
                }
                else StatusMessage = "ВНИМАНИЕ: для указания Направления используйте только слова Up или Dn";
            }
            else StatusMessage = "ВНИМАНИЕ: точки не выделенны";
        }

        public Command RecordCommand { get; }
        private bool CanRecordCommandExecute(object p) => true;
        private void OnRecordCommandExecuted(object p)
        {
            if (TLines.Any() && Breakdown.BD.Y != 0 && SelectedObject.Count == 1 && SelectedObject[0].GetType() == typeof(Polygon))
            {
                int index = Candles.IndexOf(Candles.First(a => a.id == SelectedObject[0].Uid));

                if (Candles[index].MaxPoint.X > Breakdown.BD.X)
                {
                    List<Candle> candleway = new();

                    candleway.Add(Candles[index]);

                    for (int n = index - 1; n >= 0; n--)
                    {
                        if (Direction is "Up" && Candles[n].MaxPoint.Y < Breakdown.BD.Y && Candles[n].MaxPoint.X > Breakdown.BD.X)
                            candleway.Add(Candles[n]);
                        if (Direction is "Dn" && Candles[n].MinPoint.Y > Breakdown.BD.Y && Candles[n].MinPoint.X > Breakdown.BD.X)
                            candleway.Add(Candles[n]);
                    }
                    candleway = candleway.OrderBy(a => a.MaxPoint.X).ToList();

                    double startpoint = Breakdown.BD == Breakdown.P.Tsp ||
                                        (Breakdown.P.DTP.Contains(Breakdown.BD) &&
                                         ((Breakdown.P.Direction is "Up" && Breakdown.BD.Y <= Breakdown.P.Tsp.Y) ||
                                          (Breakdown.P.Direction is "Dn" && Breakdown.BD.Y >= Breakdown.P.Tsp.Y)))
                                        ? Breakdown.P.CutOffPoint.X > Breakdown.P.Tsp.X
                                            ? Breakdown.P.CutOffPoint.Y
                                            : Breakdown.P.FallPoint.Y
                                        : Breakdown.P.Tsp.Y;

                    List<TLine> cells = CellCalculator.CellMethod(Direction, TLines, candleway, Breakdown.BD.Y, Zone1314, startpoint);

                    // Визуализация

                    for (int n = MW.myCanvas.Children.Count - 1; n >= 0; n--)
                    {
                        if (MW.myCanvas.Children[n].GetType() == typeof(Line) && MW.myCanvas.Children[n].Uid == "slice")
                        {
                            MW.myCanvas.Children.Remove(MW.myCanvas.Children[n]);
                            break;
                        }
                    }

                    Line slice = new();

                    slice.Y1 = Breakdown.BD.Y;
                    slice.X1 = Candles[index].MaxPoint.X;
                    slice.Y2 = Direction is "Up" ? 20 : HeightCanvas - 20;
                    slice.X2 = Candles[index].MaxPoint.X;
                    slice.Stroke = Brushes.LightBlue;
                    slice.StrokeThickness = 1;
                    slice.Uid = "slice";

                    slice.MouseLeftButtonDown += SelectChild;

                    MW.myCanvas.Children.Add(slice);

                    RecordWindow RW = new();
                    RW.RecordMethod(cells);
                    RW.Show();
                }
                else StatusMessage = "ВНИМАНИЕ: выбранная свечка не подходит для проведения расчетов Записи";
            }
            else StatusMessage = "ВНИМАНИЕ: для расчета Записи нобходимо рассчитать линии, указать точку Пробоя и выбрать свечку окончания расчета";
        }

        #endregion

        #region КОМАНДЫ для действий с представлениями модели (Polygon, Line, TextBlock)

        //Полное снятие выделения

        public Command ClearSelectedListCommand { get; }
        private bool CanClearSelectedListCommandExecute(object p) => true;
        private void OnClearSelectedListCommandExecuted(object p)
        {
            if (SelectedObject.Any())
            {
                for (int n = SelectedObject.Count - 1; n >= 0; n--)
                {
                    UnSelectChild(SelectedObject[n], new RoutedEventArgs());
                }
            }
        }

        // Удаление выделенных объектов

        public Command DeleteSelectedItemsCommand { get; }
        private bool CanDeleteSelectedItemsCommandExecute(object p) => true;
        private void OnDeleteSelectedItemsCommandExecuted(object p)
        {
            DeleteUIElement(p, new RoutedEventArgs());
        }

        // Управление CheckBox в меню View

        public Command CheckedChangeCommand { get; }
        private bool CanCheckedChangeCommandExecute(object p) => true;
        private void OnCheckedChangeCommandExecuted(object p)
        {
            CheckBox checkbox = (CheckBox)p;

            OnClearSelectedListCommandExecuted(null);

            StatusMessage = "";

            /// Устанвка флага IsCheked происходит при нажатии на элемент управления, суда приходит результат факта нажатия
            if (Candles.Any() && checkbox.Name == "checkBox_CandleAll")
            {
                for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                {
                    if (MW.myCanvas.Children[x].GetType() == typeof(Polygon))
                    {
                        Polygon polygon = (Polygon)MW.myCanvas.Children[x];

                        polygon.StrokeThickness = checkbox.IsChecked == true ? 1 : 0;
                    }
                }
            }
            else if (Peaks.Any() && checkbox.Name == "checkBox_Peaks")
            {
                for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                {
                    if (MW.myCanvas.Children[x].Uid.Split("_").First() == "peak")
                    {
                        TextBlock textblock = (TextBlock)MW.myCanvas.Children[x];
                        textblock.Text = checkbox.IsChecked == true ? MW.myCanvas.Children[x].Uid.Split("_").Last() : "";
                    }
                }
            }
            else if (TLines.Any())
            {
                for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                {
                    if (MW.myCanvas.Children[x].Uid.Split("_").First() == "line")
                    {
                        Line line = (Line)MW.myCanvas.Children[x];
                        line.StrokeThickness = checkbox.IsChecked == true ? 0.5 : 0;
                    }
                }
            }
        }

        #endregion

        #region МЕТОДЫ для действий с представлениями модели (Polygon, Line, TextBlock)

        internal void DeleteUIElement(object sender, EventArgs e)
        {
            if (SelectedObject.Any())
            {
                StatusMessage = "";

                for (int n = SelectedObject.Count - 1; n >= 0; n--)
                {
                    if (SelectedObject[n].GetType() == typeof(Polygon))
                    {
                        if (Peaks.Count == 0)
                        {
                            Polygon polygon = (Polygon)SelectedObject[n];

                            UnSelectChild(polygon, new RoutedEventArgs());

                            MW.myCanvas.Children.Remove(polygon);

                            Candles.Remove(Candles.First(a => a.id == polygon.Uid));
                        }
                        else StatusMessage = "ВНИМАНИЕ: нельзя удалять свечки после расчета пиков";
                    }
                    else if (SelectedObject[n].GetType() == typeof(TextBlock))
                    {
                        if (TLines.Count == 0)
                        {
                            TextBlock text = (TextBlock)SelectedObject[n];

                            UnSelectChild(text, new RoutedEventArgs());

                            if (text.Uid.Split("_").First() is "peak")
                            {
                                Peak peak = Peaks.First(a => a.Id == text.Uid);

                                if (SeriesPeaks.Any() && SeriesPeaks.Contains(peak)) SeriesPeaks.Remove(peak);

                                Peaks.Remove(peak);
                            }
                            MW.myCanvas.Children.Remove(text);
                        }
                        else StatusMessage = "ВНИМАНИЕ: нельзя удалять пики после расчета линий";
                    }

                    else if (SelectedObject[n].GetType() == typeof(Line))
                    {
                        Line line = (Line)SelectedObject[n];

                        UnSelectChild(line, new RoutedEventArgs());

                        if (line.Uid.Split("_").First() is "line")
                        {
                            TLines.Remove(TLines.First(a => a.Id == line.Uid));
                        }
                        else if (line.Uid is "breakdown") Breakdown = new(new(0, 0), null);
                        else if (line.Uid is "zone1314") Zone1314 = 0;

                        MW.myCanvas.Children.Remove(line);
                    }
                }
            }
        }

        public void PushKeyU(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.U)
            {
                OnClearSelectedListCommandExecuted(sender);
            }
        }

        // Методы выделения и снятия выделения

        internal void SelectChild(object sender, RoutedEventArgs e)
        {
            if (SelectedObject.Count == 0 || (sender.GetType() == SelectedObject[0].GetType() && !SelectedObject.Contains(sender)))
            {
                if (sender.GetType() == typeof(Polygon))
                {
                    Polygon polygon = (Polygon)sender;

                    SolidColorBrush myBrush = new(Colors.Red);
                    myBrush.Opacity = 1;

                    polygon.Fill = myBrush;

                    polygon.MouseLeftButtonDown -= SelectChild;
                    polygon.MouseLeftButtonDown += UnSelectChild;
                    polygon.MouseRightButtonDown += CandleCustomMenu;

                    SelectedObject.Add(polygon);

                    DisplayInfoToStatusMessage();

                    ShowExtremumOfCandle(Candles.First(a => a.id == polygon.Uid));
                }
                else if (sender.GetType() == typeof(TextBlock))
                {
                    TextBlock textblock = (TextBlock)sender;

                    textblock.FontWeight = FontWeights.UltraBold;

                    SelectedObject.Add(textblock);

                    textblock.MouseLeftButtonDown -= SelectChild;
                    textblock.MouseLeftButtonDown += UnSelectChild;

                    if (textblock.Uid.Split("_").First() is "peak")
                    {
                        textblock.MouseRightButtonDown += PeakCustomMenu;

                        Peak peak = Peaks.First(a => a.Id == textblock.Uid);

                        foreach (string c_id in peak.CandlesId)
                        {
                            Candle candle = Candles.First(a => a.id == c_id);
                            ShowExtremumOfCandle(candle);
                        }
                    }
                }
                else if (sender.GetType() == typeof(Line))
                {
                    Line line = (Line)sender;

                    if (SelectedObject.Count == 0 || line.Uid is "breakdown" or "zone1314" or "slice" or "random" ||
                       (line.Uid.Split("_").First() is "line" && SelectedObject[0].Uid.Split("_").First() is "line"))
                    {
                        line.MouseLeftButtonDown -= SelectChild;
                        line.MouseLeftButtonDown += UnSelectChild;

                        if (line.Uid.Split("_").First() is "line")
                            line.MouseRightButtonDown += TLineCustomMenu;

                        line.StrokeThickness = 2;

                        SelectedObject.Add(line);

                        DisplayInfoToStatusMessage();
                    }
                    else StatusMessage = "ВНИМАНИЕ: нельзя выделять вместе линии несущие разный смысл в модели";
                }
            }
            else StatusMessage = "ВНИМАНИЕ: тип выделяемого объекта не соответствует типу уже выделенных объектов";
        }
        internal void UnSelectChild(object sender, RoutedEventArgs e)
        {
            if (sender.GetType() == typeof(Polygon))
            {
                Polygon polygon = (Polygon)sender;

                SolidColorBrush myBrush = new();
                myBrush.Opacity = 0;

                polygon.Fill = myBrush;

                SelectedObject.Remove(polygon);

                DisplayInfoToStatusMessage();

                // убираем представление экстремума
                HideExtremumOfCandle(Candles.First(a => a.id == polygon.Uid));

                // меняем действие по ЛКМ
                polygon.MouseLeftButtonDown -= UnSelectChild;
                polygon.MouseRightButtonDown -= CandleCustomMenu;

                polygon.MouseLeftButtonDown += SelectChild;
            }
            else if (sender.GetType() == typeof(TextBlock))
            {
                TextBlock textblock = (TextBlock)sender;

                textblock.FontWeight = FontWeights.Normal;

                SelectedObject.Remove(textblock);

                textblock.MouseLeftButtonDown -= UnSelectChild;
                textblock.MouseLeftButtonDown += SelectChild;

                if (textblock.Uid.Split("_").First() is "peak")
                {
                    textblock.MouseRightButtonDown -= PeakCustomMenu;

                    Peak peak = Peaks.First(a => a.Id == textblock.Uid);

                    foreach (string c_id in peak.CandlesId)
                    {
                        Candle candle = Candles.First(a => a.id == c_id);
                        HideExtremumOfCandle(candle);
                    }
                }
            }
            else if (sender.GetType() == typeof(Line))
            {
                Line line = (Line)sender;

                line.MouseLeftButtonDown -= UnSelectChild;
                line.MouseRightButtonDown -= TLineCustomMenu;

                line.MouseLeftButtonDown += SelectChild;

                line.StrokeThickness = 1;

                SelectedObject.Remove(line);

                DisplayInfoToStatusMessage();
            }
        }

        internal void ShowExtremumOfCandle(Candle candle)
        {
            if (candle.ViewMax.ellipse != null)
            {
                Canvas.SetLeft(candle.ViewMax.ellipse, candle.ViewMax.point.X - 3);
                Canvas.SetTop(candle.ViewMax.ellipse, candle.ViewMax.point.Y - 15);

                if (!MW.myCanvas.Children.Contains(candle.ViewMax.ellipse)) MW.myCanvas.Children.Add(candle.ViewMax.ellipse);
            }

            if (candle.ViewMin.ellipse != null)
            {
                Canvas.SetLeft(candle.ViewMin.ellipse, candle.ViewMin.point.X - 3);
                Canvas.SetTop(candle.ViewMin.ellipse, candle.ViewMin.point.Y + 15);

                if (!MW.myCanvas.Children.Contains(candle.ViewMin.ellipse)) MW.myCanvas.Children.Add(candle.ViewMin.ellipse);
            }
        }
        internal void HideExtremumOfCandle(Candle candle)
        {
            if (candle.ViewMax.ellipse != null) MW.myCanvas.Children.Remove(candle.ViewMax.ellipse);
            if (candle.ViewMin.ellipse != null) MW.myCanvas.Children.Remove(candle.ViewMin.ellipse);
        }

        internal void DisplayInfoToStatusMessage()
        {
            StatusMessage = "";

            foreach (UIElement element in SelectedObject)
            {
                if (element.Uid is "breakdown") StatusMessage += element.Uid + ":" + Breakdown.BD.Y.ToString();
                else if (element.Uid is "zone1314") StatusMessage += element.Uid + ":" + Zone1314.ToString();

                else if (element.Uid.Split("_").First() is "line") StatusMessage += element.Uid;

                else if (element.Uid.Split("_").First() is "candle")
                {
                    Candle candle = Candles.First(a => a.id == element.Uid);

                    StatusMessage += element.Uid + ": Max " + candle.MaxPoint.Y.ToString() + " Min " + candle.MinPoint.Y.ToString();
                }

                StatusMessage += " / ";
            }
        }

        internal void InitializationSelectGroup(object sender, RoutedEventArgs e)
        {
            if (MW.brdrOne.Flag_Select)
            {
                PointStart = Mouse.GetPosition(MW.myCanvas);
                MW.brdrOne.MouseMove += SelectGroup;
            }
        }
        internal void SelectGroup(object sender, RoutedEventArgs e)
        {
            if (MW.brdrOne.Flag_Select)
            {
                Point currentPoint = Mouse.GetPosition(MW.myCanvas);

                if (currentPoint.X <= PointStart.X && currentPoint.Y <= PointStart.Y)  // сверху слева вниз направо
                {
                    for (int n = 0; n < MW.myCanvas.Children.Count; n++)
                    {
                        if (MW.myCanvas.Children[n].GetType() == typeof(Polygon))
                        {
                            Polygon polygon = (Polygon)MW.myCanvas.Children[n];

                            if (polygon.Points[0].X <= PointStart.X && polygon.Points[0].Y <= PointStart.Y &&
                                polygon.Points[0].X >= currentPoint.X && polygon.Points[0].Y >= currentPoint.Y)
                            {
                                SelectChild(polygon, e);
                            }
                        }
                    }
                }
                else if (currentPoint.X >= PointStart.X && currentPoint.Y >= PointStart.Y) // снизу справа вверх на лево
                {
                    for (int n = 0; n < MW.myCanvas.Children.Count; n++)
                    {
                        if (MW.myCanvas.Children[n].GetType() == typeof(Polygon))
                        {
                            Polygon polygon = (Polygon)MW.myCanvas.Children[n];

                            if (polygon.Points[0].X >= PointStart.X && polygon.Points[0].Y >= PointStart.Y &&
                                polygon.Points[0].X <= currentPoint.X && polygon.Points[0].Y <= currentPoint.Y)
                            {
                                SelectChild(polygon, e);
                            }
                        }
                    }
                }
                else if (currentPoint.X <= PointStart.X && currentPoint.Y >= PointStart.Y) // сверху справа вниз налево
                {
                    for (int n = 0; n < MW.myCanvas.Children.Count; n++)
                    {
                        if (MW.myCanvas.Children[n].GetType() == typeof(Polygon))
                        {
                            if (MW.myCanvas.Children[n].GetType() == typeof(Polygon))
                            {
                                Polygon polygon = (Polygon)MW.myCanvas.Children[n];

                                if (polygon.Points[0].X <= PointStart.X && polygon.Points[0].Y >= PointStart.Y &&
                                    polygon.Points[0].X >= currentPoint.X && polygon.Points[0].Y <= currentPoint.Y)
                                {
                                    SelectChild(polygon, e);
                                }
                            }
                        }
                    }
                }
                else  // (currentPoint.X >= PointStart.X && currentPoint.Y <= PointStart.Y) снизу слева вверх направо
                {
                    for (int n = 0; n < MW.myCanvas.Children.Count; n++)
                    {
                        if (MW.myCanvas.Children[n].GetType() == typeof(Polygon))
                        {
                            Polygon polygon = (Polygon)MW.myCanvas.Children[n];

                            if (polygon.Points[0].X >= PointStart.X && polygon.Points[0].Y <= PointStart.Y &&
                                polygon.Points[0].X <= currentPoint.X && polygon.Points[0].Y >= currentPoint.Y)
                            {
                                SelectChild(polygon, e);
                            }
                        }
                    }
                }
            }
        }
        internal void EndSelectGroup(object sender, RoutedEventArgs e)
        {
            if (MW.brdrOne.Flag_Select)
            {
                MW.brdrOne.MouseMove -= SelectGroup;
            }
        }


        // Polygon/Candle методы контекстного меню

        internal void BreakdownMethod(object sender, RoutedEventArgs e)
        {
            if (Direction is "Up" or "Dn" && Peaks.Any() &&
                ((SelectedObject.Count == 1 && SelectedObject[0].GetType() == typeof(Polygon)) ||
                  sender.GetType() == typeof(Peak)))
            {
                if (sender.GetType() == typeof(Peak))
                {
                    Peak peak = (Peak)sender;

                    Breakdown.BD = Direction == peak.Direction ? peak.Tsp : peak.CutOffPoint;
                    Breakdown.P = peak;
                }
                else
                {
                    Candle candle = Candles.First(a => a.id == SelectedObject[0].Uid);

                    if (Peaks.Contains(Peaks.First(a => a.CandlesId.Contains(candle.id))))
                    {
                        Peak peak = Peaks.First(a => a.CandlesId.Contains(candle.id));

                        if (peak.DTP.Contains(candle.MaxPoint) || peak.Np.Contains(candle.MaxPoint))
                        {
                            Breakdown.BD = candle.MaxPoint;
                            Breakdown.P = peak;

                            StatusMessage = "точка пробоя посчитана";
                        }
                        else if (peak.DTP.Contains(candle.MinPoint) || peak.Np.Contains(candle.MinPoint))
                        {
                            Breakdown.BD = candle.MinPoint;
                            Breakdown.P = peak;

                            StatusMessage = "точка пробоя посчитана";
                        }
                        else StatusMessage = "ВНИМАНИЕ: экстремум выделенной свечки не является DTP или Np точкой пика";

                    }
                    else StatusMessage = "ВНИМАНИЕ: экстремум выделенной свечки не пренадлижит ни одной точке пика";
                }

                // Визуализация

                for (int n = MW.myCanvas.Children.Count - 1; n >= 0; n--)
                {
                    if (MW.myCanvas.Children[n].GetType() == typeof(Line) && MW.myCanvas.Children[n].Uid == "breakdown")
                    {
                        MW.myCanvas.Children.Remove(MW.myCanvas.Children[n]);
                        break;
                    }
                }

                Line horisont = new();
                horisont.X1 = Breakdown.BD.X - 15;
                horisont.Y1 = Breakdown.BD.Y;
                horisont.X2 = WidthCanvas - 20;
                horisont.Y2 = Breakdown.BD.Y;
                horisont.Stroke = Brushes.LightBlue;
                horisont.StrokeThickness = 1;
                horisont.Uid = "breakdown";

                horisont.MouseLeftButtonDown += SelectChild;

                MW.myCanvas.Children.Add(horisont);
            }
            else StatusMessage = "ВНИМАНИЕ: для расчета требуется выделить либо один пик, либо одну свечку, экстремум которой является точкой пика, плюс необходимо указать направление расчетов в поле ввода Up/Dn, пики должны быть рассчитаны";
        }

        internal void RandomLineMethod(object sender, RoutedEventArgs e)
        {
            if (Direction is "Up" or "Dn" && SelectedObject.Count == 1 && SelectedObject[0].GetType() == typeof(Polygon))
            {
                Polygon polygon = (Polygon)SelectedObject[0];

                Candle candle = Candles.First(a => a.id == polygon.Uid);

                Line random = new();

                if (Direction is "Up")
                {
                    random.Y1 = candle.MaxPoint.Y;
                    random.Y2 = candle.MaxPoint.Y;
                }
                else
                {
                    random.Y1 = candle.MinPoint.Y;
                    random.Y2 = candle.MinPoint.Y;
                }
                random.X1 = candle.MinPoint.X - 20;
                random.X2 = WidthCanvas - 20;
                random.Stroke = Brushes.LightBlue;
                random.StrokeThickness = 1;
                random.Uid = "random";

                random.MouseLeftButtonDown += SelectChild;

                MW.myCanvas.Children.Add(random);
            }
            else StatusMessage = "ВНИМАНИЕ: выделите 1 свечку, укажите Up / Dn в поле ввода Up/Dn";
        }

        internal void AddDeletePointToPeak(object sender, RoutedEventArgs e)
        {
            if (SelectedObject.Count == 1 && Peaks.Any() && InputLine is "max" or "min")
            {
                foreach (object elelemnt in MW.myCanvas.Children.OfType<TextBlock>())
                {
                    TextBlock textblock = (TextBlock)elelemnt;

                    if (textblock.Uid.Split("_").First() == "peak")
                        textblock.MouseLeftButtonDown += ClickToPeakToAddDeletePoint;
                }
                StatusMessage = "ЗАПРОС: выберете пик";
            }
            else StatusMessage = "ВНИМАНИЕ: выделено более одной свечки, или не посчитаны пики, или надо в поле ввода Input указать max / min";
        }
        internal void ClickToPeakToAddDeletePoint(object sender, RoutedEventArgs e) 
        {
            foreach (object elelemnt in MW.myCanvas.Children.OfType<TextBlock>())
            {
                TextBlock textblock = (TextBlock)elelemnt;

                if (textblock.Uid.Split("_").First() == "peak")
                {
                    textblock.MouseLeftButtonDown -= ClickToPeakToAddDeletePoint;
                }
            }

            if (sender.GetType() == typeof(TextBlock))
            {
                TextBlock textblock = (TextBlock)sender;
                Polygon polygon = (Polygon)SelectedObject[0];

                Peak peak = Peaks.First(a => a.Id == textblock.Uid);
                Candle candle = Candles.First(a => a.id == polygon.Uid);

                UnSelectChild(polygon, new RoutedEventArgs());

                if (peak.AdePoint(candle, InputLine))
                    StatusMessage = "точка добавлена";
                else if (peak.DeletePoint(candle, InputLine))
                {
                    HideExtremumOfCandle(candle);

                    StatusMessage = "точка удалена";
                }
                else StatusMessage = "ВНИМАНИЕ: выбранная свечка не соответсвует ни одной точке пика, удалить/добавить её пику не удалось, добавте FallPoint";

                SelectChild(textblock, new RoutedEventArgs());
            }
            else StatusMessage = "ВНИМАНИЕ: условие для добавления/удаления точки пику не выполнено";
        }

        internal void CreatePeak(object sender, EventArgs e)
        {
            if (SelectedObject.Count > 2 && Peaks.Any() && InputLine is "Up" or "Dn" or "K")
            {
                List<Candle> targetcandles = new();

                foreach (Polygon polygon in SelectedObject)
                {
                    targetcandles.Add(Candles.First(a => a.id == polygon.Uid));
                }

                Peak newpeak = new(InputLine, targetcandles, MinSizePeak, Candles);

                if (newpeak.Mass != 0)
                {
                    Peaks.Add(newpeak);

                    newpeak.Id = "peak_" + (Peaks.Count - 1).ToString() + "_" + newpeak.Mass.ToString();

                    // снятие выделения со свечек
                    for (int n = SelectedObject.Count - 1; n >= 0; n--)
                    {
                        UnSelectChild(SelectedObject[n], new RoutedEventArgs());
                    }

                    // выделяем пик
                    TextBlock textblock = new();
                    textblock.Uid = newpeak.Id;
                    textblock.Foreground = Brushes.Red;
                    textblock.MouseLeftButtonDown += SelectChild;
                    textblock.Text = newpeak.Mass.ToString();

                    if (newpeak.Np.Count > 0) textblock.Text += "_Np";
                    if (newpeak.K) textblock.Text += "_K";

                    Canvas.SetTop(textblock, newpeak.TextPoint.Y);
                    Canvas.SetLeft(textblock, newpeak.TextPoint.X);
                    MW.myCanvas.Children.Add(textblock);

                    SelectChild(textblock, new RoutedEventArgs());
                }
                else StatusMessage = "ВНИМАНИЕ: создать пик не удалось, точки выбранны не корректно, И/ИЛИ не верно указан тип пика (Up/Dn/K) в строке ввода Input";
            }
            else StatusMessage = "ВНИМАНИЕ: необходимо минимум 3 свечки, И/ИЛИ нужно рассчитать пики, И/ИЛИ нужно указать тип пика (Up/Dn/K) в строке ввода Input";
        }

        internal void VectorLine(object sender, EventArgs e)
        {
            if (TLines.Any() && SelectedObject.Count > 1 && Direction is "Up" or "Dn")
            {
                List<Candle> targetcandles = new();
                List<Point> maxpoint = new();
                List<Point> minpoint = new();
                bool check = Direction is "Up";

                foreach (Polygon polygon in SelectedObject)
                {
                    targetcandles.Add(Candles.First(a => a.id == polygon.Uid));
                }
                targetcandles = targetcandles.OrderBy(a => a.MaxPoint.X).ToList();

                foreach (Candle candle in targetcandles)
                {
                    if (check) { maxpoint.Add(candle.MaxPoint); check = false; }
                    else { minpoint.Add(candle.MinPoint); check = true; }
                }

                check = maxpoint.Count == minpoint.Count;

                if (check)
                {
                    foreach (TLine tline in TLines)
                    {
                        tline.VectorTypeMethod(Direction, maxpoint, minpoint);

                        if (tline.VectorType is not "" and not null)
                        {
                            foreach (Line line in MW.myCanvas.Children.OfType<Line>())
                            {
                                if (line.Uid == tline.Id) line.Stroke = Brushes.OrangeRed; break;
                            }
                        }
                    }
                }
                else StatusMessage = "ВНИМАНИЕ: точки выбраны не корректно, есть выпадающие или лишние точки";
            }
            else StatusMessage = "ВНИМАНИЕ: либо не посчитаны линии, либо не указано направление движения в поле ввода Up/Dn, либо вы выбрали мало точек";
        }


        // TextBlock/Peak методы контекстного меню

        internal void Zone1314Method(object sender, RoutedEventArgs e)
        {
            if (SelectedObject.Count == 1)
            {
                Peak peak = Peaks.First(a => a.Id == SelectedObject[0].Uid);

                BreakdownMethod(peak, new RoutedEventArgs());

                if (Direction is "Up" && peak.Direction is "Up")
                {
                    Zone1314 = peak.Tsp.Y - (0.117 * (peak.CutOffPoint.Y - peak.Tsp.Y));
                }
                else if (Direction is "Dn" && peak.Direction is "Dn")
                {
                    Zone1314 = peak.Tsp.Y + (0.117 * (peak.Tsp.Y - peak.CutOffPoint.Y));
                }
                else
                {
                    Zone1314 = peak.Direction is "Up"
                    ? peak.CutOffPoint.Y + (0.117 * (peak.CutOffPoint.Y - peak.Tsp.Y))
                    : peak.CutOffPoint.Y - (0.117 * (peak.Tsp.Y - peak.CutOffPoint.Y));
                }

                // Визуализация
                for (int n = MW.myCanvas.Children.Count - 1; n >= 0; n--)
                {
                    if (MW.myCanvas.Children[n].GetType() == typeof(Line) && MW.myCanvas.Children[n].Uid == "zone1314")
                    {
                        MW.myCanvas.Children.Remove(MW.myCanvas.Children[n]);
                        break;
                    }
                }

                Line zoneline = new();
                zoneline.X1 = peak.CutOffPoint.X - 15;
                zoneline.Y1 = Zone1314;
                zoneline.X2 = WidthCanvas - 30;
                zoneline.Y2 = Zone1314;
                zoneline.Stroke = Brushes.LightBlue;
                zoneline.StrokeThickness = 1;
                zoneline.Uid = "zone1314";

                zoneline.MouseLeftButtonDown += SelectChild;

                MW.myCanvas.Children.Add(zoneline);

                StatusMessage = "ИНСТРУКЦИЯ: чтобы провести расчеты для точки Tsp направление указанное в поле ввода Up/Dn должно совпадать с направлением пика";
            }
            else StatusMessage = "ВНИМАНИЕ: Зона 13/14 считается для одного пика";
        }

        internal void AddOrDeletePeakToSeriesPeaks(object sender, RoutedEventArgs e)
        {
            // Собираем пики по Uid TextBlock
            List<Peak> listpeaks = new();
            foreach (TextBlock item in SelectedObject)
            {
                listpeaks.Add(Peaks.First(a => a.Id == item.Uid));
            }

            bool contains = false;
            bool notcontains = false;
            foreach (Peak peak in listpeaks)
            {
                if (SeriesPeaks.Contains(peak)) contains = true;
                else notcontains = true;
            }

            if (contains && !notcontains)
            {
                foreach (Peak peak in listpeaks)
                {
                    SeriesPeaks.Remove(peak);

                    TextBlock textblock = (TextBlock)SelectedObject.First(a => a.Uid == peak.Id);

                    textblock.Foreground = Brushes.Gray;
                    textblock.FontWeight = FontWeights.Normal;

                    CalculateSeriesPeaks.ViewPointOfSeriesPeaks(false, listpeaks, Candles);
                }
            }
            else if (notcontains && !contains)
            {
                foreach (Peak peak in listpeaks)
                {
                    SeriesPeaks.Add(peak);

                    TextBlock textblock = (TextBlock)SelectedObject.First(a => a.Uid == peak.Id);

                    textblock.Foreground = Brushes.Blue;
                    textblock.FontWeight = FontWeights.UltraBold;

                    CalculateSeriesPeaks.ViewPointOfSeriesPeaks(true, listpeaks, Candles);
                }
            }
            else StatusMessage = "ВНИМАНИЕ: выделите пики либо только принадлежащие Ряду, либо только не относящиеся к Ряду";
        }

        internal void SelectSeriesPeaks(object sender, RoutedEventArgs e)
        {
            // Собираем пики по Uid TextBlock
            List<Peak> listpeaks = new();
            foreach (TextBlock item in SelectedObject)
            {
                listpeaks.Add(Peaks.First(a => a.Id == item.Uid));
            }

            // Проверяем, что они из SeriesPeaks
            bool checkpeaks = true;
            foreach (Peak peak in listpeaks)
            {
                if (!SeriesPeaks.Contains(peak)) checkpeaks = false; break;
            }

            if (checkpeaks)
            {
                // Выделеяем Ряд пиков
                foreach (Peak peak in SeriesPeaks)
                {
                    foreach (UIElement element in MW.myCanvas.Children.OfType<TextBlock>())
                    {
                        if (element.Uid == peak.Id)
                        {
                            TextBlock textblock = (TextBlock)element;

                            SelectChild(textblock, new RoutedEventArgs());

                            break;
                        }
                    }
                }
                StatusMessage = "выделен текущий Ряд пиков";
            }
            else StatusMessage = "ВНИМАНИЕ: выделенные пики или пик не относятся к текущему Ряду пиков";
        }

        internal void SelectPointFromPeak(object sender, RoutedEventArgs e)
        {
            // Отбираем пики
            List<Peak> listpeaks = new();
            foreach (TextBlock item in SelectedObject)
            {
                listpeaks.Add(Peaks.First(a => a.Id == item.Uid));
            }

            // Снимаем выделение с TextBlock
            for (int n = SelectedObject.Count - 1; n >= 0; n--)
            {
                UnSelectChild(SelectedObject[n], new RoutedEventArgs());
            }

            // Выделяем сами свечки с точками
            List<Candle> rowcandles = new();
            foreach (Peak peak in listpeaks)
            {
                foreach (string s in peak.CandlesId)
                {
                    rowcandles.Add(Candles.First(a => a.id == s));
                }
            }
            rowcandles = rowcandles.Distinct().ToList();

            foreach (Candle candle in rowcandles)
            {
                foreach (UIElement element in MW.myCanvas.Children.OfType<Polygon>())
                {
                    if (element.Uid == candle.id)
                    {
                        Polygon polygon = (Polygon)element;

                        SelectChild(polygon, new RoutedEventArgs());

                        break;
                    }
                }
            }
            StatusMessage = "выделен точки по выделенным пикам";
        }

        internal void ShowPointsOfPeak(object sender, RoutedEventArgs e)
        {
            if (SelectedObject.Count == 1)
            {
                Peak peak = Peaks.First(a => a.Id == SelectedObject[0].Uid);
                List<Point> points = new();
                string record = "";

                points.Add(peak.CutOffPoint);
                points.Add(peak.Tsp);
                points.Add(peak.FallPoint);
                foreach (Point point in peak.DTP)
                {
                    points.Add(point);
                }
                foreach (Point point in peak.Np)
                {
                    points.Add(point);
                }
                points = points.OrderBy(a => a.X).ToList();

                foreach (Point point in points)
                {
                    if (point == peak.CutOffPoint) record += "Cut: " + peak.CutOffPoint.Y.ToString() + "; ";
                    else if (point == peak.FallPoint) record += "Fall: " + peak.FallPoint.Y.ToString() + "; ";
                    else if (point == peak.Tsp) record += "Tsp: " + peak.Tsp.Y.ToString() + "; ";
                    else if (peak.DTP.Contains(point)) record += "Dtp: " + peak.DTP.First(a => a.X == point.X).Y.ToString() + "; ";
                    else if (peak.Np.Contains(point)) record += "Np: " + peak.Np.First(a => a.X == point.X).Y.ToString() + "; ";
                }

                RecordWindow showpoints = new();
                showpoints.ShowPointsOfPeak(record);
                showpoints.Show();
            }
            else StatusMessage = "ВНИМАНИЕ: выделенно больше 1 пика";
        }

        // Line/TLine методы контекстного меню 

        internal void AppointRemoveCommonType(object sender, RoutedEventArgs e)
        {
            if (SelectedObject.Count == 1)
            {
                Line line = (Line)SelectedObject[0];
                TLine tline = TLines.First(a => a.Id == line.Uid);

                if (!tline.CommonType)
                {
                    tline.CommonType = true;

                    if (tline.MainType is "Лн" or "Ор") line.Stroke = Brushes.Green;
                    else if (tline.MainType is "Рэ") line.Stroke = Brushes.DarkGreen;
                }
                else
                {
                    tline.CommonType = false;

                    if (tline.MainType is "Лн") line.Stroke = Brushes.LightCoral;
                    else if (tline.MainType is "Ор") line.Stroke = Brushes.LightCoral;
                    else if (tline.MainType is "Рэ") line.Stroke = Brushes.Black;
                }
            }
            else
            {
                foreach (Line line in SelectedObject)
                {
                    TLine tline = TLines.First(a => a.Id == line.Uid);
                    tline.CommonType = true;

                    if (tline.MainType is "Лн" or "Ор") line.Stroke = Brushes.Green;
                    else if (tline.MainType is "Рэ") line.Stroke = Brushes.DarkGreen;
                }
                StatusMessage = "ЗАМЕТКА: чтобы убрать тип Общие, выделите только одну линию";
            }
        }
        internal void RemoveVectorType(object sender, RoutedEventArgs e)
        {
            foreach (Line line in SelectedObject)
            {
                TLine tline = TLines.First(a => a.Id == line.Uid);
                tline.VectorType = "";

                if (tline.MainType is "Лн") line.Stroke = Brushes.LightCoral;
                else if (tline.MainType is "Ор") line.Stroke = Brushes.LightCoral;
                else if (tline.MainType is "Рэ") line.Stroke = Brushes.Black;
            }
            StatusMessage = "тип Вектор обнулен у всех вделенных линий, чтобы рассчитать его снова -- выделете нужные пики и вызовите действие для расчета";
        }

        internal void Select_Ln_Or_Re_Type(object sender, RoutedEventArgs e)
        {
            string selecttype = TLines.First(a => a.Id == SelectedObject[0].Uid).MainType;

            bool onetype = true;
            foreach (Line line in SelectedObject)
            {
                if (TLines.First(a => a.Id == line.Uid).MainType != selecttype) onetype = false;
            }

            if (onetype)
            {
                foreach (TLine tline in TLines)
                {
                    if (tline.MainType == selecttype)
                    {
                        foreach (Line line in MW.myCanvas.Children.OfType<Line>())
                        {
                            if (tline.Id == line.Uid && !SelectedObject.Contains(line))
                                SelectChild(line, new RoutedEventArgs());
                        }
                    }
                }
            }
            else StatusMessage = "ВНИМАНИЕ: выделены линии разных типов, невозможно определить линии какого типа выделять";
        }
        internal void Select_CommonType(object sender, RoutedEventArgs e)
        {
            for (int n = SelectedObject.Count - 1; n >= 0; n--)
            {
                UnSelectChild(SelectedObject[n], new RoutedEventArgs());
            }

            foreach (TLine tline in TLines)
            {
                if(tline.CommonType)
                {
                    foreach (Line line in MW.myCanvas.Children.OfType<Line>())
                    {
                        if (tline.Id == line.Uid)
                            SelectChild(line, new RoutedEventArgs());
                    }
                }
            }
        }
        internal void Select_HistoryType(object sender, RoutedEventArgs e)
        {
            if (InputLine.Contains('С') && (InputLine.Last().ToString() is "п" || int.TryParse(InputLine.Last().ToString(), out int _)))
            {
                for (int n = SelectedObject.Count - 1; n >= 0; n--)
                {
                    UnSelectChild(SelectedObject[n], new RoutedEventArgs());
                }

                double his = InputLine is "п" || InputLine.Last().ToString() is "п" ? 2.5 : double.Parse(InputLine.Split("С").Last());

                string s = InputLine.Split("С").First() is "" ? "" : InputLine.Split("С").First();

                foreach (TLine tline in TLines)
                {
                    double tlhis = tline.HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(tline.HistoryType.Split("С").Last());

                    if ((s is "" || s == tline.MainType) &&
                       ((his < 2 && tlhis < 2) || (his > 2 && tlhis > 2)))
                    {
                        foreach (Line line in MW.myCanvas.Children.OfType<Line>())
                        {
                            if (tline.Id == line.Uid)
                            {
                                if (tlhis > 2 && !tline.CommonType && tline.VectorType is "")
                                    line.Stroke = Brushes.Red;

                                SelectChild(line, new RoutedEventArgs());
                            }
                        }
                    }
                }
            }
            else StatusMessage = "ВНИМАНИЕ: в строке ввода Input надо указать русскими буквами(!) основной тип и/или тип истории, чтобы выделить нужные линии (пример: ЛнС1,5, ОрС3, ЛнС3п, С3) (указывать на русском языке)";
        }
        internal void Select_VectorType(object sender, RoutedEventArgs e)
        {
            for (int n = SelectedObject.Count - 1; n >= 0; n--)
            {
                UnSelectChild(SelectedObject[n], new RoutedEventArgs());
            }

            foreach (TLine tline in TLines)
            {
                if (tline.VectorType != "")
                {
                    foreach (Line line in MW.myCanvas.Children.OfType<Line>())
                    {
                        if (tline.Id == line.Uid)
                            SelectChild(line, new RoutedEventArgs());
                    }
                }
            }
        }
        internal void Select_AllTLine(object sender, RoutedEventArgs e)
        {
            for (int n = SelectedObject.Count - 1; n >= 0; n--)
            {
                UnSelectChild(SelectedObject[n], new RoutedEventArgs());
            }

            foreach (Line line in MW.myCanvas.Children.OfType<Line>())
            {
                if (line.Uid.Split("_").First() is "line")
                    SelectChild(line, new RoutedEventArgs());
            }
        }

        internal void HideNotSelectedShowAll(object sender, RoutedEventArgs e)
        {
            if (MW.checkBox_TLine.IsChecked == true)
            {
                foreach (Line line in MW.myCanvas.Children.OfType<Line>())
                {
                    if (!SelectedObject.Contains(line)) line.StrokeThickness = 0;
                }
                MW.checkBox_TLine.IsChecked = false;
            }
            else
            {
                foreach (Line line in MW.myCanvas.Children.OfType<Line>())
                {
                    line.StrokeThickness = SelectedObject.Contains(line) ? 2 : 0.5;
                }
                MW.checkBox_TLine.IsChecked = true;
            }
        }

        internal void ControlBrightness(object sender, RoutedEventArgs e)
        {
            foreach (Line line in SelectedObject)
            {
                TLine tline = TLines.First(a => a.Id == line.Uid);

                if (tline.MainType is "Лн" or "Ор")
                    line.Stroke = line.Stroke == Brushes.LightCoral ? Brushes.LightPink : Brushes.LightCoral;
                else if (tline.MainType is "Рэ")
                    line.Stroke = line.Stroke == Brushes.Black ? Brushes.LightGray : Brushes.Black;
            }
        }

        internal void ProximityCalculatingStart(object sender, RoutedEventArgs e)
        {
            if (SeriesPeaks.Any() && Breakdown.BD.X != 0 && Direction is "Up" or "Dn")
            {
                foreach (Polygon polygon in MW.myCanvas.Children.OfType<Polygon>())
                {
                    polygon.MouseLeftButtonDown -= SelectChild;
                    polygon.MouseLeftButtonDown += ProximityCalculatingEnd;
                }
                StatusMessage = "ЗАПРОС: выберете свечку, чтобы получить X координату для расчета";
            }
            else StatusMessage = "ВНИМАНИЕ: пустой Ряд Пиков или не задана точка Пробоя или не задано направление расчетов, задача отменена";
        }
        internal void ProximityCalculatingEnd(object sender, RoutedEventArgs e)
        {
            foreach (Polygon polygon in MW.myCanvas.Children.OfType<Polygon>())
            {
                polygon.MouseLeftButtonDown -= ProximityCalculatingEnd;
                polygon.MouseLeftButtonDown += SelectChild;
            }

            if (sender.GetType() == typeof(Polygon))
            {
                StatusMessage = "";
                SeriesPeaks = SeriesPeaks.OrderBy(a => a.Tsp.X).ToList();

                Polygon polygon = (Polygon)sender;
                Candle candle = Candles.First(a => a.id == polygon.Uid);


                if (candle.MaxPoint.X >= SeriesPeaks[^1].Tsp.X)
                {
                    double startpoint = Breakdown.BD == Breakdown.P.Tsp ||
                                        (Breakdown.P.DTP.Contains(Breakdown.BD) &&
                                         ((Breakdown.P.Direction is "Up" && Breakdown.BD.Y <= Breakdown.P.Tsp.Y) ||
                                          (Breakdown.P.Direction is "Dn" && Breakdown.BD.Y >= Breakdown.P.Tsp.Y)))
                                        ? Breakdown.P.CutOffPoint.X > Breakdown.P.Tsp.X
                                            ? Breakdown.P.CutOffPoint.Y
                                            : Breakdown.P.FallPoint.Y
                                        : Breakdown.P.Tsp.Y;

                    double proximity;

                    List<TLine> listtlines = new();

                    foreach (Line line in SelectedObject)
                    {
                        TLine tline = TLines.First(a => a.Id == line.Uid);

                        tline.SecondPoint = new(candle.MaxPoint.X, tline.CalculateY(candle.MaxPoint.X));

                        listtlines.Add(tline);
                    }
                    listtlines = Direction is "Up"
                        ? listtlines.OrderByDescending(a => a.SecondPoint.Y).ToList()
                        : listtlines.OrderBy(a => a.SecondPoint.Y).ToList();

                    for (int n = 0; n + 1 < listtlines.Count; n++)
                    {
                        proximity = Direction is "Up"
                            ? (listtlines[n].SecondPoint.Y - listtlines[n + 1].SecondPoint.Y) / (startpoint - listtlines[n].SecondPoint.Y) * 100
                            : (listtlines[n + 1].SecondPoint.Y - listtlines[n].SecondPoint.Y) / (listtlines[n].SecondPoint.Y - startpoint) * 100;
                        proximity = Math.Round(proximity, 2);

                        StatusMessage += listtlines[n].MainType + "--" + proximity.ToString() + "--";
                    }
                    StatusMessage += listtlines[^1].MainType;
                }
                else StatusMessage = "ВНИМАНИЕ: выбранная точка лежит за пределами зоны расчета или не удалось найти пик, от которого взята точка Пробоя, чтобы посчитать силу движения, задача отменена";
            }
            else StatusMessage = "ВНИМАНИЕ: условие расчета близости для выделенных линий не выполнено, задача отменена";
        }

        #endregion

        #region Контекстное Меню для действий с представлениями модели (Polygon, Line, TextBlock)
        // срабатывает при выделении объекта; смотреть методы SelecteChild и UnSelectChild

        internal void CandleCustomMenu(object sender, EventArgs e)
        {
            MenuItem BreakdownMenuItem = new() { Header = "Breakdown Dtp/Np" };
            BreakdownMenuItem.Click += BreakdownMethod;

            MenuItem RandomLineMenuItem = new() { Header = "Add Line" };
            RandomLineMenuItem.Click += RandomLineMethod;

            MenuItem AddDeletePointToPeakMenuItem = new() { Header = "Add/Delete Point to Peak" };
            AddDeletePointToPeakMenuItem.Click += AddDeletePointToPeak;

            MenuItem CreatePeakMenuItem = new() { Header = "Create Peak" };
            CreatePeakMenuItem.Click += CreatePeak;

            MenuItem VectorLineMenuItem = new() { Header = "Vector Line" };
            VectorLineMenuItem.Click += VectorLine;

            MenuItem DeleteCandleMenuItem = new() { Header = "Delete Candle" };
            DeleteCandleMenuItem.Click += DeleteUIElement;


            ContextMenu RightClickMenu = new();

            RightClickMenu.Items.Add(BreakdownMenuItem);
            RightClickMenu.Items.Add(RandomLineMenuItem);
            RightClickMenu.Items.Add(AddDeletePointToPeakMenuItem);
            RightClickMenu.Items.Add(CreatePeakMenuItem);
            RightClickMenu.Items.Add(VectorLineMenuItem);
            RightClickMenu.Items.Add(DeleteCandleMenuItem);

            RightClickMenu.IsOpen = true;
        }

        internal void PeakCustomMenu(object sender, EventArgs e)
        {
            MenuItem Zone1314MenuItem = new() { Header = "Zone 13/14" };
            Zone1314MenuItem.Click += Zone1314Method;

            MenuItem AddOrDeletePeakToSeriesPeaksMenuItem = new() { Header = "Add/Delete Peak to SeriesPeaks" };
            AddOrDeletePeakToSeriesPeaksMenuItem.Click += AddOrDeletePeakToSeriesPeaks;

            MenuItem SelectSeriesPeakMenuItem = new() { Header = "Select Series Peaks" };
            SelectSeriesPeakMenuItem.Click += SelectSeriesPeaks;

            MenuItem SelectPointPeakOrSeriesPeaksMenuItem = new() { Header = "Select Point Peak or Series Peaks" };
            SelectPointPeakOrSeriesPeaksMenuItem.Click += SelectPointFromPeak;

            MenuItem ShowPointsOfPeakMenuItem = new() { Header = "Show Points" };
            ShowPointsOfPeakMenuItem.Click += ShowPointsOfPeak;

            MenuItem DeletePeakMenuItem = new() { Header = "Delete Peak" };
            DeletePeakMenuItem.Click += DeleteUIElement;


            ContextMenu RightClickMenu = new();

            RightClickMenu.Items.Add(Zone1314MenuItem);
            RightClickMenu.Items.Add(AddOrDeletePeakToSeriesPeaksMenuItem);
            RightClickMenu.Items.Add(SelectSeriesPeakMenuItem);
            RightClickMenu.Items.Add(SelectPointPeakOrSeriesPeaksMenuItem);
            RightClickMenu.Items.Add(ShowPointsOfPeakMenuItem);
            RightClickMenu.Items.Add(DeletePeakMenuItem);

            RightClickMenu.IsOpen = true;
        }

        internal void TLineCustomMenu(object sender, EventArgs e)
        {
            MenuItem MenuAppointAdditionalTypeMenuItem = new() { Header = "Apoint additionl type" };

            MenuItem AppointRemoveCommonTypeMenuItem = new() { Header = "on/no Common" };
            AppointRemoveCommonTypeMenuItem.Click += AppointRemoveCommonType;
            MenuItem RemoveVectorTypeMenuItem = new() { Header = "no Vector type" };
            RemoveVectorTypeMenuItem.Click += RemoveVectorType;

            MenuAppointAdditionalTypeMenuItem.Items.Add(AppointRemoveCommonTypeMenuItem);
            MenuAppointAdditionalTypeMenuItem.Items.Add(RemoveVectorTypeMenuItem);


            MenuItem MenuSelectTypeMenuItem = new() { Header = "Select line Menu" };

            MenuItem MainTypeMenuItem = new() { Header = "Ln/Or/Re" };
            MainTypeMenuItem.Click += Select_Ln_Or_Re_Type;
            MenuItem HistoryTypeMenuItem = new() { Header = "History(...)" };
            HistoryTypeMenuItem.Click += Select_HistoryType;
            MenuItem CommonTypeMenuItem = new() { Header = "Common" };
            CommonTypeMenuItem.Click += Select_CommonType;
            MenuItem VectorTypeMenuItem = new() { Header = "Vector" };
            VectorTypeMenuItem.Click += Select_VectorType;
            MenuItem AllTLineMenuItem = new() { Header = "All" };
            AllTLineMenuItem.Click += Select_AllTLine;

            MenuSelectTypeMenuItem.Items.Add(MainTypeMenuItem);
            MenuSelectTypeMenuItem.Items.Add(HistoryTypeMenuItem);
            MenuSelectTypeMenuItem.Items.Add(CommonTypeMenuItem);
            MenuSelectTypeMenuItem.Items.Add(VectorTypeMenuItem);
            MenuSelectTypeMenuItem.Items.Add(AllTLineMenuItem);


            MenuItem HideNotSelectedShowAllMenuItem = new() { Header = "Hide not Selected / Show All" };
            HideNotSelectedShowAllMenuItem.Click += HideNotSelectedShowAll;

            MenuItem ControlBrightnessMenuItem = new() { Header = "Down/Up Brightness of Select Line" };
            ControlBrightnessMenuItem.Click += ControlBrightness;

            MenuItem ProximityCalculatingMenuItem = new() { Header = "Line Proximity" };
            ProximityCalculatingMenuItem.Click += ProximityCalculatingStart;

            MenuItem DeleteTLineMenuItem = new() { Header = "Delete TLine" };
            DeleteTLineMenuItem.Click += DeleteUIElement;


            ContextMenu RightClickMenu = new();

            RightClickMenu.Items.Add(MenuAppointAdditionalTypeMenuItem);
            RightClickMenu.Items.Add(MenuSelectTypeMenuItem);
            RightClickMenu.Items.Add(HideNotSelectedShowAllMenuItem);
            RightClickMenu.Items.Add(ControlBrightnessMenuItem);
            RightClickMenu.Items.Add(ProximityCalculatingMenuItem);
            RightClickMenu.Items.Add(DeleteTLineMenuItem);

            RightClickMenu.IsOpen = true;
        }

        #endregion

        public MainWindowVM()
        {
            SelectedObject = new();

            MW = (MainWindow)Application.Current.MainWindow;

            #region Команды

            OpenNewImageCommand = new Command(OnOpenNewImageCommandExecuted, CanOpenNewImageCommandExecute);
            PasteCtrlVCommand = new Command(OnPasteCtrlVCommandExecuted, CanPasteCtrlVCommandExecute);
            ResetZoomBorderCommand = new Command(OnResetZoomBorderCommandExecuted, CanResetZoomBorderCommandExecute);

            FindContoursCommand = new Command(OnFindContoursCommandExecuted, CanFindContoursCommandExecute);
            CalculatePeaksCommand = new Command(OnCalculatePeaksCommandExecuted, CanCalculatePeaksCommandExecute);
            SeriesPeaksCommand = new Command(OnSeriesPeaksCommandExecuted, CanSeriesPeaksCommandExecute);
            CalculateTLinesCommand = new Command(OnCalculateTLinesCommandExecuted, CanCalculateTLinesCommandExecute);
            RecordCommand = new Command(OnRecordCommandExecuted, CanRecordCommandExecute);

            ClearSelectedListCommand = new Command(OnClearSelectedListCommandExecuted, CanClearSelectedListCommandExecute);
            DeleteSelectedItemsCommand = new Command(OnDeleteSelectedItemsCommandExecuted, CanDeleteSelectedItemsCommandExecute);

            CheckedChangeCommand = new Command(OnCheckedChangeCommandExecuted, CanCheckedChangeCommandExecute);

            #endregion
        }

    }
}
