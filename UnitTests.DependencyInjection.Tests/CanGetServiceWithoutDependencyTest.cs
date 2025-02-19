using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace UnitTests.DependencyInjection.Tests;

[TestClass]
public class CanGetServiceWithoutDependencyTest
{
    public interface IVehicle { string Color { get; set; } bool Run(); }
    public interface IVehiclePower { }
    public interface IPainter { void Paint(IVehicle vehicle, string color) => vehicle.Color = color; }
    public interface IUser { string Name { get; set; } }

    public class Vehicle : IVehicle
    {
        public string Color { get; set; } = null!;

        public bool Run()
        {
            return true;
        }
    }

    public class Painter : IPainter
    {
        public Painter(IUser user)
        {
            User = user;
        }

        public IUser User { get; }
    }

    public class Outsource : IUser
    {
        public string Name { get; set; } = null!;
    }


    public class VehicleFactory
    {
        public IVehicle Vehicle { get; }
        public IPainter Painter { get; }

        public VehicleFactory(IVehicle vehicle, IPainter painter)
        {
            Vehicle = vehicle;
            Painter = painter;
        }

        public bool TestRunVehicle()
        {
            var result = Vehicle.Run();

            return result;
        }

        public void PaintVehicle(string color)
        {
            Painter.Paint(Vehicle, color);
        }
    }

    [TestMethod]
    public void CanResolveInstanceWithNotRegisteredDependency()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<VehicleFactory>();

        var services = serviceCollection.BuildTestServiceProvider();

        var vehicleFactory = services.GetRequiredService<VehicleFactory>();

        Assert.IsNotNull(vehicleFactory);
        Assert.IsNull(vehicleFactory.Vehicle);
        Assert.IsNull(vehicleFactory.Painter);
    }

    [TestMethod]
    public void CanResolveInstanceWithDependencyInActivator()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<VehicleFactory>();

        IVehicle vehicleActivator()
        {
            return new Vehicle();
        }

        var services = serviceCollection.BuildTestServiceProvider(vehicleActivator);

        var vehicleFactory = services.GetRequiredService<VehicleFactory>();

        Assert.IsNotNull(vehicleFactory);
        Assert.IsNotNull(vehicleFactory.Vehicle);
        Assert.IsNull(vehicleFactory.Painter);

        var result = vehicleFactory.TestRunVehicle();

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CanRunInstanceWithMockingDependencyInActivator()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<VehicleFactory>();

        IVehicle vehicleActivator()
        {
            var mockVehicle = new Mock<IVehicle>();
            mockVehicle.Setup(a => a.Run()).Returns(true);

            return mockVehicle.Object;
        }

        var services = serviceCollection.BuildTestServiceProvider(vehicleActivator);

        var vehicleFactory = services.GetRequiredService<VehicleFactory>();

        var result = vehicleFactory.TestRunVehicle();

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CanRunInstanceWithMockingDependencyInActivatorWithServiceProviderAccess()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<VehicleFactory>();
        serviceCollection.AddScoped<IUser>(sp => new Outsource { Name = "Vince" });

        IPainter painterActivator(IServiceProvider services)
        {
            return new Painter(services.GetRequiredService<IUser>());
        }

        var services = serviceCollection.BuildTestServiceProvider(painterActivator);

        var vehicleFactory = services.GetRequiredService<VehicleFactory>();

        Assert.IsNotNull(vehicleFactory);
        Assert.IsNull(vehicleFactory.Vehicle, "Vehicle should be null");
        Assert.IsNotNull(vehicleFactory.Painter);

        Assert.IsInstanceOfType<Painter>(vehicleFactory.Painter);

        var painter = vehicleFactory.Painter as Painter;

        Assert.IsNotNull(painter);
        Assert.IsNotNull(painter.User);

        Assert.IsInstanceOfType<Outsource>(painter.User);

        var user = painter.User as Outsource;
        Assert.IsNotNull(user);

        Assert.AreEqual("Vince", user.Name);

    }
}