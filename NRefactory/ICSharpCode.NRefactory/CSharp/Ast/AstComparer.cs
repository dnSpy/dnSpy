// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Compares whether two ASTs are structurally identical.
	/// </summary>
	public static class AstComparer
	{
		static HashSet<Type> nodeTypesWithoutExtraInfo = new HashSet<Type> {
			typeof(IdentifierExpression)
		};
		
		public static bool? AreEqual(AstNode node1, AstNode node2)
		{
			if (node1 == node2)
				return true;
			if (node1 == null || node1.IsNull || node2 == null || node2.IsNull)
				return false;
			Type nodeType = node1.GetType();
			if (nodeType != node2.GetType())
				return false;
			if (node1.Role != node2.Role)
				return false;
			AstNode child1 = node1.FirstChild;
			AstNode child2 = node2.FirstChild;
			bool? result = true;
			while (result != false && (child1 != null || child2 != null)) {
				result &= AreEqual(child1, child2);
				child1 = child1.NextSibling;
				child2 = child2.NextSibling;
			}
			if (nodeTypesWithoutExtraInfo.Contains(nodeType))
				return result;
			if (nodeType == typeof(Identifier))
				return ((Identifier)node1).Name == ((Identifier)node2).Name;
			return null;
		}
	}
}
