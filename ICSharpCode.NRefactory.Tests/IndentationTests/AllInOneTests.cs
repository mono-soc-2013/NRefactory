﻿using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
    [TestFixture]
    public class AllInOneTests
    {
        const string ProjectDir = "../../";
        const string TestFilesPath = "ICSharpCode.NRefactory.Tests/IndentationTests/TestFiles";

        public void BeginFileTest(string fileName)
        {
            Helper.ReadAndTest(System.IO.Path.Combine(ProjectDir, TestFilesPath, fileName));
        }

        [Test]
        public void TestAllInOne_Simple()
        {
            BeginFileTest("Simple.cs");   
        }

        [Test]
        public void TestAllInOne_PreProcessorDirectives()
        {
            BeginFileTest("PreProcessorDirectives.cs");
        }
    }
}
