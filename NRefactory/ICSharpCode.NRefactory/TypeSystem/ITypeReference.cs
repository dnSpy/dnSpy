// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a reference to a type.
	/// Must be resolved before it can be used as type.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(ITypeReferenceContract))]
	#endif
	public interface ITypeReference
	{
		// Keep this interface simple: I decided against having GetMethods/GetEvents etc. here,
		// so that the Resolve step is never hidden from the consumer.
		
		// I decided against implementing IFreezable here: ITypeDefinition can be used as ITypeReference,
		// but when freezing the reference, one wouldn't expect the definition to freeze.
		
		/// <summary>
		/// Resolves this type reference.
		/// </summary>
		IType Resolve(ITypeResolveContext context);
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(ITypeReference))]
	abstract class ITypeReferenceContract : ITypeReference
	{
		IType ITypeReference.Resolve(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IType>() != null);
			return null;
		}
	}
	#endif
}