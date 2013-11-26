using System;

namespace Caliburn.Micro.WinRT.Autofac.StorageHandlers
{
    public class SuspensionManagerException : Exception
    {
        public SuspensionManagerException(Exception exception) : base(exception.Message, exception)
        {
        }
    }
}