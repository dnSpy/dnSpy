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

using System.Threading;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	public sealed class RoslynClassifierColors {
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

		/// <summary>
		/// Gets the cached instance that contains <see cref="IClassificationType"/> values
		/// </summary>
		/// <returns></returns>
		public static RoslynClassifierColors GetClassificationTypeInstance(IThemeClassificationTypes themeClassificationTypes) {
			if (classificationTypeInstance == null)
				Interlocked.CompareExchange(ref classificationTypeInstance, new RoslynClassifierColors(themeClassificationTypes), null);
			return classificationTypeInstance;
		}
		static RoslynClassifierColors classificationTypeInstance;

		RoslynClassifierColors(IThemeClassificationTypes themeClassificationTypes) {
			Comment = themeClassificationTypes.GetClassificationType(ColorType.Comment);
			Delegate = themeClassificationTypes.GetClassificationType(ColorType.Delegate);
			Enum = themeClassificationTypes.GetClassificationType(ColorType.Enum);
			EnumField = themeClassificationTypes.GetClassificationType(ColorType.EnumField);
			ExcludedCode = themeClassificationTypes.GetClassificationType(ColorType.ExcludedCode);
			ExtensionMethod = themeClassificationTypes.GetClassificationType(ColorType.ExtensionMethod);
			InstanceEvent = themeClassificationTypes.GetClassificationType(ColorType.InstanceEvent);
			InstanceField = themeClassificationTypes.GetClassificationType(ColorType.InstanceField);
			InstanceMethod = themeClassificationTypes.GetClassificationType(ColorType.InstanceMethod);
			InstanceProperty = themeClassificationTypes.GetClassificationType(ColorType.InstanceProperty);
			Interface = themeClassificationTypes.GetClassificationType(ColorType.Interface);
			Keyword = themeClassificationTypes.GetClassificationType(ColorType.Keyword);
			Label = themeClassificationTypes.GetClassificationType(ColorType.Label);
			LiteralField = themeClassificationTypes.GetClassificationType(ColorType.LiteralField);
			Local = themeClassificationTypes.GetClassificationType(ColorType.Local);
			MethodGenericParameter = themeClassificationTypes.GetClassificationType(ColorType.MethodGenericParameter);
			Module = themeClassificationTypes.GetClassificationType(ColorType.Module);
			Namespace = themeClassificationTypes.GetClassificationType(ColorType.Namespace);
			Number = themeClassificationTypes.GetClassificationType(ColorType.Number);
			Operator = themeClassificationTypes.GetClassificationType(ColorType.Operator);
			Parameter = themeClassificationTypes.GetClassificationType(ColorType.Parameter);
			PreprocessorKeyword = themeClassificationTypes.GetClassificationType(ColorType.PreprocessorKeyword);
			PreprocessorText = themeClassificationTypes.GetClassificationType(ColorType.PreprocessorText);
			Punctuation = themeClassificationTypes.GetClassificationType(ColorType.Punctuation);
			SealedType = themeClassificationTypes.GetClassificationType(ColorType.SealedType);
			StaticEvent = themeClassificationTypes.GetClassificationType(ColorType.StaticEvent);
			StaticField = themeClassificationTypes.GetClassificationType(ColorType.StaticField);
			StaticMethod = themeClassificationTypes.GetClassificationType(ColorType.StaticMethod);
			StaticProperty = themeClassificationTypes.GetClassificationType(ColorType.StaticProperty);
			StaticType = themeClassificationTypes.GetClassificationType(ColorType.StaticType);
			String = themeClassificationTypes.GetClassificationType(ColorType.String);
			Text = themeClassificationTypes.GetClassificationType(ColorType.Text);
			Type = themeClassificationTypes.GetClassificationType(ColorType.Type);
			TypeGenericParameter = themeClassificationTypes.GetClassificationType(ColorType.TypeGenericParameter);
			ValueType = themeClassificationTypes.GetClassificationType(ColorType.ValueType);
			VerbatimString = themeClassificationTypes.GetClassificationType(ColorType.VerbatimString);
			XmlDocCommentAttributeName = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentAttributeName);
			XmlDocCommentAttributeQuotes = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentAttributeQuotes);
			XmlDocCommentAttributeValue = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentAttributeValue);
			XmlDocCommentCDataSection = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentCDataSection);
			XmlDocCommentComment = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentComment);
			XmlDocCommentDelimiter = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentDelimiter);
			XmlDocCommentEntityReference = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentEntityReference);
			XmlDocCommentName = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentName);
			XmlDocCommentProcessingInstruction = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentProcessingInstruction);
			XmlDocCommentText = themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentText);
			XmlLiteralAttributeName = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralAttributeName);
			XmlLiteralAttributeQuotes = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralAttributeQuotes);
			XmlLiteralAttributeValue = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralAttributeValue);
			XmlLiteralCDataSection = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralCDataSection);
			XmlLiteralComment = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralComment);
			XmlLiteralDelimiter = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralDelimiter);
			XmlLiteralEmbeddedExpression = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralEmbeddedExpression);
			XmlLiteralEntityReference = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralEntityReference);
			XmlLiteralName = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralName);
			XmlLiteralProcessingInstruction = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralProcessingInstruction);
			XmlLiteralText = themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralText);
		}

		/// <summary>
		/// Gets the cached instance that contains boxed <see cref="TextColor"/> values
		/// </summary>
		/// <returns></returns>
		public static RoslynClassifierColors GetTextColorInstance() {
			if (textColorInstance == null)
				Interlocked.CompareExchange(ref textColorInstance, new RoslynClassifierColors(true), null);
			return textColorInstance;
		}
		static RoslynClassifierColors textColorInstance;

		RoslynClassifierColors(bool dummy) {
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
	}
}
