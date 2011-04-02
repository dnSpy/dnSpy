// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class MethodDeclarationTests
	{
		#region VB.NET
		
		[Test]
		public void VBNetDefiningPartialMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(@"Partial Sub MyMethod()
			                                                                         End Sub");
			Assert.AreEqual(0, md.Parameters.Count);
			Assert.AreEqual("MyMethod", md.Name);
			Assert.IsFalse(md.IsExtensionMethod);
			Assert.AreEqual(Modifiers.Partial, md.Modifier);
		}
		
		[Test]
		public void VBNetMethodWithModifiersRegionTest()
		{
			const string program = @"public shared sub MyMethod()
				OtherMethod()
			end sub";
			
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(Modifiers.Public | Modifiers.Static, md.Modifier);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(2, md.EndLocation.Line, "EndLocation.Y");
			Assert.AreEqual(2, md.StartLocation.Column, "StartLocation.X");
		}
		
		[Test]
		public void VBNetFunctionMethodDeclarationTest()
		{
			const string program = @"public function MyFunction() as Integer
				return 1
			end function";
			
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(Modifiers.Public, md.Modifier);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(2, md.StartLocation.Column, "StartLocation.X");
			Assert.AreEqual(2, md.EndLocation.Line, "EndLocation.Y");
		}
		
		[Test]
		public void VBNetSubroutineMethodDeclarationTest()
		{
			const string program = @"public Sub MyMethod()
				OtherMethod()
			end Sub";
			
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(Modifiers.Public, md.Modifier);
			Assert.AreEqual(2, md.StartLocation.Line, "StartLocation.Y");
			Assert.AreEqual(2, md.StartLocation.Column, "StartLocation.X");
			Assert.AreEqual(2, md.EndLocation.Line, "EndLocation.Y");
		}
		
		[Test]
		public void VBNetGenericFunctionMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>("function MyMethod(Of T)(a As T) As Double\nEnd Function");
			Assert.AreEqual("System.Double", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void VBNetGenericMethodDeclarationTest()
		{
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>("Function MyMethod(Of T)(a As T) As T\nEnd Function ");
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
		}
		
		[Test]
		public void VBNetGenericMethodDeclarationWithConstraintTest()
		{
			string program = "Function MyMethod(Of T As { ISomeInterface })(a As T) As T\n End Function";
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void VBNetExtensionMethodDeclaration()
		{
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(
				@"<Extension> _
				Sub Print(s As String)
					Console.WriteLine(s)
				End Sub");
			
			Assert.AreEqual("Print", md.Name);
			
			// IsExtensionMethod is only valid for c#.
			// Assert.IsTrue(md.IsExtensionMethod);
			
			Assert.AreEqual("s", md.Parameters[0].ParameterName);
			Assert.AreEqual("System.String", md.Parameters[0].TypeReference.Type);
		}
		
		[Test]
		public void VBNetGenericMethodInInterface()
		{
			const string program = @"Interface MyInterface
				Function MyMethod(Of T As {ISomeInterface})(a As T) As T
				End Interface";
			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("T", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void VBNetGenericVoidMethodInInterface()
		{
			const string program = @"interface MyInterface
	Sub MyMethod(Of T As {ISomeInterface})(a as T)
End Interface
";
			TypeDeclaration td = ParseUtil.ParseGlobal<TypeDeclaration>(program);
			MethodDeclaration md = (MethodDeclaration)td.Children[0];
			Assert.AreEqual("System.Void", md.TypeReference.Type);
			Assert.AreEqual(1, md.Parameters.Count);
			Assert.AreEqual("T", ((ParameterDeclarationExpression)md.Parameters[0]).TypeReference.Type);
			Assert.AreEqual("a", ((ParameterDeclarationExpression)md.Parameters[0]).ParameterName);
			
			Assert.AreEqual(1, md.Templates.Count);
			Assert.AreEqual("T", md.Templates[0].Name);
			Assert.AreEqual(1, md.Templates[0].Bases.Count);
			Assert.AreEqual("ISomeInterface", md.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void VBNetMethodWithHandlesClause()
		{
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(
				@"Public Sub MyMethod(sender As Object, e As EventArgs) Handles x.y
			End Sub");
			Assert.AreEqual(new string[] { "x.y" }, md.HandlesClause.ToArray());
			
			md = ParseUtil.ParseTypeMember<MethodDeclaration>(
				@"Public Sub MyMethod() Handles Me.FormClosing
			End Sub");
			Assert.AreEqual(new string[] { "Me.FormClosing" }, md.HandlesClause.ToArray());
			
			md = ParseUtil.ParseTypeMember<MethodDeclaration>(
				@"Public Sub MyMethod() Handles MyBase.Event, Button1.Click
			End Sub");
			Assert.AreEqual(new string[] { "MyBase.Event", "Button1.Click" }, md.HandlesClause.ToArray());
		}
		
		[Test]
		public void VBNetMethodWithTypeCharactersTest()
		{
			const string program = @"Public Function Func!(ByVal Param&)
				Func! = CSingle(Param&)
			End Function";
			
			MethodDeclaration md = ParseUtil.ParseTypeMember<MethodDeclaration>(program);
			Assert.AreEqual(Modifiers.Public, md.Modifier);
		}
		
		#endregion
	}
}
