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

using System.Threading;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	/// <summary>
	/// Classification types used by <see cref="RoslynClassifier"/>
	/// </summary>
	public sealed class RoslynClassificationTypes {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public readonly IClassificationType Comment;
		public readonly IClassificationType Delegate;
		public readonly IClassificationType Enum;
		public readonly IClassificationType EnumField;
		public readonly IClassificationType ExcludedCode;
		public readonly IClassificationType ExtensionMethod;
		public readonly IClassificationType InstanceEvent;
		public readonly IClassificationType InstanceField;
		public readonly IClassificationType InstanceMethod;
		public readonly IClassificationType InstanceProperty;
		public readonly IClassificationType Interface;
		public readonly IClassificationType Keyword;
		public readonly IClassificationType Label;
		public readonly IClassificationType LiteralField;
		public readonly IClassificationType Local;
		public readonly IClassificationType MethodGenericParameter;
		public readonly IClassificationType Module;
		public readonly IClassificationType Namespace;
		public readonly IClassificationType Number;
		public readonly IClassificationType Operator;
		public readonly IClassificationType Parameter;
		public readonly IClassificationType PreprocessorKeyword;
		public readonly IClassificationType PreprocessorText;
		public readonly IClassificationType Punctuation;
		public readonly IClassificationType SealedType;
		public readonly IClassificationType StaticEvent;
		public readonly IClassificationType StaticField;
		public readonly IClassificationType StaticMethod;
		public readonly IClassificationType StaticProperty;
		public readonly IClassificationType StaticType;
		public readonly IClassificationType String;
		public readonly IClassificationType Text;
		public readonly IClassificationType Type;
		public readonly IClassificationType TypeGenericParameter;
		public readonly IClassificationType ValueType;
		public readonly IClassificationType VerbatimString;
		public readonly IClassificationType XmlDocCommentAttributeName;
		public readonly IClassificationType XmlDocCommentAttributeQuotes;
		public readonly IClassificationType XmlDocCommentAttributeValue;
		public readonly IClassificationType XmlDocCommentCDataSection;
		public readonly IClassificationType XmlDocCommentComment;
		public readonly IClassificationType XmlDocCommentDelimiter;
		public readonly IClassificationType XmlDocCommentEntityReference;
		public readonly IClassificationType XmlDocCommentName;
		public readonly IClassificationType XmlDocCommentProcessingInstruction;
		public readonly IClassificationType XmlDocCommentText;
		public readonly IClassificationType XmlLiteralAttributeName;
		public readonly IClassificationType XmlLiteralAttributeQuotes;
		public readonly IClassificationType XmlLiteralAttributeValue;
		public readonly IClassificationType XmlLiteralCDataSection;
		public readonly IClassificationType XmlLiteralComment;
		public readonly IClassificationType XmlLiteralDelimiter;
		public readonly IClassificationType XmlLiteralEmbeddedExpression;
		public readonly IClassificationType XmlLiteralEntityReference;
		public readonly IClassificationType XmlLiteralName;
		public readonly IClassificationType XmlLiteralProcessingInstruction;
		public readonly IClassificationType XmlLiteralText;
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

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
