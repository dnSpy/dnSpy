// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture, Ignore]
	public class AttributeSectionTests
	{
		[Test, Ignore]
		public void GlobalAttributeCSharp()
		{
			string program = @"[global::Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
[someprefix::DesignerGenerated()]
public class Form1 {
}";
			// TODO This old NRefactory test checked that [global] attributes are incorrectly applied to the following type???
			
			//TypeDeclaration decl = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			//Assert.AreEqual("Microsoft.VisualBasic.CompilerServices.DesignerGenerated", decl.Attributes.First().Attributes.Single().Name);
			//Assert.AreEqual("someprefix.DesignerGenerated", decl.Attributes.Last().Attributes.Single().Name);
		}
		
		[Test]
		public void AssemblyAttributeCSharp()
		{
			string program = @"[assembly: System.Attribute()]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual(new AstLocation(1, 1), decl.StartLocation);
			Assert.AreEqual("assembly", decl.AttributeTarget);
		}
		
		[Test]
		public void AssemblyAttributeCSharpWithNamedArguments()
		{
			string program = @"[assembly: Foo(1, namedArg: 2, prop = 3)]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual("assembly", decl.AttributeTarget);
			var a = decl.Attributes.Single();
			Assert.AreEqual("Foo", a.Type);
			Assert.AreEqual(3, a.Arguments.Count());
			
			// TODO: check arguments
		}
		
		[Test]
		public void ModuleAttributeCSharp()
		{
			string program = @"[module: System.Attribute()]";
			AttributeSection decl = ParseUtilCSharp.ParseGlobal<AttributeSection>(program);
			Assert.AreEqual(new AstLocation(1, 1), decl.StartLocation);
			Assert.AreEqual(AttributeTarget.Module, decl.AttributeTarget);
		}
		
		[Test]
		public void TypeAttributeCSharp()
		{
			string program = @"[type: System.Attribute()] class Test {}";
			TypeDeclaration type = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			AttributeSection decl = type.Attributes.Single();
			Assert.AreEqual(new AstLocation(1, 1), decl.StartLocation);
			Assert.AreEqual(AttributeTarget.Type, decl.AttributeTarget);
		}
	}
}
