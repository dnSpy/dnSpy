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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	[Serializable]
	public sealed class MethodTypeParameterWithInheritedConstraints : DefaultUnresolvedTypeParameter
	{
		public MethodTypeParameterWithInheritedConstraints(int index, string name)
			: base(SymbolKind.Method, index, name)
		{
		}
		
		static ITypeParameter ResolveBaseTypeParameter(IMethod parentMethod, int index)
		{
			IMethod baseMethod = null;
			if (parentMethod.IsOverride) {
				foreach (IMethod m in InheritanceHelper.GetBaseMembers(parentMethod, false).OfType<IMethod>()) {
					if (!m.IsOverride) {
						baseMethod = m;
						break;
					}
				}
			} else if (parentMethod.IsExplicitInterfaceImplementation && parentMethod.ImplementedInterfaceMembers.Count == 1) {
				baseMethod = parentMethod.ImplementedInterfaceMembers[0] as IMethod;
			}
			if (baseMethod != null && index < baseMethod.TypeParameters.Count)
				return baseMethod.TypeParameters[index];
			else
				return null;
		}
		
		public override ITypeParameter CreateResolvedTypeParameter(ITypeResolveContext context)
		{
			if (context.CurrentMember is IMethod) {
				return new ResolvedMethodTypeParameterWithInheritedConstraints(this, context);
			} else {
				return base.CreateResolvedTypeParameter(context);
			}
		}
		
		sealed class ResolvedMethodTypeParameterWithInheritedConstraints : AbstractTypeParameter
		{
			volatile ITypeParameter baseTypeParameter;
			
			public ResolvedMethodTypeParameterWithInheritedConstraints(MethodTypeParameterWithInheritedConstraints unresolved, ITypeResolveContext context)
				: base(context.CurrentMember, unresolved.Index, unresolved.Name, unresolved.Variance,
				       unresolved.Attributes.CreateResolvedAttributes(context), unresolved.Region)
			{
			}
			
			ITypeParameter GetBaseTypeParameter()
			{
				ITypeParameter baseTP = this.baseTypeParameter;
				if (baseTP == null) {
					// ResolveBaseTypeParameter() is idempotent, so this is thread-safe.
					this.baseTypeParameter = baseTP = ResolveBaseTypeParameter((IMethod)this.Owner, this.Index);
				}
				return baseTP;
			}
			
			public override bool HasValueTypeConstraint {
				get {
					ITypeParameter baseTP = GetBaseTypeParameter();
					return baseTP != null ? baseTP.HasValueTypeConstraint : false;
				}
			}
			
			public override bool HasReferenceTypeConstraint {
				get {
					ITypeParameter baseTP = GetBaseTypeParameter();
					return baseTP != null ? baseTP.HasReferenceTypeConstraint : false;
				}
			}
			
			public override bool HasDefaultConstructorConstraint {
				get {
					ITypeParameter baseTP = GetBaseTypeParameter();
					return baseTP != null ? baseTP.HasDefaultConstructorConstraint : false;
				}
			}
			
			public override IEnumerable<IType> DirectBaseTypes {
				get {
					ITypeParameter baseTP = GetBaseTypeParameter();
					if (baseTP != null) {
						// Substitute occurrences of the base method's type parameters in the constraints
						// with the type parameters from the
						IMethod owner = (IMethod)this.Owner;
						var substitution = new TypeParameterSubstitution(null, new ProjectedList<ITypeParameter, IType>(owner.TypeParameters, t => t));
						return baseTP.DirectBaseTypes.Select(t => t.AcceptVisitor(substitution));
					} else {
						return EmptyList<IType>.Instance;
					}
				}
			}
		}
	}
}
