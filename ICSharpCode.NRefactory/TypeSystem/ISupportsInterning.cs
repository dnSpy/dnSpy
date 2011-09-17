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
	/// Interface for TypeSystem objects that support interning.
	/// See <see cref="IInterningProvider"/> for more information.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(ISupportsInterningContract))]
	#endif
	public interface ISupportsInterning
	{
		/// <summary>
		/// Interns child objects and strings.
		/// </summary>
		void PrepareForInterning(IInterningProvider provider);
		
		/// <summary>
		/// Gets a hash code for interning.
		/// </summary>
		int GetHashCodeForInterning();
		
		/// <summary>
		/// Equality test for interning.
		/// </summary>
		bool EqualsForInterning(ISupportsInterning other);
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(ISupportsInterning))]
	abstract class ISupportsInterningContract : ISupportsInterning
	{
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			Contract.Requires(provider != null);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return 0;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			return false;
		}
	}
	#endif
}
