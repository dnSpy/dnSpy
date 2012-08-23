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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.TypeSystem.TestCase;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Base class for the type system tests.
	/// Test fixtures for specific APIs (Cecil, C# Parser) derive from this class.
	/// </summary>
	public abstract class TypeSystemTests
	{
		protected ICompilation compilation;
		
		protected ITypeDefinition GetTypeDefinition(Type type)
		{
			return compilation.FindType(type).GetDefinition();
		}
		
		[Test]
		public void SimplePublicClassTest()
		{
			ITypeDefinition c = GetTypeDefinition(typeof(SimplePublicClass));
			Assert.AreEqual(typeof(SimplePublicClass).Name, c.Name);
			Assert.AreEqual(typeof(SimplePublicClass).FullName, c.FullName);
			Assert.AreEqual(typeof(SimplePublicClass).Namespace, c.Namespace);
			Assert.AreEqual(typeof(SimplePublicClass).FullName, c.ReflectionName);
			
			Assert.AreEqual(Accessibility.Public, c.Accessibility);
			Assert.IsFalse(c.IsAbstract);
			Assert.IsFalse(c.IsSealed);
			Assert.IsFalse(c.IsStatic);
			Assert.IsFalse(c.IsShadowing);
		}
		
		[Test]
		public void SimplePublicClassMethodTest()
		{
			ITypeDefinition c = GetTypeDefinition(typeof(SimplePublicClass));
			
			IMethod method = c.Methods.Single(m => m.Name == "Method");
			Assert.AreEqual(typeof(SimplePublicClass).FullName + ".Method", method.FullName);
			Assert.AreSame(c, method.DeclaringType);
			Assert.AreEqual(Accessibility.Public, method.Accessibility);
			Assert.AreEqual(EntityType.Method, method.EntityType);
			Assert.IsFalse(method.IsVirtual);
			Assert.IsFalse(method.IsStatic);
			Assert.AreEqual(0, method.Parameters.Count);
			Assert.AreEqual(0, method.Attributes.Count);
			Assert.IsTrue(method.HasBody);
		}
		
		[Test]
		public void DynamicType()
		{
			ITypeDefinition testClass = GetTypeDefinition(typeof(DynamicTest));
			Assert.AreEqual(SpecialType.Dynamic, testClass.Properties.Single().ReturnType);
			Assert.AreEqual(0, testClass.Properties.Single().Attributes.Count);
		}
		
		[Test]
		public void DynamicTypeInGenerics()
		{
			ITypeDefinition testClass = GetTypeDefinition(typeof(DynamicTest));
			
			IMethod m1 = testClass.Methods.Single(me => me.Name == "DynamicGenerics1");
			Assert.AreEqual("System.Collections.Generic.List`1[[dynamic]]", m1.ReturnType.ReflectionName);
			Assert.AreEqual("System.Action`3[[System.Object],[dynamic[]],[System.Object]]", m1.Parameters[0].Type.ReflectionName);
			
			IMethod m2 = testClass.Methods.Single(me => me.Name == "DynamicGenerics2");
			Assert.AreEqual("System.Action`3[[System.Object],[dynamic],[System.Object]]", m2.Parameters[0].Type.ReflectionName);
			
			IMethod m3 = testClass.Methods.Single(me => me.Name == "DynamicGenerics3");
			Assert.AreEqual("System.Action`3[[System.Int32],[dynamic],[System.Object]]", m3.Parameters[0].Type.ReflectionName);
			
			IMethod m4 = testClass.Methods.Single(me => me.Name == "DynamicGenerics4");
			Assert.AreEqual("System.Action`3[[System.Int32[]],[dynamic],[System.Object]]", m4.Parameters[0].Type.ReflectionName);
			
			IMethod m5 = testClass.Methods.Single(me => me.Name == "DynamicGenerics5");
			Assert.AreEqual("System.Action`3[[System.Int32*[]],[dynamic],[System.Object]]", m5.Parameters[0].Type.ReflectionName);
			
			IMethod m6 = testClass.Methods.Single(me => me.Name == "DynamicGenerics6");
			Assert.AreEqual("System.Action`3[[System.Object],[dynamic],[System.Object]]&", m6.Parameters[0].Type.ReflectionName);
			
			IMethod m7 = testClass.Methods.Single(me => me.Name == "DynamicGenerics7");
			Assert.AreEqual("System.Action`3[[System.Int32[][,]],[dynamic],[System.Object]]", m7.Parameters[0].Type.ReflectionName);
		}
		
		[Test]
		public void DynamicParameterHasNoAttributes()
		{
			ITypeDefinition testClass = GetTypeDefinition(typeof(DynamicTest));
			IMethod m1 = testClass.Methods.Single(me => me.Name == "DynamicGenerics1");
			Assert.AreEqual(0, m1.Parameters[0].Attributes.Count);
		}
		
		[Test]
		public void AssemblyAttribute()
		{
			var attributes = compilation.MainAssembly.AssemblyAttributes;
			var typeTest = attributes.Single(a => a.AttributeType.FullName == typeof(TypeTestAttribute).FullName);
			Assert.AreEqual(3, typeTest.PositionalArguments.Count);
			// first argument is (int)42
			Assert.AreEqual(42, (int)typeTest.PositionalArguments[0].ConstantValue);
			// second argument is typeof(System.Action<>)
			TypeOfResolveResult rt = (TypeOfResolveResult)typeTest.PositionalArguments[1];
			Assert.IsFalse(rt.ReferencedType is ParameterizedType); // rt must not be constructed - it's just an unbound type
			Assert.AreEqual("System.Action", rt.ReferencedType.FullName);
			Assert.AreEqual(1, rt.ReferencedType.TypeParameterCount);
			// third argument is typeof(IDictionary<string, IList<TestAttribute>>)
			rt = (TypeOfResolveResult)typeTest.PositionalArguments[2];
			ParameterizedType crt = (ParameterizedType)rt.ReferencedType;
			Assert.AreEqual("System.Collections.Generic.IDictionary", crt.FullName);
			Assert.AreEqual("System.String", crt.TypeArguments[0].FullName);
			// ? for NUnit.TestAttribute (because that assembly isn't in ctx)
			Assert.AreEqual("System.Collections.Generic.IList`1[[?]]", crt.TypeArguments[1].ReflectionName);
		}
		
		[Test]
		public void TypeForwardedTo_Attribute()
		{
			var attributes = compilation.MainAssembly.AssemblyAttributes;
			var forwardAttribute = attributes.Single(a => a.AttributeType.FullName == typeof(TypeForwardedToAttribute).FullName);
			Assert.AreEqual(1, forwardAttribute.PositionalArguments.Count);
			TypeOfResolveResult rt = (TypeOfResolveResult)forwardAttribute.PositionalArguments[0];
			Assert.AreEqual("System.Func`2", rt.ReferencedType.ReflectionName);
		}
		
		[Test]
		public void TestClassTypeParameters()
		{
			var testClass = GetTypeDefinition(typeof(GenericClass<,>));
			Assert.AreEqual(EntityType.TypeDefinition, testClass.TypeParameters[0].OwnerType);
			Assert.AreEqual(EntityType.TypeDefinition, testClass.TypeParameters[1].OwnerType);
			Assert.AreSame(testClass.TypeParameters[1], testClass.TypeParameters[0].DirectBaseTypes.First());
		}
		
		[Test]
		public void TestMethod()
		{
			var testClass = GetTypeDefinition(typeof(GenericClass<,>));
			
			IMethod m = testClass.Methods.Single(me => me.Name == "TestMethod");
			Assert.AreEqual("K", m.TypeParameters[0].Name);
			Assert.AreEqual("V", m.TypeParameters[1].Name);
			Assert.AreEqual(EntityType.Method, m.TypeParameters[0].OwnerType);
			Assert.AreEqual(EntityType.Method, m.TypeParameters[1].OwnerType);
			
			Assert.AreEqual("System.IComparable`1[[``1]]", m.TypeParameters[0].DirectBaseTypes.First().ReflectionName);
			Assert.AreSame(m.TypeParameters[0], m.TypeParameters[1].DirectBaseTypes.First());
		}
		
		[Test]
		public void GetIndex()
		{
			var testClass = GetTypeDefinition(typeof(GenericClass<,>));
			
			IMethod m = testClass.Methods.Single(me => me.Name == "GetIndex");
			Assert.AreEqual("T", m.TypeParameters[0].Name);
			Assert.AreEqual(EntityType.Method, m.TypeParameters[0].OwnerType);
			Assert.AreSame(m, m.TypeParameters[0].Owner);
			
			ParameterizedType constraint = (ParameterizedType)m.TypeParameters[0].DirectBaseTypes.First();
			Assert.AreEqual("IEquatable", constraint.Name);
			Assert.AreEqual(1, constraint.TypeParameterCount);
			Assert.AreEqual(1, constraint.TypeArguments.Count);
			Assert.AreSame(m.TypeParameters[0], constraint.TypeArguments[0]);
			Assert.AreSame(m.TypeParameters[0], m.Parameters[0].Type);
		}
		
		[Test]
		public void GetIndexSpecializedTypeParameter()
		{
			var testClass = GetTypeDefinition(typeof(GenericClass<,>));
			var methodDef = testClass.Methods.Single(me => me.Name == "GetIndex");
			var m = new SpecializedMethod(methodDef, new TypeParameterSubstitution(
				new[] { compilation.FindType(KnownTypeCode.Int16), compilation.FindType(KnownTypeCode.Int32) },
				null
			));
			
			Assert.AreEqual("T", m.TypeParameters[0].Name);
			Assert.AreEqual(EntityType.Method, m.TypeParameters[0].OwnerType);
			Assert.AreSame(m, m.TypeParameters[0].Owner);
			
			ParameterizedType constraint = (ParameterizedType)m.TypeParameters[0].DirectBaseTypes.First();
			Assert.AreEqual("IEquatable", constraint.Name);
			Assert.AreEqual(1, constraint.TypeParameterCount);
			Assert.AreEqual(1, constraint.TypeArguments.Count);
			Assert.AreSame(m.TypeParameters[0], constraint.TypeArguments[0]);
			Assert.AreSame(m.TypeParameters[0], m.Parameters[0].Type);
		}
		
		[Test]
		public void GetIndexDoubleSpecialization()
		{
			var testClass = GetTypeDefinition(typeof(GenericClass<,>));
			// GenericClass<A, B>.GetIndex<T>
			var methodDef = testClass.Methods.Single(me => me.Name == "GetIndex");
			
			// GenericClass<B, A>.GetIndex<A>
			var m1 = new SpecializedMethod(methodDef, new TypeParameterSubstitution(
				new[] { testClass.TypeParameters[1], testClass.TypeParameters[0] },
				new[] { testClass.TypeParameters[0] }
			));
			// GenericClass<string, int>.GetIndex<int>
			var m2 = new SpecializedMethod(m1, new TypeParameterSubstitution(
				new[] { compilation.FindType(KnownTypeCode.Int32), compilation.FindType(KnownTypeCode.String) },
				null
			));
			
			// GenericClass<string, int>.GetIndex<int>
			var m12 = new SpecializedMethod(methodDef, new TypeParameterSubstitution(
				new[] { compilation.FindType(KnownTypeCode.String), compilation.FindType(KnownTypeCode.Int32) },
				new[] { compilation.FindType(KnownTypeCode.Int32) }
			));
			Assert.AreEqual(m12, m2);
		}
		
		[Test]
		public void Specialized_GetIndex_ToMemberReference()
		{
			var method = compilation.FindType(typeof(GenericClass<string, object>)).GetMethods(m => m.Name == "GetIndex").Single();
			Assert.AreSame(method.TypeParameters[0], method.Parameters[0].Type);
			Assert.AreSame(method, method.TypeParameters[0].Owner);
			Assert.IsInstanceOf<SpecializedMethod>(method);
			Assert.AreEqual(0, ((SpecializedMethod)method).TypeArguments.Count); // the method itself is not specialized
			var methodReference = method.ToMemberReference();
			var resolvedMethod = methodReference.Resolve(compilation.TypeResolveContext);
			Assert.AreEqual(method, resolvedMethod);
		}
		
		[Test]
		public void Specialized_GetIndex_SpecializeWithIdentityHasNoEffect()
		{
			var genericClass = compilation.FindType(typeof(GenericClass<string, object>));
			IType[] methodTypeArguments = { DummyTypeParameter.GetMethodTypeParameter(0) };
			var method = (SpecializedMethod)genericClass.GetMethods(methodTypeArguments, m => m.Name == "GetIndex").Single();
			// GenericClass<string,object>.GetIndex<!!0>()
			Assert.AreSame(method, method.TypeParameters[0].Owner);
			Assert.AreNotEqual(method.TypeParameters[0], method.TypeArguments[0]);
			Assert.IsNull(((ITypeParameter)method.TypeArguments[0]).Owner);
			// Now apply identity substitution:
			var method2 = new SpecializedMethod(method, TypeParameterSubstitution.Identity);
			Assert.AreSame(method2, method2.TypeParameters[0].Owner);
			Assert.AreNotEqual(method2.TypeParameters[0], method2.TypeArguments[0]);
			Assert.IsNull(((ITypeParameter)method2.TypeArguments[0]).Owner);
			
			Assert.AreEqual(method, method2);
		}
		
		[Test]
		public void GenericEnum()
		{
			var testClass = GetTypeDefinition(typeof(GenericClass<,>.NestedEnum));
			Assert.AreEqual(2, testClass.TypeParameterCount);
		}
		
		[Test]
		public void FieldInGenericClassWithNestedEnumType()
		{
			var testClass = GetTypeDefinition(typeof(GenericClass<,>));
			var enumClass = GetTypeDefinition(typeof(GenericClass<,>.NestedEnum));
			var field = testClass.Fields.Single(f => f.Name == "EnumField");
			Assert.AreEqual(new ParameterizedType(enumClass, testClass.TypeParameters), field.ReturnType);
		}
		
		[Test]
		public void GenericEnumMemberReturnType()
		{
			var enumClass = GetTypeDefinition(typeof(GenericClass<,>.NestedEnum));
			var field = enumClass.Fields.Single(f => f.Name == "EnumMember");
			Assert.AreEqual(new ParameterizedType(enumClass, enumClass.TypeParameters), field.ReturnType);
		}
		
		[Test]
		public void PropertyWithProtectedSetter()
		{
			var testClass = GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.Name == "PropertyWithProtectedSetter");
			Assert.IsTrue(p.CanGet);
			Assert.IsTrue(p.CanSet);
			Assert.AreEqual(Accessibility.Public, p.Accessibility);
			Assert.AreEqual(Accessibility.Public, p.Getter.Accessibility);
			Assert.AreEqual(Accessibility.Protected, p.Setter.Accessibility);
		}
		
		[Test]
		public void PropertyWithPrivateSetter()
		{
			var testClass = GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.Name == "PropertyWithPrivateSetter");
			Assert.IsTrue(p.CanGet);
			Assert.IsTrue(p.CanSet);
			Assert.AreEqual(Accessibility.Public, p.Accessibility);
			Assert.AreEqual(Accessibility.Public, p.Getter.Accessibility);
			Assert.AreEqual(Accessibility.Private, p.Setter.Accessibility);
			Assert.IsTrue(p.Getter.HasBody);
		}
		
		[Test]
		public void PropertyWithoutSetter()
		{
			var testClass = GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.Name == "PropertyWithoutSetter");
			Assert.IsTrue(p.CanGet);
			Assert.IsFalse(p.CanSet);
			Assert.AreEqual(Accessibility.Public, p.Accessibility);
			Assert.AreEqual(Accessibility.Public, p.Getter.Accessibility);
			Assert.IsNull(p.Setter);
		}
		
		[Test]
		public void Indexer()
		{
			var testClass = GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.IsIndexer);
			Assert.AreEqual("Item", p.Name);
			Assert.AreEqual(new[] { "index" }, p.Parameters.Select(x => x.Name).ToArray());
		}
		
		[Test]
		public void IndexerGetter()
		{
			var testClass = GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.IsIndexer);
			Assert.IsTrue(p.CanGet);
			Assert.AreEqual(EntityType.Accessor, p.Getter.EntityType);
			Assert.AreEqual("get_Item", p.Getter.Name);
			Assert.AreEqual(Accessibility.Public, p.Getter.Accessibility);
			Assert.AreEqual(new[] { "index" }, p.Getter.Parameters.Select(x => x.Name).ToArray());
			Assert.AreEqual("System.String", p.Getter.ReturnType.ReflectionName);
			Assert.AreEqual(p, p.Getter.AccessorOwner);
		}
		
		[Test]
		public void IndexerSetter()
		{
			var testClass = GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.IsIndexer);
			Assert.IsTrue(p.CanSet);
			Assert.AreEqual(EntityType.Accessor, p.Setter.EntityType);
			Assert.AreEqual("set_Item", p.Setter.Name);
			Assert.AreEqual(Accessibility.Public, p.Setter.Accessibility);
			Assert.AreEqual(new[] { "index", "value" }, p.Setter.Parameters.Select(x => x.Name).ToArray());
			Assert.AreEqual(TypeKind.Void, p.Setter.ReturnType.Kind);
		}
		
		[Test]
		public void GenericPropertyGetter()
		{
			var type = compilation.FindType(typeof(GenericClass<string, object>));
			var prop = type.GetProperties(p => p.Name == "Property").Single();
			Assert.AreEqual("System.String", prop.Getter.ReturnType.ReflectionName);
			Assert.IsTrue(prop.Getter.IsAccessor);
			Assert.AreEqual(prop, prop.Getter.AccessorOwner);
		}
		
		[Test]
		public void EnumTest()
		{
			var e = GetTypeDefinition(typeof(MyEnum));
			Assert.AreEqual(TypeKind.Enum, e.Kind);
			Assert.AreEqual(false, e.IsReferenceType);
			Assert.AreEqual("System.Int16", e.EnumUnderlyingType.ReflectionName);
			Assert.AreEqual(new[] { "System.Enum" }, e.DirectBaseTypes.Select(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void EnumFieldsTest()
		{
			var e = GetTypeDefinition(typeof(MyEnum));
			IField[] fields = e.Fields.ToArray();
			Assert.AreEqual(5, fields.Length);
			
			foreach (IField f in fields) {
				Assert.IsTrue(f.IsStatic);
				Assert.IsTrue(f.IsConst);
				Assert.AreEqual(Accessibility.Public, f.Accessibility);
				Assert.AreSame(e, f.Type);
				Assert.AreEqual(typeof(short), f.ConstantValue.GetType());
			}
			
			Assert.AreEqual("First", fields[0].Name);
			Assert.AreEqual(0, fields[0].ConstantValue);
			
			Assert.AreEqual("Second", fields[1].Name);
			Assert.AreSame(e, fields[1].Type);
			Assert.AreEqual(1, fields[1].ConstantValue);
			
			Assert.AreEqual("Flag1", fields[2].Name);
			Assert.AreEqual(0x10, fields[2].ConstantValue);

			Assert.AreEqual("Flag2", fields[3].Name);
			Assert.AreEqual(0x20, fields[3].ConstantValue);
			
			Assert.AreEqual("CombinedFlags", fields[4].Name);
			Assert.AreEqual(0x30, fields[4].ConstantValue);
		}
		
		[Test]
		public void GetNestedTypesFromBaseClassTest()
		{
			ITypeDefinition d = GetTypeDefinition(typeof(Derived<,>));
			
			IType pBase = d.DirectBaseTypes.Single();
			Assert.AreEqual(typeof(Base<>).FullName + "[[`1]]", pBase.ReflectionName);
			// Base[`1].GetNestedTypes() = { Base`1+Nested`1[`1, unbound] }
			Assert.AreEqual(new[] { typeof(Base<>.Nested<>).FullName + "[[`1],[]]" },
			                pBase.GetNestedTypes().Select(n => n.ReflectionName).ToArray());
			
			// Derived.GetNestedTypes() = { Base`1+Nested`1[`1, unbound] }
			Assert.AreEqual(new[] { typeof(Base<>.Nested<>).FullName + "[[`1],[]]" },
			                d.GetNestedTypes().Select(n => n.ReflectionName).ToArray());
			// This is 'leaking' the type parameter from B as is usual when retrieving any members from an unbound type.
		}
		
		[Test]
		public void ParameterizedTypeGetNestedTypesFromBaseClassTest()
		{
			// Derived[string,int].GetNestedTypes() = { Base`1+Nested`1[int, unbound] }
			var d = compilation.FindType(typeof(Derived<string, int>));
			Assert.AreEqual(new[] { typeof(Base<>.Nested<>).FullName + "[[System.Int32],[]]" },
			                d.GetNestedTypes().Select(n => n.ReflectionName).ToArray());
		}
		
		[Test]
		public void ConstraintsOnOverrideAreInherited()
		{
			ITypeDefinition d = GetTypeDefinition(typeof(Derived<,>));
			ITypeParameter tp = d.Methods.Single(m => m.Name == "GenericMethodWithConstraints").TypeParameters.Single();
			Assert.AreEqual("Y", tp.Name);
			Assert.IsFalse(tp.HasValueTypeConstraint);
			Assert.IsFalse(tp.HasReferenceTypeConstraint);
			Assert.IsTrue(tp.HasDefaultConstructorConstraint);
			Assert.AreEqual(new string[] { "System.Collections.Generic.IComparer`1[[`1]]", "System.Object" },
			                tp.DirectBaseTypes.Select(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void DefaultConstructorAddedToStruct()
		{
			var ctors = compilation.FindType(typeof(MyStructWithCtor)).GetConstructors();
			Assert.AreEqual(2, ctors.Count());
			Assert.IsFalse(ctors.Any(c => c.IsStatic));
			Assert.IsTrue(ctors.All(c => c.ReturnType.Kind == TypeKind.Void));
			Assert.IsTrue(ctors.All(c => c.Accessibility == Accessibility.Public));
		}
		
		[Test]
		public void NoDefaultConstructorAddedToClass()
		{
			var ctors = compilation.FindType(typeof(MyClassWithCtor)).GetConstructors();
			Assert.AreEqual(Accessibility.Private, ctors.Single().Accessibility);
			Assert.AreEqual(1, ctors.Single().Parameters.Count);
		}
		
		[Test]
		public void DefaultConstructorOnAbstractClassIsProtected()
		{
			var ctors = compilation.FindType(typeof(AbstractClass)).GetConstructors();
			Assert.AreEqual(0, ctors.Single().Parameters.Count);
			Assert.AreEqual(Accessibility.Protected, ctors.Single().Accessibility);
		}
		
		[Test]
		public void SerializableAttribute()
		{
			IAttribute attr = GetTypeDefinition(typeof(NonCustomAttributes)).Attributes.Single();
			Assert.AreEqual("System.SerializableAttribute", attr.AttributeType.FullName);
		}
		
		[Test]
		public void NonSerializedAttribute()
		{
			IField field = GetTypeDefinition(typeof(NonCustomAttributes)).Fields.Single(f => f.Name == "NonSerializedField");
			Assert.AreEqual("System.NonSerializedAttribute", field.Attributes.Single().AttributeType.FullName);
		}
		
		[Test]
		public void ExplicitStructLayoutAttribute()
		{
			IAttribute attr = GetTypeDefinition(typeof(ExplicitFieldLayoutStruct)).Attributes.Single();
			Assert.AreEqual("System.Runtime.InteropServices.StructLayoutAttribute", attr.AttributeType.FullName);
			ResolveResult arg1 = attr.PositionalArguments.Single();
			Assert.AreEqual("System.Runtime.InteropServices.LayoutKind", arg1.Type.FullName);
			Assert.AreEqual((int)LayoutKind.Explicit, arg1.ConstantValue);
			
			var arg2 = attr.NamedArguments[0];
			Assert.AreEqual("CharSet", arg2.Key.Name);
			Assert.AreEqual("System.Runtime.InteropServices.CharSet", arg2.Value.Type.FullName);
			Assert.AreEqual((int)CharSet.Unicode, arg2.Value.ConstantValue);
			
			var arg3 = attr.NamedArguments[1];
			Assert.AreEqual("Pack", arg3.Key.Name);
			Assert.AreEqual("System.Int32", arg3.Value.Type.FullName);
			Assert.AreEqual(8, arg3.Value.ConstantValue);
		}
		
		[Test]
		public void FieldOffsetAttribute()
		{
			IField field = GetTypeDefinition(typeof(ExplicitFieldLayoutStruct)).Fields.Single(f => f.Name == "Field0");
			Assert.AreEqual("System.Runtime.InteropServices.FieldOffsetAttribute", field.Attributes.Single().AttributeType.FullName);
			ResolveResult arg = field.Attributes.Single().PositionalArguments.Single();
			Assert.AreEqual("System.Int32", arg.Type.FullName);
			Assert.AreEqual(0, arg.ConstantValue);
			
			field = GetTypeDefinition(typeof(ExplicitFieldLayoutStruct)).Fields.Single(f => f.Name == "Field100");
			Assert.AreEqual("System.Runtime.InteropServices.FieldOffsetAttribute", field.Attributes.Single().AttributeType.FullName);
			arg = field.Attributes.Single().PositionalArguments.Single();
			Assert.AreEqual("System.Int32", arg.Type.FullName);
			Assert.AreEqual(100, arg.ConstantValue);
		}
		
		[Test]
		public void DllImportAttribute()
		{
			IMethod method = GetTypeDefinition(typeof(NonCustomAttributes)).Methods.Single(m => m.Name == "DllMethod");
			IAttribute dllImport = method.Attributes.Single();
			Assert.AreEqual("System.Runtime.InteropServices.DllImportAttribute", dllImport.AttributeType.FullName);
			Assert.AreEqual("unmanaged.dll", dllImport.PositionalArguments[0].ConstantValue);
			Assert.AreEqual((int)CharSet.Unicode, dllImport.NamedArguments.Single().Value.ConstantValue);
		}
		
		[Test]
		public void InOutParametersOnRefMethod()
		{
			IParameter p = GetTypeDefinition(typeof(NonCustomAttributes)).Methods.Single(m => m.Name == "DllMethod").Parameters.Single();
			Assert.IsTrue(p.IsRef);
			Assert.IsFalse(p.IsOut);
			Assert.AreEqual(2, p.Attributes.Count);
			Assert.AreEqual("System.Runtime.InteropServices.InAttribute", p.Attributes[0].AttributeType.FullName);
			Assert.AreEqual("System.Runtime.InteropServices.OutAttribute", p.Attributes[1].AttributeType.FullName);
		}
		
		[Test]
		public void MarshalAsAttributeOnMethod()
		{
			IMethod method = GetTypeDefinition(typeof(NonCustomAttributes)).Methods.Single(m => m.Name == "DllMethod");
			IAttribute marshalAs = method.ReturnTypeAttributes.Single();
			Assert.AreEqual((int)UnmanagedType.Bool, marshalAs.PositionalArguments.Single().ConstantValue);
		}
		
		[Test]
		public void MethodWithOutParameter()
		{
			IParameter p = GetTypeDefinition(typeof(ParameterTests)).Methods.Single(m => m.Name == "MethodWithOutParameter").Parameters.Single();
			Assert.IsFalse(p.IsOptional);
			Assert.IsFalse(p.IsRef);
			Assert.IsTrue(p.IsOut);
			Assert.AreEqual(0, p.Attributes.Count);
			Assert.IsTrue(p.Type.Kind == TypeKind.ByReference);
		}
		
		[Test]
		public void MethodWithParamsArray()
		{
			IParameter p = GetTypeDefinition(typeof(ParameterTests)).Methods.Single(m => m.Name == "MethodWithParamsArray").Parameters.Single();
			Assert.IsFalse(p.IsOptional);
			Assert.IsFalse(p.IsRef);
			Assert.IsFalse(p.IsOut);
			Assert.IsTrue(p.IsParams);
			Assert.AreEqual(0, p.Attributes.Count);
			Assert.IsTrue(p.Type.Kind == TypeKind.Array);
		}
		
		[Test]
		public void MethodWithOptionalParameter()
		{
			IParameter p = GetTypeDefinition(typeof(ParameterTests)).Methods.Single(m => m.Name == "MethodWithOptionalParameter").Parameters.Single();
			Assert.IsTrue(p.IsOptional);
			Assert.IsFalse(p.IsRef);
			Assert.IsFalse(p.IsOut);
			Assert.IsFalse(p.IsParams);
			Assert.AreEqual(0, p.Attributes.Count);
			Assert.AreEqual(4, p.ConstantValue);
		}
		
		[Test]
		public void MethodWithEnumOptionalParameter()
		{
			IParameter p = GetTypeDefinition(typeof(ParameterTests)).Methods.Single(m => m.Name == "MethodWithEnumOptionalParameter").Parameters.Single();
			Assert.IsTrue(p.IsOptional);
			Assert.IsFalse(p.IsRef);
			Assert.IsFalse(p.IsOut);
			Assert.IsFalse(p.IsParams);
			Assert.AreEqual(0, p.Attributes.Count);
			Assert.AreEqual((int)StringComparison.OrdinalIgnoreCase, p.ConstantValue);
		}
		
		[Test]
		public void GenericDelegate_Variance()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(GenericDelegate<,>));
			Assert.AreEqual(VarianceModifier.Contravariant, type.TypeParameters[0].Variance);
			Assert.AreEqual(VarianceModifier.Covariant, type.TypeParameters[1].Variance);
			
			Assert.AreSame(type.TypeParameters[1], type.TypeParameters[0].DirectBaseTypes.FirstOrDefault());
		}
		
		[Test]
		public void GenericDelegate_ReferenceTypeConstraints()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(GenericDelegate<,>));
			Assert.IsFalse(type.TypeParameters[0].HasReferenceTypeConstraint);
			Assert.IsTrue(type.TypeParameters[1].HasReferenceTypeConstraint);
			
			Assert.IsNull(type.TypeParameters[0].IsReferenceType);
			Assert.AreEqual(true, type.TypeParameters[1].IsReferenceType);
		}
		
		[Test]
		public void GenericDelegate_GetInvokeMethod()
		{
			IType type = compilation.FindType(typeof(GenericDelegate<string, object>));
			IMethod m = type.GetDelegateInvokeMethod();
			Assert.AreEqual("Invoke", m.Name);
			Assert.AreEqual("System.Object", m.ReturnType.FullName);
			Assert.AreEqual("System.String", m.Parameters[0].Type.FullName);
		}
		
		[Test]
		public void ComInterfaceTest()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(IAssemblyEnum));
			// [ComImport]
			Assert.AreEqual(1, type.Attributes.Count(a => a.AttributeType.FullName == typeof(ComImportAttribute).FullName));
			
			IMethod m = type.Methods.Single();
			Assert.AreEqual("GetNextAssembly", m.Name);
			Assert.AreEqual(Accessibility.Public, m.Accessibility);
			Assert.IsTrue(m.IsAbstract);
			Assert.IsFalse(m.IsVirtual);
			Assert.IsFalse(m.IsSealed);
		}
		
		[Test]
		public void ConstantAnswer()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ConstantTest));
			IField answer = type.Fields.Single(f => f.Name == "Answer");
			Assert.IsTrue(answer.IsConst);
			Assert.IsTrue(answer.IsStatic);
			Assert.AreEqual(42, answer.ConstantValue);
		}
		
		[Test]
		public void ConstantEnumFromAnotherAssembly()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ConstantTest));
			IField answer = type.Fields.Single(f => f.Name == "EnumFromAnotherAssembly");
			Assert.IsTrue(answer.IsConst);
			Assert.AreEqual((int)StringComparison.OrdinalIgnoreCase, answer.ConstantValue);
		}
		
		[Test]
		public void ConstantNullString()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ConstantTest));
			IField answer = type.Fields.Single(f => f.Name == "NullString");
			Assert.IsTrue(answer.IsConst);
			Assert.IsNull(answer.ConstantValue);
		}
		
		[Test]
		public void InnerClassInGenericClassIsReferencedUsingParameterizedType()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(OuterGeneric<>));
			IField field1 = type.Fields.Single(f => f.Name == "Field1");
			IField field2 = type.Fields.Single(f => f.Name == "Field2");
			IField field3 = type.Fields.Single(f => f.Name == "Field3");
			
			// types must be self-parameterized
			Assert.AreEqual("ICSharpCode.NRefactory.TypeSystem.TestCase.OuterGeneric`1+Inner[[`0]]", field1.Type.ReflectionName);
			Assert.AreEqual("ICSharpCode.NRefactory.TypeSystem.TestCase.OuterGeneric`1+Inner[[`0]]", field2.Type.ReflectionName);
			Assert.AreEqual("ICSharpCode.NRefactory.TypeSystem.TestCase.OuterGeneric`1+Inner[[ICSharpCode.NRefactory.TypeSystem.TestCase.OuterGeneric`1+Inner[[`0]]]]", field3.Type.ReflectionName);
		}

		[Test]
		public void FlagsOnInterfaceMembersAreCorrect() {
			ITypeDefinition type = GetTypeDefinition(typeof(IInterfaceWithProperty));
			var p = type.Properties.Single();
			Assert.AreEqual(false, p.IsIndexer);
			Assert.AreEqual(true, p.IsAbstract);
			Assert.AreEqual(true, p.IsOverridable);
			Assert.AreEqual(false, p.IsOverride);
			Assert.AreEqual(true, p.IsPublic);
			Assert.AreEqual(true, p.Getter.IsAbstract);
			Assert.AreEqual(true, p.Getter.IsOverridable);
			Assert.AreEqual(false, p.Getter.IsOverride);
			Assert.AreEqual(true, p.Getter.IsPublic);
			Assert.AreEqual(false, p.Getter.HasBody);
			Assert.AreEqual(true, p.Setter.IsAbstract);
			Assert.AreEqual(true, p.Setter.IsOverridable);
			Assert.AreEqual(false, p.Setter.IsOverride);
			Assert.AreEqual(true, p.Setter.IsPublic);
			Assert.AreEqual(false, p.Setter.HasBody);

			type = GetTypeDefinition(typeof(IInterfaceWithIndexers));
			p = type.Properties.Single(x => x.Parameters.Count == 2);
			Assert.AreEqual(true, p.IsIndexer);
			Assert.AreEqual(true, p.IsAbstract);
			Assert.AreEqual(true, p.IsOverridable);
			Assert.AreEqual(false, p.IsOverride);
			Assert.AreEqual(true, p.IsPublic);
			Assert.AreEqual(true, p.Getter.IsAbstract);
			Assert.AreEqual(true, p.Getter.IsOverridable);
			Assert.AreEqual(false, p.Getter.IsOverride);
			Assert.AreEqual(true, p.Getter.IsPublic);
			Assert.AreEqual(true, p.Setter.IsAbstract);
			Assert.AreEqual(true, p.Setter.IsOverridable);
			Assert.AreEqual(false, p.Setter.IsOverride);
			Assert.AreEqual(true, p.Setter.IsPublic);

			type = GetTypeDefinition(typeof(IHasEvent));
			var e = type.Events.Single();
			Assert.AreEqual(true, e.IsAbstract);
			Assert.AreEqual(true, e.IsOverridable);
			Assert.AreEqual(false, e.IsOverride);
			Assert.AreEqual(true, e.IsPublic);
			Assert.AreEqual(true, e.AddAccessor.IsAbstract);
			Assert.AreEqual(true, e.AddAccessor.IsOverridable);
			Assert.AreEqual(false, e.AddAccessor.IsOverride);
			Assert.AreEqual(true, e.AddAccessor.IsPublic);
			Assert.AreEqual(true, e.RemoveAccessor.IsAbstract);
			Assert.AreEqual(true, e.RemoveAccessor.IsOverridable);
			Assert.AreEqual(false, e.RemoveAccessor.IsOverride);
			Assert.AreEqual(true, e.RemoveAccessor.IsPublic);

			type = GetTypeDefinition(typeof(IDisposable));
			var m = type.Methods.Single();
			Assert.AreEqual(true, m.IsAbstract);
			Assert.AreEqual(true, m.IsOverridable);
			Assert.AreEqual(false, m.IsOverride);
			Assert.AreEqual(true, m.IsPublic);
		}
		
		[Test]
		public void InnerClassInGenericClass_TypeParameterOwner()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(OuterGeneric<>.Inner));
			Assert.AreSame(type.DeclaringTypeDefinition.TypeParameters[0], type.TypeParameters[0]);
			Assert.AreSame(type.DeclaringTypeDefinition, type.TypeParameters[0].Owner);
		}
		
		[Test]
		public void InnerClassInGenericClass_ReferencesTheOuterClass_Field()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(OuterGeneric<>.Inner));
			IField f = type.Fields.Single();
			Assert.AreEqual("ICSharpCode.NRefactory.TypeSystem.TestCase.OuterGeneric`1[[`0]]", f.Type.ReflectionName);
		}
		
		[Test]
		public void InnerClassInGenericClass_ReferencesTheOuterClass_Parameter()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(OuterGeneric<>.Inner));
			IParameter p = type.Methods.Single(m => m.IsConstructor).Parameters.Single();
			Assert.AreEqual("ICSharpCode.NRefactory.TypeSystem.TestCase.OuterGeneric`1[[`0]]", p.Type.ReflectionName);
		}
		
		ResolveResult GetParamsAttributeArgument(int index)
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ParamsAttribute));
			var arr = (ArrayCreateResolveResult)type.Attributes.Single().PositionalArguments.Single();
			Assert.AreEqual(5, arr.InitializerElements.Count);
			return arr.InitializerElements[index];
		}
		
		ResolveResult Unbox(ResolveResult resolveResult)
		{
			ConversionResolveResult crr = (ConversionResolveResult)resolveResult;
			Assert.AreEqual(TypeKind.Class, crr.Type.Kind);
			Assert.AreEqual("System.Object", crr.Type.FullName);
			Assert.AreEqual(Conversion.BoxingConversion, crr.Conversion);
			return crr.Input;
		}
		
		[Test]
		public void ParamsAttribute_Integer()
		{
			ResolveResult rr = Unbox(GetParamsAttributeArgument(0));
			Assert.AreEqual("System.Int32", rr.Type.FullName);
			Assert.AreEqual(1, rr.ConstantValue);
		}
		
		[Test]
		public void ParamsAttribute_Enum()
		{
			ResolveResult rr = Unbox(GetParamsAttributeArgument(1));
			Assert.AreEqual("System.StringComparison", rr.Type.FullName);
			Assert.AreEqual((int)StringComparison.CurrentCulture, rr.ConstantValue);
		}
		
		[Test]
		public void ParamsAttribute_NullReference()
		{
			ResolveResult rr = GetParamsAttributeArgument(2);
			Assert.AreEqual("System.Object", rr.Type.FullName);
			Assert.IsTrue(rr.IsCompileTimeConstant);
			Assert.IsNull(rr.ConstantValue);
		}
		
		[Test]
		public void ParamsAttribute_Double()
		{
			ResolveResult rr = Unbox(GetParamsAttributeArgument(3));
			Assert.AreEqual("System.Double", rr.Type.FullName);
			Assert.AreEqual(4.0, rr.ConstantValue);
		}
		
		[Test]
		public void ParamsAttribute_String()
		{
			ConversionResolveResult rr = (ConversionResolveResult)GetParamsAttributeArgument(4);
			Assert.AreEqual("System.Object", rr.Type.FullName);
			Assert.AreEqual("System.String", rr.Input.Type.FullName);
			Assert.AreEqual("Test", rr.Input.ConstantValue);
		}
		
		[Test]
		public void ParamsAttribute_Property()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ParamsAttribute));
			IProperty prop = type.Properties.Single(p => p.Name == "Property");
			var attr = prop.Attributes.Single();
			Assert.AreEqual(type, attr.AttributeType);
			
			var normalArguments = ((ArrayCreateResolveResult)attr.PositionalArguments.Single()).InitializerElements;
			Assert.AreEqual(0, normalArguments.Count);
			
			var namedArg = attr.NamedArguments.Single();
			Assert.AreEqual(prop, namedArg.Key);
			var arrayElements = ((ArrayCreateResolveResult)namedArg.Value).InitializerElements;
			Assert.AreEqual(2, arrayElements.Count);
		}
		
		[Test]
		public void DoubleAttribute_ImplicitNumericConversion()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(DoubleAttribute));
			var arg = type.Attributes.Single().PositionalArguments.ElementAt(0);
			Assert.AreEqual("System.Double", arg.Type.ReflectionName);
			Assert.AreEqual(1.0, arg.ConstantValue);
		}
		
		[Test]
		public void ImplicitImplementationOfUnifiedMethods()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ImplementationOfUnifiedMethods));
			IMethod test = type.Methods.Single(m => m.Name == "Test");
			Assert.AreEqual(2, test.ImplementedInterfaceMembers.Count);
			Assert.AreEqual("Int32", ((IMethod)test.ImplementedInterfaceMembers[0]).Parameters.Single().Type.Name);
			Assert.AreEqual("Int32", ((IMethod)test.ImplementedInterfaceMembers[1]).Parameters.Single().Type.Name);
			Assert.AreEqual("T", ((IMethod)test.ImplementedInterfaceMembers[0].MemberDefinition).Parameters.Single().Type.Name);
			Assert.AreEqual("S", ((IMethod)test.ImplementedInterfaceMembers[1].MemberDefinition).Parameters.Single().Type.Name);
		}
		
		[Test]
		public void StaticityOfEventAccessors()
		{
			// https://github.com/icsharpcode/NRefactory/issues/20
			ITypeDefinition type = GetTypeDefinition(typeof(ClassWithStaticAndNonStaticMembers));
			var evt1 = type.Events.Single(e => e.Name == "Event1");
			Assert.IsTrue(evt1.IsStatic);
			Assert.IsTrue(evt1.AddAccessor.IsStatic);
			Assert.IsTrue(evt1.RemoveAccessor.IsStatic);

			var evt2 = type.Events.Single(e => e.Name == "Event2");
			Assert.IsFalse(evt2.IsStatic);
			Assert.IsFalse(evt2.AddAccessor.IsStatic);
			Assert.IsFalse(evt2.RemoveAccessor.IsStatic);

			var evt3 = type.Events.Single(e => e.Name == "Event3");
			Assert.IsTrue(evt3.IsStatic);
			Assert.IsTrue(evt3.AddAccessor.IsStatic);
			Assert.IsTrue(evt3.RemoveAccessor.IsStatic);

			var evt4 = type.Events.Single(e => e.Name == "Event4");
			Assert.IsFalse(evt4.IsStatic);
			Assert.IsFalse(evt4.AddAccessor.IsStatic);
			Assert.IsFalse(evt4.RemoveAccessor.IsStatic);
		}
		
		[Test]
		public void StaticityOfPropertyAccessors()
		{
			// https://github.com/icsharpcode/NRefactory/issues/20
			ITypeDefinition type = GetTypeDefinition(typeof(ClassWithStaticAndNonStaticMembers));
			var prop1 = type.Properties.Single(e => e.Name == "Prop1");
			Assert.IsTrue(prop1.IsStatic);
			Assert.IsTrue(prop1.Getter.IsStatic);
			Assert.IsTrue(prop1.Setter.IsStatic);

			var prop2 = type.Properties.Single(e => e.Name == "Prop2");
			Assert.IsFalse(prop2.IsStatic);
			Assert.IsFalse(prop2.Getter.IsStatic);
			Assert.IsFalse(prop2.Setter.IsStatic);

			var prop3 = type.Properties.Single(e => e.Name == "Prop3");
			Assert.IsTrue(prop3.IsStatic);
			Assert.IsTrue(prop3.Getter.IsStatic);
			Assert.IsTrue(prop3.Setter.IsStatic);

			var prop4 = type.Properties.Single(e => e.Name == "Prop4");
			Assert.IsFalse(prop4.IsStatic);
			Assert.IsFalse(prop4.Getter.IsStatic);
			Assert.IsFalse(prop4.Setter.IsStatic);
		}
		
		[Test]
		public void EventAccessorNames()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ClassWithStaticAndNonStaticMembers));
			var customEvent = type.Events.Single(e => e.Name == "Event1");
			Assert.AreEqual("add_Event1", customEvent.AddAccessor.Name);
			Assert.AreEqual("remove_Event1", customEvent.RemoveAccessor.Name);
			
			var normalEvent = type.Events.Single(e => e.Name == "Event3");
			Assert.AreEqual("add_Event3", normalEvent.AddAccessor.Name);
			Assert.AreEqual("remove_Event3", normalEvent.RemoveAccessor.Name);
		}

		[Test]
		public void InterfacePropertyAccessorsShouldNotBeOverrides() {
			ITypeDefinition type = GetTypeDefinition(typeof(IInterfaceWithProperty));
			var prop = type.Properties.Single(p => p.Name == "Prop");
			Assert.That(prop.Getter.IsOverride, Is.False);
			Assert.That(prop.Getter.IsOverridable, Is.True);
			Assert.That(prop.Setter.IsOverride, Is.False);
			Assert.That(prop.Setter.IsOverridable, Is.True);
		}

		[Test]
		public void VirtualPropertyAccessorsShouldNotBeOverrides() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassWithVirtualProperty));
			var prop = type.Properties.Single(p => p.Name == "Prop");
			Assert.That(prop.Getter.IsOverride, Is.False);
			Assert.That(prop.Getter.IsOverridable, Is.True);
			Assert.That(prop.Setter.IsOverride, Is.False);
			Assert.That(prop.Setter.IsOverridable, Is.True);
		}

		[Test]
		public void PropertyAccessorsShouldBeReportedAsImplementingInterfaceAccessors() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsProperty));
			var prop = type.Properties.Single(p => p.Name == "Prop");
			Assert.That(prop.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithProperty.Prop" }));
			Assert.That(prop.Getter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithProperty.get_Prop" }));
			Assert.That(prop.Setter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithProperty.set_Prop" }));
		}
		
		[Test]
		public void PropertyThatImplementsInterfaceIsNotVirtual()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsProperty));
			var prop = type.Properties.Single(p => p.Name == "Prop");
			Assert.IsFalse(prop.IsVirtual);
			Assert.IsFalse(prop.IsOverridable);
			Assert.IsFalse(prop.IsSealed);
		}

		[Test]
		public void Property_SealedOverride()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatOverridesAndSealsVirtualProperty));
			var prop = type.Properties.Single(p => p.Name == "Prop");
			Assert.IsFalse(prop.IsVirtual);
			Assert.IsTrue(prop.IsOverride);
			Assert.IsTrue(prop.IsSealed);
			Assert.IsFalse(prop.IsOverridable);
		}
		
		[Test]
		public void PropertyAccessorsShouldSupportToMemberReference()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsProperty));
			var prop = type.Properties.Single(p => p.Name == "Prop");
			var mr = prop.Getter.ToMemberReference();
			Assert.AreEqual(prop.Getter, mr.Resolve(compilation.TypeResolveContext));
		}
		
		[Test]
		public void IndexerAccessorsShouldBeReportedAsImplementingTheCorrectInterfaceAccessors() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsIndexers));
			var ix1 = type.Properties.Single(p => p.Parameters.Count == 1 && p.Parameters[0].Type.GetDefinition().KnownTypeCode == KnownTypeCode.Int32);
			var ix2 = type.Properties.Single(p => p.Parameters.Count == 1 && p.Parameters[0].Type.GetDefinition().KnownTypeCode == KnownTypeCode.String);
			var ix3 = type.Properties.Single(p => p.Parameters.Count == 2);

			Assert.That(ix1.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EquivalentTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.Item", "ICSharpCode.NRefactory.TypeSystem.TestCase.IGenericInterfaceWithIndexer`1.Item" }));
			Assert.That(ix1.ImplementedInterfaceMembers.All(p => ((IProperty)p).Parameters.Select(x => x.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.Int32 })));
			Assert.That(ix1.Getter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EquivalentTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.get_Item", "ICSharpCode.NRefactory.TypeSystem.TestCase.IGenericInterfaceWithIndexer`1.get_Item" }));
			Assert.That(ix1.Getter.ImplementedInterfaceMembers.All(m => ((IMethod)m).Parameters.Select(p => p.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.Int32 })));
			Assert.That(ix1.Setter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EquivalentTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.set_Item", "ICSharpCode.NRefactory.TypeSystem.TestCase.IGenericInterfaceWithIndexer`1.set_Item" }));
			Assert.That(ix1.Setter.ImplementedInterfaceMembers.All(m => ((IMethod)m).Parameters.Select(p => p.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.Int32, KnownTypeCode.Int32 })));

			Assert.That(ix2.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.Item" }));
			Assert.That(ix2.ImplementedInterfaceMembers.All(p => ((IProperty)p).Parameters.Select(x => x.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.String })));
			Assert.That(ix2.Getter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.get_Item" }));
			Assert.That(ix2.Getter.ImplementedInterfaceMembers.All(m => ((IMethod)m).Parameters.Select(p => p.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.String })));
			Assert.That(ix2.Setter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.set_Item" }));
			Assert.That(ix2.Setter.ImplementedInterfaceMembers.All(m => ((IMethod)m).Parameters.Select(p => p.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.String, KnownTypeCode.Int32 })));

			Assert.That(ix3.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.Item" }));
			Assert.That(ix3.ImplementedInterfaceMembers.All(p => ((IProperty)p).Parameters.Select(x => x.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.Int32, KnownTypeCode.Int32 })));
			Assert.That(ix3.Getter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.get_Item" }));
			Assert.That(ix3.Getter.ImplementedInterfaceMembers.All(m => ((IMethod)m).Parameters.Select(p => p.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.Int32, KnownTypeCode.Int32 })));
			Assert.That(ix3.Setter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithIndexers.set_Item" }));
			Assert.That(ix3.Setter.ImplementedInterfaceMembers.All(m => ((IMethod)m).Parameters.Select(p => p.Type.GetDefinition().KnownTypeCode).SequenceEqual(new[] { KnownTypeCode.Int32, KnownTypeCode.Int32, KnownTypeCode.Int32 })));
		}

		[Test]
		public void ExplicitIndexerImplementationReturnsTheCorrectMembers() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsIndexersExplicitly));

			Assert.That(type.Properties.All(p => p.EntityType == EntityType.Indexer));
			Assert.That(type.Properties.All(p => p.ImplementedInterfaceMembers.Count == 1));
			Assert.That(type.Properties.All(p => p.Getter.ImplementedInterfaceMembers.Count == 1));
			Assert.That(type.Properties.All(p => p.Setter.ImplementedInterfaceMembers.Count == 1));
		}

		[Test]
		public void ExplicitlyImplementedPropertyAccessorsShouldSupportToMemberReference()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsPropertyExplicitly));
			var prop = type.Properties.Single();
			var mr = prop.Getter.ToMemberReference();
			Assert.AreEqual(prop.Getter, mr.Resolve(compilation.TypeResolveContext));
		}
		
		[Test]
		public void ExplicitDisposableImplementation()
		{
			ITypeDefinition disposable = GetTypeDefinition(typeof(NRefactory.TypeSystem.TestCase.ExplicitDisposableImplementation));
			IMethod method = disposable.Methods.Single(m => !m.IsConstructor);
			Assert.IsTrue(method.IsExplicitInterfaceImplementation);
			Assert.AreEqual("System.IDisposable.Dispose", method.ImplementedInterfaceMembers.Single().FullName);
		}
		
		[Test]
		public void ExplicitImplementationOfUnifiedMethods()
		{
			IType type = compilation.FindType(typeof(ExplicitGenericInterfaceImplementationWithUnifiableMethods<int, int>));
			Assert.AreEqual(2, type.GetMethods(m => m.IsExplicitInterfaceImplementation).Count());
			foreach (IMethod method in type.GetMethods(m => m.IsExplicitInterfaceImplementation)) {
				Assert.AreEqual(1, method.ImplementedInterfaceMembers.Count, method.ToString());
				Assert.AreEqual("System.Int32", method.Parameters.Single().Type.ReflectionName);
				IMethod interfaceMethod = (IMethod)method.ImplementedInterfaceMembers.Single();
				Assert.AreEqual("System.Int32", interfaceMethod.Parameters.Single().Type.ReflectionName);
				var genericParamType = ((IMethod)method.MemberDefinition).Parameters.Single().Type;
				var interfaceGenericParamType = ((IMethod)interfaceMethod.MemberDefinition).Parameters.Single().Type;
				Assert.AreEqual(TypeKind.TypeParameter, genericParamType.Kind);
				Assert.AreEqual(TypeKind.TypeParameter, interfaceGenericParamType.Kind);
				Assert.AreEqual(genericParamType.ReflectionName, interfaceGenericParamType.ReflectionName);
			}
		}
		
		[Test]
		public void ExplicitImplementationOfUnifiedMethods_ToMemberReference()
		{
			IType type = compilation.FindType(typeof(ExplicitGenericInterfaceImplementationWithUnifiableMethods<int, int>));
			Assert.AreEqual(2, type.GetMethods(m => m.IsExplicitInterfaceImplementation).Count());
			foreach (IMethod method in type.GetMethods(m => m.IsExplicitInterfaceImplementation)) {
				IMethod resolvedMethod = (IMethod)method.ToMemberReference().Resolve(compilation.TypeResolveContext);
				Assert.AreEqual(method, resolvedMethod);
			}
		}

		[Test]
		public void ExplicitGenericInterfaceImplementation()
		{
			ITypeDefinition impl = GetTypeDefinition(typeof(ExplicitGenericInterfaceImplementation));
			IType genericInterfaceOfString = compilation.FindType(typeof(IGenericInterface<string>));
			IMethod implMethod1 = impl.Methods.Single(m => !m.IsConstructor && !m.Parameters[1].IsRef);
			IMethod implMethod2 = impl.Methods.Single(m => !m.IsConstructor && m.Parameters[1].IsRef);
			Assert.IsTrue(implMethod1.IsExplicitInterfaceImplementation);
			Assert.IsTrue(implMethod2.IsExplicitInterfaceImplementation);
			
			IMethod interfaceMethod1 = (IMethod)implMethod1.ImplementedInterfaceMembers.Single();
			Assert.AreEqual(genericInterfaceOfString, interfaceMethod1.DeclaringType);
			Assert.IsTrue(!interfaceMethod1.Parameters[1].IsRef);
			
			IMethod interfaceMethod2 = (IMethod)implMethod2.ImplementedInterfaceMembers.Single();
			Assert.AreEqual(genericInterfaceOfString, interfaceMethod2.DeclaringType);
			Assert.IsTrue(interfaceMethod2.Parameters[1].IsRef);
		}

		[Test]
		public void ExplicitlyImplementedPropertiesShouldBeReportedAsBeingImplemented() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsPropertyExplicitly));
			var prop = type.Properties.Single();
			Assert.That(prop.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithProperty.Prop" }));
			Assert.That(prop.Getter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithProperty.get_Prop" }));
			Assert.That(prop.Setter.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IInterfaceWithProperty.set_Prop" }));
		}
		
		[Test]
		public void ExplicitlyImplementedPropertiesShouldHaveExplicitlyImplementedAccessors() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsPropertyExplicitly));
			var prop = type.Properties.Single();
			Assert.IsTrue(prop.IsExplicitInterfaceImplementation);
			Assert.IsTrue(prop.Getter.IsExplicitInterfaceImplementation);
			Assert.IsTrue(prop.Setter.IsExplicitInterfaceImplementation);
		}

		[Test]
		public void EventAccessorsShouldBeReportedAsImplementingInterfaceAccessors() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsEvent));
			var evt = type.Events.Single(p => p.Name == "Event");
			Assert.That(evt.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.Event" }));
			Assert.That(evt.AddAccessor.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.add_Event" }));
			Assert.That(evt.RemoveAccessor.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.remove_Event" }));
		}

		[Test]
		public void EventAccessorsShouldBeReportedAsImplementingInterfaceAccessorsWhenCustomAccessorMethodsAreUsed() {
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsEventWithCustomAccessors));
			var evt = type.Events.Single(p => p.Name == "Event");
			Assert.That(evt.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.Event" }));
			Assert.That(evt.AddAccessor.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.add_Event" }));
			Assert.That(evt.RemoveAccessor.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.remove_Event" }));
		}

		[Test]
		public void ExplicitlyImplementedEventsShouldBeReportedAsBeingImplemented()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ClassThatImplementsEventExplicitly));
			var evt = type.Events.Single();
			Assert.That(evt.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.Event" }));
			Assert.That(evt.AddAccessor.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.add_Event" }));
			Assert.That(evt.RemoveAccessor.ImplementedInterfaceMembers.Select(p => p.ReflectionName).ToList(), Is.EqualTo(new[] { "ICSharpCode.NRefactory.TypeSystem.TestCase.IHasEvent.remove_Event" }));
		}

		[Test]
		public void MembersDeclaredInDerivedInterfacesDoNotImplementBaseMembers() {
			ITypeDefinition type = GetTypeDefinition(typeof(IShadowTestDerived));
			var method = type.Methods.Single(m => m.Name == "Method");
			var indexer = type.Properties.Single(p => p.IsIndexer);
			var prop = type.Properties.Single(p => p.Name == "Prop");
			var evt = type.Events.Single(e => e.Name == "Evt");

			Assert.That(method.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(indexer.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(indexer.Getter.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(indexer.Setter.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(prop.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(prop.Getter.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(prop.Setter.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(evt.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(evt.AddAccessor.ImplementedInterfaceMembers, Is.Empty);
			Assert.That(evt.RemoveAccessor.ImplementedInterfaceMembers, Is.Empty);
		}
		
		[Test]
		public void StaticClassTest()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(StaticClass));
			Assert.IsTrue(type.IsAbstract);
			Assert.IsTrue(type.IsSealed);
			Assert.IsTrue(type.IsStatic);
		}
		
		[Test]
		public void NoDefaultConstructorOnStaticClassTest()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(StaticClass));
			Assert.AreEqual(0, type.GetConstructors().Count());
		}
		
		[Test]
		[Ignore("not yet implemented in C# TypeSystemConvertVisitor")]
		public void IndexerNonDefaultName()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(IndexerNonDefaultName));
			var indexer = type.GetProperties(p => p.IsIndexer).Single();
			Assert.AreEqual("Foo", indexer.Name);
		}
	}
}
