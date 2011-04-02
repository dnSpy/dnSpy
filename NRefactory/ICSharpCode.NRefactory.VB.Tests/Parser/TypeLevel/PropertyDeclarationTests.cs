// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class PropertyDeclarationTests
	{
		#region VB.NET
		[Test]
		public void VBNetSimpleGetSetPropertyDeclarationTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("Property MyProperty As Integer \n Get \n End Get \n Set \n End Set\nEnd Property");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.IsTrue(pd.HasGetRegion);
			Assert.IsTrue(pd.HasSetRegion);
		}
		
		[Test]
		public void VBNetSimpleGetPropertyDeclarationTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("ReadOnly Property MyProperty \nGet\nEnd Get\nEnd Property");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.IsTrue(pd.HasGetRegion);
			Assert.IsFalse(pd.HasSetRegion);
			Assert.IsTrue((pd.Modifier & Modifiers.ReadOnly) == Modifiers.ReadOnly);
		}
		
		[Test]
		public void VBNetSimpleSetPropertyDeclarationTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("WriteOnly Property MyProperty \n Set\nEnd Set\nEnd Property ");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.IsFalse(pd.HasGetRegion);
			Assert.IsTrue(pd.HasSetRegion);
			Assert.IsTrue((pd.Modifier & Modifiers.WriteOnly) == Modifiers.WriteOnly);
		}
		
		[Test]
		public void VBNetAutoPropertyTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("Property MyProperty");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.IsTrue(pd.HasGetRegion);
			Assert.IsTrue(pd.HasSetRegion);
			Assert.AreEqual(pd.Initializer, Expression.Null);
		}
		
		[Test]
		public void VBNetReadOnlyAutoPropertyTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("ReadOnly Property MyProperty");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.IsTrue(pd.HasGetRegion);
			Assert.IsFalse(pd.HasSetRegion);
			Assert.AreEqual(pd.Initializer, Expression.Null);
		}
		
		[Test]
		public void VBNetWriteOnlyAutoPropertyTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("WriteOnly Property MyProperty");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.IsFalse(pd.HasGetRegion);
			Assert.IsTrue(pd.HasSetRegion);
			Assert.AreEqual(pd.Initializer, Expression.Null);
		}
		
		[Test]
		public void VBNetSimpleInitializerAutoPropertyTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("Property MyProperty = 5");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.IsTrue(pd.HasGetRegion);
			Assert.IsTrue(pd.HasSetRegion);
			Assert.AreEqual(pd.Initializer.ToString(), new PrimitiveExpression(5).ToString());
		}
		
		[Test]
		public void VBNetSimpleInitializerAutoPropertyWithTypeTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("Property MyProperty As Integer = 5");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.AreEqual("System.Int32", pd.TypeReference.Type);
			Assert.IsTrue(pd.HasGetRegion);
			Assert.IsTrue(pd.HasSetRegion);
			Assert.AreEqual(pd.Initializer.ToString(), new PrimitiveExpression(5).ToString());
		}
		
		[Test]
		public void VBNetSimpleObjectInitializerAutoPropertyTest()
		{
			PropertyDeclaration pd = ParseUtil.ParseTypeMember<PropertyDeclaration>("Property MyProperty As New List");
			Assert.AreEqual("MyProperty", pd.Name);
			Assert.AreEqual("List", pd.TypeReference.Type);
			Assert.IsTrue(pd.HasGetRegion);
			Assert.IsTrue(pd.HasSetRegion);
			Assert.AreEqual(pd.Initializer.ToString(), new ObjectCreateExpression(new TypeReference("List"), null).ToString());
		}
		#endregion
	}
}
