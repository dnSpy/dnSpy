//
// ParameterCouldBeDeclaredWithBaseTypeIssue.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;
using System;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("A parameter can be demoted to base class",
	                   Description = "Finds parameters that can be demoted to a base class.",
	                   Category = IssueCategories.Opportunities,
	                   Severity = Severity.Suggestion)]
	public class ParameterCanBeDemotedIssue : ICodeIssueProvider
	{
		bool tryResolve;

		public ParameterCanBeDemotedIssue() : this (true)
		{
		}

		public ParameterCanBeDemotedIssue(bool tryResolve)
		{
			this.tryResolve = tryResolve;
		}

		#region ICodeIssueProvider implementation
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			var sw = new Stopwatch();
			sw.Start();
			var gatherer = new GatherVisitor(context, tryResolve);
			var issues = gatherer.GetIssues();
			sw.Stop();
			Console.WriteLine("Elapsed time in ParameterCanBeDemotedIssue: {0} (Checked types: {3, 4} Qualified for resolution check: {5, 4} Members with issues: {4, 4} Method bodies resolved: {2, 4} File: '{1}')",
			                  sw.Elapsed, context.UnresolvedFile.FileName, gatherer.MethodResolveCount, gatherer.TypesChecked, gatherer.MembersWithIssues, gatherer.TypeResolveCount);
			return issues;
		}
		#endregion

		class GatherVisitor : GatherVisitorBase
		{
			bool tryResolve;
			
			public GatherVisitor(BaseRefactoringContext context, bool tryResolve) : base (context)
			{
				this.tryResolve = tryResolve;
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				var eligibleParameters = methodDeclaration.Parameters
					.Where(p => p.ParameterModifier != ParameterModifier.Out && p.ParameterModifier != ParameterModifier.Ref)
					.ToList();
				if (eligibleParameters.Count == 0)
					return;
				var declarationResolveResult = ctx.Resolve(methodDeclaration) as MemberResolveResult;
				if (declarationResolveResult == null)
					return;
				var member = declarationResolveResult.Member;
				if (member.IsOverride || member.IsOverridable || member.ImplementedInterfaceMembers.Any())
					return;

				var collector = new TypeCriteriaCollector(ctx);
				methodDeclaration.AcceptVisitor(collector);
				
				foreach (var parameter in eligibleParameters) {
					ProcessParameter(parameter, methodDeclaration.Body, collector);
				}
			}

			void ProcessParameter(ParameterDeclaration parameter, AstNode rootResolutionNode, TypeCriteriaCollector collector)
			{
				var localResolveResult = ctx.Resolve(parameter) as LocalResolveResult;
				var variable = localResolveResult.Variable;
				var typeKind = variable.Type.Kind;
				if (!(typeKind == TypeKind.Class ||
					  typeKind == TypeKind.Struct ||
					  typeKind == TypeKind.Interface ||
					  typeKind == TypeKind.Array) ||
				    parameter.Type is PrimitiveType ||
					!collector.UsedVariables.Contains(variable)) {
					return;
				}

				var candidateTypes = localResolveResult.Type.GetAllBaseTypes().ToList();
				TypesChecked += candidateTypes.Count;
				var criterion = collector.GetCriterion(variable);

				var possibleTypes = 
					(from type in candidateTypes
					 where !type.Equals(localResolveResult.Type) && criterion.SatisfiedBy(type)
					 select type).ToList();

				TypeResolveCount += possibleTypes.Count;
				var validTypes = 
					(from type in possibleTypes
					 where !tryResolve || TypeChangeResolvesCorrectly(parameter, rootResolutionNode, type)
					 orderby GetInheritanceDepth(type) ascending
					 select type).ToList();
				if (validTypes.Any()) {
					AddIssue(parameter, ctx.TranslateString("Parameter can be demoted to base class"), GetActions(parameter, validTypes));
					MembersWithIssues++;
				}
			}

			internal int TypeResolveCount = 0;
			internal int TypesChecked = 0;
			internal int MembersWithIssues = 0;
			internal int MethodResolveCount = 0;

			bool TypeChangeResolvesCorrectly(ParameterDeclaration parameter, AstNode rootNode, IType type)
			{
				MethodResolveCount++;
				var resolver = ctx.GetResolverStateBefore(rootNode);
				resolver = resolver.AddVariable(new DefaultParameter(type, parameter.Name));
				var astResolver = new CSharpAstResolver(resolver, rootNode, ctx.UnresolvedFile);
				var validator = new TypeChangeValidationNavigator();
				astResolver.ApplyNavigator(validator, ctx.CancellationToken);
				return !validator.FoundErrors;
			}

			IEnumerable<CodeAction> GetActions(ParameterDeclaration parameter, IEnumerable<IType> possibleTypes)
			{
				var csResolver = ctx.Resolver.GetResolverStateBefore(parameter);
				var astBuilder = new TypeSystemAstBuilder(csResolver);
				foreach (var type in possibleTypes) {
					var localType = type;
					var message = string.Format(ctx.TranslateString("Demote parameter to '{0}'"), type.FullName);
					yield return new CodeAction(message, script => {
						script.Replace(parameter.Type, astBuilder.ConvertType(localType));
					});
				}
			}

			int GetInheritanceDepth(IType declaringType)
			{
				var depth = 0;
				foreach (var baseType in declaringType.DirectBaseTypes) {
					var newDepth = GetInheritanceDepth(baseType);
					depth = Math.Max(depth, newDepth);
				}
				return depth;
			}
		}

		class TypeChangeValidationNavigator : IResolveVisitorNavigator
		{
			public bool FoundErrors { get; private set; }

			#region IResolveVisitorNavigator implementation
			public ResolveVisitorNavigationMode Scan(AstNode node)
			{
				if (FoundErrors)
					return ResolveVisitorNavigationMode.Skip;
				return ResolveVisitorNavigationMode.Resolve;
			}

			public void Resolved(AstNode node, ResolveResult result)
			{
				bool errors = result.IsError;
				FoundErrors |= result.IsError;
			}

			public void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				// no-op
			}
			#endregion
			
		}
	}
}

