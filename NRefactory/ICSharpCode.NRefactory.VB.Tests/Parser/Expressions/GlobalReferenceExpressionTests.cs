// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class GlobalReferenceExpressionTests
	{
		[Test]
		public void VBNetGlobalReferenceExpressionTest()
		{
			TypeReferenceExpression tre = ParseUtil.ParseExpression<TypeReferenceExpression>("Global.System");
			Assert.IsTrue(tre.TypeReference.IsGlobal);
			Assert.AreEqual("System", tre.TypeReference.Type);
		}
		
		[Test]
		public void VBNetGlobalTypeDeclaration()
		{
			LocalVariableDeclaration lvd = ParseUtil.ParseStatement<LocalVariableDeclaration>("Dim a As Global.System.String");
			TypeReference typeRef = lvd.GetTypeForVariable(0);
			Assert.IsTrue(typeRef.IsGlobal);
			Assert.AreEqual("System.String", typeRef.Type);
		}
	}
}
