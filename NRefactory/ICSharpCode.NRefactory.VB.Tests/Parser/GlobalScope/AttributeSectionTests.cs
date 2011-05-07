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
	public class AttributeSectionTests
	{
//		[Test]
//		public void AttributeOnStructure()
//		{
//			string program = @"
//<StructLayout( LayoutKind.Explicit )> _
//Public Structure MyUnion
//
//	<FieldOffset( 0 )> Public i As Integer
//	< FieldOffset( 0 )> Public d As Double
//	
//End Structure 'MyUnion
//";
//			TypeDeclaration decl = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			Assert.AreEqual("StructLayout", decl.Attributes[0].Attributes[0].Type);
//		}
//		
//		[Test]
//		public void AttributeOnModule()
//		{
//			string program = @"
//<HideModule> _
//Public Module MyExtra
//
//	Public i As Integer
//	Public d As Double
//	
//End Module
//";
//			TypeDeclaration decl = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			Assert.AreEqual("HideModule", decl.Attributes[0].Attributes[0].Type);
//		}
//		
//		[Test]
//		public void GlobalAttribute()
//		{
//			string program = @"<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
//Public Class Form1
//	
//End Class";
//			TypeDeclaration decl = ParseUtil.ParseGlobal<TypeDeclaration>(program);
//			Assert.AreEqual("Microsoft.VisualBasic.CompilerServices.DesignerGenerated", decl.Attributes[0].Attributes[0].Type);
//		}
//		
//		[Test]
//		public void AssemblyAttribute()
//		{
//			string program = @"<assembly: System.Attribute()>";
//			AttributeSection decl = ParseUtil.ParseGlobal<AttributeSection>(program);
//			Assert.AreEqual(new Location(1, 1), decl.StartLocation);
//			Assert.AreEqual("assembly", decl.AttributeTarget);
//		}
//		
//		[Test]
//		public void ModuleAttributeTargetEscaped()
//		{
//			// check that this doesn't crash the parser:
//			ParseUtil.ParseGlobal<AttributeSection>("<[Module]: SuppressMessageAttribute>", true);
//		}
	}
}
