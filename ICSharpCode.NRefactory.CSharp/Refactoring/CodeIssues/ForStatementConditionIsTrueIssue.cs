// 
// ForStatementConditionIsTrueIssue.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("true is redundant as for statement condition, thus can be safely ommited",
	                   Description = "Remove redundant true in for statement condition",
	                   Category = IssueCategories.Redundancies,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.GrayOut, 
	                   ResharperDisableKeyword = "ForStatementConditionIsTrue")]
	public class ForStatementConditionIsTrueIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<ForStatementConditionIsTrueIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}
		
			public override void VisitForStatement (ForStatement forstatement)
			{
				base.VisitForStatement(forstatement);

				var condition = forstatement.Condition;
				if (condition == null)
					return;
				while(condition is ParenthesizedExpression)
				{
					condition = condition.FirstChild as Expression;
				}
				if (!(forstatement.Condition is PrimitiveExpression) || (forstatement.Condition as PrimitiveExpression).LiteralValue.Equals("true"))
				{
					AddIssue(forstatement.Condition, ctx.TranslateString("Remove redundant condition"),
					         Script => Script.Remove(forstatement.Condition));
				}
			}
		}
	}
}
