// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
