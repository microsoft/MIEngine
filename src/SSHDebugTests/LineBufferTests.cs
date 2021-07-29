// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SSHDebugTests
{
    public class LineBufferTests
    {
        [Fact]
        public void LineBuffer_FullLine()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello\n", new string[] { "hello" });
        }

        [Fact]
        public void LineBuffer_FullLineWithCR()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello\r\n", new string[] { "hello" });
        }

        [Fact]
        public void LineBuffer_TwoParts()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello", new string[] { });
            Verify(lineBuffer, "\n", new string[] { "hello" });
        }

        [Fact]
        public void LineBuffer_MultiLine()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello\nworld\n!\n", new string[] { "hello", "world", "!" });
        }

        [Fact]
        public void LineBuffer_SplitLine1()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello\nmore text", new string[] { "hello" });
            Verify(lineBuffer, "comes now\n", new string[] { "more textcomes now" });
        }

        [Fact]
        public void LineBuffer_SplitLine2()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello\nworld\nmore text", new string[] { "hello", "world" });
            Verify(lineBuffer, "comes now\n", new string[] { "more textcomes now" });
        }

        [Fact]
        public void LineBuffer_EmptyLine()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello", new string[] { });
            Verify(lineBuffer, "\n\nworld\n", new string[] { "hello", "", "world" });
        }

        [Fact]
        public void LineBuffer_EmptyLineCR()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "hello", new string[] { });
            Verify(lineBuffer, "\r\n\nworld\r\n", new string[] { "hello", "", "world" });
        }

        [Fact]
        public void LineBuffer_CharByChar()
        {
            var lineBuffer = new LineBuffer();
            Verify(lineBuffer, "a", new string[] { });
            Verify(lineBuffer, "b", new string[] { });
            Verify(lineBuffer, "c", new string[] { });
            Verify(lineBuffer, "d", new string[] { });
            Verify(lineBuffer, "e", new string[] { });
            Verify(lineBuffer, "\n", new string[] { "abcde" });
        }

        private void Verify(LineBuffer lineBuffer, string textToAdd, string[] expectedOutput)
        {
            IEnumerable<string> result;
            lineBuffer.ProcessText(textToAdd, out result);
            string[] r = result.ToArray();

            Assert.Equal<int>(expectedOutput.Length, r.Length);

            for (int i = 0; i < expectedOutput.Length; i++)
            {
                Assert.Equal(expectedOutput[i], r[i]);
            }
        }
    }
}
