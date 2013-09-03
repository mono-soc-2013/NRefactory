﻿using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
	[TestFixture]
	public class BracketsTest
	{
		[Test]
		public void TestBrackets_Simple()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_PreProcessor_If()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if NOTTHERE
	{
#endif
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_PreProcessor_If2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if NOTTHERE || true
		{
#endif
		$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_If()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		if (true)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_While()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		while (true)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_For()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		for (;;)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Foreach()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		foreach (var v in V)$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Do()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do
			$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Do2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do
			;
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDo()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do
				$");
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDo2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do
				;
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuationSetBack()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do
					foo();
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuationSetBack2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do 
			do
				do
					foo();
$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuation_ExpressionEnded()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do foo(); $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NestedDoContinuation_ExpressionNotEnded()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		do do do foo() $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_ThisLineIndentAfterCurlyBrace()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
	}$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_ThisLineIndentAfterCurlyBrace2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{ }$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parameters()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foo(true,$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t    ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parameters2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foo($");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t    ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parenthesis()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foooo(a, b, c, // ) 
				$");
			Assert.AreEqual("\t\t      ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t      ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Parenthesis2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foooo(a, b, c, // ) 
				d) $");
			Assert.AreEqual("\t\t      ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_SquareBrackets()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = [a, b, c, // ] 
					$");
			Assert.AreEqual("\t\t         ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t         ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_SquareBrackets2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = [a, b, c, // ]
					d]; $");
			Assert.AreEqual("\t\t         ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_AngleBrackets()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Func<a, b, c, // > 
				$");
			Assert.Inconclusive("Not implemented.");
			Assert.AreEqual("\t\t     ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t     ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_AngleBrackets2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Func<a, b, c, // >
				d> $");
			Assert.Inconclusive("Not implemented.");
			Assert.AreEqual("\t\t     ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_Nested()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		Foo(a, b, bar(c, d[T,  // T
							G], // G
						e), $    // e
			f);");
			Assert.AreEqual("\t\t              ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t    ", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_NotLineStart()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		void Test(int i,
		          double d) { $");
			Assert.AreEqual("\t\t          ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_RightHandExpression()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = from i in I
				where i == ';'
				select i; $");
			Assert.AreEqual("\t\t        ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_DotExpression()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = I.Where(i => i == ';')
		         .Select(i => i); $");
			Assert.AreEqual("\t\t         ", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_LambdaExpression()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = () => { $
		};");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_LambdaExpression2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = () => {
		}; $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_EqualContinuation()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = 
			0; $");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_EqualExtraSpaces()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	void Test ()
	{
		var v = 1 + $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t        ", indent.NextLineIndent);
		}
	}
}
