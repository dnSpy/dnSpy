// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.Semantics;
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
		
		[Test, Ignore("Parser produces incorrect position (attribute position doesn't include empty arg list)")]
		public void AttributeWithShortName()
		{
			string program = "using System; [$Obsolete$()] class Test {}";
			
			TypeResolveResult result = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.ObsoleteAttribute", result.Type.FullName);
		}
		
		[Test, Ignore("Parser produces incorrect position (attribute position doesn't include empty arg list)")]
		public void QualifiedAttributeWithShortName()
		{
			string program = "using System; [$System.Obsolete$()] class Test {}";
			
			TypeResolveResult result = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.ObsoleteAttribute", result.Type.FullName);
		}
		
		[Test]
		public void AttributeConstructor1()
		{
			string program = "using System; [$LoaderOptimization(3)$] class Test { }";
			var mrr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("System.LoaderOptimizationAttribute..ctor", mrr.Member.FullName);
			Assert.AreEqual("System.Byte", mrr.Member.Parameters[0].Type.Resolve(context).FullName);
		}
		
		[Test]
		public void AttributeConstructor2()
		{
			string program = "using System; [$LoaderOptimization(LoaderOptimization.NotSpecified)$] class Test { }";
			var mrr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("System.LoaderOptimizationAttribute..ctor", mrr.Member.FullName);
			Assert.AreEqual("System.LoaderOptimization", mrr.Member.Parameters[0].Type.Resolve(context).FullName);
		}
		
		[Test]
		public void AttributeWithoutArgumentListRefersToConstructor()
		{
			string program = "using System; [$Obsolete$] class Test {}";
			
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("System.ObsoleteAttribute..ctor", result.Member.FullName);
		}
		
		[Test]
		public void AttributeArgumentInClassContext1()
		{
			string program = @"using System;
[AttributeUsage($XXX$)] class MyAttribute : Attribute {
	public const AttributeTargets XXX = AttributeTargets.All;
}
";
			var result = Resolve<MemberResolveResult>(program);
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
			var result = Resolve<MemberResolveResult>(program);
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
