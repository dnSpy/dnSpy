// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class ConstructorDeclarationTests
	{
		#region VB.NET
		[Test]
		public void VBNetConstructorDeclarationTest1()
		{
			string program = @"Sub New()
								End Sub";
			ConstructorDeclaration cd = ParseUtil.ParseTypeMember<ConstructorDeclaration>(program);
			Assert.IsTrue(cd.ConstructorInitializer.IsNull);
		}
		
		[Test]
		public void VBNetConstructorDeclarationTest2()
		{
			ConstructorDeclaration cd = ParseUtil.ParseTypeMember<ConstructorDeclaration>("Sub New(x As Integer, Optional y As String) \nEnd Sub");
			Assert.AreEqual(2, cd.Parameters.Count);
			Assert.AreEqual("System.Int32", cd.Parameters[0].TypeReference.Type);
			Assert.AreEqual("System.String", cd.Parameters[1].TypeReference.Type);
			Assert.AreEqual(ParameterModifiers.Optional, cd.Parameters[1].ParamModifier & ParameterModifiers.Optional);
		}
		#endregion
	}
}
