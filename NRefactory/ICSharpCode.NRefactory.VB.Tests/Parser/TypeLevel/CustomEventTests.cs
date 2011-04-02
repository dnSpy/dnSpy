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
	public class CustomEventTests
	{
		#region VB.NET
		[Test]
		public void VBNetCustomEventsStatementTest()
		{
			string code = @" Public Custom Event TestEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            Handlers = CType([Delegate].Combine(Handlers, value), _
                EventHandler)
        End AddHandler

        RemoveHandler(ByVal value as EventHandler)
            Handlers = CType([Delegate].Remove(Handlers, value), _
                EventHandler)
        End RemoveHandler

        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            Dim TempHandlers As EventHandler = Handlers

            If TempHandlers IsNot Nothing Then
                TempHandlers(sender, e)
            End If
        End RaiseEvent
    End Event";
			EventDeclaration customEventDecl = ParseUtil.ParseTypeMember<EventDeclaration>(code);
			Assert.IsNotNull(customEventDecl);
			Assert.AreEqual("TestEvent", customEventDecl.Name);
		}
		#endregion
	}
}
