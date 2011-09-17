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
