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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Text.Classification {
	/// <summary>
	/// Classifier result
	/// </summary>
	public readonly struct ClassifierResult {
		/// <summary>
		/// Span
		/// </summary>
		public readonly Span Span;

		/// <summary>
		/// Color
		/// </summary>
		public readonly object Color;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="color">Color</param>
		public ClassifierResult(Span span, object color) {
			Span = span;
			Color = color;
		}
	}

	/// <summary>
	/// Roslyn classifier
	/// </summary>
	public readonly struct RoslynClassifier {
		readonly SyntaxNode syntaxRoot;
		readonly SemanticModel semanticModel;
		readonly Workspace workspace;
		readonly RoslynClassificationTypes roslynClassificationTypes;
		readonly object? defaultColor;
		readonly CancellationToken cancellationToken;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="syntaxRoot">Syntax root</param>
		/// <param name="semanticModel">Semantic model</param>
		/// <param name="workspace">Workspace</param>
		/// <param name="roslynClassificationTypes">Colors</param>
		/// <param name="defaultColor">Default color if a token can't be classified or null to not use anything</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public RoslynClassifier(SyntaxNode syntaxRoot, SemanticModel semanticModel, Workspace workspace, RoslynClassificationTypes roslynClassificationTypes, object? defaultColor, CancellationToken cancellationToken) {
			this.syntaxRoot = syntaxRoot;
			this.semanticModel = semanticModel;
			this.workspace = workspace;
			this.roslynClassificationTypes = roslynClassificationTypes;
			this.defaultColor = defaultColor;
			this.cancellationToken = cancellationToken;
		}

		/// <summary>
		/// Returns all colors
		/// </summary>
		/// <param name="textSpan">Span to classify</param>
		/// <returns></returns>
		public IEnumerable<ClassifierResult> GetColors(TextSpan textSpan) {
			foreach (var cspan in Classifier.GetClassifiedSpans(semanticModel, textSpan, workspace)) {
				var color = GetClassificationType(cspan) ?? defaultColor;
				if (color is not null)
					yield return new ClassifierResult(Span.FromBounds(cspan.TextSpan.Start, cspan.TextSpan.End), color);
			}
		}

		readonly struct SymbolResult {
			public readonly ISymbol? Symbol;
			public readonly object? Color;

			public SymbolResult(ISymbol symbol) {
				Symbol = symbol;
				Color = null;
			}

			public SymbolResult(object color) {
				Symbol = null;
				Color = color;
			}
		}

		SymbolResult GetSymbolResult(TextSpan span) {
			var node = syntaxRoot.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);

			// Fix for: using DNS = System;
			if (node.Parent?.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax)
				return new SymbolResult(roslynClassificationTypes.Namespace);

			var symInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
			var symbol = symInfo.Symbol ?? symInfo.CandidateSymbols.FirstOrDefault() ??
						semanticModel.GetDeclaredSymbol(node, cancellationToken);

			return new SymbolResult(symbol);
		}

		object? GetClassificationType2(ClassifiedSpan cspan) {
			var symRes = GetSymbolResult(cspan.TextSpan);
			if (symRes.Color is not null)
				return symRes.Color;
			var symbol = symRes.Symbol;
			if (symbol is null)
				return null;
the_switch:
			switch (symbol.Kind) {
			case SymbolKind.Alias:
				return roslynClassificationTypes.Namespace;

			case SymbolKind.ArrayType:
			case SymbolKind.Assembly:
			case SymbolKind.DynamicType:
			case SymbolKind.ErrorType:
				break;

			case SymbolKind.Event:
				var evtSym = (IEventSymbol)symbol;
				return evtSym.IsStatic ? roslynClassificationTypes.StaticEvent : roslynClassificationTypes.InstanceEvent;

			case SymbolKind.Field:
				var fldSym = (IFieldSymbol)symbol;
				if (fldSym.ContainingType?.IsScriptClass == true)
					return roslynClassificationTypes.Local;
				if (fldSym.ContainingType?.TypeKind == TypeKind.Enum)
					return roslynClassificationTypes.EnumField;
				if (fldSym.IsConst)
					return roslynClassificationTypes.LiteralField;
				if (fldSym.IsStatic)
					return roslynClassificationTypes.StaticField;
				return roslynClassificationTypes.InstanceField;

			case SymbolKind.Label:
				return roslynClassificationTypes.Label;

			case SymbolKind.Local:
				return roslynClassificationTypes.Local;

			case SymbolKind.Method:
				var methSym = (IMethodSymbol)symbol;
				switch (methSym.MethodKind) {
				case MethodKind.Constructor:
				case MethodKind.Destructor:
				case MethodKind.StaticConstructor:
					symbol = methSym.ContainingType;
					goto the_switch;

				case MethodKind.Ordinary:
				case MethodKind.DelegateInvoke:
				case MethodKind.ExplicitInterfaceImplementation:
				case MethodKind.AnonymousFunction:
				case MethodKind.Conversion:
				case MethodKind.EventAdd:
				case MethodKind.EventRaise:
				case MethodKind.EventRemove:
				case MethodKind.UserDefinedOperator:
				case MethodKind.PropertyGet:
				case MethodKind.PropertySet:
				case MethodKind.BuiltinOperator:
				case MethodKind.DeclareMethod:
				case MethodKind.LocalFunction:
				default:
					if (methSym.IsExtensionMethod)
						return roslynClassificationTypes.ExtensionMethod;
					if (methSym.IsStatic)
						return roslynClassificationTypes.StaticMethod;
					return roslynClassificationTypes.InstanceMethod;

				case MethodKind.ReducedExtension:
					return roslynClassificationTypes.ExtensionMethod;
				}

			case SymbolKind.NetModule:
				break;

			case SymbolKind.NamedType:
				var nts = (INamedTypeSymbol)symbol;
				switch (nts.TypeKind) {
				case TypeKind.Class:
					if (nts.IsStatic)
						return roslynClassificationTypes.StaticType;
					if (nts.IsSealed)
						return roslynClassificationTypes.SealedType;
					return roslynClassificationTypes.Type;

				case TypeKind.Delegate:
					return roslynClassificationTypes.Delegate;

				case TypeKind.Enum:
					return roslynClassificationTypes.Enum;

				case TypeKind.Interface:
					return roslynClassificationTypes.Interface;

				case TypeKind.Struct:
					return roslynClassificationTypes.ValueType;

				case TypeKind.TypeParameter:
					if ((symbol as ITypeParameterSymbol)?.DeclaringMethod is not null)
						return roslynClassificationTypes.MethodGenericParameter;
					return roslynClassificationTypes.TypeGenericParameter;

				case TypeKind.Unknown:
				case TypeKind.Array:
				case TypeKind.Dynamic:
				case TypeKind.Error:
				case TypeKind.Module:
				case TypeKind.Pointer:
				case TypeKind.Submission:
				default:
					break;
				}
				break;

			case SymbolKind.Namespace:
				return roslynClassificationTypes.Namespace;

			case SymbolKind.Parameter:
				return roslynClassificationTypes.Parameter;

			case SymbolKind.PointerType:
				break;

			case SymbolKind.Property:
				var propSym = (IPropertySymbol)symbol;
				return propSym.IsStatic ? roslynClassificationTypes.StaticProperty : roslynClassificationTypes.InstanceProperty;

			case SymbolKind.RangeVariable:
				return roslynClassificationTypes.Local;

			case SymbolKind.TypeParameter:
				return (symbol as ITypeParameterSymbol)?.DeclaringMethod is not null ?
					roslynClassificationTypes.MethodGenericParameter : roslynClassificationTypes.TypeGenericParameter;

			case SymbolKind.Preprocessing:
				break;

			case SymbolKind.Discard:
				break;//TODO:

			default:
				Debug.WriteLine($"Unknown SymbolKind: {symbol.Kind}");
				break;
			}

			return null;
		}

		object? GetClassificationType(ClassifiedSpan cspan) {
			object? classificationType;
			SymbolResult symRes;
			switch (cspan.ClassificationType) {
			case ClassificationTypeNames.FieldName:
			case ClassificationTypeNames.PropertyName:
			case ClassificationTypeNames.EventName:
			case ClassificationTypeNames.MethodName:
				return GetClassificationType2(cspan);

			case ClassificationTypeNames.ConstantName:
				return roslynClassificationTypes.LiteralField;

			case ClassificationTypeNames.EnumMemberName:
				return roslynClassificationTypes.EnumField;

			case ClassificationTypeNames.LocalName:
				return roslynClassificationTypes.Local;

			case ClassificationTypeNames.ParameterName:
				return roslynClassificationTypes.Parameter;

			case ClassificationTypeNames.ExtensionMethodName:
				return roslynClassificationTypes.ExtensionMethod;

			case ClassificationTypeNames.ClassName:
				symRes = GetSymbolResult(cspan.TextSpan);
				if (symRes.Color is not null)
					return symRes.Color;
				if (symRes.Symbol?.IsStatic == true)
					return roslynClassificationTypes.StaticType;
				if (symRes.Symbol?.IsSealed == true)
					return roslynClassificationTypes.SealedType;
				Debug.WriteLineIf(symRes.Symbol is null, "Couldn't get ClassName classification type");
				return roslynClassificationTypes.Type;

			case ClassificationTypeNames.Comment:
				return roslynClassificationTypes.Comment;

			case ClassificationTypeNames.DelegateName:
				return roslynClassificationTypes.Delegate;

			case ClassificationTypeNames.EnumName:
				return roslynClassificationTypes.Enum;

			case ClassificationTypeNames.ExcludedCode:
				return roslynClassificationTypes.ExcludedCode;

			case ClassificationTypeNames.Identifier:
				return GetClassificationType2(cspan);

			case ClassificationTypeNames.InterfaceName:
				return roslynClassificationTypes.Interface;

			case ClassificationTypeNames.Keyword:
				return roslynClassificationTypes.Keyword;

			case ClassificationTypeNames.ModuleName:
				return roslynClassificationTypes.Module;

			case ClassificationTypeNames.NumericLiteral:
				return roslynClassificationTypes.Number;

			case ClassificationTypeNames.Operator:
				return roslynClassificationTypes.Operator;

			case ClassificationTypeNames.PreprocessorKeyword:
				return roslynClassificationTypes.PreprocessorKeyword;

			case ClassificationTypeNames.PreprocessorText:
				return roslynClassificationTypes.PreprocessorText;

			case ClassificationTypeNames.Punctuation:
				return roslynClassificationTypes.Punctuation;

			case ClassificationTypeNames.StringLiteral:
				return roslynClassificationTypes.String;

			case ClassificationTypeNames.StructName:
				return roslynClassificationTypes.ValueType;

			case ClassificationTypeNames.Text:
				return roslynClassificationTypes.Text;

			case ClassificationTypeNames.TypeParameterName:
				classificationType = GetClassificationType2(cspan);
				Debug.WriteLineIf(classificationType is null, "Couldn't get TypeParameterName color type");
				return classificationType ?? roslynClassificationTypes.TypeGenericParameter;

			case ClassificationTypeNames.VerbatimStringLiteral:
				return roslynClassificationTypes.VerbatimString;

			case ClassificationTypeNames.WhiteSpace:
				return roslynClassificationTypes.Text;

			case ClassificationTypeNames.XmlDocCommentAttributeName:
				return roslynClassificationTypes.XmlDocCommentAttributeName;

			case ClassificationTypeNames.XmlDocCommentAttributeQuotes:
				return roslynClassificationTypes.XmlDocCommentAttributeQuotes;

			case ClassificationTypeNames.XmlDocCommentAttributeValue:
				return roslynClassificationTypes.XmlDocCommentAttributeValue;

			case ClassificationTypeNames.XmlDocCommentCDataSection:
				return roslynClassificationTypes.XmlDocCommentCDataSection;

			case ClassificationTypeNames.XmlDocCommentComment:
				return roslynClassificationTypes.XmlDocCommentComment;

			case ClassificationTypeNames.XmlDocCommentDelimiter:
				return roslynClassificationTypes.XmlDocCommentDelimiter;

			case ClassificationTypeNames.XmlDocCommentEntityReference:
				return roslynClassificationTypes.XmlDocCommentEntityReference;

			case ClassificationTypeNames.XmlDocCommentName:
				return roslynClassificationTypes.XmlDocCommentName;

			case ClassificationTypeNames.XmlDocCommentProcessingInstruction:
				return roslynClassificationTypes.XmlDocCommentProcessingInstruction;

			case ClassificationTypeNames.XmlDocCommentText:
				return roslynClassificationTypes.XmlDocCommentText;

			case ClassificationTypeNames.XmlLiteralAttributeName:
				return roslynClassificationTypes.XmlLiteralAttributeName;

			case ClassificationTypeNames.XmlLiteralAttributeQuotes:
				return roslynClassificationTypes.XmlLiteralAttributeQuotes;

			case ClassificationTypeNames.XmlLiteralAttributeValue:
				return roslynClassificationTypes.XmlLiteralAttributeValue;

			case ClassificationTypeNames.XmlLiteralCDataSection:
				return roslynClassificationTypes.XmlLiteralCDataSection;

			case ClassificationTypeNames.XmlLiteralComment:
				return roslynClassificationTypes.XmlLiteralComment;

			case ClassificationTypeNames.XmlLiteralDelimiter:
				return roslynClassificationTypes.XmlLiteralDelimiter;

			case ClassificationTypeNames.XmlLiteralEmbeddedExpression:
				return roslynClassificationTypes.XmlLiteralEmbeddedExpression;

			case ClassificationTypeNames.XmlLiteralEntityReference:
				return roslynClassificationTypes.XmlLiteralEntityReference;

			case ClassificationTypeNames.XmlLiteralName:
				return roslynClassificationTypes.XmlLiteralName;

			case ClassificationTypeNames.XmlLiteralProcessingInstruction:
				return roslynClassificationTypes.XmlLiteralProcessingInstruction;

			case ClassificationTypeNames.XmlLiteralText:
				return roslynClassificationTypes.XmlLiteralText;

			default:
				Debug.WriteLine($"Unknown ClassificationType = '{cspan.ClassificationType}'");
				return null;
			}
		}
	}
}
