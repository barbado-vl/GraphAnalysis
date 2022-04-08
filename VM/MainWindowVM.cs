using GraphAnalysis.DataModel;
using GraphAnalysis.Infrastructure.Commands;

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        #region Параметры окна
        
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

        #region Параметры VM
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

        /// <summary> Список Выделенных детей холста /// </summary>
        public List<object> SelectedObject = new();



        #endregion

        #region Параметры логической модели

        /// <summary> Списки данных /// </summary>
        public List<Candle> Candles = new();
        public List<Peak> Peaks = new();
        public List<TLine> TLines = new();

        /// <summary> Параметры настраиваемые в ручную /// </summary>
        public int MinSizePeak = 9;
        public string direction = "0";

        #endregion

        #region Команды основной логики + VM

        /// <summary> Новое изображение </summary>
        public Command OpenNewImageCommand { get; }
        private bool CanOpenNewImageCommandExecute(object p) => true;
        private void OnOpenNewImageCommandExecuted(object p)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";
                openFileDialog.InitialDirectory = @"F:\C#\CanvasTest";

                if (openFileDialog.ShowDialog() == true)
                {
                    ClearData();

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
                ClearData();

                BitmapSource image = Clipboard.GetImage();
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(image));
                using var filestream = new FileStream("temp.jpg", FileMode.Create);
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

        private void ClearData()
        {
            MW.brdrOne.Reset();
            MW.myCanvas.Children.Clear();
            Candles.Clear();
            Peaks.Clear();
            TLines.Clear();
        }

        /// <summary> Расчеты </summary>
        public Command FindContoursCommand { get; }
        private bool CanFindContoursCommandExecute(object p) => true;
        private void OnFindContoursCommandExecuted(object p)
        {
            Candles.Clear();
            Peaks.Clear();
            TLines.Clear();

            if (ImgFilename != null)
            {
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
                CalculatePeaks calculatePeaks = new(Candles, MinSizePeak);
                Peaks = calculatePeaks.Peaks;

                MW.checkBox_Peaks.IsChecked = true;

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

                    //foreach (string i in peak.CandlesId)
                    //{
                    //    Candle fff = Candles.First(a => a.id == i);

                    //    Polygon fff = MW.myCanvas.Children.OfType<Polygon>().First(a => a.Uid == i);

                    //    fff.Contour.StrokeThickness = 3;          
                    //    // MW.myCanvas.Children.Add(fff.Contour);             //  НЕ ПРОХОДИТ -- холст еще не обновился... Clear не прошла еще
                    //}
                }

            }
            else
            {
                MessageBox.Show("Use Find Contours to get Candles befor calculate Peaks", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        /// <summary> Вспомогательные </summary>
        public Command ResetZoomBorderCommand { get; }
        private bool CanResetZoomBorderCommandExecute(object p) => true;
        private void OnResetZoomBorderCommandExecuted(object p)
        {
            MW.brdrOne.Reset();
        }

        #endregion

        #region Команды и методы представлений модели (Polygon, Line, TextBlock)

        /// <summary> Выделение/Снятие выделения щелчком ЛКМ по представлению /// </summary>
        private void SelectChild(object sender, RoutedEventArgs e)
        {
            if (SelectedObject.Count == 0 || sender.GetType() == SelectedObject[0].GetType())
            {
                if (sender.GetType() == typeof(Polygon))
                {
                    Polygon polygon = (Polygon)sender;

                    SolidColorBrush myBrush = new(Colors.Red);
                    myBrush.Opacity = 1;

                    polygon.Fill = myBrush;

                    SelectedObject.Add(polygon);

                    polygon.MouseLeftButtonDown -= SelectChild;
                    polygon.MouseLeftButtonDown += UnSelectChild;

                    // Добавить обработку ПКМ
                }
                else if (sender.GetType() == typeof(Line))
                {

                }
                else if (sender.GetType() == typeof(TextBlock))
                {
                    TextBlock text = (TextBlock)sender;

                    text.FontWeight = FontWeights.UltraBold;

                    SelectedObject.Add(text);

                    text.MouseLeftButtonDown -= SelectChild;
                    text.MouseLeftButtonDown += UnSelectChild;
                }
            }
            else
            {
                MessageBox.Show("Тип данного объекта не соответствует типу уже выделенных объектов", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UnSelectChild(object sender, RoutedEventArgs e)
        {
            if (sender.GetType() == typeof(Polygon))
            {
                Polygon polygon = (Polygon)sender;

                SolidColorBrush myBrush = new();
                myBrush.Opacity = 0;

                polygon.Fill = myBrush;

                SelectedObject.Remove(polygon);

                polygon.MouseLeftButtonDown -= UnSelectChild;
                polygon.MouseLeftButtonDown += SelectChild;

                // отключить обработку ПКМ
            }
            else if (sender.GetType() == typeof(Line))
            {

            }
            else if (sender.GetType() == typeof(TextBlock))
            {
                TextBlock text = (TextBlock)sender;

                text.FontWeight = FontWeights.Normal;

                SelectedObject.Remove(text);

                text.MouseLeftButtonDown -= UnSelectChild;
                text.MouseLeftButtonDown += SelectChild;
            }
        }

        /// <summary> Полное снятие выделения /// </summary>
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

        /// <summary> Удаление выделенных обпредставлений /// </summary>
        public Command DeleteSelectedItemsCommand { get; }
        private bool CanDeleteSelectedItemsCommandExecute(object p) => true;
        private void OnDeleteSelectedItemsCommandExecuted(object p)
        {
            if (SelectedObject.Any())
            {
                for (int n = SelectedObject.Count - 1; n >= 0; n--)
                {
                    if (SelectedObject[n].GetType() == typeof(Polygon))
                    {
                        if (Peaks.Count == 0)
                        {
                            Polygon polygon = (Polygon)SelectedObject[n];
                            MW.myCanvas.Children.Remove(polygon);
                            Candles.Remove(Candles.First(a => a.id == polygon.Uid));
                            SelectedObject.Remove(SelectedObject[n]);
                        }
                        else
                        {
                            StatusMessage = "ВНИМАНИЕ: нельзя удалять свечки после расчета пиков";
                            break;
                        }
                    }
                    else if (SelectedObject[n].GetType() == typeof(Line))
                    {
                        Line line = (Line)SelectedObject[n];

                        if (line.Uid == "")      //    доделать условие
                        {
                            MW.myCanvas.Children.Remove(line);
                            TLines.Remove(TLines.First(a => a.Id == line.Uid));
                            SelectedObject.Remove(SelectedObject[n]);
                        }
                        else
                        {
                            //  Message      эти линиии удалять нельзя
                            break;
                        }
                    }
                    else if (SelectedObject[n].GetType() == typeof(TextBlock))
                    {
                        if (TLines.Count == 0)
                        {
                            TextBlock text = (TextBlock)SelectedObject[n];

                            MW.myCanvas.Children.Remove(text);
                            Peaks.Remove(Peaks.First(a => a.Id == text.Uid));
                            SelectedObject.Remove(SelectedObject[n]);
                        }
                        else
                        {
                            StatusMessage = "ВНИМАНИЕ: нельзя удалять пики после расчета линий";
                            break;
                        }
                    }
                }
            }
        }

        /// <summary> Отобразить/Скрыть, для меню управления View /// </summary>
        public Command CheckedChangeCommand { get; }
        private bool CanCheckedChangeCommandExecute(object p) => true;
        private void OnCheckedChangeCommandExecuted(object p)
        {
            CheckBox checkbox = (CheckBox)p;

            /// Устанвка флага IsCheked происходит при нажатии на элемент управления
            if (Candles.Any())
            {
                if (checkbox.Name == "checkBox_CandleAll")
                {
                    if (checkbox.IsChecked == true)
                    {
                        for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                        {
                            if (MW.myCanvas.Children[x].GetType() == typeof(Polygon))
                            {
                                Polygon polygon = (Polygon)MW.myCanvas.Children[x];
                                polygon.StrokeThickness = 1;
                            }
                        }
                    }
                    else
                    {
                        for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                        {
                            if (MW.myCanvas.Children[x].GetType() == typeof(Polygon))
                            {
                                Polygon polygon = (Polygon)MW.myCanvas.Children[x];
                                polygon.StrokeThickness = 0;
                            }
                        }
                    }
                }
                if (checkbox.Name == "checkBox_CandleByPeaks")
                {
                    if (checkbox.IsChecked == true) // 
                    {

                    }
                    else
                    {

                    }
                }
                if (checkbox.Name == "checkBox_CandleByLines")
                {
                    if (checkbox.IsChecked == true) // 
                    {

                    }
                    else
                    {

                    }
                }   // не сделано

            }
            if (Peaks.Any() && checkbox.Name == "checkBox_Peaks")
            {
                if (checkbox.IsChecked == true)
                {
                    for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                    {
                        if (MW.myCanvas.Children[x].Uid.Split("_").First() == "peak")
                        {
                            TextBlock textblock = (TextBlock)MW.myCanvas.Children[x];
                            textblock.Text = MW.myCanvas.Children[x].Uid.Split("_").Last();
                        }
                    }
                }
                else
                {
                    for (int x = 0; x < MW.myCanvas.Children.Count; x++)
                    {
                        if (MW.myCanvas.Children[x].Uid.Split("_").First() == "peak")
                        {
                            TextBlock textblock = (TextBlock)MW.myCanvas.Children[x];
                            textblock.Text = "";
                        }
                    }
                }
            }
            if (TLines.Any())
            {
                // Сложно(через наследование типов??) или много(перебором) ... типов много
            }
        }

        #endregion

        public MainWindowVM()
        {
            MW = (MainWindow)Application.Current.MainWindow;

            #region Команды

            OpenNewImageCommand = new Command(OnOpenNewImageCommandExecuted, CanOpenNewImageCommandExecute);
            FindContoursCommand = new Command(OnFindContoursCommandExecuted, CanFindContoursCommandExecute);
            CalculatePeaksCommand = new Command(OnCalculatePeaksCommandExecuted, CanCalculatePeaksCommandExecute);
            ResetZoomBorderCommand = new Command(OnResetZoomBorderCommandExecuted, CanResetZoomBorderCommandExecute);
            ClearSelectedListCommand = new Command(OnClearSelectedListCommandExecuted, CanClearSelectedListCommandExecute);
            DeleteSelectedItemsCommand = new Command(OnDeleteSelectedItemsCommandExecuted, CanDeleteSelectedItemsCommandExecute);
            CheckedChangeCommand = new Command(OnCheckedChangeCommandExecuted, CanCheckedChangeCommandExecute);
            PasteCtrlVCommand = new Command(OnPasteCtrlVCommandExecuted, CanPasteCtrlVCommandExecute);

            #endregion
        }


        // ______//___________________//__________________//_______________//__________________//_______________


    }
}
