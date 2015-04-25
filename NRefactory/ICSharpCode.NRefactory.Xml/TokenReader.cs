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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	class TokenReader
	{
		protected readonly ITextSource input;
		protected readonly int inputLength;
		int currentLocation;
		
		// CurrentLocation is assumed to be touched and the fact does not
		//   have to be recorded in this variable.
		// This stores any value bigger than that if applicable.
		// Actual value is max(currentLocation, maxTouchedLocation).
		int maxTouchedLocation;
		
		public int InputLength {
			get { return inputLength; }
		}
		
		public int CurrentLocation {
			get { return currentLocation; }
		}
		
		public int MaxTouchedLocation {
			// add 1 to currentLocation because single-char-peek does not increment maxTouchedLocation
			get { return Math.Max(currentLocation + 1, maxTouchedLocation); }
		}
		
		public TokenReader(ITextSource input)
		{
			this.input = input;
			this.inputLength = input.TextLength;
		}
		
		protected bool IsEndOfFile()
		{
			return currentLocation == inputLength;
		}
		
		protected bool HasMoreData()
		{
			return currentLocation < inputLength;
		}
		
		protected void AssertHasMoreData()
		{
			Log.Assert(HasMoreData(), "Unexpected end of file");
		}
		
		protected bool TryMoveNext()
		{
			if (currentLocation == inputLength) return false;
			
			currentLocation++;
			return true;
		}
		
		protected void Skip(int count)
		{
			Log.Assert(currentLocation + count <= inputLength, "Skipping after the end of file");
			currentLocation += count;
		}
		
		protected void GoBack(int oldLocation)
		{
			Log.Assert(oldLocation <= currentLocation, "Trying to move forward");
			// add 1 because single-char-peek does not increment maxTouchedLocation
			maxTouchedLocation = Math.Max(maxTouchedLocation, currentLocation + 1);
			currentLocation = oldLocation;
		}
		
		protected bool TryRead(char c)
		{
			if (currentLocation == inputLength) return false;
			
			if (input.GetCharAt(currentLocation) == c) {
				currentLocation++;
				return true;
			} else {
				return false;
			}
		}
		
		protected bool TryReadAnyOf(params char[] c)
		{
			if (currentLocation == inputLength) return false;
			
			if (c.Contains(input.GetCharAt(currentLocation))) {
				currentLocation++;
				return true;
			} else {
				return false;
			}
		}
		
		protected bool TryRead(string text)
		{
			if (TryPeek(text)) {
				currentLocation += text.Length;
				return true;
			} else {
				return false;
			}
		}
		
		protected bool TryPeekPrevious(char c, int back)
		{
			if (currentLocation - back == inputLength) return false;
			if (currentLocation - back < 0 ) return false;
			
			return input.GetCharAt(currentLocation - back) == c;
		}
		
		protected bool TryPeek(char c)
		{
			if (currentLocation == inputLength) return false;
			
			return input.GetCharAt(currentLocation) == c;
		}
		
		protected bool TryPeekAnyOf(params char[] chars)
		{
			if (currentLocation == inputLength) return false;
			
			return chars.Contains(input.GetCharAt(currentLocation));
		}
		
		protected bool TryPeek(string text)
		{
			if (!TryPeek(text[0])) return false; // Early exit
			
			maxTouchedLocation = Math.Max(maxTouchedLocation, currentLocation + text.Length);
			// The following comparison 'touches' the end of file - it does depend on the end being there
			if (currentLocation + text.Length > inputLength) return false;
			
			return input.GetText(currentLocation, text.Length) == text;
		}
		
		protected bool TryPeekWhiteSpace()
		{
			if (currentLocation == inputLength) return false;
			
			char c = input.GetCharAt(currentLocation);
			return ((int)c <= 0x20) && (c == ' ' || c == '\t' || c == '\n' || c == '\r');
		}
		
		// The move functions do not have to move if already at target
		// The move functions allow 'overriding' of the document length
		
		protected bool TryMoveTo(char c)
		{
			return TryMoveTo(c, inputLength);
		}
		
		protected bool TryMoveTo(char c, int inputLength)
		{
			if (currentLocation == inputLength) return false;
			int index = input.IndexOf(c, currentLocation, inputLength - currentLocation);
			if (index != -1) {
				currentLocation = index;
				return true;
			} else {
				currentLocation = inputLength;
				return false;
			}
		}
		
		protected bool TryMoveToAnyOf(params char[] c)
		{
			return TryMoveToAnyOf(c, inputLength);
		}
		
		protected bool TryMoveToAnyOf(char[] c, int inputLength)
		{
			if (currentLocation == inputLength) return false;
			int index = input.IndexOfAny(c, currentLocation, inputLength - currentLocation);
			if (index != -1) {
				currentLocation = index;
				return true;
			} else {
				currentLocation = inputLength;
				return false;
			}
		}
		
		protected bool TryMoveTo(string text)
		{
			return TryMoveTo(text, inputLength);
		}
		
		protected bool TryMoveTo(string text, int inputLength)
		{
			if (currentLocation == inputLength) return false;
			int index = input.IndexOf(text, currentLocation, inputLength - currentLocation, StringComparison.Ordinal);
			if (index != -1) {
				maxTouchedLocation = index + text.Length;
				currentLocation = index;
				return true;
			} else {
				currentLocation = inputLength;
				return false;
			}
		}
		
		protected bool TryMoveToNonWhiteSpace()
		{
			return TryMoveToNonWhiteSpace(inputLength);
		}
		
		protected bool TryMoveToNonWhiteSpace(int inputLength)
		{
			while(true) {
				if (currentLocation == inputLength) return false; // Reject end of file
				char c = input.GetCharAt(currentLocation);
				if (((int)c <= 0x20) && (c == ' ' || c == '\t' || c == '\n' || c == '\r')) {
					currentLocation++;                            // Accept white-space
					continue;
				} else {
					return true;  // Found non-white-space
				}
			}
		}
		
		/// <summary>
		/// Read a name token.
		/// The following characters are not allowed:
		///   ""         End of file
		///   " \n\r\t"  Whitesapce
		///   "=\'\""    Attribute value
		///   "&lt;>/?"  Tags
		/// </summary>
		/// <returns>Returns the length of the name</returns>
		protected bool TryReadName()
		{
			int start = currentLocation;
			// Keep reading up to invalid character
			while (HasMoreData()) {
				char c = input.GetCharAt(currentLocation);
				if (0x41 <= (int)c) {                                   // Accpet from 'A' onwards
					currentLocation++;
					continue;
				}
				if (c == ' ' || c == '\n' || c == '\r' || c == '\t' ||  // Reject whitesapce
				    c == '=' || c == '\'' || c == '"'  ||               // Reject attributes
				    c == '<' || c == '>'  || c == '/'  || c == '?') {   // Reject tags
					break;
				} else {
					currentLocation++;
					continue;                                            // Accept other character
				}
			}
			return currentLocation > start;
		}
		
		protected bool TryReadName(out string name)
		{
			int start = currentLocation;
			if (TryReadName()) {
				name = GetCachedString(GetText(start, currentLocation));
				return true;
			} else {
				name = string.Empty;
				return false;
			}
		}
		
		protected string GetText(int start, int end)
		{
			Log.Assert(end <= currentLocation, "Reading ahead of current location");
			return input.GetText(start, end - start);
		}
		
		Dictionary<string, string> stringCache = new Dictionary<string, string>();
		
		#if DEBUG
		int stringCacheRequestedCount;
		int stringCacheRequestedSize;
		int stringCacheStoredCount;
		int stringCacheStoredSize;
		#endif
		
		internal void PrintStringCacheStats()
		{
			#if DEBUG
			Log.WriteLine("String cache: Requested {0} ({1} bytes);  Actaully stored {2} ({3} bytes); {4}% stored", stringCacheRequestedCount, stringCacheRequestedSize, stringCacheStoredCount, stringCacheStoredSize, stringCacheRequestedSize == 0 ? 0 : stringCacheStoredSize * 100 / stringCacheRequestedSize);
			#endif
		}
		
		[Conditional("DEBUG")]
		void AddToRequestedSize(string text)
		{
			#if DEBUG
			stringCacheRequestedCount += 1;
			stringCacheRequestedSize += 8 + 2 * text.Length;
			#endif
		}
		
		[Conditional("DEBUG")]
		void AddToStoredSize(string text)
		{
			#if DEBUG
			stringCacheStoredCount += 1;
			stringCacheStoredSize += 8 + 2 * text.Length;
			#endif
		}
		
		protected string GetCachedString(string cached)
		{
			AddToRequestedSize(cached);
			// Do not bother with long strings
			if (cached.Length > 32) {
				AddToStoredSize(cached);
				return cached;
			}
			string result;
			if (stringCache.TryGetValue(cached, out result)) {
				// Get the instance from the cache instead
				return result;
			} else {
				// Add to cache
				AddToStoredSize(cached);
				stringCache.Add(cached, cached);
				return cached;
			}
		}
	}
}
