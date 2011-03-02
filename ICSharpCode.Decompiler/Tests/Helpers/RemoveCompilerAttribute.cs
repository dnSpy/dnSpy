using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using Decompiler.Transforms;

namespace ICSharpCode.Decompiler.Tests.Helpers
{
	class RemoveCompilerAttribute : DepthFirstAstVisitor<object, object>, IAstTransform
	{
		public override object VisitAttribute(NRefactory.CSharp.Attribute attribute, object data)
		{
			var section = (AttributeSection)attribute.Parent;
			SimpleType type = attribute.Type as SimpleType;
			if (section.AttributeTarget == AttributeTarget.Assembly &&
				(type.Identifier == "CompilationRelaxationsAttribute" || type.Identifier == "RuntimeCompatibilityAttribute"))
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
