/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed partial class XmlParser {
		readonly string text;
		readonly XamlAttributeParser? xamlAttributeParser;
		readonly CodeBracesRangeFlags blockFlags;
		readonly List<ReferenceInfo> references;
		readonly List<CodeBracesRange> bracesInfo;
		readonly List<XmlNamespaceReference> xmlNamespaceReferences;
		readonly Dictionary<SubString, string> subStringDict;
		readonly List<int> lineSeparators;
		XmlNamespaces xmlNamespaces;
		int textPosition;
		int recursionCounter;
		const int MAX_RECURSION = 500;

		readonly struct ReferenceInfo {
			public Span Span { get; }
			public object Reference { get; }
			public bool IsDefinition { get; }
			public ReferenceInfo(Span span, object reference, bool isDefinition) {
				Span = span;
				Reference = reference;
				IsDefinition = isDefinition;
			}
		}

		enum TokenKind {
			EOF,
			Unknown,
			Comment,
			LessThan,
			GreaterThan,
			Colon,
			Equals,
			SingleQuoteString,
			DoubleQuoteString,
			ProcessingInstruction,
			Name,
			SlashGreaterThan,
			LessThanSlash,
		}

		readonly struct Token {
			public Span Span { get; }
			public TokenKind Kind { get; }
			public Token(Span span, TokenKind kind) {
				Span = span;
				Kind = kind;
			}
		}

		readonly struct NameToken {
			public bool HasNamespace => Namespace.Kind != TokenKind.EOF;
			public Span Span => HasNamespace ? Span.FromBounds(Namespace.Span.Start, Name.Span.End) : Name.Span;
			public Token FirstToken => HasNamespace ? Namespace : Name;
			public Token Namespace { get; }
			public Token Colon { get; }
			public Token Name { get; }

			public NameToken(in Token name) {
				Namespace = new Token(new Span(0, 0), TokenKind.EOF);
				Colon = new Token(new Span(0, 0), TokenKind.EOF);
				Name = name;
			}

			public NameToken(in Token @namespace, in Token colon, in Token name) {
				Namespace = @namespace;
				Colon = colon;
				Name = name;
			}
		}

		public XmlParser(string text, bool isXaml) {
			this.text = text ?? throw new ArgumentNullException(nameof(text));
			xamlAttributeParser = isXaml ? new XamlAttributeParser(this) : null;
			blockFlags = isXaml ? CodeBracesRangeFlags.XamlBlockBraces : CodeBracesRangeFlags.XmlBlockBraces;
			references = new List<ReferenceInfo>();
			bracesInfo = new List<CodeBracesRange>();
			xmlNamespaceReferences = new List<XmlNamespaceReference>();
			subStringDict = new Dictionary<SubString, string>(EqualityComparer<SubString>.Default);
			lineSeparators = new List<int>();
			xmlNamespaces = new XmlNamespaces();
			xmlNamespaces.Initialize(null);
		}

		void SaveBraceInfo(Span span, int leftLength, int rightLength, CodeBracesRangeFlags flags) {
			if (span.Length < leftLength + rightLength)
				return;
			bracesInfo.Add(new CodeBracesRange(new TextSpan(span.Start, leftLength), new TextSpan(span.End - rightLength, rightLength), flags));
		}

		void SaveBraceInfo(Span left, Span right, CodeBracesRangeFlags flags) =>
			bracesInfo.Add(new CodeBracesRange(new TextSpan(left.Start, left.Length), new TextSpan(right.Start, right.Length), flags));

		void SaveComment(in Token token) =>
			SaveBraceInfo(token.Span, 4, 3, blockFlags);
		void SaveString(in Token token) =>
			SaveBraceInfo(token.Span, 1, 1, token.Kind == TokenKind.SingleQuoteString ? CodeBracesRangeFlags.SingleQuotes : CodeBracesRangeFlags.DoubleQuotes);
		void SaveProcessingInstruction(in Token token) =>
			SaveBraceInfo(token.Span, 2, 2, blockFlags);

		enum XmlNameReferenceKind {
			Tag,// or markup extension
			Attribute,// or property
			Resource,
			RelativeSource,
			Xmlns,
		}

		sealed class XmlNameTextViewerReference {
			readonly XmlNamespaceReference nsRef;
			readonly string name;
			readonly XmlNameReferenceKind refKind;

			public XmlNameTextViewerReference(XmlNamespaceReference nsRef, string name, XmlNameReferenceKind refKind) {
				this.nsRef = nsRef ?? throw new ArgumentNullException(nameof(nsRef));
				this.name = name ?? throw new ArgumentNullException(nameof(name));
				this.refKind = refKind;
			}

			public override bool Equals(object? obj) {
				var other = obj as XmlNameTextViewerReference;
				return !(other is null) && nsRef.Equals(other.nsRef) && name == other.name && refKind == other.refKind;
			}

			public override int GetHashCode() => nsRef.GetHashCode() ^ name.GetHashCode() ^ (int)refKind;
		}

		sealed class XmlNamespaceTextViewerReference {
			public XmlNamespaceReference XmlNamespaceReference { get; }
			public XmlNamespaceTextViewerReference(XmlNamespaceReference nsRef) => XmlNamespaceReference = nsRef ?? throw new ArgumentNullException(nameof(nsRef));
			public override bool Equals(object? obj) {
				var other = obj as XmlNamespaceTextViewerReference;
				return !(other is null) && XmlNamespaceReference.Equals(other.XmlNamespaceReference);
			}
			public override int GetHashCode() => XmlNamespaceReference.GetHashCode();
		}

		readonly struct SubString : IEquatable<SubString> {
			readonly string text;
			readonly int start;
			readonly int length;

			public SubString(string text, int start, int length) {
				this.text = text;
				this.start = start;
				this.length = length;
			}

			public bool Equals(SubString other) {
				var lengthLocal = length;
				if (lengthLocal != other.length)
					return false;
				var textLocal = text;
				var otherTextLocal = other.text;
				var startLocal = start;
				var otherStartLocal = other.start;
				for (int i = 0; i < lengthLocal; i++) {
					if (textLocal[startLocal + i] != otherTextLocal[otherStartLocal + i])
						return false;
				}
				return true;
			}

			public override bool Equals(object? obj) => obj is SubString && Equals((SubString)obj);

			public override int GetHashCode() {
				int h = 17;
				var textLocal = text;
				var startLocal = start;
				var lengthLocal = length;
				for (int i = 0; i < lengthLocal; i++)
					h = 23 * h + textLocal[startLocal + i];
				return h;
			}

			public override string ToString() => text.Substring(start, length);
		}

		string GetSubstring(Span span) {
			var key = new SubString(text, span.Start, span.Length);
			if (subStringDict.TryGetValue(key, out var s))
				return s;
			s = key.ToString();
			subStringDict[key] = s;
			return s;
		}

		void SaveDefinition(Span aliasSpan) {
			var nsRef = GetAttributeNamespaceReference(aliasSpan);
			var @ref = new XmlNamespaceTextViewerReference(nsRef);
			references.Add(new ReferenceInfo(aliasSpan, @ref, true));
		}

		void SaveReference(in NameToken name, XmlNameReferenceKind refKind, bool findDefsOnly) =>
			SaveReference(name.HasNamespace, name.Namespace.Span, name.Name.Span, refKind, findDefsOnly);

		void SaveReference(bool hasNamespace, Span namespaceSpan, Span nameSpan, XmlNameReferenceKind refKind, bool findDefsOnly) {
			var aliasSpan = hasNamespace ? namespaceSpan : new Span(0, 0);
			XmlNamespaceReference nsRef;
			if (findDefsOnly) {
				var alias = GetSubstring(aliasSpan);
				var def = xmlNamespaces.GetOrCreate(alias);
				nsRef = new XmlNamespaceReference(alias) { Definition = def };
			}
			else
				nsRef = GetAttributeNamespaceReference(aliasSpan);

			if (hasNamespace) {
				var @ref = new XmlNamespaceTextViewerReference(nsRef);
				references.Add(new ReferenceInfo(namespaceSpan, @ref, false));
			}

			var nameRef = new XmlNameTextViewerReference(nsRef, GetSubstring(nameSpan), refKind);
			references.Add(new ReferenceInfo(nameSpan, nameRef, false));
		}

		public void Parse() {
			for (;;) {
				var peekToken = PeekToken();
				ReadTagContents(true);
				var peekToken2 = PeekToken();
				if (peekToken2.Kind == TokenKind.EOF)
					break;
				Debug.Assert(peekToken.Span.Start != peekToken2.Span.Start);
				if (peekToken.Span.Start == peekToken2.Span.Start)
					GetNextToken();
			}
		}

		public void WriteTo(IDecompilerOutput output) {
#if DEBUG
			for (int i = 1; i < references.Count; i++) {
				if (references[i - 1].Span.End > references[i].Span.Start)
					throw new InvalidOperationException();
			}
#endif

			int outputStart = output.Length;
			int pos = 0;
			var textLocal = text;
			foreach (var info in references) {
				if (info.Span.Length == 0)
					continue;
				if (pos < info.Span.Start)
					output.Write(textLocal, pos, info.Span.Start - pos, BoxedTextColor.Text);
				var flags = DecompilerReferenceFlags.Local;
				if (info.IsDefinition)
					flags |= DecompilerReferenceFlags.Definition;
				output.Write(textLocal, info.Span.Start, info.Span.Length, info.Reference, flags, BoxedTextColor.Text);
				pos = info.Span.End;
			}
			if (pos < textLocal.Length)
				output.Write(textLocal, pos, textLocal.Length - pos, BoxedTextColor.Text);
			Debug.Assert(output.Length - outputStart == textLocal.Length);

			// Don't add a line separator after the last tag
			int end = lineSeparators.Count - 1;
			for (int i = 0; i < end; i++)
				output.AddLineSeparator(lineSeparators[i]);

			foreach (var info in bracesInfo)
				output.AddCodeBracesRange(info);
		}

		void ReadTagContents(bool isTopLevel) {
			if (++recursionCounter >= MAX_RECURSION) {
				textPosition = text.Length;
				// Remove a possible cached token
				GetNextToken();
				return;
			}

			try {
				for (;;) {
					var token = GetNextToken();
					switch (token.Kind) {
					case TokenKind.EOF:
						return;

					case TokenKind.Comment:
						SaveComment(token);
						break;

					case TokenKind.SingleQuoteString:
					case TokenKind.DoubleQuoteString:
						SaveString(token);
						break;

					case TokenKind.ProcessingInstruction:
						SaveProcessingInstruction(token);
						break;

					case TokenKind.LessThan:
						ReadTag(token);
						break;

					case TokenKind.LessThanSlash:
						if (!isTopLevel)
							Undo(token);
						return;

					case TokenKind.SlashGreaterThan:
					case TokenKind.Unknown:
					case TokenKind.Colon:
					case TokenKind.Equals:
					case TokenKind.Name:
					case TokenKind.GreaterThan:
						break;

					default:
						throw new InvalidOperationException();
					}
				}
			}
			finally {
				recursionCounter--;
			}
		}

		readonly List<XmlNamespaces> cachedXmlNamespaces = new List<XmlNamespaces>();
		XmlNamespaces GetCachedXmlNamespaces(XmlNamespaces previous) {
			XmlNamespaces newInst;
			if (cachedXmlNamespaces.Count == 0)
				newInst = new XmlNamespaces();
			else {
				int index = cachedXmlNamespaces.Count - 1;
				newInst = cachedXmlNamespaces[index];
				cachedXmlNamespaces.RemoveAt(index);
			}
			newInst.Initialize(previous);
			return newInst;
		}
		void FreeXmlNamespaces(XmlNamespaces xmlNamespaces) {
			xmlNamespaces.Clear();
			cachedXmlNamespaces.Add(xmlNamespaces);
		}

		void ReadTag(in Token lessThanToken) {
			var tagName = ReadNameToken();
			if (tagName is null)
				return;

			var oldXmlNamespaces = xmlNamespaces;
			try {
				xmlNamespaces = GetCachedXmlNamespaces(xmlNamespaces);
				Debug.Assert(xmlNamespaceReferences.Count == 0);
				SaveReference(tagName.Value, XmlNameReferenceKind.Tag, findDefsOnly: false);
				ReadAttributes();
				SaveXmlNamespaceReferences();

				var token = GetNextToken();
				int endTagPos = -1;
				switch (token.Kind) {
				case TokenKind.EOF:
					return;

				case TokenKind.SlashGreaterThan:
					SaveBraceInfo(Span.FromBounds(lessThanToken.Span.Start, tagName.Value.Span.End), token.Span, blockFlags);
					endTagPos = token.Span.Start;
					break;

				case TokenKind.GreaterThan:
					var firstGreaterThan = token;
					ReadTagContents(false);
					token = GetNextToken();
					Debug.Assert(token.Kind == TokenKind.EOF || token.Kind == TokenKind.LessThanSlash);
					if (token.Kind != TokenKind.LessThanSlash)
						return;
					var tagEndName = ReadNameToken();
					if (tagEndName is null)
						return;
					var greaterThanToken = GetNextToken();
					if (greaterThanToken.Kind != TokenKind.GreaterThan)
						return;
					SaveBraceInfo(Span.FromBounds(lessThanToken.Span.Start, tagName.Value.Span.End == firstGreaterThan.Span.Start ? firstGreaterThan.Span.End : tagName.Value.Span.End), Span.FromBounds(token.Span.Start, greaterThanToken.Span.End), blockFlags);
					SaveReference(tagEndName.Value, XmlNameReferenceKind.Tag, findDefsOnly: true);
					endTagPos = greaterThanToken.Span.Start;
					break;

				default:
					Debug.Assert(token.Kind == TokenKind.EOF);
					return;
				}
				if (endTagPos >= 0 && recursionCounter == 2)
					lineSeparators.Add(endTagPos);
			}
			finally {
				FreeXmlNamespaces(xmlNamespaces);
				xmlNamespaces = oldXmlNamespaces;
			}
		}

		sealed class XmlNamespaces {
			XmlNamespaces? previous;
			readonly Dictionary<string, XmlNamespaceDefinition> namespaces;

			public XmlNamespaces() => namespaces = new Dictionary<string, XmlNamespaceDefinition>(StringComparer.Ordinal);

			public void Clear() {
				previous = null;
				namespaces.Clear();
			}

			public void Initialize(XmlNamespaces? previous) =>
				this.previous = previous;

			public XmlNamespaceDefinition GetOrCreate(string xmlNsAlias) {
				XmlNamespaces? curr = this;
				XmlNamespaceDefinition? def;
				while (!(curr is null)) {
					if (curr.namespaces.TryGetValue(xmlNsAlias, out def))
						return def;
					curr = curr.previous;
				}
				def = new XmlNamespaceDefinition(xmlNsAlias, "???");
				namespaces.Add(xmlNsAlias, def);
				return def;
			}

			public void Add(XmlParser xmlParser, Span aliasSpan, Span quotedSpan) {
				var valueSpan = new Span(quotedSpan.Start + 1, quotedSpan.Length - 2);
				var def = new XmlNamespaceDefinition(xmlParser.GetSubstring(aliasSpan), xmlParser.GetSubstring(new Span(quotedSpan.Start + 1, quotedSpan.Length - 2)));
				if (!namespaces.ContainsKey(def.Alias))
					namespaces.Add(def.Alias, def);
			}
		}

		sealed class XmlNamespaceDefinition {
			public string Alias { get; }
			public string Name { get; }
			public XmlNamespaceDefinition(string alias, string name) {
				Alias = alias ?? throw new ArgumentNullException(nameof(alias));
				Name = name ?? throw new ArgumentNullException(nameof(name));
			}
			public override bool Equals(object? obj) {
				var other = obj as XmlNamespaceDefinition;
				return !(other is null) && Name == other.Name;
			}
			public override int GetHashCode() => Name.GetHashCode();
		}

		sealed class XmlNamespaceReference : IEquatable<XmlNamespaceReference> {
			public string Alias { get; }
			public XmlNamespaceDefinition? Definition { get; set; }
			public XmlNamespaceReference(string alias) => Alias = alias ?? throw new ArgumentNullException(nameof(alias));
			public bool Equals([AllowNull] XmlNamespaceReference other) => Equals(Definition, other?.Definition);
			public override bool Equals(object? obj) => obj is XmlNamespaceReference && Equals((XmlNamespaceReference)obj);
			public override int GetHashCode() => Definition?.GetHashCode() ?? 0;
		}

		XmlNamespaceReference GetAttributeNamespaceReference(Span aliasSpan) {
			var nsRef = TryGetAttributeNamespaceReference(aliasSpan);
			if (!(nsRef is null))
				return nsRef;
			var nsName = GetSubstring(aliasSpan);
			nsRef = new XmlNamespaceReference(nsName);
			xmlNamespaceReferences.Add(nsRef);
			return nsRef;
		}

		XmlNamespaceReference? TryGetAttributeNamespaceReference(Span aliasSpan) {
			foreach (var nsRef in xmlNamespaceReferences) {
				if (Equals(aliasSpan, nsRef.Alias))
					return nsRef;
			}
			return null;
		}

		void SaveXmlNamespaceReferences() {
			foreach (var nsRef in xmlNamespaceReferences)
				nsRef.Definition = xmlNamespaces.GetOrCreate(nsRef.Alias);
			xmlNamespaceReferences.Clear();
		}

		bool Equals(Span span, string name) {
			if (name.Length != span.Length)
				return false;
			var textLocal = text;
			int spanIndex = span.Start;
			for (int i = 0; i < name.Length; i++, spanIndex++) {
				if (textLocal[spanIndex] != name[i])
					return false;
			}
			return true;
		}

		void ReadAttributes() {
			for (;;) {
				var token = GetNextToken();
				if (token.Kind == TokenKind.EOF)
					break;
				switch (token.Kind) {
				case TokenKind.EOF:
					return;

				case TokenKind.SlashGreaterThan:
				case TokenKind.GreaterThan:
					Undo(token);
					return;

				case TokenKind.Name:
					Undo(token);
					var name = ReadNameToken()!.Value;// Undo() was called so force '!'
					var eq = GetNextToken();
					if (eq.Kind != TokenKind.Equals)
						break;
					var value = GetNextToken();
					if (value.Kind != TokenKind.SingleQuoteString && value.Kind != TokenKind.DoubleQuoteString)
						break;

					if (Equals(name.FirstToken.Span, "xmlns")) {
						if (name.HasNamespace) {
							SaveReference(false, new Span(0, 0), name.Namespace.Span, XmlNameReferenceKind.Xmlns, findDefsOnly: false);
							xmlNamespaces.Add(this, name.Name.Span, value.Span);
							SaveDefinition(name.Name.Span);
						}
						else {
							SaveReference(name, XmlNameReferenceKind.Xmlns, findDefsOnly: false);
							xmlNamespaces.Add(this, new Span(0, 0), value.Span);
						}
					}
					else {
						SaveReference(name, XmlNameReferenceKind.Attribute, findDefsOnly: false);
						if (!(xamlAttributeParser is null))
							ParseXamlString(new Span(value.Span.Start + 1, value.Span.Length - 2));
					}
					SaveString(value);
					break;

				case TokenKind.Unknown:
				case TokenKind.Comment:
				case TokenKind.LessThan:
				case TokenKind.LessThanSlash:
				case TokenKind.Colon:
				case TokenKind.Equals:
				case TokenKind.SingleQuoteString:
				case TokenKind.DoubleQuoteString:
				case TokenKind.ProcessingInstruction:
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		void ParseXamlString(Span span) {
			Debug2.Assert(!(xamlAttributeParser is null));

			// Absolute minimum is "{x}", but most likely it's longer
			if (span.Length <= 3)
				return;
			if (text[span.Start] != '{' || text[span.Start + 1] == '}')
				return;

			xamlAttributeParser.Parse(text, span);
		}

		NameToken? ReadNameToken() {
			var first = GetNextToken();
			if (first.Kind != TokenKind.Name)
				return null;
			var colon = GetNextToken();
			if (colon.Kind != TokenKind.Colon) {
				Undo(colon);
				return new NameToken(first);
			}
			var last = GetNextToken();
			if (last.Kind != TokenKind.Name) {
				Undo(last);
				return null;
			}
			return new NameToken(first, colon, last);
		}

		void Undo(in Token token) {
			Debug2.Assert(cachedToken is null);
			if (!(cachedToken is null))
				throw new InvalidOperationException();
			cachedToken = token;
		}

		Token PeekToken() {
			if (!(cachedToken is null))
				return cachedToken.Value;
			cachedToken = GetNextToken();
			return cachedToken.Value;
		}

		Token GetNextToken() {
			if (!(cachedToken is null)) {
				var token = cachedToken.Value;
				cachedToken = null;
				return token;
			}

			return ReadTokenCore();
		}
		Token? cachedToken;

		Token ReadTokenCore() {
			Debug2.Assert(cachedToken is null);

			SkipWhitespace();
			int startPos = textPosition;
			var c = NextChar();
			if (c < 0)
				return new Token(new Span(text.Length, 0), TokenKind.EOF);

			if (c == '<') {
				c = PeekChar();
				if (c == '/') {
					SkipChar();
					return new Token(new Span(startPos, textPosition - startPos), TokenKind.LessThanSlash);
				}
				if (c == '?') {
					SkipChar();
					return ReadProcessingInstruction(startPos);
				}
				if (c != '!')
					return new Token(new Span(startPos, textPosition - startPos), TokenKind.LessThan);
				SkipChar();
				c = PeekChar();
				if (c != '-')
					return new Token(new Span(startPos, textPosition - startPos), TokenKind.Unknown);
				SkipChar();
				c = PeekChar();
				if (c != '-')
					return new Token(new Span(startPos, textPosition - startPos), TokenKind.Unknown);
				SkipChar();
				return ReadComment(startPos);
			}
			if (c == '>')
				return new Token(new Span(startPos, 1), TokenKind.GreaterThan);
			if (c == ':')
				return new Token(new Span(startPos, 1), TokenKind.Colon);
			if (c == '=')
				return new Token(new Span(startPos, 1), TokenKind.Equals);
			if (c == '/') {
				c = PeekChar();
				if (c == '>') {
					SkipChar();
					return new Token(new Span(startPos, textPosition - startPos), TokenKind.SlashGreaterThan);
				}
				return new Token(new Span(startPos, textPosition - startPos), TokenKind.Unknown);
			}
			if (c == '\'' || c == '"')
				return ReadString(startPos, (char)c);
			if (IsNameStartChar((char)c))
				return ReadName(startPos);
			return new Token(new Span(startPos, textPosition - startPos), TokenKind.Unknown);
		}

		Token ReadName(int startPos) {
			for (;;) {
				var c = PeekChar();
				if (c < 0 || !IsNameChar((char)c))
					return new Token(new Span(startPos, textPosition - startPos), TokenKind.Name);
				SkipChar();
			}
		}

		// https://www.w3.org/TR/REC-xml/#d0e804
		bool IsNameStartChar(char c) =>
			//c == ':' ||
			('A' <= c && c <= 'Z') ||
			c == '_' ||
			('a' <= c && c <= 'z') ||
			(0xC0 <= c && c <= 0xD6) ||
			(0xD8 <= c && c <= 0xF6) ||
			(0xF8 <= c && c <= 0x02FF) ||
			(0x0370 <= c && c <= 0x037D) ||
			(0x037F <= c && c <= 0x1FFF) ||
			(0x200C <= c && c <= 0x200D) ||
			(0x2070 <= c && c <= 0x218F) ||
			(0x2C00 <= c && c <= 0x2FEF) ||
			(0x3001 <= c && c <= 0xD7FF) ||
			(0xF900 <= c && c <= 0xFDCF) ||
			(0xFDF0 <= c && c <= 0xFFFD);//#x10000-#xEFFFF

		bool IsNameChar(char c) =>
			IsNameStartChar(c) ||
			c == '-' ||
			c == '.' ||
			('0' <= c && c <= '9') ||
			c == 0xB7 ||
			(0x0300 <= c && c <= 0x036F) ||
			(0x203F <= c && c <= 0x2040);

		Token ReadString(int startPos, char stringChar) {
			for (;;) {
				int c = NextChar();
				if (c < 0)
					return new Token(new Span(startPos, textPosition - startPos), TokenKind.Unknown);
				if (c == stringChar)
					return new Token(new Span(startPos, textPosition - startPos), stringChar == '\'' ? TokenKind.SingleQuoteString : TokenKind.DoubleQuoteString);
			}
		}

		Token ReadProcessingInstruction(int startPos) {
			// We've already read <?
			for (;;) {
				int c = NextChar();
				if (c < 0)
					break;
				if (c != '?')
					continue;
				c = NextChar();
				if (c < 0)
					break;
				if (c != '>')
					continue;

				break;
			}
			return new Token(new Span(startPos, textPosition - startPos), TokenKind.ProcessingInstruction);
		}

		Token ReadComment(int startPos) {
			// We've already read <!--
			for (;;) {
				int c = NextChar();
				if (c < 0)
					break;
				if (c != '-')
					continue;

				c = NextChar();
				if (c < 0)
					break;
				if (c != '-')
					continue;

				c = NextChar();
				if (c < 0)
					break;
				if (c != '>')
					continue;

				break;
			}
			return new Token(new Span(startPos, textPosition - startPos), TokenKind.Comment);
		}

		void SkipWhitespace() {
			for (;;) {
				var c = PeekChar();
				if (c < 0 || !char.IsWhiteSpace((char)c))
					break;
				SkipChar();
			}
		}

		int NextChar() {
			if (textPosition >= text.Length)
				return -1;
			return text[textPosition++];
		}

		int PeekChar() {
			if (textPosition >= text.Length)
				return -1;
			return text[textPosition];
		}

		void SkipChar() => textPosition++;
	}
}
