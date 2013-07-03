using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
    [TestFixture]
    public class PreProcessorTests
    {
        [Test]
        public void TestPreProcessor_Simple()
        {
            var indent = Helper.CreateEngine("#if MONO");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessorComment_Simple()
        {
            var indent = Helper.CreateEngine(@"
#if false
{ $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessorStatement_Simple()
        {
            var indent = Helper.CreateEngine(@"
#if true
{ $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessorComment_NestedBlocks()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#if bla
        { $");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessorStatement_NestedBlocks()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#if true
        { $");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Elif()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#if true
        {
#elif false
        } 
#endif
            $");
            Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Elif2()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#if false
        {
#elif true
    }
#endif
    $");
            Assert.AreEqual("\t", indent.ThisLineIndent);
            Assert.AreEqual("\t", indent.NewLineIndent);
        }

        #region Single-line directives

        [Test]
        public void TestPreProcessor_Region()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
        #region Foo $");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Endegion()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
        #region
        void Test() { }
        #endregion $");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Pragma()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#pragma Foo 42 $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Warning()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#warning Foo $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Error()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#error Foo $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Line()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#line 42 $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Define()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#define Foo 42 $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestPreProcessor_Undef()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
    class Foo {
#undef Foo $");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        #endregion
    }
}
