// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	public class VBNetCodeGenerator : NRefactoryCodeGenerator
	{
		internal static readonly VBNetCodeGenerator Instance = new VBNetCodeGenerator();
		
		public override IOutputAstVisitor CreateOutputVisitor()
		{
			VBNetOutputVisitor v = new VBNetOutputVisitor();
			VBNetPrettyPrintOptions pOpt = v.Options;
			
			pOpt.IndentationChar = this.Options.IndentString[0];
			pOpt.IndentSize = this.Options.IndentString.Length;
			pOpt.TabSize = this.Options.IndentString.Length;
			
			return v;
		}
		
		public override string GetFieldName(string propertyName)
		{
			return "m_" + propertyName;
		}
		
		public override PropertyDeclaration CreateProperty(IField field, bool createGetter, bool createSetter)
		{
			string propertyName = GetPropertyName(field.Name);
			if (string.Equals(propertyName, field.Name, StringComparison.InvariantCultureIgnoreCase)) {
				if (HostCallback.RenameMember(field, "m_" + field.Name)) {
					field = new DefaultField(field.ReturnType, "m_" + field.Name,
					                         field.Modifiers, field.Region, field.DeclaringType);
				}
			}
			return base.CreateProperty(field, createGetter, createSetter);
		}
	}
}
