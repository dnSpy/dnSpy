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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// An unknown type where (part) of the name is known.
	/// </summary>
	[Serializable]
	public class UnknownType : AbstractType, ITypeReference
	{
		readonly string namespaceName;
		readonly string name;
		readonly int typeParameterCount;
		
		/// <summary>
		/// Creates a new unknown type.
		/// </summary>
		/// <param name="namespaceName">Namespace name, if known. Can be null if unknown.</param>
		/// <param name="name">Name of the type, must not be null.</param>
		/// <param name="typeParameterCount">Type parameter count, zero if unknown.</param>
		public UnknownType(string namespaceName, string name, int typeParameterCount = 0)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.namespaceName = namespaceName;
			this.name = name;
			this.typeParameterCount = typeParameterCount;
		}
		
		public override TypeKind Kind {
			get { return TypeKind.Unknown; }
		}
		
		public override ITypeReference ToTypeReference()
		{
			return this;
		}
		
		IType ITypeReference.Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			return this;
		}
		
		public override string Name {
			get { return name; }
		}
		
		public override string Namespace {
			get { return namespaceName ?? string.Empty; }
		}
		
		public override string ReflectionName {
			get { return "?"; }
		}
		
		public override bool? IsReferenceType {
			get { return null; }
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (namespaceName != null)
					hashCode += 1000000007 * namespaceName.GetHashCode();
				hashCode += 1000000009 * name.GetHashCode();
				hashCode += 1000000021 * typeParameterCount.GetHashCode();
			}
			return hashCode;
		}
		
		public override bool Equals(IType other)
		{
			UnknownType o = other as UnknownType;
			if (o == null)
				return false;
			return this.namespaceName == o.namespaceName && this.name == o.name && this.typeParameterCount == o.typeParameterCount;
		}
		
		public override string ToString()
		{
			return "[UnknownType " + this.FullName + "]";
		}
	}
}
