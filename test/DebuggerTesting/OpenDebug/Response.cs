// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug
{
    /// <summary>
    /// Provides a convenience base class for classes
    /// that will be serialized to JSON.
    /// </summary>
    public class JsonValue
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public abstract class Response<T> : IResponse
        where T : new()
    {
        public Response()
        {
            this.ExpectedResponse = new T();
        }

        #region IResponse

        object IResponse.DynamicResponse { get { return ExpectedResponse; } }

        public bool IgnoreOrder { get; protected set; }

        public bool IgnoreResponseOrder { get; protected set; }

    #endregion

    public T ExpectedResponse { get; protected set; }

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}
