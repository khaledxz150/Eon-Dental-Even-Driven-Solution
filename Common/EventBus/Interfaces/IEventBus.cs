﻿using EventBus.Abstracts;
using EventBus.Handlers;

namespace EventBus.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event) where T : IntegrationEvent;
        Task SubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        Task UnsubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    }
}
