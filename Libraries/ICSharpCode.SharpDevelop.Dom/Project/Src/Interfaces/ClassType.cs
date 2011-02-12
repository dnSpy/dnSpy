// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

namespace ICSharpCode.SharpDevelop.Dom
{
	public enum ClassType {
		Class = ICSharpCode.NRefactory.Ast.ClassType.Class,
		Enum = ICSharpCode.NRefactory.Ast.ClassType.Enum,
		Interface = ICSharpCode.NRefactory.Ast.ClassType.Interface,
		Struct = ICSharpCode.NRefactory.Ast.ClassType.Struct,
		Delegate = 0x5,
		Module = ICSharpCode.NRefactory.Ast.ClassType.Module
	}
}
