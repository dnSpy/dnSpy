// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Helper methods for managing using declarations.
	/// </summary>
	public class UsingHelper
	{
		/// <summary>
		/// Inserts 'using ns;' in the current scope, and then removes all explicit
		/// usages of ns that were made redundant by the new using.
		/// </summary>
		public static void InsertUsingAndRemoveRedundantNamespaceUsage(RefactoringContext context, Script script, string ns)
		{
			InsertUsing(context, script, new UsingDeclaration(ns));
			// TODO: remove the usages that were made redundant
		}
		
		/// <summary>
		/// Inserts 'newUsing' in the current scope.
		/// This method will try to insert new usings in the correct position (depending on
		/// where the existing usings are; and maintaining the sort order).
		/// </summary>
		public static void InsertUsing(RefactoringContext context, Script script, AstNode newUsing)
		{
			UsingInfo newUsingInfo = new UsingInfo(newUsing, context);
			AstNode enclosingNamespace = context.GetNode<NamespaceDeclaration>() ?? context.RootNode;
			// Find nearest enclosing parent that has usings:
			AstNode usingParent = enclosingNamespace;
			while (usingParent != null && !usingParent.Children.OfType<UsingDeclaration>().Any())
				usingParent = usingParent.Parent;
			if (usingParent == null) {
				// No existing usings at all -> use the default location
				if (script.FormattingOptions.UsingPlacement == UsingPlacement.TopOfFile) {
					usingParent = context.RootNode;
				} else {
					usingParent = enclosingNamespace;
				}
			}
			// Find the main block of using declarations in the chosen scope:
			AstNode blockStart = usingParent.Children.FirstOrDefault(IsUsingDeclaration);
			AstNode insertionPoint;
			bool insertAfter = false;
			if (blockStart == null) {
				// no using declarations in the file
				Debug.Assert(SyntaxTree.MemberRole == NamespaceDeclaration.MemberRole);
				insertionPoint = usingParent.GetChildrenByRole(SyntaxTree.MemberRole).SkipWhile(CanAppearBeforeUsings).FirstOrDefault();
			} else {
				insertionPoint = blockStart;
				while (IsUsingFollowing (ref insertionPoint) && newUsingInfo.CompareTo(new UsingInfo(insertionPoint, context)) > 0)
					insertionPoint = insertionPoint.NextSibling;
				if (!IsUsingDeclaration(insertionPoint)) {
					// Insert after last using instead of before next node
					// This affects where empty lines get placed.
					insertionPoint = insertionPoint.PrevSibling;
					insertAfter = true;
				}
			}
			if (insertionPoint != null) {
				if (insertAfter)
					script.InsertAfter(insertionPoint, newUsing);
				else
					script.InsertBefore(insertionPoint, newUsing);
			}
		}

		static bool IsUsingFollowing(ref AstNode insertionPoint)
		{
			var node = insertionPoint;
			while (node != null && node.Role == Roles.NewLine)
				node = node.NextSibling;
			if (IsUsingDeclaration(node)) {
				insertionPoint = node;
				return true;
			}
			return false;
		}
		
		static bool IsUsingDeclaration(AstNode node)
		{
			return node is UsingDeclaration || node is UsingAliasDeclaration;
		}
		
		static bool CanAppearBeforeUsings(AstNode node)
		{
			if (node is ExternAliasDeclaration)
				return true;
			if (node is PreProcessorDirective)
				return true;
			if (node is NewLineNode)
				return true;
			Comment c = node as Comment;
			if (c != null)
				return !c.IsDocumentation;
			return false;
		}
		
		/// <summary>
		/// Sorts the specified usings.
		/// </summary>
		public static IEnumerable<AstNode> SortUsingBlock(IEnumerable<AstNode> nodes, BaseRefactoringContext context)
		{
			var infos = nodes.Select(_ => new UsingInfo(_, context));
			var orderedInfos = infos.OrderBy(_ => _);
			var orderedNodes = orderedInfos.Select(_ => _.Node);

			return orderedNodes;
		}


		private sealed class UsingInfo : IComparable<UsingInfo>
		{
			public AstNode Node;

			public string Alias;
			public string Name;

			public bool IsAlias;
			public bool HasTypesFromOtherAssemblies;
			public bool IsSystem;

			public UsingInfo(AstNode node, BaseRefactoringContext context)
			{
				var importAndAlias = GetImportAndAlias(node);

				Node = node;

				Alias = importAndAlias.Item2;
				Name = importAndAlias.Item1.ToString();

				IsAlias = Alias != null;

				ResolveResult rr;
				if (node.Ancestors.Contains(context.RootNode)) {
					rr = context.Resolve(importAndAlias.Item1);
				} else {
					// It's possible that we're looking at a new using that
					// isn't part of the AST.
					var resolver = new CSharpAstResolver(new CSharpResolver(context.Compilation), node);
					rr = resolver.Resolve(importAndAlias.Item1);
				}
				
				var nrr = rr as NamespaceResolveResult;
				HasTypesFromOtherAssemblies = nrr != null && nrr.Namespace.ContributingAssemblies.Any(a => !a.IsMainAssembly);

				IsSystem = HasTypesFromOtherAssemblies && (Name == "System" || Name.StartsWith("System.", StringComparison.Ordinal));
			}

			private static Tuple<AstType, string> GetImportAndAlias(AstNode node)
			{
				var plainUsing = node as UsingDeclaration;
				if (plainUsing != null)
					return Tuple.Create(plainUsing.Import, (string)null);
				
				var aliasUsing = node as UsingAliasDeclaration;
				if (aliasUsing != null)
					return Tuple.Create(aliasUsing.Import, aliasUsing.Alias);

				throw new InvalidOperationException(string.Format("Invalid using node: {0}", node));
			}

			public int CompareTo(UsingInfo y)
			{
				UsingInfo x = this;
				if (x.IsAlias != y.IsAlias)
					return x.IsAlias ? 1 : -1;
				if (x.IsAlias)
					return StringComparer.OrdinalIgnoreCase.Compare(x.Alias, y.Alias);
//				if (x.HasTypesFromOtherAssemblies != y.HasTypesFromOtherAssemblies)
//					return x.HasTypesFromOtherAssemblies ? -1 : 1;
				if (x.IsSystem != y.IsSystem)
					return x.IsSystem ? -1 : 1;
				return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
			}
		}
	}
}
