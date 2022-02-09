using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;

using GraphAnalysis.DataModel;
using GraphAnalysis.VM;

namespace GraphAnalysis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ImgFilename;

        public MainWindow()
        {
            InitializeComponent();

        }




        private void MenuItemNew_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                brdrOne.Reset();

                ImgFilename = openFileDialog.FileName;

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(openFileDialog.FileName);
                bi.EndInit();

                myCanvas.Width = bi.Width;
                myCanvas.Height = bi.Height;

                ImageBrush ib = new ImageBrush();
                ib.ImageSource = bi;
                myCanvas.Background = ib;
            }
            else
            {
                MessageBox.Show("Incorrect action, try again please", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void ClickFindContours(object sender, RoutedEventArgs e)
        {
            FindContours FC = new FindContours(ImgFilename);

        }


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

                BitmapImage bImg = new BitmapImage();
                bImg.BeginInit();
                bImg.CacheOption = BitmapCacheOption.OnLoad;
                bImg.UriSource = new Uri("pack://siteoforigin:,,,/" + "temp.jpg", UriKind.Absolute);
                bImg.EndInit();

                ImageBrush BackgroundBrush = new ImageBrush();
                BackgroundBrush.ImageSource = bImg;

                myCanvas.Width = image.Width;
                myCanvas.Height = image.Height;
                myCanvas.Background = BackgroundBrush;
            }
        }
    }


    #region  Converter остался
    // XAML code
    //<Canvas.Background>
    //     <ImageBrush ImageSource = "{Binding Source={StaticResource CanvasBG}, Path=TempImgFile,
    //            UpdateSourceTrigger=PropertyChanged, Converter={StaticResource StringToImageConverter}}"/>
    //</ Canvas.Background >



    public class StringToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof(string))
            {
                throw new InvalidOperationException("The value must be a string");
            }

            return new BitmapImage(new Uri((string)value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    #endregion   
}
