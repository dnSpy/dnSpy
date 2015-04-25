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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Resolver logging helper.
	/// Wraps System.Diagnostics.Debug so that resolver-specific logging can be enabled/disabled on demand.
	/// (it's a huge amount of debug spew and slows down the resolver quite a bit)
	/// </summary>
	static class Log
	{
		const bool logEnabled = false;
#if __MonoCS__
		[Conditional("MCS_DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		internal static void WriteLine(string text)
		{
			Debug.WriteLine(text);
		}
		
#if __MonoCS__
		[Conditional("MCS_DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		internal static void WriteLine(string format, params object[] args)
		{
			Debug.WriteLine(format, args);
		}
		
#if __MonoCS__
		[Conditional("MCS_DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		internal static void WriteCollection<T>(string text, IEnumerable<T> lines)
		{
			#if DEBUG
			T[] arr = lines.ToArray();
			if (arr.Length == 0) {
				Debug.WriteLine(text + "<empty collection>");
			} else {
				Debug.WriteLine(text + (arr[0] != null ? arr[0].ToString() : "<null>"));
				for (int i = 1; i < arr.Length; i++) {
					Debug.WriteLine(new string(' ', text.Length) + (arr[i] != null ? arr[i].ToString() : "<null>"));
				}
			}
			#endif
		}
		
#if __MonoCS__
		[Conditional("MCS_DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		public static void Indent()
		{
			Debug.Indent();
		}
		
#if __MonoCS__
		[Conditional("MCS_DEBUG")]
#else
		[Conditional(logEnabled ? "DEBUG" : "LOG_DISABLED")]
#endif
		public static void Unindent()
		{
			Debug.Unindent();
		}
	}
}
