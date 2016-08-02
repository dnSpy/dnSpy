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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Classification {
	static class ThemeClassificationFormatDefinitions {
#pragma warning disable 0169
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

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.LineNumber)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LineNumberClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplLineNumberInput1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplLineNumberInput1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplLineNumberInput2)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplLineNumberInput2ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ReplLineNumberOutput)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ReplLineNumberOutputClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.Link)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LinkClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.VisibleWhitespace)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition VisibleWhitespaceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.SelectedText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition SelectedTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.InactiveSelectedText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition InactiveSelectedTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HighlightedReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HighlightedReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HighlightedWrittenReference)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HighlightedWrittenReferenceClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HighlightedDefinition)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HighlightedDefinitionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CurrentStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CurrentStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CallReturn)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CallReturnClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CallReturnMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CallReturnMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.ActiveStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition ActiveStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointStatement)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition BreakpointStatementClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition BreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.DisabledBreakpointStatementMarker)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition DisabledBreakpointStatementMarkerClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentLine)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CurrentLineClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.CurrentLineNoFocus)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition CurrentLineNoFocusClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexText)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexTextClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexOffset)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexOffsetClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexByte0)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexByte0ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexByte1)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexByte1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexByteError)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexByteErrorClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexAscii)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexAsciiClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexCaret)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexCaretClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexInactiveCaret)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexInactiveCaretClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.HexSelection)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexSelectionClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.GlyphMargin)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition GlyphMarginClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.BraceMatching)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition BraceMatchingClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(ThemeClassificationTypeNames.LineSeparator)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LineSeparatorClassificationTypeDefinition;
#pragma warning restore 0169

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Text)]
		[Name(ThemeClassificationTypeNameKeys.Text)]
		[UserVisible(true)]
		[Order(After = Priority.Low)]
		sealed class Text : ThemeClassificationFormatDefinition {
			Text() : base(TextColor.Text) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Operator)]
		[Name(ThemeClassificationTypeNameKeys.Operator)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.PreprocessorKeyword)]
		sealed class Operator : ThemeClassificationFormatDefinition {
			Operator() : base(TextColor.Operator) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Punctuation)]
		[Name(ThemeClassificationTypeNameKeys.Punctuation)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class Punctuation : ThemeClassificationFormatDefinition {
			Punctuation() : base(TextColor.Punctuation) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Number)]
		[Name(ThemeClassificationTypeNameKeys.Number)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Operator)]
		sealed class Number : ThemeClassificationFormatDefinition {
			Number() : base(TextColor.Number) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Comment)]
		[Name(ThemeClassificationTypeNameKeys.Comment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.ExcludedCode)]
		sealed class Comment : ThemeClassificationFormatDefinition {
			Comment() : base(TextColor.Comment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Keyword)]
		[Name(ThemeClassificationTypeNameKeys.Keyword)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Literal)]
		sealed class Keyword : ThemeClassificationFormatDefinition {
			Keyword() : base(TextColor.Keyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.String)]
		[Name(ThemeClassificationTypeNameKeys.String)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage)]
		sealed class String : ThemeClassificationFormatDefinition {
			String() : base(TextColor.String) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.VerbatimString)]
		[Name(ThemeClassificationTypeNameKeys.VerbatimString)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class VerbatimString : ThemeClassificationFormatDefinition {
			VerbatimString() : base(TextColor.VerbatimString) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Char)]
		[Name(ThemeClassificationTypeNameKeys.Char)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class Char : ThemeClassificationFormatDefinition {
			Char() : base(TextColor.Char) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Namespace)]
		[Name(ThemeClassificationTypeNameKeys.Namespace)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Namespace : ThemeClassificationFormatDefinition {
			Namespace() : base(TextColor.Namespace) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Type)]
		[Name(ThemeClassificationTypeNameKeys.Type)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Type : ThemeClassificationFormatDefinition {
			Type() : base(TextColor.Type) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SealedType)]
		[Name(ThemeClassificationTypeNameKeys.SealedType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class SealedType : ThemeClassificationFormatDefinition {
			SealedType() : base(TextColor.SealedType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticType)]
		[Name(ThemeClassificationTypeNameKeys.StaticType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticType : ThemeClassificationFormatDefinition {
			StaticType() : base(TextColor.StaticType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Delegate)]
		[Name(ThemeClassificationTypeNameKeys.Delegate)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Delegate : ThemeClassificationFormatDefinition {
			Delegate() : base(TextColor.Delegate) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Enum)]
		[Name(ThemeClassificationTypeNameKeys.Enum)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Enum : ThemeClassificationFormatDefinition {
			Enum() : base(TextColor.Enum) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Interface)]
		[Name(ThemeClassificationTypeNameKeys.Interface)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Interface : ThemeClassificationFormatDefinition {
			Interface() : base(TextColor.Interface) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ValueType)]
		[Name(ThemeClassificationTypeNameKeys.ValueType)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ValueType : ThemeClassificationFormatDefinition {
			ValueType() : base(TextColor.ValueType) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.TypeGenericParameter)]
		[Name(ThemeClassificationTypeNameKeys.TypeGenericParameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class TypeGenericParameter : ThemeClassificationFormatDefinition {
			TypeGenericParameter() : base(TextColor.TypeGenericParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.MethodGenericParameter)]
		[Name(ThemeClassificationTypeNameKeys.MethodGenericParameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class MethodGenericParameter : ThemeClassificationFormatDefinition {
			MethodGenericParameter() : base(TextColor.MethodGenericParameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceMethod)]
		[Name(ThemeClassificationTypeNameKeys.InstanceMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceMethod : ThemeClassificationFormatDefinition {
			InstanceMethod() : base(TextColor.InstanceMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticMethod)]
		[Name(ThemeClassificationTypeNameKeys.StaticMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticMethod : ThemeClassificationFormatDefinition {
			StaticMethod() : base(TextColor.StaticMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ExtensionMethod)]
		[Name(ThemeClassificationTypeNameKeys.ExtensionMethod)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ExtensionMethod : ThemeClassificationFormatDefinition {
			ExtensionMethod() : base(TextColor.ExtensionMethod) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceField)]
		[Name(ThemeClassificationTypeNameKeys.InstanceField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceField : ThemeClassificationFormatDefinition {
			InstanceField() : base(TextColor.InstanceField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.EnumField)]
		[Name(ThemeClassificationTypeNameKeys.EnumField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class EnumField : ThemeClassificationFormatDefinition {
			EnumField() : base(TextColor.EnumField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.LiteralField)]
		[Name(ThemeClassificationTypeNameKeys.LiteralField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class LiteralField : ThemeClassificationFormatDefinition {
			LiteralField() : base(TextColor.LiteralField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticField)]
		[Name(ThemeClassificationTypeNameKeys.StaticField)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticField : ThemeClassificationFormatDefinition {
			StaticField() : base(TextColor.StaticField) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceEvent)]
		[Name(ThemeClassificationTypeNameKeys.InstanceEvent)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceEvent : ThemeClassificationFormatDefinition {
			InstanceEvent() : base(TextColor.InstanceEvent) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticEvent)]
		[Name(ThemeClassificationTypeNameKeys.StaticEvent)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticEvent : ThemeClassificationFormatDefinition {
			StaticEvent() : base(TextColor.StaticEvent) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InstanceProperty)]
		[Name(ThemeClassificationTypeNameKeys.InstanceProperty)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class InstanceProperty : ThemeClassificationFormatDefinition {
			InstanceProperty() : base(TextColor.InstanceProperty) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.StaticProperty)]
		[Name(ThemeClassificationTypeNameKeys.StaticProperty)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class StaticProperty : ThemeClassificationFormatDefinition {
			StaticProperty() : base(TextColor.StaticProperty) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Local)]
		[Name(ThemeClassificationTypeNameKeys.Local)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Local : ThemeClassificationFormatDefinition {
			Local() : base(TextColor.Local) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Parameter)]
		[Name(ThemeClassificationTypeNameKeys.Parameter)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Parameter : ThemeClassificationFormatDefinition {
			Parameter() : base(TextColor.Parameter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.PreprocessorKeyword)]
		[Name(ThemeClassificationTypeNameKeys.PreprocessorKeyword)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.String)]
		sealed class PreprocessorKeyword : ThemeClassificationFormatDefinition {
			PreprocessorKeyword() : base(TextColor.PreprocessorKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.PreprocessorText)]
		[Name(ThemeClassificationTypeNameKeys.PreprocessorText)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class PreprocessorText : ThemeClassificationFormatDefinition {
			PreprocessorText() : base(TextColor.PreprocessorText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Label)]
		[Name(ThemeClassificationTypeNameKeys.Label)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Label : ThemeClassificationFormatDefinition {
			Label() : base(TextColor.Label) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.OpCode)]
		[Name(ThemeClassificationTypeNameKeys.OpCode)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class OpCode : ThemeClassificationFormatDefinition {
			OpCode() : base(TextColor.OpCode) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ILDirective)]
		[Name(ThemeClassificationTypeNameKeys.ILDirective)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ILDirective : ThemeClassificationFormatDefinition {
			ILDirective() : base(TextColor.ILDirective) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ILModule)]
		[Name(ThemeClassificationTypeNameKeys.ILModule)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class ILModule : ThemeClassificationFormatDefinition {
			ILModule() : base(TextColor.ILModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ExcludedCode)]
		[Name(ThemeClassificationTypeNameKeys.ExcludedCode)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = ThemeClassificationTypeNameKeys.Identifier)]
		sealed class ExcludedCode : ThemeClassificationFormatDefinition {
			ExcludedCode() : base(TextColor.ExcludedCode) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeName)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeName : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeName() : base(TextColor.XmlDocCommentAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeQuotes() : base(TextColor.XmlDocCommentAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentAttributeValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentAttributeValue : ThemeClassificationFormatDefinition {
			XmlDocCommentAttributeValue() : base(TextColor.XmlDocCommentAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentCDataSection)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentCDataSection : ThemeClassificationFormatDefinition {
			XmlDocCommentCDataSection() : base(TextColor.XmlDocCommentCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentComment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentComment : ThemeClassificationFormatDefinition {
			XmlDocCommentComment() : base(TextColor.XmlDocCommentComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentDelimiter)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentDelimiter : ThemeClassificationFormatDefinition {
			XmlDocCommentDelimiter() : base(TextColor.XmlDocCommentDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentEntityReference)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentEntityReference)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentEntityReference : ThemeClassificationFormatDefinition {
			XmlDocCommentEntityReference() : base(TextColor.XmlDocCommentEntityReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentName : ThemeClassificationFormatDefinition {
			XmlDocCommentName() : base(TextColor.XmlDocCommentName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlDocCommentProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlDocCommentProcessingInstruction() : base(TextColor.XmlDocCommentProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocCommentText)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocCommentText)]
		[UserVisible(true)]
		[Order(After = Priority.Default, Before = Priority.High)]
		sealed class XmlDocCommentText : ThemeClassificationFormatDefinition {
			XmlDocCommentText() : base(TextColor.XmlDocCommentText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeName : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeName() : base(TextColor.XmlLiteralAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeQuotes() : base(TextColor.XmlLiteralAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralAttributeValue)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralAttributeValue : ThemeClassificationFormatDefinition {
			XmlLiteralAttributeValue() : base(TextColor.XmlLiteralAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralCDataSection)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralCDataSection : ThemeClassificationFormatDefinition {
			XmlLiteralCDataSection() : base(TextColor.XmlLiteralCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralComment)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralComment : ThemeClassificationFormatDefinition {
			XmlLiteralComment() : base(TextColor.XmlLiteralComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralDelimiter)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralDelimiter : ThemeClassificationFormatDefinition {
			XmlLiteralDelimiter() : base(TextColor.XmlLiteralDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralEmbeddedExpression)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralEmbeddedExpression : ThemeClassificationFormatDefinition {
			XmlLiteralEmbeddedExpression() : base(TextColor.XmlLiteralEmbeddedExpression) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralEntityReference)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralEntityReference)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralEntityReference : ThemeClassificationFormatDefinition {
			XmlLiteralEntityReference() : base(TextColor.XmlLiteralEntityReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralName)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralName)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralName : ThemeClassificationFormatDefinition {
			XmlLiteralName() : base(TextColor.XmlLiteralName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlLiteralProcessingInstruction() : base(TextColor.XmlLiteralProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlLiteralText)]
		[Name(ThemeClassificationTypeNameKeys.XmlLiteralText)]
		[UserVisible(true)]
		[Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
		sealed class XmlLiteralText : ThemeClassificationFormatDefinition {
			XmlLiteralText() : base(TextColor.XmlLiteralText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeName)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeName : ThemeClassificationFormatDefinition {
			XmlAttributeName() : base(TextColor.XmlAttributeName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeQuotes)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeQuotes)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeQuotes : ThemeClassificationFormatDefinition {
			XmlAttributeQuotes() : base(TextColor.XmlAttributeQuotes) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlAttributeValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlAttributeValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlAttributeValue : ThemeClassificationFormatDefinition {
			XmlAttributeValue() : base(TextColor.XmlAttributeValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlCDataSection)]
		[Name(ThemeClassificationTypeNameKeys.XmlCDataSection)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlCDataSection : ThemeClassificationFormatDefinition {
			XmlCDataSection() : base(TextColor.XmlCDataSection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlComment)]
		[Name(ThemeClassificationTypeNameKeys.XmlComment)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlComment : ThemeClassificationFormatDefinition {
			XmlComment() : base(TextColor.XmlComment) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDelimiter)]
		[Name(ThemeClassificationTypeNameKeys.XmlDelimiter)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDelimiter : ThemeClassificationFormatDefinition {
			XmlDelimiter() : base(TextColor.XmlDelimiter) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlKeyword)]
		[Name(ThemeClassificationTypeNameKeys.XmlKeyword)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlKeyword : ThemeClassificationFormatDefinition {
			XmlKeyword() : base(TextColor.XmlKeyword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlName)]
		[Name(ThemeClassificationTypeNameKeys.XmlName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlName : ThemeClassificationFormatDefinition {
			XmlName() : base(TextColor.XmlName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlProcessingInstruction)]
		[Name(ThemeClassificationTypeNameKeys.XmlProcessingInstruction)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlProcessingInstruction : ThemeClassificationFormatDefinition {
			XmlProcessingInstruction() : base(TextColor.XmlProcessingInstruction) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlText)]
		[Name(ThemeClassificationTypeNameKeys.XmlText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlText : ThemeClassificationFormatDefinition {
			XmlText() : base(TextColor.XmlText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipColon)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipColon)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipColon : ThemeClassificationFormatDefinition {
			XmlDocToolTipColon() : base(TextColor.XmlDocToolTipColon) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipExample)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipExample)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipExample : ThemeClassificationFormatDefinition {
			XmlDocToolTipExample() : base(TextColor.XmlDocToolTipExample) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipExceptionCref)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipExceptionCref)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipExceptionCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipExceptionCref() : base(TextColor.XmlDocToolTipExceptionCref) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipReturns)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipReturns)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipReturns : ThemeClassificationFormatDefinition {
			XmlDocToolTipReturns() : base(TextColor.XmlDocToolTipReturns) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeCref)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeCref)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeCref() : base(TextColor.XmlDocToolTipSeeCref) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeLangword)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeLangword)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeLangword : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeLangword() : base(TextColor.XmlDocToolTipSeeLangword) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeAlso)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeAlso)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeAlso : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeAlso() : base(TextColor.XmlDocToolTipSeeAlso) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSeeAlsoCref)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSeeAlsoCref)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSeeAlsoCref : ThemeClassificationFormatDefinition {
			XmlDocToolTipSeeAlsoCref() : base(TextColor.XmlDocToolTipSeeAlsoCref) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipParamRefName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipParamRefName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipParamRefName : ThemeClassificationFormatDefinition {
			XmlDocToolTipParamRefName() : base(TextColor.XmlDocToolTipParamRefName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipParamName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipParamName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipParamName : ThemeClassificationFormatDefinition {
			XmlDocToolTipParamName() : base(TextColor.XmlDocToolTipParamName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipTypeParamName)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipTypeParamName)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipTypeParamName : ThemeClassificationFormatDefinition {
			XmlDocToolTipTypeParamName() : base(TextColor.XmlDocToolTipTypeParamName) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipValue)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipValue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipValue : ThemeClassificationFormatDefinition {
			XmlDocToolTipValue() : base(TextColor.XmlDocToolTipValue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipSummary)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipSummary)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipSummary : ThemeClassificationFormatDefinition {
			XmlDocToolTipSummary() : base(TextColor.XmlDocToolTipSummary) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.XmlDocToolTipText)]
		[Name(ThemeClassificationTypeNameKeys.XmlDocToolTipText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class XmlDocToolTipText : ThemeClassificationFormatDefinition {
			XmlDocToolTipText() : base(TextColor.XmlDocToolTipText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Assembly)]
		[Name(ThemeClassificationTypeNameKeys.Assembly)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Assembly : ThemeClassificationFormatDefinition {
			Assembly() : base(TextColor.Assembly) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.AssemblyExe)]
		[Name(ThemeClassificationTypeNameKeys.AssemblyExe)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class AssemblyExe : ThemeClassificationFormatDefinition {
			AssemblyExe() : base(TextColor.AssemblyExe) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Module)]
		[Name(ThemeClassificationTypeNameKeys.Module)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class Module : ThemeClassificationFormatDefinition {
			Module() : base(TextColor.Module) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DirectoryPart)]
		[Name(ThemeClassificationTypeNameKeys.DirectoryPart)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class DirectoryPart : ThemeClassificationFormatDefinition {
			DirectoryPart() : base(TextColor.DirectoryPart) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.FileNameNoExtension)]
		[Name(ThemeClassificationTypeNameKeys.FileNameNoExtension)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class FileNameNoExtension : ThemeClassificationFormatDefinition {
			FileNameNoExtension() : base(TextColor.FileNameNoExtension) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.FileExtension)]
		[Name(ThemeClassificationTypeNameKeys.FileExtension)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.Identifier), Order(After = ThemeClassificationTypeNameKeys.Keyword)]
		sealed class FileExtension : ThemeClassificationFormatDefinition {
			FileExtension() : base(TextColor.FileExtension) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Error)]
		[Name(ThemeClassificationTypeNameKeys.Error)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Error : ThemeClassificationFormatDefinition {
			Error() : base(TextColor.Error) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ToStringEval)]
		[Name(ThemeClassificationTypeNameKeys.ToStringEval)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ToStringEval : ThemeClassificationFormatDefinition {
			ToStringEval() : base(TextColor.ToStringEval) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplPrompt1)]
		[Name(ThemeClassificationTypeNameKeys.ReplPrompt1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplPrompt1 : ThemeClassificationFormatDefinition {
			ReplPrompt1() : base(TextColor.ReplPrompt1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplPrompt2)]
		[Name(ThemeClassificationTypeNameKeys.ReplPrompt2)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplPrompt2 : ThemeClassificationFormatDefinition {
			ReplPrompt2() : base(TextColor.ReplPrompt2) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplOutputText)]
		[Name(ThemeClassificationTypeNameKeys.ReplOutputText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplOutputText : ThemeClassificationFormatDefinition {
			ReplOutputText() : base(TextColor.ReplOutputText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplScriptOutputText)]
		[Name(ThemeClassificationTypeNameKeys.ReplScriptOutputText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplScriptOutputText : ThemeClassificationFormatDefinition {
			ReplScriptOutputText() : base(TextColor.ReplScriptOutputText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Black)]
		[Name(ThemeClassificationTypeNameKeys.Black)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Black : ThemeClassificationFormatDefinition {
			Black() : base(TextColor.Black) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Blue)]
		[Name(ThemeClassificationTypeNameKeys.Blue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Blue : ThemeClassificationFormatDefinition {
			Blue() : base(TextColor.Blue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Cyan)]
		[Name(ThemeClassificationTypeNameKeys.Cyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Cyan : ThemeClassificationFormatDefinition {
			Cyan() : base(TextColor.Cyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkBlue)]
		[Name(ThemeClassificationTypeNameKeys.DarkBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkBlue : ThemeClassificationFormatDefinition {
			DarkBlue() : base(TextColor.DarkBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkCyan)]
		[Name(ThemeClassificationTypeNameKeys.DarkCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkCyan : ThemeClassificationFormatDefinition {
			DarkCyan() : base(TextColor.DarkCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkGray)]
		[Name(ThemeClassificationTypeNameKeys.DarkGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkGray : ThemeClassificationFormatDefinition {
			DarkGray() : base(TextColor.DarkGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkGreen)]
		[Name(ThemeClassificationTypeNameKeys.DarkGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkGreen : ThemeClassificationFormatDefinition {
			DarkGreen() : base(TextColor.DarkGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkMagenta)]
		[Name(ThemeClassificationTypeNameKeys.DarkMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkMagenta : ThemeClassificationFormatDefinition {
			DarkMagenta() : base(TextColor.DarkMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkRed)]
		[Name(ThemeClassificationTypeNameKeys.DarkRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkRed : ThemeClassificationFormatDefinition {
			DarkRed() : base(TextColor.DarkRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DarkYellow)]
		[Name(ThemeClassificationTypeNameKeys.DarkYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DarkYellow : ThemeClassificationFormatDefinition {
			DarkYellow() : base(TextColor.DarkYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Gray)]
		[Name(ThemeClassificationTypeNameKeys.Gray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Gray : ThemeClassificationFormatDefinition {
			Gray() : base(TextColor.Gray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Green)]
		[Name(ThemeClassificationTypeNameKeys.Green)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Green : ThemeClassificationFormatDefinition {
			Green() : base(TextColor.Green) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Magenta)]
		[Name(ThemeClassificationTypeNameKeys.Magenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Magenta : ThemeClassificationFormatDefinition {
			Magenta() : base(TextColor.Magenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Red)]
		[Name(ThemeClassificationTypeNameKeys.Red)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Red : ThemeClassificationFormatDefinition {
			Red() : base(TextColor.Red) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.White)]
		[Name(ThemeClassificationTypeNameKeys.White)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class White : ThemeClassificationFormatDefinition {
			White() : base(TextColor.White) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Yellow)]
		[Name(ThemeClassificationTypeNameKeys.Yellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Yellow : ThemeClassificationFormatDefinition {
			Yellow() : base(TextColor.Yellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvBlack)]
		[Name(ThemeClassificationTypeNameKeys.InvBlack)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvBlack : ThemeClassificationFormatDefinition {
			InvBlack() : base(TextColor.InvBlack) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvBlue)]
		[Name(ThemeClassificationTypeNameKeys.InvBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvBlue : ThemeClassificationFormatDefinition {
			InvBlue() : base(TextColor.InvBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvCyan)]
		[Name(ThemeClassificationTypeNameKeys.InvCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvCyan : ThemeClassificationFormatDefinition {
			InvCyan() : base(TextColor.InvCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkBlue)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkBlue)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkBlue : ThemeClassificationFormatDefinition {
			InvDarkBlue() : base(TextColor.InvDarkBlue) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkCyan)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkCyan)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkCyan : ThemeClassificationFormatDefinition {
			InvDarkCyan() : base(TextColor.InvDarkCyan) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkGray)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkGray : ThemeClassificationFormatDefinition {
			InvDarkGray() : base(TextColor.InvDarkGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkGreen)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkGreen : ThemeClassificationFormatDefinition {
			InvDarkGreen() : base(TextColor.InvDarkGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkMagenta)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkMagenta : ThemeClassificationFormatDefinition {
			InvDarkMagenta() : base(TextColor.InvDarkMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkRed)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkRed : ThemeClassificationFormatDefinition {
			InvDarkRed() : base(TextColor.InvDarkRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvDarkYellow)]
		[Name(ThemeClassificationTypeNameKeys.InvDarkYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvDarkYellow : ThemeClassificationFormatDefinition {
			InvDarkYellow() : base(TextColor.InvDarkYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvGray)]
		[Name(ThemeClassificationTypeNameKeys.InvGray)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvGray : ThemeClassificationFormatDefinition {
			InvGray() : base(TextColor.InvGray) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvGreen)]
		[Name(ThemeClassificationTypeNameKeys.InvGreen)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvGreen : ThemeClassificationFormatDefinition {
			InvGreen() : base(TextColor.InvGreen) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvMagenta)]
		[Name(ThemeClassificationTypeNameKeys.InvMagenta)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvMagenta : ThemeClassificationFormatDefinition {
			InvMagenta() : base(TextColor.InvMagenta) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvRed)]
		[Name(ThemeClassificationTypeNameKeys.InvRed)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvRed : ThemeClassificationFormatDefinition {
			InvRed() : base(TextColor.InvRed) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvWhite)]
		[Name(ThemeClassificationTypeNameKeys.InvWhite)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvWhite : ThemeClassificationFormatDefinition {
			InvWhite() : base(TextColor.InvWhite) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InvYellow)]
		[Name(ThemeClassificationTypeNameKeys.InvYellow)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InvYellow : ThemeClassificationFormatDefinition {
			InvYellow() : base(TextColor.InvYellow) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExceptionHandled)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExceptionHandled)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExceptionHandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionHandled() : base(TextColor.DebugLogExceptionHandled) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExceptionUnhandled)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExceptionUnhandled)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExceptionUnhandled : ThemeClassificationFormatDefinition {
			DebugLogExceptionUnhandled() : base(TextColor.DebugLogExceptionUnhandled) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogStepFiltering)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogStepFiltering)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogStepFiltering : ThemeClassificationFormatDefinition {
			DebugLogStepFiltering() : base(TextColor.DebugLogStepFiltering) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogLoadModule)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogLoadModule)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogLoadModule : ThemeClassificationFormatDefinition {
			DebugLogLoadModule() : base(TextColor.DebugLogLoadModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogUnloadModule)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogUnloadModule)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogUnloadModule : ThemeClassificationFormatDefinition {
			DebugLogUnloadModule() : base(TextColor.DebugLogUnloadModule) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExitProcess)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExitProcess)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExitProcess : ThemeClassificationFormatDefinition {
			DebugLogExitProcess() : base(TextColor.DebugLogExitProcess) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogExitThread)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogExitThread)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogExitThread : ThemeClassificationFormatDefinition {
			DebugLogExitThread() : base(TextColor.DebugLogExitThread) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogProgramOutput)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogProgramOutput)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogProgramOutput : ThemeClassificationFormatDefinition {
			DebugLogProgramOutput() : base(TextColor.DebugLogProgramOutput) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogMDA)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogMDA)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogMDA : ThemeClassificationFormatDefinition {
			DebugLogMDA() : base(TextColor.DebugLogMDA) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DebugLogTimestamp)]
		[Name(ThemeClassificationTypeNameKeys.DebugLogTimestamp)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DebugLogTimestamp : ThemeClassificationFormatDefinition {
			DebugLogTimestamp() : base(TextColor.DebugLogTimestamp) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.LineNumber)]
		[Name(ThemeClassificationTypeNameKeys.LineNumber)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class LineNumber : ThemeClassificationFormatDefinition {
			LineNumber() : base(TextColor.LineNumber) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplLineNumberInput1)]
		[Name(ThemeClassificationTypeNameKeys.ReplLineNumberInput1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplLineNumberInput1 : ThemeClassificationFormatDefinition {
			ReplLineNumberInput1() : base(TextColor.ReplLineNumberInput1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplLineNumberInput2)]
		[Name(ThemeClassificationTypeNameKeys.ReplLineNumberInput2)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplLineNumberInput2 : ThemeClassificationFormatDefinition {
			ReplLineNumberInput2() : base(TextColor.ReplLineNumberInput2) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ReplLineNumberOutput)]
		[Name(ThemeClassificationTypeNameKeys.ReplLineNumberOutput)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ReplLineNumberOutput : ThemeClassificationFormatDefinition {
			ReplLineNumberOutput() : base(TextColor.ReplLineNumberOutput) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.Link)]
		[Name(ThemeClassificationTypeNameKeys.Link)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class Link : ThemeClassificationFormatDefinition {
			Link() : base(TextColor.Link) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.VisibleWhitespace)]
		[Name(ThemeClassificationTypeNameKeys.VisibleWhitespace)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class VisibleWhitespace : ThemeClassificationFormatDefinition {
			VisibleWhitespace() : base(TextColor.VisibleWhitespace) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.SelectedText)]
		[Name(ThemeClassificationTypeNameKeys.SelectedText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class SelectedText : ThemeClassificationFormatDefinition {
			SelectedText() : base(TextColor.SelectedText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.InactiveSelectedText)]
		[Name(ThemeClassificationTypeNameKeys.InactiveSelectedText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class InactiveSelectedText : ThemeClassificationFormatDefinition {
			InactiveSelectedText() : base(TextColor.InactiveSelectedText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HighlightedReference)]
		[Name(ThemeClassificationTypeNameKeys.HighlightedReference)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HighlightedReference : ThemeMarkerFormatDefinition {
			HighlightedReference() : base(TextColor.HighlightedReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HighlightedWrittenReference)]
		[Name(ThemeClassificationTypeNameKeys.HighlightedWrittenReference)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HighlightedWrittenReference : ThemeMarkerFormatDefinition {
			HighlightedWrittenReference() : base(TextColor.HighlightedWrittenReference) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HighlightedDefinition)]
		[Name(ThemeClassificationTypeNameKeys.HighlightedDefinition)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HighlightedDefinition : ThemeMarkerFormatDefinition {
			HighlightedDefinition() : base(TextColor.HighlightedDefinition) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentStatement)]
		[Name(ThemeClassificationTypeNameKeys.CurrentStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class CurrentStatement : ThemeClassificationFormatDefinition {
			CurrentStatement() : base(TextColor.CurrentStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.CurrentStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class CurrentStatementMarker : ThemeMarkerFormatDefinition {
			CurrentStatementMarker() : base(TextColor.CurrentStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CallReturn)]
		[Name(ThemeClassificationTypeNameKeys.CallReturn)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class CallReturn : ThemeClassificationFormatDefinition {
			CallReturn() : base(TextColor.CallReturn) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CallReturnMarker)]
		[Name(ThemeClassificationTypeNameKeys.CallReturnMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class CallReturnMarker : ThemeMarkerFormatDefinition {
			CallReturnMarker() : base(TextColor.CallReturnMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.ActiveStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.ActiveStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class ActiveStatementMarker : ThemeMarkerFormatDefinition {
			ActiveStatementMarker() : base(TextColor.ActiveStatementMarker) {
				ZOrder = TextMarkerServiceZIndexes.ActiveStatement;
			}
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointStatement)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointStatement)]
		[UserVisible(true)]
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class BreakpointStatement : ThemeClassificationFormatDefinition {
			BreakpointStatement() : base(TextColor.BreakpointStatement) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.BreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class BreakpointStatementMarker : ThemeMarkerFormatDefinition {
			BreakpointStatementMarker() : base(TextColor.BreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.DisabledBreakpointStatementMarker)]
		[Name(ThemeClassificationTypeNameKeys.DisabledBreakpointStatementMarker)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class DisabledBreakpointStatementMarker : ThemeMarkerFormatDefinition {
			DisabledBreakpointStatementMarker() : base(TextColor.DisabledBreakpointStatementMarker) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentLine)]
		[Name(ThemeClassificationTypeNameKeys.CurrentLine)]
		[UserVisible(true)]
		[Order(Before = Priority.Default)]
		sealed class CurrentLine : ThemeClassificationFormatDefinition {
			CurrentLine() : base(TextColor.CurrentLine) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.CurrentLineNoFocus)]
		[Name(ThemeClassificationTypeNameKeys.CurrentLineNoFocus)]
		[UserVisible(true)]
		[Order(Before = Priority.Default)]
		sealed class CurrentLineNoFocus : ThemeClassificationFormatDefinition {
			CurrentLineNoFocus() : base(TextColor.CurrentLineNoFocus) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexText)]
		[Name(ThemeClassificationTypeNameKeys.HexText)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexText : ThemeClassificationFormatDefinition {
			HexText() : base(TextColor.HexText) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexOffset)]
		[Name(ThemeClassificationTypeNameKeys.HexOffset)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexOffset : ThemeClassificationFormatDefinition {
			HexOffset() : base(TextColor.HexOffset) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexByte0)]
		[Name(ThemeClassificationTypeNameKeys.HexByte0)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexByte0 : ThemeClassificationFormatDefinition {
			HexByte0() : base(TextColor.HexByte0) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexByte1)]
		[Name(ThemeClassificationTypeNameKeys.HexByte1)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexByte1 : ThemeClassificationFormatDefinition {
			HexByte1() : base(TextColor.HexByte1) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexByteError)]
		[Name(ThemeClassificationTypeNameKeys.HexByteError)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexByteError : ThemeClassificationFormatDefinition {
			HexByteError() : base(TextColor.HexByteError) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexAscii)]
		[Name(ThemeClassificationTypeNameKeys.HexAscii)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexAscii : ThemeClassificationFormatDefinition {
			HexAscii() : base(TextColor.HexAscii) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexCaret)]
		[Name(ThemeClassificationTypeNameKeys.HexCaret)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexCaret : ThemeClassificationFormatDefinition {
			HexCaret() : base(TextColor.HexCaret) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexInactiveCaret)]
		[Name(ThemeClassificationTypeNameKeys.HexInactiveCaret)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexInactiveCaret : ThemeClassificationFormatDefinition {
			HexInactiveCaret() : base(TextColor.HexInactiveCaret) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.HexSelection)]
		[Name(ThemeClassificationTypeNameKeys.HexSelection)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class HexSelection : ThemeClassificationFormatDefinition {
			HexSelection() : base(TextColor.HexSelection) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.GlyphMargin)]
		[Name(ThemeClassificationTypeNameKeys.GlyphMargin)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class GlyphMargin : ThemeClassificationFormatDefinition {
			GlyphMargin() : base(TextColor.GlyphMargin) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.BraceMatching)]
		[Name(ThemeClassificationTypeNameKeys.BraceMatching)]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class BraceMatching : ThemeClassificationFormatDefinition {
			BraceMatching() : base(TextColor.BraceMatching) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = ThemeClassificationTypeNames.LineSeparator)]
		[Name(ThemeClassificationTypeNameKeys.LineSeparator)]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class LineSeparator : ThemeClassificationFormatDefinition {
			LineSeparator() : base(TextColor.LineSeparator) { }
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "----------------")]
		[Name(Priority.High)]
		[UserVisible(false)]
		[Order(After = Priority.Default)]
		// Make sure High priority really is HIGH PRIORITY. string happens to be the last one unless I add this.
		[Order(After = ThemeClassificationTypeNameKeys.String)]
		sealed class PriorityHigh : ClassificationFormatDefinition {
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
