// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class AttributeTests : ResolverTestBase
	{
		[Test]
		public void NamespaceInAttributeContext()
		{
			string program = "using System; [$System.Runtime$.CompilerServices.IndexerName(\"bla\")] class Test { }";
			NamespaceResolveResult result = Resolve<NamespaceResolveResult>(program);
			Assert.AreEqual("System.Runtime", result.NamespaceName);
		}
		
		[Test]
		public void AttributeWithShortName()
		{
			string program = "using System; [$Obsolete$] class Test {}";
			
			TypeResolveResult result = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.ObsoleteAttribute", result.Type.FullName);
		}
		
		[Test]
		public void QualifiedAttributeWithShortName()
		{
			string program = "using System; [$System.Obsolete$] class Test {}";
			
			TypeResolveResult result = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.ObsoleteAttribute", result.Type.FullName);
		}
		
		[Test]
		public void AttributeConstructor1()
		{
			string program = "using System; [$LoaderOptimization(3)$] class Test { }";
			var mrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("System.LoaderOptimizationAttribute..ctor", mrr.Member.FullName);
			Assert.AreEqual("System.Byte", (mrr.Member as IMethod).Parameters[0].Type.Resolve(context).FullName);
		}
		
		[Test]
		public void AttributeConstructor2()
		{
			string program = "using System; [$LoaderOptimization(LoaderOptimization.NotSpecified)$] class Test { }";
			var mrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("System.LoaderOptimizationAttribute..ctor", mrr.Member.FullName);
			Assert.AreEqual("System.LoaderOptimization", (mrr.Member as IMethod).Parameters[0].Type.Resolve(context).FullName);
		}
		
		[Test]
		public void AttributeArgumentInClassContext1()
		{
			string program = @"using System;
[AttributeUsage($XXX$)] class MyAttribute : Attribute {
	public const AttributeTargets XXX = AttributeTargets.All;
}
";
			MemberResolveResult result = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("MyAttribute.XXX", result.Member.FullName);
		}
		
		[Test]
		public void AttributeArgumentInClassContext2()
		{
			string program = @"using System; namespace MyNamespace {
[SomeAttribute($E.A$)] class Test { }
enum E { A, B }
}
";
			MemberResolveResult result = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("MyNamespace.E.A", result.Member.FullName);
		}
		
		[Test, Ignore("Not implemented in type system.")]
		public void SD_1384()
		{
			string program = @"using System;
class Flags {
	[Flags]
	enum $Test$ { }
}";
			TypeResolveResult result = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("Flags.Test", result.Type.FullName);
			
			var rt = result.Type.GetDefinition().Attributes[0].AttributeType.Resolve(context);
			Assert.AreEqual("System.FlagsAttribute", rt.FullName);
		}
	}
}
