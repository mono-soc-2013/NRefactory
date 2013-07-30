﻿// 
// RedundantBaseTypeIssue.cs
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
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Type is either mentioned in the base type list of other part, or it is interface and appears as other's type base and contains no explicit implementation.
	/// </summary>
	[IssueDescription("Remove redundant base type specifaction in the list",
	                  Description= "Remove redundant base type specifaction in the list",
	                  Category = IssueCategories.Redundancies,
	                  Severity = Severity.Warning,
	                  IssueMarker = IssueMarker.GrayOut,
	                  ResharperDisableKeyword = "RedundantBaseType")]
	public class RedundantBaseTypeIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase<RedundantBaseTypeIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantBaseTypeIssue issueProvider) : base (ctx, issueProvider)
			{
			}
			
			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				if (typeDeclaration == null)
					return;
				
				base.VisitTypeDeclaration(typeDeclaration);
				
				if (typeDeclaration.BaseTypes.Count == 0)
					return;
				
				List<AstNode> redundantBase = new List<AstNode>();
				var type = ctx.Resolve(typeDeclaration).Type;
				
				if (typeDeclaration.HasModifier(Modifiers.Partial)) {
					var parts = type.GetDefinition().Parts;
					foreach (var node in typeDeclaration.BaseTypes) {
						int count = 0;
						foreach (var unresolvedTypeDefinition in parts) {
							var baseTypes = unresolvedTypeDefinition.BaseTypes;
							
							if (baseTypes.Any(f => f.ToString().Equals(node.ToString()))) {
								count++;
								if (count > 1) {
									if (!redundantBase.Contains(node))
										redundantBase.Add(node);
									break;
								}
							}
						}
					}
				}
				
				var directBaseType = type.DirectBaseTypes.Where(f => f.Kind == TypeKind.Class);
				if (directBaseType.Count() != 1)
					return;
				var members = type.GetMembers();
				var memberDeclaration = typeDeclaration.Members;
				var interfaceBase = typeDeclaration.BaseTypes.Where(f => ctx.Resolve(f).Type.GetDefinition().Kind == TypeKind.Interface);
				foreach (var node in interfaceBase) {
					if (directBaseType.Single().GetDefinition().GetAllBaseTypeDefinitions().Any(f => f.Name.Equals(node.ToString()))) {
						bool flag = false;
						foreach (var member in members) {
							if (!memberDeclaration.Any(f => f.Name.Equals(member.Name))) {
								continue;
							}
							if (
								member.ImplementedInterfaceMembers.Any(
								g => g.DeclaringType.Name.Equals(node.ToString()))) {
								flag = true;
								break;
							}
						}
						if (!flag) {
							if (!redundantBase.Contains(node))
								redundantBase.Add(node);
						}
					}			
				}
				foreach (var node in redundantBase) {
					AddIssue(node, ctx.TranslateString("Remove redundant base specification"), Script =>
					{
						if (typeDeclaration.GetCSharpNodeBefore(node).ToString().Equals(":")) {
							if (node.GetNextNode().Role != Roles.BaseType) {
								Script.Remove(typeDeclaration.GetCSharpNodeBefore(node));
							}
						}
						if (typeDeclaration.GetCSharpNodeBefore(node).ToString().Equals(",")) {
							Script.Remove(typeDeclaration.GetCSharpNodeBefore(node));
						}
						Script.Remove(node);

					}
					);
				}
			}
			
		}
	}
}