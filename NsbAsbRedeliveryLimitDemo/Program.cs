using Azure.Messaging.ServiceBus;
using NServiceBus.Pipeline;

var configuration = new EndpointConfiguration("RedeliveryLimitsDemo");

var connectionString = Environment.GetEnvironmentVariable("AsbConnectionString")!;
var azureServiceBusTransport = new AzureServiceBusTransport(connectionString);
configuration.UseTransport(azureServiceBusTransport);

configuration.EnableInstallers();

configuration.Recoverability().Immediate(s => s.NumberOfRetries(int.MaxValue));

configuration.Pipeline.Register(new InterceptingBehavior(), "Prevents endless retries due to process crashes");
configuration.Recoverability().AddUnrecoverableException<MessageDeliveriesExceededException>();

var endpoint = await Endpoint.Start(configuration);

Console.WriteLine("Press [s] to send a message. Press [ESC] to stop the endpoint.");
while (true)
{
    var key = Console.ReadKey();
    switch (key.Key)
    {
        case ConsoleKey.S:
            await endpoint.SendLocal(new DemoMessage());
            break;
        case ConsoleKey.Escape:
            await endpoint.Stop();
            return;
    }
    Console.WriteLine();
}

class InterceptingBehavior : Behavior<ITransportReceiveContext>
{
    public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
    {
        var asbMessage = context.Extensions.Get<ServiceBusReceivedMessage>();
        if (asbMessage.DeliveryCount >= 5)
        {
            throw new MessageDeliveriesExceededException();
        }
        
        return next();
    }
}

class DemoMessageHandler : IHandleMessages<DemoMessage>
{
    public async Task Handle(DemoMessage message, IMessageHandlerContext context)
    {
        // Simulate the actual behavior by exiting the process but it will take much longer to demo the behavior due to 
        //Environment.Exit(42);
        await Task.Delay(500);
        throw new Exception("demo");
    }
}

class DemoMessage : ICommand
{
}

class MessageDeliveriesExceededException : Exception;