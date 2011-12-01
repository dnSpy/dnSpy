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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// Allows caching values for a specific compilation.
	/// A CacheManager consists of two dictionaries: one for shared instances (shared among all threads working with that resolve context),
	/// and one for thread-local instances.
	/// </summary>
	/// <remarks>This class is thread-safe</remarks>
	public sealed class CacheManager
	{
		readonly ConcurrentDictionary<object, object> sharedDict = new ConcurrentDictionary<object, object>(ReferenceComparer.Instance);
		readonly ThreadLocal<Dictionary<object, object>> localDict = new ThreadLocal<Dictionary<object, object>>(() => new Dictionary<object, object>(ReferenceComparer.Instance));
		
		public object GetShared(object key)
		{
			object val;
			sharedDict.TryGetValue(key, out val);
			return val;
		}
		
		public object GetOrAddShared(object key, Func<object, object> valueFactory)
		{
			return sharedDict.GetOrAdd(key, valueFactory);
		}
		
		public object GetOrAddShared(object key, object val)
		{
			return sharedDict.GetOrAdd(key, val);
		}
		
		public void SetShared(object key, object val)
		{
			sharedDict[key] = val;
		}
		
		public object GetThreadLocal(object key)
		{
			object val;
			localDict.Value.TryGetValue(key, out val);
			return val;
		}
		
		public void SetThreadLocal(object key, object val)
		{
			localDict.Value[key] = val;
		}
	}
}
