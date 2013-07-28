﻿using ICSharpCode.NRefactory.CSharp;
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
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_If()
		{
			var indent = Helper.CreateEngine(@"
#if false
{ $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_If2()
		{
			var indent = Helper.CreateEngine(@"
#if true
{ $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessorComment_NestedBlocks()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if false
		{ $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
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
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
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
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
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
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Else()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if false
		{
#else
	}
#endif
	$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Else2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#if true
		{
#else
		} 
#endif
			$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
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
			Assert.AreEqual("\t\t", indent.NextLineIndent);
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
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Pragma()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#pragma Foo 42 $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Warning()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#warning Foo $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Error()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#error Foo $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Line()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#line 42 $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Define()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#define Foo 42 $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_Undef()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
#undef Foo $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		#endregion

		[Test]
		public void TestBrackets_PreProcessor_If_DefineDirective()
		{
			var indent = Helper.CreateEngine(@"
#define NOTTHERE
namespace Foo {
	class Foo {
#if NOTTHERE
		{
#endif
		$");
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestBrackets_PreProcessor_If_UndefDirective()
		{
			var indent = Helper.CreateEngine(@"
#define NOTTHERE
namespace Foo {
	class Foo {
#undef NOTTHERE
#if NOTTHERE
		{
#endif
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestPreProcessor_IndentPreprocessor()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.IndentPreprocessorStatements = true;

			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo {
		#if true $ ", policy);

			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}
	}
}
