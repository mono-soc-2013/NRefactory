using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
    [TestFixture]
    public class BlockTest
    {
        [Test]
        public void TestBlock_Simple()
        {
            var indent = Helper.CreateEngine("namespace Foo {$");
            Assert.AreEqual("", indent.ThisLineIndent);
            Assert.AreEqual("\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_PreProcessor()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if NOTTHERE
	{
#endif
		$");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_PreProcessor_IfStatement()
        {
            var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if NOTTHERE || true
	    {
#endif
		$");
            Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_If()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		if (true)$");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_While()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		while (true)$");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_For()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		for (;;)$");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_Foreach()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		foreach (var v in V)$");
            Assert.AreEqual("\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_Do()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do
$");
            Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_NestedDo()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do
$");
            Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_NestedDoContinuationSetBack()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do
foo();$");
            Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
            Assert.AreEqual("\t\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_ThisLineIndentAfterCurlyBrace()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
	}$");
            Assert.AreEqual("\t", indent.ThisLineIndent);
            Assert.AreEqual("\t", indent.NewLineIndent);
        }

        [Test]
        public void TestBlock_ThisLineIndentAfterCurlyBrace2()
        {
            var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ }$");
            Assert.AreEqual("\t", indent.ThisLineIndent);
            Assert.AreEqual("\t", indent.NewLineIndent);
        }

//        [Test]
//        public void TestBlock_Parameters()
//        {
//            var indent = Helper.CreateEngine(@"
//class Foo {
//	void Test ()
//	{
//		Foo(true,$");
//            Assert.AreEqual("\t\t", indent.ThisLineIndent);
//            Assert.AreEqual("\t\t   ", indent.NewLineIndent);
//        }

//        [Test]
//        public void TestBlock_Parameters2()
//        {
//            var indent = Helper.CreateEngine(@"
//class Foo {
//	void Test ()
//	{
//		Foo($");
//            Assert.AreEqual("\t\t", indent.ThisLineIndent);
//            Assert.AreEqual("\t\t\t", indent.NewLineIndent);
//        }
    }
}
