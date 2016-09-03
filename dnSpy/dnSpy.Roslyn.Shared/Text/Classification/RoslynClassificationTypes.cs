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
		public static RoslynClassificationTypes GetClassificationTypeInstance(IThemeClassificationTypes themeClassificationTypes) {
			if (classificationTypeInstance == null)
				Interlocked.CompareExchange(ref classificationTypeInstance, new RoslynClassificationTypes(themeClassificationTypes), null);
			return classificationTypeInstance;
		}
		static RoslynClassificationTypes classificationTypeInstance;

		RoslynClassificationTypes(IThemeClassificationTypes themeClassificationTypes) {
			Comment = themeClassificationTypes.GetClassificationType(TextColor.Comment);
			Delegate = themeClassificationTypes.GetClassificationType(TextColor.Delegate);
			Enum = themeClassificationTypes.GetClassificationType(TextColor.Enum);
			EnumField = themeClassificationTypes.GetClassificationType(TextColor.EnumField);
			ExcludedCode = themeClassificationTypes.GetClassificationType(TextColor.ExcludedCode);
			ExtensionMethod = themeClassificationTypes.GetClassificationType(TextColor.ExtensionMethod);
			InstanceEvent = themeClassificationTypes.GetClassificationType(TextColor.InstanceEvent);
			InstanceField = themeClassificationTypes.GetClassificationType(TextColor.InstanceField);
			InstanceMethod = themeClassificationTypes.GetClassificationType(TextColor.InstanceMethod);
			InstanceProperty = themeClassificationTypes.GetClassificationType(TextColor.InstanceProperty);
			Interface = themeClassificationTypes.GetClassificationType(TextColor.Interface);
			Keyword = themeClassificationTypes.GetClassificationType(TextColor.Keyword);
			Label = themeClassificationTypes.GetClassificationType(TextColor.Label);
			LiteralField = themeClassificationTypes.GetClassificationType(TextColor.LiteralField);
			Local = themeClassificationTypes.GetClassificationType(TextColor.Local);
			MethodGenericParameter = themeClassificationTypes.GetClassificationType(TextColor.MethodGenericParameter);
			Module = themeClassificationTypes.GetClassificationType(TextColor.Module);
			Namespace = themeClassificationTypes.GetClassificationType(TextColor.Namespace);
			Number = themeClassificationTypes.GetClassificationType(TextColor.Number);
			Operator = themeClassificationTypes.GetClassificationType(TextColor.Operator);
			Parameter = themeClassificationTypes.GetClassificationType(TextColor.Parameter);
			PreprocessorKeyword = themeClassificationTypes.GetClassificationType(TextColor.PreprocessorKeyword);
			PreprocessorText = themeClassificationTypes.GetClassificationType(TextColor.PreprocessorText);
			Punctuation = themeClassificationTypes.GetClassificationType(TextColor.Punctuation);
			SealedType = themeClassificationTypes.GetClassificationType(TextColor.SealedType);
			StaticEvent = themeClassificationTypes.GetClassificationType(TextColor.StaticEvent);
			StaticField = themeClassificationTypes.GetClassificationType(TextColor.StaticField);
			StaticMethod = themeClassificationTypes.GetClassificationType(TextColor.StaticMethod);
			StaticProperty = themeClassificationTypes.GetClassificationType(TextColor.StaticProperty);
			StaticType = themeClassificationTypes.GetClassificationType(TextColor.StaticType);
			String = themeClassificationTypes.GetClassificationType(TextColor.String);
			Text = themeClassificationTypes.GetClassificationType(TextColor.Text);
			Type = themeClassificationTypes.GetClassificationType(TextColor.Type);
			TypeGenericParameter = themeClassificationTypes.GetClassificationType(TextColor.TypeGenericParameter);
			ValueType = themeClassificationTypes.GetClassificationType(TextColor.ValueType);
			VerbatimString = themeClassificationTypes.GetClassificationType(TextColor.VerbatimString);
			XmlDocCommentAttributeName = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentAttributeName);
			XmlDocCommentAttributeQuotes = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentAttributeQuotes);
			XmlDocCommentAttributeValue = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentAttributeValue);
			XmlDocCommentCDataSection = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentCDataSection);
			XmlDocCommentComment = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentComment);
			XmlDocCommentDelimiter = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentDelimiter);
			XmlDocCommentEntityReference = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentEntityReference);
			XmlDocCommentName = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentName);
			XmlDocCommentProcessingInstruction = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentProcessingInstruction);
			XmlDocCommentText = themeClassificationTypes.GetClassificationType(TextColor.XmlDocCommentText);
			XmlLiteralAttributeName = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralAttributeName);
			XmlLiteralAttributeQuotes = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralAttributeQuotes);
			XmlLiteralAttributeValue = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralAttributeValue);
			XmlLiteralCDataSection = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralCDataSection);
			XmlLiteralComment = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralComment);
			XmlLiteralDelimiter = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralDelimiter);
			XmlLiteralEmbeddedExpression = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralEmbeddedExpression);
			XmlLiteralEntityReference = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralEntityReference);
			XmlLiteralName = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralName);
			XmlLiteralProcessingInstruction = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralProcessingInstruction);
			XmlLiteralText = themeClassificationTypes.GetClassificationType(TextColor.XmlLiteralText);
		}
	}
}
