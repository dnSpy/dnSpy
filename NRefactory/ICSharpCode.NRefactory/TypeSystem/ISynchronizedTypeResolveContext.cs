// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Context representing the set of assemblies in which a type is being searched.
	/// Guarantees that the list of types available in the context is not changed until Dispose() is called.
	/// The Dispose() method must be called from the same thread that create the
	/// <c>ISynchronizedTypeResolveContext</c>.
	/// </summary>
	/// <remarks>
	/// A simple implementation might enter a ReaderWriterLock when the synchronized context
	/// is created, and releases the lock when Dispose() is called.
	/// However, implementations based on immutable data structures are also possible.
	/// 
	/// Calling Synchronize() on an already synchronized context is possible, but has no effect.
	/// Only disposing the outermost ISynchronizedTypeResolveContext releases the lock.
	/// </remarks>
	public interface ISynchronizedTypeResolveContext : ITypeResolveContext, IDisposable
	{
	}
}
