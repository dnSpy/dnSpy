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
	public class LabelStatementTests
	{
		#region VB.NET
		[Test]
		public void VBNetLabelStatementTest()
		{
			MethodDeclaration method = ParseUtil.ParseTypeMember<MethodDeclaration>("Sub Test \n myLabel: Console.WriteLine() \n End Sub");
			Assert.AreEqual(2, method.Body.Children.Count);
			LabelStatement labelStmt = (LabelStatement)method.Body.Children[0];
			Assert.AreEqual("myLabel", labelStmt.Label);
		}
		#endregion 
	}
}
