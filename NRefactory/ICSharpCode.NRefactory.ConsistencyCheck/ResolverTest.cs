/*
 * Created by SharpDevelop.
 * User: Daniel
 * Date: 12/9/2011
 * Time: 01:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	/// <summary>
	/// Description of ResolverTest.
	/// </summary>
	public class ResolverTest
	{
		public static void RunTest(CSharpFile file)
		{
			CSharpAstResolver resolver = new CSharpAstResolver(file.Project.Compilation, file.CompilationUnit, file.ParsedFile);
			var navigator = new ValidatingResolveAllNavigator(file.FileName);
			resolver.ApplyNavigator(navigator, CancellationToken.None);
			navigator.Validate(file.CompilationUnit);
		}
		
		sealed class ValidatingResolveAllNavigator : IResolveVisitorNavigator
		{
			string fileName;
			
			public ValidatingResolveAllNavigator(string fileName)
			{
				this.fileName = fileName;
			}
			
			HashSet<AstNode> resolvedNodes = new HashSet<AstNode>();
			HashSet<AstNode> nodesWithConversions = new HashSet<AstNode>();
			
			public ResolveVisitorNavigationMode Scan(AstNode node)
			{
				return ResolveVisitorNavigationMode.Resolve;
			}
			
			public void Resolved(AstNode node, ResolveResult result)
			{
				if (!resolvedNodes.Add(node))
					throw new InvalidOperationException("Duplicate Resolved() call");
				if (CSharpAstResolver.IsUnresolvableNode(node))
					throw new InvalidOperationException("Resolved unresolvable node");
				
				if (result.IsError) {
					Console.WriteLine("Compiler error at " + fileName + ":" + node.StartLocation + ": " + result);
				}
			}
			
			public void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				if (!nodesWithConversions.Add(expression))
					throw new InvalidOperationException("Duplicate ProcessConversion() call");
				if (conversion == Conversion.None) {
					Console.WriteLine("Compiler error at " + fileName + ":" + expression.StartLocation + ": Cannot convert from " + result + " to " + targetType);
				}
			}
			
			public void Validate(CompilationUnit cu)
			{
				foreach (AstNode node in cu.DescendantsAndSelf.Except(resolvedNodes)) {
					if (node.NodeType != NodeType.Token) {
						if (!CSharpAstResolver.IsUnresolvableNode(node)) {
							Console.WriteLine("Forgot to resolve " + node);
						}
					}
				}
			}
		}
	}
}
