using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace Sprinkler.Tests
{
    public class TextSplitterTest
    {
        [TestCase("hoge", 1)]
        [TestCase(" hoge ", 1)]
        [TestCase("hoge hage", 2)]
        [TestCase(" hoge hage ", 2)]
        [TestCase("hoge hage hige", 3)]
        [TestCase(" hoge hage hige ", 3)]
        [TestCase("  hoge  hage  hige  ", 3)]
        public void CountTest(string str, int count)
        {
            Assert.AreEqual((new TextSplitter(str, ' ')).Count(), count);
        }

        [TestCase("hoge", 0, "hoge")]
        [TestCase("hoge hage", 1, "hage")]
        public void IndexTest(string str, int index, string result)
        {
            Assert.AreEqual((new TextSplitter(str, ' '))[index], result);
        }

        [TestCase("hoge", 0, "hoge")]
        [TestCase("0,1", 1, "1")]
        public void IndexTest2(string str, int index, string result)
        {
            Assert.AreEqual((new TextSplitter(str, ','))[index], result);
        }

        [TestCase("0,1")]
        public void ForEachTest(string str)
        {
            var ts = new TextSplitter(str, ',');
            foreach (var v in ts)
            {
                Debug.Log(v.ToString());
            }
        }



    }
}
