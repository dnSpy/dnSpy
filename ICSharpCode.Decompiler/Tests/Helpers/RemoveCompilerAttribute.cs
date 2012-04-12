using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Tests.Helpers
{
	class RemoveCompilerAttribute : DepthFirstAstVisitor<object, object>, IAstTransform
	{
		public override object VisitAttribute(NRefactory.CSharp.Attribute attribute, object data)
		{
			var section = (AttributeSection)attribute.Parent;
			SimpleType type = attribute.Type as SimpleType;
			if (section.AttributeTarget == "assembly" &&
				(type.Identifier == "CompilationRelaxations" || type.Identifier == "RuntimeCompatibility" || type.Identifier == "SecurityPermission" || type.Identifier == "AssemblyVersion" || type.Identifier == "Debuggable"))
			{
				attribute.Remove();
				if (section.Attributes.Count == 0)
					section.Remove();
			}
			if (section.AttributeTarget == "module" && type.Identifier == "UnverifiableCode")
			{
				attribute.Remove();
				if (section.Attributes.Count == 0)
					section.Remove();
			}
			return null;
		}

		public void Run(AstNode node)
		{
			node.AcceptVisitor(this, null);
		}
	}
}
