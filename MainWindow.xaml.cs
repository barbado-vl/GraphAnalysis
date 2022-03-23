using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GraphAnalysis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }



        #region Команды      постепенно переношу в MainWindowVM


        // !!!НАДО сначала с передачей параметров Canvas разобраться             для NewOpenCommand разбираюсь
        private void PasteCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                BitmapSource image = Clipboard.GetImage();
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(image));
                using var filestream = new FileStream("temp.jpg", FileMode.Create);
                encoder.Save(filestream);
                filestream.Close();

                string ImgFilename = "temp.jpg";

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.CacheOption = BitmapCacheOption.OnLoad;
                bImg.UriSource = new Uri("pack://siteoforigin:,,,/" + "temp.jpg", UriKind.Absolute);
                bImg.EndInit();

                ImageBrush BackgroundBrush = new ImageBrush();
                BackgroundBrush.ImageSource = bImg;

                //myCanvas.Width = image.Width;
                //myCanvas.Height = image.Height;
                //myCanvas.Background = BackgroundBrush;
            }
        }

    }
    #endregion

 }
