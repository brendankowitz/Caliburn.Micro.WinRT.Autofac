using System;

namespace Caliburn.Micro.WinRT.Autofac.StorageHandlers
{
    public interface IActivateComponent
    {
        event Action<object> Activated;
    }
}