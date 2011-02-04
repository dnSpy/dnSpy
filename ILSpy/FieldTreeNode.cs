// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents a field in the TreeView.
	/// </summary>
	sealed class FieldTreeNode : SharpTreeNode
	{
		readonly FieldDefinition field;
		
		public FieldTreeNode(FieldDefinition field)
		{
			if (field == null)
				throw new ArgumentNullException("field");
			this.field = field;
		}
		
		public override object Text {
			get { return field.Name; }
		}
		
		public override object Icon {
			get {
				if (field.IsLiteral)
					return Images.Literal;
				switch (field.Attributes & FieldAttributes.FieldAccessMask) {
					case FieldAttributes.Public:
						return Images.Field;
					case FieldAttributes.Assembly:
					case FieldAttributes.FamANDAssem:
						return Images.InternalField;
					case FieldAttributes.Family:
					case FieldAttributes.FamORAssem:
						return Images.ProtectedField;
					default:
						return Images.PrivateField;
				}
			}
		}
	}
}
