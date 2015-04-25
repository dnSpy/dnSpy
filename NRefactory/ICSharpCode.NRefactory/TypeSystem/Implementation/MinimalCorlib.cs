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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Resolve context represents the minimal mscorlib required for evaluating constants.
	/// This contains all known types (<see cref="KnownTypeCode"/>) and no other types.
	/// </summary>
	public sealed class MinimalCorlib : DefaultUnresolvedAssembly
	{
		static readonly Lazy<MinimalCorlib> instance = new Lazy<MinimalCorlib>(() => new MinimalCorlib());
		
		public static MinimalCorlib Instance {
			get { return instance.Value; }
		}
		
		public ICompilation CreateCompilation()
		{
			return new SimpleCompilation(new DefaultSolutionSnapshot(), this);
		}
		
		private MinimalCorlib() : base("corlib")
		{
			var types = new DefaultUnresolvedTypeDefinition[KnownTypeReference.KnownTypeCodeCount];
			for (int i = 0; i < types.Length; i++) {
				var typeRef = KnownTypeReference.Get((KnownTypeCode)i);
				if (typeRef != null) {
					types[i] = new DefaultUnresolvedTypeDefinition(typeRef.Namespace, typeRef.Name);
					for (int j = 0; j < typeRef.TypeParameterCount; j++) {
						types[i].TypeParameters.Add(new DefaultUnresolvedTypeParameter(SymbolKind.TypeDefinition, j));
					}
					AddTypeDefinition(types[i]);
				}
			}
			for (int i = 0; i < types.Length; i++) {
				var typeRef = KnownTypeReference.Get((KnownTypeCode)i);
				if (typeRef != null && typeRef.baseType != KnownTypeCode.None) {
					types[i].BaseTypes.Add(types[(int)typeRef.baseType]);
					if (typeRef.baseType == KnownTypeCode.ValueType && i != (int)KnownTypeCode.Enum) {
						types[i].Kind = TypeKind.Struct;
					}
				}
			}
			Freeze();
		}
	}
}
