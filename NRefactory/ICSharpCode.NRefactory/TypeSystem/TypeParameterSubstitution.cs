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
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Substitutes class and method type parameters.
	/// </summary>
	public class TypeParameterSubstitution : TypeVisitor
	{
		/// <summary>
		/// The identity function.
		/// </summary>
		public static readonly TypeParameterSubstitution Identity = new TypeParameterSubstitution(null, null);
		
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
		
		/// <summary>
		/// Gets the list of class type arguments.
		/// Returns <c>null</c> if this substitution keeps class type parameters unmodified.
		/// </summary>
		public IList<IType> ClassTypeArguments {
			get { return classTypeArguments; }
		}
		
		/// <summary>
		/// Gets the list of method type arguments.
		/// Returns <c>null</c> if this substitution keeps method type parameters unmodified.
		/// </summary>
		public IList<IType> MethodTypeArguments {
			get { return methodTypeArguments; }
		}
		
		#region Compose
		/// <summary>
		/// Computes a single TypeParameterSubstitution so that for all types <c>t</c>:
		/// <c>t.AcceptVisitor(Compose(g, f)) equals t.AcceptVisitor(f).AcceptVisitor(g)</c>
		/// </summary>
		/// <remarks>If you consider type parameter substitution to be a function, this is function composition.</remarks>
		public static TypeParameterSubstitution Compose(TypeParameterSubstitution g, TypeParameterSubstitution f)
		{
			if (g == null)
				return f;
			if (f == null || (f.classTypeArguments == null && f.methodTypeArguments == null))
				return g;
			// The composition is a copy of 'f', with 'g' applied on the array elements.
			// If 'f' has a null list (keeps type parameters unmodified), we have to treat it as
			// the identity function, and thus use the list from 'g'.
			var classTypeArguments = f.classTypeArguments != null ? GetComposedTypeArguments(f.classTypeArguments, g) : g.classTypeArguments;
			var methodTypeArguments = f.methodTypeArguments != null ? GetComposedTypeArguments(f.methodTypeArguments, g) : g.methodTypeArguments;
			return new TypeParameterSubstitution(classTypeArguments, methodTypeArguments);
		}
		
		static IList<IType> GetComposedTypeArguments(IList<IType> input, TypeParameterSubstitution substitution)
		{
			IType[] result = new IType[input.Count];
			for (int i = 0; i < result.Length; i++) {
				result[i] = input[i].AcceptVisitor(substitution);
			}
			return result;
		}
		#endregion
		
		#region Equals and GetHashCode implementation
		public override bool Equals(object obj)
		{
			TypeParameterSubstitution other = obj as TypeParameterSubstitution;
			if (other == null)
				return false;
			return TypeListEquals(classTypeArguments, other.classTypeArguments)
				&& TypeListEquals(methodTypeArguments, other.methodTypeArguments);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return 1124131 * TypeListHashCode(classTypeArguments) + 1821779 * TypeListHashCode(methodTypeArguments);
			}
		}
		
		static bool TypeListEquals(IList<IType> a, IList<IType> b)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!a[i].Equals(b[i]))
					return false;
			}
			return true;
		}
		
		static int TypeListHashCode(IList<IType> obj)
		{
			if (obj == null)
				return 0;
			unchecked {
				int hashCode = 1;
				foreach (var element in obj) {
					hashCode *= 27;
					hashCode += element.GetHashCode();
				}
				return hashCode;
			}
		}
		#endregion
		
		public override IType VisitTypeParameter(ITypeParameter type)
		{
			int index = type.Index;
			if (classTypeArguments != null && type.OwnerType == SymbolKind.TypeDefinition) {
				if (index >= 0 && index < classTypeArguments.Count)
					return classTypeArguments[index];
				else
					return SpecialType.UnknownType;
			} else if (methodTypeArguments != null && type.OwnerType == SymbolKind.Method) {
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
					b.Append(classTypeArguments[i].ReflectionName);
				}
			}
			if (methodTypeArguments != null) {
				for (int i = 0; i < methodTypeArguments.Count; i++) {
					if (first) first = false; else b.Append(", ");
					b.Append("``");
					b.Append(i);
					b.Append(" -> ");
					b.Append(methodTypeArguments[i].ReflectionName);
				}
			}
			b.Append(']');
			return b.ToString();
		}
	}
}
