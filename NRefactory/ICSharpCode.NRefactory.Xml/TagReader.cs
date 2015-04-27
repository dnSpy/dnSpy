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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	class TagReader : TokenReader
	{
		readonly AXmlParser tagSoupParser;
		readonly Stack<string> elementNameStack;
		
		public TagReader(AXmlParser tagSoupParser, ITextSource input, bool collapseProperlyNestedElements) : base(input)
		{
			this.tagSoupParser = tagSoupParser;
			if (collapseProperlyNestedElements)
				elementNameStack = new Stack<string>();
		}
		
		public List<InternalObject> ReadAllObjects(CancellationToken cancellationToken)
		{
			while (HasMoreData()) {
				cancellationToken.ThrowIfCancellationRequested();
				ReadObject();
			}
			return objects;
		}
		
		public List<InternalObject> ReadAllObjectsIncremental(InternalObject[] oldObjects, List<UnchangedSegment> reuseMap, CancellationToken cancellationToken)
		{
			ObjectIterator oldObjectIterator = new ObjectIterator(oldObjects);
			int reuseMapIndex = 0;
			while (reuseMapIndex < reuseMap.Count) {
				var reuseEntry = reuseMap[reuseMapIndex];
				while (this.CurrentLocation < reuseEntry.NewOffset) {
					cancellationToken.ThrowIfCancellationRequested();
					ReadObject();
				}
				if (this.CurrentLocation >= reuseEntry.NewOffset + reuseEntry.Length) {
					reuseMapIndex++;
					continue;
				}
				Debug.Assert(reuseEntry.NewOffset <= this.CurrentLocation && this.CurrentLocation < reuseEntry.NewOffset + reuseEntry.Length);
				// reuse the nodes within this reuseEntry starting at oldOffset:
				int oldOffset = this.CurrentLocation - reuseEntry.NewOffset + reuseEntry.OldOffset;
				// seek to oldOffset in the oldObjects array:
				oldObjectIterator.SkipTo(oldOffset);
				if (oldObjectIterator.CurrentPosition == oldOffset) {
					// reuse old objects within this reuse entry:
					int reuseEnd = reuseEntry.OldOffset + reuseEntry.Length;
					while (oldObjectIterator.CurrentObject != null && oldObjectIterator.CurrentPosition + oldObjectIterator.CurrentObject.LengthTouched < reuseEnd) {
						StoreObject(oldObjectIterator.CurrentObject);
						Skip(oldObjectIterator.CurrentObject.Length);
						oldObjectIterator.MoveNext();
					}
					reuseMapIndex++; // go to next re-use map
				} else {
					// We are in a region where old objects are available, but aren't aligned correctly.
					// Don't skip this reuse entry, and read a single object so that we can re-align
					ReadObject();
				}
			}
			while (HasMoreData()) {
				cancellationToken.ThrowIfCancellationRequested();
				ReadObject();
			}
			return objects;
		}
		
		void StoreObject(InternalObject obj)
		{
			objects.Add(obj);
			
			// Now combine properly-nested elements:
			if (elementNameStack == null)
				return; // parsing tag soup
			InternalTag tag = obj as InternalTag;
			if (tag == null)
				return;
			if (tag.IsEmptyTag) {
				// the tag is its own element
				objects[objects.Count - 1] = new InternalElement(tag) {
					Length = tag.Length,
					LengthTouched = tag.LengthTouched,
					IsPropertyNested = true,
					StartRelativeToParent = tag.StartRelativeToParent,
					NestedObjects = new [] { tag.SetStartRelativeToParent(0) }
				};
			} else if (tag.IsStartTag) {
				elementNameStack.Push(tag.Name);
			} else if (tag.IsEndTag && elementNameStack.Count > 0) {
				// Now look for the start element:
				int startIndex = objects.Count - 2;
				bool ok = false;
				string expectedName = elementNameStack.Pop();
				if (tag.Name == expectedName) {
					while (startIndex > 0) {
						var startTag = objects[startIndex] as InternalTag;
						if (startTag != null) {
							if (startTag.IsStartTag) {
								ok = (startTag.Name == expectedName);
								break;
							} else if (startTag.IsEndTag) {
								break;
							}
						}
						startIndex--;
					}
				}
				if (ok) {
					// We found a correct nesting, let's create an element:
					InternalObject[] nestedObjects = new InternalObject[objects.Count - startIndex];
					int oldStartRelativeToParent = objects[startIndex].StartRelativeToParent;
					int pos = 0;
					int maxLengthTouched = 0;
					for (int i = 0; i < nestedObjects.Length; i++) {
						nestedObjects[i] = objects[startIndex + i].SetStartRelativeToParent(pos);
						maxLengthTouched = Math.Max(maxLengthTouched, pos + nestedObjects[i].LengthTouched);
						pos += nestedObjects[i].Length;
					}
					objects.RemoveRange(startIndex, nestedObjects.Length);
					objects.Add(
						new InternalElement((InternalTag)nestedObjects[0]) {
							HasEndTag = true,
							IsPropertyNested = true,
							Length = pos,
							LengthTouched = maxLengthTouched,
							StartRelativeToParent = oldStartRelativeToParent,
							NestedObjects = nestedObjects
						});
				} else {
					// Mismatched name - the nesting isn't properly;
					// clear the whole stack so that none of the currently open elements are closed as properly-nested.
					elementNameStack.Clear();
				}
			}
		}
		
		/// <summary>
		/// Reads one or more objects.
		/// </summary>
		void ReadObject()
		{
			if (TryPeek('<')) {
				ReadTag();
			} else {
				ReadText(TextType.CharacterData);
			}
		}
		
		#region BeginInternalObject / EndInternalObject
		List<InternalObject> objects = new List<InternalObject>();
		int internalObjectStartPosition;
		
		int CurrentRelativeLocation {
			get { return CurrentLocation - internalObjectStartPosition; }
		}
		
		struct InternalObjectFrame
		{
			public readonly InternalObject InternalObject;
			public readonly int ParentStartPosition;
			
			public InternalObjectFrame(InternalObject internalObject, int parentStartPosition)
			{
				this.InternalObject = internalObject;
				this.ParentStartPosition = parentStartPosition;
			}
		}
		
		InternalObjectFrame BeginInternalObject(InternalObject internalObject)
		{
			return BeginInternalObject(internalObject, this.CurrentLocation);
		}
		
		InternalObjectFrame BeginInternalObject(InternalObject internalObject, int beginLocation)
		{
			internalObject.StartRelativeToParent = beginLocation - internalObjectStartPosition;
			
			var frame = new InternalObjectFrame(internalObject, internalObjectStartPosition);
			
			internalObjectStartPosition = CurrentLocation;
			return frame;
		}
		
		void EndInternalObject(InternalObjectFrame frame, bool storeNewObject = true)
		{
			frame.InternalObject.Length = this.CurrentLocation - internalObjectStartPosition;
			frame.InternalObject.LengthTouched = this.MaxTouchedLocation - internalObjectStartPosition;
			frame.InternalObject.SyntaxErrors = GetSyntaxErrors();
			if (storeNewObject)
				StoreObject(frame.InternalObject);
			internalObjectStartPosition = frame.ParentStartPosition;
		}
		#endregion
		
		#region Read Tag
		/// <summary>
		/// Context: "&lt;"
		/// </summary>
		void ReadTag()
		{
			AssertHasMoreData();
			
			int tagStart = this.CurrentLocation;
			InternalTag tag = new InternalTag();
			var frame = BeginInternalObject(tag);
			
			// Read the opening bracket
			// It identifies the type of tag and parsing behavior for the rest of it
			tag.OpeningBracket = ReadOpeningBracket();
			
			if (tag.IsUnknownBang && !TryPeekWhiteSpace())
				OnSyntaxError(tagStart, this.CurrentLocation, "Unknown tag");
			
			if (tag.IsStartOrEmptyTag || tag.IsEndTag || tag.IsProcessingInstruction) {
				// Read the name
				TryMoveToNonWhiteSpace();
				tag.RelativeNameStart = this.CurrentRelativeLocation;
				string name;
				if (TryReadName(out name)) {
					if (!IsValidName(name)) {
						OnSyntaxError(this.CurrentLocation - name.Length, this.CurrentLocation, "The name '{0}' is invalid", name);
					}
				} else {
					OnSyntaxError("Element name expected");
				}
				tag.Name = name;
			} else {
				tag.Name = string.Empty;
			}
			
			bool isXmlDeclr = tag.Name == "xml" && tag.IsProcessingInstruction;
			int oldObjectCount = objects.Count;
			
			if (tag.IsStartOrEmptyTag || tag.IsEndTag || isXmlDeclr) {
				// Read attributes for the tag
				while (HasMoreData()) {
					// Chech for all forbiden 'name' characters first - see ReadName
					TryMoveToNonWhiteSpace();
					if (TryPeek('<')) break;
					string endBr;
					int endBrStart = this.CurrentLocation; // Just peek
					if (TryReadClosingBracket(out endBr)) {  // End tag
						GoBack(endBrStart);
						break;
					}
					
					// We have "=\'\"" or name - read attribute
					int attrStartOffset = this.CurrentLocation;
					ReadAttribute();
					if (tag.IsEndTag)
						OnSyntaxError(attrStartOffset, this.CurrentLocation, "Attribute not allowed in end tag.");
				}
			} else if (tag.IsDocumentType) {
				ReadContentOfDTD();
			} else {
				int start = this.CurrentLocation;
				if (tag.IsComment) {
					ReadText(TextType.Comment);
				} else if (tag.IsCData) {
					ReadText(TextType.CData);
				} else if (tag.IsProcessingInstruction) {
					ReadText(TextType.ProcessingInstruction);
				} else if (tag.IsUnknownBang) {
					ReadText(TextType.UnknownBang);
				} else {
					throw new InternalException(string.Format(CultureInfo.InvariantCulture, "Unknown opening bracket '{0}'", tag.OpeningBracket));
				}
				// Backtrack at complete start
				if (IsEndOfFile() || (tag.IsUnknownBang && TryPeek('<'))) {
					GoBack(start);
					objects.RemoveRange(oldObjectCount, objects.Count - oldObjectCount);
				}
			}
			
			// Read closing bracket
			string bracket;
			TryReadClosingBracket(out bracket);
			tag.ClosingBracket = bracket;
			
			// Error check
			int brStart = this.CurrentLocation - (tag.ClosingBracket ?? string.Empty).Length;
			int brEnd = this.CurrentLocation;
			if (tag.Name == null) {
				// One error was reported already
			} else if (tag.IsStartOrEmptyTag) {
				if (tag.ClosingBracket != ">" && tag.ClosingBracket != "/>") OnSyntaxError(brStart, brEnd, "'>' or '/>' expected");
			} else if (tag.IsEndTag) {
				if (tag.ClosingBracket != ">") OnSyntaxError(brStart, brEnd, "'>' expected");
			} else if (tag.IsComment) {
				if (tag.ClosingBracket != "-->") OnSyntaxError(brStart, brEnd, "'-->' expected");
			} else if (tag.IsCData) {
				if (tag.ClosingBracket != "]]>") OnSyntaxError(brStart, brEnd, "']]>' expected");
			} else if (tag.IsProcessingInstruction) {
				if (tag.ClosingBracket != "?>") OnSyntaxError(brStart, brEnd, "'?>' expected");
			} else if (tag.IsUnknownBang) {
				if (tag.ClosingBracket != ">") OnSyntaxError(brStart, brEnd, "'>' expected");
			} else if (tag.IsDocumentType) {
				if (tag.ClosingBracket != ">") OnSyntaxError(brStart, brEnd, "'>' expected");
			} else {
				throw new InternalException(string.Format(CultureInfo.InvariantCulture, "Unknown opening bracket '{0}'", tag.OpeningBracket));
			}
			
			// Attribute name may not apper multiple times
			if (objects.Count > oldObjectCount) {
				// Move nested objects into tag.NestedObjects:
				tag.NestedObjects = new InternalObject[objects.Count - oldObjectCount];
				objects.CopyTo(oldObjectCount, tag.NestedObjects, 0, tag.NestedObjects.Length);
				objects.RemoveRange(oldObjectCount, objects.Count - oldObjectCount);
				
				// Look for duplicate attributes:
				HashSet<string> attributeNames = new HashSet<string>();
				foreach (var obj in tag.NestedObjects) {
					InternalAttribute attr = obj as InternalAttribute;
					if (attr != null && !attributeNames.Add(attr.Name)) {
						int attrStart = tagStart + attr.StartRelativeToParent;
						OnSyntaxError(attrStart, attrStart + attr.Name.Length, "Attribute with name '{0}' already exists", attr.Name);
					}
				}
			}
			
			EndInternalObject(frame);
		}
		#endregion
		
		#region Read DTD
		void ReadContentOfDTD()
		{
			int start = this.CurrentLocation;
			while (HasMoreData()) {
				TryMoveToNonWhiteSpace();            // Skip whitespace
				if (TryRead('\'')) TryMoveTo('\'');  // Skip single quoted string TODO: Bug
				if (TryRead('\"')) TryMoveTo('\"');  // Skip single quoted string
				if (TryRead('[')) {                  // Start of nested infoset
					// Reading infoset
					while (HasMoreData()) {
						TryMoveToAnyOf('<', ']');
						if (TryPeek('<')) {
							if (start != this.CurrentLocation) {  // Two following tags
								MakeText(start, this.CurrentLocation);
							}
							ReadTag();
							start = this.CurrentLocation;
						}
						if (TryPeek(']')) break;
					}
				}
				TryRead(']');                        // End of nested infoset
				if (TryPeek('>')) break;             // Proper closing
				if (TryPeek('<')) break;             // Malformed XML
				TryMoveNext();                       // Skip anything else
			}
			if (start != this.CurrentLocation) {
				MakeText(start, this.CurrentLocation);
			}
		}
		
		void MakeText(int start, int end)
		{
			Log.DebugAssert(end > start, "Empty text");
			Log.DebugAssert(end == this.CurrentLocation, "end == current location");
			
			InternalText text = new InternalText();
			var frame = BeginInternalObject(text, start);
			text.Type = TextType.Other;
			text.Value = GetText(start, end);
			EndInternalObject(frame);
		}
		#endregion
		
		#region Read Brackets
		/// <summary>
		/// Reads any of the know opening brackets.  (only full bracket)
		/// Context: "&lt;"
		/// </summary>
		string ReadOpeningBracket()
		{
			// We are using a lot of string literals so that the memory instances are shared
			//int start = this.CurrentLocation;
			if (TryRead('<')) {
				if (TryRead('/')) {
					return "</";
				} else if (TryRead('?')) {
					return "<?";
				} else if (TryRead('!')) {
					if (TryRead("--")) {
						return "<!--";
					} else if (TryRead("[CDATA[")) {
						return "<![CDATA[";
					} else {
						foreach (string dtdName in AXmlTag.DtdNames) {
							// the dtdName includes "<!"
							if (TryRead(dtdName.Remove(0, 2))) return dtdName;
						}
						return "<!";
					}
				} else {
					return "<";
				}
			} else {
				throw new InternalException("'<' expected");
			}
		}
		
		/// <summary>
		/// Reads any of the know closing brackets.  (only full bracket)
		/// Context: any
		/// </summary>
		bool TryReadClosingBracket(out string bracket)
		{
			// We are using a lot of string literals so that the memory instances are shared
			if (TryRead('>')) {
				bracket = ">";
			} else 	if (TryRead("/>")) {
				bracket = "/>";
			} else 	if (TryRead("?>")) {
				bracket = "?>";
			} else if (TryRead("-->")) {
				bracket = "-->";
			} else if (TryRead("]]>")) {
				bracket = "]]>";
			} else {
				bracket = string.Empty;
				return false;
			}
			return true;
		}
		#endregion
		
		#region Attributes
		/// <summary>
		/// Context: name or "=\'\""
		/// </summary>
		void ReadAttribute()
		{
			AssertHasMoreData();
			
			InternalAttribute attr = new InternalAttribute();
			var frame = BeginInternalObject(attr);
			
			// Read name
			string name;
			if (TryReadName(out name)) {
				if (!IsValidName(name)) {
					OnSyntaxError(this.CurrentLocation - name.Length, this.CurrentLocation, "The name '{0}' is invalid", name);
				}
			} else {
				OnSyntaxError("Attribute name expected");
			}
			attr.Name = name;
			
			// Read equals sign and surrounding whitespace
			int checkpoint = this.CurrentLocation;
			TryMoveToNonWhiteSpace();
			if (TryRead('=')) {
				int chk2 = this.CurrentLocation;
				TryMoveToNonWhiteSpace();
				if (!TryPeek('"') && !TryPeek('\'')) {
					// Do not read whitespace if quote does not follow
					GoBack(chk2);
				}
				attr.EqualsSignLength = this.CurrentLocation - checkpoint;
			} else {
				GoBack(checkpoint);
				OnSyntaxError("'=' expected");
				attr.EqualsSignLength = 0;
			}
			
			// Read attribute value
			int start = this.CurrentLocation;
			char quoteChar = TryPeek('"') ? '"' : '\'';
			bool startsWithQuote;
			if (TryRead(quoteChar)) {
				startsWithQuote = true;
				int valueStart = this.CurrentLocation;
				TryMoveToAnyOf(quoteChar, '<');
				if (TryRead(quoteChar)) {
					if (!TryPeekAnyOf(' ', '\t', '\n', '\r', '/', '>', '?')) {
						if (TryPeekPrevious('=', 2) || (TryPeekPrevious('=', 3) && TryPeekPrevious(' ', 2))) {
							// This actually most likely means that we are in the next attribute value
							GoBack(valueStart);
							ReadAttributeValue(quoteChar);
							if (TryRead(quoteChar)) {
								OnSyntaxError("White space or end of tag expected");
							} else {
								OnSyntaxError("Quote {0} expected (or add whitespace after the following one)", quoteChar);
							}
						} else {
							OnSyntaxError("White space or end of tag expected");
						}
					}
				} else {
					// '<' or end of file
					GoBack(valueStart);
					ReadAttributeValue(quoteChar);
					OnSyntaxError("Quote {0} expected", quoteChar);
				}
			} else {
				startsWithQuote = false;
				int valueStart = this.CurrentLocation;
				ReadAttributeValue(null);
				TryRead('\"');
				TryRead('\'');
				if (valueStart == this.CurrentLocation) {
					OnSyntaxError("Attribute value expected");
				} else {
					OnSyntaxError(valueStart, this.CurrentLocation, "Attribute value must be quoted");
				}
			}
			string val = GetText(start, this.CurrentLocation);
			val = Unquote(val);
			attr.Value = Dereference(val, startsWithQuote ? start + 1 : start);
			
			EndInternalObject(frame);
		}
		
		/// <summary>
		/// Read everything up to quote (excluding), opening/closing tag or attribute signature
		/// </summary>
		void ReadAttributeValue(char? quote)
		{
			while (HasMoreData()) {
				// What is next?
				int start = this.CurrentLocation;
				TryMoveToNonWhiteSpace();  // Read white space (if any)
				if (quote.HasValue) {
					if (TryPeek(quote.Value)) return;
				} else {
					if (TryPeek('"') || TryPeek('\'')) return;
				}
				// Opening/closing tag
				string endBr;
				if (TryPeek('<') || TryReadClosingBracket(out endBr)) {
					GoBack(start);
					return;
				}
				// Try reading attribute signature
				if (TryReadName()) {
					int nameEnd = this.CurrentLocation;
					if (TryMoveToNonWhiteSpace() && TryRead("=") &&
					    TryMoveToNonWhiteSpace() && TryPeekAnyOf('"', '\''))
					{
						// Start of attribute.  Great
						GoBack(start);
						return;  // Done
					} else {
						// Just some gargabe - make it part of the value
						GoBack(nameEnd);
						continue;  // Read more
					}
				}
				TryMoveNext(); // Accept everyting else
			}
		}
		
		/// <summary> Remove quoting from the given string </summary>
		static string Unquote(string quoted)
		{
			if (string.IsNullOrEmpty(quoted)) return string.Empty;
			char first = quoted[0];
			if (quoted.Length == 1) return (first == '"' || first == '\'') ? string.Empty : quoted;
			char last  = quoted[quoted.Length - 1];
			if (first == '"' || first == '\'') {
				if (first == last) {
					// Remove both quotes
					return quoted.Substring(1, quoted.Length - 2);
				} else {
					// Remove first quote
					return quoted.Remove(0, 1);
				}
			} else {
				if (last == '"' || last == '\'') {
					// Remove last quote
					return quoted.Substring(0, quoted.Length - 1);
				} else {
					// Keep whole string
					return quoted;
				}
			}
		}
		#endregion
		
		#region Text
		/// <summary>
		/// Reads text.
		/// </summary>
		void ReadText(TextType type)
		{
			var text = new InternalText();
			var frame = BeginInternalObject(text);
			text.Type = type;
			
			int start = this.CurrentLocation;
			int fragmentEnd = inputLength;
			
			// Whitespace would be skipped anyway by any operation
			TryMoveToNonWhiteSpace(fragmentEnd);
			int wsEnd = this.CurrentLocation;
			
			// Try move to the terminator given by the context
			if (type == TextType.WhiteSpace) {
				TryMoveToNonWhiteSpace(fragmentEnd);
			} else if (type == TextType.CharacterData) {
				while(true) {
					if (!TryMoveToAnyOf(new char[] {'<', ']'}, fragmentEnd)) break; // End of fragment
					if (TryPeek('<')) break;
					if (TryPeek(']')) {
						if (TryPeek("]]>")) {
							OnSyntaxError(this.CurrentLocation, this.CurrentLocation + 3, "']]>' is not allowed in text");
						}
						TryMoveNext();
						continue;
					}
					throw new InternalException("Infinite loop");
				}
			} else if (type == TextType.Comment) {
				// Do not report too many errors
				bool errorReported = false;
				while(true) {
					if (!TryMoveTo('-', fragmentEnd)) break; // End of fragment
					if (TryPeek("-->")) break;
					if (TryPeek("--") && !errorReported) {
						OnSyntaxError(this.CurrentLocation, this.CurrentLocation + 2, "'--' is not allowed in comment");
						errorReported = true;
					}
					TryMoveNext();
				}
			} else if (type == TextType.CData) {
				while(true) {
					// We can not use use TryMoveTo("]]>", fragmentEnd) because it may incorectly accept "]" at the end of fragment
					if (!TryMoveTo(']', fragmentEnd)) break; // End of fragment
					if (TryPeek("]]>")) break;
					TryMoveNext();
				}
			} else if (type == TextType.ProcessingInstruction) {
				while(true) {
					if (!TryMoveTo('?', fragmentEnd)) break; // End of fragment
					if (TryPeek("?>")) break;
					TryMoveNext();
				}
			} else if (type == TextType.UnknownBang) {
				TryMoveToAnyOf(new char[] {'<', '>'}, fragmentEnd);
			} else {
				throw new InternalException("Unknown type " + type);
			}
			
			text.ContainsOnlyWhitespace = (wsEnd == this.CurrentLocation);
			
			string escapedValue = GetText(start, this.CurrentLocation);
			if (type == TextType.CharacterData) {
				text.Value = Dereference(escapedValue, start);
			} else {
				text.Value = escapedValue;
			}
			text.Value = GetCachedString(text.Value);
			
			EndInternalObject(frame, storeNewObject: this.CurrentLocation > start);
		}
		#endregion
		
		#region Dereference
		const int maxEntityLength = 16; // The longest built-in one is 10 ("&#1114111;")
		
		string Dereference(string text, int textLocation)
		{
			StringBuilder sb = null;  // The dereferenced text so far (all up to 'curr')
			int curr = 0;
			while(true) {
				// Reached end of input
				if (curr == text.Length) {
					if (sb != null) {
						return sb.ToString();
					} else {
						return text;
					}
				}
				
				// Try to find reference
				int start = text.IndexOf('&', curr);
				
				// No more references found
				if (start == -1) {
					if (sb != null) {
						sb.Append(text, curr, text.Length - curr); // Add rest
						return sb.ToString();
					} else {
						return text;
					}
				}
				
				// Append text before the enitiy reference
				if (sb == null) sb = new StringBuilder(text.Length);
				sb.Append(text, curr, start - curr);
				curr = start;
				
				// Process the entity
				int errorLoc = textLocation + sb.Length;
				
				// Find entity name
				int end = text.IndexOfAny(new char[] {'&', ';'}, start + 1, Math.Min(maxEntityLength, text.Length - (start + 1)));
				if (end == -1 || text[end] == '&') {
					// Not found
					OnSyntaxError(errorLoc, errorLoc + 1, "Entity reference must be terminated with ';'");
					// Keep '&'
					sb.Append('&');
					curr++;
					continue;  // Restart and next character location
				}
				string name = text.Substring(start + 1, end - (start + 1));
				
				// Resolve the name
				string replacement;
				if (name.Length == 0) {
					replacement = null;
					OnSyntaxError(errorLoc + 1, errorLoc + 1, "Entity name expected");
				} else if (name == "amp") {
					replacement = "&";
				} else if (name == "lt") {
					replacement = "<";
				} else if (name == "gt") {
					replacement = ">";
				} else if (name == "apos") {
					replacement = "'";
				} else if (name == "quot") {
					replacement = "\"";
				} else if (name.Length > 0 && name[0] == '#') {
					int num;
					if (name.Length > 1 && name[1] == 'x') {
						if (!int.TryParse(name.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture.NumberFormat, out num)) {
							num = -1;
							OnSyntaxError(errorLoc + 3, errorLoc + 1 + name.Length, "Hexadecimal code of unicode character expected");
						}
					} else {
						if (!int.TryParse(name.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out num)) {
							num = -1;
							OnSyntaxError(errorLoc + 2, errorLoc + 1 + name.Length, "Numeric code of unicode character expected");
						}
					}
					if (num != -1) {
						try {
							replacement = char.ConvertFromUtf32(num);
						} catch (ArgumentOutOfRangeException) {
							replacement = null;
							OnSyntaxError(errorLoc + 2, errorLoc + 1 + name.Length, "Invalid unicode character U+{0:X} ({0})", num);
						}
					} else {
						replacement = null;
					}
				} else if (!IsValidName(name)) {
					replacement = null;
					OnSyntaxError(errorLoc + 1, errorLoc + 1, "Invalid entity name");
				} else {
					replacement = null;
					if (tagSoupParser.UnknownEntityReferenceIsError) {
						OnSyntaxError(errorLoc, errorLoc + 1 + name.Length + 1, "Unknown entity reference '{0}'", name);
					}
				}
				
				// Append the replacement to output
				if (replacement != null) {
					sb.Append(replacement);
				} else {
					sb.Append('&');
					sb.Append(name);
					sb.Append(';');
				}
				curr = end + 1;
				continue;
			}
		}
		#endregion
		
		#region Syntax Errors
		List<InternalSyntaxError> syntaxErrors = new List<InternalSyntaxError>();
		
		InternalSyntaxError[] GetSyntaxErrors()
		{
			if (syntaxErrors.Count > 0) {
				var arr = syntaxErrors.ToArray();
				syntaxErrors.Clear();
				return arr;
			} else {
				return null;
			}
		}
		
		void OnSyntaxError(string message, params object[] args)
		{
			OnSyntaxError(this.CurrentLocation, this.CurrentLocation + 1, message, args);
		}
		
		void OnSyntaxError(int start, int end, string message, params object[] args)
		{
			if (end <= start) end = start + 1;
			string formattedMessage = string.Format(CultureInfo.InvariantCulture, message, args);
			Log.WriteLine("Syntax error ({0}-{1}): {2}", start, end, formattedMessage);
			syntaxErrors.Add(new InternalSyntaxError(start - internalObjectStartPosition, end - internalObjectStartPosition, formattedMessage));
		}
		#endregion
		
		#region Helper functions
		internal static bool IsValidName(string name)
		{
			try {
				System.Xml.XmlConvert.VerifyName(name);
				return true;
			} catch (System.Xml.XmlException) {
				return false;
			}
		}
		#endregion
	}
}
