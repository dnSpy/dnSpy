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
	}
}
