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
using System.Linq;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Implementation of <see cref="IMember"/> that resolves an unresolved member.
	/// </summary>
	public abstract class AbstractResolvedMember : AbstractResolvedEntity, IMember
	{
		protected new readonly IUnresolvedMember unresolved;
		protected readonly ITypeResolveContext context;
		volatile IType returnType;
		IList<IMember> implementedInterfaceMembers;
		
		protected AbstractResolvedMember(IUnresolvedMember unresolved, ITypeResolveContext parentContext)
			: base(unresolved, parentContext)
		{
			this.unresolved = unresolved;
			this.context = parentContext.WithCurrentMember(this);
		}
		
		IMember IMember.MemberDefinition {
			get { return this; }
		}
		
		public IType ReturnType {
			get {
				return this.returnType ?? (this.returnType = unresolved.ReturnType.Resolve(context));
			}
		}
		
		public IUnresolvedMember UnresolvedMember {
			get { return unresolved; }
		}
		
		public IList<IMember> ImplementedInterfaceMembers {
			get {
				IList<IMember> result = LazyInit.VolatileRead(ref this.implementedInterfaceMembers);
				if (result != null) {
					return result;
				} else {
					return LazyInit.GetOrSet(ref implementedInterfaceMembers, FindImplementedInterfaceMembers());
				}
			}
		}
		
		IList<IMember> FindImplementedInterfaceMembers()
		{
			if (unresolved.IsExplicitInterfaceImplementation) {
				List<IMember> result = new List<IMember>();
				foreach (var memberReference in unresolved.ExplicitInterfaceImplementations) {
					IMember member = memberReference.Resolve(context);
					if (member != null)
						result.Add(member);
				}
				return result.ToArray();
			} else if (unresolved.IsStatic) {
				return EmptyList<IMember>.Instance;
			} else {
				// TODO: implement interface member mappings correctly
				return InheritanceHelper.GetBaseMembers(this, true)
					.Where(m => m.DeclaringTypeDefinition != null && m.DeclaringTypeDefinition.Kind == TypeKind.Interface)
					.ToArray();
			}
		}
		
		public override DocumentationComment Documentation {
			get {
				IUnresolvedDocumentationProvider docProvider = unresolved.ParsedFile as IUnresolvedDocumentationProvider;
				if (docProvider != null) {
					var doc = docProvider.GetDocumentation(unresolved, this);
					if (doc != null)
						return doc;
				}
				return base.Documentation;
			}
		}
		
		public bool IsExplicitInterfaceImplementation {
			get { return unresolved.IsExplicitInterfaceImplementation; }
		}
		
		public bool IsVirtual {
			get { return unresolved.IsVirtual; }
		}
		
		public bool IsOverride {
			get { return unresolved.IsOverride; }
		}
		
		public bool IsOverridable {
			get { return unresolved.IsOverridable; }
		}
		
		public virtual IMemberReference ToMemberReference()
		{
			var declTypeRef = this.DeclaringType.ToTypeReference();
			if (IsExplicitInterfaceImplementation && ImplementedInterfaceMembers.Count == 1) {
				return new ExplicitInterfaceImplementationMemberReference(declTypeRef, ImplementedInterfaceMembers[0].ToMemberReference());
			} else {
				return new DefaultMemberReference(this.EntityType, declTypeRef, this.Name);
			}
		}
		
		internal IMethod GetAccessor(ref IMethod accessorField, IUnresolvedMethod unresolvedAccessor)
		{
			if (unresolvedAccessor == null)
				return null;
			IMethod result = LazyInit.VolatileRead(ref accessorField);
			if (result != null) {
				return result;
			} else {
				return LazyInit.GetOrSet(ref accessorField, (IMethod)unresolvedAccessor.CreateResolved(context));
			}
		}
	}
}
