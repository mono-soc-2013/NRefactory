// 
// ConvertIfStatementToReturnStatementIssue.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.Refactoring;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("ConvertIfStatementToReturnStatement",
						Description = "if-return' statement can be re-written as 'return' statement",
						Category = IssueCategories.Redundancies,
						Severity = Severity.Hint,
						ResharperDisableKeyword = "ConvertIfStatementToReturnStatement",
						IssueMarker = IssueMarker.Underline)]
	public class ConvertIfStatementToReturnStatementIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			var unit = context.RootNode as SyntaxTree;
			if (unit == null)
				return Enumerable.Empty<CodeIssue> ();
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase<ConvertIfStatementToReturnStatementIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitIfElseStatement (IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement (ifElseStatement);

				var trueStatement = ifElseStatement.TrueStatement as ReturnStatement;
				if (trueStatement == null) {
					var trueBlockStatement = ifElseStatement.TrueStatement as BlockStatement;
					if (trueBlockStatement == null || trueBlockStatement.Statements.Count != 1) {
						return;
					} else {
						trueStatement = trueBlockStatement.Statements.FirstOrNullObject () as ReturnStatement;
						if (trueStatement == null) {
							return;
						}
					}
				}

				var falseStatement = ifElseStatement.FalseStatement as ReturnStatement;
				if (falseStatement == null) {
					var falseBlockStatement = ifElseStatement.FalseStatement as BlockStatement;
					if (falseBlockStatement == null || falseBlockStatement.Statements.Count != 1) {
						return;
					} else {
						falseStatement = falseBlockStatement.Statements.FirstOrNullObject () as ReturnStatement;
						if (falseStatement == null) {
							return;
						}
					}
				}

				AddIssue (ifElseStatement, ctx.TranslateString ("if-return' statement can be re-written as 'return' statement."),
					script =>
				{
					TextLocation delStartPoint1 = ifElseStatement.StartLocation;
					TextLocation delEndingPoint1 = ifElseStatement.Condition.StartLocation;

					TextLocation delStartPoint2 = ifElseStatement.Condition.EndLocation;
					TextLocation delEndingPoint2 = trueStatement.ReturnToken.EndLocation;

					TextLocation delStartPoint3 = trueStatement.Expression.EndLocation;
					TextLocation delEndingPoint3 = falseStatement.ReturnToken.EndLocation;

					TextLocation delStartPoint4 = falseStatement.Expression.EndLocation;
					TextLocation delEndingPoint4 = ifElseStatement.EndLocation;

					RemoveText (script, delStartPoint1, delEndingPoint1);
					RemoveText (script, delStartPoint2, delEndingPoint2);
					RemoveText (script, delStartPoint3, delEndingPoint3);
					RemoveText (script, delStartPoint4, delEndingPoint4);

					script.InsertText (script.GetCurrentOffset (delStartPoint4), ";");
					script.InsertText (script.GetCurrentOffset (delStartPoint3), " : ");
					script.InsertText (script.GetCurrentOffset (delStartPoint2), " ? ");
					script.InsertText (script.GetCurrentOffset (delStartPoint1), "return ");

					script.FormatText (ifElseStatement.Parent);
				});
			}

			void RemoveText (Script script, TextLocation start, TextLocation end)
			{
				var startOffset = script.GetCurrentOffset (start);
				var endOffset = script.GetCurrentOffset (end);
				if (startOffset < endOffset)
					script.RemoveText (startOffset, endOffset - startOffset);
			}
		}
	}
}