// 
// WordParser.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	public static class WordParser
	{
		public static List<string> BreakWords (string identifier)
		{
			var words = new List<string> ();
			int wordStart = 0;
			bool lastWasLower = false, lastWasUpper = false;
			for (int i = 0; i < identifier.Length; i++) {
				char c = identifier[i];
				var category = char.GetUnicodeCategory (c);
				if (category == System.Globalization.UnicodeCategory.LowercaseLetter) {
					if (lastWasUpper && (i - wordStart) > 2) {
						words.Add (identifier.Substring (wordStart, i - wordStart - 1));
						wordStart = i - 1;
					}
					lastWasLower = true;
					lastWasUpper = false;
				} else if (category == System.Globalization.UnicodeCategory.UppercaseLetter) {
					if (lastWasLower) {
						words.Add (identifier.Substring (wordStart, i - wordStart));
						wordStart = i;
					}
					lastWasLower = false;
					lastWasUpper = true;
				} else {
					if (c == '_') {
						if ((i - wordStart) > 0)
							words.Add(identifier.Substring(wordStart, i - wordStart));
						wordStart = i + 1;
						lastWasLower = lastWasUpper = false;
					}
				}
			}
			if (wordStart < identifier.Length)
				words.Add (identifier.Substring (wordStart));
			return words;
		}
	}
}

