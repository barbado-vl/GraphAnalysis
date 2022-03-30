using GraphAnalysis.DataModel;
using GraphAnalysis.Infrastructure.Commands;

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GraphAnalysis.VM
{
    internal class MainWindowVM : BaseViewModel
    {
        #region Загаловок окна
        private string _Title = "GraphAnalysis";
        /// <summary> Заголовок окна </summary>
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
        #endregion

        #region ПАРАМЕТРЫ  

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

        /// <summary> Список Свечек по FindContoursCommand /// </summary>
        public List<Candle> Candles = new();
        public ObservableCollection<Polygon> Polygones;

        /// <summary> Список Пиков по ?????? /// </summary>
        public List<Peak> Peaks = new();

        #endregion

        #region КОМАНДЫ

        public Command OpenNewCommand { get; }
        private bool CanOpenNewCommandExecute(object p) => true;
        private void OnOpenNewCommandExecuted(object p)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";
                openFileDialog.InitialDirectory = @"F:\C#\CanvasTest";

                if (openFileDialog.ShowDialog() == true)
                {
                    MW.brdrOne.Reset();
                    MW.myCanvas.Children.Clear();

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

        public Command FindContoursCommand { get; }
        private bool CanFindContoursCommandExecute(object p) => true;
        private void OnFindContoursCommandExecuted(object p)
        {
            if (ImgFilename != null)
            {
                Candles = FindContours.ContourToCandle(ImgFilename);

                for (int x = 0; x < Candles.Count; x++)
                {
                    Candles[x].PContour.MouseLeftButtonDown += ClickChild;
                    Polygones.Add(Candles[x].PContour);
                    MW.myCanvas.Children.Add(Candles[x].PContour);
                }
            }
            else
            {
                MessageBox.Show("Open image file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Command CalculatePeaksCommand { get; }
        private bool CanCalculatePeaksCommandExecute(object p) => true;
        private void OnCalculatePeaksCommandExecuted(object p)         //отчистку холста организовать выборочную!!!        на потом
        {

            if (Candles.Any())
            {
                MW.myCanvas.Children.Clear();

                CalculatePeaks calculatePeaks = new(Candles, 9);  // 10 передавать из настроек!!!             на потом
                Peaks = calculatePeaks.Peaks;

                /// TEST        visual for me
                foreach (Peak peak in Peaks)
                {
                    TextBlock Text = new();
                    Text.Text = peak.Mass.ToString();
                    Text.Foreground = Brushes.Red;

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

        public Command ResetZoomBorderCommand { get; }
        private bool CanResetZoomBorderCommandExecute(object p) => true;
        private void OnResetZoomBorderCommandExecuted(object p)
        {
            MW.brdrOne.Reset();
        }

        #endregion


        public MainWindowVM()
        {
            MW = (MainWindow)Application.Current.MainWindow;
            Polygones = new ObservableCollection<Polygon>();

            #region Команды

            OpenNewCommand = new Command(OnOpenNewCommandExecuted, CanOpenNewCommandExecute);
            FindContoursCommand = new Command(OnFindContoursCommandExecuted, CanFindContoursCommandExecute);
            ResetZoomBorderCommand = new Command(OnResetZoomBorderCommandExecuted, CanResetZoomBorderCommandExecute);
            CalculatePeaksCommand = new Command(OnCalculatePeaksCommandExecuted, CanCalculatePeaksCommandExecute);

            #endregion
        }


        // ______//___________________//__________________//_______________//__________________//_______________




        private void ClickChild(object sender, RoutedEventArgs e)
        {

        }


        public bool direction;

        public List<Polygon> PolygonCandles;



        // ____________________________________________________________________________________________



    }
}
