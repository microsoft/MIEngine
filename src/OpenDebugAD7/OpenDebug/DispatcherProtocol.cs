// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenDebug
{
    /*
     * The V8ServerProtocol can be used to implement a server that uses the V8 protocol.
     */
    public class DispatcherProtocol
    {
        private const int BUFFER_SIZE = 4096;
        private const int RESPONSE_TIMEOUT = 5000;  // 5 seconds
        private const string TWO_CRLF = "\r\n\r\n";
        private static readonly Regex s_CONTENT_LENGTH_MATCHER = new Regex(@"Content-Length: (\d+)");
        private static readonly Regex s_VERSION_MATCHER = new Regex(@"Embedding-Host:\snode\sv(\d+)\.\d+\.\d+");

        private static readonly Encoding s_encoding = System.Text.Encoding.UTF8;

        public Action<string> TraceCallback { get; set; } = null;
        public Action<string> ResponseCallback { get; set; } = null;

        private int _sequenceNumber;

        private Stream _inputStream;
        private Stream _outputStream;

        private ByteBuffer _rawData;
        private int _bodyLength;

        private bool _stopRequested;
        private bool _isDispatchingData;

        private Action<string, dynamic, IResponder> _callback;

        private ConcurrentQueue<DispatcherEvent> _queuedEvent;

        // This is a general purpose lock. Don't hold it across long operations.
        private readonly object _lock = new object();

        public DispatcherProtocol(Stream inputStream, Stream outputStream)
        {
            _sequenceNumber = 1;
            _inputStream = inputStream;
            _outputStream = outputStream;
            _bodyLength = -1;
            _rawData = new ByteBuffer();
            _queuedEvent = new ConcurrentQueue<DispatcherEvent>();
        }

        public async Task<int> Start(Action<string, dynamic, IResponder> cb)
        {
            _callback = cb;

            byte[] buffer = new byte[BUFFER_SIZE];

            _stopRequested = false;
            while (!_stopRequested)
            {
                var read = await _inputStream.ReadAsync(buffer, 0, buffer.Length);

                if (read == 0)
                {
                    break;
                }

                if (read > 0)
                {
                    _rawData.Append(buffer, read);
                    ProcessData();
                }
            }
            return 0;
        }

        public void Stop()
        {
            _stopRequested = true;
        }

        public void SendEvent(string eventType, dynamic body)
        {
            SendMessage(new DispatcherEvent(eventType, body));
        }

        public void SendRawEvent(string eventType, dynamic body)
        {
            SendRaw(new DispatcherEvent(eventType, body));
        }

        public void SendEventLater(string eventType, dynamic body)
        {
            lock (_lock)
            {
                if (_isDispatchingData)
                {
                    _queuedEvent.Enqueue(new DispatcherEvent(eventType, body));
                }
                else
                {
                    SendMessage(new DispatcherEvent(eventType, body));
                }
            }
        }

        // ---- private ------------------------------------------------------------------------

        private void ProcessData()
        {
            while (true)
            {
                if (_bodyLength >= 0)
                {
                    if (_rawData.Length >= _bodyLength)
                    {
                        var buf = _rawData.RemoveFirst(_bodyLength);

                        _bodyLength = -1;

                        Dispatch(s_encoding.GetString(buf));

                        continue;   // there may be more complete messages to process
                    }
                }
                else
                {
                    string s = _rawData.GetString(s_encoding);
                    var idx = s.IndexOf(TWO_CRLF, StringComparison.Ordinal);
                    if (idx != -1)
                    {
                        Match m = s_CONTENT_LENGTH_MATCHER.Match(s);
                        if (m.Success && m.Groups.Count == 2)
                        {
                            _bodyLength = Convert.ToInt32(m.Groups[1].ToString(), CultureInfo.InvariantCulture);

                            _rawData.RemoveFirst(idx + TWO_CRLF.Length);

                            continue;   // try to handle a complete message
                        }
                    }
                }
                break;
            }
        }

        private void Dispatch(string req)
        {
            var request = JsonConvert.DeserializeObject<DispatcherRequest>(req);
            if (request != null && request.type == "request")
            {
                TraceCallback?.Invoke(string.Format("C {0}: {1}", request.command, JsonConvert.SerializeObject(request.arguments)));

                if (_callback != null)
                {
                    try
                    {
                        lock (_lock)
                        {
                            _isDispatchingData = true;
                        }

                        var response = new DispatcherResponse(request.seq, request.command);
                        var responder = new DispatchResponder(this, response);

                        _callback(request.command, request.arguments, responder);

                        SendMessage(response);
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _isDispatchingData = false;
                        }

                        DispatcherEvent e;
                        while (_queuedEvent.TryDequeue(out e))
                        {
                            SendMessage(e);
                        }
                    }
                }
            }
        }

        private void SendMessage(DispatcherMessage message)
        {
            if (message.type == "response")
            {
                ResponseCallback?.Invoke(string.Format(CultureInfo.InvariantCulture, " R: {0}", JsonConvert.SerializeObject(message)));
            }
            else if (message.type == "event")
            {
                DispatcherEvent e = (DispatcherEvent)message;
                ResponseCallback?.Invoke(string.Format("E {0}: {1}", e.eventType, JsonConvert.SerializeObject(e.body)));
            }

            SendRaw(message);
        }

        private void SendRaw(DispatcherMessage message)
        {
            message.seq = _sequenceNumber++;

            var asJson = JsonConvert.SerializeObject(message);
            byte[] jsonBytes = s_encoding.GetBytes(asJson);

            string header = string.Format(CultureInfo.InvariantCulture, "Content-Length: {0}{1}", jsonBytes.Length, TWO_CRLF);
            byte[] headerBytes = s_encoding.GetBytes(header);

            byte[] data = new byte[headerBytes.Length + jsonBytes.Length];
            System.Buffer.BlockCopy(headerBytes, 0, data, 0, headerBytes.Length);
            System.Buffer.BlockCopy(jsonBytes, 0, data, headerBytes.Length, jsonBytes.Length);

            try
            {
                _outputStream.Write(data, 0, data.Length);
                _outputStream.Flush();
            }
            catch (Exception e)
            {
                Debug.Fail("Exception encountered during stream write: " + e.Message);
            }
        }
    }

    //--------------------------------------------------------------------------------------

    public interface IResponder
    {
        void SetBody(dynamic body);
        void AddEvent(string type, dynamic body);
    }

    internal class DispatchResponder : IResponder
    {
        private DispatcherProtocol _protocol;
        private DispatcherResponse _response;

        public DispatchResponder(DispatcherProtocol protocol, DispatcherResponse response)
        {
            _protocol = protocol;
            _response = response;
        }

        public void SetBody(dynamic body)
        {
            _response.body = body;
            if (body is ErrorResponseBody)
            {
                var e = (ErrorResponseBody)body;
                var message = e.error;
                _response.success = false;
                _response.message = message.variables != null ? Utilities.ExpandVariables(message.format, message.variables) : message.format;
            }
            else
            {
                _response.success = true;

                if (body is InitializeResponseBody)
                {
                    _response.body = body.body;
                }
            }
        }

        public void AddEvent(string type, dynamic body)
        {
            _protocol.SendEventLater(type, body);
        }
    }

    internal class ByteBuffer
    {
        private byte[] _buffer;

        public ByteBuffer()
        {
            _buffer = new byte[0];
        }

        public int Length
        {
            get { return _buffer.Length; }
        }

        public string GetString(Encoding enc)
        {
            return enc.GetString(_buffer);
        }

        public void Append(byte[] b, int length)
        {
            byte[] newBuffer = new byte[_buffer.Length + length];
            System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);
            System.Buffer.BlockCopy(b, 0, newBuffer, _buffer.Length, length);
            _buffer = newBuffer;
        }

        public byte[] RemoveFirst(int n)
        {
            byte[] b = new byte[n];
            System.Buffer.BlockCopy(_buffer, 0, b, 0, n);
            byte[] newBuffer = new byte[_buffer.Length - n];
            System.Buffer.BlockCopy(_buffer, n, newBuffer, 0, _buffer.Length - n);
            _buffer = newBuffer;
            return b;
        }
    }
}
