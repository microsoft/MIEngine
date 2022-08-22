// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using OpenDebug;
namespace DAREditor
{
    class JSONDescrepency
    {
        public static object inputJSon(string input, out Exception error)
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