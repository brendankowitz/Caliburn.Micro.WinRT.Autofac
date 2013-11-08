using System.Windows.Input;
using Caliburn.Micro.WinRT.Autofac.Sample.Infrastructure;

namespace Caliburn.Micro.WinRT.Autofac.Sample.ViewModels
{
    public class SecondViewModel : Screen
    {
        public SecondViewModel(INavigationService navigationService)
        {
            GoBackCommand = new ActionCommand(x => navigationService.GoBack(), x => navigationService.CanGoBack);
            ThirdView = new ActionCommand(x => navigationService.NavigateToViewModel<ThirdViewModel>());
        }

        public ICommand GoBackCommand { get; set; }
        public ICommand ThirdView { get; set; }
    }
}