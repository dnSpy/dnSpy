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
		IList<IConstantValue> PositionalArguments { get; }
		
		/// <summary>
		/// Gets the named arguments passed to the attribute.
		/// </summary>
		IList<KeyValuePair<string, IConstantValue>> NamedArguments { get; }
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
		
		IList<IConstantValue> IAttribute.PositionalArguments {
			get {
				Contract.Ensures(Contract.Result<IList<IConstantValue>>() != null);
				return null;
			}
		}
		
		IList<KeyValuePair<string, IConstantValue>> IAttribute.NamedArguments {
			get {
				Contract.Ensures(Contract.Result<IList<KeyValuePair<string, IConstantValue>>>() != null);
				return null;
			}
		}
	}
	#endif
}
