﻿// 
// SameGuardConditionExpressionInIfelseBranchesIssue.cs
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
using Mono.CSharp;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("SameGuardConditionExpressionInIfelseBranchesIssue",
						Description = "A warning should be given for the case: if (condition) {…} else if (condition) {…}.",
						Category = IssueCategories.Notifications,
						Severity = Severity.Warning,
						ResharperDisableKeyword = "SameGuardConditionExpression",
						IssueMarker = IssueMarker.Underline)]
	public class SameGuardConditionExpressionInIfelseBranchesIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			var unit = context.RootNode as SyntaxTree;
			if (unit == null)
				return Enumerable.Empty<CodeIssue> ();
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase<SameGuardConditionExpressionInIfelseBranchesIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitIfElseStatement (IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement (ifElseStatement);
				var ifCondition = ifElseStatement.Condition;
				var elseStatement = ifElseStatement.FalseStatement as IfElseStatement;

				if (elseStatement != null) {
					var elseCondition = elseStatement.Condition;

					if (string.Compare (ifCondition.ToString (), elseCondition.ToString (), false) == 0) {
						AddIssue (elseCondition, ctx.TranslateString ("A warning should be given for the case: if (condition) {…} else if (condition) {…}."));
					}
				}
			}
		}
	}
}
