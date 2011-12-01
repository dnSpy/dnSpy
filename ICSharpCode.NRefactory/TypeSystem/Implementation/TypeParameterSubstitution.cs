// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Substitues class and method type parameters.
	/// </summary>
	public class TypeParameterSubstitution : TypeVisitor
	{
		readonly IList<IType> classTypeArguments;
		readonly IList<IType> methodTypeArguments;
		
		/// <summary>
		/// Creates a new type parameter substitution.
		/// </summary>
		/// <param name="classTypeArguments">
		/// The type arguments to substitute for class type parameters.
		/// Pass <c>null</c> to keep class type parameters unmodified.
		/// </param>
		/// <param name="methodTypeArguments">
		/// The type arguments to substitute for method type parameters.
		/// Pass <c>null</c> to keep method type parameters unmodified.
		/// </param>
		public TypeParameterSubstitution(IList<IType> classTypeArguments, IList<IType> methodTypeArguments)
		{
			this.classTypeArguments = classTypeArguments;
			this.methodTypeArguments = methodTypeArguments;
		}
		
		public override IType VisitTypeParameter(ITypeParameter type)
		{
			int index = type.Index;
			if (classTypeArguments != null && type.OwnerType == EntityType.TypeDefinition) {
				if (index >= 0 && index < classTypeArguments.Count)
					return classTypeArguments[index];
				else
					return SpecialType.UnknownType;
			} else if (methodTypeArguments != null && type.OwnerType == EntityType.Method) {
				if (index >= 0 && index < methodTypeArguments.Count)
					return methodTypeArguments[index];
				else
					return SpecialType.UnknownType;
			} else {
				return base.VisitTypeParameter(type);
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append('[');
			bool first = true;
			if (classTypeArguments != null) {
				for (int i = 0; i < classTypeArguments.Count; i++) {
					if (first) first = false; else b.Append(", ");
					b.Append('`');
					b.Append(i);
					b.Append(" -> ");
					b.Append(classTypeArguments[i]);
				}
			}
			if (methodTypeArguments != null) {
				for (int i = 0; i < methodTypeArguments.Count; i++) {
					if (first) first = false; else b.Append(", ");
					b.Append("``");
					b.Append(i);
					b.Append(" -> ");
					b.Append(methodTypeArguments[i]);
				}
			}
			b.Append(']');
			return b.ToString();
		}
	}
}
