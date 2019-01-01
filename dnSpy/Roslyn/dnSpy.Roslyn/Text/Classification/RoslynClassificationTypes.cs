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

using System.Threading;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Roslyn.Text.Classification {
	/// <summary>
	/// Classification types used by <see cref="RoslynClassifier"/>
	/// </summary>
	public sealed class RoslynClassificationTypes {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public readonly object Comment;
		public readonly object Delegate;
		public readonly object Enum;
		public readonly object EnumField;
		public readonly object ExcludedCode;
		public readonly object ExtensionMethod;
		public readonly object InstanceEvent;
		public readonly object InstanceField;
		public readonly object InstanceMethod;
		public readonly object InstanceProperty;
		public readonly object Interface;
		public readonly object Keyword;
		public readonly object Label;
		public readonly object LiteralField;
		public readonly object Local;
		public readonly object MethodGenericParameter;
		public readonly object Module;
		public readonly object Namespace;
		public readonly object Number;
		public readonly object Operator;
		public readonly object Parameter;
		public readonly object PreprocessorKeyword;
		public readonly object PreprocessorText;
		public readonly object Punctuation;
		public readonly object SealedType;
		public readonly object StaticEvent;
		public readonly object StaticField;
		public readonly object StaticMethod;
		public readonly object StaticProperty;
		public readonly object StaticType;
		public readonly object String;
		public readonly object Text;
		public readonly object Type;
		public readonly object TypeGenericParameter;
		public readonly object ValueType;
		public readonly object VerbatimString;
		public readonly object XmlDocCommentAttributeName;
		public readonly object XmlDocCommentAttributeQuotes;
		public readonly object XmlDocCommentAttributeValue;
		public readonly object XmlDocCommentCDataSection;
		public readonly object XmlDocCommentComment;
		public readonly object XmlDocCommentDelimiter;
		public readonly object XmlDocCommentEntityReference;
		public readonly object XmlDocCommentName;
		public readonly object XmlDocCommentProcessingInstruction;
		public readonly object XmlDocCommentText;
		public readonly object XmlLiteralAttributeName;
		public readonly object XmlLiteralAttributeQuotes;
		public readonly object XmlLiteralAttributeValue;
		public readonly object XmlLiteralCDataSection;
		public readonly object XmlLiteralComment;
		public readonly object XmlLiteralDelimiter;
		public readonly object XmlLiteralEmbeddedExpression;
		public readonly object XmlLiteralEntityReference;
		public readonly object XmlLiteralName;
		public readonly object XmlLiteralProcessingInstruction;
		public readonly object XmlLiteralText;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Gets the default instance
		/// </summary>
		public static readonly RoslynClassificationTypes Default = new RoslynClassificationTypes();

		RoslynClassificationTypes() {
			Comment = BoxedTextColor.Comment;
			Delegate = BoxedTextColor.Delegate;
			Enum = BoxedTextColor.Enum;
			EnumField = BoxedTextColor.EnumField;
			ExcludedCode = BoxedTextColor.ExcludedCode;
			ExtensionMethod = BoxedTextColor.ExtensionMethod;
			InstanceEvent = BoxedTextColor.InstanceEvent;
			InstanceField = BoxedTextColor.InstanceField;
			InstanceMethod = BoxedTextColor.InstanceMethod;
			InstanceProperty = BoxedTextColor.InstanceProperty;
			Interface = BoxedTextColor.Interface;
			Keyword = BoxedTextColor.Keyword;
			Label = BoxedTextColor.Label;
			LiteralField = BoxedTextColor.LiteralField;
			Local = BoxedTextColor.Local;
			MethodGenericParameter = BoxedTextColor.MethodGenericParameter;
			Module = BoxedTextColor.Module;
			Namespace = BoxedTextColor.Namespace;
			Number = BoxedTextColor.Number;
			Operator = BoxedTextColor.Operator;
			Parameter = BoxedTextColor.Parameter;
			PreprocessorKeyword = BoxedTextColor.PreprocessorKeyword;
			PreprocessorText = BoxedTextColor.PreprocessorText;
			Punctuation = BoxedTextColor.Punctuation;
			SealedType = BoxedTextColor.SealedType;
			StaticEvent = BoxedTextColor.StaticEvent;
			StaticField = BoxedTextColor.StaticField;
			StaticMethod = BoxedTextColor.StaticMethod;
			StaticProperty = BoxedTextColor.StaticProperty;
			StaticType = BoxedTextColor.StaticType;
			String = BoxedTextColor.String;
			Text = BoxedTextColor.Text;
			Type = BoxedTextColor.Type;
			TypeGenericParameter = BoxedTextColor.TypeGenericParameter;
			ValueType = BoxedTextColor.ValueType;
			VerbatimString = BoxedTextColor.VerbatimString;
			XmlDocCommentAttributeName = BoxedTextColor.XmlDocCommentAttributeName;
			XmlDocCommentAttributeQuotes = BoxedTextColor.XmlDocCommentAttributeQuotes;
			XmlDocCommentAttributeValue = BoxedTextColor.XmlDocCommentAttributeValue;
			XmlDocCommentCDataSection = BoxedTextColor.XmlDocCommentCDataSection;
			XmlDocCommentComment = BoxedTextColor.XmlDocCommentComment;
			XmlDocCommentDelimiter = BoxedTextColor.XmlDocCommentDelimiter;
			XmlDocCommentEntityReference = BoxedTextColor.XmlDocCommentEntityReference;
			XmlDocCommentName = BoxedTextColor.XmlDocCommentName;
			XmlDocCommentProcessingInstruction = BoxedTextColor.XmlDocCommentProcessingInstruction;
			XmlDocCommentText = BoxedTextColor.XmlDocCommentText;
			XmlLiteralAttributeName = BoxedTextColor.XmlLiteralAttributeName;
			XmlLiteralAttributeQuotes = BoxedTextColor.XmlLiteralAttributeQuotes;
			XmlLiteralAttributeValue = BoxedTextColor.XmlLiteralAttributeValue;
			XmlLiteralCDataSection = BoxedTextColor.XmlLiteralCDataSection;
			XmlLiteralComment = BoxedTextColor.XmlLiteralComment;
			XmlLiteralDelimiter = BoxedTextColor.XmlLiteralDelimiter;
			XmlLiteralEmbeddedExpression = BoxedTextColor.XmlLiteralEmbeddedExpression;
			XmlLiteralEntityReference = BoxedTextColor.XmlLiteralEntityReference;
			XmlLiteralName = BoxedTextColor.XmlLiteralName;
			XmlLiteralProcessingInstruction = BoxedTextColor.XmlLiteralProcessingInstruction;
			XmlLiteralText = BoxedTextColor.XmlLiteralText;
		}

		/// <summary>
		/// Gets the cached instance that contains <see cref="IClassificationType"/> values
		/// </summary>
		/// <returns></returns>
		public static RoslynClassificationTypes GetClassificationTypeInstance(IThemeClassificationTypeService themeClassificationTypeService) {
			if (classificationTypeInstance == null)
				Interlocked.CompareExchange(ref classificationTypeInstance, new RoslynClassificationTypes(themeClassificationTypeService), null);
			return classificationTypeInstance;
		}
		static RoslynClassificationTypes classificationTypeInstance;

		RoslynClassificationTypes(IThemeClassificationTypeService themeClassificationTypeService) {
			Comment = themeClassificationTypeService.GetClassificationType(TextColor.Comment);
			Delegate = themeClassificationTypeService.GetClassificationType(TextColor.Delegate);
			Enum = themeClassificationTypeService.GetClassificationType(TextColor.Enum);
			EnumField = themeClassificationTypeService.GetClassificationType(TextColor.EnumField);
			ExcludedCode = themeClassificationTypeService.GetClassificationType(TextColor.ExcludedCode);
			ExtensionMethod = themeClassificationTypeService.GetClassificationType(TextColor.ExtensionMethod);
			InstanceEvent = themeClassificationTypeService.GetClassificationType(TextColor.InstanceEvent);
			InstanceField = themeClassificationTypeService.GetClassificationType(TextColor.InstanceField);
			InstanceMethod = themeClassificationTypeService.GetClassificationType(TextColor.InstanceMethod);
			InstanceProperty = themeClassificationTypeService.GetClassificationType(TextColor.InstanceProperty);
			Interface = themeClassificationTypeService.GetClassificationType(TextColor.Interface);
			Keyword = themeClassificationTypeService.GetClassificationType(TextColor.Keyword);
			Label = themeClassificationTypeService.GetClassificationType(TextColor.Label);
			LiteralField = themeClassificationTypeService.GetClassificationType(TextColor.LiteralField);
			Local = themeClassificationTypeService.GetClassificationType(TextColor.Local);
			MethodGenericParameter = themeClassificationTypeService.GetClassificationType(TextColor.MethodGenericParameter);
			Module = themeClassificationTypeService.GetClassificationType(TextColor.Module);
			Namespace = themeClassificationTypeService.GetClassificationType(TextColor.Namespace);
			Number = themeClassificationTypeService.GetClassificationType(TextColor.Number);
			Operator = themeClassificationTypeService.GetClassificationType(TextColor.Operator);
			Parameter = themeClassificationTypeService.GetClassificationType(TextColor.Parameter);
			PreprocessorKeyword = themeClassificationTypeService.GetClassificationType(TextColor.PreprocessorKeyword);
			PreprocessorText = themeClassificationTypeService.GetClassificationType(TextColor.PreprocessorText);
			Punctuation = themeClassificationTypeService.GetClassificationType(TextColor.Punctuation);
			SealedType = themeClassificationTypeService.GetClassificationType(TextColor.SealedType);
			StaticEvent = themeClassificationTypeService.GetClassificationType(TextColor.StaticEvent);
			StaticField = themeClassificationTypeService.GetClassificationType(TextColor.StaticField);
			StaticMethod = themeClassificationTypeService.GetClassificationType(TextColor.StaticMethod);
			StaticProperty = themeClassificationTypeService.GetClassificationType(TextColor.StaticProperty);
			StaticType = themeClassificationTypeService.GetClassificationType(TextColor.StaticType);
			String = themeClassificationTypeService.GetClassificationType(TextColor.String);
			Text = themeClassificationTypeService.GetClassificationType(TextColor.Text);
			Type = themeClassificationTypeService.GetClassificationType(TextColor.Type);
			TypeGenericParameter = themeClassificationTypeService.GetClassificationType(TextColor.TypeGenericParameter);
			ValueType = themeClassificationTypeService.GetClassificationType(TextColor.ValueType);
			VerbatimString = themeClassificationTypeService.GetClassificationType(TextColor.VerbatimString);
			XmlDocCommentAttributeName = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentAttributeName);
			XmlDocCommentAttributeQuotes = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentAttributeQuotes);
			XmlDocCommentAttributeValue = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentAttributeValue);
			XmlDocCommentCDataSection = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentCDataSection);
			XmlDocCommentComment = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentComment);
			XmlDocCommentDelimiter = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentDelimiter);
			XmlDocCommentEntityReference = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentEntityReference);
			XmlDocCommentName = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentName);
			XmlDocCommentProcessingInstruction = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentProcessingInstruction);
			XmlDocCommentText = themeClassificationTypeService.GetClassificationType(TextColor.XmlDocCommentText);
			XmlLiteralAttributeName = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralAttributeName);
			XmlLiteralAttributeQuotes = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralAttributeQuotes);
			XmlLiteralAttributeValue = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralAttributeValue);
			XmlLiteralCDataSection = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralCDataSection);
			XmlLiteralComment = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralComment);
			XmlLiteralDelimiter = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralDelimiter);
			XmlLiteralEmbeddedExpression = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralEmbeddedExpression);
			XmlLiteralEntityReference = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralEntityReference);
			XmlLiteralName = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralName);
			XmlLiteralProcessingInstruction = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralProcessingInstruction);
			XmlLiteralText = themeClassificationTypeService.GetClassificationType(TextColor.XmlLiteralText);
		}
	}
}
