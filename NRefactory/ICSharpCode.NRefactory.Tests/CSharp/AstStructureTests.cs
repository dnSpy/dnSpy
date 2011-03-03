// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Reflection;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp
{
	[TestFixture]
	public class AstStructureTests
	{
		[Test]
		public void RolesAreStaticReadOnly()
		{
			foreach (Type type in typeof(AstNode).Assembly.GetExportedTypes()) {
				if (type.IsSubclassOf(typeof(AstNode))) {
					foreach (FieldInfo field in type.GetFields()) {
						if (field.FieldType.IsSubclassOf(typeof(Role))) {
							Assert.IsTrue(field.IsPublic);
							Assert.IsTrue(field.IsStatic);
							Assert.IsTrue(field.IsInitOnly);
							Assert.IsTrue(field.Name.EndsWith("Role", StringComparison.Ordinal));
							Assert.IsNotNull(field.GetValue(null));
						}
					}
				}
			}
		}
		
		[Test]
		public void AstNodesDoNotDeriveFromEachOther()
		{
			// Ast nodes should derive only from abstract classes; not from concrete types.
			// For example, we want to avoid that an AST consumer doing "if (node is PropertyDeclaration)"
			// unknowingly also handles IndexerDeclarations.
			foreach (Type type in typeof(AstNode).Assembly.GetExportedTypes()) {
				if (type == typeof(CSharpModifierToken)) // CSharpModifierToken is the exception (though I'm not too happy about that)
					continue;
				if (type.IsSubclassOf(typeof(AstNode))) {
					Assert.IsTrue(type.BaseType.IsAbstract, type.FullName);
				}
			}
		}
	}
}
