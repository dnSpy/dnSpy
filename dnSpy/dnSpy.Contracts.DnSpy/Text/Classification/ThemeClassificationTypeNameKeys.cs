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
	/// Classification type keys
	/// </summary>
	public static class ThemeClassificationTypeNameKeys {
		/// <summary>
		/// Identifier
		/// </summary>
		public const string Identifier = nameof(Identifier);

		/// <summary>
		/// Literal
		/// </summary>
		public const string Literal = nameof(Literal);

		/// <summary>
		/// <see cref="TextColor.Text"/>
		/// </summary>
		public const string Text = RoslynClassificationTypeNames.Text;

		/// <summary>
		/// <see cref="TextColor.Operator"/>
		/// </summary>
		public const string Operator = nameof(Operator);

		/// <summary>
		/// <see cref="TextColor.Punctuation"/>
		/// </summary>
		public const string Punctuation = RoslynClassificationTypeNames.Punctuation;

		/// <summary>
		/// <see cref="TextColor.Number"/>
		/// </summary>
		public const string Number = nameof(Number);

		/// <summary>
		/// <see cref="TextColor.Comment"/>
		/// </summary>
		public const string Comment = nameof(Comment);

		/// <summary>
		/// <see cref="TextColor.Keyword"/>
		/// </summary>
		public const string Keyword = nameof(Keyword);

		/// <summary>
		/// <see cref="TextColor.String"/>
		/// </summary>
		public const string String = nameof(String);

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
		public const string Namespace = nameof(Namespace);

		/// <summary>
		/// <see cref="TextColor.Type"/>
		/// </summary>
		public const string Type = RoslynClassificationTypeNames.ClassName;

		/// <summary>
		/// <see cref="TextColor.SealedType"/>
		/// </summary>
		public const string SealedType = nameof(SealedType);

		/// <summary>
		/// <see cref="TextColor.StaticType"/>
		/// </summary>
		public const string StaticType = nameof(StaticType);

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
		/// <see cref="TextColor.Module"/>
		/// </summary>
		public const string Module = RoslynClassificationTypeNames.ModuleName;

		/// <summary>
		/// <see cref="TextColor.TypeGenericParameter"/>
		/// </summary>
		public const string TypeGenericParameter = RoslynClassificationTypeNames.TypeParameterName;

		/// <summary>
		/// <see cref="TextColor.MethodGenericParameter"/>
		/// </summary>
		public const string MethodGenericParameter = nameof(MethodGenericParameter);

		/// <summary>
		/// <see cref="TextColor.InstanceMethod"/>
		/// </summary>
		public const string InstanceMethod = nameof(InstanceMethod);

		/// <summary>
		/// <see cref="TextColor.StaticMethod"/>
		/// </summary>
		public const string StaticMethod = nameof(StaticMethod);

		/// <summary>
		/// <see cref="TextColor.ExtensionMethod"/>
		/// </summary>
		public const string ExtensionMethod = nameof(ExtensionMethod);

		/// <summary>
		/// <see cref="TextColor.InstanceField"/>
		/// </summary>
		public const string InstanceField = nameof(InstanceField);

		/// <summary>
		/// <see cref="TextColor.EnumField"/>
		/// </summary>
		public const string EnumField = nameof(EnumField);

		/// <summary>
		/// <see cref="TextColor.LiteralField"/>
		/// </summary>
		public const string LiteralField = nameof(LiteralField);

		/// <summary>
		/// <see cref="TextColor.StaticField"/>
		/// </summary>
		public const string StaticField = nameof(StaticField);

		/// <summary>
		/// <see cref="TextColor.InstanceEvent"/>
		/// </summary>
		public const string InstanceEvent = nameof(InstanceEvent);

		/// <summary>
		/// <see cref="TextColor.StaticEvent"/>
		/// </summary>
		public const string StaticEvent = nameof(StaticEvent);

		/// <summary>
		/// <see cref="TextColor.InstanceProperty"/>
		/// </summary>
		public const string InstanceProperty = nameof(InstanceProperty);

		/// <summary>
		/// <see cref="TextColor.StaticProperty"/>
		/// </summary>
		public const string StaticProperty = nameof(StaticProperty);

		/// <summary>
		/// <see cref="TextColor.Local"/>
		/// </summary>
		public const string Local = nameof(Local);

		/// <summary>
		/// <see cref="TextColor.Parameter"/>
		/// </summary>
		public const string Parameter = nameof(Parameter);

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
		public const string Label = nameof(Label);

		/// <summary>
		/// <see cref="TextColor.OpCode"/>
		/// </summary>
		public const string OpCode = nameof(OpCode);

		/// <summary>
		/// <see cref="TextColor.ILDirective"/>
		/// </summary>
		public const string ILDirective = nameof(ILDirective);

		/// <summary>
		/// <see cref="TextColor.ILModule"/>
		/// </summary>
		public const string ILModule = nameof(ILModule);

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
		/// <see cref="TextColor.XmlAttribute"/>
		/// </summary>
		public const string XmlAttribute = nameof(XmlAttribute);

		/// <summary>
		/// <see cref="TextColor.XmlAttributeQuotes"/>
		/// </summary>
		public const string XmlAttributeQuotes = nameof(XmlAttributeQuotes);

		/// <summary>
		/// <see cref="TextColor.XmlAttributeValue"/>
		/// </summary>
		public const string XmlAttributeValue = nameof(XmlAttributeValue);

		/// <summary>
		/// <see cref="TextColor.XmlCDataSection"/>
		/// </summary>
		public const string XmlCDataSection = nameof(XmlCDataSection);

		/// <summary>
		/// <see cref="TextColor.XmlComment"/>
		/// </summary>
		public const string XmlComment = nameof(XmlComment);

		/// <summary>
		/// <see cref="TextColor.XmlDelimiter"/>
		/// </summary>
		public const string XmlDelimiter = nameof(XmlDelimiter);

		/// <summary>
		/// <see cref="TextColor.XmlKeyword"/>
		/// </summary>
		public const string XmlKeyword = nameof(XmlKeyword);

		/// <summary>
		/// <see cref="TextColor.XmlName"/>
		/// </summary>
		public const string XmlName = nameof(XmlName);

		/// <summary>
		/// <see cref="TextColor.XmlProcessingInstruction"/>
		/// </summary>
		public const string XmlProcessingInstruction = nameof(XmlProcessingInstruction);

		/// <summary>
		/// <see cref="TextColor.XmlText"/>
		/// </summary>
		public const string XmlText = nameof(XmlText);

		/// <summary>
		/// <see cref="TextColor.XamlAttribute"/>
		/// </summary>
		public const string XamlAttribute = nameof(XamlAttribute);

		/// <summary>
		/// <see cref="TextColor.XamlAttributeQuotes"/>
		/// </summary>
		public const string XamlAttributeQuotes = nameof(XamlAttributeQuotes);

		/// <summary>
		/// <see cref="TextColor.XamlAttributeValue"/>
		/// </summary>
		public const string XamlAttributeValue = nameof(XamlAttributeValue);

		/// <summary>
		/// <see cref="TextColor.XamlCDataSection"/>
		/// </summary>
		public const string XamlCDataSection = nameof(XamlCDataSection);

		/// <summary>
		/// <see cref="TextColor.XamlComment"/>
		/// </summary>
		public const string XamlComment = nameof(XamlComment);

		/// <summary>
		/// <see cref="TextColor.XamlDelimiter"/>
		/// </summary>
		public const string XamlDelimiter = nameof(XamlDelimiter);

		/// <summary>
		/// <see cref="TextColor.XamlKeyword"/>
		/// </summary>
		public const string XamlKeyword = nameof(XamlKeyword);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionClass"/>
		/// </summary>
		public const string XamlMarkupExtensionClass = nameof(XamlMarkupExtensionClass);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionParameterName"/>
		/// </summary>
		public const string XamlMarkupExtensionParameterName = nameof(XamlMarkupExtensionParameterName);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionParameterValue"/>
		/// </summary>
		public const string XamlMarkupExtensionParameterValue = nameof(XamlMarkupExtensionParameterValue);

		/// <summary>
		/// <see cref="TextColor.XamlName"/>
		/// </summary>
		public const string XamlName = nameof(XamlName);

		/// <summary>
		/// <see cref="TextColor.XamlProcessingInstruction"/>
		/// </summary>
		public const string XamlProcessingInstruction = nameof(XamlProcessingInstruction);

		/// <summary>
		/// <see cref="TextColor.XamlText"/>
		/// </summary>
		public const string XamlText = nameof(XamlText);

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipHeader"/>
		/// </summary>
		public const string XmlDocToolTipHeader = nameof(XmlDocToolTipHeader);

		/// <summary>
		/// <see cref="TextColor.Assembly"/>
		/// </summary>
		public const string Assembly = nameof(Assembly);

		/// <summary>
		/// <see cref="TextColor.AssemblyExe"/>
		/// </summary>
		public const string AssemblyExe = nameof(AssemblyExe);

		/// <summary>
		/// <see cref="TextColor.AssemblyModule"/>
		/// </summary>
		public const string AssemblyModule = nameof(AssemblyModule);

		/// <summary>
		/// <see cref="TextColor.DirectoryPart"/>
		/// </summary>
		public const string DirectoryPart = nameof(DirectoryPart);

		/// <summary>
		/// <see cref="TextColor.FileNameNoExtension"/>
		/// </summary>
		public const string FileNameNoExtension = nameof(FileNameNoExtension);

		/// <summary>
		/// <see cref="TextColor.FileExtension"/>
		/// </summary>
		public const string FileExtension = nameof(FileExtension);

		/// <summary>
		/// <see cref="TextColor.Error"/>
		/// </summary>
		public const string Error = nameof(Error);

		/// <summary>
		/// <see cref="TextColor.ToStringEval"/>
		/// </summary>
		public const string ToStringEval = nameof(ToStringEval);

		/// <summary>
		/// <see cref="TextColor.ReplPrompt1"/>
		/// </summary>
		public const string ReplPrompt1 = nameof(ReplPrompt1);

		/// <summary>
		/// <see cref="TextColor.ReplPrompt2"/>
		/// </summary>
		public const string ReplPrompt2 = nameof(ReplPrompt2);

		/// <summary>
		/// <see cref="TextColor.ReplOutputText"/>
		/// </summary>
		public const string ReplOutputText = nameof(ReplOutputText);

		/// <summary>
		/// <see cref="TextColor.ReplScriptOutputText"/>
		/// </summary>
		public const string ReplScriptOutputText = nameof(ReplScriptOutputText);

		/// <summary>
		/// <see cref="TextColor.Black"/>
		/// </summary>
		public const string Black = nameof(Black);

		/// <summary>
		/// <see cref="TextColor.Blue"/>
		/// </summary>
		public const string Blue = nameof(Blue);

		/// <summary>
		/// <see cref="TextColor.Cyan"/>
		/// </summary>
		public const string Cyan = nameof(Cyan);

		/// <summary>
		/// <see cref="TextColor.DarkBlue"/>
		/// </summary>
		public const string DarkBlue = nameof(DarkBlue);

		/// <summary>
		/// <see cref="TextColor.DarkCyan"/>
		/// </summary>
		public const string DarkCyan = nameof(DarkCyan);

		/// <summary>
		/// <see cref="TextColor.DarkGray"/>
		/// </summary>
		public const string DarkGray = nameof(DarkGray);

		/// <summary>
		/// <see cref="TextColor.DarkGreen"/>
		/// </summary>
		public const string DarkGreen = nameof(DarkGreen);

		/// <summary>
		/// <see cref="TextColor.DarkMagenta"/>
		/// </summary>
		public const string DarkMagenta = nameof(DarkMagenta);

		/// <summary>
		/// <see cref="TextColor.DarkRed"/>
		/// </summary>
		public const string DarkRed = nameof(DarkRed);

		/// <summary>
		/// <see cref="TextColor.DarkYellow"/>
		/// </summary>
		public const string DarkYellow = nameof(DarkYellow);

		/// <summary>
		/// <see cref="TextColor.Gray"/>
		/// </summary>
		public const string Gray = nameof(Gray);

		/// <summary>
		/// <see cref="TextColor.Green"/>
		/// </summary>
		public const string Green = nameof(Green);

		/// <summary>
		/// <see cref="TextColor.Magenta"/>
		/// </summary>
		public const string Magenta = nameof(Magenta);

		/// <summary>
		/// <see cref="TextColor.Red"/>
		/// </summary>
		public const string Red = nameof(Red);

		/// <summary>
		/// <see cref="TextColor.White"/>
		/// </summary>
		public const string White = nameof(White);

		/// <summary>
		/// <see cref="TextColor.Yellow"/>
		/// </summary>
		public const string Yellow = nameof(Yellow);

		/// <summary>
		/// <see cref="TextColor.InvBlack"/>
		/// </summary>
		public const string InvBlack = nameof(InvBlack);

		/// <summary>
		/// <see cref="TextColor.InvBlue"/>
		/// </summary>
		public const string InvBlue = nameof(InvBlue);

		/// <summary>
		/// <see cref="TextColor.InvCyan"/>
		/// </summary>
		public const string InvCyan = nameof(InvCyan);

		/// <summary>
		/// <see cref="TextColor.InvDarkBlue"/>
		/// </summary>
		public const string InvDarkBlue = nameof(InvDarkBlue);

		/// <summary>
		/// <see cref="TextColor.InvDarkCyan"/>
		/// </summary>
		public const string InvDarkCyan = nameof(InvDarkCyan);

		/// <summary>
		/// <see cref="TextColor.InvDarkGray"/>
		/// </summary>
		public const string InvDarkGray = nameof(InvDarkGray);

		/// <summary>
		/// <see cref="TextColor.InvDarkGreen"/>
		/// </summary>
		public const string InvDarkGreen = nameof(InvDarkGreen);

		/// <summary>
		/// <see cref="TextColor.InvDarkMagenta"/>
		/// </summary>
		public const string InvDarkMagenta = nameof(InvDarkMagenta);

		/// <summary>
		/// <see cref="TextColor.InvDarkRed"/>
		/// </summary>
		public const string InvDarkRed = nameof(InvDarkRed);

		/// <summary>
		/// <see cref="TextColor.InvDarkYellow"/>
		/// </summary>
		public const string InvDarkYellow = nameof(InvDarkYellow);

		/// <summary>
		/// <see cref="TextColor.InvGray"/>
		/// </summary>
		public const string InvGray = nameof(InvGray);

		/// <summary>
		/// <see cref="TextColor.InvGreen"/>
		/// </summary>
		public const string InvGreen = nameof(InvGreen);

		/// <summary>
		/// <see cref="TextColor.InvMagenta"/>
		/// </summary>
		public const string InvMagenta = nameof(InvMagenta);

		/// <summary>
		/// <see cref="TextColor.InvRed"/>
		/// </summary>
		public const string InvRed = nameof(InvRed);

		/// <summary>
		/// <see cref="TextColor.InvWhite"/>
		/// </summary>
		public const string InvWhite = nameof(InvWhite);

		/// <summary>
		/// <see cref="TextColor.InvYellow"/>
		/// </summary>
		public const string InvYellow = nameof(InvYellow);

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionHandled"/>
		/// </summary>
		public const string DebugLogExceptionHandled = nameof(DebugLogExceptionHandled);

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionUnhandled"/>
		/// </summary>
		public const string DebugLogExceptionUnhandled = nameof(DebugLogExceptionUnhandled);

		/// <summary>
		/// <see cref="TextColor.DebugLogStepFiltering"/>
		/// </summary>
		public const string DebugLogStepFiltering = nameof(DebugLogStepFiltering);

		/// <summary>
		/// <see cref="TextColor.DebugLogLoadModule"/>
		/// </summary>
		public const string DebugLogLoadModule = nameof(DebugLogLoadModule);

		/// <summary>
		/// <see cref="TextColor.DebugLogUnloadModule"/>
		/// </summary>
		public const string DebugLogUnloadModule = nameof(DebugLogUnloadModule);

		/// <summary>
		/// <see cref="TextColor.DebugLogExitProcess"/>
		/// </summary>
		public const string DebugLogExitProcess = nameof(DebugLogExitProcess);

		/// <summary>
		/// <see cref="TextColor.DebugLogExitThread"/>
		/// </summary>
		public const string DebugLogExitThread = nameof(DebugLogExitThread);

		/// <summary>
		/// <see cref="TextColor.DebugLogProgramOutput"/>
		/// </summary>
		public const string DebugLogProgramOutput = nameof(DebugLogProgramOutput);

		/// <summary>
		/// <see cref="TextColor.DebugLogMDA"/>
		/// </summary>
		public const string DebugLogMDA = nameof(DebugLogMDA);

		/// <summary>
		/// <see cref="TextColor.DebugLogTimestamp"/>
		/// </summary>
		public const string DebugLogTimestamp = nameof(DebugLogTimestamp);

		/// <summary>
		/// <see cref="TextColor.LineNumber"/>
		/// </summary>
		public const string LineNumber = "Line Number";

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput1"/>
		/// </summary>
		public const string ReplLineNumberInput1 = nameof(ReplLineNumberInput1);

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput2"/>
		/// </summary>
		public const string ReplLineNumberInput2 = nameof(ReplLineNumberInput2);

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberOutput"/>
		/// </summary>
		public const string ReplLineNumberOutput = nameof(ReplLineNumberOutput);

		/// <summary>
		/// <see cref="TextColor.VisibleWhitespace"/>
		/// </summary>
		public const string VisibleWhitespace = nameof(VisibleWhitespace);

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
		public const string CurrentStatement = nameof(CurrentStatement);

		/// <summary>
		/// <see cref="TextColor.CurrentStatementMarker"/>
		/// </summary>
		public const string CurrentStatementMarker = nameof(CurrentStatementMarker);

		/// <summary>
		/// <see cref="TextColor.CallReturn"/>
		/// </summary>
		public const string CallReturn = nameof(CallReturn);

		/// <summary>
		/// <see cref="TextColor.CallReturnMarker"/>
		/// </summary>
		public const string CallReturnMarker = nameof(CallReturnMarker);

		/// <summary>
		/// <see cref="TextColor.ActiveStatementMarker"/>
		/// </summary>
		public const string ActiveStatementMarker = nameof(ActiveStatementMarker);

		/// <summary>
		/// <see cref="TextColor.BreakpointStatement"/>
		/// </summary>
		public const string BreakpointStatement = nameof(BreakpointStatement);

		/// <summary>
		/// <see cref="TextColor.BreakpointStatementMarker"/>
		/// </summary>
		public const string BreakpointStatementMarker = nameof(BreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedBreakpointStatementMarker"/>
		/// </summary>
		public const string SelectedBreakpointStatementMarker = nameof(SelectedBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.DisabledBreakpointStatementMarker"/>
		/// </summary>
		public const string DisabledBreakpointStatementMarker = nameof(DisabledBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.CurrentLine"/>
		/// </summary>
		public const string CurrentLine = nameof(CurrentLine);

		/// <summary>
		/// <see cref="TextColor.CurrentLineNoFocus"/>
		/// </summary>
		public const string CurrentLineNoFocus = nameof(CurrentLineNoFocus);

		/// <summary>
		/// <see cref="TextColor.HexText"/>
		/// </summary>
		public const string HexText = nameof(HexText);

		/// <summary>
		/// <see cref="TextColor.HexOffset"/>
		/// </summary>
		public const string HexOffset = nameof(HexOffset);

		/// <summary>
		/// <see cref="TextColor.HexByte0"/>
		/// </summary>
		public const string HexByte0 = nameof(HexByte0);

		/// <summary>
		/// <see cref="TextColor.HexByte1"/>
		/// </summary>
		public const string HexByte1 = nameof(HexByte1);

		/// <summary>
		/// <see cref="TextColor.HexByteError"/>
		/// </summary>
		public const string HexByteError = nameof(HexByteError);

		/// <summary>
		/// <see cref="TextColor.HexAscii"/>
		/// </summary>
		public const string HexAscii = nameof(HexAscii);

		/// <summary>
		/// <see cref="TextColor.HexCaret"/>
		/// </summary>
		public const string HexCaret = nameof(HexCaret);

		/// <summary>
		/// <see cref="TextColor.HexInactiveCaret"/>
		/// </summary>
		public const string HexInactiveCaret = nameof(HexInactiveCaret);

		/// <summary>
		/// <see cref="TextColor.HexSelection"/>
		/// </summary>
		public const string HexSelection = nameof(HexSelection);

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
		public const string LineSeparator = nameof(LineSeparator);

		/// <summary>
		/// <see cref="TextColor.FindMatchHighlightMarker"/>
		/// </summary>
		public const string FindMatchHighlightMarker = nameof(FindMatchHighlightMarker);

		/// <summary>
		/// <see cref="TextColor.BlockStructureNamespace"/>
		/// </summary>
		public const string BlockStructureNamespace = nameof(BlockStructureNamespace);

		/// <summary>
		/// <see cref="TextColor.BlockStructureType"/>
		/// </summary>
		public const string BlockStructureType = nameof(BlockStructureType);

		/// <summary>
		/// <see cref="TextColor.BlockStructureModule"/>
		/// </summary>
		public const string BlockStructureModule = nameof(BlockStructureModule);

		/// <summary>
		/// <see cref="TextColor.BlockStructureValueType"/>
		/// </summary>
		public const string BlockStructureValueType = nameof(BlockStructureValueType);

		/// <summary>
		/// <see cref="TextColor.BlockStructureInterface"/>
		/// </summary>
		public const string BlockStructureInterface = nameof(BlockStructureInterface);

		/// <summary>
		/// <see cref="TextColor.BlockStructureMethod"/>
		/// </summary>
		public const string BlockStructureMethod = nameof(BlockStructureMethod);

		/// <summary>
		/// <see cref="TextColor.BlockStructureAccessor"/>
		/// </summary>
		public const string BlockStructureAccessor = nameof(BlockStructureAccessor);

		/// <summary>
		/// <see cref="TextColor.BlockStructureAnonymousMethod"/>
		/// </summary>
		public const string BlockStructureAnonymousMethod = nameof(BlockStructureAnonymousMethod);

		/// <summary>
		/// <see cref="TextColor.BlockStructureConstructor"/>
		/// </summary>
		public const string BlockStructureConstructor = nameof(BlockStructureConstructor);

		/// <summary>
		/// <see cref="TextColor.BlockStructureDestructor"/>
		/// </summary>
		public const string BlockStructureDestructor = nameof(BlockStructureDestructor);

		/// <summary>
		/// <see cref="TextColor.BlockStructureOperator"/>
		/// </summary>
		public const string BlockStructureOperator = nameof(BlockStructureOperator);

		/// <summary>
		/// <see cref="TextColor.BlockStructureConditional"/>
		/// </summary>
		public const string BlockStructureConditional = nameof(BlockStructureConditional);

		/// <summary>
		/// <see cref="TextColor.BlockStructureLoop"/>
		/// </summary>
		public const string BlockStructureLoop = nameof(BlockStructureLoop);

		/// <summary>
		/// <see cref="TextColor.BlockStructureProperty"/>
		/// </summary>
		public const string BlockStructureProperty = nameof(BlockStructureProperty);

		/// <summary>
		/// <see cref="TextColor.BlockStructureEvent"/>
		/// </summary>
		public const string BlockStructureEvent = nameof(BlockStructureEvent);

		/// <summary>
		/// <see cref="TextColor.BlockStructureTry"/>
		/// </summary>
		public const string BlockStructureTry = nameof(BlockStructureTry);

		/// <summary>
		/// <see cref="TextColor.BlockStructureCatch"/>
		/// </summary>
		public const string BlockStructureCatch = nameof(BlockStructureCatch);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFilter"/>
		/// </summary>
		public const string BlockStructureFilter = nameof(BlockStructureFilter);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFinally"/>
		/// </summary>
		public const string BlockStructureFinally = nameof(BlockStructureFinally);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFault"/>
		/// </summary>
		public const string BlockStructureFault = nameof(BlockStructureFault);

		/// <summary>
		/// <see cref="TextColor.BlockStructureLock"/>
		/// </summary>
		public const string BlockStructureLock = nameof(BlockStructureLock);

		/// <summary>
		/// <see cref="TextColor.BlockStructureUsing"/>
		/// </summary>
		public const string BlockStructureUsing = nameof(BlockStructureUsing);

		/// <summary>
		/// <see cref="TextColor.BlockStructureFixed"/>
		/// </summary>
		public const string BlockStructureFixed = nameof(BlockStructureFixed);

		/// <summary>
		/// <see cref="TextColor.BlockStructureSwitch"/>
		/// </summary>
		public const string BlockStructureSwitch = nameof(BlockStructureSwitch);

		/// <summary>
		/// <see cref="TextColor.BlockStructureCase"/>
		/// </summary>
		public const string BlockStructureCase = nameof(BlockStructureCase);

		/// <summary>
		/// <see cref="TextColor.BlockStructureLocalFunction"/>
		/// </summary>
		public const string BlockStructureLocalFunction = nameof(BlockStructureLocalFunction);

		/// <summary>
		/// <see cref="TextColor.BlockStructureOther"/>
		/// </summary>
		public const string BlockStructureOther = nameof(BlockStructureOther);

		/// <summary>
		/// <see cref="TextColor.BlockStructureXml"/>
		/// </summary>
		public const string BlockStructureXml = nameof(BlockStructureXml);

		/// <summary>
		/// <see cref="TextColor.BlockStructureXaml"/>
		/// </summary>
		public const string BlockStructureXaml = nameof(BlockStructureXaml);

		/// <summary>
		/// <see cref="TextColor.CompletionMatchHighlight"/>
		/// </summary>
		public const string CompletionMatchHighlight = nameof(CompletionMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.CompletionSuffix"/>
		/// </summary>
		public const string CompletionSuffix = nameof(CompletionSuffix);

		/// <summary>
		/// <see cref="TextColor.SignatureHelpDocumentation"/>
		/// </summary>
		public const string SignatureHelpDocumentation = "SigHelpDocumentationFormat";

		/// <summary>
		/// <see cref="TextColor.SignatureHelpCurrentParameter"/>
		/// </summary>
		public const string SignatureHelpCurrentParameter = "CurrentParameterFormat";

		/// <summary>
		/// <see cref="TextColor.SignatureHelpParameter"/>
		/// </summary>
		public const string SignatureHelpParameter = nameof(SignatureHelpParameter);

		/// <summary>
		/// <see cref="TextColor.SignatureHelpParameterDocumentation"/>
		/// </summary>
		public const string SignatureHelpParameterDocumentation = nameof(SignatureHelpParameterDocumentation);

		/// <summary>
		/// <see cref="TextColor.Url"/>
		/// </summary>
		public const string Url = "urlformat";

		/// <summary>
		/// <see cref="TextColor.HexPeDosHeader"/>
		/// </summary>
		public const string HexPeDosHeader = nameof(HexPeDosHeader);

		/// <summary>
		/// <see cref="TextColor.HexPeFileHeader"/>
		/// </summary>
		public const string HexPeFileHeader = nameof(HexPeFileHeader);

		/// <summary>
		/// <see cref="TextColor.HexPeOptionalHeader32"/>
		/// </summary>
		public const string HexPeOptionalHeader32 = nameof(HexPeOptionalHeader32);

		/// <summary>
		/// <see cref="TextColor.HexPeOptionalHeader64"/>
		/// </summary>
		public const string HexPeOptionalHeader64 = nameof(HexPeOptionalHeader64);

		/// <summary>
		/// <see cref="TextColor.HexPeSection"/>
		/// </summary>
		public const string HexPeSection = nameof(HexPeSection);

		/// <summary>
		/// <see cref="TextColor.HexPeSectionName"/>
		/// </summary>
		public const string HexPeSectionName = nameof(HexPeSectionName);

		/// <summary>
		/// <see cref="TextColor.HexCor20Header"/>
		/// </summary>
		public const string HexCor20Header = nameof(HexCor20Header);

		/// <summary>
		/// <see cref="TextColor.HexStorageSignature"/>
		/// </summary>
		public const string HexStorageSignature = nameof(HexStorageSignature);

		/// <summary>
		/// <see cref="TextColor.HexStorageHeader"/>
		/// </summary>
		public const string HexStorageHeader = nameof(HexStorageHeader);

		/// <summary>
		/// <see cref="TextColor.HexStorageStream"/>
		/// </summary>
		public const string HexStorageStream = nameof(HexStorageStream);

		/// <summary>
		/// <see cref="TextColor.HexStorageStreamName"/>
		/// </summary>
		public const string HexStorageStreamName = nameof(HexStorageStreamName);

		/// <summary>
		/// <see cref="TextColor.HexStorageStreamNameInvalid"/>
		/// </summary>
		public const string HexStorageStreamNameInvalid = nameof(HexStorageStreamNameInvalid);

		/// <summary>
		/// <see cref="TextColor.HexTablesStream"/>
		/// </summary>
		public const string HexTablesStream = nameof(HexTablesStream);

		/// <summary>
		/// <see cref="TextColor.HexTableName"/>
		/// </summary>
		public const string HexTableName = nameof(HexTableName);

		/// <summary>
		/// <see cref="TextColor.DocumentListMatchHighlight"/>
		/// </summary>
		public const string DocumentListMatchHighlight = nameof(DocumentListMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.GacMatchHighlight"/>
		/// </summary>
		public const string GacMatchHighlight = nameof(GacMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.AppSettingsTreeViewNodeMatchHighlight"/>
		/// </summary>
		public const string AppSettingsTreeViewNodeMatchHighlight = nameof(AppSettingsTreeViewNodeMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.AppSettingsTextMatchHighlight"/>
		/// </summary>
		public const string AppSettingsTextMatchHighlight = nameof(AppSettingsTextMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.HexCurrentLine"/>
		/// </summary>
		public const string HexCurrentLine = nameof(HexCurrentLine);

		/// <summary>
		/// <see cref="TextColor.HexCurrentLineNoFocus"/>
		/// </summary>
		public const string HexCurrentLineNoFocus = nameof(HexCurrentLineNoFocus);

		/// <summary>
		/// <see cref="TextColor.HexInactiveSelectedText"/>
		/// </summary>
		public const string HexInactiveSelectedText = nameof(HexInactiveSelectedText);

		/// <summary>
		/// <see cref="TextColor.HexColumnLine0"/>
		/// </summary>
		public const string HexColumnLine0 = nameof(HexColumnLine0);

		/// <summary>
		/// <see cref="TextColor.HexColumnLine1"/>
		/// </summary>
		public const string HexColumnLine1 = nameof(HexColumnLine1);

		/// <summary>
		/// <see cref="TextColor.HexColumnLineGroup0"/>
		/// </summary>
		public const string HexColumnLineGroup0 = nameof(HexColumnLineGroup0);

		/// <summary>
		/// <see cref="TextColor.HexColumnLineGroup1"/>
		/// </summary>
		public const string HexColumnLineGroup1 = nameof(HexColumnLineGroup1);

		/// <summary>
		/// <see cref="TextColor.HexHighlightedValuesColumn"/>
		/// </summary>
		public const string HexHighlightedValuesColumn = nameof(HexHighlightedValuesColumn);

		/// <summary>
		/// <see cref="TextColor.HexHighlightedAsciiColumn"/>
		/// </summary>
		public const string HexHighlightedAsciiColumn = nameof(HexHighlightedAsciiColumn);

		/// <summary>
		/// <see cref="TextColor.HexGlyphMargin"/>
		/// </summary>
		public const string HexGlyphMargin = nameof(HexGlyphMargin);

		/// <summary>
		/// <see cref="TextColor.HexCurrentValueCell"/>
		/// </summary>
		public const string HexCurrentValueCell = nameof(HexCurrentValueCell);

		/// <summary>
		/// <see cref="TextColor.HexCurrentAsciiCell"/>
		/// </summary>
		public const string HexCurrentAsciiCell = nameof(HexCurrentAsciiCell);

		/// <summary>
		/// <see cref="TextColor.OutputWindowText"/>
		/// </summary>
		public const string OutputWindowText = nameof(OutputWindowText);

		/// <summary>
		/// <see cref="TextColor.HexFindMatchHighlightMarker"/>
		/// </summary>
		public const string HexFindMatchHighlightMarker = nameof(HexFindMatchHighlightMarker);

		/// <summary>
		/// <see cref="TextColor.HexToolTipServiceField0"/>
		/// </summary>
		public const string HexToolTipServiceField0 = nameof(HexToolTipServiceField0);

		/// <summary>
		/// <see cref="TextColor.HexToolTipServiceField1"/>
		/// </summary>
		public const string HexToolTipServiceField1 = nameof(HexToolTipServiceField1);

		/// <summary>
		/// <see cref="TextColor.HexToolTipServiceCurrentField"/>
		/// </summary>
		public const string HexToolTipServiceCurrentField = nameof(HexToolTipServiceCurrentField);
	}
}
