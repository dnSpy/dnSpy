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
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Shared.Classification {
	public struct ClassifierResult {
		public readonly Span Span;
		public readonly IClassificationType Type;

		public ClassifierResult(Span span, IClassificationType type) {
			this.Span = span;
			this.Type = type;
		}
	}

	public struct RoslynClassifier {
		readonly SyntaxNode syntaxRoot;
		readonly SemanticModel semanticModel;
		readonly Workspace workspace;
		readonly IThemeClassificationTypes themeClassificationTypes;
		readonly IClassificationType defaultType;
		/*readonly*/ CancellationToken cancellationToken;

		public RoslynClassifier(SyntaxNode syntaxRoot, SemanticModel semanticModel, Workspace workspace, IThemeClassificationTypes themeClassificationTypes, IClassificationType defaultType, CancellationToken cancellationToken) {
			this.syntaxRoot = syntaxRoot;
			this.semanticModel = semanticModel;
			this.workspace = workspace;
			this.themeClassificationTypes = themeClassificationTypes;
			this.defaultType = defaultType;
			this.cancellationToken = cancellationToken;
		}

		public IEnumerable<ClassifierResult> GetClassificationColors(TextSpan textSpan) {
			foreach (var cspan in Classifier.GetClassifiedSpans(semanticModel, textSpan, workspace)) {
				var color = GetClassificationType(cspan) ?? defaultType;
				if (color != null)
					yield return new ClassifierResult(Span.FromBounds(cspan.TextSpan.Start, cspan.TextSpan.End), color);
			}
		}

		struct SymbolResult {
			public readonly ISymbol Symbol;
			public readonly IClassificationType Type;

			public SymbolResult(ISymbol symbol) {
				Symbol = symbol;
				Type = null;
			}

			public SymbolResult(IClassificationType type) {
				Symbol = null;
				Type = type;
			}
		}

		SymbolResult GetSymbolResult(TextSpan span) {
			var node = syntaxRoot.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);

			// Fix for: using DNS = System;
			if (node.Parent?.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax)
				return new SymbolResult(themeClassificationTypes.GetClassificationType(ColorType.Namespace));

			var symInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
			var symbol = symInfo.Symbol ?? symInfo.CandidateSymbols.FirstOrDefault() ??
						semanticModel.GetDeclaredSymbol(node, cancellationToken);

			return new SymbolResult(symbol);
		}

		IClassificationType GetClassificationType2(ClassifiedSpan cspan) {
			var symRes = GetSymbolResult(cspan.TextSpan);
			if (symRes.Type != null)
				return symRes.Type;
			var symbol = symRes.Symbol;
			if (symbol == null)
				return null;
the_switch:
			switch (symbol.Kind) {
			case SymbolKind.Alias:
				return themeClassificationTypes.GetClassificationType(ColorType.Namespace);

			case SymbolKind.ArrayType:
			case SymbolKind.Assembly:
			case SymbolKind.DynamicType:
			case SymbolKind.ErrorType:
				break;

			case SymbolKind.Event:
				var evtSym = (IEventSymbol)symbol;
				return evtSym.IsStatic ? themeClassificationTypes.GetClassificationType(ColorType.StaticEvent) : themeClassificationTypes.GetClassificationType(ColorType.InstanceEvent);

			case SymbolKind.Field:
				var fldSym = (IFieldSymbol)symbol;
				if (fldSym.ContainingType?.IsScriptClass == true)
					return themeClassificationTypes.GetClassificationType(ColorType.Local);
				if (fldSym.ContainingType?.TypeKind == TypeKind.Enum)
					return themeClassificationTypes.GetClassificationType(ColorType.EnumField);
				if (fldSym.IsConst)
					return themeClassificationTypes.GetClassificationType(ColorType.LiteralField);
				if (fldSym.IsStatic)
					return themeClassificationTypes.GetClassificationType(ColorType.StaticField);
				return themeClassificationTypes.GetClassificationType(ColorType.InstanceField);

			case SymbolKind.Label:
				return themeClassificationTypes.GetClassificationType(ColorType.Label);

			case SymbolKind.Local:
				return themeClassificationTypes.GetClassificationType(ColorType.Local);

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
						return themeClassificationTypes.GetClassificationType(ColorType.ExtensionMethod);
					if (methSym.IsStatic)
						return themeClassificationTypes.GetClassificationType(ColorType.StaticMethod);
					return themeClassificationTypes.GetClassificationType(ColorType.InstanceMethod);

				case MethodKind.ReducedExtension:
					return themeClassificationTypes.GetClassificationType(ColorType.ExtensionMethod);
				}

			case SymbolKind.NetModule:
				break;

			case SymbolKind.NamedType:
				var nts = (INamedTypeSymbol)symbol;
				switch (nts.TypeKind) {
				case TypeKind.Class:
					if (nts.IsStatic)
						return themeClassificationTypes.GetClassificationType(ColorType.StaticType);
					if (nts.IsSealed)
						return themeClassificationTypes.GetClassificationType(ColorType.SealedType);
					return themeClassificationTypes.GetClassificationType(ColorType.Type);

				case TypeKind.Delegate:
					return themeClassificationTypes.GetClassificationType(ColorType.Delegate);

				case TypeKind.Enum:
					return themeClassificationTypes.GetClassificationType(ColorType.Enum);

				case TypeKind.Interface:
					return themeClassificationTypes.GetClassificationType(ColorType.Interface);

				case TypeKind.Struct:
					return themeClassificationTypes.GetClassificationType(ColorType.ValueType);

				case TypeKind.TypeParameter:
					if ((symbol as ITypeParameterSymbol)?.DeclaringMethod != null)
						return themeClassificationTypes.GetClassificationType(ColorType.MethodGenericParameter);
					return themeClassificationTypes.GetClassificationType(ColorType.TypeGenericParameter);

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
				return themeClassificationTypes.GetClassificationType(ColorType.Namespace);

			case SymbolKind.Parameter:
				return themeClassificationTypes.GetClassificationType(ColorType.Parameter);

			case SymbolKind.PointerType:
				break;

			case SymbolKind.Property:
				var propSym = (IPropertySymbol)symbol;
				return propSym.IsStatic ? themeClassificationTypes.GetClassificationType(ColorType.StaticProperty) : themeClassificationTypes.GetClassificationType(ColorType.InstanceProperty);

			case SymbolKind.RangeVariable:
				return themeClassificationTypes.GetClassificationType(ColorType.Local);

			case SymbolKind.TypeParameter:
				return (symbol as ITypeParameterSymbol)?.DeclaringMethod != null ?
					themeClassificationTypes.GetClassificationType(ColorType.MethodGenericParameter) : themeClassificationTypes.GetClassificationType(ColorType.TypeGenericParameter);

			case SymbolKind.Preprocessing:
				break;

			default:
				Debug.WriteLine($"Unknown SymbolKind: {symbol.Kind}");
				break;
			}

			return null;
		}

		IClassificationType GetClassificationType(ClassifiedSpan cspan) {
			IClassificationType classfiicationType;
			SymbolResult symRes;
			switch (cspan.ClassificationType) {
			case ClassificationTypeNames.ClassName:
				symRes = GetSymbolResult(cspan.TextSpan);
				if (symRes.Type != null)
					return symRes.Type;
				if (symRes.Symbol?.IsStatic == true)
					return themeClassificationTypes.GetClassificationType(ColorType.StaticType);
				if (symRes.Symbol?.IsSealed == true)
					return themeClassificationTypes.GetClassificationType(ColorType.SealedType);
				Debug.WriteLineIf(symRes.Symbol == null, "Couldn't get ClassName classification type");
				return themeClassificationTypes.GetClassificationType(ColorType.Type);

			case ClassificationTypeNames.Comment:
				return themeClassificationTypes.GetClassificationType(ColorType.Comment);

			case ClassificationTypeNames.DelegateName:
				return themeClassificationTypes.GetClassificationType(ColorType.Delegate);

			case ClassificationTypeNames.EnumName:
				return themeClassificationTypes.GetClassificationType(ColorType.Enum);

			case ClassificationTypeNames.ExcludedCode:
				return themeClassificationTypes.GetClassificationType(ColorType.ExcludedCode);

			case ClassificationTypeNames.Identifier:
				return GetClassificationType2(cspan);

			case ClassificationTypeNames.InterfaceName:
				return themeClassificationTypes.GetClassificationType(ColorType.Interface);

			case ClassificationTypeNames.Keyword:
				return themeClassificationTypes.GetClassificationType(ColorType.Keyword);

			case ClassificationTypeNames.ModuleName:
				return themeClassificationTypes.GetClassificationType(ColorType.Module);

			case ClassificationTypeNames.NumericLiteral:
				return themeClassificationTypes.GetClassificationType(ColorType.Number);

			case ClassificationTypeNames.Operator:
				return themeClassificationTypes.GetClassificationType(ColorType.Operator);

			case ClassificationTypeNames.PreprocessorKeyword:
				return themeClassificationTypes.GetClassificationType(ColorType.PreprocessorKeyword);

			case ClassificationTypeNames.PreprocessorText:
				return themeClassificationTypes.GetClassificationType(ColorType.PreprocessorText);

			case ClassificationTypeNames.Punctuation:
				return themeClassificationTypes.GetClassificationType(ColorType.Punctuation);

			case ClassificationTypeNames.StringLiteral:
				return themeClassificationTypes.GetClassificationType(ColorType.String);

			case ClassificationTypeNames.StructName:
				return themeClassificationTypes.GetClassificationType(ColorType.ValueType);

			case ClassificationTypeNames.Text:
				return themeClassificationTypes.GetClassificationType(ColorType.Text);

			case ClassificationTypeNames.TypeParameterName:
				classfiicationType = GetClassificationType2(cspan);
				Debug.WriteLineIf(classfiicationType == null, "Couldn't get TypeParameterName classification type");
				return classfiicationType ?? themeClassificationTypes.GetClassificationType(ColorType.TypeGenericParameter);

			case ClassificationTypeNames.VerbatimStringLiteral:
				return themeClassificationTypes.GetClassificationType(ColorType.VerbatimString);

			case ClassificationTypeNames.WhiteSpace:
				return themeClassificationTypes.GetClassificationType(ColorType.Text);

			case ClassificationTypeNames.XmlDocCommentAttributeName:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentAttributeName);

			case ClassificationTypeNames.XmlDocCommentAttributeQuotes:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentAttributeQuotes);

			case ClassificationTypeNames.XmlDocCommentAttributeValue:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentAttributeValue);

			case ClassificationTypeNames.XmlDocCommentCDataSection:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentCDataSection);

			case ClassificationTypeNames.XmlDocCommentComment:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentComment);

			case ClassificationTypeNames.XmlDocCommentDelimiter:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentDelimiter);

			case ClassificationTypeNames.XmlDocCommentEntityReference:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentEntityReference);

			case ClassificationTypeNames.XmlDocCommentName:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentName);

			case ClassificationTypeNames.XmlDocCommentProcessingInstruction:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentProcessingInstruction);

			case ClassificationTypeNames.XmlDocCommentText:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlDocCommentText);

			case ClassificationTypeNames.XmlLiteralAttributeName:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralAttributeName);

			case ClassificationTypeNames.XmlLiteralAttributeQuotes:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralAttributeQuotes);

			case ClassificationTypeNames.XmlLiteralAttributeValue:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralAttributeValue);

			case ClassificationTypeNames.XmlLiteralCDataSection:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralCDataSection);

			case ClassificationTypeNames.XmlLiteralComment:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralComment);

			case ClassificationTypeNames.XmlLiteralDelimiter:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralDelimiter);

			case ClassificationTypeNames.XmlLiteralEmbeddedExpression:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralEmbeddedExpression);

			case ClassificationTypeNames.XmlLiteralEntityReference:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralEntityReference);

			case ClassificationTypeNames.XmlLiteralName:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralName);

			case ClassificationTypeNames.XmlLiteralProcessingInstruction:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralProcessingInstruction);

			case ClassificationTypeNames.XmlLiteralText:
				return themeClassificationTypes.GetClassificationType(ColorType.XmlLiteralText);

			default:
				Debug.WriteLine($"Unknown ClassificationType = '{cspan.ClassificationType}'");
				return null;
			}
		}
	}
}
