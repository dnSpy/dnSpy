// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Threading;

namespace ICSharpCode.NRefactory.Utils
{
	public static class LazyInit
	{
		public static T VolatileRead<T>(ref T location) where T : class
		{
			#if NET_4_5
			return Volatile.Read(ref location);
			#else
			T result = location;
			Thread.MemoryBarrier();
			return result;
			#endif
		}
		
		/// <summary>
		/// Atomically performs the following operation:
		/// - If target is null: stores newValue in target and returns newValue.
		/// - If target is not null: returns target.
		/// </summary>
		public static T GetOrSet<T>(ref T target, T newValue) where T : class
		{
			T oldValue = Interlocked.CompareExchange(ref target, newValue, null);
			return oldValue ?? newValue;
		}
	}
}
