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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Helper class for the GetAllBaseTypes() implementation.
	/// </summary>
	sealed class BaseTypeCollector : List<IType>
	{
		readonly Stack<IType> activeTypes = new Stack<IType>();
		
		/// <summary>
		/// If this option is enabled, the list will not contain interfaces when retrieving the base types
		/// of a class.
		/// </summary>
		internal bool SkipImplementedInterfaces;
		
		public void CollectBaseTypes(IType type)
		{
			IType def = type.GetDefinition() ?? type;
			
			// Maintain a stack of currently active type definitions, and avoid having one definition
			// multiple times on that stack.
			// This is necessary to ensure the output is finite in the presence of cyclic inheritance:
			// class C<X> : C<C<X>> {} would not be caught by the 'no duplicate output' check, yet would
			// produce infinite output.
			if (activeTypes.Contains(def))
				return;
			activeTypes.Push(def);
			// Note that we also need to push non-type definitions, e.g. for protecting against
			// cyclic inheritance in type parameters (where T : S where S : T).
			// The output check doesn't help there because we call Add(type) only at the end.
			// We can't simply call this.Add(type); at the start because that would return in an incorrect order.
			
			// Avoid outputting a type more than once - necessary for "diamond" multiple inheritance
			// (e.g. C implements I1 and I2, and both interfaces derive from Object)
			if (!this.Contains(type)) {
				foreach (IType baseType in type.DirectBaseTypes) {
					if (SkipImplementedInterfaces && def != null && def.Kind != TypeKind.Interface && def.Kind != TypeKind.TypeParameter) {
						if (baseType.Kind == TypeKind.Interface) {
							// skip the interface
							continue;
						}
					}
					CollectBaseTypes(baseType);
				}
				// Add(type) at the end - we want a type to be output only after all its base types were added.
				this.Add(type);
				// Note that this is not the same as putting the this.Add() call in front and then reversing the list.
				// For the diamond inheritance, Add() at the start produces "C, I1, Object, I2",
				// while Add() at the end produces "Object, I1, I2, C".
			}
			activeTypes.Pop();
		}
	}
}
