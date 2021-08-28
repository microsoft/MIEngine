// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.OpenDebug.Events
{
    public enum CategoryValue
    {
        Console = 0,
        Stdout = 1,
        Stderr = 2,
        Telemetry = 3,
        Unknown = Int32.MaxValue
    }

    public sealed class OutputEventValue : EventValue
    {
        public sealed class Body
        {
            public string output;

            public string category;
        }

        public Body body = new Body();
    }

    public sealed class OutputEvent : Event<OutputEventValue>
    {
        public OutputEvent(string text, CategoryValue category) : base("output")
        {
            this.ExpectedResponse.body.category = GeCategory(category);
            this.ExpectedResponse.body.output = text;
        }

        private static string GeCategory(CategoryValue category)
        {
            Parameter.ThrowIfIsInvalid(category, CategoryValue.Unknown, nameof(category));
            return category.ToString().ToLowerInvariant();
        }
    }
}
