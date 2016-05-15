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
using dnSpy.Contracts.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Shared.Classification {
	public struct ClassifierResult {
		public readonly Span Span;
		public readonly OutputColor Color;

		public ClassifierResult(Span span, OutputColor color) {
			this.Span = span;
			this.Color = color;
		}
	}

	public struct RoslynClassifier {
		readonly SyntaxNode syntaxRoot;
		readonly SemanticModel semanticModel;
		readonly Workspace workspace;
		readonly OutputColor? defaultColor;
		/*readonly*/ CancellationToken cancellationToken;

		public RoslynClassifier(SyntaxNode syntaxRoot, SemanticModel semanticModel, Workspace workspace, OutputColor? defaultColor, CancellationToken cancellationToken) {
			this.syntaxRoot = syntaxRoot;
			this.semanticModel = semanticModel;
			this.workspace = workspace;
			this.defaultColor = defaultColor;
			this.cancellationToken = cancellationToken;
		}

		public IEnumerable<ClassifierResult> GetClassificationColors(TextSpan textSpan) {
			foreach (var cspan in Classifier.GetClassifiedSpans(semanticModel, textSpan, workspace)) {
				var color = GetColor(cspan) ?? defaultColor;
				if (color != null)
					yield return new ClassifierResult(Span.FromBounds(cspan.TextSpan.Start, cspan.TextSpan.End), color.Value);
			}
		}

		struct SymbolResult {
			public readonly ISymbol Symbol;
			public readonly OutputColor? Color;

			public SymbolResult(ISymbol symbol) {
				Symbol = symbol;
				Color = null;
			}

			public SymbolResult(OutputColor color) {
				Symbol = null;
				Color = color;
			}
		}

		SymbolResult GetSymbolResult(TextSpan span) {
			var node = syntaxRoot.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);

			// Fix for: using DNS = System;
			if (node.Parent?.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax)
				return new SymbolResult(OutputColor.Namespace);

			var symInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
			var symbol = symInfo.Symbol ?? symInfo.CandidateSymbols.FirstOrDefault() ??
						semanticModel.GetDeclaredSymbol(node, cancellationToken);

			return new SymbolResult(symbol);
		}

		OutputColor? GetColor2(ClassifiedSpan cspan) {
			var symRes = GetSymbolResult(cspan.TextSpan);
			if (symRes.Color != null)
				return symRes.Color;
			var symbol = symRes.Symbol;
			if (symbol == null)
				return null;
the_switch:
			switch (symbol.Kind) {
			case SymbolKind.Alias:
				return OutputColor.Namespace;

			case SymbolKind.ArrayType:
			case SymbolKind.Assembly:
			case SymbolKind.DynamicType:
			case SymbolKind.ErrorType:
				break;

			case SymbolKind.Event:
				var evtSym = (IEventSymbol)symbol;
				return evtSym.IsStatic ? OutputColor.StaticEvent : OutputColor.InstanceEvent;

			case SymbolKind.Field:
				var fldSym = (IFieldSymbol)symbol;
				if (fldSym.ContainingType?.IsScriptClass == true)
					return OutputColor.Local;
				if (fldSym.ContainingType?.TypeKind == TypeKind.Enum)
					return OutputColor.EnumField;
				if (fldSym.IsConst)
					return OutputColor.LiteralField;
				if (fldSym.IsStatic)
					return OutputColor.StaticField;
				return OutputColor.InstanceField;

			case SymbolKind.Label:
				return OutputColor.Label;

			case SymbolKind.Local:
				return OutputColor.Local;

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
						return OutputColor.ExtensionMethod;
					if (methSym.IsStatic)
						return OutputColor.StaticMethod;
					return OutputColor.InstanceMethod;

				case MethodKind.ReducedExtension:
					return OutputColor.ExtensionMethod;
				}

			case SymbolKind.NetModule:
				break;

			case SymbolKind.NamedType:
				var nts = (INamedTypeSymbol)symbol;
				switch (nts.TypeKind) {
				case TypeKind.Class:
					if (nts.IsStatic)
						return OutputColor.StaticType;
					if (nts.IsSealed)
						return OutputColor.SealedType;
					return OutputColor.Type;

				case TypeKind.Delegate:
					return OutputColor.Delegate;

				case TypeKind.Enum:
					return OutputColor.Enum;

				case TypeKind.Interface:
					return OutputColor.Interface;

				case TypeKind.Struct:
					return OutputColor.ValueType;

				case TypeKind.TypeParameter:
					if ((symbol as ITypeParameterSymbol)?.DeclaringMethod != null)
						return OutputColor.MethodGenericParameter;
					return OutputColor.TypeGenericParameter;

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
				return OutputColor.Namespace;

			case SymbolKind.Parameter:
				return OutputColor.Parameter;

			case SymbolKind.PointerType:
				break;

			case SymbolKind.Property:
				var propSym = (IPropertySymbol)symbol;
				return propSym.IsStatic ? OutputColor.StaticProperty : OutputColor.InstanceProperty;

			case SymbolKind.RangeVariable:
				return OutputColor.Local;

			case SymbolKind.TypeParameter:
				return (symbol as ITypeParameterSymbol)?.DeclaringMethod != null ?
					OutputColor.MethodGenericParameter : OutputColor.TypeGenericParameter;

			case SymbolKind.Preprocessing:
				break;

			default:
				Debug.WriteLine($"Unknown SymbolKind: {symbol.Kind}");
				break;
			}

			return null;
		}

		OutputColor? GetColor(ClassifiedSpan cspan) {
			OutputColor? color;
			SymbolResult symRes;
			switch (cspan.ClassificationType) {
			case ClassificationTypeNames.ClassName:
				symRes = GetSymbolResult(cspan.TextSpan);
				if (symRes.Color != null)
					return symRes.Color;
				if (symRes.Symbol?.IsStatic == true)
					return OutputColor.StaticType;
				if (symRes.Symbol?.IsSealed == true)
					return OutputColor.SealedType;
				Debug.WriteLineIf(symRes.Symbol == null, "Couldn't get ClassName classification type");
				return OutputColor.Type;

			case ClassificationTypeNames.Comment:
				return OutputColor.Comment;

			case ClassificationTypeNames.DelegateName:
				return OutputColor.Delegate;

			case ClassificationTypeNames.EnumName:
				return OutputColor.Enum;

			case ClassificationTypeNames.ExcludedCode:
				return OutputColor.ExcludedCode;

			case ClassificationTypeNames.Identifier:
				return GetColor2(cspan);

			case ClassificationTypeNames.InterfaceName:
				return OutputColor.Interface;

			case ClassificationTypeNames.Keyword:
				return OutputColor.Keyword;

			case ClassificationTypeNames.ModuleName:
				return OutputColor.Module;

			case ClassificationTypeNames.NumericLiteral:
				return OutputColor.Number;

			case ClassificationTypeNames.Operator:
				return OutputColor.Operator;

			case ClassificationTypeNames.PreprocessorKeyword:
				return OutputColor.PreprocessorKeyword;

			case ClassificationTypeNames.PreprocessorText:
				return OutputColor.PreprocessorText;

			case ClassificationTypeNames.Punctuation:
				return OutputColor.Punctuation;

			case ClassificationTypeNames.StringLiteral:
				return OutputColor.String;

			case ClassificationTypeNames.StructName:
				return OutputColor.ValueType;

			case ClassificationTypeNames.Text:
				return OutputColor.Text;

			case ClassificationTypeNames.TypeParameterName:
				color = GetColor2(cspan);
				Debug.WriteLineIf(color == null, "Couldn't get TypeParameterName classification type");
				return color ?? OutputColor.TypeGenericParameter;

			case ClassificationTypeNames.VerbatimStringLiteral:
				return OutputColor.VerbatimString;

			case ClassificationTypeNames.WhiteSpace:
				return OutputColor.Text;

			case ClassificationTypeNames.XmlDocCommentAttributeName:
				return OutputColor.XmlDocCommentAttributeName;

			case ClassificationTypeNames.XmlDocCommentAttributeQuotes:
				return OutputColor.XmlDocCommentAttributeQuotes;

			case ClassificationTypeNames.XmlDocCommentAttributeValue:
				return OutputColor.XmlDocCommentAttributeValue;

			case ClassificationTypeNames.XmlDocCommentCDataSection:
				return OutputColor.XmlDocCommentCDataSection;

			case ClassificationTypeNames.XmlDocCommentComment:
				return OutputColor.XmlDocCommentComment;

			case ClassificationTypeNames.XmlDocCommentDelimiter:
				return OutputColor.XmlDocCommentDelimiter;

			case ClassificationTypeNames.XmlDocCommentEntityReference:
				return OutputColor.XmlDocCommentEntityReference;

			case ClassificationTypeNames.XmlDocCommentName:
				return OutputColor.XmlDocCommentName;

			case ClassificationTypeNames.XmlDocCommentProcessingInstruction:
				return OutputColor.XmlDocCommentProcessingInstruction;

			case ClassificationTypeNames.XmlDocCommentText:
				return OutputColor.XmlDocCommentText;

			case ClassificationTypeNames.XmlLiteralAttributeName:
				return OutputColor.XmlLiteralAttributeName;

			case ClassificationTypeNames.XmlLiteralAttributeQuotes:
				return OutputColor.XmlLiteralAttributeQuotes;

			case ClassificationTypeNames.XmlLiteralAttributeValue:
				return OutputColor.XmlLiteralAttributeValue;

			case ClassificationTypeNames.XmlLiteralCDataSection:
				return OutputColor.XmlLiteralCDataSection;

			case ClassificationTypeNames.XmlLiteralComment:
				return OutputColor.XmlLiteralComment;

			case ClassificationTypeNames.XmlLiteralDelimiter:
				return OutputColor.XmlLiteralDelimiter;

			case ClassificationTypeNames.XmlLiteralEmbeddedExpression:
				return OutputColor.XmlLiteralEmbeddedExpression;

			case ClassificationTypeNames.XmlLiteralEntityReference:
				return OutputColor.XmlLiteralEntityReference;

			case ClassificationTypeNames.XmlLiteralName:
				return OutputColor.XmlLiteralName;

			case ClassificationTypeNames.XmlLiteralProcessingInstruction:
				return OutputColor.XmlLiteralProcessingInstruction;

			case ClassificationTypeNames.XmlLiteralText:
				return OutputColor.XmlLiteralText;

			default:
				Debug.WriteLine($"Unknown ClassificationType = '{cspan.ClassificationType}'");
				return null;
			}
		}
	}
}
