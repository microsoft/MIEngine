// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MICore;

namespace MICoreUnitTests
{
    [TestClass]
    public class MIResultsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestParseCStringNull()
        {
            string miString = null; //input = <null>
            MIResults.ParseCString(miString);
        }

        [TestMethod]
        public void TestParseCStringEmpty()
        {
            string miString = ""; // input = <empty>
            string result = MIResults.ParseCString(miString);

            Assert.AreEqual(String.Empty, result);

            miString = "\"\""; // input = ""
            result = MIResults.ParseCString(miString);

            Assert.AreEqual(String.Empty, result);
        }

        [TestMethod]
        public void TestParseCString()
        {
            string miString = "\"hello\""; //input = "hello"
            string result = MIResults.ParseCString(miString);
            Assert.AreEqual("hello", result);

            miString = "\"\\tHello\\n\""; //input = "\tHello\n"
            result = MIResults.ParseCString(miString);
            Assert.AreEqual("\tHello\n", result);

            miString = "\"    \""; //input = <four spaces>
            result = MIResults.ParseCString(miString);
            Assert.AreEqual("    ", result);

            miString = "    \"Hello\"     "; //input = <leading spaces>"Hello"<trailing spaces>
            result = MIResults.ParseCString(miString);
            Assert.AreEqual("Hello", result);

            miString = "\"\"\"\"\"\""; //input = """"""
            result = MIResults.ParseCString(miString);
            Assert.AreEqual("\"\"", result);
        }

        [TestMethod]
        public void TestParseResultListConstValues()
        {
            string miString = @"name=""value"""; // name="value"
            Results results = MIResults.ParseResultList(miString);

            Assert.AreEqual(1, results.Content.Length);
            Assert.AreEqual("name", results.Content[0].Name);
            Assert.IsTrue(results.Content[0].Value is ConstValue);
            Assert.AreEqual("value", (results.Content[0].Value as ConstValue).Content);
            Assert.AreEqual("value", results.FindString("name"));

            miString = @"name1=""value1"",name2=""value2"""; // name1="value1",name2="value2"
            results = MIResults.ParseResultList(miString);

            Assert.AreEqual(2, results.Content.Length);
            Assert.AreEqual("name1", results.Content[0].Name);
            Assert.AreEqual("name2", results.Content[1].Name);
            Assert.IsTrue(results.Content[0].Value is ConstValue);
            Assert.IsTrue(results.Content[1].Value is ConstValue);
            Assert.AreEqual("value1", (results.Content[0].Value as ConstValue).Content);
            Assert.AreEqual("value2", (results.Content[1].Value as ConstValue).Content);
            Assert.AreEqual("value1", results.FindString("name1"));
            Assert.AreEqual("value2", results.FindString("name2"));
        }
    }
}
