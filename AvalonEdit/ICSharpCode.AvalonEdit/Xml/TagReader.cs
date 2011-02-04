// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ICSharpCode.AvalonEdit.Xml
{
	class TagReader: TokenReader
	{
		AXmlParser parser;
		TrackedSegmentCollection trackedSegments;
		string input;
		
		public TagReader(AXmlParser parser, string input): base(input)
		{
			this.parser = parser;
			this.trackedSegments = parser.TrackedSegments;
			this.input = input;
		}
		
		bool TryReadFromCacheOrNew<T>(out T res) where T: AXmlObject, new()
		{
			return TryReadFromCacheOrNew(out res, t => true);
		}
		
		bool TryReadFromCacheOrNew<T>(out T res, Predicate<T> condition) where T: AXmlObject, new()
		{
			T cached = trackedSegments.GetCachedObject<T>(this.CurrentLocation, 0, condition);
			if (cached != null) {
				Skip(cached.Length);
				AXmlParser.Assert(cached.Length > 0, "cached elements must not have zero length");
				res = cached;
				return true;
			} else {
				res = new T();
				return false;
			}
		}
		
		void OnParsed(AXmlObject obj)
		{
			AXmlParser.Log("Parsed {0}", obj);
			trackedSegments.AddParsedObject(obj, this.MaxTouchedLocation > this.CurrentLocation ? (int?)this.MaxTouchedLocation : null);
		}
		
		/// <summary>
		/// Read all tags in the document in a flat sequence.
		/// It also includes the text between tags and possibly some properly nested Elements from cache.
		/// </summary>
		public List<AXmlObject> ReadAllTags()
		{
			List<AXmlObject> stream = new List<AXmlObject>();
			
			while(true) {
				if (IsEndOfFile()) {
					break;
				} else if (TryPeek('<')) {
					AXmlElement elem;
					if (TryReadFromCacheOrNew(out elem, e => e.IsProperlyNested)) {
						stream.Add(elem);
					} else {
						stream.Add(ReadTag());
					}
				} else {
					stream.AddRange(ReadText(TextType.CharacterData));
				}
			}
			
			return stream;
		}
		
		/// <summary>
		/// Context: "&lt;"
		/// </summary>
		AXmlTag ReadTag()
		{
			AssertHasMoreData();
			
			AXmlTag tag;
			if (TryReadFromCacheOrNew(out tag)) return tag;
			
			tag.StartOffset = this.CurrentLocation;
			
			// Read the opening bracket
			// It identifies the type of tag and parsing behavior for the rest of it
			tag.OpeningBracket = ReadOpeningBracket();
			
			if (tag.IsUnknownBang && !TryPeekWhiteSpace())
				OnSyntaxError(tag, tag.StartOffset, this.CurrentLocation, "Unknown tag");
			
			if (tag.IsStartOrEmptyTag || tag.IsEndTag || tag.IsProcessingInstruction) {
				// Read the name
				string name;
				if (TryReadName(out name)) {
					if (!IsValidName(name)) {
						OnSyntaxError(tag, this.CurrentLocation - name.Length, this.CurrentLocation, "The name '{0}' is invalid", name);
					}
				} else {
					OnSyntaxError(tag, "Element name expected");
				}
				tag.Name = name;
			} else {
				tag.Name = string.Empty;
			}
			
			bool isXmlDeclr = tag.StartOffset == 0 && tag.Name == "xml";
			
			if (tag.IsStartOrEmptyTag || tag.IsEndTag || isXmlDeclr) {
				// Read attributes for the tag
				while(true) {					
					// Chech for all forbiden 'name' charcters first - see ReadName
					if (IsEndOfFile()) break;
					if (TryPeekWhiteSpace()) {
						tag.AddChildren(ReadText(TextType.WhiteSpace));
						continue;  // End of file might be next
					}
					if (TryPeek('<')) break;
					string endBr;
					int endBrStart = this.CurrentLocation; // Just peek
					if (TryReadClosingBracket(out endBr)) {  // End tag
						GoBack(endBrStart);
						break;
					}
					
					// We have "=\'\"" or name - read attribute
					AXmlAttribute attr = ReadAttribulte();
					tag.AddChild(attr);
					if (tag.IsEndTag)
						OnSyntaxError(tag, attr.StartOffset, attr.EndOffset, "Attribute not allowed in end tag.");
				}
			} else if (tag.IsDocumentType) {
				tag.AddChildren(ReadContentOfDTD());
			} else {
				int start = this.CurrentLocation;
				IEnumerable<AXmlObject> text;
				if (tag.IsComment) {
					text = ReadText(TextType.Comment);
				} else if (tag.IsCData) {
					text = ReadText(TextType.CData);
				} else if (tag.IsProcessingInstruction) {
					text = ReadText(TextType.ProcessingInstruction);
				} else if (tag.IsUnknownBang) {
					text = ReadText(TextType.UnknownBang);
				} else {
					throw new InternalException(string.Format(CultureInfo.InvariantCulture, "Unknown opening bracket '{0}'", tag.OpeningBracket));
				}
				// Enumerate
				text = text.ToList();
				// Backtrack at complete start
				if (IsEndOfFile() || (tag.IsUnknownBang && TryPeek('<'))) {
					GoBack(start);
				} else {
					tag.AddChildren(text);
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
				if (tag.ClosingBracket != ">" && tag.ClosingBracket != "/>") OnSyntaxError(tag, brStart, brEnd, "'>' or '/>' expected");
			} else if (tag.IsEndTag) {
				if (tag.ClosingBracket != ">") OnSyntaxError(tag, brStart, brEnd, "'>' expected");
			} else if (tag.IsComment) {
				if (tag.ClosingBracket != "-->") OnSyntaxError(tag, brStart, brEnd, "'-->' expected");
			} else if (tag.IsCData) {
				if (tag.ClosingBracket != "]]>") OnSyntaxError(tag, brStart, brEnd, "']]>' expected");
			} else if (tag.IsProcessingInstruction) {
				if (tag.ClosingBracket != "?>") OnSyntaxError(tag, brStart, brEnd, "'?>' expected");
			} else if (tag.IsUnknownBang) {
				if (tag.ClosingBracket != ">") OnSyntaxError(tag, brStart, brEnd, "'>' expected");
			} else if (tag.IsDocumentType) {
				if (tag.ClosingBracket != ">") OnSyntaxError(tag, brStart, brEnd, "'>' expected");
			} else {
				throw new InternalException(string.Format(CultureInfo.InvariantCulture, "Unknown opening bracket '{0}'", tag.OpeningBracket));
			}
			
			// Attribute name may not apper multiple times
			var duplicates = tag.Children.OfType<AXmlAttribute>().GroupBy(attr => attr.Name).SelectMany(g => g.Skip(1));
			foreach(AXmlAttribute attr in duplicates) {
				OnSyntaxError(tag, attr.StartOffset, attr.EndOffset, "Attribute with name '{0}' already exists", attr.Name);
			}
			
			tag.EndOffset = this.CurrentLocation;
			
			OnParsed(tag);
			return tag;
		}
		
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
						foreach(string dtdName in AXmlTag.DtdNames) {
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
		
		IEnumerable<AXmlObject> ReadContentOfDTD()
		{
			int start = this.CurrentLocation;
			while(true) {
				if (IsEndOfFile()) break;            // End of file
				TryMoveToNonWhiteSpace();            // Skip whitespace
				if (TryRead('\'')) TryMoveTo('\'');  // Skip single quoted string TODO: Bug
				if (TryRead('\"')) TryMoveTo('\"');  // Skip single quoted string
				if (TryRead('[')) {                  // Start of nested infoset
					// Reading infoset
					while(true) {
						if (IsEndOfFile()) break;
						TryMoveToAnyOf('<', ']');
						if (TryPeek('<')) {
							if (start != this.CurrentLocation) {  // Two following tags
								yield return MakeText(start, this.CurrentLocation);
							}
							yield return ReadTag();
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
				yield return MakeText(start, this.CurrentLocation);
			}
		}
		
		/// <summary>
		/// Context: name or "=\'\""
		/// </summary>
		AXmlAttribute ReadAttribulte()
		{
			AssertHasMoreData();
			
			AXmlAttribute attr;
			if (TryReadFromCacheOrNew(out attr)) return attr;
			
			attr.StartOffset = this.CurrentLocation;
			
			// Read name
			string name;
			if (TryReadName(out name)) {
				if (!IsValidName(name)) {
					OnSyntaxError(attr, this.CurrentLocation - name.Length, this.CurrentLocation, "The name '{0}' is invalid", name);
				}
			} else {
				OnSyntaxError(attr, "Attribute name expected");
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
				attr.EqualsSign = GetText(checkpoint, this.CurrentLocation);
			} else {
				GoBack(checkpoint);
				OnSyntaxError(attr, "'=' expected");
				attr.EqualsSign = string.Empty;
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
								OnSyntaxError(attr, "White space or end of tag expected");
							} else {
								OnSyntaxError(attr, "Quote {0} expected (or add whitespace after the following one)", quoteChar);
							}
						} else {
							OnSyntaxError(attr, "White space or end of tag expected");
						}
					}
				} else {
					// '<' or end of file
					GoBack(valueStart);
					ReadAttributeValue(quoteChar);
					OnSyntaxError(attr, "Quote {0} expected", quoteChar);
				}
			} else {
				startsWithQuote = false;
				int valueStart = this.CurrentLocation;
				ReadAttributeValue(null);
				TryRead('\"');
				TryRead('\'');
				if (valueStart == this.CurrentLocation) {
					OnSyntaxError(attr, "Attribute value expected");
				} else {
					OnSyntaxError(attr, valueStart, this.CurrentLocation, "Attribute value must be quoted");
				}
			}
			attr.QuotedValue = GetText(start, this.CurrentLocation);
			attr.Value = Unquote(attr.QuotedValue);
			attr.Value = Dereference(attr, attr.Value, startsWithQuote ? start + 1 : start);
			
			attr.EndOffset = this.CurrentLocation;
			
			OnParsed(attr);
			return attr;
		}
		
		/// <summary>
		/// Read everything up to quote (excluding), opening/closing tag or attribute signature
		/// </summary>
		void ReadAttributeValue(char? quote)
		{
			while(true) {
				if (IsEndOfFile()) return;
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
				string name;
				if (TryReadName(out name)) {
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
		
		AXmlText MakeText(int start, int end)
		{
			AXmlParser.DebugAssert(end > start, "Empty text");
			
			AXmlText text = new AXmlText() {
				StartOffset = start,
				EndOffset = end,
				EscapedValue = GetText(start, end),
				Type = TextType.Other
			};
			
			OnParsed(text);
			return text;
		}
		
		const int maxEntityLength = 16; // The longest build-in one is 10 ("&#1114111;")
		const int maxTextFragmentSize = 64;
		const int lookAheadLength = (3 * maxTextFragmentSize) / 2; // More so that we do not get small "what was inserted" fragments
		
		/// <summary>
		/// Reads text and optionaly separates it into fragments.
		/// It can also return empty set for no appropriate text input.
		/// Make sure you enumerate it only once
		/// </summary>
		IEnumerable<AXmlObject> ReadText(TextType type)
		{
			bool lookahead = false;
			while(true) {
				AXmlText text;
				if (TryReadFromCacheOrNew(out text, t => t.Type == type)) {
					// Cached text found
					yield return text;
					continue; // Read next fragment;  the method can handle "no text left"
				}
				text.Type = type;
				
				// Limit the reading to just a few characters
				// (the first character not to be read)
				int fragmentEnd = Math.Min(this.CurrentLocation + maxTextFragmentSize, this.InputLength);
				
				// Look if some futher text has been already processed and align so that
				// we hit that chache point.  It is expensive so it is off for the first run
				if (lookahead) {
					// Note: Must fit entity
					AXmlObject nextFragment = trackedSegments.GetCachedObject<AXmlText>(this.CurrentLocation + maxEntityLength, lookAheadLength - maxEntityLength, t => t.Type == type);
					if (nextFragment != null) {
						fragmentEnd = Math.Min(nextFragment.StartOffset, this.InputLength);
						AXmlParser.Log("Parsing only text ({0}-{1}) because later text was already processed", this.CurrentLocation, fragmentEnd);
					}
				}
				lookahead = true;
				
				text.StartOffset = this.CurrentLocation;
				int start = this.CurrentLocation;
				
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
								OnSyntaxError(text, this.CurrentLocation, this.CurrentLocation + 3, "']]>' is not allowed in text");
							}
							TryMoveNext();
							continue;
						}
						throw new Exception("Infinite loop");
					}
				} else 	if (type == TextType.Comment) {
					// Do not report too many errors
					bool errorReported = false;
					while(true) {
						if (!TryMoveTo('-', fragmentEnd)) break; // End of fragment
						if (TryPeek("-->")) break;
						if (TryPeek("--") && !errorReported) {
							OnSyntaxError(text, this.CurrentLocation, this.CurrentLocation + 2, "'--' is not allowed in comment");
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
					throw new Exception("Uknown type " + type);
				}
				
				text.ContainsOnlyWhitespace = (wsEnd == this.CurrentLocation);
				
				// Terminal found or real end was reached;
				bool finished = this.CurrentLocation < fragmentEnd || IsEndOfFile();
				
				if (!finished) {
					// We have to continue reading more text fragments
					
					// If there is entity reference, make sure the next segment starts with it to prevent framentation
					int entitySearchStart = Math.Max(start + 1 /* data for us */, this.CurrentLocation - maxEntityLength);
					int entitySearchLength = this.CurrentLocation - entitySearchStart;
					if (entitySearchLength > 0) {
						// Note that LastIndexOf works backward
						int entityIndex = input.LastIndexOf('&', this.CurrentLocation - 1, entitySearchLength);
						if (entityIndex != -1) {
							GoBack(entityIndex);
						}
					}
				}
				
				text.EscapedValue = GetText(start, this.CurrentLocation);
				if (type == TextType.CharacterData) {
					// Normalize end of line first
					text.Value = Dereference(text, NormalizeEndOfLine(text.EscapedValue), start);
				} else {
					text.Value = text.EscapedValue;
				}
				text.EndOffset = this.CurrentLocation;
				
				if (text.EscapedValue.Length > 0) {
					OnParsed(text);
					yield return text;
				}
				
				if (finished) {
					yield break;
				}
			}
		}
		
		#region Helper methods
		
		void OnSyntaxError(AXmlObject obj, string message, params object[] args)
		{
			OnSyntaxError(obj, this.CurrentLocation, this.CurrentLocation + 1, message, args);
		}
		
		public static void OnSyntaxError(AXmlObject obj, int start, int end, string message, params object[] args)
		{
			if (end <= start) end = start + 1;
			string formattedMessage = string.Format(CultureInfo.InvariantCulture, message, args);
			AXmlParser.Log("Syntax error ({0}-{1}): {2}", start, end, formattedMessage);
			obj.AddSyntaxError(new SyntaxError() {
			                   	Object = obj,
			                   	StartOffset = start,
			                   	EndOffset = end,
			                   	Message = formattedMessage,
			                   });
		}
		
		static bool IsValidName(string name)
		{
			try {
				System.Xml.XmlConvert.VerifyName(name);
				return true;
			} catch (System.Xml.XmlException) {
				return false;
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
		
		static string NormalizeEndOfLine(string text)
		{
			return text.Replace("\r\n", "\n").Replace("\r", "\n");
		}
		
		string Dereference(AXmlObject owner, string text, int textLocation)
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
					OnSyntaxError(owner, errorLoc, errorLoc + 1, "Entity reference must be terminated with ';'");
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
					OnSyntaxError(owner, errorLoc + 1, errorLoc + 1, "Entity name expected");
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
							OnSyntaxError(owner, errorLoc + 3, errorLoc + 1 + name.Length, "Hexadecimal code of unicode character expected");
						}
					} else {
						if (!int.TryParse(name.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out num)) {
							num = -1;
							OnSyntaxError(owner, errorLoc + 2, errorLoc + 1 + name.Length, "Numeric code of unicode character expected");
						}
					}
					if (num != -1) {
						try {
							replacement = char.ConvertFromUtf32(num);
						} catch (ArgumentOutOfRangeException) {
							replacement = null;
							OnSyntaxError(owner, errorLoc + 2, errorLoc + 1 + name.Length, "Invalid unicode character U+{0:X} ({0})", num);
						}
					} else {
						replacement = null;
					}
				} else if (!IsValidName(name)) {
					replacement = null;
					OnSyntaxError(owner, errorLoc + 1, errorLoc + 1, "Invalid entity name");
				} else {
					replacement = null;
					if (parser.UnknownEntityReferenceIsError) {
						OnSyntaxError(owner, errorLoc, errorLoc + 1 + name.Length + 1, "Unknown entity reference '{0}'", name);
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
	}
}
