// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.GeneralScope
{
	[TestFixture, Ignore("delegates are completely broken at the moment")]
	public class DelegateDeclarationTests
	{
		void TestParameters(DelegateDeclaration dd)
		{
			Assert.AreEqual(3, dd.Parameters.Count());
			
			Assert.AreEqual("a", ((ParameterDeclaration)dd.Parameters.ElementAt(0)).Name);
			//Assert.AreEqual("System.Int32", ((ParameterDeclaration)dd.Parameters.ElementAt(0)).TypeReference.Type);
			Assert.Ignore("check types"); // TODO
			Assert.AreEqual("secondParam", ((ParameterDeclaration)dd.Parameters.ElementAt(1)).Name);
			//Assert.AreEqual("System.Int32", ((ParameterDeclaration)dd.Parameters.ElementAt(1)).TypeReference.Type);
			
			Assert.AreEqual("lastParam", ((ParameterDeclaration)dd.Parameters.ElementAt(2)).Name);
			//Assert.AreEqual("MyObj", ((ParameterDeclaration)dd.Parameters.ElementAt(2)).TypeReference.Type);
		}
		
		[Test]
		public void SimpleCSharpDelegateDeclarationTest()
		{
			string program = "public delegate void MyDelegate(int a, int secondParam, MyObj lastParam);\n";
			DelegateDeclaration dd = ParseUtilCSharp.ParseGlobal<DelegateDeclaration>(program);
			Assert.AreEqual("MyDelegate", dd.Name);
			//Assert.AreEqual("System.Void", dd.ReturnType.Type);
			TestParameters(dd);
		}
		
		[Test, Ignore]
		public void DelegateWithoutNameDeclarationTest()
		{
			string program = "public delegate void(int a, int secondParam, MyObj lastParam);\n";
			DelegateDeclaration dd = ParseUtilCSharp.ParseGlobal<DelegateDeclaration>(program, true);
			//Assert.AreEqual("System.Void", dd.ReturnType.Type);
			//Assert.AreEqual("?", dd.Name);
			TestParameters(dd);
		}
		
		[Test, Ignore]
		public void GenericDelegateDeclarationTest()
		{
			string program = "public delegate T CreateObject<T>() where T : ICloneable;\n";
			DelegateDeclaration dd = ParseUtilCSharp.ParseGlobal<DelegateDeclaration>(program);
			Assert.AreEqual("CreateObject", dd.Name);
			//Assert.AreEqual("T", dd.ReturnType.Type);
			Assert.AreEqual(0, dd.Parameters.Count());
			/*Assert.AreEqual(1, dd.Templates.Count);
			Assert.AreEqual("T", dd.Templates[0].Name);
			Assert.AreEqual(1, dd.Templates[0].Bases.Count);
			Assert.AreEqual("ICloneable", dd.Templates[0].Bases[0].Type);*/ throw new NotImplementedException();
		}
		
		[Test]
		public void DelegateDeclarationInNamespace()
		{
			string program = "namespace N { delegate void MyDelegate(); }";
			NamespaceDeclaration nd = ParseUtilCSharp.ParseGlobal<NamespaceDeclaration>(program);
			Assert.AreEqual("MyDelegate", ((DelegateDeclaration)nd.Members.Single()).Name);
		}
		
		[Test, Ignore("inner classes not yet implemented")]
		public void DelegateDeclarationInClass()
		{
			string program = "class Outer { delegate void Inner(); }";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			Assert.AreEqual("Inner", ((DelegateDeclaration)td.Members.Single()).Name);
		}
	}
}
