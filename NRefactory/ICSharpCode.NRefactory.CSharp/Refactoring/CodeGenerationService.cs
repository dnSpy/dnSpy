//
// CodeGenerationService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public abstract class CodeGenerationService
	{
		public abstract EntityDeclaration GenerateMemberImplementation(RefactoringContext context, IMember member, bool explicitImplementation);
	}

	public class DefaultCodeGenerationService : CodeGenerationService
	{
		public override EntityDeclaration GenerateMemberImplementation(RefactoringContext context, IMember member, bool explicitImplementation)
		{
			var builder = context.CreateTypeSystemAstBuilder();
			builder.GenerateBody = true;
			builder.ShowModifiers = false;
			builder.ShowAccessibility = true;
			builder.ShowConstantValues = !explicitImplementation;
			builder.ShowTypeParameterConstraints = !explicitImplementation;
			builder.UseCustomEvents = explicitImplementation;
			var decl = builder.ConvertEntity(member);
			if (explicitImplementation) {
				decl.Modifiers = Modifiers.None;
				decl.AddChild(builder.ConvertType(member.DeclaringType), EntityDeclaration.PrivateImplementationTypeRole);
			} else if (member.DeclaringType.Kind == TypeKind.Interface) {
				decl.Modifiers |= Modifiers.Public;
			} else {
				// Remove 'internal' modifier from 'protected internal' members if the override is in a different assembly than the member
				if (!member.ParentAssembly.InternalsVisibleTo(context.Compilation.MainAssembly)) {
					decl.Modifiers &= ~Modifiers.Internal;
				}
			}
			return decl;
		}
	}
}

