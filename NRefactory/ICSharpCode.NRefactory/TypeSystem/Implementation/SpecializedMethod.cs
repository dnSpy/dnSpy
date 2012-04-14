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
		
		public SpecializedMethod(IMethod methodDefinition, TypeParameterSubstitution substitution)
			: base(methodDefinition)
		{
			// The base ctor might have unpacked a SpecializedMember
			// (in case we are specializing an already-specialized method)
			methodDefinition = (IMethod)base.MemberDefinition;
			this.methodDefinition = methodDefinition;
			if (methodDefinition.TypeParameters.Any(ConstraintNeedsSpecialization)) {
				// The method is generic, and we need to specialize the type parameters
				specializedTypeParameters = new ITypeParameter[methodDefinition.TypeParameters.Count];
				for (int i = 0; i < specializedTypeParameters.Length; i++) {
					ITypeParameter tp = methodDefinition.TypeParameters[i];
					if (ConstraintNeedsSpecialization(tp))
						tp = new SpecializedTypeParameter(tp, this);
					specializedTypeParameters[i] = tp;
				}
				// add substitution that replaces the base method's type parameters with our specialized version
				AddSubstitution(new TypeParameterSubstitution(null, specializedTypeParameters));
			}
			// Add the main substitution after the method type parameter specialization.
			AddSubstitution(substitution);
			if (specializedTypeParameters != null) {
				// Set the substitution on the type parameters to the final composed substitution
				foreach (var tp in specializedTypeParameters.OfType<SpecializedTypeParameter>()) {
					if (tp.Owner == this)
						tp.substitution = base.Substitution;
				}
			}
		}
		
		static bool ConstraintNeedsSpecialization(ITypeParameter tp)
		{
			// TODO: can we avoid specialization if a type parameter doesn't have any constraints?
			return true;
		}
		
		internal static TypeVisitor GetSubstitution(IType declaringType, IList<IType> typeArguments)
		{
			ParameterizedType pt = declaringType as ParameterizedType;
			if (pt != null)
				return pt.GetSubstitution(typeArguments);
			else if (typeArguments != null)
				return new TypeParameterSubstitution(null, typeArguments);
			else
				return null;
		}
		
		/// <summary>
		/// Gets the type arguments passed to this method.
		/// If only the type parameters for the class were specified and the generic method
		/// itself is not specialized yet, this property will return an empty list.
		/// </summary>
		public IList<IType> TypeArguments {
			get { return this.Substitution.MethodTypeArguments ?? EmptyList<IType>.Instance; }
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
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(GetType().Name);
			b.Append(' ');
			b.Append(this.DeclaringType.ToString());
			b.Append('.');
			b.Append(this.Name);
			if (this.TypeArguments.Count > 0) {
				b.Append('[');
				for (int i = 0; i < this.TypeArguments.Count; i++) {
					if (i > 0) b.Append(", ");
					b.Append(this.TypeArguments[i].ToString());
				}
				b.Append(']');
			}
			b.Append('(');
			for (int i = 0; i < this.Parameters.Count; i++) {
				if (i > 0) b.Append(", ");
				b.Append(this.Parameters[i].ToString());
			}
			b.Append("):");
			b.Append(this.ReturnType.ToString());
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
