// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Converts extension method calls into infix syntax.
	/// </summary>
	public class IntroduceExtensionMethods : IAstTransform
	{
		readonly DecompilerContext context;
		
		public IntroduceExtensionMethods(DecompilerContext context)
		{
			this.context = context;
		}
		
		public void Run(AstNode compilationUnit)
		{
			foreach (InvocationExpression invocation in compilationUnit.Descendants.OfType<InvocationExpression>()) {
				MemberReferenceExpression mre = invocation.Target as MemberReferenceExpression;
				MethodReference methodReference = invocation.Annotation<MethodReference>();
				if (mre != null && mre.Target is TypeReferenceExpression && methodReference != null && invocation.Arguments.Any()) {
					MethodDefinition d = methodReference.Resolve();
					if (d != null) {
						foreach (var ca in d.CustomAttributes) {
							if (ca.AttributeType.Name == "ExtensionAttribute" && ca.AttributeType.Namespace == "System.Runtime.CompilerServices") {
								mre.Target = invocation.Arguments.First().Detach();
								mre.TypeArguments.Clear();
								break;
							}
						}
					}
				}
			}
		}
	}
}
