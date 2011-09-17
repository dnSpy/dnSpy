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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Special type definition for 'void'.
	/// </summary>
	[Serializable]
	public class VoidTypeDefinition : DefaultTypeDefinition
	{
		public VoidTypeDefinition(IProjectContent projectContent)
			: base(projectContent, "System", "Void")
		{
			this.Kind = TypeKind.Void;
			this.Accessibility = Accessibility.Public;
			this.IsSealed = true;
		}
		
		public override IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter, GetMemberOptions options)
		{
			return EmptyList<IEvent>.Instance;
		}
		
		public override IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter, GetMemberOptions options)
		{
			return EmptyList<IField>.Instance;
		}
		
		public override IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			return EmptyList<IMethod>.Instance;
		}
		
		public override IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter, GetMemberOptions options)
		{
			return EmptyList<IProperty>.Instance;
		}
		
		public override IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter, GetMemberOptions options)
		{
			return EmptyList<IMember>.Instance;
		}
	}
}
