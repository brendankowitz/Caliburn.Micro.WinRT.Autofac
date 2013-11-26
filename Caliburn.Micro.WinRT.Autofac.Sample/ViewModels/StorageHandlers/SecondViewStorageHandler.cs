using Caliburn.Micro.WinRT.Autofac.StorageHandlers;

namespace Caliburn.Micro.WinRT.Autofac.Sample.ViewModels.StorageHandlers
{
    public class SecondViewStorageHandler : StorageHandler<SecondViewModel>
    {
        public override void Configure()
        {
            Property(x => x.DisplayName).InLocalStorage();
            Property(x => x.TimeCreated).InLocalStorage();
        }
    }
}