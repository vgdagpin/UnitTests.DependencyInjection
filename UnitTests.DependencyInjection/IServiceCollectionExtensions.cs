using Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;

namespace UnitTests.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static TestServiceProvider? TestServiceProvider;

        public static TestServiceProvider BuildTestServiceProvider(this IServiceCollection serviceCollection)
            => TestServiceProvider ??= new TestServiceProvider(serviceCollection.BuildServiceProvider(), serviceCollection);
    }

    public sealed class TestServiceProvider : IServiceProvider
    {
        public IServiceProvider ServiceProvider { get; }
        public IServiceCollection Services { get; }

        public TestServiceProvider(IServiceProvider serviceProvider, IServiceCollection services)
        {
            ServiceProvider = serviceProvider;
            Services = services;
        }

        public object? GetService(Type serviceType)
        {
            if (TryGetTestService(this, serviceType, out var result))
            {
                return result;
            }
            else
            {
                return GetServiceForceInstance(serviceType);
            }
        }

        public object? GetServiceForceInstance(Type serviceType)
        {
            if (!serviceType.IsAbstract && !serviceType.IsInterface)
            {
                var constructors = serviceType.GetConstructors();

                if (constructors.Length > 1)
                {
                    throw new Exception("Multiple constructors found");
                }

                if (constructors.Length == 0)
                {
                    return Activator.CreateInstance(serviceType);
                }

                var ctorParameters = constructors[0].GetParameters()
                    .Select(a => GetTestServiceOrNull(this, a.ParameterType))
                    .ToArray();

                return Activator.CreateInstance(serviceType, ctorParameters) ?? throw new ArgumentNullException("Instance of " + serviceType.Name);
            }

            throw new ArgumentException("Unable to create object");
        }

        static object? GetTestServiceOrNull(TestServiceProvider serviceProvider, Type serviceType)
        {
            // lets try getting it first from registered services
            // if not found or cannot be instantiated due to dependencies
            // then lets try instantiating it using this helper
            try
            {
                var services = serviceProvider.ServiceProvider.GetServices(serviceType).ToArray();

                if (services.Length == 1)
                {
                    return services.First();
                }
            }
            catch
            {
                foreach (var item in ((ServiceCollection)serviceProvider.Services))
                {
                    if (item.ServiceType != serviceType)
                    {
                        continue;
                    }

                    if (item.ServiceType.IsInterface)
                    {
                        if (item.ImplementationType != null)
                        {
                            return serviceProvider.GetServiceForceInstance(item.ImplementationType);
                        }
                    }
                    else
                    {
                        if (!item.ServiceType.IsAbstract)
                        {
                            return serviceProvider.GetServiceForceInstance(item.ServiceType);
                        }
                    }
                }
            }

            return null;
        }

        static bool TryGetTestService(TestServiceProvider serviceProvider, Type serviceType, out object? result)
        {
            result = GetTestServiceOrNull(serviceProvider, serviceType);

            return result != null;
        }
    }
}