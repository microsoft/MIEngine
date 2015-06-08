// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MICore;
using Xunit;

namespace MICoreUnitTests
{
    public class MIResultsTests
    {
        [Fact]
        public void TestParseCStringNull()
        {
            string miString = null; //input = <null>

            Assert.Throws<ArgumentNullException>(delegate
            {
                MIResults.ParseCString(miString);
            });
        }

        [Fact]
        public void TestParseCStringEmpty()
        {
            string miString = ""; // input = <empty>
            string result = MIResults.ParseCString(miString);

            Assert.Equal(String.Empty, result);

            miString = "\"\""; // input = ""
            result = MIResults.ParseCString(miString);

            Assert.Equal(String.Empty, result);
        }

        [Fact]
        public void TestParseCString()
        {
            string miString = "\"hello\""; //input = "hello"
            string result = MIResults.ParseCString(miString);
            Assert.Equal("hello", result);

            miString = "\"\\tHello\\n\""; //input = "\tHello\n"
            result = MIResults.ParseCString(miString);
            Assert.Equal("\tHello\n", result);

            miString = "\"    \""; //input = <four spaces>
            result = MIResults.ParseCString(miString);
            Assert.Equal("    ", result);

            miString = "    \"Hello\"     "; //input = <leading spaces>"Hello"<trailing spaces>
            result = MIResults.ParseCString(miString);
            Assert.Equal("Hello", result);

            miString = "\"\"\"\"\"\""; //input = """"""
            result = MIResults.ParseCString(miString);
            Assert.Equal("\"\"", result);
        }

        [Fact]
        public void TestParseResultListConstValues()
        {
            string miString = @"name=""value"""; // name="value"
            Results results = MIResults.ParseResultList(miString);

            Assert.Equal(1, results.Content.Length);
            Assert.Equal("name", results.Content[0].Name);
            Assert.True(results.Content[0].Value is ConstValue);
            Assert.Equal("value", (results.Content[0].Value as ConstValue).Content);
            Assert.Equal("value", results.FindString("name"));

            miString = @"name1=""value1"",name2=""value2"""; // name1="value1",name2="value2"
            results = MIResults.ParseResultList(miString);

            Assert.Equal(2, results.Content.Length);
            Assert.Equal("name1", results.Content[0].Name);
            Assert.Equal("name2", results.Content[1].Name);
            Assert.True(results.Content[0].Value is ConstValue);
            Assert.True(results.Content[1].Value is ConstValue);
            Assert.Equal("value1", (results.Content[0].Value as ConstValue).Content);
            Assert.Equal("value2", (results.Content[1].Value as ConstValue).Content);
            Assert.Equal("value1", results.FindString("name1"));
            Assert.Equal("value2", results.FindString("name2"));
        }
    }
}
