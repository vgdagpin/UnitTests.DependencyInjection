using Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;

namespace UnitTests.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static TestServiceProvider? TestServiceProvider;

        public static IServiceProvider BuildTestServiceProvider(this IServiceCollection serviceCollection, params Delegate[] activators)
            => TestServiceProvider ??= new TestServiceProvider(serviceCollection.BuildServiceProvider(), serviceCollection, activators);
    }

    public sealed class TestServiceProvider : IServiceProvider
    {
        private readonly Delegate[] activators;

        public IServiceProvider ServiceProvider { get; }
        public IServiceCollection Services { get; }

        public TestServiceProvider(IServiceProvider serviceProvider, IServiceCollection services, params Delegate[] activators)
        {
            ServiceProvider = serviceProvider;
            Services = services;
            this.activators = activators;
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

                foreach (var activator in activators)
                {
                    if (activator.Method.ReturnType == serviceType)
                    {
                        var parameters = activator.Method.GetParameters();
                        var firstParam = activator.Method.GetParameters().FirstOrDefault();

                        if (firstParam != null && firstParam.ParameterType == typeof(IServiceProvider))
                        {
                            return activator.DynamicInvoke(new object?[] { this }) ?? throw new ArgumentNullException("Instance of " + serviceType.Name);
                        }

                        return activator.DynamicInvoke(new object?[] { }) ?? throw new ArgumentNullException("Instance of " + serviceType.Name);
                    }
                }

                return Activator.CreateInstance(serviceType, ctorParameters) ?? throw new ArgumentNullException("Instance of " + serviceType.Name);
            }
            else
            {
                foreach (var activator in activators)
                {
                    if (activator.Method.ReturnType == serviceType)
                    {
                        var parameters = activator.Method.GetParameters();
                        var firstParam = activator.Method.GetParameters().FirstOrDefault();

                        if (firstParam != null && firstParam.ParameterType == typeof(IServiceProvider))
                        {
                            return activator.DynamicInvoke(new object?[] { this }) ?? throw new ArgumentNullException("Instance of " + serviceType.Name);
                        }

                        return activator.DynamicInvoke(new object?[] { }) ?? throw new ArgumentNullException("Instance of " + serviceType.Name);
                    }
                }
            }

            return null;
        }

        object? GetTestServiceOrNull(TestServiceProvider serviceProvider, Type serviceType)
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


                return GetServiceForceInstance(serviceType);
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

        bool TryGetTestService(TestServiceProvider serviceProvider, Type serviceType, out object? result)
        {
            result = GetTestServiceOrNull(serviceProvider, serviceType);

            return result != null;
        }
    }
}