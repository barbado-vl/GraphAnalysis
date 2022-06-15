using GraphAnalysis.VM;
using System;
using System.Windows;


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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindowVM datacontext = (MainWindowVM)DataContext;

            brdrOne.MouseLeftButtonDown += datacontext.InitializationSelectGroup;
            brdrOne.MouseLeftButtonUp += datacontext.EndSelectGroup;

            brdrOne.KeyDown += datacontext.PushKeyU;
        }

        private void brdrOne_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            brdrOne.Focus();
        }
    }
}
