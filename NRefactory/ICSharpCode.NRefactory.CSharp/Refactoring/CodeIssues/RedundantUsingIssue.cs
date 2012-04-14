// 
// RedundantUsingInspector.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using ICSharpCode.NRefactory.PatternMatching;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant using declarations.
	/// </summary>
	[IssueDescription("Remove unused usings",
	                  Description = "Removes used declarations that are not required.",
	                  Category = IssueCategories.Redundancies,
	                  Severity = Severity.Hint,
	                  IssueMarker = IssueMarker.GrayOut)]
	public class RedundantUsingIssue : ICodeIssueProvider
	{
		List<string> namespacesToKeep = new List<string>();
		
		/// <summary>
		/// The list of namespaces that should be kept even if they are not being used.
		/// Used in SharpDevelop to always keep the "System" namespace around.
		/// </summary>
		public IList<string> NamespacesToKeep {
			get { return namespacesToKeep; }
		}
		
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			var visitor = new GatherVisitor (context, this);
			context.RootNode.AcceptVisitor (visitor);
			visitor.Collect ();
			return visitor.FoundIssues;
		}

		class GatherVisitor : GatherVisitorBase
		{
			readonly RedundantUsingIssue inspector;
			Dictionary<UsingDeclaration, bool> isInUse = new Dictionary<UsingDeclaration, bool>();
			Dictionary<string, UsingDeclaration> namespaceToUsingDecl = new Dictionary<string, UsingDeclaration>();
			
			public GatherVisitor (BaseRefactoringContext ctx, RedundantUsingIssue inspector) : base (ctx)
			{
				this.inspector = inspector;
			}

			public void Collect()
			{
				foreach (var u in isInUse.Where (u => !u.Value)) {
					var decl = u.Key;
					AddIssue(decl, ctx.TranslateString("Remove redundant usings"), script => {
						foreach (var u2 in isInUse.Where (a => !a.Value)) {
							script.Remove (u2.Key);
						}
					}
					);
				}
			}

			public override void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
			{
				base.VisitUsingDeclaration(usingDeclaration);
				var nrr = ctx.Resolve(usingDeclaration.Import) as NamespaceResolveResult;
				if (nrr != null) {
					isInUse[usingDeclaration] = inspector.namespacesToKeep.Contains(nrr.NamespaceName);
					namespaceToUsingDecl[nrr.NamespaceName] = usingDeclaration;
				}
			}
			
			public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
			{
				var oldNamespaceToUsingDecl = new Dictionary<string, UsingDeclaration>(namespaceToUsingDecl);
				base.VisitNamespaceDeclaration(namespaceDeclaration);
				namespaceToUsingDecl = oldNamespaceToUsingDecl;
			}
			
			void UseNamespace(string ns)
			{
				UsingDeclaration decl;
				if (namespaceToUsingDecl.TryGetValue(ns, out decl)) {
					isInUse[decl] = true;
				}
			}

			public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
			{
				base.VisitIdentifierExpression(identifierExpression);
				var trr = ctx.Resolve(identifierExpression) as TypeResolveResult;
				if (trr != null) {
					UseNamespace(trr.Type.Namespace);
				}
			}

			public override void VisitSimpleType(SimpleType simpleType)
			{
				base.VisitSimpleType(simpleType);
				UseNamespace(ctx.Resolve(simpleType).Type.Namespace);
			}

			public override void VisitInvocationExpression (InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression (invocationExpression);
				UseExtensionMethod(ctx.Resolve(invocationExpression));
			}
			
			void UseExtensionMethod(ResolveResult rr)
			{
				var mg = rr as CSharpInvocationResolveResult;
				if (mg != null && mg.IsExtensionMethodInvocation) {
					UseNamespace (mg.Member.DeclaringType.Namespace);
				}
			}
			
			public override void VisitQueryExpression(QueryExpression queryExpression)
			{
				base.VisitQueryExpression(queryExpression);
				foreach (var clause in queryExpression.Clauses) {
					UseExtensionMethod(ctx.Resolve(clause));
				}
			}
		}
	}
}
