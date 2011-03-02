// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Type parameter of a generic class/method.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(ITypeParameterContract))]
	#endif
	public interface ITypeParameter : IType, IFreezable
	{
		/// <summary>
		/// Gets the index of the type parameter in the type parameter list of the owning method/class.
		/// </summary>
		int Index { get; }
		
		/// <summary>
		/// Gets the list of attributes declared on this type parameter.
		/// </summary>
		IList<IAttribute> Attributes { get; }
		
		/// <summary>
		/// Gets the class or method for which this type parameter is defined.
		/// This property never returns null.
		/// </summary>
		IEntity Parent { get; }
		
		/// <summary>
		/// The method this type parameter is defined for.
		/// This property returns null if the type parameter belongs to a class.
		/// </summary>
		IMethod ParentMethod { get; }
		
		/// <summary>
		/// The class this type parameter is defined for.
		/// This property returns null if the type parameter belongs to a method.
		/// </summary>
		ITypeDefinition ParentClass { get; }
		
		/// <summary>
		/// Gets the contraints of this type parameter.
		/// </summary>
		IList<ITypeReference> Constraints { get; }
		
		/// <summary>
		/// Gets if the type parameter has the 'new()' constraint.
		/// </summary>
		bool HasDefaultConstructorConstraint { get; }
		
		/// <summary>
		/// Gets if the type parameter has the 'class' constraint.
		/// </summary>
		bool HasReferenceTypeConstraint { get; }
		
		/// <summary>
		/// Gets if the type parameter has the 'struct' constraint.
		/// </summary>
		bool HasValueTypeConstraint { get; }
		
		/// <summary>
		/// Gets the variance of this type parameter.
		/// </summary>
		VarianceModifier Variance { get; }
		
		/// <summary>
		/// Gets the type that was used to bind this type parameter.
		/// This property returns null for generic methods/classes, it
		/// is non-null only for constructed versions of generic methods.
		/// </summary>
		IType BoundTo { get; }
		
		/// <summary>
		/// If this type parameter was bound, returns the unbound version of it.
		/// </summary>
		ITypeParameter UnboundTypeParameter { get; }
	}
	
	/// <summary>
	/// Represents the variance of a type parameter.
	/// </summary>
	public enum VarianceModifier : byte
	{
		/// <summary>
		/// The type parameter is not variant.
		/// </summary>
		Invariant,
		/// <summary>
		/// The type parameter is covariant (used in output position).
		/// </summary>
		Covariant,
		/// <summary>
		/// The type parameter is contravariant (used in input position).
		/// </summary>
		Contravariant
	};
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(ITypeParameter))]
	abstract class ITypeParameterContract : ITypeContract, ITypeParameter
	{
		int ITypeParameter.Index {
			get {
				Contract.Ensures(Contract.Result<int>() >= 0);
				return 0;
			}
		}
		
		IList<IAttribute> ITypeParameter.Attributes {
			get {
				Contract.Ensures(Contract.Result<IList<IAttribute>>() != null);
				return null;
			}
		}
		
		IEntity ITypeParameter.Parent {
			get {
				Contract.Ensures(Contract.Result<IEntity>() != null);
				return null;
			}
		}
		
		IMethod ITypeParameter.ParentMethod {
			get {
				return null;
			}
		}
		
		ITypeDefinition ITypeParameter.ParentClass {
			get {
				return null;
			}
		}
		
		IList<ITypeReference> ITypeParameter.Constraints {
			get {
				Contract.Ensures(Contract.Result<IList<ITypeReference>>() != null);
				return null;
			}
		}
		
		bool ITypeParameter.HasDefaultConstructorConstraint {
			get { return false; }
		}
		
		bool ITypeParameter.HasReferenceTypeConstraint {
			get { return false; }
		}
		
		bool ITypeParameter.HasValueTypeConstraint {
			get { return false; }
		}
		
		IType ITypeParameter.BoundTo {
			get { return null; }
		}
		
		ITypeParameter ITypeParameter.UnboundTypeParameter {
			get {
				ITypeParameter @this = this;
				Contract.Ensures((Contract.Result<ITypeParameter>() != null) == (@this.BoundTo != null));
				return null;
			}
		}
		
		VarianceModifier ITypeParameter.Variance {
			get { return VarianceModifier.Invariant; }
		}
		
		bool IFreezable.IsFrozen {
			get { return false; }
		}
		
		void IFreezable.Freeze()
		{
		}
	}
	#endif
}
