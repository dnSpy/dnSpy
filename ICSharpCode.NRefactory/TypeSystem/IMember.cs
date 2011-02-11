// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Method/field/entity.
	/// </summary>
	[ContractClass(typeof(IMemberContract))]
	public interface IMember : IEntity
	{
		/// <summary>
		/// Gets/Sets the declaring type (incl. type arguments, if any).
		/// This property never returns null -- for top-level members, it returns SharedTypes.UnknownType.
		/// If this is not a specialized member, the value returned is equal to <see cref="IEntity.DeclaringTypeDefinition"/>.
		/// </summary>
		IType DeclaringType { get; }
		
		/// <summary>
		/// Gets the original member definition for this member.
		/// Returns <c>this</c> if this is not a specialized member.
		/// Specialized members are the result of overload resolution with type substitution.
		/// </summary>
		IMember MemberDefinition { get; }
		
		/// <summary>
		/// Gets the return type of this member.
		/// This property never returns null.
		/// </summary>
		ITypeReference ReturnType { get; }
		
		/// <summary>
		/// Gets the list of interfaces this member is implementing explicitly.
		/// </summary>
		IList<IExplicitInterfaceImplementation> InterfaceImplementations { get; }
		
		/// <summary>
		/// Gets if the member is virtual. Is true only if the "virtual" modifier was used, but non-virtual
		/// members can be overridden, too; if they are already overriding a method.
		/// </summary>
		bool IsVirtual {
			get;
		}
		
		bool IsOverride {
			get;
		}
		
		/// <summary>
		/// Gets if the member can be overridden. Returns true when the member is "virtual" or "override" but not "sealed".
		/// </summary>
		bool IsOverridable {
			get;
		}
	}
	
	[ContractClassFor(typeof(IMember))]
	abstract class IMemberContract : IEntityContract, IMember
	{
		IType IMember.DeclaringType {
			get {
				Contract.Ensures(Contract.Result<IType>() != null);
				return null;
			}
		}
		
		IMember IMember.MemberDefinition {
			get {
				Contract.Ensures(Contract.Result<IMember>() != null);
				return null;
			}
		}
		
		ITypeReference IMember.ReturnType {
			get {
				Contract.Ensures(Contract.Result<ITypeReference>() != null);
				return null;
			}
		}
		
		IList<IExplicitInterfaceImplementation> IMember.InterfaceImplementations {
			get {
				Contract.Ensures(Contract.Result<IList<IExplicitInterfaceImplementation>>() != null);
				return null;
			}
		}
		
		bool IMember.IsVirtual {
			get { return false; }
		}
		
		bool IMember.IsOverride {
			get { return false; }
		}
		
		bool IMember.IsOverridable {
			get { return false; }
		}
	}
}
