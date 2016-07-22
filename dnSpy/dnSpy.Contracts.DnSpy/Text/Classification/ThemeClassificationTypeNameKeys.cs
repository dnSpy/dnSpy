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

using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Classification type keys
	/// </summary>
	public static class ThemeClassificationTypeNameKeys {
		/// <summary>
		/// Identifier
		/// </summary>
		public const string Identifier = "Identifier";

		/// <summary>
		/// Literal
		/// </summary>
		public const string Literal = "Literal";

		/// <summary>
		/// <see cref="ColorType.Text"/>
		/// </summary>
		public const string Text = RoslynClassificationTypeNames.Text;

		/// <summary>
		/// <see cref="ColorType.Operator"/>
		/// </summary>
		public const string Operator = "Operator";

		/// <summary>
		/// <see cref="ColorType.Punctuation"/>
		/// </summary>
		public const string Punctuation = RoslynClassificationTypeNames.Punctuation;

		/// <summary>
		/// <see cref="ColorType.Number"/>
		/// </summary>
		public const string Number = "Number";

		/// <summary>
		/// <see cref="ColorType.Comment"/>
		/// </summary>
		public const string Comment = "Comment";

		/// <summary>
		/// <see cref="ColorType.Keyword"/>
		/// </summary>
		public const string Keyword = "Keyword";

		/// <summary>
		/// <see cref="ColorType.String"/>
		/// </summary>
		public const string String = "String";

		/// <summary>
		/// <see cref="ColorType.VerbatimString"/>
		/// </summary>
		public const string VerbatimString = RoslynClassificationTypeNames.VerbatimStringLiteral;

		/// <summary>
		/// <see cref="ColorType.Char"/>
		/// </summary>
		public const string Char = PredefinedClassificationTypeNames.Character;

		/// <summary>
		/// <see cref="ColorType.Namespace"/>
		/// </summary>
		public const string Namespace = "Namespace";

		/// <summary>
		/// <see cref="ColorType.Type"/>
		/// </summary>
		public const string Type = RoslynClassificationTypeNames.ClassName;

		/// <summary>
		/// <see cref="ColorType.SealedType"/>
		/// </summary>
		public const string SealedType = "SealedType";

		/// <summary>
		/// <see cref="ColorType.StaticType"/>
		/// </summary>
		public const string StaticType = "StaticType";

		/// <summary>
		/// <see cref="ColorType.Delegate"/>
		/// </summary>
		public const string Delegate = RoslynClassificationTypeNames.DelegateName;

		/// <summary>
		/// <see cref="ColorType.Enum"/>
		/// </summary>
		public const string Enum = RoslynClassificationTypeNames.EnumName;

		/// <summary>
		/// <see cref="ColorType.Interface"/>
		/// </summary>
		public const string Interface = RoslynClassificationTypeNames.InterfaceName;

		/// <summary>
		/// <see cref="ColorType.ValueType"/>
		/// </summary>
		public const string ValueType = RoslynClassificationTypeNames.StructName;

		/// <summary>
		/// <see cref="ColorType.TypeGenericParameter"/>
		/// </summary>
		public const string TypeGenericParameter = RoslynClassificationTypeNames.TypeParameterName;

		/// <summary>
		/// <see cref="ColorType.MethodGenericParameter"/>
		/// </summary>
		public const string MethodGenericParameter = "MethodGenericParameter";

		/// <summary>
		/// <see cref="ColorType.InstanceMethod"/>
		/// </summary>
		public const string InstanceMethod = "InstanceMethod";

		/// <summary>
		/// <see cref="ColorType.StaticMethod"/>
		/// </summary>
		public const string StaticMethod = "StaticMethod";

		/// <summary>
		/// <see cref="ColorType.ExtensionMethod"/>
		/// </summary>
		public const string ExtensionMethod = "ExtensionMethod";

		/// <summary>
		/// <see cref="ColorType.InstanceField"/>
		/// </summary>
		public const string InstanceField = "InstanceField";

		/// <summary>
		/// <see cref="ColorType.EnumField"/>
		/// </summary>
		public const string EnumField = "EnumField";

		/// <summary>
		/// <see cref="ColorType.LiteralField"/>
		/// </summary>
		public const string LiteralField = "LiteralField";

		/// <summary>
		/// <see cref="ColorType.StaticField"/>
		/// </summary>
		public const string StaticField = "StaticField";

		/// <summary>
		/// <see cref="ColorType.InstanceEvent"/>
		/// </summary>
		public const string InstanceEvent = "InstanceEvent";

		/// <summary>
		/// <see cref="ColorType.StaticEvent"/>
		/// </summary>
		public const string StaticEvent = "StaticEvent";

		/// <summary>
		/// <see cref="ColorType.InstanceProperty"/>
		/// </summary>
		public const string InstanceProperty = "InstanceProperty";

		/// <summary>
		/// <see cref="ColorType.StaticProperty"/>
		/// </summary>
		public const string StaticProperty = "StaticProperty";

		/// <summary>
		/// <see cref="ColorType.Local"/>
		/// </summary>
		public const string Local = "Local";

		/// <summary>
		/// <see cref="ColorType.Parameter"/>
		/// </summary>
		public const string Parameter = "Parameter";

		/// <summary>
		/// <see cref="ColorType.PreprocessorKeyword"/>
		/// </summary>
		public const string PreprocessorKeyword = "Preprocessor Keyword";

		/// <summary>
		/// <see cref="ColorType.PreprocessorText"/>
		/// </summary>
		public const string PreprocessorText = RoslynClassificationTypeNames.PreprocessorText;

		/// <summary>
		/// <see cref="ColorType.Label"/>
		/// </summary>
		public const string Label = "Label";

		/// <summary>
		/// <see cref="ColorType.OpCode"/>
		/// </summary>
		public const string OpCode = "OpCode";

		/// <summary>
		/// <see cref="ColorType.ILDirective"/>
		/// </summary>
		public const string ILDirective = "ILDirective";

		/// <summary>
		/// <see cref="ColorType.ILModule"/>
		/// </summary>
		public const string ILModule = "ILModule";

		/// <summary>
		/// <see cref="ColorType.ExcludedCode"/>
		/// </summary>
		public const string ExcludedCode = "Excluded Code";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentAttributeName"/>
		/// </summary>
		public const string XmlDocCommentAttributeName = RoslynClassificationTypeNames.XmlDocCommentAttributeName;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentAttributeQuotes"/>
		/// </summary>
		public const string XmlDocCommentAttributeQuotes = RoslynClassificationTypeNames.XmlDocCommentAttributeQuotes;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentAttributeValue"/>
		/// </summary>
		public const string XmlDocCommentAttributeValue = RoslynClassificationTypeNames.XmlDocCommentAttributeValue;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentCDataSection"/>
		/// </summary>
		public const string XmlDocCommentCDataSection = RoslynClassificationTypeNames.XmlDocCommentCDataSection;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentComment"/>
		/// </summary>
		public const string XmlDocCommentComment = RoslynClassificationTypeNames.XmlDocCommentComment;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentDelimiter"/>
		/// </summary>
		public const string XmlDocCommentDelimiter = RoslynClassificationTypeNames.XmlDocCommentDelimiter;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentEntityReference"/>
		/// </summary>
		public const string XmlDocCommentEntityReference = RoslynClassificationTypeNames.XmlDocCommentEntityReference;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentName"/>
		/// </summary>
		public const string XmlDocCommentName = RoslynClassificationTypeNames.XmlDocCommentName;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentProcessingInstruction"/>
		/// </summary>
		public const string XmlDocCommentProcessingInstruction = RoslynClassificationTypeNames.XmlDocCommentProcessingInstruction;

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentText"/>
		/// </summary>
		public const string XmlDocCommentText = RoslynClassificationTypeNames.XmlDocCommentText;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralAttributeName"/>
		/// </summary>
		public const string XmlLiteralAttributeName = RoslynClassificationTypeNames.XmlLiteralAttributeName;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralAttributeQuotes"/>
		/// </summary>
		public const string XmlLiteralAttributeQuotes = RoslynClassificationTypeNames.XmlLiteralAttributeQuotes;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralAttributeValue"/>
		/// </summary>
		public const string XmlLiteralAttributeValue = RoslynClassificationTypeNames.XmlLiteralAttributeValue;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralCDataSection"/>
		/// </summary>
		public const string XmlLiteralCDataSection = RoslynClassificationTypeNames.XmlLiteralCDataSection;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralComment"/>
		/// </summary>
		public const string XmlLiteralComment = RoslynClassificationTypeNames.XmlLiteralComment;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralDelimiter"/>
		/// </summary>
		public const string XmlLiteralDelimiter = RoslynClassificationTypeNames.XmlLiteralDelimiter;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralEmbeddedExpression"/>
		/// </summary>
		public const string XmlLiteralEmbeddedExpression = RoslynClassificationTypeNames.XmlLiteralEmbeddedExpression;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralEntityReference"/>
		/// </summary>
		public const string XmlLiteralEntityReference = RoslynClassificationTypeNames.XmlLiteralEntityReference;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralName"/>
		/// </summary>
		public const string XmlLiteralName = RoslynClassificationTypeNames.XmlLiteralName;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralProcessingInstruction"/>
		/// </summary>
		public const string XmlLiteralProcessingInstruction = RoslynClassificationTypeNames.XmlLiteralProcessingInstruction;

		/// <summary>
		/// <see cref="ColorType.XmlLiteralText"/>
		/// </summary>
		public const string XmlLiteralText = RoslynClassificationTypeNames.XmlLiteralText;

		/// <summary>
		/// <see cref="ColorType.XmlAttributeName"/>
		/// </summary>
		public const string XmlAttributeName = "XmlAttributeName";

		/// <summary>
		/// <see cref="ColorType.XmlAttributeQuotes"/>
		/// </summary>
		public const string XmlAttributeQuotes = "XmlAttributeQuotes";

		/// <summary>
		/// <see cref="ColorType.XmlAttributeValue"/>
		/// </summary>
		public const string XmlAttributeValue = "XmlAttributeValue";

		/// <summary>
		/// <see cref="ColorType.XmlCDataSection"/>
		/// </summary>
		public const string XmlCDataSection = "XmlCDataSection";

		/// <summary>
		/// <see cref="ColorType.XmlComment"/>
		/// </summary>
		public const string XmlComment = "XmlComment";

		/// <summary>
		/// <see cref="ColorType.XmlDelimiter"/>
		/// </summary>
		public const string XmlDelimiter = "XmlDelimiter";

		/// <summary>
		/// <see cref="ColorType.XmlKeyword"/>
		/// </summary>
		public const string XmlKeyword = "XmlKeyword";

		/// <summary>
		/// <see cref="ColorType.XmlName"/>
		/// </summary>
		public const string XmlName = "XmlName";

		/// <summary>
		/// <see cref="ColorType.XmlProcessingInstruction"/>
		/// </summary>
		public const string XmlProcessingInstruction = "XmlProcessingInstruction";

		/// <summary>
		/// <see cref="ColorType.XmlText"/>
		/// </summary>
		public const string XmlText = "XmlText";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipColon"/>
		/// </summary>
		public const string XmlDocToolTipColon = "XmlDocToolTipColon";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipExample"/>
		/// </summary>
		public const string XmlDocToolTipExample = "XmlDocToolTipExample";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipExceptionCref"/>
		/// </summary>
		public const string XmlDocToolTipExceptionCref = "XmlDocToolTipExceptionCref";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipReturns"/>
		/// </summary>
		public const string XmlDocToolTipReturns = "XmlDocToolTipReturns";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeCref"/>
		/// </summary>
		public const string XmlDocToolTipSeeCref = "XmlDocToolTipSeeCref";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeLangword"/>
		/// </summary>
		public const string XmlDocToolTipSeeLangword = "XmlDocToolTipSeeLangword";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeAlso"/>
		/// </summary>
		public const string XmlDocToolTipSeeAlso = "XmlDocToolTipSeeAlso";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeAlsoCref"/>
		/// </summary>
		public const string XmlDocToolTipSeeAlsoCref = "XmlDocToolTipSeeAlsoCref";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipParamRefName"/>
		/// </summary>
		public const string XmlDocToolTipParamRefName = "XmlDocToolTipParamRefName";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipParamName"/>
		/// </summary>
		public const string XmlDocToolTipParamName = "XmlDocToolTipParamName";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipTypeParamName"/>
		/// </summary>
		public const string XmlDocToolTipTypeParamName = "XmlDocToolTipTypeParamName";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipValue"/>
		/// </summary>
		public const string XmlDocToolTipValue = "XmlDocToolTipValue";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSummary"/>
		/// </summary>
		public const string XmlDocToolTipSummary = "XmlDocToolTipSummary";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipText"/>
		/// </summary>
		public const string XmlDocToolTipText = "XmlDocToolTipText";

		/// <summary>
		/// <see cref="ColorType.Assembly"/>
		/// </summary>
		public const string Assembly = "Assembly";

		/// <summary>
		/// <see cref="ColorType.AssemblyExe"/>
		/// </summary>
		public const string AssemblyExe = "AssemblyExe";

		/// <summary>
		/// <see cref="ColorType.Module"/>
		/// </summary>
		public const string Module = RoslynClassificationTypeNames.ModuleName;

		/// <summary>
		/// <see cref="ColorType.DirectoryPart"/>
		/// </summary>
		public const string DirectoryPart = "DirectoryPart";

		/// <summary>
		/// <see cref="ColorType.FileNameNoExtension"/>
		/// </summary>
		public const string FileNameNoExtension = "FileNameNoExtension";

		/// <summary>
		/// <see cref="ColorType.FileExtension"/>
		/// </summary>
		public const string FileExtension = "FileExtension";

		/// <summary>
		/// <see cref="ColorType.Error"/>
		/// </summary>
		public const string Error = "Error";

		/// <summary>
		/// <see cref="ColorType.ToStringEval"/>
		/// </summary>
		public const string ToStringEval = "ToStringEval";

		/// <summary>
		/// <see cref="ColorType.ReplPrompt1"/>
		/// </summary>
		public const string ReplPrompt1 = "ReplPrompt1";

		/// <summary>
		/// <see cref="ColorType.ReplPrompt2"/>
		/// </summary>
		public const string ReplPrompt2 = "ReplPrompt2";

		/// <summary>
		/// <see cref="ColorType.ReplOutputText"/>
		/// </summary>
		public const string ReplOutputText = "ReplOutputText";

		/// <summary>
		/// <see cref="ColorType.ReplScriptOutputText"/>
		/// </summary>
		public const string ReplScriptOutputText = "ReplScriptOutputText";

		/// <summary>
		/// <see cref="ColorType.Black"/>
		/// </summary>
		public const string Black = "Black";

		/// <summary>
		/// <see cref="ColorType.Blue"/>
		/// </summary>
		public const string Blue = "Blue";

		/// <summary>
		/// <see cref="ColorType.Cyan"/>
		/// </summary>
		public const string Cyan = "Cyan";

		/// <summary>
		/// <see cref="ColorType.DarkBlue"/>
		/// </summary>
		public const string DarkBlue = "DarkBlue";

		/// <summary>
		/// <see cref="ColorType.DarkCyan"/>
		/// </summary>
		public const string DarkCyan = "DarkCyan";

		/// <summary>
		/// <see cref="ColorType.DarkGray"/>
		/// </summary>
		public const string DarkGray = "DarkGray";

		/// <summary>
		/// <see cref="ColorType.DarkGreen"/>
		/// </summary>
		public const string DarkGreen = "DarkGreen";

		/// <summary>
		/// <see cref="ColorType.DarkMagenta"/>
		/// </summary>
		public const string DarkMagenta = "DarkMagenta";

		/// <summary>
		/// <see cref="ColorType.DarkRed"/>
		/// </summary>
		public const string DarkRed = "DarkRed";

		/// <summary>
		/// <see cref="ColorType.DarkYellow"/>
		/// </summary>
		public const string DarkYellow = "DarkYellow";

		/// <summary>
		/// <see cref="ColorType.Gray"/>
		/// </summary>
		public const string Gray = "Gray";

		/// <summary>
		/// <see cref="ColorType.Green"/>
		/// </summary>
		public const string Green = "Green";

		/// <summary>
		/// <see cref="ColorType.Magenta"/>
		/// </summary>
		public const string Magenta = "Magenta";

		/// <summary>
		/// <see cref="ColorType.Red"/>
		/// </summary>
		public const string Red = "Red";

		/// <summary>
		/// <see cref="ColorType.White"/>
		/// </summary>
		public const string White = "White";

		/// <summary>
		/// <see cref="ColorType.Yellow"/>
		/// </summary>
		public const string Yellow = "Yellow";

		/// <summary>
		/// <see cref="ColorType.InvBlack"/>
		/// </summary>
		public const string InvBlack = "InvBlack";

		/// <summary>
		/// <see cref="ColorType.InvBlue"/>
		/// </summary>
		public const string InvBlue = "InvBlue";

		/// <summary>
		/// <see cref="ColorType.InvCyan"/>
		/// </summary>
		public const string InvCyan = "InvCyan";

		/// <summary>
		/// <see cref="ColorType.InvDarkBlue"/>
		/// </summary>
		public const string InvDarkBlue = "InvDarkBlue";

		/// <summary>
		/// <see cref="ColorType.InvDarkCyan"/>
		/// </summary>
		public const string InvDarkCyan = "InvDarkCyan";

		/// <summary>
		/// <see cref="ColorType.InvDarkGray"/>
		/// </summary>
		public const string InvDarkGray = "InvDarkGray";

		/// <summary>
		/// <see cref="ColorType.InvDarkGreen"/>
		/// </summary>
		public const string InvDarkGreen = "InvDarkGreen";

		/// <summary>
		/// <see cref="ColorType.InvDarkMagenta"/>
		/// </summary>
		public const string InvDarkMagenta = "InvDarkMagenta";

		/// <summary>
		/// <see cref="ColorType.InvDarkRed"/>
		/// </summary>
		public const string InvDarkRed = "InvDarkRed";

		/// <summary>
		/// <see cref="ColorType.InvDarkYellow"/>
		/// </summary>
		public const string InvDarkYellow = "InvDarkYellow";

		/// <summary>
		/// <see cref="ColorType.InvGray"/>
		/// </summary>
		public const string InvGray = "InvGray";

		/// <summary>
		/// <see cref="ColorType.InvGreen"/>
		/// </summary>
		public const string InvGreen = "InvGreen";

		/// <summary>
		/// <see cref="ColorType.InvMagenta"/>
		/// </summary>
		public const string InvMagenta = "InvMagenta";

		/// <summary>
		/// <see cref="ColorType.InvRed"/>
		/// </summary>
		public const string InvRed = "InvRed";

		/// <summary>
		/// <see cref="ColorType.InvWhite"/>
		/// </summary>
		public const string InvWhite = "InvWhite";

		/// <summary>
		/// <see cref="ColorType.InvYellow"/>
		/// </summary>
		public const string InvYellow = "InvYellow";

		/// <summary>
		/// <see cref="ColorType.DebugLogExceptionHandled"/>
		/// </summary>
		public const string DebugLogExceptionHandled = "DebugLogExceptionHandled";

		/// <summary>
		/// <see cref="ColorType.DebugLogExceptionUnhandled"/>
		/// </summary>
		public const string DebugLogExceptionUnhandled = "DebugLogExceptionUnhandled";

		/// <summary>
		/// <see cref="ColorType.DebugLogStepFiltering"/>
		/// </summary>
		public const string DebugLogStepFiltering = "DebugLogStepFiltering";

		/// <summary>
		/// <see cref="ColorType.DebugLogLoadModule"/>
		/// </summary>
		public const string DebugLogLoadModule = "DebugLogLoadModule";

		/// <summary>
		/// <see cref="ColorType.DebugLogUnloadModule"/>
		/// </summary>
		public const string DebugLogUnloadModule = "DebugLogUnloadModule";

		/// <summary>
		/// <see cref="ColorType.DebugLogExitProcess"/>
		/// </summary>
		public const string DebugLogExitProcess = "DebugLogExitProcess";

		/// <summary>
		/// <see cref="ColorType.DebugLogExitThread"/>
		/// </summary>
		public const string DebugLogExitThread = "DebugLogExitThread";

		/// <summary>
		/// <see cref="ColorType.DebugLogProgramOutput"/>
		/// </summary>
		public const string DebugLogProgramOutput = "DebugLogProgramOutput";

		/// <summary>
		/// <see cref="ColorType.DebugLogMDA"/>
		/// </summary>
		public const string DebugLogMDA = "DebugLogMDA";

		/// <summary>
		/// <see cref="ColorType.DebugLogTimestamp"/>
		/// </summary>
		public const string DebugLogTimestamp = "DebugLogTimestamp";

		/// <summary>
		/// <see cref="ColorType.LineNumber"/>
		/// </summary>
		public const string LineNumber = "Line Number";

		/// <summary>
		/// <see cref="ColorType.Link"/>
		/// </summary>
		public const string Link = "Link";

		/// <summary>
		/// <see cref="ColorType.VisibleWhitespace"/>
		/// </summary>
		public const string VisibleWhitespace = "VisibleWhitespace";

		/// <summary>
		/// <see cref="ColorType.SelectedText"/>
		/// </summary>
		public const string SelectedText = "Selected Text";

		/// <summary>
		/// <see cref="ColorType.InactiveSelectedText"/>
		/// </summary>
		public const string InactiveSelectedText = "Inactive Selected Text";

		/// <summary>
		/// <see cref="ColorType.HighlightedReference"/>
		/// </summary>
		public const string HighlightedReference = "MarkerFormatDefinition/HighlightedReference";

		/// <summary>
		/// <see cref="ColorType.HighlightedWrittenReference"/>
		/// </summary>
		public const string HighlightedWrittenReference = "MarkerFormatDefinition/HighlightedWrittenReference";

		/// <summary>
		/// <see cref="ColorType.HighlightedDefinition"/>
		/// </summary>
		public const string HighlightedDefinition = "MarkerFormatDefinition/HighlightedDefinition";

		/// <summary>
		/// <see cref="ColorType.CurrentStatement"/>
		/// </summary>
		public const string CurrentStatement = "CurrentStatement";

		/// <summary>
		/// <see cref="ColorType.ReturnStatement"/>
		/// </summary>
		public const string ReturnStatement = "ReturnStatement";

		/// <summary>
		/// <see cref="ColorType.SelectedReturnStatement"/>
		/// </summary>
		public const string SelectedReturnStatement = "SelectedReturnStatement";

		/// <summary>
		/// <see cref="ColorType.BreakpointStatement"/>
		/// </summary>
		public const string BreakpointStatement = "BreakpointStatement";

		/// <summary>
		/// <see cref="ColorType.DisabledBreakpointStatement"/>
		/// </summary>
		public const string DisabledBreakpointStatement = "DisabledBreakpointStatement";

		/// <summary>
		/// <see cref="ColorType.SpecialCharacterBox"/>
		/// </summary>
		public const string SpecialCharacterBox = "SpecialCharacterBox";

		/// <summary>
		/// <see cref="ColorType.SearchResultMarker"/>
		/// </summary>
		public const string SearchResultMarker = "SearchResultMarker";

		/// <summary>
		/// <see cref="ColorType.CurrentLine"/>
		/// </summary>
		public const string CurrentLine = "CurrentLineActiveFormat";

		/// <summary>
		/// <see cref="ColorType.CurrentLineNoFocus"/>
		/// </summary>
		public const string CurrentLineNoFocus = "CurrentLineInactiveFormat";

		/// <summary>
		/// <see cref="ColorType.HexText"/>
		/// </summary>
		public const string HexText = "HexText";

		/// <summary>
		/// <see cref="ColorType.HexOffset"/>
		/// </summary>
		public const string HexOffset = "HexOffset";

		/// <summary>
		/// <see cref="ColorType.HexByte0"/>
		/// </summary>
		public const string HexByte0 = "HexByte0";

		/// <summary>
		/// <see cref="ColorType.HexByte1"/>
		/// </summary>
		public const string HexByte1 = "HexByte1";

		/// <summary>
		/// <see cref="ColorType.HexByteError"/>
		/// </summary>
		public const string HexByteError = "HexByteError";

		/// <summary>
		/// <see cref="ColorType.HexAscii"/>
		/// </summary>
		public const string HexAscii = "HexAscii";

		/// <summary>
		/// <see cref="ColorType.HexCaret"/>
		/// </summary>
		public const string HexCaret = "HexCaret";

		/// <summary>
		/// <see cref="ColorType.HexInactiveCaret"/>
		/// </summary>
		public const string HexInactiveCaret = "HexInactiveCaret";

		/// <summary>
		/// <see cref="ColorType.HexSelection"/>
		/// </summary>
		public const string HexSelection = "HexSelection";

		/// <summary>
		/// <see cref="ColorType.GlyphMargin"/>
		/// </summary>
		public const string GlyphMargin = "Indicator Margin";
	}
}
