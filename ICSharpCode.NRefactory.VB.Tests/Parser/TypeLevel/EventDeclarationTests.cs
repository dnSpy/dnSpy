// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class EventDeclarationTests
	{
		#region VB.NET
		[Test]
		public void VBNetSimpleEventDeclarationTest()
		{
			EventDeclaration ed = ParseUtil.ParseTypeMember<EventDeclaration>("event MyEvent(x as Integer)");
			Assert.AreEqual(1, ed.Parameters.Count);
			Assert.AreEqual("MyEvent", ed.Name);
			Assert.IsFalse(ed.HasAddRegion);
			Assert.IsFalse(ed.HasRemoveRegion);
		}
		#endregion
	}
}
