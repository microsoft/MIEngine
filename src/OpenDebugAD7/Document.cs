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

#if CORECLR

        /// <summary>
        /// Calculates checksums of the document given the specified hash algorithm.
        /// Multiple checksums can be calculated:
        ///     1. The checksum of the raw bytes of the file
        ///     2. If guidHashAlgorithm specifies 'normalize', the checksum of the file with line 
        ///        endings opposite of what it has (CRLF becomes LF, LF becomes CRLF)
        ///        If the file has mixed line endings or it cannot be normalized, only the raw checksum will be returned
        /// </summary>
        /// <param name="guidHashAlgorithm">The AD7 GUID of the hash algorithm to use to calculate checksums</param>
        /// <returns>
        /// The raw bytes of all checksums hashes. The bytes of the raw checksum will be first in the array, followed
        /// by the bytes of the of normalized checksum, if available. If normalization failed, only the raw checksum will be available.
        /// Callers must know the size of the checksum they requested and process the byte array accordingly.
        /// </returns>
        public byte[] GetChecksums(Guid guidHashAlgorithm)
        {
            // See if we have a cached checksum and return it if the last write time of the file has not changed.
            byte[] checksumBytes;
            if (TryGetCachedChecksum(guidHashAlgorithm, out checksumBytes))
            {
                return checksumBytes;
            }

            // Get the correct HashAlgorithmName for this guid
            HashAlgorithmName hashAlgorithmName;
            bool normalizeLineEndings = false;

            if (guidHashAlgorithm == AD7Guids.guidSourceHashMD5)
            {
                hashAlgorithmName = HashAlgorithmName.MD5;
            }
            else if (guidHashAlgorithm == AD7Guids.guidSourceHashSHA1 || guidHashAlgorithm == AD7Guids.guidSourceHashSHA1Normalized)
            {
                hashAlgorithmName = HashAlgorithmName.SHA1;
                if (guidHashAlgorithm == AD7Guids.guidSourceHashSHA1Normalized)
                {
                    normalizeLineEndings = true;
                }
            }
            else if (guidHashAlgorithm == AD7Guids.guidSourceHashSHA256 || guidHashAlgorithm == AD7Guids.guidSourceHashSHA256Normalized)
            {
                hashAlgorithmName = HashAlgorithmName.SHA256;
                if (guidHashAlgorithm == AD7Guids.guidSourceHashSHA256Normalized)
                {
                    normalizeLineEndings = true;
                }
            }
            else
            {
                throw new ArgumentException("guidHashAlgorithm");
            }

            checksumBytes = null;
            using (FileStream fs = new FileStream(Path, FileMode.Open, FileAccess.Read))
            {
                if (!normalizeLineEndings)
                {
                    checksumBytes = GetRawChecksum(fs, IncrementalHash.CreateHash(hashAlgorithmName));
                }
                else
                {
                    // Use a stream reader to determine the encoding. 
                    // The underlying stream is not closed. Default to UTF8 is heuristic
                    Encoding encoding = Encoding.UTF8;
                    using (StreamReader streamReader = new StreamReader(fs, Encoding.UTF8, true, 8, true))
                    {
                        streamReader.Peek(); // required to set the encoding
                        encoding = streamReader.CurrentEncoding;
                    }

                    // StreamReader.Peek does not change the position of the stream reader but does change the position
                    // of the underlying stream. Reset it
                    fs.Position = 0;

                    // If the file is not a common encoding don't bother attempting to normalize
                    // Calculate and return the raw hash
                    if (!(encoding is UTF8Encoding) &&
                        !(encoding is UnicodeEncoding) &&
                        !(encoding is ASCIIEncoding))
                    {
                        checksumBytes = GetRawChecksum(fs, IncrementalHash.CreateHash(hashAlgorithmName));
                    }
                    else
                    {
                        checksumBytes = GetRawAndNormalizedChecksums(fs, encoding, hashAlgorithmName);
                    }
                }
            }

            _cachedChecksumFileWriteTime.Add(guidHashAlgorithm, File.GetLastWriteTimeUtc(Path));
            _cachedChecksumBytes.Add(guidHashAlgorithm, checksumBytes);
            return checksumBytes;
        }

        private bool TryGetCachedChecksum(Guid guidHashAlgorithm, out byte[] checksumBytes)
        {
            checksumBytes = null;

            DateTime fileWriteTime;
            if (_cachedChecksumFileWriteTime.TryGetValue(guidHashAlgorithm, out fileWriteTime))
            {
                if (fileWriteTime.Equals(File.GetLastWriteTimeUtc(Path)))
                {
                    if (_cachedChecksumBytes.TryGetValue(guidHashAlgorithm, out checksumBytes))
                    {
                        return true;
                    }
                }

                // We have cached data but the file has been updated. Remove this from the cache
                _cachedChecksumBytes.Remove(guidHashAlgorithm);
                _cachedChecksumFileWriteTime.Remove(guidHashAlgorithm);
            }

            return false;
        }

        // Used in cases where we can bail out of getting a normalized hash early
        // The position of the filestream must match the state of incremental hash
        // This makes this usable in the case where we stop normalizing 
        // part way through the file and can continue hashing the raw bytes with this function
        private byte[] GetRawChecksum(FileStream fs, IncrementalHash hash)
        {
            // Read the remainder of the stream in 4k chunks
            byte[] fileBytes = new byte[4096];
            int bytesRead = 0;
            do
            {
                bytesRead = fs.Read(fileBytes, 0, fileBytes.Length);
                hash.AppendData(fileBytes, 0, bytesRead);
            } while (bytesRead != 0);

            return hash.GetHashAndReset();
        }

        private class NormalizationException : Exception
        {
            public NormalizationException(string message)
                : base(message)
            {
            }
        }

        private enum LineEnding
        {
            CRLF,
            LF
        }

        private byte[] GetRawAndNormalizedChecksums(FileStream fs, Encoding encoding, HashAlgorithmName hashAlgorithmName)
        {
            char CRchar = '\r';
            char LFchar = '\n';

            byte[] checksumBytes = null;

            using (IncrementalHash rawHash = IncrementalHash.CreateHash(hashAlgorithmName))
            using (IncrementalHash normalizedHash = IncrementalHash.CreateHash(hashAlgorithmName))
            using (BinaryReader reader = new BinaryReader(fs, encoding))
            {
                // Determine first line ending
                LineEnding lineEnding = LineEnding.CRLF;
                try
                {
                    for (;;)
                    {
                        char nextChar = reader.ReadChar();
                        if (nextChar == CRchar)
                        {
                            lineEnding = LineEnding.CRLF;
                            break;
                        }
                        else if (nextChar == LFchar)
                        {
                            lineEnding = LineEnding.LF;
                            break;
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    // no line ending in file. loop below will be fine
                }
                fs.Position = 0;

                // These will capture the characters of the file which we will serialize to bytes
                // using the Encoding and append to the incremental hash on each line ending
                StringBuilder rawString = new StringBuilder();
                StringBuilder normalizedString = new StringBuilder();

                try
                {
                    for (;;) // we will leave this loop on EndOfStreamException
                    {
                        char nextChar = reader.ReadChar();

                        rawString.Append(nextChar);

                        if (nextChar != CRchar && nextChar != LFchar)
                        {
                            normalizedString.Append(nextChar);
                        }
                        else
                        {
                            if (nextChar == CRchar)
                            {
                                // we found a CR, assume this file is CRLF and we want to normalize to LF
                                if (lineEnding == LineEnding.LF)
                                {
                                    throw new NormalizationException("mixed line endings");
                                }

                                nextChar = reader.ReadChar();
                                rawString.Append(nextChar); // need to track this raw char regardless of what it is

                                if (nextChar != LFchar)
                                {
                                    throw new NormalizationException("CR not followed by LF");
                                }
                                normalizedString.Append(nextChar);
                            }
                            else if (nextChar == LFchar)
                            {
                                // we found an LF, assume this file is LF and want to normalize to CRLF
                                if (lineEnding == LineEnding.CRLF)
                                {
                                    throw new NormalizationException("mixed line endings");
                                }
                                normalizedString.Append(CRchar);
                                normalizedString.Append(LFchar);
                            }

                            // perform the incremental hashing when we see a line ending
                            rawHash.AppendData(encoding.GetBytes(rawString.ToString()));
                            normalizedHash.AppendData(encoding.GetBytes(normalizedString.ToString()));

                            rawString.Clear();
                            normalizedString.Clear();
                        }
                    }
                }
                catch (NormalizationException)
                {
                    // we failed to normalize the file, finish the raw hash
                    Logger.WriteLine(string.Format(CultureInfo.InvariantCulture, "Unable to calculate normalized checksum for '{0}'", Path));

                    rawHash.AppendData(encoding.GetBytes(rawString.ToString()));
                    checksumBytes = GetRawChecksum(fs, rawHash);
                }
                catch (EndOfStreamException)
                {
                    // We succesfully normalized the entire file
                    // grab remaining bytes in the case that the file does not end with a line ending character
                    rawHash.AppendData(encoding.GetBytes(rawString.ToString()));
                    normalizedHash.AppendData(encoding.GetBytes(normalizedString.ToString()));

                    // combine the hash bytes
                    checksumBytes = rawHash.GetHashAndReset().Concat(normalizedHash.GetHashAndReset()).ToArray();
                }
            }

            return checksumBytes;
        }

        private Dictionary<Guid, byte[]> _cachedChecksumBytes = new Dictionary<Guid, byte[]>();
        private Dictionary<Guid, DateTime> _cachedChecksumFileWriteTime = new Dictionary<Guid, DateTime>();

#endif

        private static Dictionary<string, Document> s_documentCache = new Dictionary<string, Document>();
    }
}
