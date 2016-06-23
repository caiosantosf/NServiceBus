﻿namespace NServiceBus.AcceptanceTests.Serialization
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_sanitizing_xml_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_sanitize_illegal_characters_from_messages()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointSanitizingInput>(e => e
                    .When(session => session.SendLocal(new SimpleMessage {Value = "Hello World!"})))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(context.Input, Is.EqualTo("Hello World!"));
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string Input { get; set; }
        }

        class EndpointSanitizingInput : EndpointConfigurationBuilder
        {
            public EndpointSanitizingInput()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<XmlSerializer>().SanitizeInput();
                    c.RegisterComponents(r => r.ConfigureComponent<IncomingMutator>(DependencyLifecycle.SingleInstance));
                });
            }

            class SimpleMessageHandler : IHandleMessages<SimpleMessage>
            {
                public SimpleMessageHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(SimpleMessage message, IMessageHandlerContext context)
                {
                    scenarioContext.MessageReceived = true;
                    scenarioContext.Input = message.Value;

                    return Task.FromResult(0);
                }

                Context scenarioContext;
            }

            public class IncomingMutator : IMutateIncomingTransportMessages
            {
                public IncomingMutator(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    //TODO: add illegal character to body content
//                    var body = Encoding.UTF8.GetString(context.Body);
//                    char x = \u10FFFF;
//
//                    context.Body = Encoding.UTF8.GetBytes(invalidXmlContext);

                    return Task.FromResult(0);
                }

                Context scenarioContext;
            }
        }

        class SimpleMessage : ICommand
        {
            public string Value { get; set; }
        }
    }
}