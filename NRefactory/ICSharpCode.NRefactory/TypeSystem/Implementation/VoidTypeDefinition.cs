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
	/// Special type definition for 'void'.
	/// </summary>
	public class VoidTypeDefinition : DefaultResolvedTypeDefinition
	{
		public VoidTypeDefinition(ITypeResolveContext parentContext, params IUnresolvedTypeDefinition[] parts)
			: base(parentContext, parts)
		{
		}
		
		public override TypeKind Kind {
			get { return TypeKind.Void; }
		}
		
		public override IEnumerable<IMethod> GetConstructors(Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter, GetMemberOptions options)
		{
			return EmptyList<IEvent>.Instance;
		}
		
		public override IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter, GetMemberOptions options)
		{
			return EmptyList<IField>.Instance;
		}
		
		public override IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter, GetMemberOptions options)
		{
			return EmptyList<IProperty>.Instance;
		}
		
		public override IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter, GetMemberOptions options)
		{
			return EmptyList<IMember>.Instance;
		}
	}
}