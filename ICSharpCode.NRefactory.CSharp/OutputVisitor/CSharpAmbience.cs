// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// C# ambience.
	/// </summary>
	public class CSharpAmbience : IAmbience
	{
		public ConversionFlags ConversionFlags { get; set; }
		
		#region ConvertEntity
		public string ConvertEntity(IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			
			StringWriter writer = new StringWriter();
			ConvertEntity(entity, new TextWriterOutputFormatter(writer), FormattingOptionsFactory.CreateMono ());
			return writer.ToString();
		}
		
		public void ConvertEntity(IEntity entity, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			if (formatter == null)
				throw new ArgumentNullException("formatter");
			if (formattingPolicy == null)
				throw new ArgumentNullException("options");
			
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			EntityDeclaration node = astBuilder.ConvertEntity(entity);
			PrintModifiers(node.Modifiers, formatter);
			
			if ((ConversionFlags & ConversionFlags.ShowDefinitionKeyword) == ConversionFlags.ShowDefinitionKeyword) {
				if (node is TypeDeclaration) {
					switch (((TypeDeclaration)node).ClassType) {
						case ClassType.Class:
							formatter.WriteKeyword("class");
							break;
						case ClassType.Struct:
							formatter.WriteKeyword("struct");
							break;
						case ClassType.Interface:
							formatter.WriteKeyword("interface");
							break;
						case ClassType.Enum:
							formatter.WriteKeyword("enum");
							break;
						default:
							throw new Exception("Invalid value for ClassType");
					}
					formatter.Space();
				} else if (node is DelegateDeclaration) {
					formatter.WriteKeyword("delegate");
					formatter.Space();
				} else if (node is EventDeclaration) {
					formatter.WriteKeyword("event");
					formatter.Space();
				}
			}
			
			if ((ConversionFlags & ConversionFlags.ShowReturnType) == ConversionFlags.ShowReturnType) {
				var rt = node.GetChildByRole(Roles.Type);
				if (!rt.IsNull) {
					rt.AcceptVisitor(new CSharpOutputVisitor(formatter, formattingPolicy));
					formatter.Space();
				}
			}
			
			if (entity is ITypeDefinition)
				WriteTypeDeclarationName((ITypeDefinition)entity, formatter, formattingPolicy);
			else
				WriteMemberDeclarationName((IMember)entity, formatter, formattingPolicy);
			
			if ((ConversionFlags & ConversionFlags.ShowParameterList) == ConversionFlags.ShowParameterList && HasParameters(entity)) {
				formatter.WriteToken(entity.EntityType == EntityType.Indexer ? "[" : "(");
				bool first = true;
				foreach (var param in node.GetChildrenByRole(Roles.Parameter)) {
					if (first) {
						first = false;
					} else {
						formatter.WriteToken(",");
						formatter.Space();
					}
					param.AcceptVisitor(new CSharpOutputVisitor(formatter, formattingPolicy));
				}
				formatter.WriteToken(entity.EntityType == EntityType.Indexer ? "]" : ")");
			}
			
			if ((ConversionFlags & ConversionFlags.ShowBody) == ConversionFlags.ShowBody && !(node is TypeDeclaration)) {
				IProperty property = entity as IProperty;
				if (property != null) {
					formatter.Space();
					formatter.WriteToken("{");
					formatter.Space();
					if (property.CanGet) {
						formatter.WriteKeyword("get");
						formatter.WriteToken(";");
						formatter.Space();
					}
					if (property.CanSet) {
						formatter.WriteKeyword("set");
						formatter.WriteToken(";");
						formatter.Space();
					}
					formatter.WriteToken("}");
				} else {
					formatter.WriteToken(";");
				}
			}
		}
		
		bool HasParameters(IEntity e)
		{
			switch (e.EntityType) {
				case EntityType.TypeDefinition:
					return ((ITypeDefinition)e).Kind == TypeKind.Delegate;
				case EntityType.Indexer:
				case EntityType.Method:
				case EntityType.Operator:
				case EntityType.Constructor:
				case EntityType.Destructor:
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
		
		void WriteTypeDeclarationName(ITypeDefinition typeDef, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			if (typeDef.DeclaringTypeDefinition != null) {
				WriteTypeDeclarationName(typeDef.DeclaringTypeDefinition, formatter, formattingPolicy);
				formatter.WriteToken(".");
			} else if ((ConversionFlags & ConversionFlags.UseFullyQualifiedTypeNames) == ConversionFlags.UseFullyQualifiedTypeNames) {
				formatter.WriteIdentifier(typeDef.Namespace);
				formatter.WriteToken(".");
			}
			formatter.WriteIdentifier(typeDef.Name);
			if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList) {
				var outputVisitor = new CSharpOutputVisitor(formatter, formattingPolicy);
				outputVisitor.WriteTypeParameters(astBuilder.ConvertEntity(typeDef).GetChildrenByRole(Roles.TypeParameter));
			}
		}
		
		void WriteMemberDeclarationName(IMember member, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			if ((ConversionFlags & ConversionFlags.ShowDeclaringType) == ConversionFlags.ShowDeclaringType) {
				ConvertType(member.DeclaringType, formatter, formattingPolicy);
				formatter.WriteToken(".");
			}
			switch (member.EntityType) {
				case EntityType.Indexer:
					formatter.WriteKeyword("this");
					break;
				case EntityType.Constructor:
					formatter.WriteIdentifier(member.DeclaringType.Name);
					break;
				case EntityType.Destructor:
					formatter.WriteToken("~");
					formatter.WriteIdentifier(member.DeclaringType.Name);
					break;
				case EntityType.Operator:
					switch (member.Name) {
						case "op_Implicit":
							formatter.WriteKeyword("implicit");
							formatter.Space();
							formatter.WriteKeyword("operator");
							formatter.Space();
							ConvertType(member.ReturnType, formatter, formattingPolicy);
							break;
						case "op_Explicit":
							formatter.WriteKeyword("explicit");
							formatter.Space();
							formatter.WriteKeyword("operator");
							formatter.Space();
							ConvertType(member.ReturnType, formatter, formattingPolicy);
							break;
						default:
							formatter.WriteKeyword("operator");
							formatter.Space();
							var operatorType = OperatorDeclaration.GetOperatorType(member.Name);
							if (operatorType.HasValue)
								formatter.WriteToken(OperatorDeclaration.GetToken(operatorType.Value));
							else
								formatter.WriteIdentifier(member.Name);
							break;
					}
					break;
				default:
					formatter.WriteIdentifier(member.Name);
					break;
			}
			if ((ConversionFlags & ConversionFlags.ShowTypeParameterList) == ConversionFlags.ShowTypeParameterList && member.EntityType == EntityType.Method) {
				var outputVisitor = new CSharpOutputVisitor(formatter, formattingPolicy);
				outputVisitor.WriteTypeParameters(astBuilder.ConvertEntity(member).GetChildrenByRole(Roles.TypeParameter));
			}
		}
		
		void PrintModifiers(Modifiers modifiers, IOutputFormatter formatter)
		{
			foreach (var m in CSharpModifierToken.AllModifiers) {
				if ((modifiers & m) == m) {
					formatter.WriteKeyword(CSharpModifierToken.GetModifierName(m));
					formatter.Space();
				}
			}
		}
		#endregion
		
		public string ConvertVariable(IVariable v)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AstNode astNode = astBuilder.ConvertVariable(v);
			return astNode.GetText().TrimEnd(';', '\r', '\n');
		}
		
		public string ConvertType(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AstType astType = astBuilder.ConvertType(type);
			return astType.GetText();
		}
		
		public void ConvertType(IType type, IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
		{
			TypeSystemAstBuilder astBuilder = CreateAstBuilder();
			AstType astType = astBuilder.ConvertType(type);
			astType.AcceptVisitor(new CSharpOutputVisitor(formatter, formattingPolicy));
		}
		
		public string WrapComment(string comment)
		{
			return "// " + comment;
		}
	}
}
