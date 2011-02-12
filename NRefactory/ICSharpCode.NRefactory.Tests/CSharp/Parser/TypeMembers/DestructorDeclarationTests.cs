// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class DestructorDeclarationTests
	{
		[Test]
		public void DestructorDeclarationTest()
		{
			DestructorDeclaration dd = ParseUtilCSharp.ParseTypeMember<DestructorDeclaration>("~MyClass() {}");
		}
		
		[Test]
		public void ExternDestructorDeclarationTest()
		{
			DestructorDeclaration dd = ParseUtilCSharp.ParseTypeMember<DestructorDeclaration>("extern ~MyClass();");
			Assert.AreEqual(Modifiers.Extern, dd.Modifiers);
		}
		
		[Test]
		public void UnsafeDestructorDeclarationTest()
		{
			DestructorDeclaration dd = ParseUtilCSharp.ParseTypeMember<DestructorDeclaration>("unsafe ~MyClass() {}");
			Assert.AreEqual(Modifiers.Unsafe, dd.Modifiers);
		}
	}
}
