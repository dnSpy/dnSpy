// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	#if WITH_CONTRACTS
	[ContractClass(typeof(IFreezableContract))]
	#endif
	public interface IFreezable
	{
		/// <summary>
		/// Gets if this instance is frozen. Frozen instances are immutable and thus thread-safe.
		/// </summary>
		bool IsFrozen { get; }
		
		/// <summary>
		/// Freezes this instance.
		/// </summary>
		void Freeze();
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IFreezable))]
	abstract class IFreezableContract : IFreezable
	{
		bool IFreezable.IsFrozen {
			get { return default(bool); }
		}
		
		void IFreezable.Freeze()
		{
			IFreezable self = this;
			Contract.Ensures(self.IsFrozen);
		}
	}
	#endif
}
