// RedundantBlockInDifferentBranches.cs
//
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Refactoring;
using Mono.CSharp;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("RedundantBlockInDifferentBranches",
						Description = "Blocks in if/else or switch branches can be simplified to any of the branches if they have the same block.",
						Category = IssueCategories.Redundancies,
						Severity = Severity.Hint,
						ResharperDisableKeyword = "RedundantBlockInDifferentBranches",
						IssueMarker = IssueMarker.Underline)]
	public class RedundantBlockInDifferentBranchesIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			var unit = context.RootNode as SyntaxTree;
			if (unit == null)
				return Enumerable.Empty<CodeIssue>();
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<RedundantBlockInDifferentBranchesIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}
			
			public static void WriteFile(string str)
			{
				using (System.IO.StreamWriter file = new System.IO.StreamWriter("/Users/leoji/test.txt", true))
				{
					file.WriteLine(str);
				}
			}

			private bool IsStatementsEqual(Statement statement1, Statement statement2)
			{
				return (string.Compare(statement1.ToString().Trim(), statement2.ToString().Trim(), false) == 0);
			}

			private bool IsStatementBlockEqual(BlockStatement blockStatement1, BlockStatement blockStatement2)
			{
				var statements1Fromblock = blockStatement1.Statements;
				var statements2Fromblock = blockStatement2.Statements;

				var statementEnumerator1 = statements1Fromblock.GetEnumerator();
				var statementEnumerator2 = statements2Fromblock.GetEnumerator();

				while (statementEnumerator1.MoveNext())
				{
					if (!statementEnumerator2.MoveNext())
					{
						return false;
					}
					var statement1 = statementEnumerator1.Current;
					var statement2 = statementEnumerator2.Current;

					if (!IsStatementsEqual(statement1, statement2))
					{
						return false;
					}
				}

				if (statementEnumerator2.MoveNext())
				{
					return false;
				}

				return true;
			}

			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement(ifElseStatement);

				WriteFile("=======================================");
				base.VisitIfElseStatement(ifElseStatement);
				var trueStatementBlock = ifElseStatement.TrueStatement as BlockStatement;
				var falseStatementBlock = ifElseStatement.FalseStatement as BlockStatement;

				if (falseStatementBlock == null || trueStatementBlock == null)
				{
					Statement trueStatement;
					Statement falseStatement;

					if (trueStatementBlock == null)
					{
						trueStatement = ifElseStatement.TrueStatement as Statement;
						if (trueStatement == null)
						{
							return;
						}
					}
					else
					{
						if (trueStatementBlock.Statements.Count != 1)
						{
							return;
						}
						trueStatement = trueStatementBlock.Statements.FirstOrNullObject() as Statement;
					}

					if (falseStatementBlock == null)
					{
						falseStatement = ifElseStatement.FalseStatement as Statement;
						if (falseStatement == null)
						{
							return;
						}
					}
					else
					{
						if (falseStatementBlock.Statements.Count != 1)
						{
							return;
						}
						falseStatement = falseStatementBlock.Statements.FirstOrNullObject() as Statement;
					}

					if (IsStatementsEqual(trueStatement, falseStatement))
					{
						AddIssue(ifElseStatement.IfToken, ctx.TranslateString("Blocks in if/else or switch branches can be simplified to any of the branches if they have the same block."), //ctx.TranslateString("Change if/else statement to statements"), 
						script =>
						{
							var startOffset = script.GetCurrentOffset(trueStatement.EndLocation);
							var endOffset = script.GetCurrentOffset(ifElseStatement.EndLocation);
							if (startOffset < endOffset)
								script.RemoveText(startOffset, endOffset - startOffset);

							startOffset = script.GetCurrentOffset(ifElseStatement.StartLocation);
							endOffset = script.GetCurrentOffset(trueStatement.StartLocation);
							if (startOffset < endOffset)
								script.RemoveText(startOffset, endOffset - startOffset);

							script.FormatText(ifElseStatement);
						});
					}
				}
				else if (IsStatementBlockEqual(trueStatementBlock, falseStatementBlock))
				{
					AddIssue(ifElseStatement.IfToken, ctx.TranslateString("Blocks in if/else or switch branches can be simplified to any of the branches if they have the same block."), //ctx.TranslateString("Change if/else statement to statements"), 
						script =>
						{
							var startOffset = script.GetCurrentOffset(trueStatementBlock.Statements.LastOrNullObject().EndLocation);
							var endOffset = script.GetCurrentOffset(ifElseStatement.EndLocation);
							if (startOffset < endOffset)
								script.RemoveText(startOffset, endOffset - startOffset);

							startOffset = script.GetCurrentOffset(ifElseStatement.StartLocation);
							endOffset = script.GetCurrentOffset(trueStatementBlock.Statements.FirstOrNullObject().StartLocation);
							if (startOffset < endOffset)
								script.RemoveText(startOffset, endOffset - startOffset);

							script.FormatText(ifElseStatement);
						});
				}
			}

			public override void VisitSwitchStatement(SwitchStatement switchStatement)
			{
				base.VisitSwitchStatement(switchStatement);
			}
		}
	}
}