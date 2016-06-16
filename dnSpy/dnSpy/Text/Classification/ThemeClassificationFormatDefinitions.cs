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

using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;

namespace dnSpy.Text.Classification {
	static class ThemeClassificationFormatDefinitions {
#pragma warning disable CS0169
		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Text)]
		[DisplayName("Text")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition TextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Operator)]
		[DisplayName("Operator")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition OperatorClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Punctuation)]
		[DisplayName("Punctuation")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition PunctuationClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Number)]
		[DisplayName("Number")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition NumberClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Comment)]
		[DisplayName("Comment")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CommentClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Keyword)]
		[DisplayName("Keyword")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition KeywordClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.String)]
		[DisplayName("String")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StringClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.VerbatimString)]
		[DisplayName("VerbatimString")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition VerbatimStringClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Char)]
		[DisplayName("Char")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CharClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Namespace)]
		[DisplayName("Namespace")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition NamespaceClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Type)]
		[DisplayName("Type")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition TypeClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.SealedType)]
		[DisplayName("SealedType")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition SealedTypeClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.StaticType)]
		[DisplayName("StaticType")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticTypeClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Delegate)]
		[DisplayName("Delegate")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DelegateClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Enum)]
		[DisplayName("Enum")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition EnumClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Interface)]
		[DisplayName("Interface")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InterfaceClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ValueType)]
		[DisplayName("ValueType")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ValueTypeClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.TypeGenericParameter)]
		[DisplayName("TypeGenericParameter")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition TypeGenericParameterClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.MethodGenericParameter)]
		[DisplayName("MethodGenericParameter")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition MethodGenericParameterClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InstanceMethod)]
		[DisplayName("InstanceMethod")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstanceMethodClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.StaticMethod)]
		[DisplayName("StaticMethod")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticMethodClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ExtensionMethod)]
		[DisplayName("ExtensionMethod")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ExtensionMethodClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InstanceField)]
		[DisplayName("InstanceField")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstanceFieldClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.EnumField)]
		[DisplayName("EnumField")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition EnumFieldClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.LiteralField)]
		[DisplayName("LiteralField")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LiteralFieldClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.StaticField)]
		[DisplayName("StaticField")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticFieldClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InstanceEvent)]
		[DisplayName("InstanceEvent")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstanceEventClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.StaticEvent)]
		[DisplayName("StaticEvent")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticEventClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InstanceProperty)]
		[DisplayName("InstanceProperty")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstancePropertyClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.StaticProperty)]
		[DisplayName("StaticProperty")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticPropertyClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Local)]
		[DisplayName("Local")]

		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LocalClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Parameter)]
		[DisplayName("Parameter")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ParameterClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.PreprocessorKeyword)]
		[DisplayName("PreprocessorKeyword")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition PreprocessorKeywordClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.PreprocessorText)]
		[DisplayName("PreprocessorText")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition PreprocessorTextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Label)]
		[DisplayName("Label")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LabelClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.OpCode)]
		[DisplayName("OpCode")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition OpCodeClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ILDirective)]
		[DisplayName("ILDirective")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ILDirectiveClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ILModule)]
		[DisplayName("ILModule")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ILModuleClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ExcludedCode)]
		[DisplayName("ExcludedCode")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ExcludedCodeClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentAttributeName)]
		[DisplayName("XmlDocCommentAttributeName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentAttributeNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes)]
		[DisplayName("XmlDocCommentAttributeQuotes")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentAttributeQuotesClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentAttributeValue)]
		[DisplayName("XmlDocCommentAttributeValue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentAttributeValueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentCDataSection)]
		[DisplayName("XmlDocCommentCDataSection")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentCDataSectionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentComment)]
		[DisplayName("XmlDocCommentComment")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentCommentClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentDelimiter)]
		[DisplayName("XmlDocCommentDelimiter")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentDelimiterClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentEntityReference)]
		[DisplayName("XmlDocCommentEntityReference")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentEntityReferenceClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentName)]
		[DisplayName("XmlDocCommentName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction)]
		[DisplayName("XmlDocCommentProcessingInstruction")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentProcessingInstructionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocCommentText)]
		[DisplayName("XmlDocCommentText")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentTextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralAttributeName)]
		[DisplayName("XmlLiteralAttributeName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralAttributeNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralAttributeQuotes)]
		[DisplayName("XmlLiteralAttributeQuotes")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralAttributeQuotesClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralAttributeValue)]
		[DisplayName("XmlLiteralAttributeValue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralAttributeValueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralCDataSection)]
		[DisplayName("XmlLiteralCDataSection")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralCDataSectionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralComment)]
		[DisplayName("XmlLiteralComment")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralCommentClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralDelimiter)]
		[DisplayName("XmlLiteralDelimiter")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralDelimiterClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression)]
		[DisplayName("XmlLiteralEmbeddedExpression")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralEmbeddedExpressionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralEntityReference)]
		[DisplayName("XmlLiteralEntityReference")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralEntityReferenceClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralName)]
		[DisplayName("XmlLiteralName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralProcessingInstruction)]
		[DisplayName("XmlLiteralProcessingInstruction")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralProcessingInstructionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlLiteralText)]
		[DisplayName("XmlLiteralText")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralTextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlAttributeName)]
		[DisplayName("XmlAttributeName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlAttributeNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlAttributeQuotes)]
		[DisplayName("XmlAttributeQuotes")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlAttributeQuotesClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlAttributeValue)]
		[DisplayName("XmlAttributeValue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlAttributeValueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlCDataSection)]
		[DisplayName("XmlCDataSection")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlCDataSectionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlComment)]
		[DisplayName("XmlComment")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlCommentClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDelimiter)]
		[DisplayName("XmlDelimiter")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDelimiterClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlKeyword)]
		[DisplayName("XmlKeyword")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlKeywordClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlName)]
		[DisplayName("XmlName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlProcessingInstruction)]
		[DisplayName("XmlProcessingInstruction")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlProcessingInstructionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlText)]
		[DisplayName("XmlText")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlTextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipColon)]
		[DisplayName("XmlDocToolTipColon")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipColonClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipExample)]
		[DisplayName("XmlDocToolTipExample")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipExampleClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipExceptionCref)]
		[DisplayName("XmlDocToolTipExceptionCref")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipExceptionCrefClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipReturns)]
		[DisplayName("XmlDocToolTipReturns")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipReturnsClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeCref)]
		[DisplayName("XmlDocToolTipSeeCref")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeCrefClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeLangword)]
		[DisplayName("XmlDocToolTipSeeLangword")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeLangwordClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeAlso)]
		[DisplayName("XmlDocToolTipSeeAlso")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeAlsoClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeAlsoCref)]
		[DisplayName("XmlDocToolTipSeeAlsoCref")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeAlsoCrefClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipParamRefName)]
		[DisplayName("XmlDocToolTipParamRefName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipParamRefNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipParamName)]
		[DisplayName("XmlDocToolTipParamName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipParamNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipTypeParamName)]
		[DisplayName("XmlDocToolTipTypeParamName")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipTypeParamNameClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipValue)]
		[DisplayName("XmlDocToolTipValue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipValueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipSummary)]
		[DisplayName("XmlDocToolTipSummary")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSummaryClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.XmlDocToolTipText)]
		[DisplayName("XmlDocToolTipText")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipTextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Assembly)]
		[DisplayName("Assembly")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition AssemblyClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.AssemblyExe)]
		[DisplayName("AssemblyExe")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition AssemblyExeClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Module)]
		[DisplayName("Module")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ModuleClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DirectoryPart)]
		[DisplayName("DirectoryPart")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DirectoryPartClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.FileNameNoExtension)]
		[DisplayName("FileNameNoExtension")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition FileNameNoExtensionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.FileExtension)]
		[DisplayName("FileExtension")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition FileExtensionClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Error)]
		[DisplayName("Error")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ErrorClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ToStringEval)]
		[DisplayName("ToStringEval")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ToStringEvalClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ReplPrompt1)]
		[DisplayName("ReplPrompt1")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplPrompt1ClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ReplPrompt2)]
		[DisplayName("ReplPrompt2")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplPrompt2ClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ReplOutputText)]
		[DisplayName("ReplOutputText")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplOutputTextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.ReplScriptOutputText)]
		[DisplayName("ReplScriptOutputText")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplScriptOutputTextClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Black)]
		[DisplayName("Black")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition BlackClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Blue)]
		[DisplayName("Blue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition BlueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Cyan)]
		[DisplayName("Cyan")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CyanClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DarkBlue)]
		[DisplayName("DarkBlue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkBlueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DarkCyan)]
		[DisplayName("DarkCyan")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkCyanClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DarkGray)]
		[DisplayName("DarkGray")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkGrayClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DarkGreen)]
		[DisplayName("DarkGreen")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkGreenClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DarkMagenta)]
		[DisplayName("DarkMagenta")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkMagentaClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DarkRed)]
		[DisplayName("DarkRed")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkRedClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DarkYellow)]
		[DisplayName("DarkYellow")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkYellowClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Gray)]
		[DisplayName("Gray")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition GrayClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Green)]
		[DisplayName("Green")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition GreenClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Magenta)]
		[DisplayName("Magenta")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition MagentaClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Red)]
		[DisplayName("Red")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition RedClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.White)]
		[DisplayName("White")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition WhiteClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.Yellow)]
		[DisplayName("Yellow")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition YellowClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvBlack)]
		[DisplayName("InvBlack")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvBlackClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvBlue)]
		[DisplayName("InvBlue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvBlueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvCyan)]
		[DisplayName("InvCyan")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvCyanClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvDarkBlue)]
		[DisplayName("InvDarkBlue")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkBlueClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvDarkCyan)]
		[DisplayName("InvDarkCyan")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkCyanClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvDarkGray)]
		[DisplayName("InvDarkGray")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkGrayClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvDarkGreen)]
		[DisplayName("InvDarkGreen")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkGreenClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvDarkMagenta)]
		[DisplayName("InvDarkMagenta")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkMagentaClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvDarkRed)]
		[DisplayName("InvDarkRed")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkRedClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvDarkYellow)]
		[DisplayName("InvDarkYellow")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkYellowClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvGray)]
		[DisplayName("InvGray")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvGrayClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvGreen)]
		[DisplayName("InvGreen")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvGreenClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvMagenta)]
		[DisplayName("InvMagenta")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvMagentaClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvRed)]
		[DisplayName("InvRed")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvRedClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvWhite)]
		[DisplayName("InvWhite")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvWhiteClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.InvYellow)]
		[DisplayName("InvYellow")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvYellowClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogExceptionHandled)]
		[DisplayName("DebugLogExceptionHandled")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExceptionHandledClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogExceptionUnhandled)]
		[DisplayName("DebugLogExceptionUnhandled")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExceptionUnhandledClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogStepFiltering)]
		[DisplayName("DebugLogStepFiltering")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogStepFilteringClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogLoadModule)]
		[DisplayName("DebugLogLoadModule")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogLoadModuleClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogUnloadModule)]
		[DisplayName("DebugLogUnloadModule")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogUnloadModuleClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogExitProcess)]
		[DisplayName("DebugLogExitProcess")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExitProcessClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogExitThread)]
		[DisplayName("DebugLogExitThread")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExitThreadClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogProgramOutput)]
		[DisplayName("DebugLogProgramOutput")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogProgramOutputClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogMDA)]
		[DisplayName("DebugLogMDA")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogMDAClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(ThemeClassificationTypeNames.DebugLogTimestamp)]
		[DisplayName("DebugLogTimestamp")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogTimestampClassificationTypeDefinition;
#pragma warning restore CS0169

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Text, "Text", Order = EditorFormatDefinitionPriority.Low)]
		sealed class Text : ThemeClassificationFormatDefinition {
			Text() : base(ColorType.Text) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Operator, "Operator", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Operator : ThemeClassificationFormatDefinition {
			Operator() : base(ColorType.Operator) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Punctuation, "Punctuation", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Punctuation : ThemeClassificationFormatDefinition {
			Punctuation() : base(ColorType.Punctuation) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Number, "Number", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Number : ThemeClassificationFormatDefinition {
			Number() : base(ColorType.Number) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Comment, "Comment", Order = EditorFormatDefinitionPriority.AfterDefault + 1)]
		sealed class Comment : ThemeClassificationFormatDefinition {
			Comment() : base(ColorType.Comment) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Keyword, "Keyword", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Keyword : ThemeClassificationFormatDefinition {
			Keyword() : base(ColorType.Keyword) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.String, "String", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class String : ThemeClassificationFormatDefinition {
			String() : base(ColorType.String) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.VerbatimString, "VerbatimString", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class VerbatimString : ThemeClassificationFormatDefinition {
			VerbatimString() : base(ColorType.VerbatimString) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Char, "Char", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Char : ThemeClassificationFormatDefinition {
			Char() : base(ColorType.Char) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Namespace, "Namespace", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Namespace : ThemeClassificationFormatDefinition {
			Namespace() : base(ColorType.Namespace) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Type, "Type", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Type : ThemeClassificationFormatDefinition {
			Type() : base(ColorType.Type) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.SealedType, "SealedType", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class SealedType : ThemeClassificationFormatDefinition {
			SealedType() : base(ColorType.SealedType) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.StaticType, "StaticType", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class StaticType : ThemeClassificationFormatDefinition {
			StaticType() : base(ColorType.StaticType) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Delegate, "Delegate", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Delegate : ThemeClassificationFormatDefinition {
			Delegate() : base(ColorType.Delegate) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Enum, "Enum", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Enum : ThemeClassificationFormatDefinition {
			Enum() : base(ColorType.Enum) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Interface, "Interface", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Interface : ThemeClassificationFormatDefinition {
			Interface() : base(ColorType.Interface) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ValueType, "ValueType", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ValueType : ThemeClassificationFormatDefinition {
			ValueType() : base(ColorType.ValueType) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.TypeGenericParameter, "TypeGenericParameter", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class TypeGenericParameter : ThemeClassificationFormatDefinition {
			TypeGenericParameter() : base(ColorType.TypeGenericParameter) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.MethodGenericParameter, "MethodGenericParameter", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class MethodGenericParameter : ThemeClassificationFormatDefinition {
			MethodGenericParameter() : base(ColorType.MethodGenericParameter) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InstanceMethod, "InstanceMethod", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InstanceMethod : ThemeClassificationFormatDefinition {
			InstanceMethod() : base(ColorType.InstanceMethod) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.StaticMethod, "StaticMethod", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class StaticMethod : ThemeClassificationFormatDefinition {
			StaticMethod() : base(ColorType.StaticMethod) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ExtensionMethod, "ExtensionMethod", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ExtensionMethod : ThemeClassificationFormatDefinition {
			ExtensionMethod() : base(ColorType.ExtensionMethod) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InstanceField, "InstanceField", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InstanceField : ThemeClassificationFormatDefinition {
			InstanceField() : base(ColorType.InstanceField) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.EnumField, "EnumField", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class EnumField : ThemeClassificationFormatDefinition {
			EnumField() : base(ColorType.EnumField) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.LiteralField, "LiteralField", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class LiteralField : ThemeClassificationFormatDefinition {
			LiteralField() : base(ColorType.LiteralField) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.StaticField, "StaticField", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class StaticField : ThemeClassificationFormatDefinition {
			StaticField() : base(ColorType.StaticField) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InstanceEvent, "InstanceEvent", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InstanceEvent : ThemeClassificationFormatDefinition {
			InstanceEvent() : base(ColorType.InstanceEvent) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.StaticEvent, "StaticEvent", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class StaticEvent : ThemeClassificationFormatDefinition {
			StaticEvent() : base(ColorType.StaticEvent) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InstanceProperty, "InstanceProperty", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InstanceProperty : ThemeClassificationFormatDefinition {
			InstanceProperty() : base(ColorType.InstanceProperty) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.StaticProperty, "StaticProperty", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class StaticProperty : ThemeClassificationFormatDefinition {
			StaticProperty() : base(ColorType.StaticProperty) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Local, "Local", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Local : ThemeClassificationFormatDefinition {
			Local() : base(ColorType.Local) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Parameter, "Parameter", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Parameter : ThemeClassificationFormatDefinition {
			Parameter() : base(ColorType.Parameter) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.PreprocessorKeyword, "PreprocessorKeyword", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class PreprocessorKeyword : ThemeClassificationFormatDefinition {
			PreprocessorKeyword() : base(ColorType.PreprocessorKeyword) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.PreprocessorText, "PreprocessorText", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class PreprocessorText : ThemeClassificationFormatDefinition {
			PreprocessorText() : base(ColorType.PreprocessorText) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Label, "Label", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Label : ThemeClassificationFormatDefinition {
			Label() : base(ColorType.Label) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.OpCode, "OpCode", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class OpCode : ThemeClassificationFormatDefinition {
			OpCode() : base(ColorType.OpCode) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ILDirective, "ILDirective", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ILDirective : ThemeClassificationFormatDefinition {
			ILDirective() : base(ColorType.ILDirective) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ILModule, "ILModule", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ILModule : ThemeClassificationFormatDefinition {
			ILModule() : base(ColorType.ILModule) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ExcludedCode, "ExcludedCode", Order = EditorFormatDefinitionPriority.BeforeHigh)]
		sealed class ExcludedCode : ThemeClassificationFormatDefinition {
			ExcludedCode() : base(ColorType.ExcludedCode) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentAttributeName, "XmlDocCommentAttributeName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentAttributeName : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeName() : base(ColorType.XmlDocCommentAttributeName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes, "XmlDocCommentAttributeQuotes", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeQuotes() : base(ColorType.XmlDocCommentAttributeQuotes) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentAttributeValue, "XmlDocCommentAttributeValue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentAttributeValue : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeValue() : base(ColorType.XmlDocCommentAttributeValue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentCDataSection, "XmlDocCommentCDataSection", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentCDataSection : ThemeClassificationFormatDefinition {
			XmlDocCommentCDataSection() : base(ColorType.XmlDocCommentCDataSection) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentComment, "XmlDocCommentComment", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentComment : ThemeClassificationFormatDefinition {
			XmlDocCommentComment() : base(ColorType.XmlDocCommentComment) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentDelimiter, "XmlDocCommentDelimiter", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentDelimiter : ThemeClassificationFormatDefinition {
			XmlDocCommentDelimiter() : base(ColorType.XmlDocCommentDelimiter) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentEntityReference, "XmlDocCommentEntityReference", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentEntityReference : ThemeClassificationFormatDefinition {
			XmlDocCommentEntityReference() : base(ColorType.XmlDocCommentEntityReference) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentName, "XmlDocCommentName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentName : ThemeClassificationFormatDefinition {
			XmlDocCommentName() : base(ColorType.XmlDocCommentName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction, "XmlDocCommentProcessingInstruction", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlDocCommentProcessingInstruction() : base(ColorType.XmlDocCommentProcessingInstruction) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocCommentText, "XmlDocCommentText", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocCommentText : ThemeClassificationFormatDefinition {
			XmlDocCommentText() : base(ColorType.XmlDocCommentText) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralAttributeName, "XmlLiteralAttributeName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralAttributeName : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeName() : base(ColorType.XmlLiteralAttributeName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralAttributeQuotes, "XmlLiteralAttributeQuotes", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeQuotes() : base(ColorType.XmlLiteralAttributeQuotes) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralAttributeValue, "XmlLiteralAttributeValue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralAttributeValue : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeValue() : base(ColorType.XmlLiteralAttributeValue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralCDataSection, "XmlLiteralCDataSection", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralCDataSection : ThemeClassificationFormatDefinition {
			XmlLiteralCDataSection() : base(ColorType.XmlLiteralCDataSection) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralComment, "XmlLiteralComment", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralComment : ThemeClassificationFormatDefinition {
			XmlLiteralComment() : base(ColorType.XmlLiteralComment) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralDelimiter, "XmlLiteralDelimiter", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralDelimiter : ThemeClassificationFormatDefinition {
			XmlLiteralDelimiter() : base(ColorType.XmlLiteralDelimiter) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression, "XmlLiteralEmbeddedExpression", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralEmbeddedExpression : ThemeClassificationFormatDefinition {
			XmlLiteralEmbeddedExpression() : base(ColorType.XmlLiteralEmbeddedExpression) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralEntityReference, "XmlLiteralEntityReference", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralEntityReference : ThemeClassificationFormatDefinition {
			XmlLiteralEntityReference() : base(ColorType.XmlLiteralEntityReference) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralName, "XmlLiteralName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralName : ThemeClassificationFormatDefinition {
			XmlLiteralName() : base(ColorType.XmlLiteralName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralProcessingInstruction, "XmlLiteralProcessingInstruction", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlLiteralProcessingInstruction() : base(ColorType.XmlLiteralProcessingInstruction) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlLiteralText, "XmlLiteralText", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlLiteralText : ThemeClassificationFormatDefinition {
			XmlLiteralText() : base(ColorType.XmlLiteralText) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlAttributeName, "XmlAttributeName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlAttributeName : ThemeClassificationFormatDefinition {
			XmlAttributeName() : base(ColorType.XmlAttributeName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlAttributeQuotes, "XmlAttributeQuotes", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlAttributeQuotes() : base(ColorType.XmlAttributeQuotes) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlAttributeValue, "XmlAttributeValue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlAttributeValue : ThemeClassificationFormatDefinition {
			XmlAttributeValue() : base(ColorType.XmlAttributeValue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlCDataSection, "XmlCDataSection", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlCDataSection : ThemeClassificationFormatDefinition {
			XmlCDataSection() : base(ColorType.XmlCDataSection) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlComment, "XmlComment", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlComment : ThemeClassificationFormatDefinition {
			XmlComment() : base(ColorType.XmlComment) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDelimiter, "XmlDelimiter", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDelimiter : ThemeClassificationFormatDefinition {
			XmlDelimiter() : base(ColorType.XmlDelimiter) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlKeyword, "XmlKeyword", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlKeyword : ThemeClassificationFormatDefinition {
			XmlKeyword() : base(ColorType.XmlKeyword) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlName, "XmlName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlName : ThemeClassificationFormatDefinition {
			XmlName() : base(ColorType.XmlName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlProcessingInstruction, "XmlProcessingInstruction", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlProcessingInstruction() : base(ColorType.XmlProcessingInstruction) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlText, "XmlText", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlText : ThemeClassificationFormatDefinition {
			XmlText() : base(ColorType.XmlText) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipColon, "XmlDocToolTipColon", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipColon : ThemeClassificationFormatDefinition {
			XmlDocToolTipColon() : base(ColorType.XmlDocToolTipColon) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipExample, "XmlDocToolTipExample", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipExample : ThemeClassificationFormatDefinition {
			XmlDocToolTipExample() : base(ColorType.XmlDocToolTipExample) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipExceptionCref, "XmlDocToolTipExceptionCref", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipExceptionCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipExceptionCref() : base(ColorType.XmlDocToolTipExceptionCref) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipReturns, "XmlDocToolTipReturns", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipReturns : ThemeClassificationFormatDefinition {
			XmlDocToolTipReturns() : base(ColorType.XmlDocToolTipReturns) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeCref, "XmlDocToolTipSeeCref", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipSeeCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeCref() : base(ColorType.XmlDocToolTipSeeCref) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeLangword, "XmlDocToolTipSeeLangword", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipSeeLangword : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeLangword() : base(ColorType.XmlDocToolTipSeeLangword) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeAlso, "XmlDocToolTipSeeAlso", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipSeeAlso : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeAlso() : base(ColorType.XmlDocToolTipSeeAlso) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipSeeAlsoCref, "XmlDocToolTipSeeAlsoCref", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipSeeAlsoCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeAlsoCref() : base(ColorType.XmlDocToolTipSeeAlsoCref) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipParamRefName, "XmlDocToolTipParamRefName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipParamRefName : ThemeClassificationFormatDefinition {
			XmlDocToolTipParamRefName() : base(ColorType.XmlDocToolTipParamRefName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipParamName, "XmlDocToolTipParamName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipParamName : ThemeClassificationFormatDefinition {
			XmlDocToolTipParamName() : base(ColorType.XmlDocToolTipParamName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipTypeParamName, "XmlDocToolTipTypeParamName", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipTypeParamName : ThemeClassificationFormatDefinition {
			XmlDocToolTipTypeParamName() : base(ColorType.XmlDocToolTipTypeParamName) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipValue, "XmlDocToolTipValue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipValue : ThemeClassificationFormatDefinition {
			XmlDocToolTipValue() : base(ColorType.XmlDocToolTipValue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipSummary, "XmlDocToolTipSummary", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipSummary : ThemeClassificationFormatDefinition {
			XmlDocToolTipSummary() : base(ColorType.XmlDocToolTipSummary) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.XmlDocToolTipText, "XmlDocToolTipText", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class XmlDocToolTipText : ThemeClassificationFormatDefinition {
			XmlDocToolTipText() : base(ColorType.XmlDocToolTipText) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Assembly, "Assembly", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Assembly : ThemeClassificationFormatDefinition {
			Assembly() : base(ColorType.Assembly) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.AssemblyExe, "AssemblyExe", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class AssemblyExe : ThemeClassificationFormatDefinition {
			AssemblyExe() : base(ColorType.AssemblyExe) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Module, "Module", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Module : ThemeClassificationFormatDefinition {
			Module() : base(ColorType.Module) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DirectoryPart, "DirectoryPart", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DirectoryPart : ThemeClassificationFormatDefinition {
			DirectoryPart() : base(ColorType.DirectoryPart) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.FileNameNoExtension, "FileNameNoExtension", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class FileNameNoExtension : ThemeClassificationFormatDefinition {
			FileNameNoExtension() : base(ColorType.FileNameNoExtension) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.FileExtension, "FileExtension", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class FileExtension : ThemeClassificationFormatDefinition {
			FileExtension() : base(ColorType.FileExtension) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Error, "Error", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Error : ThemeClassificationFormatDefinition {
			Error() : base(ColorType.Error) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ToStringEval, "ToStringEval", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ToStringEval : ThemeClassificationFormatDefinition {
			ToStringEval() : base(ColorType.ToStringEval) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ReplPrompt1, "ReplPrompt1", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ReplPrompt1 : ThemeClassificationFormatDefinition {
			ReplPrompt1() : base(ColorType.ReplPrompt1) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ReplPrompt2, "ReplPrompt2", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ReplPrompt2 : ThemeClassificationFormatDefinition {
			ReplPrompt2() : base(ColorType.ReplPrompt2) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ReplOutputText, "ReplOutputText", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ReplOutputText : ThemeClassificationFormatDefinition {
			ReplOutputText() : base(ColorType.ReplOutputText) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.ReplScriptOutputText, "ReplScriptOutputText", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class ReplScriptOutputText : ThemeClassificationFormatDefinition {
			ReplScriptOutputText() : base(ColorType.ReplScriptOutputText) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Black, "Black", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Black : ThemeClassificationFormatDefinition {
			Black() : base(ColorType.Black) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Blue, "Blue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Blue : ThemeClassificationFormatDefinition {
			Blue() : base(ColorType.Blue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Cyan, "Cyan", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Cyan : ThemeClassificationFormatDefinition {
			Cyan() : base(ColorType.Cyan) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DarkBlue, "DarkBlue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DarkBlue : ThemeClassificationFormatDefinition {
			DarkBlue() : base(ColorType.DarkBlue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DarkCyan, "DarkCyan", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DarkCyan : ThemeClassificationFormatDefinition {
			DarkCyan() : base(ColorType.DarkCyan) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DarkGray, "DarkGray", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DarkGray : ThemeClassificationFormatDefinition {
			DarkGray() : base(ColorType.DarkGray) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DarkGreen, "DarkGreen", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DarkGreen : ThemeClassificationFormatDefinition {
			DarkGreen() : base(ColorType.DarkGreen) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DarkMagenta, "DarkMagenta", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DarkMagenta : ThemeClassificationFormatDefinition {
			DarkMagenta() : base(ColorType.DarkMagenta) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DarkRed, "DarkRed", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DarkRed : ThemeClassificationFormatDefinition {
			DarkRed() : base(ColorType.DarkRed) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DarkYellow, "DarkYellow", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DarkYellow : ThemeClassificationFormatDefinition {
			DarkYellow() : base(ColorType.DarkYellow) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Gray, "Gray", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Gray : ThemeClassificationFormatDefinition {
			Gray() : base(ColorType.Gray) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Green, "Green", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Green : ThemeClassificationFormatDefinition {
			Green() : base(ColorType.Green) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Magenta, "Magenta", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Magenta : ThemeClassificationFormatDefinition {
			Magenta() : base(ColorType.Magenta) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Red, "Red", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Red : ThemeClassificationFormatDefinition {
			Red() : base(ColorType.Red) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.White, "White", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class White : ThemeClassificationFormatDefinition {
			White() : base(ColorType.White) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.Yellow, "Yellow", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class Yellow : ThemeClassificationFormatDefinition {
			Yellow() : base(ColorType.Yellow) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvBlack, "InvBlack", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvBlack : ThemeClassificationFormatDefinition {
			InvBlack() : base(ColorType.InvBlack) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvBlue, "InvBlue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvBlue : ThemeClassificationFormatDefinition {
			InvBlue() : base(ColorType.InvBlue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvCyan, "InvCyan", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvCyan : ThemeClassificationFormatDefinition {
			InvCyan() : base(ColorType.InvCyan) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvDarkBlue, "InvDarkBlue", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvDarkBlue : ThemeClassificationFormatDefinition {
			InvDarkBlue() : base(ColorType.InvDarkBlue) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvDarkCyan, "InvDarkCyan", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvDarkCyan : ThemeClassificationFormatDefinition {
			InvDarkCyan() : base(ColorType.InvDarkCyan) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvDarkGray, "InvDarkGray", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvDarkGray : ThemeClassificationFormatDefinition {
			InvDarkGray() : base(ColorType.InvDarkGray) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvDarkGreen, "InvDarkGreen", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvDarkGreen : ThemeClassificationFormatDefinition {
			InvDarkGreen() : base(ColorType.InvDarkGreen) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvDarkMagenta, "InvDarkMagenta", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvDarkMagenta : ThemeClassificationFormatDefinition {
			InvDarkMagenta() : base(ColorType.InvDarkMagenta) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvDarkRed, "InvDarkRed", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvDarkRed : ThemeClassificationFormatDefinition {
			InvDarkRed() : base(ColorType.InvDarkRed) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvDarkYellow, "InvDarkYellow", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvDarkYellow : ThemeClassificationFormatDefinition {
			InvDarkYellow() : base(ColorType.InvDarkYellow) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvGray, "InvGray", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvGray : ThemeClassificationFormatDefinition {
			InvGray() : base(ColorType.InvGray) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvGreen, "InvGreen", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvGreen : ThemeClassificationFormatDefinition {
			InvGreen() : base(ColorType.InvGreen) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvMagenta, "InvMagenta", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvMagenta : ThemeClassificationFormatDefinition {
			InvMagenta() : base(ColorType.InvMagenta) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvRed, "InvRed", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvRed : ThemeClassificationFormatDefinition {
			InvRed() : base(ColorType.InvRed) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvWhite, "InvWhite", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvWhite : ThemeClassificationFormatDefinition {
			InvWhite() : base(ColorType.InvWhite) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.InvYellow, "InvYellow", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class InvYellow : ThemeClassificationFormatDefinition {
			InvYellow() : base(ColorType.InvYellow) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogExceptionHandled, "DebugLogExceptionHandled", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogExceptionHandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionHandled() : base(ColorType.DebugLogExceptionHandled) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogExceptionUnhandled, "DebugLogExceptionUnhandled", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogExceptionUnhandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionUnhandled() : base(ColorType.DebugLogExceptionUnhandled) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogStepFiltering, "DebugLogStepFiltering", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogStepFiltering : ThemeClassificationFormatDefinition {
			DebugLogStepFiltering() : base(ColorType.DebugLogStepFiltering) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogLoadModule, "DebugLogLoadModule", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogLoadModule : ThemeClassificationFormatDefinition {
			DebugLogLoadModule() : base(ColorType.DebugLogLoadModule) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogUnloadModule, "DebugLogUnloadModule", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogUnloadModule : ThemeClassificationFormatDefinition {
			DebugLogUnloadModule() : base(ColorType.DebugLogUnloadModule) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogExitProcess, "DebugLogExitProcess", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogExitProcess : ThemeClassificationFormatDefinition {
			DebugLogExitProcess() : base(ColorType.DebugLogExitProcess) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogExitThread, "DebugLogExitThread", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogExitThread : ThemeClassificationFormatDefinition {
			DebugLogExitThread() : base(ColorType.DebugLogExitThread) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogProgramOutput, "DebugLogProgramOutput", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogProgramOutput : ThemeClassificationFormatDefinition {
			DebugLogProgramOutput() : base(ColorType.DebugLogProgramOutput) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogMDA, "DebugLogMDA", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogMDA : ThemeClassificationFormatDefinition {
			DebugLogMDA() : base(ColorType.DebugLogMDA) { }
		}

		[ExportClassificationFormatDefinition(ThemeClassificationTypeNames.DebugLogTimestamp, "DebugLogTimestamp", Order = EditorFormatDefinitionPriority.AfterDefault)]
		sealed class DebugLogTimestamp : ThemeClassificationFormatDefinition {
			DebugLogTimestamp() : base(ColorType.DebugLogTimestamp) { }
		}
	}
}
