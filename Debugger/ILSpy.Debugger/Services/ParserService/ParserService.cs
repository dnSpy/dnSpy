// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.ILSpy.Debugger.Services
{
	/// <summary>
	/// Very naive parser.
	/// </summary>
	static class ParserService
	{
		static HashSet<string> mySet = new HashSet<string>();
		
		static ParserService()
		{
			mySet.AddRange((new [] {
			                	".",
			                	"{",
			                	"}",
			                	"(",
			                	")",
			                	"[",
			                	"]",
			                	" ",
			                	"=",
			                	"+",
			                	"-",
			                	"/",
			                	"%",
			                	"*",
			                	"&",
			                	Environment.NewLine,
			                	";",
			                	",",
			                	"~",
			                	"!",
			                	"?",
			                	@"\n",
			                	@"\t",
			                	@"\r",
			                	"|"
			                }).AsReadOnly());
		}
		
		/// <summary>
		/// Returns the variable name
		/// </summary>
		/// <param name="fullText"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public static string SimpleParseAt(string fullText, int offset)
		{
			if (string.IsNullOrEmpty(fullText))
				return string.Empty;
			
			if (offset <= 0 || offset >= fullText.Length)
				return string.Empty;
			
			string currentValue = fullText[offset].ToString();
			
			if (mySet.Contains(currentValue))
				return string.Empty;
			
			int left = offset, right = offset;
			
			//search left
			while((!mySet.Contains(currentValue) || currentValue == ".") && left > 0)
				currentValue = fullText[--left].ToString();
			
			currentValue = fullText[offset].ToString();
			// searh right
			while(!mySet.Contains(currentValue) && right < fullText.Length - 2)
				currentValue = fullText[++right].ToString();
			
			return fullText.Substring(left + 1, right - 1 - left).Trim();
		}
	}
}
