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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Predefined classification type names
	/// </summary>
	public static class PredefinedClassificationTypeNames {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public const string Character = "character";
		public const string Comment = "comment";
		public const string ExcludedCode = "excluded code";
		public const string FormalLanguage = "formal language";
		public const string Identifier = "identifier";
		public const string Keyword = "keyword";
		public const string Literal = "literal";
		public const string NaturalLanguage = "natural language";
		public const string Number = "number";
		public const string Operator = "operator";
		public const string Other = "other";
		public const string PreprocessorKeyword = "preprocessor keyword";
		public const string String = "string";
		public const string WhiteSpace = "whitespace";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
