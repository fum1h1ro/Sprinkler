using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace Sprinkler.Tests
{
    public class SpecialParserTest
    {
        [TestCase("&lt;", '<')]
        [TestCase("&gt;", '>')]
        [TestCase("&#65;", 'A')]
        [TestCase("&#x41;", 'A')]
        public void TemplateTest(string str, char c)
        {
            Assert.AreEqual((new SpecialParser(new ReadOnlySpan(str))).Result, c);
        }
    }
}
