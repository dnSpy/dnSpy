// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
								var firstArgument = invocation.Arguments.First();
								if (firstArgument is NullReferenceExpression)
									firstArgument = firstArgument.ReplaceWith(expr => expr.CastTo(AstBuilder.ConvertType(d.Parameters.First().ParameterType)));
								else
									mre.Target = firstArgument.Detach();
								if (invocation.Arguments.Any()) {
									// HACK: removing type arguments should be done indepently from whether a method is an extension method,
									// just by testing whether the arguments can be inferred
									mre.TypeArguments.Clear();
								}
								break;
							}
						}
					}
				}
			}
		}
	}
}
