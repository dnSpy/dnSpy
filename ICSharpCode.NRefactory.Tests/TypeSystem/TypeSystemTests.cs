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
		
		ITypeDefinition GetTypeDefinition(Type type)
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
			
			ParameterizedType constraint = (ParameterizedType)m.TypeParameters[0].DirectBaseTypes.First();
			Assert.AreEqual("IEquatable", constraint.Name);
			Assert.AreEqual(1, constraint.TypeParameterCount);
			Assert.AreEqual(1, constraint.TypeArguments.Count);
			Assert.AreSame(m.TypeParameters[0], constraint.TypeArguments[0]);
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
		}
		
		[Test]
		public void Indexer()
		{
			var testClass = GetTypeDefinition(typeof(PropertyTest));
			IProperty p = testClass.Properties.Single(pr => pr.IsIndexer);
			Assert.AreEqual("Item", p.Name);
			Assert.IsTrue(p.CanGet);
			Assert.AreEqual(Accessibility.Public, p.Accessibility);
			Assert.AreEqual(Accessibility.Public, p.Getter.Accessibility);
			Assert.IsFalse(p.CanSet);
			Assert.IsNull(p.Setter);
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
		}
		
		[Test]
		public void ConstantAnswer()
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ConstantTest));
			IField answer = type.Fields.Single(f => f.Name == "Answer");
			Assert.IsTrue(answer.IsConst);
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
		
		ResolveResult GetParamsAttributeArgument(int index)
		{
			ITypeDefinition type = GetTypeDefinition(typeof(ParamsAttribute)).GetDefinition();
			var arr = (ArrayCreateResolveResult)type.Attributes.Single().PositionalArguments.Single();
			Assert.AreEqual(5, arr.InitializerElements.Length);
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
		
		[Test, Ignore("CecilLoader does not create ConversionResolveResult")]
		public void ParamsAttribute_Integer()
		{
			ResolveResult rr = Unbox(GetParamsAttributeArgument(0));
			Assert.AreEqual("System.Int32", rr.Type.FullName);
			Assert.AreEqual(1, rr.ConstantValue);
		}
		
		[Test, Ignore("CecilLoader does not create ConversionResolveResult")]
		public void ParamsAttribute_Enum()
		{
			ResolveResult rr = Unbox(GetParamsAttributeArgument(1));
			Assert.AreEqual("System.StringComparison", rr.Type.FullName);
			Assert.AreEqual((int)StringComparison.CurrentCulture, rr.ConstantValue);
		}
		
		[Test, Ignore("CecilLoader does not create ConversionResolveResult")]
		public void ParamsAttribute_NullReference()
		{
			ResolveResult rr = GetParamsAttributeArgument(2);
			Assert.AreEqual("System.Object", rr.Type.FullName);
			Assert.IsTrue(rr.IsCompileTimeConstant);
			Assert.IsNull(rr.ConstantValue);
		}
		
		[Test, Ignore("CecilLoader does not create ConversionResolveResult")]
		public void ParamsAttribute_Double()
		{
			ResolveResult rr = Unbox(GetParamsAttributeArgument(3));
			Assert.AreEqual("System.Double", rr.Type.FullName);
			Assert.AreEqual(4.0, rr.ConstantValue);
		}
		
		[Test, Ignore("CecilLoader does not create ConversionResolveResult")]
		public void ParamsAttribute_String()
		{
			ConversionResolveResult rr = (ConversionResolveResult)GetParamsAttributeArgument(4);
			Assert.AreEqual("System.Object", rr.Type.FullName);
			Assert.AreEqual("System.String", rr.Input.Type.FullName);
			Assert.AreEqual("Test", rr.Input.ConstantValue);
		}
	}
}
