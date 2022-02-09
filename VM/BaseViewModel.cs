using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace GraphAnalysis.VM
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)   // [CallerMemberName]   необязательно,  зачем оно?
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
