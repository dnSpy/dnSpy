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
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	public sealed class RoslynClassificationTypes {
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
	}
}
