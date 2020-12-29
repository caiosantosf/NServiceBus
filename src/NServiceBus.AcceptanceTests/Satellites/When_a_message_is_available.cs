﻿namespace NServiceBus.AcceptanceTests.Satellites
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Transport;

    public class When_a_message_is_available : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Send(Endpoint.MySatelliteFeature.Address, new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(context.MessageReceived);
            // In the future we want the transport transaction to be an explicit
            // concept in the persisters API as well. Adding transport transaction
            // to the context will not be necessary at that point.
            // See GitHub issue #4047 for more background information.
            Assert.True(context.TransportTransactionAddedToContext);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public bool TransportTransactionAddedToContext { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MySatelliteFeature : Feature
            {
                public MySatelliteFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("MySatellite");
                    var endpointInstance = satelliteLogicalAddress.EndpointInstance;
                    var queueAddress = new QueueAddress(
                        endpointInstance.Endpoint,
                        endpointInstance.Discriminator,
                        endpointInstance.Properties,
                        satelliteLogicalAddress.Qualifier);

                    var satelliteAddress = context.Settings.Get<TransportDefinition>().ToTransportAddress(queueAddress);

                    context.AddSatelliteReceiver("Test satellite", satelliteAddress, PushRuntimeSettings.Default,
                        (c, ec) => RecoverabilityAction.MoveToError(c.Failed.ErrorQueue),
                        (builder, messageContext) =>
                        {
                            var testContext = builder.GetService<Context>();
                            testContext.MessageReceived = true;
                            testContext.TransportTransactionAddedToContext = ReferenceEquals(messageContext.Extensions.Get<TransportTransaction>(), messageContext.TransportTransaction);
                            return Task.FromResult(true);
                        });

                    Address = satelliteAddress;
                }

                public static string Address;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}