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
using System.Diagnostics.Contracts;

using ICSharpCode.NRefactory.Semantics;

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
		IList<ResolveResult> GetPositionalArguments(ITypeResolveContext context);
		
		/// <summary>
		/// Gets the named arguments passed to the attribute.
		/// </summary>
		IList<KeyValuePair<string, ResolveResult>> GetNamedArguments(ITypeResolveContext context);
		
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
