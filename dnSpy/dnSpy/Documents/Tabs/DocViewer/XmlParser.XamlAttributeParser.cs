/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed partial class XmlParser {
		sealed class XamlAttributeParser {
			readonly XmlParser owner;
			string text;
			int textPosition;
			int textEnd;
			const int MAX_RECURSION = 50;
			int recursionCounter;

			enum TokenKind {
				EOF,
				Unknown,
				OpenCurlyBrace,
				CloseCurlyBrace,
				Colon,
				Comma,
				Period,
				Equals,
				Name,
			}

			enum MarkupExtensionKind {
				Unknown,
				// {x:Type y:z} {x:Static y:z}
				TypeParam,
				// {Binding Prop}, {TemplateBinding Prop}
				PropertyParam,
				// {DynamicResource Name}, {StaticResource Name}
				ResourceParam,
				RelativeSource,
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

			public XamlAttributeParser(XmlParser owner) => this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

			public void Parse(string text, Span span) {
				this.text = text;
				textPosition = span.Start;
				textEnd = span.End;
				recursionCounter = 0;

				var token = GetNextToken();
				if (token.Kind == TokenKind.OpenCurlyBrace)
					ReadMarkupExtension(token);
			}

			void Error() {
				textPosition = textEnd;
				// Remove a possible cached token
				GetNextToken();
			}

			void SaveReference(NameToken name, XmlNameReferenceKind refKind) =>
				owner.SaveReference(name.HasNamespace, name.Namespace.Span, name.Name.Span, refKind, findDefsOnly: false);

			MarkupExtensionKind GetMarkupExtensionKind(NameToken name) {
				// This code assumes default namespaces are used
				if (name.HasNamespace) {
					if (owner.Equals(name.Namespace.Span, "x")) {
						if (owner.Equals(name.Name.Span, "Type"))
							return MarkupExtensionKind.TypeParam;
						if (owner.Equals(name.Name.Span, "Static"))
							return MarkupExtensionKind.TypeParam;
					}
				}
				else {
					if (owner.Equals(name.Name.Span, "Binding"))
						return MarkupExtensionKind.PropertyParam;
					if (owner.Equals(name.Name.Span, "TemplateBinding"))
						return MarkupExtensionKind.PropertyParam;
					if (owner.Equals(name.Name.Span, "DynamicResource"))
						return MarkupExtensionKind.ResourceParam;
					if (owner.Equals(name.Name.Span, "StaticResource"))
						return MarkupExtensionKind.ResourceParam;
					if (owner.Equals(name.Name.Span, "RelativeSource"))
						return MarkupExtensionKind.RelativeSource;
				}
				return MarkupExtensionKind.Unknown;
			}

			void ReadMarkupExtension(Token openCurlyBraceToken) {
				Debug.Assert(openCurlyBraceToken.Kind == TokenKind.OpenCurlyBrace);

				if (++recursionCounter > MAX_RECURSION) {
					Error();
					return;
				}

				try {
					var markupExtName = ReadNameToken();
					if (markupExtName == null) {
						Error();
						return;
					}
					SaveReference(markupExtName.Value, XmlNameReferenceKind.Tag);
					var markupKind = GetMarkupExtensionKind(markupExtName.Value);

					for (int counter = 0; ; counter++) {
						var token = GetNextToken();
						if (token.Kind == TokenKind.EOF)
							break;
						if (token.Kind == TokenKind.CloseCurlyBrace) {
							owner.SaveBraceInfo(openCurlyBraceToken.Span, token.Span, CodeBracesRangeFlags.OtherBlockBraces);
							break;
						}

						switch (token.Kind) {
						case TokenKind.OpenCurlyBrace:
							ReadMarkupExtension(token);
							break;

						case TokenKind.Name:
							Undo(token);
							var name = ReadNameToken().Value;

							SkipNamesAndPeriods();
							if (PeekToken().Kind == TokenKind.Equals) {
								GetNextToken();
								SkipNamesAndPeriods();
								SaveReference(name, XmlNameReferenceKind.Attribute);
							}
							else {
								switch (markupKind) {
								case MarkupExtensionKind.Unknown:
									if (name.HasNamespace)
										SaveReference(name, XmlNameReferenceKind.Tag);
									else
										SaveReference(name, XmlNameReferenceKind.Attribute);
									break;

								case MarkupExtensionKind.TypeParam:
									if (counter == 0)
										SaveReference(name, XmlNameReferenceKind.Tag);
									else
										SaveReference(name, XmlNameReferenceKind.Attribute);
									break;

								case MarkupExtensionKind.PropertyParam:
									SaveReference(name, XmlNameReferenceKind.Attribute);
									break;

								case MarkupExtensionKind.ResourceParam:
									if (counter == 0)
										SaveReference(name, XmlNameReferenceKind.Resource);
									else
										SaveReference(name, XmlNameReferenceKind.Attribute);
									break;

								case MarkupExtensionKind.RelativeSource:
									if (counter == 0)
										SaveReference(name, XmlNameReferenceKind.RelativeSource);
									else
										SaveReference(name, XmlNameReferenceKind.Attribute);
									break;

								default:
									throw new InvalidOperationException();
								}
							}
							break;
						}
					}
				}
				finally {
					recursionCounter--;
				}
			}

			void SkipNamesAndPeriods() {
				for (;;) {
					var token = PeekToken();
					if (token.Kind == TokenKind.Period)
						GetNextToken();
					else if (token.Kind == TokenKind.Name)
						ReadNameToken();
					else
						break;
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

				if (c == '{')
					return new Token(new Span(startPos, 1), TokenKind.OpenCurlyBrace);
				if (c == '}')
					return new Token(new Span(startPos, 1), TokenKind.CloseCurlyBrace);
				if (c == ':')
					return new Token(new Span(startPos, 1), TokenKind.Colon);
				if (c == ',')
					return new Token(new Span(startPos, 1), TokenKind.Comma);
				if (c == '.')
					return new Token(new Span(startPos, 1), TokenKind.Period);
				if (c == '=')
					return new Token(new Span(startPos, 1), TokenKind.Equals);
				if (IsNameStartChar((char)c))
					return ReadName(startPos);

				return new Token(new Span(startPos, textPosition - startPos), TokenKind.Unknown);
			}

			Token ReadName(int startPos) {
				for (;;) {
					int c = PeekChar();
					if (c < 0 || !IsNameChar((char)c))
						break;
					SkipChar();
				}
				return new Token(new Span(startPos, textPosition - startPos), TokenKind.Name);
			}

			bool IsNameStartChar(char c) => char.IsLetter(c) || c == '_';
			bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c == '_';

			void SkipWhitespace() {
				for (;;) {
					var c = PeekChar();
					if (c < 0 || !char.IsWhiteSpace((char)c))
						break;
					SkipChar();
				}
			}

			int NextChar() {
				if (textPosition >= textEnd)
					return -1;
				return text[textPosition++];
			}

			int PeekChar() {
				if (textPosition >= textEnd)
					return -1;
				return text[textPosition];
			}

			void SkipChar() => textPosition++;
		}
	}
}
