using System.ComponentModel;

namespace TensileTestingApp
{

    public class MyViewModel : INotifyPropertyChanged
    {
        private int _intValue;



        public int IntValue
        {
            get => _intValue;
            set { _intValue = value; OnPropertyChanged(nameof(IntValue)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

}