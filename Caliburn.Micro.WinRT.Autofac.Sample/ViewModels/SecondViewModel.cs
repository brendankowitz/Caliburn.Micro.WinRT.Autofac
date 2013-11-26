using System;
using System.Windows.Input;
using Caliburn.Micro.WinRT.Autofac.Sample.Entities;
using Caliburn.Micro.WinRT.Autofac.Sample.Infrastructure;

namespace Caliburn.Micro.WinRT.Autofac.Sample.ViewModels
{
    public class SecondViewModel : Screen
    {
        private string _timeCreated;

        public SecondViewModel(INavigationService navigationService)
        {
            GoBackCommand = new ActionCommand(x => navigationService.GoBack(), x => navigationService.CanGoBack);
            ThirdView = new ActionCommand(x => navigationService.NavigateToViewModel<ThirdViewModel>());
            TimeCreated = DateTime.Now.ToString();
        }


        public string TimeCreated
        {
            get { return _timeCreated; }
            set
            {
                if (value == _timeCreated) return;
                _timeCreated = value;
                NotifyOfPropertyChange(() => TimeCreated);
            }
        }

        public ICommand GoBackCommand { get; set; }
        public ICommand ThirdView { get; set; }
    }
}