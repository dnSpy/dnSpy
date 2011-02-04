// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Tree Node representing a field, method, property, or event.
	/// </summary>
	public sealed class MethodTreeNode : SharpTreeNode
	{
		MethodDefinition method;
		
		public MethodTreeNode(MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException("method");
			this.method = method;
		}
		
		public override object Text {
			get { return method.Name; }
		}
		
		public override object Icon {
			get {
				switch (method.Attributes & MethodAttributes.MemberAccessMask) {
					case MethodAttributes.Public:
						return Images.Method;
					case MethodAttributes.Assembly:
					case MethodAttributes.FamANDAssem:
						return Images.InternalMethod;
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
						return Images.ProtectedMethod;
					default:
						return Images.PrivateMethod;
				}
			}
		}
	}
}
