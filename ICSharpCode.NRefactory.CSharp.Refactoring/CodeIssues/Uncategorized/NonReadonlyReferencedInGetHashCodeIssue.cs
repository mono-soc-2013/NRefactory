// 
// NonReadonlyReferencedInGetHashCodeIssue.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013  Ji Kun <jikun.nus@gmail.com>
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Non-readonly field referenced in “GetHashCode()” 
	/// </summary>
	[IssueDescription("Warning for non-readonly field referenced in “GetHashCode()”",
	                  Description= "Warning for non-readonly field referenced in “GetHashCode()”",
			Category = IssueCategories.CodeQualityIssues,
			Severity = Severity.Warning,
			IssueMarker = IssueMarker.WavedLine,
			ResharperDisableKeyword = "NonReadonlyReferencedInGetHashCode")]
	public class NonReadonlyReferencedInGetHashCodeIssue : GatherVisitorCodeIssueProvider
	{	
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}
		
		class GatherVisitor : GatherVisitorBase<NonReadonlyReferencedInGetHashCodeIssue>
		{	
			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
			}

			public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
			{
				base.VisitIdentifierExpression(identifierExpression);
		
				var method = identifierExpression.GetParent<MethodDeclaration>();
				if (method == null || !method.Name.Equals("GetHashCode") || !method.HasModifier(Modifiers.Override))
					return;
				var type = identifierExpression.GetParent<TypeDeclaration>();
				if (type == null)
					return;

				var typeResolveResult = ctx.Resolve(type) as TypeResolveResult;
				if (typeResolveResult == null)
					return;
				
				var members = typeResolveResult.Type.GetMembers();

				foreach (var member in members) {
					if (member is IField) {
						if (member.Name.Equals(identifierExpression.Identifier) && !member.IsStatic)
						if (!(member as IField).IsReadOnly) {
							AddIssue(identifierExpression, ctx.TranslateString("Make field readonly"));
						}
					}
				}
			}
		}
	}
}