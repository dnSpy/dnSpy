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

using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Decompiler.Shared;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Converts extension method calls into infix syntax.
	/// </summary>
	public class IntroduceExtensionMethods : IAstTransformPoolObject
	{
		readonly StringBuilder stringBuilder;

		public IntroduceExtensionMethods(DecompilerContext context)
		{
			this.stringBuilder = new StringBuilder();
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
		}

		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String extensionAttributeString = new UTF8String("ExtensionAttribute");
		public void Run(AstNode compilationUnit)
		{
			foreach (InvocationExpression invocation in compilationUnit.Descendants.OfType<InvocationExpression>()) {
				MemberReferenceExpression mre = invocation.Target as MemberReferenceExpression;
				IMethod methodReference = invocation.Annotation<IMethod>();
				if (mre != null && mre.Target is TypeReferenceExpression && methodReference != null && invocation.Arguments.Any()) {
					MethodDef d = methodReference.Resolve();
					if (d != null) {
						var ca = d.Find(systemRuntimeCompilerServicesString, extensionAttributeString);
						if (ca != null) {
							var firstArgument = invocation.Arguments.First();
							if (firstArgument is NullReferenceExpression)
								firstArgument = firstArgument.ReplaceWith(expr => expr.CastTo(AstBuilder.ConvertType(d.Parameters.SkipNonNormal().First().Type, stringBuilder)));
							else {
								var ilRanges = mre.Target.GetAllRecursiveILRanges();
								mre.Target = firstArgument.Detach();
								if (ilRanges.Count > 0)
									mre.Target.AddAnnotation(ilRanges);
							}
							if (invocation.Arguments.Any()) {
								// HACK: removing type arguments should be done indepently from whether a method is an extension method,
								// just by testing whether the arguments can be inferred
								var ilRanges = mre.TypeArguments.GetAllRecursiveILRanges();
								mre.TypeArguments.Clear();
								if (ilRanges.Count > 0)
									mre.AddAnnotation(ilRanges);
							}
						}
					}
				}
			}
		}
	}
}
