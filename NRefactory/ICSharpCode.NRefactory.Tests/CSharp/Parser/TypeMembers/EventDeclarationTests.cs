// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture, Ignore]
	public class EventDeclarationTests
	{
		[Test]
		public void SimpleEventDeclarationTest()
		{
			CustomEventDeclaration ed = ParseUtilCSharp.ParseTypeMember<CustomEventDeclaration>("event System.EventHandler MyEvent;");
			Assert.AreEqual("MyEvent", ed.Name);
			//Assert.AreEqual("System.EventHandler", ed.TypeReference.Type);
			Assert.Ignore(); // check type
			
			Assert.IsTrue(ed.AddAccessor.IsNull);
			Assert.IsTrue(ed.RemoveAccessor.IsNull);
		}
		
		/* TODO Port tests
		[Test]
		public void MultipleEventDeclarationTest()
		{
			TypeDeclaration t = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("class C { public event EventHandler A, B; }");
			Assert.AreEqual(2, t.Children.Count);
			
			EventDeclaration ed = (EventDeclaration)t.Children[0];
			Assert.AreEqual(Modifiers.Public, ed.Modifier);
			Assert.AreEqual("EventHandler", ed.TypeReference.Type);
			Assert.AreEqual("A", ed.Name);
			
			ed = (EventDeclaration)t.Children[1];
			Assert.AreEqual(Modifiers.Public, ed.Modifier);
			Assert.AreEqual("EventHandler", ed.TypeReference.Type);
			Assert.AreEqual("B", ed.Name);
		}
		
		[Test]
		public void EventImplementingInterfaceDeclarationTest()
		{
			EventDeclaration ed = ParseUtilCSharp.ParseTypeMember<EventDeclaration>("event EventHandler MyInterface.MyEvent;");
			
			Assert.AreEqual("MyEvent", ed.Name);
			Assert.AreEqual("EventHandler", ed.TypeReference.Type);
			
			Assert.IsFalse(ed.HasAddRegion);
			Assert.IsFalse(ed.HasRemoveRegion);
			
			Assert.AreEqual("MyInterface", ed.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("MyEvent", ed.InterfaceImplementations[0].MemberName);
		}
		
		[Test]
		public void EventImplementingGenericInterfaceDeclarationTest()
		{
			EventDeclaration ed = ParseUtilCSharp.ParseTypeMember<EventDeclaration>("event EventHandler MyInterface<string>.MyEvent;");
			
			Assert.AreEqual("MyEvent", ed.Name);
			Assert.AreEqual("EventHandler", ed.TypeReference.Type);
			
			Assert.IsFalse(ed.HasAddRegion);
			Assert.IsFalse(ed.HasRemoveRegion);
			
			Assert.AreEqual("MyInterface", ed.InterfaceImplementations[0].InterfaceType.Type);
			Assert.AreEqual("System.String", ed.InterfaceImplementations[0].InterfaceType.GenericTypes[0].Type);
			Assert.AreEqual("MyEvent", ed.InterfaceImplementations[0].MemberName);
		}
		
		[Test]
		public void AddRemoveEventDeclarationTest()
		{
			EventDeclaration ed = ParseUtilCSharp.ParseTypeMember<EventDeclaration>("event System.EventHandler MyEvent { add { } remove { } }");
			Assert.AreEqual("MyEvent", ed.Name);
			Assert.AreEqual("System.EventHandler", ed.TypeReference.Type);
			
			Assert.IsTrue(ed.HasAddRegion);
			Assert.IsTrue(ed.HasRemoveRegion);
		}*/
	}
}
