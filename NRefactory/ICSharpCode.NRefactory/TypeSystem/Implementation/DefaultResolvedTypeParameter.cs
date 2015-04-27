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
using System.Threading;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public class DefaultTypeParameter : AbstractTypeParameter
	{
		readonly bool hasValueTypeConstraint;
		readonly bool hasReferenceTypeConstraint;
		readonly bool hasDefaultConstructorConstraint;
		readonly IList<IType> constraints;
		
		public DefaultTypeParameter(
			IEntity owner,
			int index, string name = null,
			VarianceModifier variance = VarianceModifier.Invariant,
			IList<IAttribute> attributes = null,
			DomRegion region = default(DomRegion),
			bool hasValueTypeConstraint = false, bool hasReferenceTypeConstraint = false, bool hasDefaultConstructorConstraint = false,
			IList<IType> constraints = null)
			: base(owner, index, name, variance, attributes, region)
		{
			this.hasValueTypeConstraint = hasValueTypeConstraint;
			this.hasReferenceTypeConstraint = hasReferenceTypeConstraint;
			this.hasDefaultConstructorConstraint = hasDefaultConstructorConstraint;
			this.constraints = constraints ?? EmptyList<IType>.Instance;
		}
		
		public DefaultTypeParameter(
			ICompilation compilation, SymbolKind ownerType,
			int index, string name = null,
			VarianceModifier variance = VarianceModifier.Invariant,
			IList<IAttribute> attributes = null,
			DomRegion region = default(DomRegion),
			bool hasValueTypeConstraint = false, bool hasReferenceTypeConstraint = false, bool hasDefaultConstructorConstraint = false,
			IList<IType> constraints = null)
			: base(compilation, ownerType, index, name, variance, attributes, region)
		{
			this.hasValueTypeConstraint = hasValueTypeConstraint;
			this.hasReferenceTypeConstraint = hasReferenceTypeConstraint;
			this.hasDefaultConstructorConstraint = hasDefaultConstructorConstraint;
			this.constraints = constraints ?? EmptyList<IType>.Instance;
		}
		
		public override bool HasValueTypeConstraint {
			get { return hasValueTypeConstraint; }
		}
		
		public override bool HasReferenceTypeConstraint {
			get { return hasReferenceTypeConstraint; }
		}
		
		public override bool HasDefaultConstructorConstraint {
			get { return hasDefaultConstructorConstraint; }
		}
		
		public override IEnumerable<IType> DirectBaseTypes {
			get {
				bool hasNonInterfaceConstraint = false;
				foreach (IType c in constraints) {
					yield return c;
					if (c.Kind != TypeKind.Interface)
						hasNonInterfaceConstraint = true;
				}
				// Do not add the 'System.Object' constraint if there is another constraint with a base class.
				if (this.HasValueTypeConstraint || !hasNonInterfaceConstraint) {
					yield return this.Compilation.FindType(this.HasValueTypeConstraint ? KnownTypeCode.ValueType : KnownTypeCode.Object);
				}
			}
		}
	}
	
	/*
	/// <summary>
	/// Default implementation of <see cref="ITypeParameter"/>.
	/// </summary>
	[Serializable]
	public sealed class DefaultTypeParameter : AbstractTypeParameter
	{
		IList<ITypeReference> constraints;
		
		BitVector16 flags;
		
		const ushort FlagReferenceTypeConstraint      = 0x0001;
		const ushort FlagValueTypeConstraint          = 0x0002;
		const ushort FlagDefaultConstructorConstraint = 0x0004;
		
		protected override void FreezeInternal()
		{
			constraints = FreezeList(constraints);
			base.FreezeInternal();
		}
		
		public DefaultTypeParameter(SymbolKind ownerType, int index, string name)
			: base(ownerType, index, name)
		{
		}
		
		public IList<ITypeReference> Constraints {
			get {
				if (constraints == null)
					constraints = new List<ITypeReference>();
				return constraints;
			}
		}
		
		public bool HasDefaultConstructorConstraint {
			get { return flags[FlagDefaultConstructorConstraint]; }
			set {
				CheckBeforeMutation();
				flags[FlagDefaultConstructorConstraint] = value;
			}
		}
		
		public bool HasReferenceTypeConstraint {
			get { return flags[FlagReferenceTypeConstraint]; }
			set {
				CheckBeforeMutation();
				flags[FlagReferenceTypeConstraint] = value;
			}
		}
		
		public bool HasValueTypeConstraint {
			get { return flags[FlagValueTypeConstraint]; }
			set {
				CheckBeforeMutation();
				flags[FlagValueTypeConstraint] = value;
			}
		}
		
		public override bool? IsReferenceType(ITypeResolveContext context)
		{
			switch (flags.Data & (FlagReferenceTypeConstraint | FlagValueTypeConstraint)) {
				case FlagReferenceTypeConstraint:
					return true;
				case FlagValueTypeConstraint:
					return false;
			}
			
			return base.IsReferenceTypeHelper(GetEffectiveBaseClass(context));
		}
		
		public override IType GetEffectiveBaseClass(ITypeResolveContext context)
		{
			// protect against cyclic type parameters
			using (var busyLock = BusyManager.Enter(this)) {
				if (!busyLock.Success)
					return SpecialTypes.UnknownType;
				
				if (HasValueTypeConstraint)
					return context.GetTypeDefinition("System", "ValueType", 0, StringComparer.Ordinal) ?? SpecialTypes.UnknownType;
				
				List<IType> classTypeConstraints = new List<IType>();
				foreach (ITypeReference constraintRef in this.Constraints) {
					IType constraint = constraintRef.Resolve(context);
					if (constraint.Kind == TypeKind.Class) {
						classTypeConstraints.Add(constraint);
					} else if (constraint.Kind == TypeKind.TypeParameter) {
						IType baseClass = ((ITypeParameter)constraint).GetEffectiveBaseClass(context);
						if (baseClass.Kind == TypeKind.Class)
							classTypeConstraints.Add(baseClass);
					}
				}
				if (classTypeConstraints.Count == 0)
					return KnownTypeReference.Object.Resolve(context);
				// Find the derived-most type in the resulting set:
				IType result = classTypeConstraints[0];
				for (int i = 1; i < classTypeConstraints.Count; i++) {
					if (classTypeConstraints[i].GetDefinition().IsDerivedFrom(result.GetDefinition(), context))
						result = classTypeConstraints[i];
				}
				return result;
			}
		}
		
		public override IEnumerable<IType> GetEffectiveInterfaceSet(ITypeResolveContext context)
		{
			List<IType> result = new List<IType>();
			// protect against cyclic type parameters
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					foreach (ITypeReference constraintRef in this.Constraints) {
						IType constraint = constraintRef.Resolve(context);
						if (constraint.Kind == TypeKind.Interface) {
							result.Add(constraint);
						} else if (constraint.Kind == TypeKind.TypeParameter) {
							result.AddRange(((ITypeParameter)constraint).GetEffectiveInterfaceSet(context));
						}
					}
				}
			}
			return result.Distinct();
		}
		
		public override ITypeParameterConstraints GetConstraints(ITypeResolveContext context)
		{
			return new DefaultTypeParameterConstraints(
				this.Constraints.Select(c => c.Resolve(context)),
				this.HasDefaultConstructorConstraint, this.HasReferenceTypeConstraint, this.HasValueTypeConstraint);
		}
		
		/*
	 * Interning for type parameters is disabled; we can't intern cyclic structures as might
	 * occur in the constraints, and incomplete interning is dangerous for type parameters
	 * as we use reference equality.
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			// protect against cyclic constraints
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					constraints = provider.InternList(constraints);
					base.PrepareForInterning(provider);
				}
			}
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				int hashCode = base.GetHashCodeForInterning();
				if (constraints != null)
					hashCode += constraints.GetHashCode();
				hashCode += 771 * flags.Data;
				return hashCode;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultTypeParameter o = other as DefaultTypeParameter;
			return base.EqualsForInterning(o)
				&& this.constraints == o.constraints
				&& this.flags == o.flags;
		}
	}*/
}
