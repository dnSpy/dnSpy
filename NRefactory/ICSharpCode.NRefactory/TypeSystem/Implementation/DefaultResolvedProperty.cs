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
using System.Linq;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public class DefaultResolvedProperty : AbstractResolvedMember, IProperty
	{
		protected new readonly IUnresolvedProperty unresolved;
		readonly IList<IParameter> parameters;
		IMethod getter;
		IMethod setter;
		
		public DefaultResolvedProperty(IUnresolvedProperty unresolved, ITypeResolveContext parentContext)
			: base(unresolved, parentContext)
		{
			this.unresolved = unresolved;
			this.parameters = unresolved.Parameters.CreateResolvedParameters(context);
		}
		
		public IList<IParameter> Parameters {
			get { return parameters; }
		}
		
		public bool CanGet {
			get { return unresolved.CanGet; }
		}
		
		public bool CanSet {
			get { return unresolved.CanSet; }
		}
		
		public IMethod Getter {
			get { return GetAccessor(ref getter, unresolved.Getter); }
		}
		
		public IMethod Setter {
			get { return GetAccessor(ref setter, unresolved.Setter); }
		}
		
		public bool IsIndexer {
			get { return unresolved.IsIndexer; }
		}
		
		public override ISymbolReference ToReference()
		{
			var declTypeRef = this.DeclaringType.ToTypeReference();
			if (IsExplicitInterfaceImplementation && ImplementedInterfaceMembers.Count == 1) {
				return new ExplicitInterfaceImplementationMemberReference(declTypeRef, ImplementedInterfaceMembers[0].ToReference());
			} else {
				return new DefaultMemberReference(
					this.SymbolKind, declTypeRef, this.Name, 0,
					this.Parameters.Select(p => p.Type.ToTypeReference()).ToList());
			}
		}
		
		public override IMember Specialize(TypeParameterSubstitution substitution)
		{
			if (TypeParameterSubstitution.Identity.Equals(substitution))
				return this;
			return new SpecializedProperty(this, substitution);
		}
	}
}
