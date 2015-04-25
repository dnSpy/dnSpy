// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Diagnostics;
using System.Runtime.Serialization;

namespace ICSharpCode.NRefactory.Xml
{
	static class Log
	{
		/// <summary> Throws exception if condition is false </summary>
		internal static void Assert(bool condition, string message)
		{
			if (!condition) {
				throw new InternalException("Assertion failed: " + message);
			}
		}

		/// <summary> Throws exception if condition is false </summary>
		[Conditional("DEBUG")]
		internal static void DebugAssert(bool condition, string message)
		{
			if (!condition) {
				throw new InternalException("Assertion failed: " + message);
			}
		}

		[Conditional("DEBUG")]
		internal static void WriteLine(string text, params object[] pars)
		{
			//System.Diagnostics.Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "XML: " + text, pars));
		}
	}
	
	
	/// <summary>
	/// Exception used for internal errors in XML parser.
	/// This exception indicates a bug in NRefactory.
	/// </summary>
	[Serializable]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Justification = "This exception is not public because it is not supposed to be caught by user code - it indicates a bug in AvalonEdit.")]
	class InternalException : Exception
	{
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		public InternalException() : base()
		{
		}
		
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		public InternalException(string message) : base(message)
		{
		}
		
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		public InternalException(string message, Exception innerException) : base(message, innerException)
		{
		}
		
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		protected InternalException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
