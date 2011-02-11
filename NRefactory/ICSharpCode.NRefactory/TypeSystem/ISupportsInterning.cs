// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Interface for TypeSystem objects that support interning.
	/// See <see cref="IInterningProvider"/> for more information.
	/// </summary>
	[ContractClass(typeof(ISupportsInterningContract))]
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
}
