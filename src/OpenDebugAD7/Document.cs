// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using Microsoft.DebugEngineHost;
using System.Globalization;
using OpenDebugAD7.AD7Impl;

namespace OpenDebugAD7
{
    internal class Document
    {
        public string Path { get; private set; }

        protected Document(string path)
        {
            Path = path;
        }

        public static Document GetOrCreate(string path)
        {
            Document document;
            if (!s_documentCache.TryGetValue(path, out document))
            {
                document = new Document(path);
                s_documentCache.Add(path, document);
            }

            return document;
        }

        private static Dictionary<string, Document> s_documentCache = new Dictionary<string, Document>();
    }
}
