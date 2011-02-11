// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Dom;

namespace ICSharpCode.NRefactory.VB.Tests.Dom
{
	[TestFixture]
	public class RaiseEventStatementTest
	{
		#region VB.NET
		[Test]
		public void VBNetRaiseEventStatementTest()
		{
			RaiseEventStatement raiseEventStatement = ParseUtil.ParseStatement<RaiseEventStatement>("RaiseEvent MyEvent(a, 5, (6))");
		}
		#endregion
	}
}
