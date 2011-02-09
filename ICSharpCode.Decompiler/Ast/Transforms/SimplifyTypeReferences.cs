using System;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class SimplifyTypeReferences: AbstractAstTransformer
	{
		string currentNamepace = string.Empty;
		string currentClass = null;
		
		public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			currentNamepace = namespaceDeclaration.Name;
			base.VisitNamespaceDeclaration(namespaceDeclaration, data);
			currentNamepace = string.Empty;
			return null;
		}
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			currentClass = currentNamepace + "." + typeDeclaration.Name;
			base.VisitTypeDeclaration(typeDeclaration, data);
			currentClass = null;
			return null;
		}
		
		public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			IdentifierExpression id = memberReferenceExpression.TargetObject as IdentifierExpression;
			if (id != null) {
				if (id.Identifier == "System" || id.Identifier == currentClass) {
					ReplaceCurrentNode(new IdentifierExpression(memberReferenceExpression.MemberName));
					return null;
				}
				if (id.Identifier.StartsWith("System.")) {
					id.Identifier = id.Identifier.Replace("System.", "");
					return null;
				}
			}
			// we can't always remove "this", the field name might conflict with a parameter/local variable
//			if (memberReferenceExpression.TargetObject is ThisReferenceExpression) {
//				ReplaceCurrentNode(new IdentifierExpression(memberReferenceExpression.MemberName));
//				return null;
//			}
			return base.VisitMemberReferenceExpression(memberReferenceExpression, data);
		}
		
		public override object VisitTypeReference(TypeReference typeReference, object data)
		{
			string fullName = typeReference.Type;
			string shortName = GetShortName(fullName);
			if (shortName != null) {
				typeReference.Type = shortName;
				return null;
			}
			if (fullName.EndsWith("[]")) {
				shortName = GetShortName(fullName.Replace("[]",""));
				if (shortName != null) {
					typeReference.Type = shortName + "[]";
					return null;
				}
			}
			return null;
		}
		
		public string GetShortName(string fullName)
		{
			switch(fullName) {
				case "System.Boolean": return "bool";
				case "System.Byte": return "byte";
				case "System.Char": return "char";
				case "System.Decimal": return "decimal";
				case "System.Double": return "double";
				case "System.Single": return "float";
				case "System.Int32": return "int";
				case "System.Int64": return "long";
				case "System.Object": return "object";
				case "System.SByte": return "sbyte";
				case "System.Int16": return "short";
				case "System.String": return "string";
				case "System.UInt32": return "uint";
				case "System.UInt64": return "ulong";
				case "System.UInt16": return "ushort";
				case "System.Void": return "void";
			}
			return null;
		}
	}
}
