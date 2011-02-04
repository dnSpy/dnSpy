// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.AvalonEdit.Xml
{
	class TokenReader
	{
		string input;
		int    inputLength;
		int    currentLocation;
		
		// CurrentLocation is assumed to be touched and the fact does not
		//   have to be recorded in this variable.
		// This stores any value bigger than that if applicable.
		// Acutal value is max(currentLocation, maxTouchedLocation).
		int    maxTouchedLocation;
		
		public int InputLength {
			get { return inputLength; }
		}
		
		public int CurrentLocation {
			get { return currentLocation; }
		}
		
		public int MaxTouchedLocation {
			get { return Math.Max(currentLocation, maxTouchedLocation); }
		}
		
		public TokenReader(string input)
		{
			this.input = input;
			this.inputLength = input.Length;
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
			AXmlParser.Assert(HasMoreData(), "Unexpected end of file");
		}
		
		protected bool TryMoveNext()
		{
			if (currentLocation == inputLength) return false;
			
			currentLocation++;
			return true;
		}
		
		protected void Skip(int count)
		{
			AXmlParser.Assert(currentLocation + count <= inputLength, "Skipping after the end of file");
			currentLocation += count;
		}
		
		protected void GoBack(int oldLocation)
		{
			AXmlParser.Assert(oldLocation <= currentLocation, "Trying to move forward");
			maxTouchedLocation = Math.Max(maxTouchedLocation, currentLocation);
			currentLocation = oldLocation;
		}
		
		protected bool TryRead(char c)
		{
			if (currentLocation == inputLength) return false;
			
			if (input[currentLocation] == c) {
				currentLocation++;
				return true;
			} else {
				return false;
			}
		}
		
		protected bool TryReadAnyOf(params char[] c)
		{
			if (currentLocation == inputLength) return false;
			
			if (c.Contains(input[currentLocation])) {
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
			
			return input[currentLocation - back] == c;
		}
		
		protected bool TryPeek(char c)
		{
			if (currentLocation == inputLength) return false;
			
			return input[currentLocation] == c;
		}
		
		protected bool TryPeekAnyOf(params char[] chars)
		{
			if (currentLocation == inputLength) return false;
			
			return chars.Contains(input[currentLocation]);
		}
		
		protected bool TryPeek(string text)
		{
			if (!TryPeek(text[0])) return false; // Early exit
			
			maxTouchedLocation = Math.Max(maxTouchedLocation, currentLocation + (text.Length - 1));
			// The following comparison 'touches' the end of file - it does depend on the end being there
			if (currentLocation + text.Length > inputLength) return false;
			
			return input.Substring(currentLocation, text.Length) == text;
		}
		
		protected bool TryPeekWhiteSpace()
		{
			if (currentLocation == inputLength) return false;
			
			char c = input[currentLocation];
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
				maxTouchedLocation = index + text.Length - 1;
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
				char c = input[currentLocation];
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
		/// <returns> True if read at least one character </returns>
		protected bool TryReadName(out string res)
		{
			int start = currentLocation;
			// Keep reading up to invalid character
			while(true) {
				if (currentLocation == inputLength) break;              // Reject end of file
				char c = input[currentLocation];
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
			if (start == currentLocation) {
				res = string.Empty;
				return false;
			} else {
				res = GetText(start, currentLocation);
				return true;
			}
		}
		
		protected string GetText(int start, int end)
		{
			AXmlParser.Assert(end <= currentLocation, "Reading ahead of current location");
			if (start == inputLength && end == inputLength) {
				return string.Empty;
			} else {
				return GetCachedString(input.Substring(start, end - start));
			}
		}
		
		Dictionary<string, string> stringCache = new Dictionary<string, string>();
		int stringCacheRequestedCount;
		int stringCacheRequestedSize;
		int stringCacheStoredCount;
		int stringCacheStoredSize;
		
		string GetCachedString(string cached)
		{
			stringCacheRequestedCount += 1;
			stringCacheRequestedSize += 8 + 2 * cached.Length;
			// Do not bother with long strings
			if (cached.Length > 32) {
				stringCacheStoredCount += 1;
				stringCacheStoredSize += 8 + 2 * cached.Length;
				return cached;
			}
			if (stringCache.ContainsKey(cached)) {
				// Get the instance from the cache instead
				return stringCache[cached];
			} else {
				// Add to cache
				stringCacheStoredCount += 1;
				stringCacheStoredSize += 8 + 2 * cached.Length;
				stringCache.Add(cached, cached);
				return cached;
			}
		}
		
		public void PrintStringCacheStats()
		{
			AXmlParser.Log("String cache: Requested {0} ({1} bytes);  Actaully stored {2} ({3} bytes); {4}% stored", stringCacheRequestedCount, stringCacheRequestedSize, stringCacheStoredCount, stringCacheStoredSize, stringCacheRequestedSize == 0 ? 0 : stringCacheStoredSize * 100 / stringCacheRequestedSize);
		}
	}
}
