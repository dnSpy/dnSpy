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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ICSharpCode.NRefactory.TypeSystem.TestCase.TypeTestAttribute(
	42, typeof(System.Action<>), typeof(IDictionary<string, IList<NUnit.Framework.TestAttribute>>))]

[assembly: TypeForwardedTo(typeof(Func<,>))]

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
	
	[Params(1, StringComparison.CurrentCulture, null, 4.0, "Test")]
	public class ParamsAttribute : Attribute
	{
		public ParamsAttribute(params object[] x) {}
		
		[Params(Property = new string[] { "a", "b" })]
		public string[] Property { get; set; }
	}
	
	[Double(1)]
	public class DoubleAttribute : Attribute
	{
		public DoubleAttribute(double val) {}
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
		
		public NestedEnum EnumField;
		
		public A Property { get; set; }
		
		public enum NestedEnum {
			EnumMember
		}
	}
	
	public class PropertyTest
	{
		public int PropertyWithProtectedSetter { get; protected set; }
		
		public object PropertyWithPrivateSetter { get; private set; }
		
		public object PropertyWithoutSetter { get { return null; } }
		
		public string this[int index] { get { return "Test"; } set {} }
	}
	
	public enum MyEnum : short
	{
		First,
		Second,
		Flag1 = 0x10,
		Flag2 = 0x20,
		CombinedFlags = Flag1 | Flag2
	}
	
	public class Base<T>
	{
		public class Nested<X> {}
		
		public virtual void GenericMethodWithConstraints<X>(T a) where X : IComparer<T>, new() {}
	}
	public class Derived<A, B> : Base<B>
	{
		public override void GenericMethodWithConstraints<Y>(B a) { }
	}
	
	public struct MyStructWithCtor
	{
		public MyStructWithCtor(int a) {}
	}
	
	public class MyClassWithCtor
	{
		private MyClassWithCtor(int a) {}
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
		public void MethodWithOptionalParameter(int x = 4) {}
		public void MethodWithEnumOptionalParameter(StringComparison x = StringComparison.OrdinalIgnoreCase) {}
	}
	
	[ComImport(), Guid("21B8916C-F28E-11D2-A473-00C04F8EF448"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAssemblyEnum
	{
		[PreserveSig()]
		int GetNextAssembly(uint dwFlags);
	}
	
	public class ConstantTest
	{
		public const int Answer = 42;
		
		public const StringComparison EnumFromAnotherAssembly = StringComparison.OrdinalIgnoreCase;
		
		public const string NullString = null;
	}
	
	public class OuterGeneric<X>
	{
		public class Inner {
			public OuterGeneric<X> referenceToOuter;
			public Inner(OuterGeneric<X> referenceToOuter) {}
		}
		
		public OuterGeneric<X>.Inner Field1;
		public Inner Field2;
		public OuterGeneric<OuterGeneric<X>.Inner>.Inner Field3;
	}
	
	public class ExplicitDisposableImplementation : IDisposable
	{
		void IDisposable.Dispose() {}
	}
	
	public interface IGenericInterface<T>
	{
		void Test<S>(T a, S b) where S : T;
		void Test<S>(T a, ref S b);
	}
	
	public class ExplicitGenericInterfaceImplementation : IGenericInterface<string>
	{
		void IGenericInterface<string>.Test<T>(string a, T b) {}
		void IGenericInterface<string>.Test<T>(string a, ref T b) {}
	}
	
	public interface IGenericInterfaceWithUnifiableMethods<T, S>
	{
		void Test(T a);
		void Test(S a);
	}
	
	public class ImplementationOfUnifiedMethods : IGenericInterfaceWithUnifiableMethods<int, int>
	{
		public void Test(int a) {}
	}
	
	public class ExplicitGenericInterfaceImplementationWithUnifiableMethods<T, S> : IGenericInterfaceWithUnifiableMethods<T, S>
	{
		void IGenericInterfaceWithUnifiableMethods<T, S>.Test(T a) {}
		void IGenericInterfaceWithUnifiableMethods<T, S>.Test(S a) {}
	}
	
	public partial class PartialClass
	{
		partial void PartialMethodWithImplementation(int a);
		
		partial void PartialMethodWithImplementation(System.Int32 a)
		{
		}
		
		partial void PartialMethodWithImplementation(string a);
		
		partial void PartialMethodWithImplementation(System.String a)
		{
		}
		
		partial void PartialMethodWithoutImplementation();
	}
	
	public class ClassWithStaticAndNonStaticMembers
	{
		public static event System.EventHandler Event1 { add {} remove{} }
		public event System.EventHandler Event2 { add {} remove{} }
		#pragma warning disable 67
		public static event System.EventHandler Event3;
		public event System.EventHandler Event4;

		public static int Prop1 { get { return 0; } set {} }
		public int Prop2 { get { return 0; } set {} }
		public static int Prop3 { get; set; }
		public int Prop4 { get; set; }
	}

	public interface IInterfaceWithProperty {
		int Prop { get; set; }
	}

	public class ClassWithVirtualProperty {
		public virtual int Prop { get; set; }
	}
	
	public class ClassThatOverridesAndSealsVirtualProperty : ClassWithVirtualProperty {
		public sealed override int Prop { get; set; }
	}

	public class ClassThatImplementsProperty : IInterfaceWithProperty {
		public int Prop { get; set; }
	}

	public class ClassThatImplementsPropertyExplicitly : IInterfaceWithProperty {
		int IInterfaceWithProperty.Prop { get; set; }
	}

	public interface IInterfaceWithIndexers {
		int this[int x] { get; set; }
		int this[string x] { get; set; }
		int this[int x, int y] { get; set; }
	}

	public interface IGenericInterfaceWithIndexer<T> {
		int this[T x] { get; set; }
	}

	public class ClassThatImplementsIndexers : IInterfaceWithIndexers, IGenericInterfaceWithIndexer<int> {
		public int this[int x] { get { return 0; } set {} }
		public int this[string x] { get { return 0; } set {} }
		public int this[int x, int y] { get { return 0; } set {} }
	}

	public class ClassThatImplementsIndexersExplicitly : IInterfaceWithIndexers, IGenericInterfaceWithIndexer<int> {
		int IInterfaceWithIndexers.this[int x] { get { return 0; } set {} }
		int IGenericInterfaceWithIndexer<int>.this[int x] { get { return 0; } set {} }
		int IInterfaceWithIndexers.this[string x] { get { return 0; } set {} }
		int IInterfaceWithIndexers.this[int x, int y] { get { return 0; } set {} }
	}

	public interface IHasEvent {
		event EventHandler Event;
	}

	public class ClassThatImplementsEvent : IHasEvent {
		public event EventHandler Event;
	}

	public class ClassThatImplementsEventWithCustomAccessors : IHasEvent {
		public event EventHandler Event { add {} remove {} }
	}

	public class ClassThatImplementsEventExplicitly : IHasEvent {
		event EventHandler IHasEvent.Event { add {} remove {} }
	}

	public interface IShadowTestBase {
		void Method();
		int this[int i] { get; set; }
		int Prop { get; set; }
		event EventHandler Evt;
	}

	public interface IShadowTestDerived : IShadowTestBase {
		new void Method();
		new int this[int i] { get; set; }
		new int Prop { get; set; }
		new event EventHandler Evt;
	}
	
	public static class StaticClass {}
	public abstract class AbstractClass {}
	
	public class IndexerNonDefaultName {
		[IndexerName("Foo")]
		public int this[int index] {
			get { return 0; }
		}
	}
}
