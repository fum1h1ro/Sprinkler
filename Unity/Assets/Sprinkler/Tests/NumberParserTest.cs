using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sprinkler.Tests
{
    public class NumberParserTest
    {
        [TestCase("0", 0.0f)]
        [TestCase("0.5", 0.5f)]
        [TestCase("1", 1.0f)]
        [TestCase("100.0", 100.0f)]
        [TestCase("-99", -99.0f)]
        [TestCase("-99.99", -99.99f)]
        public void FloatTest(string src, float answer)
        {
            Assert.AreEqual((new NumberParser(new ReadOnlySpan(src))).FloatValue, answer);
        }

        [TestCase("#ff", (uint)0xff)]
        [TestCase("#7ff", (uint)0x7ff)]
        [TestCase("#ffffffff", 0xffffffff)]
        public void IntTest(string src, uint answer)
        {
            Assert.AreEqual((new NumberParser(new ReadOnlySpan(src))).UintValue, answer);
        }
    }
}
