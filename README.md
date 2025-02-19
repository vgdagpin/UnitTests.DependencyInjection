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
```