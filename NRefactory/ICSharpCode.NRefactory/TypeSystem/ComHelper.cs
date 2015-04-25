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
using System.Linq;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Helper methods for COM.
	/// </summary>
	public static class ComHelper
	{
		static bool IsComAttribute(IAttribute attribute, string name)
		{
			return attribute.AttributeType.Name == name && attribute.AttributeType.Namespace == "System.Runtime.InteropServices";
		}
		
		/// <summary>
		/// Gets whether the specified type is imported from COM.
		/// </summary>
		public static bool IsComImport(ITypeDefinition typeDefinition)
		{
			return typeDefinition != null
				&& typeDefinition.Kind == TypeKind.Interface
				&& typeDefinition.Attributes.Any(a => IsComAttribute(a, "ComImportAttribute"));
		}
		
		/// <summary>
		/// Gets the CoClass of the specified COM interface.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Co",
		                                                 Justification = "Consistent with CoClassAttribute")]
		public static IType GetCoClass(ITypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				return SpecialType.UnknownType;
			var coClassAttribute = typeDefinition.Attributes.FirstOrDefault(a => IsComAttribute(a, "CoClassAttribute"));
			if (coClassAttribute != null && coClassAttribute.PositionalArguments.Count == 1) {
				var rr = coClassAttribute.PositionalArguments[0] as TypeOfResolveResult;
				if (rr != null)
					return rr.ReferencedType;
			}
			return SpecialType.UnknownType;
		}
	}
}
