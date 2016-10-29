/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class XmlParser {
		readonly string text;
		readonly List<ReferenceInfo> references;
		readonly List<CodeBracesRange> bracesInfo;
		readonly List<XmlNamespaceReference> xmlNamespaceReferences;
		XmlNamespaces xmlNamespaces;
		int textPosition;
		int recursionCounter;
		const int MAX_RECURSION = 500;

		struct ReferenceInfo {
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

		struct Token {
			public Span Span { get; }
			public TokenKind Kind { get; }
			public Token(Span span, TokenKind kind) {
				Span = span;
				Kind = kind;
			}
		}

		struct NameToken {
			public bool HasNamespace => Namespace.Kind != TokenKind.EOF;
			public Span Span => HasNamespace ? Span.FromBounds(Namespace.Span.Start, Name.Span.End) : Name.Span;
			public Token FirstToken => HasNamespace ? Namespace : Name;
			public Token Namespace { get; }
			public Token Colon { get; }
			public Token Name { get; }

			public NameToken(Token name) {
				Namespace = new Token(new Span(0, 0), TokenKind.EOF);
				Colon = new Token(new Span(0, 0), TokenKind.EOF);
				Name = name;
			}

			public NameToken(Token @namespace, Token colon, Token name) {
				Namespace = @namespace;
				Colon = colon;
				Name = name;
			}
		}

		public XmlParser(string text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			this.text = text;
			references = new List<ReferenceInfo>();
			bracesInfo = new List<CodeBracesRange>();
			xmlNamespaceReferences = new List<XmlNamespaceReference>();
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

		void SaveComment(Token token) =>
			SaveBraceInfo(token.Span, 4, 3, CodeBracesRangeFlags.OtherBlockBraces);
		void SaveString(Token token) =>
			SaveBraceInfo(token.Span, 1, 1, token.Kind == TokenKind.SingleQuoteString ? CodeBracesRangeFlags.SingleQuotes : CodeBracesRangeFlags.DoubleQuotes);
		void SaveProcessingInstruction(Token token) =>
			SaveBraceInfo(token.Span, 2, 2, CodeBracesRangeFlags.OtherBlockBraces);

		sealed class XmlTagTextViewerReference {
			readonly XmlNamespaceReference nsRef;
			readonly string name;

			public XmlTagTextViewerReference(XmlNamespaceReference nsRef, string name) {
				if (nsRef == null)
					throw new ArgumentNullException(nameof(nsRef));
				if (name == null)
					throw new ArgumentNullException(nameof(name));
				this.nsRef = nsRef;
				this.name = string.Intern(name);
			}

			public override bool Equals(object obj) {
				var other = obj as XmlTagTextViewerReference;
				return other != null && nsRef.Equals(other.nsRef) && name == other.name;
			}

			public override int GetHashCode() => nsRef.GetHashCode() ^ name.GetHashCode();
		}

		sealed class XmlNamespaceTextViewerReference {
			readonly XmlNamespaceReference nsRef;
			public XmlNamespaceTextViewerReference(XmlNamespaceReference nsRef) {
				if (nsRef == null)
					throw new ArgumentNullException(nameof(nsRef));
				this.nsRef = nsRef;
			}
			public override bool Equals(object obj) {
				var other = obj as XmlNamespaceTextViewerReference;
				return other != null && nsRef.Equals(other.nsRef);
			}
			public override int GetHashCode() => nsRef.GetHashCode();
		}

		void SaveDefinition(Span aliasSpan) {
			var nsRef = GetAttributeNamespaceReference(aliasSpan);
			var @ref = new XmlNamespaceTextViewerReference(nsRef);
			references.Add(new ReferenceInfo(aliasSpan, @ref, true));
		}

		string GetSubstring(Span span) => text.Substring(span.Start, span.Length);

		void SaveReference(NameToken name, bool findDefsOnly) {
			var aliasSpan = name.HasNamespace ? name.Namespace.Span : new Span(0, 0);
			XmlNamespaceReference nsRef;
			if (findDefsOnly) {
				var alias = string.Intern(GetSubstring(aliasSpan));
				var def = xmlNamespaces.GetOrCreate(alias);
				nsRef = new XmlNamespaceReference(alias) { Definition = def };
			}
			else
				nsRef = GetAttributeNamespaceReference(aliasSpan);

			if (name.HasNamespace) {
				var @ref = new XmlNamespaceTextViewerReference(nsRef);
				references.Add(new ReferenceInfo(name.Namespace.Span, @ref, false));
			}

			var tagRef = new XmlTagTextViewerReference(nsRef, GetSubstring(name.Name.Span));
			references.Add(new ReferenceInfo(name.Name.Span, tagRef, false));
		}

		public void Parse() {
			for (;;) {
				var peekToken = PeekToken();
				ReadTagContents(true);
				var peekToken2 = PeekToken();
				if (peekToken2.Kind == TokenKind.EOF)
					break;
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
			foreach (var info in references) {
				if (info.Span.Length == 0)
					continue;
				if (pos < info.Span.Start)
					output.Write(text, pos, info.Span.Start - pos, BoxedTextColor.Text);
				var refText = GetSubstring(info.Span);
				var flags = DecompilerReferenceFlags.Local;
				if (info.IsDefinition)
					flags |= DecompilerReferenceFlags.Definition;
				output.Write(refText, info.Reference, flags, BoxedTextColor.Text);
				pos = info.Span.End;
			}
			if (pos < text.Length)
				output.Write(text, pos, text.Length - pos, BoxedTextColor.Text);
			Debug.Assert(output.Length - outputStart == text.Length);

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

		void ReadTag(Token lessThanToken) {
			var tagName = ReadNameToken();
			if (tagName == null)
				return;

			var oldXmlNamespaces = xmlNamespaces;
			try {
				xmlNamespaces = GetCachedXmlNamespaces(xmlNamespaces);
				Debug.Assert(xmlNamespaceReferences.Count == 0);
				SaveReference(tagName.Value, false);
				ReadAttributes();
				SaveXmlNamespaceReferences();

				var token = GetNextToken();
				switch (token.Kind) {
				case TokenKind.EOF:
					return;

				case TokenKind.SlashGreaterThan:
					SaveBraceInfo(Span.FromBounds(lessThanToken.Span.Start, tagName.Value.Span.End), token.Span, CodeBracesRangeFlags.OtherBlockBraces);
					break;

				case TokenKind.GreaterThan:
					var firstGreaterThan = token;
					ReadTagContents(false);
					token = GetNextToken();
					Debug.Assert(token.Kind == TokenKind.EOF || token.Kind == TokenKind.LessThanSlash);
					if (token.Kind != TokenKind.LessThanSlash)
						return;
					var tagEndName = ReadNameToken();
					if (tagEndName == null)
						return;
					var greaterThanToken = GetNextToken();
					if (greaterThanToken.Kind != TokenKind.GreaterThan)
						return;
					SaveBraceInfo(Span.FromBounds(lessThanToken.Span.Start, tagName.Value.Span.End == firstGreaterThan.Span.Start ? firstGreaterThan.Span.End : tagName.Value.Span.End), Span.FromBounds(token.Span.Start, greaterThanToken.Span.End), CodeBracesRangeFlags.OtherBlockBraces);
					SaveReference(tagEndName.Value, true);
					break;

				default:
					Debug.Assert(token.Kind == TokenKind.EOF);
					return;
				}
			}
			finally {
				FreeXmlNamespaces(xmlNamespaces);
				xmlNamespaces = oldXmlNamespaces;
			}
		}

		sealed class XmlNamespaces {
			XmlNamespaces previous;
			readonly Dictionary<string, XmlNamespaceDefinition> namespaces;

			public XmlNamespaces() {
				namespaces = new Dictionary<string, XmlNamespaceDefinition>(StringComparer.Ordinal);
			}

			public void Clear() {
				previous = null;
				namespaces.Clear();
			}

			public void Initialize(XmlNamespaces previous) =>
				this.previous = previous;

			public XmlNamespaceDefinition GetOrCreate(string xmlNsAlias) {
				var curr = this;
				XmlNamespaceDefinition def;
				while (curr != null) {
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
				if (alias == null)
					throw new ArgumentNullException(nameof(alias));
				if (name == null)
					throw new ArgumentNullException(nameof(name));
				Alias = alias;
				Name = name;
			}
			public override bool Equals(object obj) {
				var other = obj as XmlNamespaceDefinition;
				return other != null && Name == other.Name;
			}
			public override int GetHashCode() => Name.GetHashCode();
		}

		sealed class XmlNamespaceReference : IEquatable<XmlNamespaceReference> {
			public string Alias { get; }
			public XmlNamespaceDefinition Definition { get; set; }
			public XmlNamespaceReference(string alias) {
				if (alias == null)
					throw new ArgumentNullException(nameof(alias));
				Alias = string.Intern(alias);
			}
			public bool Equals(XmlNamespaceReference other) => Equals(Definition, other.Definition);
			public override bool Equals(object obj) => obj is XmlNamespaceReference && Equals((XmlNamespaceReference)obj);
			public override int GetHashCode() => Definition?.GetHashCode() ?? 0;
		}

		XmlNamespaceReference GetAttributeNamespaceReference(Span aliasSpan) {
			var nsRef = TryGetAttributeNamespaceReference(aliasSpan);
			if (nsRef != null)
				return nsRef;
			var nsName = GetSubstring(aliasSpan);
			nsRef = new XmlNamespaceReference(nsName);
			xmlNamespaceReferences.Add(nsRef);
			return nsRef;
		}

		XmlNamespaceReference TryGetAttributeNamespaceReference(Span aliasSpan) {
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
					var name = ReadNameToken().Value;
					var eq = GetNextToken();
					if (eq.Kind != TokenKind.Equals)
						break;
					var value = GetNextToken();
					if (value.Kind != TokenKind.SingleQuoteString && value.Kind != TokenKind.DoubleQuoteString)
						break;

					if (Equals(name.FirstToken.Span, "xmlns")) {
						if (name.HasNamespace) {
							xmlNamespaces.Add(this, name.Name.Span, value.Span);
							SaveDefinition(name.Name.Span);
						}
						else
							xmlNamespaces.Add(this, new Span(0, 0), value.Span);
					}
					else
						SaveReference(name, false);
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

		void Undo(Token token) {
			Debug.Assert(cachedToken == null);
			if (cachedToken != null)
				throw new InvalidOperationException();
			cachedToken = token;
		}

		Token PeekToken() {
			if (cachedToken != null)
				return cachedToken.Value;
			cachedToken = GetNextToken();
			return cachedToken.Value;
		}

		Token GetNextToken() {
			if (cachedToken != null) {
				var token = cachedToken.Value;
				cachedToken = null;
				return token;
			}

			return ReadTokenCore();
		}
		Token? cachedToken;

		Token ReadTokenCore() {
			Debug.Assert(cachedToken == null);

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
			return new Token(new Span(startPos, 1), TokenKind.Unknown);
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
