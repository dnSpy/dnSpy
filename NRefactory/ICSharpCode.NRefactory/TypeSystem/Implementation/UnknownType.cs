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
using System.Diagnostics;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// An unknown type where (part) of the name is known.
	/// </summary>
	[Serializable]
	public class UnknownType : AbstractType, ITypeReference
	{
		readonly bool namespaceKnown;
		readonly FullTypeName fullTypeName;
		
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
			this.namespaceKnown = namespaceName != null;
			this.fullTypeName = new TopLevelTypeName(namespaceName ?? string.Empty, name, typeParameterCount);
		}
		
		/// <summary>
		/// Creates a new unknown type.
		/// </summary>
		/// <param name="fullTypeName">Full name of the unknown type.</param>
		public UnknownType(FullTypeName fullTypeName)
		{
			if (fullTypeName.Name == null) {
				Debug.Assert(fullTypeName == default(FullTypeName));
				this.namespaceKnown = false;
				this.fullTypeName = new TopLevelTypeName(string.Empty, "?", 0);
			} else {
				this.namespaceKnown = true;
				this.fullTypeName = fullTypeName;
			}
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
			get { return fullTypeName.Name; }
		}
		
		public override string Namespace {
			get { return fullTypeName.TopLevelTypeName.Namespace; }
		}
		
		public override string ReflectionName {
			get { return namespaceKnown ? fullTypeName.ReflectionName : "?"; }
		}
		
		public override int TypeParameterCount {
			get { return fullTypeName.TypeParameterCount; }
		}
		
		public override bool? IsReferenceType {
			get { return null; }
		}
		
		public override int GetHashCode()
		{
			return (namespaceKnown ? 812571 : 12651) ^ fullTypeName.GetHashCode();
		}
		
		public override bool Equals(IType other)
		{
			UnknownType o = other as UnknownType;
			if (o == null)
				return false;
			return this.namespaceKnown == o.namespaceKnown && this.fullTypeName == o.fullTypeName;
		}
		
		public override string ToString()
		{
			return "[UnknownType " + fullTypeName.ReflectionName + "]";
		}
	}
}
