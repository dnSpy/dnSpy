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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	public struct ClassifierResult {
		public readonly Span Span;
		public readonly object Type;

		public ClassifierResult(Span span, object type) {
			this.Span = span;
			this.Type = type;
		}
	}

	public struct RoslynClassifier {
		readonly SyntaxNode syntaxRoot;
		readonly SemanticModel semanticModel;
		readonly Workspace workspace;
		readonly RoslynClassifierColors roslynClassifierColors;
		readonly object defaultColor;
		/*readonly*/ CancellationToken cancellationToken;

		public RoslynClassifier(SyntaxNode syntaxRoot, SemanticModel semanticModel, Workspace workspace, RoslynClassifierColors roslynClassifierColors, object defaultColor, CancellationToken cancellationToken) {
			this.syntaxRoot = syntaxRoot;
			this.semanticModel = semanticModel;
			this.workspace = workspace;
			this.roslynClassifierColors = roslynClassifierColors;
			this.defaultColor = defaultColor;
			this.cancellationToken = cancellationToken;
		}

		public IEnumerable<ClassifierResult> GetClassificationColors(TextSpan textSpan) {
			foreach (var cspan in Classifier.GetClassifiedSpans(semanticModel, textSpan, workspace)) {
				var color = GetColorType(cspan) ?? defaultColor;
				if (color != null)
					yield return new ClassifierResult(Span.FromBounds(cspan.TextSpan.Start, cspan.TextSpan.End), color);
			}
		}

		struct SymbolResult {
			public readonly ISymbol Symbol;
			public readonly object Type;

			public SymbolResult(ISymbol symbol) {
				Symbol = symbol;
				Type = null;
			}

			public SymbolResult(object type) {
				Symbol = null;
				Type = type;
			}
		}

		SymbolResult GetSymbolResult(TextSpan span) {
			var node = syntaxRoot.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);

			// Fix for: using DNS = System;
			if (node.Parent?.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax)
				return new SymbolResult(roslynClassifierColors.Namespace);

			var symInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
			var symbol = symInfo.Symbol ?? symInfo.CandidateSymbols.FirstOrDefault() ??
						semanticModel.GetDeclaredSymbol(node, cancellationToken);

			return new SymbolResult(symbol);
		}

		object GetColorType2(ClassifiedSpan cspan) {
			var symRes = GetSymbolResult(cspan.TextSpan);
			if (symRes.Type != null)
				return symRes.Type;
			var symbol = symRes.Symbol;
			if (symbol == null)
				return null;
the_switch:
			switch (symbol.Kind) {
			case SymbolKind.Alias:
				return roslynClassifierColors.Namespace;

			case SymbolKind.ArrayType:
			case SymbolKind.Assembly:
			case SymbolKind.DynamicType:
			case SymbolKind.ErrorType:
				break;

			case SymbolKind.Event:
				var evtSym = (IEventSymbol)symbol;
				return evtSym.IsStatic ? roslynClassifierColors.StaticEvent : roslynClassifierColors.InstanceEvent;

			case SymbolKind.Field:
				var fldSym = (IFieldSymbol)symbol;
				if (fldSym.ContainingType?.IsScriptClass == true)
					return roslynClassifierColors.Local;
				if (fldSym.ContainingType?.TypeKind == TypeKind.Enum)
					return roslynClassifierColors.EnumField;
				if (fldSym.IsConst)
					return roslynClassifierColors.LiteralField;
				if (fldSym.IsStatic)
					return roslynClassifierColors.StaticField;
				return roslynClassifierColors.InstanceField;

			case SymbolKind.Label:
				return roslynClassifierColors.Label;

			case SymbolKind.Local:
				return roslynClassifierColors.Local;

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
				default:
					if (methSym.IsExtensionMethod)
						return roslynClassifierColors.ExtensionMethod;
					if (methSym.IsStatic)
						return roslynClassifierColors.StaticMethod;
					return roslynClassifierColors.InstanceMethod;

				case MethodKind.ReducedExtension:
					return roslynClassifierColors.ExtensionMethod;
				}

			case SymbolKind.NetModule:
				break;

			case SymbolKind.NamedType:
				var nts = (INamedTypeSymbol)symbol;
				switch (nts.TypeKind) {
				case TypeKind.Class:
					if (nts.IsStatic)
						return roslynClassifierColors.StaticType;
					if (nts.IsSealed)
						return roslynClassifierColors.SealedType;
					return roslynClassifierColors.Type;

				case TypeKind.Delegate:
					return roslynClassifierColors.Delegate;

				case TypeKind.Enum:
					return roslynClassifierColors.Enum;

				case TypeKind.Interface:
					return roslynClassifierColors.Interface;

				case TypeKind.Struct:
					return roslynClassifierColors.ValueType;

				case TypeKind.TypeParameter:
					if ((symbol as ITypeParameterSymbol)?.DeclaringMethod != null)
						return roslynClassifierColors.MethodGenericParameter;
					return roslynClassifierColors.TypeGenericParameter;

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
				return roslynClassifierColors.Namespace;

			case SymbolKind.Parameter:
				return roslynClassifierColors.Parameter;

			case SymbolKind.PointerType:
				break;

			case SymbolKind.Property:
				var propSym = (IPropertySymbol)symbol;
				return propSym.IsStatic ? roslynClassifierColors.StaticProperty : roslynClassifierColors.InstanceProperty;

			case SymbolKind.RangeVariable:
				return roslynClassifierColors.Local;

			case SymbolKind.TypeParameter:
				return (symbol as ITypeParameterSymbol)?.DeclaringMethod != null ?
					roslynClassifierColors.MethodGenericParameter : roslynClassifierColors.TypeGenericParameter;

			case SymbolKind.Preprocessing:
				break;

			default:
				Debug.WriteLine($"Unknown SymbolKind: {symbol.Kind}");
				break;
			}

			return null;
		}

		object GetColorType(ClassifiedSpan cspan) {
			object colorType;
			SymbolResult symRes;
			switch (cspan.ClassificationType) {
			case ClassificationTypeNames.ClassName:
				symRes = GetSymbolResult(cspan.TextSpan);
				if (symRes.Type != null)
					return symRes.Type;
				if (symRes.Symbol?.IsStatic == true)
					return roslynClassifierColors.StaticType;
				if (symRes.Symbol?.IsSealed == true)
					return roslynClassifierColors.SealedType;
				Debug.WriteLineIf(symRes.Symbol == null, "Couldn't get ClassName classification type");
				return roslynClassifierColors.Type;

			case ClassificationTypeNames.Comment:
				return roslynClassifierColors.Comment;

			case ClassificationTypeNames.DelegateName:
				return roslynClassifierColors.Delegate;

			case ClassificationTypeNames.EnumName:
				return roslynClassifierColors.Enum;

			case ClassificationTypeNames.ExcludedCode:
				return roslynClassifierColors.ExcludedCode;

			case ClassificationTypeNames.Identifier:
				return GetColorType2(cspan);

			case ClassificationTypeNames.InterfaceName:
				return roslynClassifierColors.Interface;

			case ClassificationTypeNames.Keyword:
				return roslynClassifierColors.Keyword;

			case ClassificationTypeNames.ModuleName:
				return roslynClassifierColors.Module;

			case ClassificationTypeNames.NumericLiteral:
				return roslynClassifierColors.Number;

			case ClassificationTypeNames.Operator:
				return roslynClassifierColors.Operator;

			case ClassificationTypeNames.PreprocessorKeyword:
				return roslynClassifierColors.PreprocessorKeyword;

			case ClassificationTypeNames.PreprocessorText:
				return roslynClassifierColors.PreprocessorText;

			case ClassificationTypeNames.Punctuation:
				return roslynClassifierColors.Punctuation;

			case ClassificationTypeNames.StringLiteral:
				return roslynClassifierColors.String;

			case ClassificationTypeNames.StructName:
				return roslynClassifierColors.ValueType;

			case ClassificationTypeNames.Text:
				return roslynClassifierColors.Text;

			case ClassificationTypeNames.TypeParameterName:
				colorType = GetColorType2(cspan);
				Debug.WriteLineIf(colorType == null, "Couldn't get TypeParameterName color type");
				return colorType ?? roslynClassifierColors.TypeGenericParameter;

			case ClassificationTypeNames.VerbatimStringLiteral:
				return roslynClassifierColors.VerbatimString;

			case ClassificationTypeNames.WhiteSpace:
				return roslynClassifierColors.Text;

			case ClassificationTypeNames.XmlDocCommentAttributeName:
				return roslynClassifierColors.XmlDocCommentAttributeName;

			case ClassificationTypeNames.XmlDocCommentAttributeQuotes:
				return roslynClassifierColors.XmlDocCommentAttributeQuotes;

			case ClassificationTypeNames.XmlDocCommentAttributeValue:
				return roslynClassifierColors.XmlDocCommentAttributeValue;

			case ClassificationTypeNames.XmlDocCommentCDataSection:
				return roslynClassifierColors.XmlDocCommentCDataSection;

			case ClassificationTypeNames.XmlDocCommentComment:
				return roslynClassifierColors.XmlDocCommentComment;

			case ClassificationTypeNames.XmlDocCommentDelimiter:
				return roslynClassifierColors.XmlDocCommentDelimiter;

			case ClassificationTypeNames.XmlDocCommentEntityReference:
				return roslynClassifierColors.XmlDocCommentEntityReference;

			case ClassificationTypeNames.XmlDocCommentName:
				return roslynClassifierColors.XmlDocCommentName;

			case ClassificationTypeNames.XmlDocCommentProcessingInstruction:
				return roslynClassifierColors.XmlDocCommentProcessingInstruction;

			case ClassificationTypeNames.XmlDocCommentText:
				return roslynClassifierColors.XmlDocCommentText;

			case ClassificationTypeNames.XmlLiteralAttributeName:
				return roslynClassifierColors.XmlLiteralAttributeName;

			case ClassificationTypeNames.XmlLiteralAttributeQuotes:
				return roslynClassifierColors.XmlLiteralAttributeQuotes;

			case ClassificationTypeNames.XmlLiteralAttributeValue:
				return roslynClassifierColors.XmlLiteralAttributeValue;

			case ClassificationTypeNames.XmlLiteralCDataSection:
				return roslynClassifierColors.XmlLiteralCDataSection;

			case ClassificationTypeNames.XmlLiteralComment:
				return roslynClassifierColors.XmlLiteralComment;

			case ClassificationTypeNames.XmlLiteralDelimiter:
				return roslynClassifierColors.XmlLiteralDelimiter;

			case ClassificationTypeNames.XmlLiteralEmbeddedExpression:
				return roslynClassifierColors.XmlLiteralEmbeddedExpression;

			case ClassificationTypeNames.XmlLiteralEntityReference:
				return roslynClassifierColors.XmlLiteralEntityReference;

			case ClassificationTypeNames.XmlLiteralName:
				return roslynClassifierColors.XmlLiteralName;

			case ClassificationTypeNames.XmlLiteralProcessingInstruction:
				return roslynClassifierColors.XmlLiteralProcessingInstruction;

			case ClassificationTypeNames.XmlLiteralText:
				return roslynClassifierColors.XmlLiteralText;

			default:
				Debug.WriteLine($"Unknown ClassificationType = '{cspan.ClassificationType}'");
				return null;
			}
		}
	}
}
