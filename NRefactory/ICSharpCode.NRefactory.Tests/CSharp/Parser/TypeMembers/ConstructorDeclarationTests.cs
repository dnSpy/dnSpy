// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class ConstructorDeclarationTests
	{
		[Test]
		public void ConstructorDeclarationTest1()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("MyClass() {}");
			Assert.IsTrue(cd.Initializer.IsNull);
		}
		
		[Test, Ignore("Constructor initializer is broken")]
		public void ConstructorDeclarationTest2()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("MyClass() : this(5) {}");
			Assert.AreEqual(ConstructorInitializerType.This, cd.Initializer.ConstructorInitializerType);
			Assert.AreEqual(1, cd.Initializer.Arguments.Count());
		}
		
		[Test, Ignore("Constructor initializer is broken")]
		public void ConstructorDeclarationTest3()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("MyClass() : base(1, 2, 3) {}");
			Assert.AreEqual(ConstructorInitializerType.Base, cd.Initializer.ConstructorInitializerType);
			Assert.AreEqual(3, cd.Initializer.Arguments.Count());
		}
		
		[Test]
		public void StaticConstructorDeclarationTest1()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("static MyClass() {}");
			Assert.IsTrue(cd.Initializer.IsNull);
			Assert.AreEqual(Modifiers.Static, cd.Modifiers);
		}
		
		[Test]
		public void ExternStaticConstructorDeclarationTest()
		{
			ConstructorDeclaration cd = ParseUtilCSharp.ParseTypeMember<ConstructorDeclaration>("extern static MyClass();");
			Assert.IsTrue(cd.Initializer.IsNull);
			Assert.AreEqual(Modifiers.Static | Modifiers.Extern, cd.Modifiers);
		}
	}
}
