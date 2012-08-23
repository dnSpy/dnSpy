using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver {
	[TestFixture]
	public class DynamicTests : ResolverTestBase {
		private void AssertNamedArgument<T>(ResolveResult rr, string parameterName, Func<T, bool> verifier) where T : ResolveResult {
			var narr = rr as NamedArgumentResolveResult;
			Assert.That(narr, Is.Not.Null);
			Assert.That(narr.ParameterName, Is.EqualTo(parameterName));
			Assert.That(narr.Argument, Is.InstanceOf<T>());
			Assert.That(verifier((T)narr.Argument), Is.True);
		}

		[Test]
		public void AccessToDynamicMember() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		$obj.SomeProperty$ = 10;
	}
}";
			var rr = Resolve<DynamicMemberResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.Target is LocalResolveResult && ((LocalResolveResult)rr.Target).Variable.Name == "obj");
			Assert.That(rr.Member, Is.EqualTo("SomeProperty"));
		}

		[Test]
		public void DynamicInvocation() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0;
		string b = null;
		$obj.SomeMethod(a, b)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));
			Assert.That(rr.Target, Is.InstanceOf<DynamicMemberResolveResult>());
			var dynamicMember = (DynamicMemberResolveResult)rr.Target;
			Assert.That(dynamicMember.Target is LocalResolveResult && ((LocalResolveResult)dynamicMember.Target).Variable.Name == "obj");
			Assert.That(dynamicMember.Member, Is.EqualTo("SomeMethod"));
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "a");
			Assert.That(rr.Arguments[1] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[1]).Variable.Name == "b");
		}

		[Test]
		public void DynamicInvocationWithNamedArguments() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, x = 0;
		string b = null;
		$obj.SomeMethod(x, param1: a, param2: b)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));
			Assert.That(rr.Target, Is.InstanceOf<DynamicMemberResolveResult>());
			var dynamicMember = (DynamicMemberResolveResult)rr.Target;
			Assert.That(dynamicMember.Target is LocalResolveResult && ((LocalResolveResult)dynamicMember.Target).Variable.Name == "obj");
			Assert.That(dynamicMember.Member, Is.EqualTo("SomeMethod"));
			Assert.That(rr.Arguments.Count, Is.EqualTo(3));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "x");
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[1], "param1", lrr => lrr.Variable.Name == "a");
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[2], "param2", lrr => lrr.Variable.Name == "b");
		}

		[Test]
		public void TwoDynamicInvocationsInARow() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, b = 0;
		$obj.SomeMethod(a)(b)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));
			Assert.That(rr.Target, Is.InstanceOf<DynamicInvocationResolveResult>());
			var innerInvocation = (DynamicInvocationResolveResult)rr.Target;
			Assert.That(innerInvocation.Target, Is.InstanceOf<DynamicMemberResolveResult>());
			var dynamicMember = (DynamicMemberResolveResult)innerInvocation.Target;
			Assert.That(dynamicMember.Target is LocalResolveResult && ((LocalResolveResult)dynamicMember.Target).Variable.Name == "obj");
			Assert.That(dynamicMember.Member, Is.EqualTo("SomeMethod"));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));
			Assert.That(innerInvocation.Arguments.Count, Is.EqualTo(1));
			Assert.That(innerInvocation.Arguments[0] is LocalResolveResult && ((LocalResolveResult)innerInvocation.Arguments[0]).Variable.Name == "a");
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "b");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithOneApplicableMethod() {
			string program = @"using System;
class TestClass {
	public void SomeMethod(int a) {}
	public void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $this.SomeMethod(obj)$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.That(rr.Member.Name, Is.EqualTo("SomeMethod"));
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Conversion.IsImplicit, Is.True);
			Assert.That(cr.Conversion.IsDynamicConversion, Is.True);
			Assert.That(cr.Input is LocalResolveResult && ((LocalResolveResult)cr.Input).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWhenBothAnOwnAndABaseMethodAreApplicable() {
			string program = @"using System;
class TestBase {
	public void SomeMethod(int a) {}
}

class TestClass : TestBase {
	public void SomeMethod(string a) {}
	public void SomeMethod(string a, int b) {}

	void F() {
		dynamic obj = null;
		var x = $this.SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.Count(), Is.EqualTo(2));
			Assert.That(mg.Methods.Any(m => m.Parameters.Count == 1 && m.DeclaringType.Name == "TestBase" && m.Name == "SomeMethod" && m.Parameters[0].Type.Name == "Int32"));
			Assert.That(mg.Methods.Any(m => m.Parameters.Count == 1 && m.DeclaringType.Name == "TestClass" && m.Name == "SomeMethod" && m.Parameters[0].Type.Name == "String"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "obj");
		}

		[Test, Ignore("Fails")]
		public void InvocationWithDynamicArgumentWhenABaseMethodIsShadowed() {
			string program = @"using System;
class TestBase {
	public void SomeMethod(int a) {}
}

class TestClass : TestBase {
	public void SomeMethod(int a) {}
	public void SomeMethod(string a, int b) {}

	void F() {
		dynamic obj = null;
		var x = $this.SomeMethod(obj)$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.That(rr.Member.Name, Is.EqualTo("SomeMethod"));
			Assert.That(rr.Member.DeclaringType.Name, Is.EqualTo("TestClass"));
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Conversion.IsImplicit, Is.True);
			Assert.That(cr.Conversion.IsDynamicConversion, Is.True);
			Assert.That(cr.Input is LocalResolveResult && ((LocalResolveResult)cr.Input).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithTwoApplicableMethods() {
			string program = @"using System;
class TestClass {
	public void SomeMethod(int a) {}
	public void SomeMethod(string a) {}
	public void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 1));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithTwoApplicableStaticMethods() {
			string program = @"using System;
class TestClass {
	public static void SomeMethod(int a) {}
	public static void SomeMethod(string a) {}
	public static void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<TypeResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 1));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithApplicableStaticAndNonStaticMethodsFavorTheNonStaticOne() {
			string program = @"using System;
class TestClass {
	public static void SomeMethod(int a) {}
	public void SomeMethod(string a) {}
	public static void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 1));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWhenTheOnlyApplicableMethodIsAnExtensionMethod() {
			string program = @"using System;
static class OtherClass {
	public void SomeMethod(this TestClass x, int a) {}
	public void SomeMethod(this TestClass x, string a) {}
	public void SomeMethod(this TestClass x, int a, string b) {}
}
class TestClass {
	void F() {
		dynamic obj = null;
		var x = $this.SomeMethod(obj)$;
	}
}";
			var rr = Resolve(program);
			Assert.That(rr.IsError, Is.True);
		}

		[Test]
		public void InvocationWithDynamicArgumentWithTwoApplicableMethodsAndNamedArguments() {
			string program = @"using System;
class TestClass {
	public void SomeMethod(int a, int i) {}
	public void SomeMethod(string a, int i) {}
	public void SomeMethod(int a, string b, int i) {}

	void F() {
		dynamic obj = null;
		int idx = 0;
		var x = $this.SomeMethod(a: obj, i: idx)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Invocation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 2) && mg.Methods.All(m => m.Parameters[1].Type.Name == "Int32"));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[0], "a", lrr => lrr.Variable.Name == "obj");
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[1], "i", lrr => lrr.Variable.Name == "idx");
		}

		[Test]
		public void IndexingDynamicObjectWithUnnamedArguments() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, b = 0;
		object o = $obj[a]$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Indexing));
			Assert.That(rr.Target is LocalResolveResult && ((LocalResolveResult)rr.Target).Variable.Name == "obj");
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "a");
		}

		[Test]
		public void IndexingDynamicObjectWithNamedArguments() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, b = 0;
		$obj[arg1: a, arg2: b]$ = 1;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Indexing));
			Assert.That(rr.Target is LocalResolveResult && ((LocalResolveResult)rr.Target).Variable.Name == "obj");
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[0], "arg1", lrr => lrr.Variable.Name == "a");
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[1], "arg2", lrr => lrr.Variable.Name == "b");
		}

		[Test]
		public void IndexingWithDynamicArgumentWithOneApplicableIndexer() {
			string program = @"using System;
class TestClass {
	public int this[int a] { get { return 0; } }
	public int this[int a, string b] { get { return 0; } }

	void F() {
		dynamic obj = null;
		var x = $this[obj]$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.That(rr.Member.Name, Is.EqualTo("Item"));
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Conversion.IsImplicit, Is.True);
			Assert.That(cr.Conversion.IsDynamicConversion, Is.True);
			Assert.That(cr.Input is LocalResolveResult && ((LocalResolveResult)cr.Input).Variable.Name == "obj");
		}

		[Test]
		public void IndexingWithDynamicArgumentWithTwoApplicableIndexersAndUnnamedArguments() {
			string program = @"using System;
class TestClass {
	public int this[int a] { get { return 0; } }
	public int this[string a] { get { return 0; } }
	void F() {
		dynamic obj = null;
		var x = $this[obj]$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Indexing));
			Assert.That(rr.Target, Is.InstanceOf<ThisResolveResult>());
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "obj");
		}

		[Test]
		public void IndexingWithDynamicArgumentWithAnApplicableBaseIndexer() {
			string program = @"using System;
class TestBase {
	public int this[int a] { get { return 0; } }
}

class TestClass : TestBase {
	public int this[string a] { get { return 0; } }
	public int this[string a, int b] { get { return 0; } }
	void F() {
		dynamic obj = null;
		var x = $this[obj]$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Indexing));
			Assert.That(rr.Target, Is.InstanceOf<ThisResolveResult>());
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "obj");
		}

		[Test, Ignore("Fails")]
		public void IndexingWithDynamicArgumentWithTheOnlyApplicableIndexerShadowingABaseIndexer() {
			string program = @"using System;
class TestBase {
	public int this[int a] { get { return 0; } }
}

class TestClass : TestBase {
	public new int this[int a] { get { return 0; } }
	public int this[int a, string b] { get { return 0; } }

	void F() {
		dynamic obj = null;
		var x = $this[obj]$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.That(rr.Member.Name, Is.EqualTo("Item"));
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Conversion.IsImplicit, Is.True);
			Assert.That(cr.Conversion.IsDynamicConversion, Is.True);
			Assert.That(cr.Input is LocalResolveResult && ((LocalResolveResult)cr.Input).Variable.Name == "obj");
		}

		[Test]
		public void IndexingWithDynamicArgumentWithTwoApplicableIndexersAndNamedArguments() {
			string program = @"using System;
class TestClass {
	public int this[int a, int i] { get { return 0; } }
	public int this[string a, int i] { get { return 0; } }
	void F() {
		dynamic obj = null;
		int idx = 0;
		var x = $this[a: obj, i: idx]$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.Indexing));
			Assert.That(rr.Target, Is.InstanceOf<ThisResolveResult>());
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[0], "a", lrr => lrr.Variable.Name == "obj");
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[1], "i", lrr => lrr.Variable.Name == "idx");
		}

  		[Test]
		public void ConstructingObjectWithDynamicArgumentWithOneApplicableConstructor() {
			string program = @"using System;
class TestClass {
	public TestClass(int a) {}
	public void TestClass(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $new TestClass(obj)$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.That(rr.Member.Name, Is.EqualTo(".ctor"));
			Assert.That(rr.TargetResult, Is.Null);
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Input is LocalResolveResult && ((LocalResolveResult)cr.Input).Variable.Name == "obj");
		}

 		[Test]
		public void ConstructingObjectWithDynamicArgumentWithTwoApplicableConstructors() {
			string program = @"using System;
class TestClass {
	public TestClass(int a, int b) {}
	public TestClass(string a, int b) {}
	public void TestClass(int a, string b) {}

	void F() {
		dynamic obj = null;
		int i = 0;
		var x = $new TestClass(obj, i)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.ObjectCreation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.Null);
			Assert.That(mg.MethodName, Is.EqualTo(".ctor"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 2 && m.Parameters[1].Type.Name == "Int32"));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == ".ctor" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0]).Variable.Name == "obj");
			Assert.That(rr.Arguments[1] is LocalResolveResult && ((LocalResolveResult)rr.Arguments[1]).Variable.Name == "i");
		}

 		[Test]
		public void ConstructingObjectWithDynamicArgumentWithTwoApplicableConstructorsAndNamedArguments() {
			string program = @"using System;
class TestClass {
	public TestClass(int arg1, int arg2) {}
	public TestClass(string arg1, int arg2) {}
	public void TestClass(int a) {}

	void F() {
		dynamic obj = null;
		int i = 0;
		var x = $new TestClass(arg1: obj, arg2: i)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.ObjectCreation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.Null);
			Assert.That(mg.MethodName, Is.EqualTo(".ctor"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 2 && m.Parameters[1].Type.Name == "Int32"));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == ".ctor" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[0], "arg1", lrr => lrr.Variable.Name == "obj");
			AssertNamedArgument<LocalResolveResult>(rr.Arguments[1], "arg2", lrr => lrr.Variable.Name == "i");
		}

 		[Test]
		public void ConstructingObjectWithDynamicArgumentWithTwoApplicableConstructorsAndInitializerStatements() {
			string program = @"using System;
class TestClass {
	public TestClass(int a, int b) {}
	public TestClass(string a, int b) {}

	public int A { get; set; }

	void F() {
		dynamic obj = null;
		int i = 0;
		int j = 0;
		var x = $new TestClass(obj, i) { A = j }$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.ObjectCreation));
			
			Assert.That(rr.InitializerStatements.Count, Is.EqualTo(1));
			var or = rr.InitializerStatements[0] as OperatorResolveResult;
			Assert.That(or, Is.Not.Null);
			Assert.That(or.OperatorType, Is.EqualTo(ExpressionType.Assign));
			var mrr = or.Operands[0] as MemberResolveResult;
			Assert.That(mrr, Is.Not.Null);
			Assert.That(mrr.TargetResult, Is.InstanceOf<InitializedObjectResolveResult>());
			Assert.That(mrr.Member.Name, Is.EqualTo("A"));
			Assert.That(or.Operands[1], Is.InstanceOf<LocalResolveResult>());
			Assert.That(((LocalResolveResult)or.Operands[1]).Variable.Name, Is.EqualTo("j"));
		}

		[Test]
		public void InitializingBaseWithDynamicArgumentAndOneApplicableConstructor() {
			string program = @"using System;
class TestBase {
	public TestBase(int a, int b) {}
	public TestBase(string a) {}
}

class TestClass : TestBase {
	private static dynamic d;
	private static int i;

	public TestClass() : $base(d, i)$ {}
}";

			var rr = Resolve<CSharpInvocationResolveResult>(program);

			Assert.That(rr.Member.Name, Is.EqualTo(".ctor"));
			Assert.That(rr.TargetResult, Is.Null);
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Member.DeclaringType.Name, Is.EqualTo("TestBase"));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Input is MemberResolveResult && ((MemberResolveResult)cr.Input).Member.Name == "d");
			Assert.That(rr.Arguments[1] is MemberResolveResult && ((MemberResolveResult)rr.Arguments[1]).Member.Name == "i");
		}

		[Test]
		public void InitializingBaseWithDynamicArgumentAndTwoApplicableConstructors() {
			string program = @"using System;
class TestBase {
	public TestBase(int a, int b) {}
	public TestBase(string a, int b) {}
	public TestBase(string a) {}
}

class TestClass : TestBase {
	private static dynamic d;
	private static int i;

	public TestClass() : $base(d, i)$ {}
}";

			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.ObjectCreation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.Null);
			Assert.That(mg.MethodName, Is.EqualTo(".ctor"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 2 && m.Parameters[1].Type.Name == "Int32"));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == ".ctor" && m.DeclaringType.Name == "TestBase"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0] is MemberResolveResult && ((MemberResolveResult)rr.Arguments[0]).Member.Name == "d");
			Assert.That(rr.Arguments[1] is MemberResolveResult && ((MemberResolveResult)rr.Arguments[1]).Member.Name == "i");
		}

		[Test]
		public void ConstructorChainingWithDynamicArgumentAndOneApplicableConstructor() {
			string program = @"using System;
class TestClass {
	private static dynamic d;
	private static int i;

	public TestClass(int a, int b) {}
	public TestClass(string a) {}

	public TestClass() : $this(d, i)$ {}
}";

			var rr = Resolve<CSharpInvocationResolveResult>(program);

			Assert.That(rr.Member.Name, Is.EqualTo(".ctor"));
			Assert.That(rr.TargetResult, Is.Null);
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Member.DeclaringType.Name, Is.EqualTo("TestClass"));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Input is MemberResolveResult && ((MemberResolveResult)cr.Input).Member.Name == "d");
			Assert.That(rr.Arguments[1] is MemberResolveResult && ((MemberResolveResult)rr.Arguments[1]).Member.Name == "i");
		}

		[Test]
		public void ConstructorChainingWithDynamicArgumentAndTwoApplicableConstructors() {
			string program = @"using System;
class TestBase {
}

class TestClass {
	private static dynamic d;
	private static int i;

	public TestClass(int a, int b) {}
	public TestClass(string a, int b) {}
	public TestClass(string a) {}

	public TestClass() : $this(d, i)$ {}
}";

			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.InvocationType, Is.EqualTo(DynamicInvocationType.ObjectCreation));

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.Null);
			Assert.That(mg.MethodName, Is.EqualTo(".ctor"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 2 && m.Parameters[1].Type.Name == "Int32"));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == ".ctor" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0] is MemberResolveResult && ((MemberResolveResult)rr.Arguments[0]).Member.Name == "d");
			Assert.That(rr.Arguments[1] is MemberResolveResult && ((MemberResolveResult)rr.Arguments[1]).Member.Name == "i");
		}
	}
}
