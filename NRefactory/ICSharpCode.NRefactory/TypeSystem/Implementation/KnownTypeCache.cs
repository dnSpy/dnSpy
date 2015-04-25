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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Cache for KnownTypeReferences.
	/// </summary>
	sealed class KnownTypeCache
	{
		readonly ICompilation compilation;
		readonly IType[] knownTypes = new IType[KnownTypeReference.KnownTypeCodeCount];
		
		public KnownTypeCache(ICompilation compilation)
		{
			this.compilation = compilation;
		}
		
		public IType FindType(KnownTypeCode typeCode)
		{
			IType type = LazyInit.VolatileRead(ref knownTypes[(int)typeCode]);
			if (type != null) {
				return type;
			}
			return LazyInit.GetOrSet(ref knownTypes[(int)typeCode], SearchType(typeCode));
		}
		
		IType SearchType(KnownTypeCode typeCode)
		{
			KnownTypeReference typeRef = KnownTypeReference.Get(typeCode);
			if (typeRef == null)
				return SpecialType.UnknownType;
			var typeName = new TopLevelTypeName(typeRef.Namespace, typeRef.Name, typeRef.TypeParameterCount);
			foreach (IAssembly asm in compilation.Assemblies) {
				var typeDef = asm.GetTypeDefinition(typeName);
				if (typeDef != null)
					return typeDef;
			}
			return new UnknownType(typeName);
		}
	}
}
