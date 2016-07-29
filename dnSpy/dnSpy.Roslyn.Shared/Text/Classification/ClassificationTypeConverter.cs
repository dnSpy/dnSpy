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

//TODO: This class should be removed. It's only used by the REPL code and it should use RoslynClassifier instead.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	public struct ClassificationTypeConverter {
		readonly SemanticModel semanticModel;
		/*readonly*/ CancellationToken cancellationToken;

		public ClassificationTypeConverter(SemanticModel semanticModel, CancellationToken cancellationToken) {
			this.semanticModel = semanticModel;
			this.cancellationToken = cancellationToken;
			this.nodeDict = null;
		}

		List<SyntaxNode> GetNodes(TextSpan span) {
			// Don't make any changes to this code without also checking if the new code is at
			// least as fast as the current code. This class, and most likely this method, gets
			// called each time the user enters more text.

			List<SyntaxNode> list;
			if (nodeDict == null) {
				nodeDict = new Dictionary<TextSpan, List<SyntaxNode>>();

				foreach (var tmp in semanticModel.SyntaxTree.GetRoot(cancellationToken).DescendantNodesAndTokens(null, descendIntoTrivia: true)) {
					TextSpan textSpan;
					SyntaxNode node;
					if (tmp.IsNode) {
						node = tmp.AsNode();
						textSpan = node.Span;
					}
					else {
						var token = tmp.AsToken();
						textSpan = token.Span;
						node = token.Parent;
					}

					if (nodeDict.TryGetValue(textSpan, out list))
						list.Add(node);
					else {
						list = new List<SyntaxNode>();
						list.Add(node);
						nodeDict[textSpan] = list;
					}
				}
			}
			if (nodeDict.TryGetValue(span, out list))
				return list;
			return emptySyntaxNodeList;
		}
		static readonly List<SyntaxNode> emptySyntaxNodeList = new List<SyntaxNode>();
		Dictionary<TextSpan, List<SyntaxNode>> nodeDict;

		struct SymbolResult {
			public readonly ISymbol Symbol;
			public readonly TextColor? Color;

			public SymbolResult(ISymbol symbol) {
				this.Symbol = symbol;
				this.Color = null;
			}

			public SymbolResult(TextColor? color) {
				this.Symbol = null;
				this.Color = color;
			}
		}

		SymbolResult GetSymbolResult(TextSpan span) {
			foreach (var node in GetNodes(span)) {
				var declSym = semanticModel.GetDeclaredSymbol(node, cancellationToken);
				if (declSym != null)
					return new SymbolResult(declSym);
				var symInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
				var s = symInfo.Symbol ?? symInfo.CandidateSymbols.FirstOrDefault();
				if (s != null)
					return new SymbolResult(s);

				// Fix for: using DNS = System;
				if (node.Parent?.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax)
					return new SymbolResult(TextColor.Namespace);
			}

			return new SymbolResult();
		}

		public TextColor Convert(ClassifiedSpan span) {
			SymbolResult symRes;
			switch (span.ClassificationType) {
			case ClassificationTypeNames.ClassName:
				symRes = GetSymbolResult(span.TextSpan);
				if (symRes.Color != null)
					return symRes.Color.Value;
				if (symRes.Symbol?.IsStatic == true)
					return TextColor.StaticType;
				if (symRes.Symbol?.IsSealed == true)
					return TextColor.SealedType;
				return TextColor.Type;

			case ClassificationTypeNames.Comment:
				return TextColor.Comment;

			case ClassificationTypeNames.DelegateName:
				return TextColor.Delegate;

			case ClassificationTypeNames.EnumName:
				return TextColor.Enum;

			case ClassificationTypeNames.ExcludedCode:
				return TextColor.ExcludedCode;

			case ClassificationTypeNames.Identifier:
				symRes = GetSymbolResult(span.TextSpan);
				if (symRes.Color != null)
					return symRes.Color.Value;
				if (symRes.Symbol != null) {
					var sym = symRes.Symbol;
the_switch:
					switch (sym.Kind) {
					case SymbolKind.Alias:
						return TextColor.Namespace;

					case SymbolKind.ArrayType:
					case SymbolKind.Assembly:
					case SymbolKind.DynamicType:
					case SymbolKind.ErrorType:
						break;

					case SymbolKind.Event:
						var evtSym = (IEventSymbol)sym;
						return evtSym.IsStatic ? TextColor.StaticEvent : TextColor.InstanceEvent;

					case SymbolKind.Field:
						var fldSym = (IFieldSymbol)sym;
						if (fldSym.ContainingType?.IsScriptClass == true)
							return TextColor.Local;
						if (fldSym.ContainingType?.TypeKind == TypeKind.Enum)
							return TextColor.EnumField;
						if (fldSym.IsConst)
							return TextColor.LiteralField;
						if (fldSym.IsStatic)
							return TextColor.StaticField;
						return TextColor.InstanceField;

					case SymbolKind.Label:
						return TextColor.Label;

					case SymbolKind.Local:
						return TextColor.Local;

					case SymbolKind.Method:
						var methSym = (IMethodSymbol)sym;
						switch (methSym.MethodKind) {
						case MethodKind.Constructor:
						case MethodKind.Destructor:
						case MethodKind.StaticConstructor:
							sym = methSym.ContainingType;
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
								return TextColor.ExtensionMethod;
							if (methSym.IsStatic)
								return TextColor.StaticMethod;
							return TextColor.InstanceMethod;

						case MethodKind.ReducedExtension:
							return TextColor.ExtensionMethod;
						}

					case SymbolKind.NetModule:
						break;

					case SymbolKind.NamedType:
						var nts = (INamedTypeSymbol)sym;
						switch (nts.TypeKind) {
						case TypeKind.Class:
							if (nts.IsStatic)
								return TextColor.StaticType;
							if (nts.IsSealed)
								return TextColor.SealedType;
							return TextColor.Type;

						case TypeKind.Delegate:
							return TextColor.Delegate;

						case TypeKind.Enum:
							return TextColor.Enum;

						case TypeKind.Interface:
							return TextColor.Interface;

						case TypeKind.Struct:
							return TextColor.ValueType;

						case TypeKind.TypeParameter:
							return GetTypeParamKind(nts);

						case TypeKind.Unknown:
						case TypeKind.Array:
						case TypeKind.Dynamic:
						case TypeKind.Error:
						case TypeKind.Module:
						case TypeKind.Pointer:
						case TypeKind.Submission:
						default:
							return TextColor.Type;
						}

					case SymbolKind.Namespace:
						return TextColor.Namespace;

					case SymbolKind.Parameter:
						return TextColor.Parameter;

					case SymbolKind.PointerType:
						break;

					case SymbolKind.Property:
						var propSym = (IPropertySymbol)sym;
						return propSym.IsStatic ? TextColor.StaticProperty : TextColor.InstanceProperty;

					case SymbolKind.RangeVariable:
						return TextColor.Local;

					case SymbolKind.TypeParameter:
					case SymbolKind.Preprocessing:
						break;

					default:
						break;
					}
				}
				return TextColor.Error;

			case ClassificationTypeNames.InterfaceName:
				return TextColor.Interface;

			case ClassificationTypeNames.Keyword:
				return TextColor.Keyword;

			case ClassificationTypeNames.ModuleName:
				return TextColor.Module;

			case ClassificationTypeNames.NumericLiteral:
				return TextColor.Number;

			case ClassificationTypeNames.Operator:
				return TextColor.Operator;

			case ClassificationTypeNames.PreprocessorKeyword:
				return TextColor.PreprocessorKeyword;

			case ClassificationTypeNames.PreprocessorText:
				return TextColor.PreprocessorText;

			case ClassificationTypeNames.Punctuation:
				return TextColor.Punctuation;

			case ClassificationTypeNames.StringLiteral:
				return TextColor.String;

			case ClassificationTypeNames.StructName:
				return TextColor.ValueType;

			case ClassificationTypeNames.Text:
				return TextColor.Text;

			case ClassificationTypeNames.TypeParameterName:
				symRes = GetSymbolResult(span.TextSpan);
				if (symRes.Color != null)
					return symRes.Color.Value;
				return GetTypeParamKind(symRes.Symbol);

			case ClassificationTypeNames.VerbatimStringLiteral:
				return TextColor.VerbatimString;

			case ClassificationTypeNames.WhiteSpace:
				return TextColor.Text;

			case ClassificationTypeNames.XmlDocCommentAttributeName:
				return TextColor.XmlDocCommentAttributeName;

			case ClassificationTypeNames.XmlDocCommentAttributeQuotes:
				return TextColor.XmlDocCommentAttributeQuotes;

			case ClassificationTypeNames.XmlDocCommentAttributeValue:
				return TextColor.XmlDocCommentAttributeValue;

			case ClassificationTypeNames.XmlDocCommentCDataSection:
				return TextColor.XmlDocCommentCDataSection;

			case ClassificationTypeNames.XmlDocCommentComment:
				return TextColor.XmlDocCommentComment;

			case ClassificationTypeNames.XmlDocCommentDelimiter:
				return TextColor.XmlDocCommentDelimiter;

			case ClassificationTypeNames.XmlDocCommentEntityReference:
				return TextColor.XmlDocCommentEntityReference;

			case ClassificationTypeNames.XmlDocCommentName:
				return TextColor.XmlDocCommentName;

			case ClassificationTypeNames.XmlDocCommentProcessingInstruction:
				return TextColor.XmlDocCommentProcessingInstruction;

			case ClassificationTypeNames.XmlDocCommentText:
				return TextColor.XmlDocCommentText;

			case ClassificationTypeNames.XmlLiteralAttributeName:
				return TextColor.XmlLiteralAttributeName;

			case ClassificationTypeNames.XmlLiteralAttributeQuotes:
				return TextColor.XmlLiteralAttributeQuotes;

			case ClassificationTypeNames.XmlLiteralAttributeValue:
				return TextColor.XmlLiteralAttributeValue;

			case ClassificationTypeNames.XmlLiteralCDataSection:
				return TextColor.XmlLiteralCDataSection;

			case ClassificationTypeNames.XmlLiteralComment:
				return TextColor.XmlLiteralComment;

			case ClassificationTypeNames.XmlLiteralDelimiter:
				return TextColor.XmlLiteralDelimiter;

			case ClassificationTypeNames.XmlLiteralEmbeddedExpression:
				return TextColor.XmlLiteralEmbeddedExpression;

			case ClassificationTypeNames.XmlLiteralEntityReference:
				return TextColor.XmlLiteralEntityReference;

			case ClassificationTypeNames.XmlLiteralName:
				return TextColor.XmlLiteralName;

			case ClassificationTypeNames.XmlLiteralProcessingInstruction:
				return TextColor.XmlLiteralProcessingInstruction;

			case ClassificationTypeNames.XmlLiteralText:
				return TextColor.XmlLiteralText;

			default:
				return TextColor.Error;
			}
		}

		TextColor GetTypeParamKind(ISymbol symbol) {
			var tsym = symbol as ITypeParameterSymbol;
			if (tsym != null)
				return tsym.DeclaringMethod != null ? TextColor.MethodGenericParameter : TextColor.TypeGenericParameter;
			return TextColor.TypeGenericParameter;
		}
	}
}
