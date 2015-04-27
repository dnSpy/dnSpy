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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	/// <summary>
	/// Type reference used within an attribute.
	/// Looks up both 'withoutSuffix' and 'withSuffix' and returns the type that exists.
	/// </summary>
	[Serializable]
	public sealed class AttributeTypeReference : ITypeReference, ISupportsInterning
	{
		readonly ITypeReference withoutSuffix, withSuffix;
		
		public AttributeTypeReference(ITypeReference withoutSuffix, ITypeReference withSuffix)
		{
			if (withoutSuffix == null)
				throw new ArgumentNullException("withoutSuffix");
			if (withSuffix == null)
				throw new ArgumentNullException("withSuffix");
			this.withoutSuffix = withoutSuffix;
			this.withSuffix = withSuffix;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			IType t1 = withoutSuffix.Resolve(context);
			IType t2 = withSuffix.Resolve(context);
			return PreferAttributeTypeWithSuffix(t1, t2, context.Compilation) ? t2 : t1;
		}
		
		internal static bool PreferAttributeTypeWithSuffix(IType t1, IType t2, ICompilation compilation)
		{
			if (t2.Kind == TypeKind.Unknown) return false;
			if (t1.Kind == TypeKind.Unknown) return true;
			
			var attrTypeDef = compilation.FindType(KnownTypeCode.Attribute).GetDefinition();
			if (attrTypeDef != null) {
				bool t1IsAttribute = (t1.GetDefinition() != null && t1.GetDefinition().IsDerivedFrom(attrTypeDef));
				bool t2IsAttribute = (t2.GetDefinition() != null && t2.GetDefinition().IsDerivedFrom(attrTypeDef));
				if (t2IsAttribute && !t1IsAttribute)
					return true;
				// If both types exist and are attributes, C# considers that to be an ambiguity, but we are less strict.
			}
			return false;
		}
		
		public override string ToString()
		{
			return withoutSuffix.ToString() + "[Attribute]";
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return withoutSuffix.GetHashCode() + 715613 * withSuffix.GetHashCode();
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			AttributeTypeReference atr = other as AttributeTypeReference;
			return atr != null && this.withoutSuffix == atr.withoutSuffix && this.withSuffix == atr.withSuffix;
		}
	}
}
