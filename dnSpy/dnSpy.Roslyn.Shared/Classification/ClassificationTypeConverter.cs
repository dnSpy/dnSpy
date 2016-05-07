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
using System.Linq;
using System.Threading;
using dnSpy.Contracts.TextEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Shared.Classification {
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
			public readonly OutputColor? Color;

			public SymbolResult(ISymbol symbol) {
				this.Symbol = symbol;
				this.Color = null;
			}

			public SymbolResult(OutputColor? color) {
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
					return new SymbolResult(OutputColor.Namespace);
			}

			return new SymbolResult();
		}

		public OutputColor Convert(ClassifiedSpan span) {
			SymbolResult symRes;
			switch (span.ClassificationType) {
			case ClassificationTypeNames.ClassName:
				symRes = GetSymbolResult(span.TextSpan);
				if (symRes.Color != null)
					return symRes.Color.Value;
				if (symRes.Symbol?.IsStatic == true)
					return OutputColor.StaticType;
				if (symRes.Symbol?.IsSealed == true)
					return OutputColor.SealedType;
				return OutputColor.Type;

			case ClassificationTypeNames.Comment:
				return OutputColor.Comment;

			case ClassificationTypeNames.DelegateName:
				return OutputColor.Delegate;

			case ClassificationTypeNames.EnumName:
				return OutputColor.Enum;

			case ClassificationTypeNames.ExcludedCode:
				return OutputColor.Text;

			case ClassificationTypeNames.Identifier:
				symRes = GetSymbolResult(span.TextSpan);
				if (symRes.Color != null)
					return symRes.Color.Value;
				if (symRes.Symbol != null) {
					var sym = symRes.Symbol;
the_switch:
					switch (sym.Kind) {
					case SymbolKind.Alias:
						return OutputColor.Namespace;

					case SymbolKind.ArrayType:
					case SymbolKind.Assembly:
					case SymbolKind.DynamicType:
					case SymbolKind.ErrorType:
						break;

					case SymbolKind.Event:
						var evtSym = (IEventSymbol)sym;
						return evtSym.IsStatic ? OutputColor.StaticEvent : OutputColor.InstanceEvent;

					case SymbolKind.Field:
						var fldSym = (IFieldSymbol)sym;
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
							if (methSym.IsExtensionMethod)
								return OutputColor.ExtensionMethod;
							if (methSym.IsStatic)
								return OutputColor.StaticMethod;
							return OutputColor.InstanceMethod;

						case MethodKind.ReducedExtension:
							return OutputColor.ExtensionMethod;

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
							break;
						}
						break;

					case SymbolKind.NetModule:
						break;

					case SymbolKind.NamedType:
						var nts = (INamedTypeSymbol)sym;
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
							return GetTypeParamKind(nts);

						case TypeKind.Unknown:
						case TypeKind.Array:
						case TypeKind.Dynamic:
						case TypeKind.Error:
						case TypeKind.Module:
						case TypeKind.Pointer:
						case TypeKind.Submission:
						default:
							return OutputColor.Type;
						}

					case SymbolKind.Namespace:
						return OutputColor.Namespace;

					case SymbolKind.Parameter:
						return OutputColor.Parameter;

					case SymbolKind.PointerType:
						break;

					case SymbolKind.Property:
						var propSym = (IPropertySymbol)sym;
						return propSym.IsStatic ? OutputColor.StaticProperty : OutputColor.InstanceProperty;

					case SymbolKind.RangeVariable:
						return OutputColor.Local;

					case SymbolKind.TypeParameter:
					case SymbolKind.Preprocessing:
						break;

					default:
						break;
					}
				}
				return OutputColor.Error;

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
				return OutputColor.Operator;

			case ClassificationTypeNames.StringLiteral:
				return OutputColor.String;

			case ClassificationTypeNames.StructName:
				return OutputColor.ValueType;

			case ClassificationTypeNames.Text:
				return OutputColor.Text;

			case ClassificationTypeNames.TypeParameterName:
				symRes = GetSymbolResult(span.TextSpan);
				if (symRes.Color != null)
					return symRes.Color.Value;
				return GetTypeParamKind(symRes.Symbol);

			case ClassificationTypeNames.VerbatimStringLiteral:
				return OutputColor.String;

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
				return OutputColor.Error;
			}
		}

		OutputColor GetTypeParamKind(ISymbol symbol) {
			var tsym = symbol as ITypeParameterSymbol;
			if (tsym != null)
				return tsym.DeclaringMethod != null ? OutputColor.MethodGenericParameter : OutputColor.TypeGenericParameter;
			return OutputColor.TypeGenericParameter;
		}
	}
}
