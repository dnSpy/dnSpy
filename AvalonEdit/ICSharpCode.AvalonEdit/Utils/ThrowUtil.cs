// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Globalization;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Contains exception-throwing helper methods.
	/// </summary>
	static class ThrowUtil
	{
		/// <summary>
		/// Throws an ArgumentNullException if <paramref name="val"/> is null; otherwise
		/// returns val.
		/// </summary>
		/// <example>
		/// Use this method to throw an ArgumentNullException when using parameters for base
		/// constructor calls.
		/// <code>
		/// public VisualLineText(string text) : base(ThrowUtil.CheckNotNull(text, "text").Length)
		/// </code>
		/// </example>
		public static T CheckNotNull<T>(T val, string parameterName) where T : class
		{
			if (val == null)
				throw new ArgumentNullException(parameterName);
			return val;
		}
		
		public static int CheckNotNegative(int val, string parameterName)
		{
			if (val < 0)
				throw new ArgumentOutOfRangeException(parameterName, val, "value must not be negative");
			return val;
		}
		
		public static int CheckInRangeInclusive(int val, string parameterName, int lower, int upper)
		{
			if (val < lower || val > upper)
				throw new ArgumentOutOfRangeException(parameterName, val, "Expected: " + lower.ToString(CultureInfo.InvariantCulture) + " <= " + parameterName + " <= " + upper.ToString(CultureInfo.InvariantCulture));
			return val;
		}
		
		public static InvalidOperationException NoDocumentAssigned()
		{
			return new InvalidOperationException("Document is null");
		}
		
		public static InvalidOperationException NoValidCaretPosition()
		{
			return new InvalidOperationException("Could not find a valid caret position in the line");
		}
	}
}
