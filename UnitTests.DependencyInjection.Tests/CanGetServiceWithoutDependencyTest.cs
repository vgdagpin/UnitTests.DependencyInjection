using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace UnitTests.DependencyInjection.Tests;

[TestClass]
public class CanGetServiceWithoutDependencyTest
{
    public interface IVehicle { string Color { get; set; } bool Run(); }
    public interface IVehiclePower { }
    public interface IPainter { void Paint(IVehicle vehicle, string color) => vehicle.Color = color; }

    public class VehicleFactory(IVehicle vehicle, IPainter painter)
    {
        private readonly IVehicle p_Vehicle = vehicle;
        private readonly IPainter p_Painter = painter;

        public bool TestRunVehicle()
        {
            return p_Vehicle.Run();
        }

        public void PaintVehicle(string color)
        {
            p_Painter.Paint(vehicle, color);
        }
    }

    [TestMethod]
    public void CanResolveInstanceWithNotRegisteredDependency()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<VehicleFactory>();

        var mockVehicle = new Mock<IVehicle>();
        mockVehicle.Setup(a => a.Run()).Returns(true);
        serviceCollection.AddScoped(sp => mockVehicle.Object);

        var services = serviceCollection.BuildTestServiceProvider();

        var vehicleFactory = services.GetRequiredService<VehicleFactory>();

        var result = vehicleFactory.TestRunVehicle();

        Assert.IsTrue(result);
    }
}