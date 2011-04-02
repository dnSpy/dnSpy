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
	public class OnErrorStatementTest
	{
		#region VB.NET
		[Test]
		public void VBNetOnErrorStatementTest()
		{
			OnErrorStatement onErrorStatement = ParseUtil.ParseStatement<OnErrorStatement>("On Error Goto err");
		}
		#endregion
	}
}
