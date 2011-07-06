// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[assembly: ICSharpCode.NRefactory.TypeSystem.TestCase.TypeTestAttribute(
	42, typeof(System.Action<>), typeof(IDictionary<string, IList<NUnit.Framework.TestAttribute>>))]

namespace ICSharpCode.NRefactory.TypeSystem.TestCase
{
	public delegate S GenericDelegate<in T, out S>(T input) where T : S where S : class;
	
	public class SimplePublicClass
	{
		public void Method() {}
	}
	
	public class TypeTestAttribute : Attribute
	{
		public TypeTestAttribute(int a1, Type a2, Type a3) {}
	}
	
	public unsafe class DynamicTest
	{
		public dynamic SimpleProperty { get; set; }
		
		public List<dynamic> DynamicGenerics1(Action<object, dynamic[], object> param) { return null; }
		public void DynamicGenerics2(Action<object, dynamic, object> param) { }
		public void DynamicGenerics3(Action<int, dynamic, object> param) { }
		public void DynamicGenerics4(Action<int[], dynamic, object> param) { }
		public void DynamicGenerics5(Action<int*[], dynamic, object> param) { }
		public void DynamicGenerics6(ref Action<object, dynamic, object> param) { }
		public void DynamicGenerics7(Action<int[,][], dynamic, object> param) { }
	}
	
	public class GenericClass<A, B> where A : B
	{
		public void TestMethod<K, V>(string param) where V: K where K: IComparable<V> {}
		public void GetIndex<T>(T element) where T : IEquatable<T> {}
	}
	
	public class PropertyTest
	{
		public int PropertyWithProtectedSetter { get; protected set; }
		
		public object PropertyWithPrivateSetter { get; private set; }
		
		public string this[int index] { get { return "Test"; } }
	}
	
	public enum MyEnum : short
	{
		First,
		Second,
		Flag1 = 0x10,
		Flag2 = 0x20,
		CombinedFlags = Flag1 | Flag2
	}
	
	public class Base<T> {
		public class Nested {}
	}
	public class Derived<A, B> : Base<B> {}
	
	public struct MyStructWithCtor
	{
		public MyStructWithCtor(int a) {}
	}
	
	[Serializable]
	public class NonCustomAttributes
	{
		[NonSerialized]
		public readonly int NonSerializedField;
		
		[DllImport("unmanaged.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DllMethod([In, Out] ref int p);
	}
	
	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack = 8)]
	public struct ExplicitFieldLayoutStruct
	{
		[FieldOffset(0)]
		public int Field0;
		
		[FieldOffset(100)]
		public int Field100;
	}
	
	public class ParameterTests
	{
		public void MethodWithOutParameter(out int x) { x = 0; }
		public void MethodWithParamsArray(params object[] x) {}
	}
}
