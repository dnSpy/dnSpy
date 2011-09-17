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
		/// <returns>
		/// Returns the resolved type.
		/// In case of an error, returns <see cref="SharedTypes.UnknownType"/>.
		/// Never returns null.
		/// </returns>
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