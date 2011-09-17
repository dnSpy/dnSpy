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
	/// Represents a specialized IMethod (e.g. after type substitution).
	/// </summary>
	public class SpecializedMethod : SpecializedParameterizedMember, IMethod
	{
		readonly IMethod methodDefinition;
		readonly IList<IType> typeArguments;
		
		public SpecializedMethod(IType declaringType, IMethod methodDefinition, IList<IType> typeArguments = null)
			: this(declaringType, methodDefinition, typeArguments, GetSubstitution(declaringType, typeArguments), null)
		{
		}
		
		internal SpecializedMethod(IType declaringType, IMethod methodDefinition, IList<IType> typeArguments, TypeVisitor substitution, ITypeResolveContext context) : base(declaringType, methodDefinition, substitution, context)
		{
			this.methodDefinition = methodDefinition;
			
			if (typeArguments != null) {
				if (typeArguments.Count != methodDefinition.TypeParameters.Count)
					throw new ArgumentException("Number of type arguments does not match number of type parameters");
				this.typeArguments = typeArguments;
			} else {
				this.typeArguments = EmptyList<IType>.Instance;
			}
		}
		
		internal static TypeVisitor GetSubstitution(IType declaringType, IList<IType> typeArguments)
		{
			ParameterizedType pt = declaringType as ParameterizedType;
			if (pt != null)
				return pt.GetSubstitution(typeArguments);
			else if (typeArguments != null)
				return new TypeParameterSubstitution(null, typeArguments);
			else
				return null;
		}
		
		/// <summary>
		/// Gets the type arguments passed to this method.
		/// </summary>
		public IList<IType> TypeArguments {
			get { return typeArguments; }
		}
		
		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			unchecked {
				for (int i = 0; i < typeArguments.Count; i++) {
					hashCode *= 362631391;
					hashCode += typeArguments[i].GetHashCode();
				}
			}
			return hashCode;
		}
		
		public override bool Equals(object obj)
		{
			SpecializedMethod other = obj as SpecializedMethod;
			if (!base.Equals(other))
				return false;
			if (typeArguments.Count != other.typeArguments.Count)
				return false;
			for (int i = 0; i < typeArguments.Count; i++) {
				if (!typeArguments[i].Equals(other.typeArguments[i]))
					return false;
			}
			return true;
		}
		
		public IList<IAttribute> ReturnTypeAttributes {
			get { return methodDefinition.ReturnTypeAttributes; }
		}
		
		public IList<ITypeParameter> TypeParameters {
			get { return methodDefinition.TypeParameters; }
		}
		
		public bool IsExtensionMethod {
			get { return methodDefinition.IsExtensionMethod; }
		}
		
		public bool IsConstructor {
			get { return methodDefinition.IsConstructor; }
		}
		
		public bool IsDestructor {
			get { return methodDefinition.IsDestructor; }
		}
		
		public bool IsOperator {
			get { return methodDefinition.IsOperator; }
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(GetType().Name);
			b.Append(' ');
			b.Append(this.DeclaringType.ToString());
			b.Append('.');
			b.Append(this.Name);
			if (typeArguments != null && typeArguments.Count > 0) {
				b.Append('[');
				for (int i = 0; i < typeArguments.Count; i++) {
					if (i > 0) b.Append(", ");
					b.Append(typeArguments[i].ToString());
				}
				b.Append(']');
			}
			b.Append('(');
			for (int i = 0; i < this.Parameters.Count; i++) {
				if (i > 0) b.Append(", ");
				b.Append(this.Parameters[i].ToString());
			}
			b.Append("):");
			b.Append(this.ReturnType.ToString());
			b.Append(']');
			return b.ToString();
		}
	}
}
