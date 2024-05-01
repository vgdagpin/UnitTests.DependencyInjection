# UnitTests.DependencyInjection

Using ServiceCollection and Dependency Injection in .NET Core in your unittesting, this will resolve services in constructor if its registered else it will just resolve as null.   

```csharp
public class VehicleFactory(IVehicle vehicle, IPainter painter)
```
We have this constructor that accepts IVehicle and IPainter, if we have registered IVehicle and IPainter in our service collection then it will resolve the instance of IVehicle and IPainter but in our unittest we only mock IVehicle, originally the DependencyInjection will throw an exception because it cannot resolve IPainter but with this extension it will just resolve as null.


```csharp
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
```