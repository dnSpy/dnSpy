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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a specialized IMethod (e.g. after type substitution).
	/// </summary>
	public class SpecializedMethod : SpecializedParameterizedMember, IMethod
	{
		readonly IMethod methodDefinition;
		readonly ITypeParameter[] specializedTypeParameters;
		readonly bool genericMethodIsSpecialized;
		readonly TypeParameterSubstitution substitutionWithoutSpecializedTypeParameters;
		
		public SpecializedMethod(IMethod methodDefinition, TypeParameterSubstitution substitution)
			: base(methodDefinition)
		{
			SpecializedMethod specializedMethodDefinition = methodDefinition as SpecializedMethod;
			if (specializedMethodDefinition != null)
				this.genericMethodIsSpecialized = specializedMethodDefinition.genericMethodIsSpecialized;
			
			// The base ctor might have unpacked a SpecializedMember
			// (in case we are specializing an already-specialized method)
			methodDefinition = (IMethod)base.baseMember;
			this.methodDefinition = methodDefinition;
			if (methodDefinition.TypeParameters.Count > 0) {
				// The method is generic, so we need to specialize the type parameters
				// (for specializing the constraints, and also to set the correct Owner)
				specializedTypeParameters = new ITypeParameter[methodDefinition.TypeParameters.Count];
				for (int i = 0; i < specializedTypeParameters.Length; i++) {
					specializedTypeParameters[i] = new SpecializedTypeParameter(methodDefinition.TypeParameters[i], this);
				}
				if (!genericMethodIsSpecialized) {
					// Add substitution that replaces the base method's type parameters with our specialized version
					// but do this only if the type parameters on the baseMember have not already been substituted
					substitutionWithoutSpecializedTypeParameters = this.Substitution;
					AddSubstitution(new TypeParameterSubstitution(null, specializedTypeParameters));
				}
			}
			// Add the main substitution after the method type parameter specialization.
			AddSubstitution(substitution);
			if (substitutionWithoutSpecializedTypeParameters != null) {
				// If we already have a substitution without specialized type parameters, update that:
				substitutionWithoutSpecializedTypeParameters = TypeParameterSubstitution.Compose(substitution, substitutionWithoutSpecializedTypeParameters);
			} else {
				// Otherwise just use the whole substitution, as that doesn't contain specialized type parameters
				// in this case.
				substitutionWithoutSpecializedTypeParameters = this.Substitution;
			}
			if (substitution != null && substitution.MethodTypeArguments != null && methodDefinition.TypeParameters.Count > 0)
				this.genericMethodIsSpecialized = true;
			if (specializedTypeParameters != null) {
				// Set the substitution on the type parameters to the final composed substitution
				foreach (var tp in specializedTypeParameters.OfType<SpecializedTypeParameter>()) {
					if (tp.Owner == this)
						tp.substitution = base.Substitution;
				}
			}
		}
		
		/// <summary>
		/// Gets the type arguments passed to this method.
		/// If only the type parameters for the class were specified and the generic method
		/// itself is not specialized yet, this property will return an empty list.
		/// </summary>
		public IList<IType> TypeArguments {
			get { return genericMethodIsSpecialized ? this.Substitution.MethodTypeArguments : EmptyList<IType>.Instance; }
		}
		
		public IList<IUnresolvedMethod> Parts {
			get { return methodDefinition.Parts; }
		}
		
		public IList<IAttribute> ReturnTypeAttributes {
			get { return methodDefinition.ReturnTypeAttributes; }
		}
		
		public IList<ITypeParameter> TypeParameters {
			get {
				return specializedTypeParameters ?? methodDefinition.TypeParameters;
			}
		}
		
		public bool IsExtensionMethod {
			get { return methodDefinition.IsExtensionMethod; }
		}
		
		public bool IsConstructor {
			get { return methodDefinition.IsConstructor; }
		}
		
		public bool IsDestructor {
			get { return methodDefinition.IsDestructor; }
		}
		
		public bool IsOperator {
			get { return methodDefinition.IsOperator; }
		}
		
		public bool IsPartial {
			get { return methodDefinition.IsPartial; }
		}
		
		public bool HasBody {
			get { return methodDefinition.HasBody; }
		}
		
		public bool IsAccessor {
			get { return methodDefinition.IsAccessor; }
		}
		
		IMember accessorOwner;
		
		public IMember AccessorOwner {
			get {
				var result = LazyInit.VolatileRead(ref accessorOwner);
				if (result != null) {
					return result;
				} else {
					result = SpecializedMember.Create(methodDefinition.AccessorOwner, this.Substitution);
					return LazyInit.GetOrSet(ref accessorOwner, result);
				}
			}
			internal set {
				accessorOwner = value;
			}
		}
		
		public override IMemberReference ToMemberReference()
		{
			// Pass the MethodTypeArguments to the SpecializingMemberReference only if
			// the generic method itself is specialized, not if the generic method is only
			// specialized with class type arguments.
			
			// This is necessary due to this part of the ToMemberReference() contract:
			//   If this member is specialized using open generic types, the resulting member reference will need to be looked up in an appropriate generic context.
			//   Otherwise, the main resolve context of a compilation is sufficient.
			// ->
			// This means that if the method itself isn't specialized,
			// we must not include TypeParameterReferences for the specialized type parameters
			// in the resulting member reference.
			if (genericMethodIsSpecialized) {
				return new SpecializingMemberReference(
					baseMember.ToMemberReference(),
					ToTypeReference(base.Substitution.ClassTypeArguments),
					ToTypeReference(base.Substitution.MethodTypeArguments));
			} else {
				return base.ToMemberReference();
			}
		}
		
		public override bool Equals(object obj)
		{
			SpecializedMethod other = obj as SpecializedMethod;
			if (other == null)
				return false;
			return this.baseMember.Equals(other.baseMember) && this.substitutionWithoutSpecializedTypeParameters.Equals(other.substitutionWithoutSpecializedTypeParameters);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return 1000000013 * baseMember.GetHashCode() + 1000000009 * substitutionWithoutSpecializedTypeParameters.GetHashCode();
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(GetType().Name);
			b.Append(' ');
			b.Append(this.DeclaringType.ReflectionName);
			b.Append('.');
			b.Append(this.Name);
			if (this.TypeArguments.Count > 0) {
				b.Append('[');
				for (int i = 0; i < this.TypeArguments.Count; i++) {
					if (i > 0) b.Append(", ");
					b.Append(this.TypeArguments[i].ReflectionName);
				}
				b.Append(']');
			} else if (this.TypeParameters.Count > 0) {
				b.Append("``");
				b.Append(this.TypeParameters.Count);
			}
			b.Append('(');
			for (int i = 0; i < this.Parameters.Count; i++) {
				if (i > 0) b.Append(", ");
				b.Append(this.Parameters[i].ToString());
			}
			b.Append("):");
			b.Append(this.ReturnType.ReflectionName);
			b.Append(']');
			return b.ToString();
		}
		
		sealed class SpecializedTypeParameter : AbstractTypeParameter
		{
			readonly ITypeParameter baseTp;
			
			// The substition is set at the end of SpecializedMethod constructor
			internal TypeVisitor substitution;
			
			public SpecializedTypeParameter(ITypeParameter baseTp, IMethod specializedOwner)
				: base(specializedOwner, baseTp.Index, baseTp.Name, baseTp.Variance, baseTp.Attributes, baseTp.Region)
			{
				// We don't have to consider already-specialized baseTps because
				// we read the baseTp directly from the unpacked memberDefinition.
				this.baseTp = baseTp;
			}
			
			public override int GetHashCode()
			{
				return baseTp.GetHashCode() ^ this.Owner.GetHashCode();
			}
			
			public override bool Equals(IType other)
			{
				// Compare the owner, not the substitution, because the substitution may contain this specialized type parameter recursively
				SpecializedTypeParameter o = other as SpecializedTypeParameter;
				return o != null && baseTp.Equals(o.baseTp) && this.Owner.Equals(o.Owner);
			}
			
			public override bool HasValueTypeConstraint {
				get { return baseTp.HasValueTypeConstraint; }
			}
			
			public override bool HasReferenceTypeConstraint {
				get { return baseTp.HasReferenceTypeConstraint; }
			}
			
			public override bool HasDefaultConstructorConstraint {
				get { return baseTp.HasDefaultConstructorConstraint; }
			}
			
			public override IEnumerable<IType> DirectBaseTypes {
				get {
					return baseTp.DirectBaseTypes.Select(t => t.AcceptVisitor(substitution));
				}
			}
		}
	}
}
