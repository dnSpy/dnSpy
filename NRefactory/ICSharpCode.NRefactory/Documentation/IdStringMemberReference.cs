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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Documentation
{
	[Serializable]
	class IdStringMemberReference : IMemberReference
	{
		readonly ITypeReference declaringTypeReference;
		readonly char memberType;
		readonly string memberIdString;
		
		public IdStringMemberReference(ITypeReference declaringTypeReference, char memberType, string memberIdString)
		{
			this.declaringTypeReference = declaringTypeReference;
			this.memberType = memberType;
			this.memberIdString = memberIdString;
		}
		
		bool CanMatch(IUnresolvedMember member)
		{
			switch (member.SymbolKind) {
				case SymbolKind.Field:
					return memberType == 'F';
				case SymbolKind.Property:
				case SymbolKind.Indexer:
					return memberType == 'P';
				case SymbolKind.Event:
					return memberType == 'E';
				case SymbolKind.Method:
				case SymbolKind.Operator:
				case SymbolKind.Constructor:
				case SymbolKind.Destructor:
					return memberType == 'M';
				default:
					throw new NotSupportedException(member.SymbolKind.ToString());
			}
		}
		
		public ITypeReference DeclaringTypeReference {
			get { return declaringTypeReference; }
		}
		
		public IMember Resolve(ITypeResolveContext context)
		{
			IType declaringType = declaringTypeReference.Resolve(context);
			foreach (var member in declaringType.GetMembers(CanMatch, GetMemberOptions.IgnoreInheritedMembers)) {
				if (IdStringProvider.GetIdString(member) == memberIdString)
					return member;
			}
			return null;
		}
		
		ISymbol ISymbolReference.Resolve(ITypeResolveContext context)
		{
			return Resolve(context);
		}
	}
}
