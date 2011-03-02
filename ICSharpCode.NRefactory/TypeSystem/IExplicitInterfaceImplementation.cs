// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents an explicit interface implementation.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(IExplicitInterfaceImplementationContract))]
	#endif
	public interface IExplicitInterfaceImplementation : IFreezable
	{
		/// <summary>
		/// Gets the type of the interface.
		/// </summary>
		ITypeReference InterfaceType { get; }
		
		/// <summary>
		/// Gets the member name.
		/// </summary>
		string MemberName { get; }
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IExplicitInterfaceImplementation))]
	abstract class IExplicitInterfaceImplementationContract : IFreezableContract, IExplicitInterfaceImplementation
	{
		ITypeReference IExplicitInterfaceImplementation.InterfaceType {
			get {
				Contract.Ensures(Contract.Result<ITypeReference>() != null);
				return null;
			}
		}
		
		string IExplicitInterfaceImplementation.MemberName {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
	}
	#endif
}
