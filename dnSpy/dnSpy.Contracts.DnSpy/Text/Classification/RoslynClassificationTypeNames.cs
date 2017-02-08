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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Roslyn classification type names
	/// </summary>
	public static class RoslynClassificationTypeNames {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string ClassName = "class name";
		public const string DelegateName = "delegate name";
		public const string EnumName = "enum name";
		public const string InterfaceName = "interface name";
		public const string ModuleName = "module name";
		public const string NumericLiteral = PredefinedClassificationTypeNames.Number;
		public const string PreprocessorText = "preprocessor text";
		public const string Punctuation = "punctuation";
		public const string StringLiteral = PredefinedClassificationTypeNames.String;
		public const string StructName = "struct name";
		public const string Text = "text";
		public const string TypeParameterName = "type parameter name";
		public const string VerbatimStringLiteral = "string - verbatim";
		public const string XmlDocCommentAttributeName = "xml doc comment - attribute name";
		public const string XmlDocCommentAttributeQuotes = "xml doc comment - attribute quotes";
		public const string XmlDocCommentAttributeValue = "xml doc comment - attribute value";
		public const string XmlDocCommentCDataSection = "xml doc comment - cdata section";
		public const string XmlDocCommentComment = "xml doc comment - comment";
		public const string XmlDocCommentDelimiter = "xml doc comment - delimiter";
		public const string XmlDocCommentEntityReference = "xml doc comment - entity reference";
		public const string XmlDocCommentName = "xml doc comment - name";
		public const string XmlDocCommentProcessingInstruction = "xml doc comment - processing instruction";
		public const string XmlDocCommentText = "xml doc comment - text";
		public const string XmlLiteralAttributeName = "xml literal - attribute name";
		public const string XmlLiteralAttributeQuotes = "xml literal - attribute quotes";
		public const string XmlLiteralAttributeValue = "xml literal - attribute value";
		public const string XmlLiteralCDataSection = "xml literal - cdata section";
		public const string XmlLiteralComment = "xml literal - comment";
		public const string XmlLiteralDelimiter = "xml literal - delimiter";
		public const string XmlLiteralEmbeddedExpression = "xml literal - embedded expression";
		public const string XmlLiteralEntityReference = "xml literal - entity reference";
		public const string XmlLiteralName = "xml literal - name";
		public const string XmlLiteralProcessingInstruction = "xml literal - processing instruction";
		public const string XmlLiteralText = "xml literal - text";
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
