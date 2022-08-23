// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using OpenDebug;
namespace DAREditor
{
    static class JSONHandler
    {
        public static object TryDeserialize(string input, out Exception error)
        {
            error = null;
            try 
            {
                return JsonConvert.DeserializeObject<DispatcherMessage>(input);
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }
        }
    }
}