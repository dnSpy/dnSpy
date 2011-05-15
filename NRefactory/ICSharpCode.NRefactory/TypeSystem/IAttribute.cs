// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents an attribute.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(IAttributeContract))]
	#endif
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	public interface IAttribute : IFreezable
	{
		/// <summary>
		/// Gets the code region of this attribute.
		/// </summary>
		DomRegion Region { get; }
		
		/// <summary>
		/// Gets the type of the attribute.
		/// </summary>
		ITypeReference AttributeType { get; }
		
		/// <summary>
		/// Gets the positional arguments passed to the attribute.
		/// </summary>
		IList<IConstantValue> GetPositionalArguments(ITypeResolveContext context);
		
		/// <summary>
		/// Gets the named arguments passed to the attribute.
		/// </summary>
		IList<KeyValuePair<string, IConstantValue>> GetNamedArguments(ITypeResolveContext context);
		
		/// <summary>
		/// Resolves the constructor method used for this attribute invocation.
		/// Returns null if the constructor cannot be found.
		/// </summary>
		IMethod ResolveConstructor(ITypeResolveContext context);
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IAttribute))]
	abstract class IAttributeContract : IFreezableContract, IAttribute
	{
		DomRegion IAttribute.Region {
			get { return DomRegion.Empty; }
		}
		
		ITypeReference IAttribute.AttributeType {
			get {
				Contract.Ensures(Contract.Result<ITypeReference>() != null);
				return null;
			}
		}
		
		IList<IConstantValue> IAttribute.GetPositionalArguments(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IList<IConstantValue>>() != null);
			return null;
		}
		
		IList<KeyValuePair<string, IConstantValue>> IAttribute.GetNamedArguments(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IList<KeyValuePair<string, IConstantValue>>>() != null);
			return null;
		}
		
		IMethod IAttribute.ResolveConstructor(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			return null;
		}
	}
	#endif
}
