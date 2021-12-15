using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCat_Motion_ADS.Core;
namespace TwinCat_Motion_ADS.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        
        public RelayCommand NcAxisCommand { get; set; }
        public RelayCommand AirAxisCommand { get; set; }

        public AirAxisViewModel AirAxisVm { get; set; }
        public NcAxisViewModel NcAxisVm { get; set; }

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value;
                OnPropertyChanged();
            }
        }


        

        public MainViewModel()
        {

            AirAxisVm = new AirAxisViewModel();
            NcAxisVm = new NcAxisViewModel();
            CurrentView = NcAxisVm;
            NcAxisCommand = new RelayCommand(o=> 
            {
                CurrentView = NcAxisVm;
            });
            AirAxisCommand = new RelayCommand(o =>
            {
                CurrentView = AirAxisVm;
            });


        }
    }
}
