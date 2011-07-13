// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Runtime.InteropServices;
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
		protected IProjectContent testCasePC;
		
		ITypeResolveContext ctx;
		
		[SetUpAttribute]
		public void SetUp()
		{
			ctx = CompositeTypeResolveContext.Combine(testCasePC, CecilLoaderTests.Mscorlib);
		}
		
		ITypeDefinition GetClass(Type type)
		{
			return testCasePC.GetTypeDefinition(type);
		}
		
		[Test]
		public void SimplePublicClassTest()
		{
			ITypeDefinition c = testCasePC.GetTypeDefinition(typeof(SimplePublicClass));
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
			ITypeDefinition c = testCasePC.GetTypeDefinition(typeof(SimplePublicClass));
			
			IMethod method = c.Methods.Single(m => m.Name == "Method");
			Assert.AreEqual(typeof(SimplePublicClass).FullName + ".Method", method.FullName);
			Assert.AreSame(c, method.DeclaringType);
			Assert.AreEqual(Accessibility.Public, method.Accessibility);
			Assert.AreEqual(EntityType.Method, method.EntityType);
			Assert.IsFalse(method.IsVirtual);
			Assert.IsFalse(method.IsStatic);
			Assert.IsTrue(method.IsFrozen);
			Assert.AreEqual(0, method.Parameters.Count);
			Assert.AreEqual(0, method.Attributes.Count);
		}
		
		[Test]
		public void DynamicType()
		{
			ITypeDefinition testClass = testCasePC.GetTypeDefinition(typeof(DynamicTest));
			Assert.AreSame(SharedTypes.Dynamic, testClass.Properties.Single().ReturnType.Resolve(ctx));
			Assert.AreEqual(0, testClass.Properties.Single().Attributes.Count);
		}
		
		[Test]
		public void DynamicTypeInGenerics()
		{
			ITypeDefinition testClass = testCasePC.GetTypeDefinition(typeof(DynamicTest));
			
			IMethod m1 = testClass.Methods.Single(me => me.Name == "DynamicGenerics1");
			Assert.AreEqual("System.Collections.Generic.List`1[[dynamic]]", m1.ReturnType.Resolve(ctx).ReflectionName);
			Assert.AreEqual("System.Action`3[[System.Object],[dynamic[]],[System.Object]]", m1.Parameters[0].Type.Resolve(ctx).ReflectionName);
			
			IMethod m2 = testClass.Methods.Single(me => me.Name == "DynamicGenerics2");
			Assert.AreEqual("System.Action`3[[System.Object],[dynamic],[System.Object]]", m2.Parameters[0].Type.Resolve(ctx).ReflectionName);
			
			IMethod m3 = testClass.Methods.Single(me => me.Name == "DynamicGenerics3");
			Assert.AreEqual("System.Action`3[[System.Int32],[dynamic],[System.Object]]", m3.Parameters[0].Type.Resolve(ctx).ReflectionName);
			
			IMethod m4 = testClass.Methods.Single(me => me.Name == "DynamicGenerics4");
			Assert.AreEqual("System.Action`3[[System.Int32[]],[dynamic],[System.Object]]", m4.Parameters[0].Type.Resolve(ctx).ReflectionName);
			
			IMethod m5 = testClass.Methods.Single(me => me.Name == "DynamicGenerics5");
			Assert.AreEqual("System.Action`3[[System.Int32*[]],[dynamic],[System.Object]]", m5.Parameters[0].Type.Resolve(ctx).ReflectionName);
			
			IMethod m6 = testClass.Methods.Single(me => me.Name == "DynamicGenerics6");
			Assert.AreEqual("System.Action`3[[System.Object],[dynamic],[System.Object]]&", m6.Parameters[0].Type.Resolve(ctx).ReflectionName);
			
			IMethod m7 = testClass.Methods.Single(me => me.Name == "DynamicGenerics7");
			Assert.AreEqual("System.Action`3[[System.Int32[][,]],[dynamic],[System.Object]]", m7.Parameters[0].Type.Resolve(ctx).ReflectionName);
		}
		
		[Test]
		public void DynamicParameterHasNoAttributes()
		{
			ITypeDefinition testClass = testCasePC.GetTypeDefinition(typeof(DynamicTest));
			IMethod m1 = testClass.Methods.Single(me => me.Name == "DynamicGenerics1");
			Assert.AreEqual(0, m1.Parameters[0].Attributes.Count);
		}
		
		[Test]
		public void AssemblyAttribute()
		{
			var attributes = testCasePC.AssemblyAttributes;
			var typeTest = attributes.First(a => a.AttributeType.Resolve(ctx).FullName == typeof(TypeTestAttribute).FullName);
			var posArgs = typeTest.GetPositionalArguments(ctx);
			Assert.AreEqual(3, posArgs.Count);
			// first argument is (int)42
			Assert.AreEqual(42, (int)posArgs[0].GetValue(ctx));
			// second argument is typeof(System.Action<>)
			IType rt = (IType)posArgs[1].GetValue(ctx);
			Assert.IsFalse(rt is ParameterizedType); // rt must not be constructed - it's just an unbound type
			Assert.AreEqual("System.Action", rt.FullName);
			Assert.AreEqual(1, rt.TypeParameterCount);
			// third argument is typeof(IDictionary<string, IList<TestAttribute>>)
			ParameterizedType crt = (ParameterizedType)posArgs[2].GetValue(ctx);
			Assert.AreEqual("System.Collections.Generic.IDictionary", crt.FullName);
			Assert.AreEqual("System.String", crt.TypeArguments[0].FullName);
			// ? for NUnit.TestAttribute (because that assembly isn't in ctx)
			Assert.AreEqual("System.Collections.Generic.IList`1[[?]]", crt.TypeArguments[1].ReflectionName);
		}
		
		[Test]
		public void TestClassTypeParameters()
		{
			var testClass = testCasePC.GetTypeDefinition(typeof(GenericClass<,>));
			Assert.AreEqual(EntityType.TypeDefinition, testClass.TypeParameters[0].OwnerType);
			Assert.AreEqual(EntityType.TypeDefinition, testClass.TypeParameters[1].OwnerType);
			Assert.AreSame(testClass.TypeParameters[1], testClass.TypeParameters[0].Constraints[0].Resolve(ctx));
		}
		
		[Test]
		public void TestMethod()
		{
			var testClass = testCasePC.GetTypeDefinition(typeof(GenericClass<,>));
			
			IMethod m = testClass.Methods.Single(me => me.Name == "TestMethod");
			Assert.AreEqual("K", m.TypeParameters[0].Name);
			Assert.AreEqual("V", m.TypeParameters[1].Name);
			Assert.AreEqual(EntityType.Method, m.TypeParameters[0].OwnerType);
			Assert.AreEqual(EntityType.Method, m.TypeParameters[1].OwnerType);
			
			Assert.AreEqual("System.IComparable`1[[``1]]", m.TypeParameters[0].Constraints[0].Resolve(ctx).ReflectionName);
			Assert.AreSame(m.TypeParameters[0], m.TypeParameters[1].Constraints[0].Resolve(ctx));
		}
		
		[Test]
		public void GetIndex()
		{
			var testClass = testCasePC.GetTypeDefinition(typeof(GenericClass<,>));
			
			IMethod m = testClass.Methods.Single(me => me.Name == "GetIndex");
			Assert.AreEqual("T", m.TypeParameters[0].Name);
			Assert.AreEqual(EntityType.Method, m.TypeParameters[0].OwnerType);
			
			ParameterizedType constraint = (ParameterizedType)m.TypeParameters[0].Constraints[0].Resolve(ctx);
			Assert.AreEqual("IEquatable", constraint.Name);
			Assert.AreEqual(1, constraint.TypeParameterCount);
			Assert.AreEqual(1, constraint.TypeArguments.Count);
			Assert.AreSame(m.TypeParameters[0], constraint.TypeArguments[0]);
		}
		
		[Test]
		public void PropertyWithProtectedSetter()
		{
			var testClass = testCasePC.GetTypeDefinition(typeof(PropertyTest));
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
			var testClass = testCasePC.GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.Name == "PropertyWithPrivateSetter");
			Assert.IsTrue(p.CanGet);
			Assert.IsTrue(p.CanSet);
			Assert.AreEqual(Accessibility.Public, p.Accessibility);
			Assert.AreEqual(Accessibility.Public, p.Getter.Accessibility);
			Assert.AreEqual(Accessibility.Private, p.Setter.Accessibility);
		}
		
		[Test]
		public void Indexer()
		{
			var testClass = testCasePC.GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.IsIndexer);
			Assert.IsTrue(p.CanGet);
			Assert.AreEqual(Accessibility.Public, p.Accessibility);
			Assert.AreEqual(Accessibility.Public, p.Getter.Accessibility);
			Assert.IsFalse(p.CanSet);
			Assert.IsNull(p.Setter);
		}
		
		[Test]
		public void EnumTest()
		{
			var e = testCasePC.GetTypeDefinition(typeof(MyEnum));
			Assert.AreEqual(ClassType.Enum, e.ClassType);
			Assert.AreEqual(false, e.IsReferenceType(ctx));
			Assert.AreEqual("System.Int16", e.BaseTypes[0].Resolve(ctx).ReflectionName);
			Assert.AreEqual(new[] { "System.Enum" }, e.GetBaseTypes(ctx).Select(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void EnumFieldsTest()
		{
			var e = testCasePC.GetTypeDefinition(typeof(MyEnum));
			Assert.AreEqual(5, e.Fields.Count);
			
			foreach (IField f in e.Fields) {
				Assert.IsTrue(f.IsStatic);
				Assert.IsTrue(f.IsConst);
				Assert.AreEqual(Accessibility.Public, f.Accessibility);
				Assert.AreSame(e, f.ConstantValue.GetValueType(ctx));
				Assert.AreEqual(typeof(short), f.ConstantValue.GetValue(ctx).GetType());
			}
			
			Assert.AreEqual("First", e.Fields[0].Name);
			Assert.AreEqual(0, e.Fields[0].ConstantValue.GetValue(ctx));
			
			Assert.AreEqual("Second", e.Fields[1].Name);
			Assert.AreSame(e, e.Fields[1].ConstantValue.GetValueType(ctx));
			Assert.AreEqual(1, e.Fields[1].ConstantValue.GetValue(ctx));
			
			Assert.AreEqual("Flag1", e.Fields[2].Name);
			Assert.AreEqual(0x10, e.Fields[2].ConstantValue.GetValue(ctx));

			Assert.AreEqual("Flag2", e.Fields[3].Name);
			Assert.AreEqual(0x20, e.Fields[3].ConstantValue.GetValue(ctx));
			
			Assert.AreEqual("CombinedFlags", e.Fields[4].Name);
			Assert.AreEqual(0x30, e.Fields[4].ConstantValue.GetValue(ctx));
		}
		
		[Test]
		public void ParameterizedTypeGetNestedTypesFromBaseClassTest()
		{
			var d = typeof(Derived<string, int>).ToTypeReference().Resolve(ctx);
			Assert.AreEqual(new[] { typeof(Base<>.Nested).FullName + "[[System.Int32]]" },
			                d.GetNestedTypes(ctx).Select(n => n.ReflectionName).ToArray());
		}
		
		[Test]
		public void DefaultConstructorAddedToStruct()
		{
			var ctors = typeof(MyStructWithCtor).ToTypeReference().Resolve(ctx).GetConstructors(ctx);
			Assert.AreEqual(2, ctors.Count());
			Assert.IsFalse(ctors.Any(c => c.IsStatic));
		}
		
		[Test]
		public void SerializableAttribute()
		{
			IAttribute attr = ctx.GetTypeDefinition(typeof(NonCustomAttributes)).Attributes.Single();
			Assert.AreEqual("System.SerializableAttribute", attr.AttributeType.Resolve(ctx).FullName);
		}
		
		[Test]
		public void NonSerializedAttribute()
		{
			IField field = ctx.GetTypeDefinition(typeof(NonCustomAttributes)).Fields.Single(f => f.Name == "NonSerializedField");
			Assert.AreEqual("System.NonSerializedAttribute", field.Attributes.Single().AttributeType.Resolve(ctx).FullName);
		}
		
		[Test]
		public void ExplicitStructLayoutAttribute()
		{
			IAttribute attr = ctx.GetTypeDefinition(typeof(ExplicitFieldLayoutStruct)).Attributes.Single();
			Assert.AreEqual("System.Runtime.InteropServices.StructLayoutAttribute", attr.AttributeType.Resolve(ctx).FullName);
			IConstantValue arg1 = attr.GetPositionalArguments(ctx).Single();
			Assert.AreEqual("System.Runtime.InteropServices.LayoutKind", arg1.GetValueType(ctx).FullName);
			Assert.AreEqual((int)LayoutKind.Explicit, arg1.GetValue(ctx));
			
			var namedArgs = attr.GetNamedArguments(ctx);
			var arg2 = namedArgs[0];
			Assert.AreEqual("CharSet", arg2.Key);
			Assert.AreEqual("System.Runtime.InteropServices.CharSet", arg2.Value.GetValueType(ctx).FullName);
			Assert.AreEqual((int)CharSet.Unicode, arg2.Value.GetValue(ctx));
			
			var arg3 = namedArgs[1];
			Assert.AreEqual("Pack", arg3.Key);
			Assert.AreEqual("System.Int32", arg3.Value.GetValueType(ctx).FullName);
			Assert.AreEqual(8, arg3.Value.GetValue(ctx));
		}
		
		[Test]
		public void FieldOffsetAttribute()
		{
			IField field = ctx.GetTypeDefinition(typeof(ExplicitFieldLayoutStruct)).Fields.Single(f => f.Name == "Field0");
			Assert.AreEqual("System.Runtime.InteropServices.FieldOffsetAttribute", field.Attributes.Single().AttributeType.Resolve(ctx).FullName);
			IConstantValue arg = field.Attributes.Single().GetPositionalArguments(ctx).Single();
			Assert.AreEqual("System.Int32", arg.GetValueType(ctx).FullName);
			Assert.AreEqual(0, arg.GetValue(ctx));
			
			field = ctx.GetTypeDefinition(typeof(ExplicitFieldLayoutStruct)).Fields.Single(f => f.Name == "Field100");
			Assert.AreEqual("System.Runtime.InteropServices.FieldOffsetAttribute", field.Attributes.Single().AttributeType.Resolve(ctx).FullName);
			arg = field.Attributes.Single().GetPositionalArguments(ctx).Single();
			Assert.AreEqual("System.Int32", arg.GetValueType(ctx).FullName);
			Assert.AreEqual(100, arg.GetValue(ctx));
		}
		
		[Test]
		public void DllImportAttribute()
		{
			IMethod method = ctx.GetTypeDefinition(typeof(NonCustomAttributes)).Methods.Single(m => m.Name == "DllMethod");
			IAttribute dllImport = method.Attributes.Single();
			Assert.AreEqual("System.Runtime.InteropServices.DllImportAttribute", dllImport.AttributeType.Resolve(ctx).FullName);
			Assert.AreEqual("unmanaged.dll", dllImport.GetPositionalArguments(ctx)[0].GetValue(ctx));
			Assert.AreEqual((int)CharSet.Unicode, dllImport.GetNamedArguments(ctx).Single().Value.GetValue(ctx));
		}
		
		[Test]
		public void InOutParametersOnRefMethod()
		{
			IParameter p = ctx.GetTypeDefinition(typeof(NonCustomAttributes)).Methods.Single(m => m.Name == "DllMethod").Parameters.Single();
			Assert.IsTrue(p.IsRef);
			Assert.IsFalse(p.IsOut);
			Assert.AreEqual(2, p.Attributes.Count);
			Assert.AreEqual("System.Runtime.InteropServices.InAttribute", p.Attributes[0].AttributeType.Resolve(ctx).FullName);
			Assert.AreEqual("System.Runtime.InteropServices.OutAttribute", p.Attributes[1].AttributeType.Resolve(ctx).FullName);
		}
		
		[Test]
		public void MarshalAsAttributeOnMethod()
		{
			IMethod method = ctx.GetTypeDefinition(typeof(NonCustomAttributes)).Methods.Single(m => m.Name == "DllMethod");
			IAttribute marshalAs = method.ReturnTypeAttributes.Single();
			Assert.AreEqual((int)UnmanagedType.Bool, marshalAs.GetPositionalArguments(ctx).Single().GetValue(ctx));
		}
		
		[Test]
		public void MethodWithOutParameter()
		{
			IParameter p = ctx.GetTypeDefinition(typeof(ParameterTests)).Methods.Single(m => m.Name == "MethodWithOutParameter").Parameters.Single();
			Assert.IsFalse(p.IsRef);
			Assert.IsTrue(p.IsOut);
			Assert.AreEqual(0, p.Attributes.Count);
			Assert.IsTrue(p.Type is ByReferenceTypeReference);
		}
		
		[Test]
		public void MethodWithParamsArray()
		{
			IParameter p = ctx.GetTypeDefinition(typeof(ParameterTests)).Methods.Single(m => m.Name == "MethodWithParamsArray").Parameters.Single();
			Assert.IsFalse(p.IsRef);
			Assert.IsFalse(p.IsOut);
			Assert.IsTrue(p.IsParams);
			Assert.AreEqual(0, p.Attributes.Count);
			Assert.IsTrue(p.Type is ArrayTypeReference);
		}
		
		[Test, Ignore("C# Parser does not set the variance")]
		public void GenericDelegate_Variance()
		{
			ITypeDefinition type = ctx.GetTypeDefinition(typeof(GenericDelegate<,>));
			Assert.AreEqual(VarianceModifier.Contravariant, type.TypeParameters[0].Variance);
			Assert.AreEqual(VarianceModifier.Covariant, type.TypeParameters[1].Variance);
			
			Assert.AreSame(type.TypeParameters[1], type.TypeParameters[0].Constraints[0]);
		}
		
		[Test]
		public void GenericDelegate_ReferenceTypeConstraints()
		{
			ITypeDefinition type = ctx.GetTypeDefinition(typeof(GenericDelegate<,>));
			Assert.IsFalse(type.TypeParameters[0].HasReferenceTypeConstraint);
			Assert.IsTrue(type.TypeParameters[1].HasReferenceTypeConstraint);
			
			Assert.IsTrue(type.TypeParameters[0].IsReferenceType(ctx) == true);
			Assert.IsTrue(type.TypeParameters[1].IsReferenceType(ctx) == true);
		}
		
		[Test]
		public void GenericDelegate_GetInvokeMethod()
		{
			IType type = typeof(GenericDelegate<string, object>).ToTypeReference().Resolve(ctx);
			IMethod m = type.GetDelegateInvokeMethod();
			Assert.AreEqual("Invoke", m.Name);
			Assert.AreEqual("System.Object", m.ReturnType.Resolve(ctx).FullName);
			Assert.AreEqual("System.String", m.Parameters[0].Type.Resolve(ctx).FullName);
		}
	}
}
