// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// C# ambience. Used to convert type system symbols to text (usually for displaying the symbol to the user; e.g. in editor tooltips)
	/// </summary>
	public class CSharpAmbience : IAmbience
	{
		public ConversionFlags ConversionFlags { get; set; }
		
		#region ConvertSymbol
		[Obsolete("Use ConvertSymbol() instead")]
		public string ConvertEntity(IEntity entity)
		{
			return ConvertSymbol(entity);
		}
		
		public string ConvertSymbol(ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			
			StringWriter writer = new StringWriter();
			ConvertSymbol(symbol, new TextWriterTokenWriter(writer), FormattingOptionsFactory.CreateMono ());
			return writer.ToString();
		}
		
		[Obsolete("Use ConvertSymbol() instead")]
		public void ConvertEntity(IEntity entity, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
		{
			ConvertSymbol(entity, writer, formattingPolicy);
		}
		
		public void ConvertSymbol(ISymbol symbol, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (formattingPolicy == null)
				throw new ArgumentNullException("formattingPolicy");
			
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AstNode node = astBuilder.ConvertSymbol(symbol);
			EntityDeclaration entityDecl = node as EntityDeclaration;
			if (entityDecl != null)
				PrintModifiers(entityDecl.Modifiers, writer);
			
			if ((ConversionFlags & ConversionFlags.ShowDefinitionKeyword) == ConversionFlags.ShowDefinitionKeyword) {
				if (node is TypeDeclaration) {
					switch (((TypeDeclaration)node).ClassType) {
						case ClassType.Class:
							writer.WriteKeyword(Roles.ClassKeyword, "class");
							break;
						case ClassType.Struct:
							writer.WriteKeyword(Roles.StructKeyword, "struct");
							break;
						case ClassType.Interface:
							writer.WriteKeyword(Roles.InterfaceKeyword, "interface");
							break;
						case ClassType.Enum:
							writer.WriteKeyword(Roles.EnumKeyword, "enum");
							break;
						default:
							throw new Exception("Invalid value for ClassType");
					}
					writer.Space();
				} else if (node is DelegateDeclaration) {
					writer.WriteKeyword(Roles.DelegateKeyword, "delegate");
					writer.Space();
				} else if (node is EventDeclaration) {
					writer.WriteKeyword(EventDeclaration.EventKeywordRole, "event");
					writer.Space();
				} else if (node is NamespaceDeclaration) {
					writer.WriteKeyword(Roles.NamespaceKeyword, "namespace");
					writer.Space();
				}
			}
			
			if ((ConversionFlags & ConversionFlags.ShowReturnType) == ConversionFlags.ShowReturnType) {
				var rt = node.GetChildByRole(Roles.Type);
				if (!rt.IsNull) {
					rt.AcceptVisitor(new CSharpOutputVisitor(writer, formattingPolicy));
					writer.Space();
				}
			}
			
			if (symbol is ITypeDefinition)
				WriteTypeDeclarationName((ITypeDefinition)symbol, writer, formattingPolicy);
			else if (symbol is IMember)
				WriteMemberDeclarationName((IMember)symbol, writer, formattingPolicy);
			else
				writer.WriteIdentifier(Identifier.Create(symbol.Name));
			
			if ((ConversionFlags & ConversionFlags.ShowParameterList) == ConversionFlags.ShowParameterList && HasParameters(symbol)) {
				writer.WriteToken(symbol.SymbolKind == SymbolKind.Indexer ? Roles.LBracket : Roles.LPar, symbol.SymbolKind == SymbolKind.Indexer ? "[" : "(");
				bool first = true;
				foreach (var param in node.GetChildrenByRole(Roles.Parameter)) {
					if (first) {
						first = false;
					} else {
						writer.WriteToken(Roles.Comma, ",");
						writer.Space();
					}
					param.AcceptVisitor(new CSharpOutputVisitor(writer, formattingPolicy));
				}
				writer.WriteToken(symbol.SymbolKind == SymbolKind.Indexer ? Roles.RBracket : Roles.RPar, symbol.SymbolKind == SymbolKind.Indexer ? "]" : ")");
			}
			
			if ((ConversionFlags & ConversionFlags.ShowBody) == ConversionFlags.ShowBody && !(node is TypeDeclaration)) {
				IProperty property = symbol as IProperty;
				if (property != null) {
					writer.Space();
					writer.WriteToken(Roles.LBrace, "{");
					writer.Space();
					if (property.CanGet) {
						writer.WriteKeyword(PropertyDeclaration.GetKeywordRole, "get");
						writer.WriteToken(Roles.Semicolon, ";");
						writer.Space();
					}
					if (property.CanSet) {
						writer.WriteKeyword(PropertyDeclaration.SetKeywordRole, "set");
						writer.WriteToken(Roles.Semicolon, ";");
						writer.Space();
					}
					writer.WriteToken(Roles.RBrace, "}");
				} else {
					writer.WriteToken(Roles.Semicolon, ";");
				}
			}
		}
		
		static bool HasParameters(ISymbol e)
		{
			switch (e.SymbolKind) {
				case SymbolKind.TypeDefinition:
					return ((ITypeDefinition)e).Kind == TypeKind.Delegate;
				case SymbolKind.Indexer:
				case SymbolKind.Method:
				case SymbolKind.Operator:
				case SymbolKind.Constructor:
				case SymbolKind.Destructor:
					return true;
				default:
					return false;
			}
		}
		
		TypeSystemAstBuilder CreateAstBuilder()
		{
			TypeSystemAstBuilder astBuilder = new TypeSystemAstBuilder();
			astBuilder.AddAnnotations = true;
			astBuilder.ShowModifiers = (ConversionFlags & ConversionFlags.ShowModifiers) == ConversionFlags.ShowModifiers;
			astBuilder.ShowAccessibility = (ConversionFlags & ConversionFlags.ShowAccessibility) == ConversionFlags.ShowAccessibility;
			astBuilder.AlwaysUseShortTypeNames = (ConversionFlags & ConversionFlags.UseFullyQualifiedTypeNames) != ConversionFlags.UseFullyQualifiedTypeNames;
			astBuilder.ShowParameterNames = (ConversionFlags & ConversionFlags.ShowParameterNames) == ConversionFlags.ShowParameterNames;
			return astBuilder;
		}
		
		void WriteTypeDeclarationName(ITypeDefinition typeDef, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			EntityDeclaration node = astBuilder.ConvertEntity(typeDef);
			if (typeDef.DeclaringTypeDefinition != null) {
				WriteTypeDeclarationName(typeDef.DeclaringTypeDefinition, writer, formattingPolicy);
				writer.WriteToken(Roles.Dot, ".");
			} else if ((ConversionFlags & ConversionFlags.UseFullyQualifiedEntityNames) == ConversionFlags.UseFullyQualifiedEntityNames) {
				if (!string.IsNullOrEmpty(typeDef.Namespace)) {
					WriteQualifiedName(typeDef.Namespace, writer, formattingPolicy);
					writer.WriteToken(Roles.Dot, ".");
				}
			}
			writer.WriteIdentifier(node.NameToken);
			if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList) {
				var outputVisitor = new CSharpOutputVisitor(writer, formattingPolicy);
				outputVisitor.WriteTypeParameters(node.GetChildrenByRole(Roles.TypeParameter));
			}
		}
		
		void WriteMemberDeclarationName(IMember member, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			EntityDeclaration node = astBuilder.ConvertEntity(member);
			if ((ConversionFlags & ConversionFlags.ShowDeclaringType) == ConversionFlags.ShowDeclaringType) {
				ConvertType(member.DeclaringType, writer, formattingPolicy);
				writer.WriteToken(Roles.Dot, ".");
			}
			switch (member.SymbolKind) {
				case SymbolKind.Indexer:
					writer.WriteKeyword(Roles.Identifier, "this");
					break;
				case SymbolKind.Constructor:
					WriteQualifiedName(member.DeclaringType.Name, writer, formattingPolicy);
					break;
				case SymbolKind.Destructor:
					writer.WriteToken(DestructorDeclaration.TildeRole, "~");
					WriteQualifiedName(member.DeclaringType.Name, writer, formattingPolicy);
					break;
				case SymbolKind.Operator:
					switch (member.Name) {
						case "op_Implicit":
							writer.WriteKeyword(OperatorDeclaration.ImplicitRole, "implicit");
							writer.Space();
							writer.WriteKeyword(OperatorDeclaration.OperatorKeywordRole, "operator");
							writer.Space();
							ConvertType(member.ReturnType, writer, formattingPolicy);
							break;
						case "op_Explicit":
							writer.WriteKeyword(OperatorDeclaration.ExplicitRole, "explicit");
							writer.Space();
							writer.WriteKeyword(OperatorDeclaration.OperatorKeywordRole, "operator");
							writer.Space();
							ConvertType(member.ReturnType, writer, formattingPolicy);
							break;
						default:
							writer.WriteKeyword(OperatorDeclaration.OperatorKeywordRole, "operator");
							writer.Space();
							var operatorType = OperatorDeclaration.GetOperatorType(member.Name);
							if (operatorType.HasValue)
								writer.WriteToken(OperatorDeclaration.GetRole(operatorType.Value), OperatorDeclaration.GetToken(operatorType.Value));
							else
								writer.WriteIdentifier(node.NameToken);
							break;
					}
					break;
				default:
					writer.WriteIdentifier(Identifier.Create(member.Name));
					break;
			}
			if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList && member.SymbolKind == SymbolKind.Method) {
				var outputVisitor = new CSharpOutputVisitor(writer, formattingPolicy);
				outputVisitor.WriteTypeParameters(node.GetChildrenByRole(Roles.TypeParameter));
			}
		}
		
		void PrintModifiers(Modifiers modifiers, TokenWriter writer)
		{
			foreach (var m in CSharpModifierToken.AllModifiers) {
				if ((modifiers & m) == m) {
					writer.WriteKeyword(EntityDeclaration.ModifierRole, CSharpModifierToken.GetModifierName(m));
					writer.Space();
				}
			}
		}
		
		void WriteQualifiedName(string name, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
		{
			var node = AstType.Create(name);
			var outputVisitor = new CSharpOutputVisitor(writer, formattingPolicy);
			node.AcceptVisitor(outputVisitor);
		}
		#endregion
		
		public string ConvertVariable(IVariable v)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AstNode astNode = astBuilder.ConvertVariable(v);
			return astNode.ToString().TrimEnd(';', '\r', '\n', (char)8232);
		}
		
		public string ConvertType(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			astBuilder.AlwaysUseShortTypeNames = (ConversionFlags & ConversionFlags.UseFullyQualifiedEntityNames) != ConversionFlags.UseFullyQualifiedEntityNames;
			AstType astType = astBuilder.ConvertType(type);
			return astType.ToString();
		}
		
		public void ConvertType(IType type, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			astBuilder.AlwaysUseShortTypeNames = (ConversionFlags & ConversionFlags.UseFullyQualifiedEntityNames) != ConversionFlags.UseFullyQualifiedEntityNames;
			AstType astType = astBuilder.ConvertType(type);
			astType.AcceptVisitor(new CSharpOutputVisitor(writer, formattingPolicy));
		}
		
		public string ConvertConstantValue(object constantValue)
		{
			return TextWriterTokenWriter.PrintPrimitiveValue(constantValue);
		}
		
		public string WrapComment(string comment)
		{
			return "// " + comment;
		}
	}
}
