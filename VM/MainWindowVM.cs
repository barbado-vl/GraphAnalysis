using GraphAnalysis.DataModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GraphAnalysis.VM
{
    public class MainWindowVM : BaseViewModel
    {
        public MainWindowVM()
        {
            
        }

        /// <summary>
        /// Update Canvas Background              НЕ используется
        /// </summary>
        private string _tempImgFile = "temp.jpg";
        public string TempImgFile
        {
            get
            {
                return _tempImgFile;
            }
            set
            {
                _tempImgFile = value;
                OnPropertyChanged(nameof(TempImgFile));
            }
        }


        private RelayCommand _openCommand;
        public RelayCommand OpenCommand
        {
            get
            {
                return _openCommand ??
                  (_openCommand = new RelayCommand(obj =>
                  {
                      FindContours FC = new FindContours("temp.jpg");
                  }));
            }
        }

        public bool direction;

        public List<Polygon> PolygonCandles;
        public List<Candle> Candles;



    }
}
