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
		/// <see cref="TextColor.Text"/>
		/// </summary>
		public const string Text = RoslynClassificationTypeNames.Text;

		/// <summary>
		/// <see cref="TextColor.Operator"/>
		/// </summary>
		public const string Operator = "Operator";

		/// <summary>
		/// <see cref="TextColor.Punctuation"/>
		/// </summary>
		public const string Punctuation = RoslynClassificationTypeNames.Punctuation;

		/// <summary>
		/// <see cref="TextColor.Number"/>
		/// </summary>
		public const string Number = "Number";

		/// <summary>
		/// <see cref="TextColor.Comment"/>
		/// </summary>
		public const string Comment = "Comment";

		/// <summary>
		/// <see cref="TextColor.Keyword"/>
		/// </summary>
		public const string Keyword = "Keyword";

		/// <summary>
		/// <see cref="TextColor.String"/>
		/// </summary>
		public const string String = "String";

		/// <summary>
		/// <see cref="TextColor.VerbatimString"/>
		/// </summary>
		public const string VerbatimString = RoslynClassificationTypeNames.VerbatimStringLiteral;

		/// <summary>
		/// <see cref="TextColor.Char"/>
		/// </summary>
		public const string Char = PredefinedClassificationTypeNames.Character;

		/// <summary>
		/// <see cref="TextColor.Namespace"/>
		/// </summary>
		public const string Namespace = "Namespace";

		/// <summary>
		/// <see cref="TextColor.Type"/>
		/// </summary>
		public const string Type = RoslynClassificationTypeNames.ClassName;

		/// <summary>
		/// <see cref="TextColor.SealedType"/>
		/// </summary>
		public const string SealedType = "SealedType";

		/// <summary>
		/// <see cref="TextColor.StaticType"/>
		/// </summary>
		public const string StaticType = "StaticType";

		/// <summary>
		/// <see cref="TextColor.Delegate"/>
		/// </summary>
		public const string Delegate = RoslynClassificationTypeNames.DelegateName;

		/// <summary>
		/// <see cref="TextColor.Enum"/>
		/// </summary>
		public const string Enum = RoslynClassificationTypeNames.EnumName;

		/// <summary>
		/// <see cref="TextColor.Interface"/>
		/// </summary>
		public const string Interface = RoslynClassificationTypeNames.InterfaceName;

		/// <summary>
		/// <see cref="TextColor.ValueType"/>
		/// </summary>
		public const string ValueType = RoslynClassificationTypeNames.StructName;

		/// <summary>
		/// <see cref="TextColor.TypeGenericParameter"/>
		/// </summary>
		public const string TypeGenericParameter = RoslynClassificationTypeNames.TypeParameterName;

		/// <summary>
		/// <see cref="TextColor.MethodGenericParameter"/>
		/// </summary>
		public const string MethodGenericParameter = "MethodGenericParameter";

		/// <summary>
		/// <see cref="TextColor.InstanceMethod"/>
		/// </summary>
		public const string InstanceMethod = "InstanceMethod";

		/// <summary>
		/// <see cref="TextColor.StaticMethod"/>
		/// </summary>
		public const string StaticMethod = "StaticMethod";

		/// <summary>
		/// <see cref="TextColor.ExtensionMethod"/>
		/// </summary>
		public const string ExtensionMethod = "ExtensionMethod";

		/// <summary>
		/// <see cref="TextColor.InstanceField"/>
		/// </summary>
		public const string InstanceField = "InstanceField";

		/// <summary>
		/// <see cref="TextColor.EnumField"/>
		/// </summary>
		public const string EnumField = "EnumField";

		/// <summary>
		/// <see cref="TextColor.LiteralField"/>
		/// </summary>
		public const string LiteralField = "LiteralField";

		/// <summary>
		/// <see cref="TextColor.StaticField"/>
		/// </summary>
		public const string StaticField = "StaticField";

		/// <summary>
		/// <see cref="TextColor.InstanceEvent"/>
		/// </summary>
		public const string InstanceEvent = "InstanceEvent";

		/// <summary>
		/// <see cref="TextColor.StaticEvent"/>
		/// </summary>
		public const string StaticEvent = "StaticEvent";

		/// <summary>
		/// <see cref="TextColor.InstanceProperty"/>
		/// </summary>
		public const string InstanceProperty = "InstanceProperty";

		/// <summary>
		/// <see cref="TextColor.StaticProperty"/>
		/// </summary>
		public const string StaticProperty = "StaticProperty";

		/// <summary>
		/// <see cref="TextColor.Local"/>
		/// </summary>
		public const string Local = "Local";

		/// <summary>
		/// <see cref="TextColor.Parameter"/>
		/// </summary>
		public const string Parameter = "Parameter";

		/// <summary>
		/// <see cref="TextColor.PreprocessorKeyword"/>
		/// </summary>
		public const string PreprocessorKeyword = "Preprocessor Keyword";

		/// <summary>
		/// <see cref="TextColor.PreprocessorText"/>
		/// </summary>
		public const string PreprocessorText = RoslynClassificationTypeNames.PreprocessorText;

		/// <summary>
		/// <see cref="TextColor.Label"/>
		/// </summary>
		public const string Label = "Label";

		/// <summary>
		/// <see cref="TextColor.OpCode"/>
		/// </summary>
		public const string OpCode = "OpCode";

		/// <summary>
		/// <see cref="TextColor.ILDirective"/>
		/// </summary>
		public const string ILDirective = "ILDirective";

		/// <summary>
		/// <see cref="TextColor.ILModule"/>
		/// </summary>
		public const string ILModule = "ILModule";

		/// <summary>
		/// <see cref="TextColor.ExcludedCode"/>
		/// </summary>
		public const string ExcludedCode = "Excluded Code";

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentAttributeName"/>
		/// </summary>
		public const string XmlDocCommentAttributeName = RoslynClassificationTypeNames.XmlDocCommentAttributeName;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentAttributeQuotes"/>
		/// </summary>
		public const string XmlDocCommentAttributeQuotes = RoslynClassificationTypeNames.XmlDocCommentAttributeQuotes;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentAttributeValue"/>
		/// </summary>
		public const string XmlDocCommentAttributeValue = RoslynClassificationTypeNames.XmlDocCommentAttributeValue;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentCDataSection"/>
		/// </summary>
		public const string XmlDocCommentCDataSection = RoslynClassificationTypeNames.XmlDocCommentCDataSection;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentComment"/>
		/// </summary>
		public const string XmlDocCommentComment = RoslynClassificationTypeNames.XmlDocCommentComment;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentDelimiter"/>
		/// </summary>
		public const string XmlDocCommentDelimiter = RoslynClassificationTypeNames.XmlDocCommentDelimiter;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentEntityReference"/>
		/// </summary>
		public const string XmlDocCommentEntityReference = RoslynClassificationTypeNames.XmlDocCommentEntityReference;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentName"/>
		/// </summary>
		public const string XmlDocCommentName = RoslynClassificationTypeNames.XmlDocCommentName;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentProcessingInstruction"/>
		/// </summary>
		public const string XmlDocCommentProcessingInstruction = RoslynClassificationTypeNames.XmlDocCommentProcessingInstruction;

		/// <summary>
		/// <see cref="TextColor.XmlDocCommentText"/>
		/// </summary>
		public const string XmlDocCommentText = RoslynClassificationTypeNames.XmlDocCommentText;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralAttributeName"/>
		/// </summary>
		public const string XmlLiteralAttributeName = RoslynClassificationTypeNames.XmlLiteralAttributeName;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralAttributeQuotes"/>
		/// </summary>
		public const string XmlLiteralAttributeQuotes = RoslynClassificationTypeNames.XmlLiteralAttributeQuotes;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralAttributeValue"/>
		/// </summary>
		public const string XmlLiteralAttributeValue = RoslynClassificationTypeNames.XmlLiteralAttributeValue;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralCDataSection"/>
		/// </summary>
		public const string XmlLiteralCDataSection = RoslynClassificationTypeNames.XmlLiteralCDataSection;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralComment"/>
		/// </summary>
		public const string XmlLiteralComment = RoslynClassificationTypeNames.XmlLiteralComment;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralDelimiter"/>
		/// </summary>
		public const string XmlLiteralDelimiter = RoslynClassificationTypeNames.XmlLiteralDelimiter;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralEmbeddedExpression"/>
		/// </summary>
		public const string XmlLiteralEmbeddedExpression = RoslynClassificationTypeNames.XmlLiteralEmbeddedExpression;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralEntityReference"/>
		/// </summary>
		public const string XmlLiteralEntityReference = RoslynClassificationTypeNames.XmlLiteralEntityReference;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralName"/>
		/// </summary>
		public const string XmlLiteralName = RoslynClassificationTypeNames.XmlLiteralName;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralProcessingInstruction"/>
		/// </summary>
		public const string XmlLiteralProcessingInstruction = RoslynClassificationTypeNames.XmlLiteralProcessingInstruction;

		/// <summary>
		/// <see cref="TextColor.XmlLiteralText"/>
		/// </summary>
		public const string XmlLiteralText = RoslynClassificationTypeNames.XmlLiteralText;

		/// <summary>
		/// <see cref="TextColor.XmlAttributeName"/>
		/// </summary>
		public const string XmlAttributeName = "XmlAttributeName";

		/// <summary>
		/// <see cref="TextColor.XmlAttributeQuotes"/>
		/// </summary>
		public const string XmlAttributeQuotes = "XmlAttributeQuotes";

		/// <summary>
		/// <see cref="TextColor.XmlAttributeValue"/>
		/// </summary>
		public const string XmlAttributeValue = "XmlAttributeValue";

		/// <summary>
		/// <see cref="TextColor.XmlCDataSection"/>
		/// </summary>
		public const string XmlCDataSection = "XmlCDataSection";

		/// <summary>
		/// <see cref="TextColor.XmlComment"/>
		/// </summary>
		public const string XmlComment = "XmlComment";

		/// <summary>
		/// <see cref="TextColor.XmlDelimiter"/>
		/// </summary>
		public const string XmlDelimiter = "XmlDelimiter";

		/// <summary>
		/// <see cref="TextColor.XmlKeyword"/>
		/// </summary>
		public const string XmlKeyword = "XmlKeyword";

		/// <summary>
		/// <see cref="TextColor.XmlName"/>
		/// </summary>
		public const string XmlName = "XmlName";

		/// <summary>
		/// <see cref="TextColor.XmlProcessingInstruction"/>
		/// </summary>
		public const string XmlProcessingInstruction = "XmlProcessingInstruction";

		/// <summary>
		/// <see cref="TextColor.XmlText"/>
		/// </summary>
		public const string XmlText = "XmlText";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipColon"/>
		/// </summary>
		public const string XmlDocToolTipColon = "XmlDocToolTipColon";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipExample"/>
		/// </summary>
		public const string XmlDocToolTipExample = "XmlDocToolTipExample";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipExceptionCref"/>
		/// </summary>
		public const string XmlDocToolTipExceptionCref = "XmlDocToolTipExceptionCref";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipReturns"/>
		/// </summary>
		public const string XmlDocToolTipReturns = "XmlDocToolTipReturns";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipSeeCref"/>
		/// </summary>
		public const string XmlDocToolTipSeeCref = "XmlDocToolTipSeeCref";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipSeeLangword"/>
		/// </summary>
		public const string XmlDocToolTipSeeLangword = "XmlDocToolTipSeeLangword";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipSeeAlso"/>
		/// </summary>
		public const string XmlDocToolTipSeeAlso = "XmlDocToolTipSeeAlso";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipSeeAlsoCref"/>
		/// </summary>
		public const string XmlDocToolTipSeeAlsoCref = "XmlDocToolTipSeeAlsoCref";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipParamRefName"/>
		/// </summary>
		public const string XmlDocToolTipParamRefName = "XmlDocToolTipParamRefName";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipParamName"/>
		/// </summary>
		public const string XmlDocToolTipParamName = "XmlDocToolTipParamName";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipTypeParamName"/>
		/// </summary>
		public const string XmlDocToolTipTypeParamName = "XmlDocToolTipTypeParamName";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipValue"/>
		/// </summary>
		public const string XmlDocToolTipValue = "XmlDocToolTipValue";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipSummary"/>
		/// </summary>
		public const string XmlDocToolTipSummary = "XmlDocToolTipSummary";

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipText"/>
		/// </summary>
		public const string XmlDocToolTipText = "XmlDocToolTipText";

		/// <summary>
		/// <see cref="TextColor.Assembly"/>
		/// </summary>
		public const string Assembly = "Assembly";

		/// <summary>
		/// <see cref="TextColor.AssemblyExe"/>
		/// </summary>
		public const string AssemblyExe = "AssemblyExe";

		/// <summary>
		/// <see cref="TextColor.Module"/>
		/// </summary>
		public const string Module = RoslynClassificationTypeNames.ModuleName;

		/// <summary>
		/// <see cref="TextColor.DirectoryPart"/>
		/// </summary>
		public const string DirectoryPart = "DirectoryPart";

		/// <summary>
		/// <see cref="TextColor.FileNameNoExtension"/>
		/// </summary>
		public const string FileNameNoExtension = "FileNameNoExtension";

		/// <summary>
		/// <see cref="TextColor.FileExtension"/>
		/// </summary>
		public const string FileExtension = "FileExtension";

		/// <summary>
		/// <see cref="TextColor.Error"/>
		/// </summary>
		public const string Error = "Error";

		/// <summary>
		/// <see cref="TextColor.ToStringEval"/>
		/// </summary>
		public const string ToStringEval = "ToStringEval";

		/// <summary>
		/// <see cref="TextColor.ReplPrompt1"/>
		/// </summary>
		public const string ReplPrompt1 = "ReplPrompt1";

		/// <summary>
		/// <see cref="TextColor.ReplPrompt2"/>
		/// </summary>
		public const string ReplPrompt2 = "ReplPrompt2";

		/// <summary>
		/// <see cref="TextColor.ReplOutputText"/>
		/// </summary>
		public const string ReplOutputText = "ReplOutputText";

		/// <summary>
		/// <see cref="TextColor.ReplScriptOutputText"/>
		/// </summary>
		public const string ReplScriptOutputText = "ReplScriptOutputText";

		/// <summary>
		/// <see cref="TextColor.Black"/>
		/// </summary>
		public const string Black = "Black";

		/// <summary>
		/// <see cref="TextColor.Blue"/>
		/// </summary>
		public const string Blue = "Blue";

		/// <summary>
		/// <see cref="TextColor.Cyan"/>
		/// </summary>
		public const string Cyan = "Cyan";

		/// <summary>
		/// <see cref="TextColor.DarkBlue"/>
		/// </summary>
		public const string DarkBlue = "DarkBlue";

		/// <summary>
		/// <see cref="TextColor.DarkCyan"/>
		/// </summary>
		public const string DarkCyan = "DarkCyan";

		/// <summary>
		/// <see cref="TextColor.DarkGray"/>
		/// </summary>
		public const string DarkGray = "DarkGray";

		/// <summary>
		/// <see cref="TextColor.DarkGreen"/>
		/// </summary>
		public const string DarkGreen = "DarkGreen";

		/// <summary>
		/// <see cref="TextColor.DarkMagenta"/>
		/// </summary>
		public const string DarkMagenta = "DarkMagenta";

		/// <summary>
		/// <see cref="TextColor.DarkRed"/>
		/// </summary>
		public const string DarkRed = "DarkRed";

		/// <summary>
		/// <see cref="TextColor.DarkYellow"/>
		/// </summary>
		public const string DarkYellow = "DarkYellow";

		/// <summary>
		/// <see cref="TextColor.Gray"/>
		/// </summary>
		public const string Gray = "Gray";

		/// <summary>
		/// <see cref="TextColor.Green"/>
		/// </summary>
		public const string Green = "Green";

		/// <summary>
		/// <see cref="TextColor.Magenta"/>
		/// </summary>
		public const string Magenta = "Magenta";

		/// <summary>
		/// <see cref="TextColor.Red"/>
		/// </summary>
		public const string Red = "Red";

		/// <summary>
		/// <see cref="TextColor.White"/>
		/// </summary>
		public const string White = "White";

		/// <summary>
		/// <see cref="TextColor.Yellow"/>
		/// </summary>
		public const string Yellow = "Yellow";

		/// <summary>
		/// <see cref="TextColor.InvBlack"/>
		/// </summary>
		public const string InvBlack = "InvBlack";

		/// <summary>
		/// <see cref="TextColor.InvBlue"/>
		/// </summary>
		public const string InvBlue = "InvBlue";

		/// <summary>
		/// <see cref="TextColor.InvCyan"/>
		/// </summary>
		public const string InvCyan = "InvCyan";

		/// <summary>
		/// <see cref="TextColor.InvDarkBlue"/>
		/// </summary>
		public const string InvDarkBlue = "InvDarkBlue";

		/// <summary>
		/// <see cref="TextColor.InvDarkCyan"/>
		/// </summary>
		public const string InvDarkCyan = "InvDarkCyan";

		/// <summary>
		/// <see cref="TextColor.InvDarkGray"/>
		/// </summary>
		public const string InvDarkGray = "InvDarkGray";

		/// <summary>
		/// <see cref="TextColor.InvDarkGreen"/>
		/// </summary>
		public const string InvDarkGreen = "InvDarkGreen";

		/// <summary>
		/// <see cref="TextColor.InvDarkMagenta"/>
		/// </summary>
		public const string InvDarkMagenta = "InvDarkMagenta";

		/// <summary>
		/// <see cref="TextColor.InvDarkRed"/>
		/// </summary>
		public const string InvDarkRed = "InvDarkRed";

		/// <summary>
		/// <see cref="TextColor.InvDarkYellow"/>
		/// </summary>
		public const string InvDarkYellow = "InvDarkYellow";

		/// <summary>
		/// <see cref="TextColor.InvGray"/>
		/// </summary>
		public const string InvGray = "InvGray";

		/// <summary>
		/// <see cref="TextColor.InvGreen"/>
		/// </summary>
		public const string InvGreen = "InvGreen";

		/// <summary>
		/// <see cref="TextColor.InvMagenta"/>
		/// </summary>
		public const string InvMagenta = "InvMagenta";

		/// <summary>
		/// <see cref="TextColor.InvRed"/>
		/// </summary>
		public const string InvRed = "InvRed";

		/// <summary>
		/// <see cref="TextColor.InvWhite"/>
		/// </summary>
		public const string InvWhite = "InvWhite";

		/// <summary>
		/// <see cref="TextColor.InvYellow"/>
		/// </summary>
		public const string InvYellow = "InvYellow";

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionHandled"/>
		/// </summary>
		public const string DebugLogExceptionHandled = "DebugLogExceptionHandled";

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionUnhandled"/>
		/// </summary>
		public const string DebugLogExceptionUnhandled = "DebugLogExceptionUnhandled";

		/// <summary>
		/// <see cref="TextColor.DebugLogStepFiltering"/>
		/// </summary>
		public const string DebugLogStepFiltering = "DebugLogStepFiltering";

		/// <summary>
		/// <see cref="TextColor.DebugLogLoadModule"/>
		/// </summary>
		public const string DebugLogLoadModule = "DebugLogLoadModule";

		/// <summary>
		/// <see cref="TextColor.DebugLogUnloadModule"/>
		/// </summary>
		public const string DebugLogUnloadModule = "DebugLogUnloadModule";

		/// <summary>
		/// <see cref="TextColor.DebugLogExitProcess"/>
		/// </summary>
		public const string DebugLogExitProcess = "DebugLogExitProcess";

		/// <summary>
		/// <see cref="TextColor.DebugLogExitThread"/>
		/// </summary>
		public const string DebugLogExitThread = "DebugLogExitThread";

		/// <summary>
		/// <see cref="TextColor.DebugLogProgramOutput"/>
		/// </summary>
		public const string DebugLogProgramOutput = "DebugLogProgramOutput";

		/// <summary>
		/// <see cref="TextColor.DebugLogMDA"/>
		/// </summary>
		public const string DebugLogMDA = "DebugLogMDA";

		/// <summary>
		/// <see cref="TextColor.DebugLogTimestamp"/>
		/// </summary>
		public const string DebugLogTimestamp = "DebugLogTimestamp";

		/// <summary>
		/// <see cref="TextColor.LineNumber"/>
		/// </summary>
		public const string LineNumber = "Line Number";

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput1"/>
		/// </summary>
		public const string ReplLineNumberInput1 = "ReplLineNumberInput1";

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput2"/>
		/// </summary>
		public const string ReplLineNumberInput2 = "ReplLineNumberInput2";

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberOutput"/>
		/// </summary>
		public const string ReplLineNumberOutput = "ReplLineNumberOutput";

		/// <summary>
		/// <see cref="TextColor.Link"/>
		/// </summary>
		public const string Link = "Link";

		/// <summary>
		/// <see cref="TextColor.VisibleWhitespace"/>
		/// </summary>
		public const string VisibleWhitespace = "VisibleWhitespace";

		/// <summary>
		/// <see cref="TextColor.SelectedText"/>
		/// </summary>
		public const string SelectedText = "Selected Text";

		/// <summary>
		/// <see cref="TextColor.InactiveSelectedText"/>
		/// </summary>
		public const string InactiveSelectedText = "Inactive Selected Text";

		/// <summary>
		/// <see cref="TextColor.HighlightedReference"/>
		/// </summary>
		public const string HighlightedReference = "MarkerFormatDefinition/HighlightedReference";

		/// <summary>
		/// <see cref="TextColor.HighlightedWrittenReference"/>
		/// </summary>
		public const string HighlightedWrittenReference = "MarkerFormatDefinition/HighlightedWrittenReference";

		/// <summary>
		/// <see cref="TextColor.HighlightedDefinition"/>
		/// </summary>
		public const string HighlightedDefinition = "MarkerFormatDefinition/HighlightedDefinition";

		/// <summary>
		/// <see cref="TextColor.CurrentStatement"/>
		/// </summary>
		public const string CurrentStatement = "CurrentStatement";

		/// <summary>
		/// <see cref="TextColor.CurrentStatementMarker"/>
		/// </summary>
		public const string CurrentStatementMarker = "CurrentStatementMarker";

		/// <summary>
		/// <see cref="TextColor.CallReturn"/>
		/// </summary>
		public const string CallReturn = "CallReturn";

		/// <summary>
		/// <see cref="TextColor.CallReturnMarker"/>
		/// </summary>
		public const string CallReturnMarker = "CallReturnMarker";

		/// <summary>
		/// <see cref="TextColor.ActiveStatementMarker"/>
		/// </summary>
		public const string ActiveStatementMarker = "ActiveStatementMarker";

		/// <summary>
		/// <see cref="TextColor.BreakpointStatement"/>
		/// </summary>
		public const string BreakpointStatement = "BreakpointStatement";

		/// <summary>
		/// <see cref="TextColor.BreakpointStatementMarker"/>
		/// </summary>
		public const string BreakpointStatementMarker = "BreakpointStatementMarker";

		/// <summary>
		/// <see cref="TextColor.DisabledBreakpointStatementMarker"/>
		/// </summary>
		public const string DisabledBreakpointStatementMarker = "DisabledBreakpointStatementMarker";

		/// <summary>
		/// <see cref="TextColor.CurrentLine"/>
		/// </summary>
		public const string CurrentLine = "CurrentLineActiveFormat";

		/// <summary>
		/// <see cref="TextColor.CurrentLineNoFocus"/>
		/// </summary>
		public const string CurrentLineNoFocus = "CurrentLineInactiveFormat";

		/// <summary>
		/// <see cref="TextColor.HexText"/>
		/// </summary>
		public const string HexText = "HexText";

		/// <summary>
		/// <see cref="TextColor.HexOffset"/>
		/// </summary>
		public const string HexOffset = "HexOffset";

		/// <summary>
		/// <see cref="TextColor.HexByte0"/>
		/// </summary>
		public const string HexByte0 = "HexByte0";

		/// <summary>
		/// <see cref="TextColor.HexByte1"/>
		/// </summary>
		public const string HexByte1 = "HexByte1";

		/// <summary>
		/// <see cref="TextColor.HexByteError"/>
		/// </summary>
		public const string HexByteError = "HexByteError";

		/// <summary>
		/// <see cref="TextColor.HexAscii"/>
		/// </summary>
		public const string HexAscii = "HexAscii";

		/// <summary>
		/// <see cref="TextColor.HexCaret"/>
		/// </summary>
		public const string HexCaret = "HexCaret";

		/// <summary>
		/// <see cref="TextColor.HexInactiveCaret"/>
		/// </summary>
		public const string HexInactiveCaret = "HexInactiveCaret";

		/// <summary>
		/// <see cref="TextColor.HexSelection"/>
		/// </summary>
		public const string HexSelection = "HexSelection";

		/// <summary>
		/// <see cref="TextColor.GlyphMargin"/>
		/// </summary>
		public const string GlyphMargin = "Indicator Margin";

		/// <summary>
		/// <see cref="TextColor.BraceMatching"/>
		/// </summary>
		public const string BraceMatching = "brace matching";

		/// <summary>
		/// <see cref="TextColor.LineSeparator"/>
		/// </summary>
		public const string LineSeparator = "LineSeparator";

		/// <summary>
		/// <see cref="TextColor.FindMatchHighlightMarker"/>
		/// </summary>
		public const string FindMatchHighlightMarker = "FindMatchHighlightMarker";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerNamespace"/>
		/// </summary>
		public const string StructureVisualizerNamespace = "StructureVisualizerNamespace";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerType"/>
		/// </summary>
		public const string StructureVisualizerType = "StructureVisualizerType";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerValueType"/>
		/// </summary>
		public const string StructureVisualizerValueType = "StructureVisualizerValueType";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerInterface"/>
		/// </summary>
		public const string StructureVisualizerInterface = "StructureVisualizerInterface";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerMethod"/>
		/// </summary>
		public const string StructureVisualizerMethod = "StructureVisualizerMethod";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerAccessor"/>
		/// </summary>
		public const string StructureVisualizerAccessor = "StructureVisualizerAccessor";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerAnonymousMethod"/>
		/// </summary>
		public const string StructureVisualizerAnonymousMethod = "StructureVisualizerAnonymousMethod";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerConstructor"/>
		/// </summary>
		public const string StructureVisualizerConstructor = "StructureVisualizerConstructor";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerDestructor"/>
		/// </summary>
		public const string StructureVisualizerDestructor = "StructureVisualizerDestructor";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerOperator"/>
		/// </summary>
		public const string StructureVisualizerOperator = "StructureVisualizerOperator";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerConditional"/>
		/// </summary>
		public const string StructureVisualizerConditional = "StructureVisualizerConditional";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerLoop"/>
		/// </summary>
		public const string StructureVisualizerLoop = "StructureVisualizerLoop";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerProperty"/>
		/// </summary>
		public const string StructureVisualizerProperty = "StructureVisualizerProperty";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerEvent"/>
		/// </summary>
		public const string StructureVisualizerEvent = "StructureVisualizerEvent";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerTry"/>
		/// </summary>
		public const string StructureVisualizerTry = "StructureVisualizerTry";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerCatch"/>
		/// </summary>
		public const string StructureVisualizerCatch = "StructureVisualizerCatch";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFilter"/>
		/// </summary>
		public const string StructureVisualizerFilter = "StructureVisualizerFilter";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFinally"/>
		/// </summary>
		public const string StructureVisualizerFinally = "StructureVisualizerFinally";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFault"/>
		/// </summary>
		public const string StructureVisualizerFault = "StructureVisualizerFault";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerLock"/>
		/// </summary>
		public const string StructureVisualizerLock = "StructureVisualizerLock";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerUsing"/>
		/// </summary>
		public const string StructureVisualizerUsing = "StructureVisualizerUsing";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFixed"/>
		/// </summary>
		public const string StructureVisualizerFixed = "StructureVisualizerFixed";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerCase"/>
		/// </summary>
		public const string StructureVisualizerCase = "StructureVisualizerCase";

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerOther"/>
		/// </summary>
		public const string StructureVisualizerOther = "StructureVisualizerOther";
	}
}
