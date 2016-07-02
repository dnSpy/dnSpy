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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Classification {
	static class ThemeClassificationFormatDefinitions {
#pragma warning disable CS0169
		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.NaturalLanguage)]
		static ClassificationTypeDefinition NaturalLanguageClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition FormalLanguageClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Literal)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LiteralClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Identifier)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition IdentifierClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.Other)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition OtherClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(PredefinedClassificationTypeNames.WhiteSpace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition WhiteSpaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Text)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition TextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Operator)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition OperatorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Punctuation)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition PunctuationClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Number)]
		[BaseDefinition(PredefinedClassificationTypeNames.Literal)]
		static ClassificationTypeDefinition NumberClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Comment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Keyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition KeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.String)]
		[BaseDefinition(PredefinedClassificationTypeNames.Literal)]
		static ClassificationTypeDefinition StringClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.VerbatimString)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition VerbatimStringClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Char)]
		[BaseDefinition(PredefinedClassificationTypeNames.Literal)]
		static ClassificationTypeDefinition CharClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Namespace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition NamespaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Type)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition TypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SealedType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition SealedTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Delegate)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DelegateClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Enum)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition EnumClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Interface)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InterfaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ValueType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ValueTypeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.TypeGenericParameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition TypeGenericParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.MethodGenericParameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition MethodGenericParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstanceMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ExtensionMethod)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ExtensionMethodClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstanceFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.EnumField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition EnumFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.LiteralField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LiteralFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticField)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticFieldClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceEvent)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstanceEventClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticEvent)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticEventClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InstanceProperty)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InstancePropertyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.StaticProperty)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition StaticPropertyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Local)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LocalClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Parameter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ParameterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.PreprocessorKeyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition PreprocessorKeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.PreprocessorText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition PreprocessorTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Label)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LabelClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.OpCode)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition OpCodeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ILDirective)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ILDirectiveClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ILModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ILModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ExcludedCode)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ExcludedCodeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentAttributeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentAttributeNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentAttributeQuotesClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentAttributeValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentAttributeValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentCDataSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentCDataSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentDelimiter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentDelimiterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentEntityReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentEntityReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentProcessingInstructionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocCommentText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocCommentTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralAttributeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralAttributeNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralAttributeQuotes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralAttributeQuotesClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralAttributeValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralAttributeValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralCDataSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralCDataSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralDelimiter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralDelimiterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralEmbeddedExpressionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralEntityReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralEntityReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralProcessingInstruction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralProcessingInstructionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlLiteralText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlLiteralTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlAttributeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlAttributeNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlAttributeQuotes)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlAttributeQuotesClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlAttributeValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlAttributeValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlCDataSection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlCDataSectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlComment)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlCommentClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDelimiter)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDelimiterClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlKeyword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlKeywordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlProcessingInstruction)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlProcessingInstructionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipColon)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipColonClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipExample)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipExampleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipExceptionCref)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipExceptionCrefClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipReturns)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipReturnsClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipSeeCref)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeCrefClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipSeeLangword)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeLangwordClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipSeeAlso)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeAlsoClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipSeeAlsoCref)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSeeAlsoCrefClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipParamRefName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipParamRefNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipParamName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipParamNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipTypeParamName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipTypeParamNameClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipValue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipValueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipSummary)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipSummaryClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.XmlDocToolTipText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition XmlDocToolTipTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Assembly)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition AssemblyClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.AssemblyExe)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition AssemblyExeClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Module)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DirectoryPart)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DirectoryPartClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.FileNameNoExtension)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition FileNameNoExtensionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.FileExtension)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition FileExtensionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Error)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ErrorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ToStringEval)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ToStringEvalClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplPrompt1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplPrompt1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplPrompt2)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplPrompt2ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplOutputText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplOutputTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplScriptOutputText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplScriptOutputTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Black)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition BlackClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Blue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition BlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Cyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkBlue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkBlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkCyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkCyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkGray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkGrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkGreen)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkGreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkMagenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkMagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkRed)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkRedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DarkYellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DarkYellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Gray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition GrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Green)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition GreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Magenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition MagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Red)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition RedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.White)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition WhiteClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Yellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition YellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvBlack)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvBlackClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvBlue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvBlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvCyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvCyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkBlue)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkBlueClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkCyan)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkCyanClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkGray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkGrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkGreen)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkGreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkMagenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkMagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkRed)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkRedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvDarkYellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvDarkYellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvGray)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvGrayClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvGreen)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvGreenClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvMagenta)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvMagentaClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvRed)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvRedClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvWhite)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvWhiteClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InvYellow)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InvYellowClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExceptionHandled)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExceptionHandledClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExceptionUnhandled)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExceptionUnhandledClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogStepFiltering)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogStepFilteringClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogLoadModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogLoadModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogUnloadModule)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogUnloadModuleClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExitProcess)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExitProcessClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogExitThread)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogExitThreadClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogProgramOutput)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogProgramOutputClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogMDA)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogMDAClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DebugLogTimestamp)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DebugLogTimestampClassificationTypeDefinition;
#pragma warning restore CS0169

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Text)]
		[Name(ThemeClassificationTypeNameKeys.Text)]
		[UserVisible(true)]
		[Order(After = Priority.Low)]
		sealed class Text : ThemeClassificationFormatDefinition {
			Text() : base(ColorType.Text) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Operator)]
		[Name(ThemeClassificationTypeNameKeys.Operator)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.PreprocessorKeyword)]
		sealed class Operator : ThemeClassificationFormatDefinition {
			Operator() : base(ColorType.Operator) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Punctuation)]
		[Name(ThemeClassificationTypeNameKeys.Punctuation)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class Punctuation : ThemeClassificationFormatDefinition {
			Punctuation() : base(ColorType.Punctuation) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Number)]
		[Name(ThemeClassificationTypeNameKeys.Number)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Operator)]
		sealed class Number : ThemeClassificationFormatDefinition {
			Number() : base(ColorType.Number) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Comment)]
		[Name(ThemeClassificationTypeNameKeys.Comment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.ExcludedCode)]
		sealed class Comment : ThemeClassificationFormatDefinition {
			Comment() : base(ColorType.Comment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Keyword)]
		[Name(ThemeClassificationTypeNameKeys.Keyword)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Literal)]
		sealed class Keyword : ThemeClassificationFormatDefinition {
			Keyword() : base(ColorType.Keyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.String)]
		[Name(ThemeClassificationTypeNameKeys.String)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage)]
		sealed class String : ThemeClassificationFormatDefinition {
			String() : base(ColorType.String) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.VerbatimString)]
		[Name(ThemeClassificationTypeNameKeys.VerbatimString)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class VerbatimString : ThemeClassificationFormatDefinition {
			VerbatimString() : base(ColorType.VerbatimString) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Char)]
		[Name(ThemeClassificationTypeNameKeys.Char)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class Char : ThemeClassificationFormatDefinition {
			Char() : base(ColorType.Char) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Namespace)]
		[Name(ThemeClassificationTypeNameKeys.Namespace)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Namespace : ThemeClassificationFormatDefinition {
			Namespace() : base(ColorType.Namespace) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Type)]
		[Name(ThemeClassificationTypeNameKeys.Type)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Type : ThemeClassificationFormatDefinition {
			Type() : base(ColorType.Type) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SealedType)]
		[Name(ThemeClassificationTypeNameKeys.SealedType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class SealedType : ThemeClassificationFormatDefinition {
			SealedType() : base(ColorType.SealedType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticType)]
		[Name(ThemeClassificationTypeNameKeys.StaticType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticType : ThemeClassificationFormatDefinition {
			StaticType() : base(ColorType.StaticType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Delegate)]
		[Name(ThemeClassificationTypeNameKeys.Delegate)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Delegate : ThemeClassificationFormatDefinition {
			Delegate() : base(ColorType.Delegate) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Enum)]
		[Name(ThemeClassificationTypeNameKeys.Enum)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Enum : ThemeClassificationFormatDefinition {
			Enum() : base(ColorType.Enum) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Interface)]
		[Name(ThemeClassificationTypeNameKeys.Interface)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Interface : ThemeClassificationFormatDefinition {
			Interface() : base(ColorType.Interface) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ValueType)]
		[Name(ThemeClassificationTypeNameKeys.ValueType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ValueType : ThemeClassificationFormatDefinition {
			ValueType() : base(ColorType.ValueType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TypeGenericParameter)]
		[Name(ThemeClassificationTypeNameKeys.TypeGenericParameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class TypeGenericParameter : ThemeClassificationFormatDefinition {
			TypeGenericParameter() : base(ColorType.TypeGenericParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.MethodGenericParameter)]
		[Name(ThemeClassificationTypeNameKeys.MethodGenericParameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class MethodGenericParameter : ThemeClassificationFormatDefinition {
			MethodGenericParameter() : base(ColorType.MethodGenericParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceMethod)]
		[Name(ThemeClassificationTypeNameKeys.InstanceMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceMethod : ThemeClassificationFormatDefinition {
			InstanceMethod() : base(ColorType.InstanceMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticMethod)]
		[Name(ThemeClassificationTypeNameKeys.StaticMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticMethod : ThemeClassificationFormatDefinition {
			StaticMethod() : base(ColorType.StaticMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ExtensionMethod)]
		[Name(ThemeClassificationTypeNameKeys.ExtensionMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ExtensionMethod : ThemeClassificationFormatDefinition {
			ExtensionMethod() : base(ColorType.ExtensionMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceField)]
		[Name(ThemeClassificationTypeNameKeys.InstanceField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceField : ThemeClassificationFormatDefinition {
			InstanceField() : base(ColorType.InstanceField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.EnumField)]
		[Name(ThemeClassificationTypeNameKeys.EnumField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class EnumField : ThemeClassificationFormatDefinition {
			EnumField() : base(ColorType.EnumField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.LiteralField)]
		[Name(ThemeClassificationTypeNameKeys.LiteralField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class LiteralField : ThemeClassificationFormatDefinition {
			LiteralField() : base(ColorType.LiteralField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticField)]
		[Name(ThemeClassificationTypeNameKeys.StaticField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticField : ThemeClassificationFormatDefinition {
			StaticField() : base(ColorType.StaticField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceEvent)]
		[Name(ThemeClassificationTypeNameKeys.InstanceEvent)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceEvent : ThemeClassificationFormatDefinition {
			InstanceEvent() : base(ColorType.InstanceEvent) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticEvent)]
		[Name(ThemeClassificationTypeNameKeys.StaticEvent)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticEvent : ThemeClassificationFormatDefinition {
			StaticEvent() : base(ColorType.StaticEvent) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceProperty)]
		[Name(ThemeClassificationTypeNameKeys.InstanceProperty)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceProperty : ThemeClassificationFormatDefinition {
			InstanceProperty() : base(ColorType.InstanceProperty) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticProperty)]
		[Name(ThemeClassificationTypeNameKeys.StaticProperty)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticProperty : ThemeClassificationFormatDefinition {
			StaticProperty() : base(ColorType.StaticProperty) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Local)]
		[Name(ThemeClassificationTypeNameKeys.Local)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Local : ThemeClassificationFormatDefinition {
			Local() : base(ColorType.Local) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Parameter)]
		[Name(ThemeClassificationTypeNameKeys.Parameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Parameter : ThemeClassificationFormatDefinition {
			Parameter() : base(ColorType.Parameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.PreprocessorKeyword)]
		[Name(ThemeClassificationTypeNameKeys.PreprocessorKeyword)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.String)]
		sealed class PreprocessorKeyword : ThemeClassificationFormatDefinition {
			PreprocessorKeyword() : base(ColorType.PreprocessorKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.PreprocessorText)]
		[Name(ThemeClassificationTypeNameKeys.PreprocessorText)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class PreprocessorText : ThemeClassificationFormatDefinition {
			PreprocessorText() : base(ColorType.PreprocessorText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Label)]
		[Name(ThemeClassificationTypeNameKeys.Label)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Label : ThemeClassificationFormatDefinition {
			Label() : base(ColorType.Label) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.OpCode)]
		[Name(ThemeClassificationTypeNameKeys.OpCode)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class OpCode : ThemeClassificationFormatDefinition {
			OpCode() : base(ColorType.OpCode) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ILDirective)]
		[Name(ThemeClassificationTypeNameKeys.ILDirective)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ILDirective : ThemeClassificationFormatDefinition {
			ILDirective() : base(ColorType.ILDirective) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ILModule)]
		[Name(ThemeClassificationTypeNameKeys.ILModule)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ILModule : ThemeClassificationFormatDefinition {
			ILModule() : base(ColorType.ILModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ExcludedCode)]
		[Name(ThemeClassificationTypeNameKeys.ExcludedCode)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Identifier)]
		sealed class ExcludedCode : ThemeClassificationFormatDefinition {
			ExcludedCode() : base(ColorType.ExcludedCode) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeName)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeName : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeName() : base(ColorType.XmlDocCommentAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeQuotes() : base(ColorType.XmlDocCommentAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeValue : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeValue() : base(ColorType.XmlDocCommentAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentCDataSection)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentCDataSection : ThemeClassificationFormatDefinition {
			XmlDocCommentCDataSection() : base(ColorType.XmlDocCommentCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentComment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentComment : ThemeClassificationFormatDefinition {
			XmlDocCommentComment() : base(ColorType.XmlDocCommentComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentDelimiter)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentDelimiter : ThemeClassificationFormatDefinition {
			XmlDocCommentDelimiter() : base(ColorType.XmlDocCommentDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentEntityReference)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentEntityReference)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentEntityReference : ThemeClassificationFormatDefinition {
			XmlDocCommentEntityReference() : base(ColorType.XmlDocCommentEntityReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentName : ThemeClassificationFormatDefinition {
			XmlDocCommentName() : base(ColorType.XmlDocCommentName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlDocCommentProcessingInstruction() : base(ColorType.XmlDocCommentProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentText)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentText)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentText : ThemeClassificationFormatDefinition {
			XmlDocCommentText() : base(ColorType.XmlDocCommentText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeName : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeName() : base(ColorType.XmlLiteralAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeQuotes() : base(ColorType.XmlLiteralAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeValue)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeValue : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeValue() : base(ColorType.XmlLiteralAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralCDataSection)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralCDataSection : ThemeClassificationFormatDefinition {
			XmlLiteralCDataSection() : base(ColorType.XmlLiteralCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralComment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralComment : ThemeClassificationFormatDefinition {
			XmlLiteralComment() : base(ColorType.XmlLiteralComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralDelimiter)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralDelimiter : ThemeClassificationFormatDefinition {
			XmlLiteralDelimiter() : base(ColorType.XmlLiteralDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralEmbeddedExpression)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralEmbeddedExpression : ThemeClassificationFormatDefinition {
			XmlLiteralEmbeddedExpression() : base(ColorType.XmlLiteralEmbeddedExpression) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralEntityReference)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralEntityReference)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralEntityReference : ThemeClassificationFormatDefinition {
			XmlLiteralEntityReference() : base(ColorType.XmlLiteralEntityReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralName)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralName : ThemeClassificationFormatDefinition {
			XmlLiteralName() : base(ColorType.XmlLiteralName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlLiteralProcessingInstruction() : base(ColorType.XmlLiteralProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralText)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralText)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralText : ThemeClassificationFormatDefinition {
			XmlLiteralText() : base(ColorType.XmlLiteralText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeName : ThemeClassificationFormatDefinition {
			XmlAttributeName() : base(ColorType.XmlAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlAttributeQuotes() : base(ColorType.XmlAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeValue : ThemeClassificationFormatDefinition {
			XmlAttributeValue() : base(ColorType.XmlAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlCDataSection)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlCDataSection : ThemeClassificationFormatDefinition {
			XmlCDataSection() : base(ColorType.XmlCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlComment)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlComment : ThemeClassificationFormatDefinition {
			XmlComment() : base(ColorType.XmlComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlDelimiter)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDelimiter : ThemeClassificationFormatDefinition {
			XmlDelimiter() : base(ColorType.XmlDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlKeyword)]
		[Name(ThemeClassificationTypeNameKeys.XmlKeyword)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlKeyword : ThemeClassificationFormatDefinition {
			XmlKeyword() : base(ColorType.XmlKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlName)]
		[Name(ThemeClassificationTypeNameKeys.XmlName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlName : ThemeClassificationFormatDefinition {
			XmlName() : base(ColorType.XmlName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlProcessingInstruction() : base(ColorType.XmlProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlText)]
		[Name(ThemeClassificationTypeNameKeys.XmlText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlText : ThemeClassificationFormatDefinition {
			XmlText() : base(ColorType.XmlText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipColon)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipColon)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipColon : ThemeClassificationFormatDefinition {
			XmlDocToolTipColon() : base(ColorType.XmlDocToolTipColon) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipExample)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipExample)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipExample : ThemeClassificationFormatDefinition {
			XmlDocToolTipExample() : base(ColorType.XmlDocToolTipExample) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipExceptionCref)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipExceptionCref)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipExceptionCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipExceptionCref() : base(ColorType.XmlDocToolTipExceptionCref) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipReturns)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipReturns)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipReturns : ThemeClassificationFormatDefinition {
			XmlDocToolTipReturns() : base(ColorType.XmlDocToolTipReturns) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeCref)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeCref)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeCref() : base(ColorType.XmlDocToolTipSeeCref) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeLangword)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeLangword)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeLangword : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeLangword() : base(ColorType.XmlDocToolTipSeeLangword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeAlso)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeAlso)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeAlso : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeAlso() : base(ColorType.XmlDocToolTipSeeAlso) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeAlsoCref)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeAlsoCref)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeAlsoCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeAlsoCref() : base(ColorType.XmlDocToolTipSeeAlsoCref) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipParamRefName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipParamRefName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipParamRefName : ThemeClassificationFormatDefinition {
			XmlDocToolTipParamRefName() : base(ColorType.XmlDocToolTipParamRefName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipParamName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipParamName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipParamName : ThemeClassificationFormatDefinition {
			XmlDocToolTipParamName() : base(ColorType.XmlDocToolTipParamName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipTypeParamName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipTypeParamName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipTypeParamName : ThemeClassificationFormatDefinition {
			XmlDocToolTipTypeParamName() : base(ColorType.XmlDocToolTipTypeParamName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipValue : ThemeClassificationFormatDefinition {
			XmlDocToolTipValue() : base(ColorType.XmlDocToolTipValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSummary)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSummary)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSummary : ThemeClassificationFormatDefinition {
			XmlDocToolTipSummary() : base(ColorType.XmlDocToolTipSummary) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipText)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipText : ThemeClassificationFormatDefinition {
			XmlDocToolTipText() : base(ColorType.XmlDocToolTipText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Assembly)]
		[Name(ThemeClassificationTypeNameKeys.Assembly)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Assembly : ThemeClassificationFormatDefinition {
			Assembly() : base(ColorType.Assembly) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AssemblyExe)]
		[Name(ThemeClassificationTypeNameKeys.AssemblyExe)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AssemblyExe : ThemeClassificationFormatDefinition {
			AssemblyExe() : base(ColorType.AssemblyExe) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Module)]
		[Name(ThemeClassificationTypeNameKeys.Module)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Module : ThemeClassificationFormatDefinition {
			Module() : base(ColorType.Module) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DirectoryPart)]
		[Name(ThemeClassificationTypeNameKeys.DirectoryPart)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DirectoryPart : ThemeClassificationFormatDefinition {
			DirectoryPart() : base(ColorType.DirectoryPart) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.FileNameNoExtension)]
		[Name(ThemeClassificationTypeNameKeys.FileNameNoExtension)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class FileNameNoExtension : ThemeClassificationFormatDefinition {
			FileNameNoExtension() : base(ColorType.FileNameNoExtension) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.FileExtension)]
		[Name(ThemeClassificationTypeNameKeys.FileExtension)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class FileExtension : ThemeClassificationFormatDefinition {
			FileExtension() : base(ColorType.FileExtension) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Error)]
		[Name(ThemeClassificationTypeNameKeys.Error)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Error : ThemeClassificationFormatDefinition {
			Error() : base(ColorType.Error) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ToStringEval)]
		[Name(ThemeClassificationTypeNameKeys.ToStringEval)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ToStringEval : ThemeClassificationFormatDefinition {
			ToStringEval() : base(ColorType.ToStringEval) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplPrompt1)]
		[Name(ThemeClassificationTypeNameKeys.ReplPrompt1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplPrompt1 : ThemeClassificationFormatDefinition {
			ReplPrompt1() : base(ColorType.ReplPrompt1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplPrompt2)]
		[Name(ThemeClassificationTypeNameKeys.ReplPrompt2)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplPrompt2 : ThemeClassificationFormatDefinition {
			ReplPrompt2() : base(ColorType.ReplPrompt2) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplOutputText)]
		[Name(ThemeClassificationTypeNameKeys.ReplOutputText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplOutputText : ThemeClassificationFormatDefinition {
			ReplOutputText() : base(ColorType.ReplOutputText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplScriptOutputText)]
		[Name(ThemeClassificationTypeNameKeys.ReplScriptOutputText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplScriptOutputText : ThemeClassificationFormatDefinition {
			ReplScriptOutputText() : base(ColorType.ReplScriptOutputText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Black)]
		[Name(ThemeClassificationTypeNameKeys.Black)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Black : ThemeClassificationFormatDefinition {
			Black() : base(ColorType.Black) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Blue)]
		[Name(ThemeClassificationTypeNameKeys.Blue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Blue : ThemeClassificationFormatDefinition {
			Blue() : base(ColorType.Blue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Cyan)]
		[Name(ThemeClassificationTypeNameKeys.Cyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Cyan : ThemeClassificationFormatDefinition {
			Cyan() : base(ColorType.Cyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkBlue)]
		[Name(ThemeClassificationTypeNameKeys.DarkBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkBlue : ThemeClassificationFormatDefinition {
			DarkBlue() : base(ColorType.DarkBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkCyan)]
		[Name(ThemeClassificationTypeNameKeys.DarkCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkCyan : ThemeClassificationFormatDefinition {
			DarkCyan() : base(ColorType.DarkCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkGray)]
		[Name(ThemeClassificationTypeNameKeys.DarkGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkGray : ThemeClassificationFormatDefinition {
			DarkGray() : base(ColorType.DarkGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkGreen)]
		[Name(ThemeClassificationTypeNameKeys.DarkGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkGreen : ThemeClassificationFormatDefinition {
			DarkGreen() : base(ColorType.DarkGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkMagenta)]
		[Name(ThemeClassificationTypeNameKeys.DarkMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkMagenta : ThemeClassificationFormatDefinition {
			DarkMagenta() : base(ColorType.DarkMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkRed)]
		[Name(ThemeClassificationTypeNameKeys.DarkRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkRed : ThemeClassificationFormatDefinition {
			DarkRed() : base(ColorType.DarkRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkYellow)]
		[Name(ThemeClassificationTypeNameKeys.DarkYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkYellow : ThemeClassificationFormatDefinition {
			DarkYellow() : base(ColorType.DarkYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Gray)]
		[Name(ThemeClassificationTypeNameKeys.Gray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Gray : ThemeClassificationFormatDefinition {
			Gray() : base(ColorType.Gray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Green)]
		[Name(ThemeClassificationTypeNameKeys.Green)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Green : ThemeClassificationFormatDefinition {
			Green() : base(ColorType.Green) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Magenta)]
		[Name(ThemeClassificationTypeNameKeys.Magenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Magenta : ThemeClassificationFormatDefinition {
			Magenta() : base(ColorType.Magenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Red)]
		[Name(ThemeClassificationTypeNameKeys.Red)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Red : ThemeClassificationFormatDefinition {
			Red() : base(ColorType.Red) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.White)]
		[Name(ThemeClassificationTypeNameKeys.White)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class White : ThemeClassificationFormatDefinition {
			White() : base(ColorType.White) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Yellow)]
		[Name(ThemeClassificationTypeNameKeys.Yellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Yellow : ThemeClassificationFormatDefinition {
			Yellow() : base(ColorType.Yellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvBlack)]
		[Name(ThemeClassificationTypeNameKeys.InvBlack)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvBlack : ThemeClassificationFormatDefinition {
			InvBlack() : base(ColorType.InvBlack) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvBlue)]
		[Name(ThemeClassificationTypeNameKeys.InvBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvBlue : ThemeClassificationFormatDefinition {
			InvBlue() : base(ColorType.InvBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvCyan)]
		[Name(ThemeClassificationTypeNameKeys.InvCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvCyan : ThemeClassificationFormatDefinition {
			InvCyan() : base(ColorType.InvCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkBlue)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkBlue : ThemeClassificationFormatDefinition {
			InvDarkBlue() : base(ColorType.InvDarkBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkCyan)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkCyan : ThemeClassificationFormatDefinition {
			InvDarkCyan() : base(ColorType.InvDarkCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkGray)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkGray : ThemeClassificationFormatDefinition {
			InvDarkGray() : base(ColorType.InvDarkGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkGreen)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkGreen : ThemeClassificationFormatDefinition {
			InvDarkGreen() : base(ColorType.InvDarkGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkMagenta)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkMagenta : ThemeClassificationFormatDefinition {
			InvDarkMagenta() : base(ColorType.InvDarkMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkRed)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkRed : ThemeClassificationFormatDefinition {
			InvDarkRed() : base(ColorType.InvDarkRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkYellow)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkYellow : ThemeClassificationFormatDefinition {
			InvDarkYellow() : base(ColorType.InvDarkYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvGray)]
		[Name(ThemeClassificationTypeNameKeys.InvGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvGray : ThemeClassificationFormatDefinition {
			InvGray() : base(ColorType.InvGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvGreen)]
		[Name(ThemeClassificationTypeNameKeys.InvGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvGreen : ThemeClassificationFormatDefinition {
			InvGreen() : base(ColorType.InvGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvMagenta)]
		[Name(ThemeClassificationTypeNameKeys.InvMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvMagenta : ThemeClassificationFormatDefinition {
			InvMagenta() : base(ColorType.InvMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvRed)]
		[Name(ThemeClassificationTypeNameKeys.InvRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvRed : ThemeClassificationFormatDefinition {
			InvRed() : base(ColorType.InvRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvWhite)]
		[Name(ThemeClassificationTypeNameKeys.InvWhite)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvWhite : ThemeClassificationFormatDefinition {
			InvWhite() : base(ColorType.InvWhite) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvYellow)]
		[Name(ThemeClassificationTypeNameKeys.InvYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvYellow : ThemeClassificationFormatDefinition {
			InvYellow() : base(ColorType.InvYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExceptionHandled)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExceptionHandled)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExceptionHandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionHandled() : base(ColorType.DebugLogExceptionHandled) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExceptionUnhandled)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExceptionUnhandled)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExceptionUnhandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionUnhandled() : base(ColorType.DebugLogExceptionUnhandled) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogStepFiltering)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogStepFiltering)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogStepFiltering : ThemeClassificationFormatDefinition {
			DebugLogStepFiltering() : base(ColorType.DebugLogStepFiltering) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogLoadModule)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogLoadModule)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogLoadModule : ThemeClassificationFormatDefinition {
			DebugLogLoadModule() : base(ColorType.DebugLogLoadModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogUnloadModule)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogUnloadModule)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogUnloadModule : ThemeClassificationFormatDefinition {
			DebugLogUnloadModule() : base(ColorType.DebugLogUnloadModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExitProcess)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExitProcess)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExitProcess : ThemeClassificationFormatDefinition {
			DebugLogExitProcess() : base(ColorType.DebugLogExitProcess) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExitThread)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExitThread)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExitThread : ThemeClassificationFormatDefinition {
			DebugLogExitThread() : base(ColorType.DebugLogExitThread) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogProgramOutput)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogProgramOutput)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogProgramOutput : ThemeClassificationFormatDefinition {
			DebugLogProgramOutput() : base(ColorType.DebugLogProgramOutput) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogMDA)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogMDA)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogMDA : ThemeClassificationFormatDefinition {
			DebugLogMDA() : base(ColorType.DebugLogMDA) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogTimestamp)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogTimestamp)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogTimestamp : ThemeClassificationFormatDefinition {
			DebugLogTimestamp() : base(ColorType.DebugLogTimestamp) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "----------------")]
		[Name(Priority.Default)]
		[UserVisible(false)]
		[Order(After = Priority.Low, Before = Priority.High)]
		sealed class PriorityDefault : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "----------------")]
		[Name(Priority.High)]
		[UserVisible(false)]
		[Order(After = Priority.Default)]
		sealed class PriorityHigh : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "----------------")]
		[Name(Priority.Low)]
		[UserVisible(false)]
		[Order(Before = Priority.Default)]
		sealed class PriorityLow : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.FormalLanguage)]
		[Name(LanguagePriority.FormalLanguage)]
		[UserVisible(false)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = Priority.High)]
		sealed class FormalLanguage : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.NaturalLanguage)]
		[Name(LanguagePriority.NaturalLanguage)]
		[UserVisible(false)]
		[Order(After = Priority.Default, Before = LanguagePriority.FormalLanguage)]
		sealed class NaturalLanguage : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Identifier)]
		[Name(ThemeClassificationTypeNameKeys.Identifier)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Identifier : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Literal)]
		[Name(ThemeClassificationTypeNameKeys.Literal)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Number)]
		sealed class Literal : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Other)]
		[Name(PredefinedClassificationTypeNames.Other)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Other : ClassificationFormatDefinition {
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.WhiteSpace)]
		[Name(PredefinedClassificationTypeNames.WhiteSpace)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class WhiteSpace : ClassificationFormatDefinition {
		}
	}
}
