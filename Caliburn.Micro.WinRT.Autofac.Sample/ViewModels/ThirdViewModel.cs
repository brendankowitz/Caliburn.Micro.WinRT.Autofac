using System.Windows.Input;
using Caliburn.Micro.WinRT.Autofac.Sample.Infrastructure;

namespace Caliburn.Micro.WinRT.Autofac.Sample.ViewModels
{
    public class ThirdViewModel : Screen
    {
        public ThirdViewModel(INavigationService navigationService)
        {
            GoBackCommand = new ActionCommand(x => navigationService.GoBack(), x => navigationService.CanGoBack);
        }

        public ICommand GoBackCommand { get; set; }
    }
}