// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Events
{
    public abstract class EventValue : JsonValue
    {
        public string @event;
    }

    public abstract class Event<T> : Response<T>, IEvent
        where T : EventValue, new()
    {
        public Event(string eventName)
        {
            Parameter.ThrowIfNull(eventName, nameof(eventName));
            this.ExpectedResponse.@event = eventName;
        }

        public string Name
        {
            get { return this.ExpectedResponse.@event; }
        }

        public virtual void ProcessActualResponse(IActualResponse response)
        {
            this.ActualEvent = response.Convert<T>();
        }

        public T ActualEvent { get; protected set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
