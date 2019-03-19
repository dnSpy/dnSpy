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
using System.Text;

namespace dnSpy.Contracts.ToolWindows.Search {
	sealed class SearchColumnCommandParser {
		readonly SearchColumnDefinition[] definitions;
		readonly List<Token> tokens;
		readonly List<SearchCommand> commands;
		readonly StringBuilder tokenizerStringBuilder;

		public SearchColumnCommandParser(SearchColumnDefinition[] definitions) {
			this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
			tokens = new List<Token>();
			commands = new List<SearchCommand>();
			tokenizerStringBuilder = new StringBuilder();
		}

		public SearchCommand[] GetSearchCommands(string searchText) {
			var tokenizer = new Tokenizer(tokenizerStringBuilder, definitions, searchText);
			commands.Clear();
			tokens.Clear();
			tokens.AddRange(tokenizer.GetTokens());

			for (int i = 0; i < tokens.Count; i++) {
				var token = tokens[i];
				switch (token.Kind) {
				case TokenKind.Text:
					commands.Add(new SearchCommand(columnId: null, searchText: token.Text, negate: false));
					break;

				case TokenKind.Command:
				case TokenKind.NegatedCommand:
					i++;
					if (i < tokens.Count) {
						var next = tokens[i];
						commands.Add(new SearchCommand(columnId: token.Text, searchText: next.Text, negate: token.Kind == TokenKind.NegatedCommand));
					}
					break;

				default: throw new InvalidOperationException();
				}
			}

			tokens.Clear();
			var res = commands.ToArray();
			commands.Clear();
			return res;
		}

		enum TokenKind {
			Text,
			Command,
			NegatedCommand,
		}

		readonly struct Token {
			public TokenKind Kind { get; }
			public string Text { get; }
			public Token(TokenKind kind, string text) {
				Kind = kind;
				Text = text ?? throw new ArgumentNullException(nameof(text));
			}
		}

		struct Tokenizer {
			readonly StringBuilder sb;
			readonly SearchColumnDefinition[] definitions;
			readonly string text;
			int currentPosition;

			public Tokenizer(StringBuilder sb, SearchColumnDefinition[] definitions, string text) {
				this.sb = sb ?? throw new ArgumentNullException(nameof(sb));
				this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
				this.text = text ?? throw new ArgumentNullException(nameof(text));
				currentPosition = 0;
			}

			public IEnumerable<Token> GetTokens() {
				for (;;) {
					var word = GetNextWord(out bool isText);
					if (word == null)
						break;
					if (isText)
						yield return new Token(TokenKind.Text, word);
					else if (TryGetDefinition(word, out var def, out bool negate))
						yield return new Token(negate ? TokenKind.NegatedCommand : TokenKind.Command, def.Id);
					else
						yield return new Token(TokenKind.Text, word);
				}
			}

			bool TryGetDefinition(string word, out SearchColumnDefinition def, out bool negate) {
				if (word.Length != 0 && word[0] == '-') {
					foreach (var d in definitions) {
						if (CompareShortOptionName(word, d.ShortOptionName, out negate)) {
							def = d;
							return true;
						}
					}
				}
				def = null;
				negate = false;
				return false;
			}

			bool CompareShortOptionName(string word, string shortOptionName, out bool negate) {
				negate = false;
				int wordLength = word.Length;
				if (wordLength > 0 && word[wordLength - 1] == '!') {
					negate = true;
					wordLength--;
				}
				if (wordLength != shortOptionName.Length + 1)
					return false;
				if (word[0] != '-')
					return false;
				for (int i = 0; i < shortOptionName.Length; i++) {
					if (word[i + 1] != shortOptionName[i])
						return false;
				}
				return true;
			}

			string GetNextWord(out bool isText) {
				isText = false;
				SkipWhitespace();
				var currentPositionLocal = currentPosition;
				var textLocal = text;
				if (currentPositionLocal == textLocal.Length)
					return null;

				var sbLocal = sb;
				sbLocal.Clear();

				var firstChar = textLocal[currentPositionLocal];
				bool needEndQuote = firstChar == '"';
				isText = needEndQuote || firstChar == '\\';
				if (needEndQuote)
					currentPositionLocal++;
				while (currentPositionLocal < textLocal.Length) {
					var c = textLocal[currentPositionLocal++];
					if (needEndQuote) {
						if (c == '"')
							break;
					}
					else if (char.IsWhiteSpace(c))
						break;
					if (c == '\\' && currentPositionLocal < textLocal.Length)
						c = textLocal[currentPositionLocal++];
					sbLocal.Append(c);
				}

				currentPosition = currentPositionLocal;
				return sbLocal.ToString();
			}

			void SkipWhitespace() {
				var currentPositionLocal = currentPosition;
				var textLocal = text;
				while (currentPositionLocal < textLocal.Length && char.IsWhiteSpace(textLocal[currentPositionLocal]))
					currentPositionLocal++;
				currentPosition = currentPositionLocal;
			}
		}
	}

	readonly struct SearchCommand {
		/// <summary>
		/// Column id (<see cref="SearchColumnDefinition.Id"/>) or null if it can match any column.
		/// </summary>
		public string ColumnId { get; }

		/// <summary>
		/// The text to search for in the column(s)
		/// </summary>
		public string SearchText { get; }

		/// <summary>
		/// true to negate the result, matching everything that doesn't match <see cref="SearchText"/>
		/// </summary>
		public bool Negate { get; }

		public SearchCommand(string columnId, string searchText, bool negate) {
			ColumnId = columnId;
			SearchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
			Negate = negate;
		}
	}
}
