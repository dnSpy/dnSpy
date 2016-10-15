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
	/// Classification type names
	/// </summary>
	public static class ThemeClassificationTypeNames {
		/// <summary>
		/// Identifier
		/// </summary>
		public const string Identifier = "identifier";

		/// <summary>
		/// Literal
		/// </summary>
		public const string Literal = "literal";

		/// <summary>
		/// <see cref="TextColor.Text"/>
		/// </summary>
		public const string Text = RoslynClassificationTypeNames.Text;

		/// <summary>
		/// <see cref="TextColor.Operator"/>
		/// </summary>
		public const string Operator = PredefinedClassificationTypeNames.Operator;

		/// <summary>
		/// <see cref="TextColor.Punctuation"/>
		/// </summary>
		public const string Punctuation = RoslynClassificationTypeNames.Punctuation;

		/// <summary>
		/// <see cref="TextColor.Number"/>
		/// </summary>
		public const string Number = PredefinedClassificationTypeNames.Number;

		/// <summary>
		/// <see cref="TextColor.Comment"/>
		/// </summary>
		public const string Comment = PredefinedClassificationTypeNames.Comment;

		/// <summary>
		/// <see cref="TextColor.Keyword"/>
		/// </summary>
		public const string Keyword = PredefinedClassificationTypeNames.Keyword;

		/// <summary>
		/// <see cref="TextColor.String"/>
		/// </summary>
		public const string String = PredefinedClassificationTypeNames.String;

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
		public const string Namespace = "Theme-" + nameof(Namespace);

		/// <summary>
		/// <see cref="TextColor.Type"/>
		/// </summary>
		public const string Type = RoslynClassificationTypeNames.ClassName;

		/// <summary>
		/// <see cref="TextColor.SealedType"/>
		/// </summary>
		public const string SealedType = "Theme-" + nameof(SealedType);

		/// <summary>
		/// <see cref="TextColor.StaticType"/>
		/// </summary>
		public const string StaticType = "Theme-" + nameof(StaticType);

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
		public const string MethodGenericParameter = "Theme-" + nameof(MethodGenericParameter);

		/// <summary>
		/// <see cref="TextColor.InstanceMethod"/>
		/// </summary>
		public const string InstanceMethod = "Theme-" + nameof(InstanceMethod);

		/// <summary>
		/// <see cref="TextColor.StaticMethod"/>
		/// </summary>
		public const string StaticMethod = "Theme-" + nameof(StaticMethod);

		/// <summary>
		/// <see cref="TextColor.ExtensionMethod"/>
		/// </summary>
		public const string ExtensionMethod = "Theme-" + nameof(ExtensionMethod);

		/// <summary>
		/// <see cref="TextColor.InstanceField"/>
		/// </summary>
		public const string InstanceField = "Theme-" + nameof(InstanceField);

		/// <summary>
		/// <see cref="TextColor.EnumField"/>
		/// </summary>
		public const string EnumField = "Theme-" + nameof(EnumField);

		/// <summary>
		/// <see cref="TextColor.LiteralField"/>
		/// </summary>
		public const string LiteralField = "Theme-" + nameof(LiteralField);

		/// <summary>
		/// <see cref="TextColor.StaticField"/>
		/// </summary>
		public const string StaticField = "Theme-" + nameof(StaticField);

		/// <summary>
		/// <see cref="TextColor.InstanceEvent"/>
		/// </summary>
		public const string InstanceEvent = "Theme-" + nameof(InstanceEvent);

		/// <summary>
		/// <see cref="TextColor.StaticEvent"/>
		/// </summary>
		public const string StaticEvent = "Theme-" + nameof(StaticEvent);

		/// <summary>
		/// <see cref="TextColor.InstanceProperty"/>
		/// </summary>
		public const string InstanceProperty = "Theme-" + nameof(InstanceProperty);

		/// <summary>
		/// <see cref="TextColor.StaticProperty"/>
		/// </summary>
		public const string StaticProperty = "Theme-" + nameof(StaticProperty);

		/// <summary>
		/// <see cref="TextColor.Local"/>
		/// </summary>
		public const string Local = "Theme-" + nameof(Local);

		/// <summary>
		/// <see cref="TextColor.Parameter"/>
		/// </summary>
		public const string Parameter = "Theme-" + nameof(Parameter);

		/// <summary>
		/// <see cref="TextColor.PreprocessorKeyword"/>
		/// </summary>
		public const string PreprocessorKeyword = PredefinedClassificationTypeNames.PreprocessorKeyword;

		/// <summary>
		/// <see cref="TextColor.PreprocessorText"/>
		/// </summary>
		public const string PreprocessorText = RoslynClassificationTypeNames.PreprocessorText;

		/// <summary>
		/// <see cref="TextColor.Label"/>
		/// </summary>
		public const string Label = "Theme-" + nameof(Label);

		/// <summary>
		/// <see cref="TextColor.OpCode"/>
		/// </summary>
		public const string OpCode = "Theme-" + nameof(OpCode);

		/// <summary>
		/// <see cref="TextColor.ILDirective"/>
		/// </summary>
		public const string ILDirective = "Theme-" + nameof(ILDirective);

		/// <summary>
		/// <see cref="TextColor.ILModule"/>
		/// </summary>
		public const string ILModule = "Theme-" + nameof(ILModule);

		/// <summary>
		/// <see cref="TextColor.ExcludedCode"/>
		/// </summary>
		public const string ExcludedCode = PredefinedClassificationTypeNames.ExcludedCode;

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
		public const string XmlAttribute = "Theme-" + nameof(XmlAttribute);

		/// <summary>
		/// <see cref="TextColor.XmlAttributeQuotes"/>
		/// </summary>
		public const string XmlAttributeQuotes = "Theme-" + nameof(XmlAttributeQuotes);

		/// <summary>
		/// <see cref="TextColor.XmlAttributeValue"/>
		/// </summary>
		public const string XmlAttributeValue = "Theme-" + nameof(XmlAttributeValue);

		/// <summary>
		/// <see cref="TextColor.XmlCDataSection"/>
		/// </summary>
		public const string XmlCDataSection = "Theme-" + nameof(XmlCDataSection);

		/// <summary>
		/// <see cref="TextColor.XmlComment"/>
		/// </summary>
		public const string XmlComment = "Theme-" + nameof(XmlComment);

		/// <summary>
		/// <see cref="TextColor.XmlDelimiter"/>
		/// </summary>
		public const string XmlDelimiter = "Theme-" + nameof(XmlDelimiter);

		/// <summary>
		/// <see cref="TextColor.XmlKeyword"/>
		/// </summary>
		public const string XmlKeyword = "Theme-" + nameof(XmlKeyword);

		/// <summary>
		/// <see cref="TextColor.XmlName"/>
		/// </summary>
		public const string XmlName = "Theme-" + nameof(XmlName);

		/// <summary>
		/// <see cref="TextColor.XmlProcessingInstruction"/>
		/// </summary>
		public const string XmlProcessingInstruction = "Theme-" + nameof(XmlProcessingInstruction);

		/// <summary>
		/// <see cref="TextColor.XmlText"/>
		/// </summary>
		public const string XmlText = "Theme-" + nameof(XmlText);

		/// <summary>
		/// <see cref="TextColor.XamlAttribute"/>
		/// </summary>
		public const string XamlAttribute = "Theme-" + nameof(XamlAttribute);

		/// <summary>
		/// <see cref="TextColor.XamlAttributeQuotes"/>
		/// </summary>
		public const string XamlAttributeQuotes = "Theme-" + nameof(XamlAttributeQuotes);

		/// <summary>
		/// <see cref="TextColor.XamlAttributeValue"/>
		/// </summary>
		public const string XamlAttributeValue = "Theme-" + nameof(XamlAttributeValue);

		/// <summary>
		/// <see cref="TextColor.XamlCDataSection"/>
		/// </summary>
		public const string XamlCDataSection = "Theme-" + nameof(XamlCDataSection);

		/// <summary>
		/// <see cref="TextColor.XamlComment"/>
		/// </summary>
		public const string XamlComment = "Theme-" + nameof(XamlComment);

		/// <summary>
		/// <see cref="TextColor.XamlDelimiter"/>
		/// </summary>
		public const string XamlDelimiter = "Theme-" + nameof(XamlDelimiter);

		/// <summary>
		/// <see cref="TextColor.XamlKeyword"/>
		/// </summary>
		public const string XamlKeyword = "Theme-" + nameof(XamlKeyword);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionClass"/>
		/// </summary>
		public const string XamlMarkupExtensionClass = "Theme-" + nameof(XamlMarkupExtensionClass);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionParameterName"/>
		/// </summary>
		public const string XamlMarkupExtensionParameterName = "Theme-" + nameof(XamlMarkupExtensionParameterName);

		/// <summary>
		/// <see cref="TextColor.XamlMarkupExtensionParameterValue"/>
		/// </summary>
		public const string XamlMarkupExtensionParameterValue = "Theme-" + nameof(XamlMarkupExtensionParameterValue);

		/// <summary>
		/// <see cref="TextColor.XamlName"/>
		/// </summary>
		public const string XamlName = "Theme-" + nameof(XamlName);

		/// <summary>
		/// <see cref="TextColor.XamlProcessingInstruction"/>
		/// </summary>
		public const string XamlProcessingInstruction = "Theme-" + nameof(XamlProcessingInstruction);

		/// <summary>
		/// <see cref="TextColor.XamlText"/>
		/// </summary>
		public const string XamlText = "Theme-" + nameof(XamlText);

		/// <summary>
		/// <see cref="TextColor.XmlDocToolTipHeader"/>
		/// </summary>
		public const string XmlDocToolTipHeader = "Theme-" + nameof(XmlDocToolTipHeader);

		/// <summary>
		/// <see cref="TextColor.Assembly"/>
		/// </summary>
		public const string Assembly = "Theme-" + nameof(Assembly);

		/// <summary>
		/// <see cref="TextColor.AssemblyExe"/>
		/// </summary>
		public const string AssemblyExe = "Theme-" + nameof(AssemblyExe);

		/// <summary>
		/// <see cref="TextColor.Module"/>
		/// </summary>
		public const string Module = RoslynClassificationTypeNames.ModuleName;

		/// <summary>
		/// <see cref="TextColor.DirectoryPart"/>
		/// </summary>
		public const string DirectoryPart = "Theme-" + nameof(DirectoryPart);

		/// <summary>
		/// <see cref="TextColor.FileNameNoExtension"/>
		/// </summary>
		public const string FileNameNoExtension = "Theme-" + nameof(FileNameNoExtension);

		/// <summary>
		/// <see cref="TextColor.FileExtension"/>
		/// </summary>
		public const string FileExtension = "Theme-" + nameof(FileExtension);

		/// <summary>
		/// <see cref="TextColor.Error"/>
		/// </summary>
		public const string Error = "Theme-" + nameof(Error);

		/// <summary>
		/// <see cref="TextColor.ToStringEval"/>
		/// </summary>
		public const string ToStringEval = "Theme-" + nameof(ToStringEval);

		/// <summary>
		/// <see cref="TextColor.ReplPrompt1"/>
		/// </summary>
		public const string ReplPrompt1 = "Theme-" + nameof(ReplPrompt1);

		/// <summary>
		/// <see cref="TextColor.ReplPrompt2"/>
		/// </summary>
		public const string ReplPrompt2 = "Theme-" + nameof(ReplPrompt2);

		/// <summary>
		/// <see cref="TextColor.ReplOutputText"/>
		/// </summary>
		public const string ReplOutputText = "Theme-" + nameof(ReplOutputText);

		/// <summary>
		/// <see cref="TextColor.ReplScriptOutputText"/>
		/// </summary>
		public const string ReplScriptOutputText = "Theme-" + nameof(ReplScriptOutputText);

		/// <summary>
		/// <see cref="TextColor.Black"/>
		/// </summary>
		public const string Black = "Theme-" + nameof(Black);

		/// <summary>
		/// <see cref="TextColor.Blue"/>
		/// </summary>
		public const string Blue = "Theme-" + nameof(Blue);

		/// <summary>
		/// <see cref="TextColor.Cyan"/>
		/// </summary>
		public const string Cyan = "Theme-" + nameof(Cyan);

		/// <summary>
		/// <see cref="TextColor.DarkBlue"/>
		/// </summary>
		public const string DarkBlue = "Theme-" + nameof(DarkBlue);

		/// <summary>
		/// <see cref="TextColor.DarkCyan"/>
		/// </summary>
		public const string DarkCyan = "Theme-" + nameof(DarkCyan);

		/// <summary>
		/// <see cref="TextColor.DarkGray"/>
		/// </summary>
		public const string DarkGray = "Theme-" + nameof(DarkGray);

		/// <summary>
		/// <see cref="TextColor.DarkGreen"/>
		/// </summary>
		public const string DarkGreen = "Theme-" + nameof(DarkGreen);

		/// <summary>
		/// <see cref="TextColor.DarkMagenta"/>
		/// </summary>
		public const string DarkMagenta = "Theme-" + nameof(DarkMagenta);

		/// <summary>
		/// <see cref="TextColor.DarkRed"/>
		/// </summary>
		public const string DarkRed = "Theme-" + nameof(DarkRed);

		/// <summary>
		/// <see cref="TextColor.DarkYellow"/>
		/// </summary>
		public const string DarkYellow = "Theme-" + nameof(DarkYellow);

		/// <summary>
		/// <see cref="TextColor.Gray"/>
		/// </summary>
		public const string Gray = "Theme-" + nameof(Gray);

		/// <summary>
		/// <see cref="TextColor.Green"/>
		/// </summary>
		public const string Green = "Theme-" + nameof(Green);

		/// <summary>
		/// <see cref="TextColor.Magenta"/>
		/// </summary>
		public const string Magenta = "Theme-" + nameof(Magenta);

		/// <summary>
		/// <see cref="TextColor.Red"/>
		/// </summary>
		public const string Red = "Theme-" + nameof(Red);

		/// <summary>
		/// <see cref="TextColor.White"/>
		/// </summary>
		public const string White = "Theme-" + nameof(White);

		/// <summary>
		/// <see cref="TextColor.Yellow"/>
		/// </summary>
		public const string Yellow = "Theme-" + nameof(Yellow);

		/// <summary>
		/// <see cref="TextColor.InvBlack"/>
		/// </summary>
		public const string InvBlack = "Theme-" + nameof(InvBlack);

		/// <summary>
		/// <see cref="TextColor.InvBlue"/>
		/// </summary>
		public const string InvBlue = "Theme-" + nameof(InvBlue);

		/// <summary>
		/// <see cref="TextColor.InvCyan"/>
		/// </summary>
		public const string InvCyan = "Theme-" + nameof(InvCyan);

		/// <summary>
		/// <see cref="TextColor.InvDarkBlue"/>
		/// </summary>
		public const string InvDarkBlue = "Theme-" + nameof(InvDarkBlue);

		/// <summary>
		/// <see cref="TextColor.InvDarkCyan"/>
		/// </summary>
		public const string InvDarkCyan = "Theme-" + nameof(InvDarkCyan);

		/// <summary>
		/// <see cref="TextColor.InvDarkGray"/>
		/// </summary>
		public const string InvDarkGray = "Theme-" + nameof(InvDarkGray);

		/// <summary>
		/// <see cref="TextColor.InvDarkGreen"/>
		/// </summary>
		public const string InvDarkGreen = "Theme-" + nameof(InvDarkGreen);

		/// <summary>
		/// <see cref="TextColor.InvDarkMagenta"/>
		/// </summary>
		public const string InvDarkMagenta = "Theme-" + nameof(InvDarkMagenta);

		/// <summary>
		/// <see cref="TextColor.InvDarkRed"/>
		/// </summary>
		public const string InvDarkRed = "Theme-" + nameof(InvDarkRed);

		/// <summary>
		/// <see cref="TextColor.InvDarkYellow"/>
		/// </summary>
		public const string InvDarkYellow = "Theme-" + nameof(InvDarkYellow);

		/// <summary>
		/// <see cref="TextColor.InvGray"/>
		/// </summary>
		public const string InvGray = "Theme-" + nameof(InvGray);

		/// <summary>
		/// <see cref="TextColor.InvGreen"/>
		/// </summary>
		public const string InvGreen = "Theme-" + nameof(InvGreen);

		/// <summary>
		/// <see cref="TextColor.InvMagenta"/>
		/// </summary>
		public const string InvMagenta = "Theme-" + nameof(InvMagenta);

		/// <summary>
		/// <see cref="TextColor.InvRed"/>
		/// </summary>
		public const string InvRed = "Theme-" + nameof(InvRed);

		/// <summary>
		/// <see cref="TextColor.InvWhite"/>
		/// </summary>
		public const string InvWhite = "Theme-" + nameof(InvWhite);

		/// <summary>
		/// <see cref="TextColor.InvYellow"/>
		/// </summary>
		public const string InvYellow = "Theme-" + nameof(InvYellow);

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionHandled"/>
		/// </summary>
		public const string DebugLogExceptionHandled = "Theme-" + nameof(DebugLogExceptionHandled);

		/// <summary>
		/// <see cref="TextColor.DebugLogExceptionUnhandled"/>
		/// </summary>
		public const string DebugLogExceptionUnhandled = "Theme-" + nameof(DebugLogExceptionUnhandled);

		/// <summary>
		/// <see cref="TextColor.DebugLogStepFiltering"/>
		/// </summary>
		public const string DebugLogStepFiltering = "Theme-" + nameof(DebugLogStepFiltering);

		/// <summary>
		/// <see cref="TextColor.DebugLogLoadModule"/>
		/// </summary>
		public const string DebugLogLoadModule = "Theme-" + nameof(DebugLogLoadModule);

		/// <summary>
		/// <see cref="TextColor.DebugLogUnloadModule"/>
		/// </summary>
		public const string DebugLogUnloadModule = "Theme-" + nameof(DebugLogUnloadModule);

		/// <summary>
		/// <see cref="TextColor.DebugLogExitProcess"/>
		/// </summary>
		public const string DebugLogExitProcess = "Theme-" + nameof(DebugLogExitProcess);

		/// <summary>
		/// <see cref="TextColor.DebugLogExitThread"/>
		/// </summary>
		public const string DebugLogExitThread = "Theme-" + nameof(DebugLogExitThread);

		/// <summary>
		/// <see cref="TextColor.DebugLogProgramOutput"/>
		/// </summary>
		public const string DebugLogProgramOutput = "Theme-" + nameof(DebugLogProgramOutput);

		/// <summary>
		/// <see cref="TextColor.DebugLogMDA"/>
		/// </summary>
		public const string DebugLogMDA = "Theme-" + nameof(DebugLogMDA);

		/// <summary>
		/// <see cref="TextColor.DebugLogTimestamp"/>
		/// </summary>
		public const string DebugLogTimestamp = "Theme-" + nameof(DebugLogTimestamp);

		/// <summary>
		/// <see cref="TextColor.LineNumber"/>
		/// </summary>
		public const string LineNumber = "line number";

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput1"/>
		/// </summary>
		public const string ReplLineNumberInput1 = "Theme-" + nameof(ReplLineNumberInput1);

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberInput2"/>
		/// </summary>
		public const string ReplLineNumberInput2 = "Theme-" + nameof(ReplLineNumberInput2);

		/// <summary>
		/// <see cref="TextColor.ReplLineNumberOutput"/>
		/// </summary>
		public const string ReplLineNumberOutput = "Theme-" + nameof(ReplLineNumberOutput);

		/// <summary>
		/// <see cref="TextColor.VisibleWhitespace"/>
		/// </summary>
		public const string VisibleWhitespace = "Theme-" + nameof(VisibleWhitespace);

		/// <summary>
		/// <see cref="TextColor.SelectedText"/>
		/// </summary>
		public const string SelectedText = "Theme-" + nameof(SelectedText);

		/// <summary>
		/// <see cref="TextColor.InactiveSelectedText"/>
		/// </summary>
		public const string InactiveSelectedText = "Theme-" + nameof(InactiveSelectedText);

		/// <summary>
		/// <see cref="TextColor.HighlightedReference"/>
		/// </summary>
		public const string HighlightedReference = "Theme-" + nameof(HighlightedReference);

		/// <summary>
		/// <see cref="TextColor.HighlightedWrittenReference"/>
		/// </summary>
		public const string HighlightedWrittenReference = "Theme-" + nameof(HighlightedWrittenReference);

		/// <summary>
		/// <see cref="TextColor.HighlightedDefinition"/>
		/// </summary>
		public const string HighlightedDefinition = "Theme-" + nameof(HighlightedDefinition);

		/// <summary>
		/// <see cref="TextColor.CurrentStatement"/>
		/// </summary>
		public const string CurrentStatement = "Theme-" + nameof(CurrentStatement);

		/// <summary>
		/// <see cref="TextColor.CurrentStatementMarker"/>
		/// </summary>
		public const string CurrentStatementMarker = "Theme-" + nameof(CurrentStatementMarker);

		/// <summary>
		/// <see cref="TextColor.CallReturn"/>
		/// </summary>
		public const string CallReturn = "Theme-" + nameof(CallReturn);

		/// <summary>
		/// <see cref="TextColor.CallReturnMarker"/>
		/// </summary>
		public const string CallReturnMarker = "Theme-" + nameof(CallReturnMarker);

		/// <summary>
		/// <see cref="TextColor.ActiveStatementMarker"/>
		/// </summary>
		public const string ActiveStatementMarker = "Theme-" + nameof(ActiveStatementMarker);

		/// <summary>
		/// <see cref="TextColor.BreakpointStatement"/>
		/// </summary>
		public const string BreakpointStatement = "Theme-" + nameof(BreakpointStatement);

		/// <summary>
		/// <see cref="TextColor.BreakpointStatementMarker"/>
		/// </summary>
		public const string BreakpointStatementMarker = "Theme-" + nameof(BreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.SelectedBreakpointStatementMarker"/>
		/// </summary>
		public const string SelectedBreakpointStatementMarker = "Theme-" + nameof(SelectedBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.DisabledBreakpointStatementMarker"/>
		/// </summary>
		public const string DisabledBreakpointStatementMarker = "Theme-" + nameof(DisabledBreakpointStatementMarker);

		/// <summary>
		/// <see cref="TextColor.CurrentLine"/>
		/// </summary>
		public const string CurrentLine = "Theme-" + nameof(CurrentLine);

		/// <summary>
		/// <see cref="TextColor.CurrentLineNoFocus"/>
		/// </summary>
		public const string CurrentLineNoFocus = "Theme-" + nameof(CurrentLineNoFocus);

		/// <summary>
		/// <see cref="TextColor.HexText"/>
		/// </summary>
		public const string HexText = "Theme-" + nameof(HexText);

		/// <summary>
		/// <see cref="TextColor.HexOffset"/>
		/// </summary>
		public const string HexOffset = "Theme-" + nameof(HexOffset);

		/// <summary>
		/// <see cref="TextColor.HexByte0"/>
		/// </summary>
		public const string HexByte0 = "Theme-" + nameof(HexByte0);

		/// <summary>
		/// <see cref="TextColor.HexByte1"/>
		/// </summary>
		public const string HexByte1 = "Theme-" + nameof(HexByte1);

		/// <summary>
		/// <see cref="TextColor.HexByteError"/>
		/// </summary>
		public const string HexByteError = "Theme-" + nameof(HexByteError);

		/// <summary>
		/// <see cref="TextColor.HexAscii"/>
		/// </summary>
		public const string HexAscii = "Theme-" + nameof(HexAscii);

		/// <summary>
		/// <see cref="TextColor.HexCaret"/>
		/// </summary>
		public const string HexCaret = "Theme-" + nameof(HexCaret);

		/// <summary>
		/// <see cref="TextColor.HexInactiveCaret"/>
		/// </summary>
		public const string HexInactiveCaret = "Theme-" + nameof(HexInactiveCaret);

		/// <summary>
		/// <see cref="TextColor.HexSelection"/>
		/// </summary>
		public const string HexSelection = "Theme-" + nameof(HexSelection);

		/// <summary>
		/// <see cref="TextColor.GlyphMargin"/>
		/// </summary>
		public const string GlyphMargin = "Theme-" + nameof(GlyphMargin);

		/// <summary>
		/// <see cref="TextColor.BraceMatching"/>
		/// </summary>
		public const string BraceMatching = "brace matching";

		/// <summary>
		/// <see cref="TextColor.LineSeparator"/>
		/// </summary>
		public const string LineSeparator = "Theme-" + nameof(LineSeparator);

		/// <summary>
		/// <see cref="TextColor.FindMatchHighlightMarker"/>
		/// </summary>
		public const string FindMatchHighlightMarker = "Theme-" + nameof(FindMatchHighlightMarker);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerNamespace"/>
		/// </summary>
		public const string StructureVisualizerNamespace = "Theme-" + nameof(StructureVisualizerNamespace);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerType"/>
		/// </summary>
		public const string StructureVisualizerType = "Theme-" + nameof(StructureVisualizerType);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerModule"/>
		/// </summary>
		public const string StructureVisualizerModule = "Theme-" + nameof(StructureVisualizerModule);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerValueType"/>
		/// </summary>
		public const string StructureVisualizerValueType = "Theme-" + nameof(StructureVisualizerValueType);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerInterface"/>
		/// </summary>
		public const string StructureVisualizerInterface = "Theme-" + nameof(StructureVisualizerInterface);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerMethod"/>
		/// </summary>
		public const string StructureVisualizerMethod = "Theme-" + nameof(StructureVisualizerMethod);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerAccessor"/>
		/// </summary>
		public const string StructureVisualizerAccessor = "Theme-" + nameof(StructureVisualizerAccessor);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerAnonymousMethod"/>
		/// </summary>
		public const string StructureVisualizerAnonymousMethod = "Theme-" + nameof(StructureVisualizerAnonymousMethod);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerConstructor"/>
		/// </summary>
		public const string StructureVisualizerConstructor = "Theme-" + nameof(StructureVisualizerConstructor);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerDestructor"/>
		/// </summary>
		public const string StructureVisualizerDestructor = "Theme-" + nameof(StructureVisualizerDestructor);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerOperator"/>
		/// </summary>
		public const string StructureVisualizerOperator = "Theme-" + nameof(StructureVisualizerOperator);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerConditional"/>
		/// </summary>
		public const string StructureVisualizerConditional = "Theme-" + nameof(StructureVisualizerConditional);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerLoop"/>
		/// </summary>
		public const string StructureVisualizerLoop = "Theme-" + nameof(StructureVisualizerLoop);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerProperty"/>
		/// </summary>
		public const string StructureVisualizerProperty = "Theme-" + nameof(StructureVisualizerProperty);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerEvent"/>
		/// </summary>
		public const string StructureVisualizerEvent = "Theme-" + nameof(StructureVisualizerEvent);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerTry"/>
		/// </summary>
		public const string StructureVisualizerTry = "Theme-" + nameof(StructureVisualizerTry);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerCatch"/>
		/// </summary>
		public const string StructureVisualizerCatch = "Theme-" + nameof(StructureVisualizerCatch);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFilter"/>
		/// </summary>
		public const string StructureVisualizerFilter = "Theme-" + nameof(StructureVisualizerFilter);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFinally"/>
		/// </summary>
		public const string StructureVisualizerFinally = "Theme-" + nameof(StructureVisualizerFinally);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFault"/>
		/// </summary>
		public const string StructureVisualizerFault = "Theme-" + nameof(StructureVisualizerFault);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerLock"/>
		/// </summary>
		public const string StructureVisualizerLock = "Theme-" + nameof(StructureVisualizerLock);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerUsing"/>
		/// </summary>
		public const string StructureVisualizerUsing = "Theme-" + nameof(StructureVisualizerUsing);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerFixed"/>
		/// </summary>
		public const string StructureVisualizerFixed = "Theme-" + nameof(StructureVisualizerFixed);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerSwitch"/>
		/// </summary>
		public const string StructureVisualizerSwitch = "Theme-" + nameof(StructureVisualizerSwitch);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerCase"/>
		/// </summary>
		public const string StructureVisualizerCase = "Theme-" + nameof(StructureVisualizerCase);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerLocalFunction"/>
		/// </summary>
		public const string StructureVisualizerLocalFunction = "Theme-" + nameof(StructureVisualizerLocalFunction);

		/// <summary>
		/// <see cref="TextColor.StructureVisualizerOther"/>
		/// </summary>
		public const string StructureVisualizerOther = "Theme-" + nameof(StructureVisualizerOther);

		/// <summary>
		/// <see cref="TextColor.CompletionMatchHighlight"/>
		/// </summary>
		public const string CompletionMatchHighlight = "Theme-" + nameof(CompletionMatchHighlight);

		/// <summary>
		/// <see cref="TextColor.CompletionSuffix"/>
		/// </summary>
		public const string CompletionSuffix = "Theme-" + nameof(CompletionSuffix);

		/// <summary>
		/// <see cref="TextColor.SignatureHelpDocumentation"/>
		/// </summary>
		public const string SignatureHelpDocumentation = "sighelp-documentation";

		/// <summary>
		/// <see cref="TextColor.SignatureHelpCurrentParameter"/>
		/// </summary>
		public const string SignatureHelpCurrentParameter = "currentParam";

		/// <summary>
		/// <see cref="TextColor.SignatureHelpParameter"/>
		/// </summary>
		public const string SignatureHelpParameter = "Theme-" + nameof(SignatureHelpParameter);

		/// <summary>
		/// <see cref="TextColor.SignatureHelpParameterDocumentation"/>
		/// </summary>
		public const string SignatureHelpParameterDocumentation = "Theme-" + nameof(SignatureHelpParameterDocumentation);

		/// <summary>
		/// <see cref="TextColor.Url"/>
		/// </summary>
		public const string Url = "url";

		/// <summary>
		/// <see cref="TextColor.HexPeDosHeader"/>
		/// </summary>
		public const string HexPeDosHeader = "Theme-" + nameof(HexPeDosHeader);

		/// <summary>
		/// <see cref="TextColor.HexPeFileHeader"/>
		/// </summary>
		public const string HexPeFileHeader = "Theme-" + nameof(HexPeFileHeader);

		/// <summary>
		/// <see cref="TextColor.HexPeOptionalHeader32"/>
		/// </summary>
		public const string HexPeOptionalHeader32 = "Theme-" + nameof(HexPeOptionalHeader32);

		/// <summary>
		/// <see cref="TextColor.HexPeOptionalHeader64"/>
		/// </summary>
		public const string HexPeOptionalHeader64 = "Theme-" + nameof(HexPeOptionalHeader64);

		/// <summary>
		/// <see cref="TextColor.HexPeSection"/>
		/// </summary>
		public const string HexPeSection = "Theme-" + nameof(HexPeSection);

		/// <summary>
		/// <see cref="TextColor.HexPeSectionName"/>
		/// </summary>
		public const string HexPeSectionName = "Theme-" + nameof(HexPeSectionName);

		/// <summary>
		/// <see cref="TextColor.HexCor20Header"/>
		/// </summary>
		public const string HexCor20Header = "Theme-" + nameof(HexCor20Header);

		/// <summary>
		/// <see cref="TextColor.HexStorageSignature"/>
		/// </summary>
		public const string HexStorageSignature = "Theme-" + nameof(HexStorageSignature);

		/// <summary>
		/// <see cref="TextColor.HexStorageHeader"/>
		/// </summary>
		public const string HexStorageHeader = "Theme-" + nameof(HexStorageHeader);

		/// <summary>
		/// <see cref="TextColor.HexStorageStream"/>
		/// </summary>
		public const string HexStorageStream = "Theme-" + nameof(HexStorageStream);

		/// <summary>
		/// <see cref="TextColor.HexStorageStreamName"/>
		/// </summary>
		public const string HexStorageStreamName = "Theme-" + nameof(HexStorageStreamName);

		/// <summary>
		/// <see cref="TextColor.HexStorageStreamNameInvalid"/>
		/// </summary>
		public const string HexStorageStreamNameInvalid = "Theme-" + nameof(HexStorageStreamNameInvalid);

		/// <summary>
		/// <see cref="TextColor.HexTablesStream"/>
		/// </summary>
		public const string HexTablesStream = "Theme-" + nameof(HexTablesStream);

		/// <summary>
		/// <see cref="TextColor.HexTableName"/>
		/// </summary>
		public const string HexTableName = "Theme-" + nameof(HexTableName);
	}
}
