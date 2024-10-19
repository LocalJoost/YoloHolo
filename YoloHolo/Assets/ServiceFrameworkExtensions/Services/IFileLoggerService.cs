
using RealityCollective.ServiceFramework.Interfaces;

namespace ServiceFrameworkExtensions.Services
{
    public interface IFileLoggerService : IService
    {
        public void StartLogging();
        public void StopLogging();
    }
}