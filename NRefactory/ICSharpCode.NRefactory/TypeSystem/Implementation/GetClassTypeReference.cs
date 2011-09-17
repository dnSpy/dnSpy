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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Type Reference used when the fully qualified type name is known.
	/// </summary>
	[Serializable]
	public sealed class GetClassTypeReference : ITypeReference, ISupportsInterning
	{
		string nameSpace, name;
		int typeParameterCount;
		// [NonSerialized] volatile CachedResult v_cachedResult;
		
		public GetClassTypeReference(string nameSpace, string name, int typeParameterCount)
		{
			if (nameSpace == null)
				throw new ArgumentNullException("nameSpace");
			if (name == null)
				throw new ArgumentNullException("name");
			this.nameSpace = nameSpace;
			this.name = name;
			this.typeParameterCount = typeParameterCount;
		}
		
		public GetClassTypeReference(string fullTypeName, int typeParameterCount)
		{
			if (fullTypeName == null)
				throw new ArgumentNullException("fullTypeName");
			int pos = fullTypeName.LastIndexOf('.');
			if (pos < 0) {
				nameSpace = string.Empty;
				name = fullTypeName;
			} else {
				nameSpace = fullTypeName.Substring(0, pos);
				name = fullTypeName.Substring(pos + 1);
			}
			this.typeParameterCount = typeParameterCount;
		}
		
		public string Namespace { get { return nameSpace; } }
		public string Name { get { return name; } }
		public int TypeParameterCount { get { return typeParameterCount; } }
		
		/*
		sealed class CachedResult
		{
			public readonly CacheManager CacheManager;
			public readonly IType Result;
			
			public CachedResult(CacheManager cacheManager, IType result)
			{
				this.CacheManager = cacheManager;
				this.Result = result;
			}
		}
		 */
		
		public IType Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			
//			CacheManager cache = context.CacheManager;
//			if (cache != null) {
//				IType cachedType = cache.GetShared(this) as IType;
//				if (cachedType != null)
//					return cachedType;
//			}
			
			IType type = context.GetTypeDefinition(nameSpace, name, typeParameterCount, StringComparer.Ordinal) ?? SharedTypes.UnknownType;
//			if (cache != null)
//				cache.SetShared(this, type);
			return type;
		}
		
		public override string ToString()
		{
			if (typeParameterCount == 0)
				return BuildQualifiedName(nameSpace, name);
			else
				return BuildQualifiedName(nameSpace, name) + "`" + typeParameterCount;
		}
		
		static string BuildQualifiedName (string name1, string name2)
		{
			if (string.IsNullOrEmpty (name1))
				return name2;
			if (string.IsNullOrEmpty (name2))
				return name1;
			return name1 + "." + name2;
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			nameSpace = provider.Intern(nameSpace);
			name = provider.Intern(name);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return nameSpace.GetHashCode() ^ name.GetHashCode() ^ typeParameterCount;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			GetClassTypeReference o = other as GetClassTypeReference;
			return o != null && name == o.name && nameSpace == o.nameSpace && typeParameterCount == o.typeParameterCount;
		}
	}
}
