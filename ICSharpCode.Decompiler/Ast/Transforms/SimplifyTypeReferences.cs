using System;

using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms.Ast
{
	public class SimplifyTypeReferences: DepthFirstAstVisitor<object, object>
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
	}
}
