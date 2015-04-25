// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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

//$CS
using System;
//$CE

//$$ IndexerWithGetOnly
namespace IndexerWithGetOnly
{
	public class MyClass
	{
		public int this[int i]
		{
			get
			{
				return i;
			}
		}
	}
}
//$$ IndexerWithSetOnly
namespace IndexerWithSetOnly
{
	public class MyClass
	{
		public int this[int i]
		{
			set
			{
			}
		}
	}
}
//$$ IndexerWithMoreParameters
namespace IndexerWithMoreParameters
{
	public class MyClass
	{
		public int this[int i, string s, Type t]
		{
			get
			{
				return 0;
			}
		}
	}
}
//$$ IndexerInGenericClass
namespace IndexerInGenericClass
{
	public class MyClass<T>
	{
		public int this[T t]
		{
			get
			{
				return 0;
			}
		}
	}
}
//$$ OverloadedIndexer
namespace OverloadedIndexer
{
	public class MyClass
	{
		public int this[int t]
		{
			get
			{
				return 0;
			}
		}
		public int this[string s]
		{
			get
			{
				return 0;
			}
			set
			{
				Console.WriteLine(value + " " + s);
			}
		}
	}
}
//$$ IndexerInInterface
namespace IndexerInInterface
{
	public interface IInterface
	{
		int this[string s, string s2]
		{
			set;
		}
	}
}
//$$ IndexerInterfaceExplicitImplementation
namespace IndexerInterfaceExplicitImplementation
{
	public interface IMyInterface
	{
		int this[string s]
		{
			get;
		}
	}
	public class MyClass : IMyInterface
	{
		int IMyInterface.this[string s]
		{
			get
			{
				return 3;
			}
		}
	}
}
//$$ IndexerInterfaceImplementation
namespace IndexerInterfaceImplementation
{
	public interface IMyInterface
	{
		int this[string s]
		{
			get;
		}
	}
	public class MyClass : IMyInterface
	{
		public int this[string s]
		{
			get
			{
				return 3;
			}
		}
	}
}
//$$ IndexerAbstract
namespace IndexerAbstract
{
	public abstract class MyClass
	{
		public abstract int this[string s, string s2]
		{
			set;
		}
		protected abstract string this[int index]
		{
			get;
		}
	}
}
//$$ MethodExplicit
namespace MethodExplicit
{
	public interface IMyInterface
	{
		void MyMethod();
	}
	public class MyClass : IMyInterface
	{
		void IMyInterface.MyMethod()
		{
		}
	}
}
//$$ MethodFromInterfaceVirtual
namespace MethodFromInterfaceVirtual
{
	public interface IMyInterface
	{
		void MyMethod();
	}
	public class MyClass : IMyInterface
	{
		public virtual void MyMethod()
		{
		}
	}
}
//$$ MethodFromInterface
namespace MethodFromInterface
{
	public interface IMyInterface
	{
		void MyMethod();
	}
	public class MyClass : IMyInterface
	{
		public void MyMethod()
		{
		}
	}
}
//$$ MethodFromInterfaceAbstract
namespace MethodFromInterfaceAbstract
{
	public interface IMyInterface
	{
		void MyMethod();
	}
	public abstract class MyClass : IMyInterface
	{
		public abstract void MyMethod();
	}
}
//$$ PropertyInterface
namespace PropertyInterface
{
	public interface IMyInterface
	{
		int MyProperty
		{
			get;
			set;
		}
	}
}
//$$ PropertyInterfaceExplicitImplementation
namespace PropertyInterfaceExplicitImplementation
{
	public interface IMyInterface
	{
		int MyProperty
		{
			get;
			set;
		}
	}
	public class MyClass : IMyInterface
	{
		int IMyInterface.MyProperty
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
}
//$$ PropertyInterfaceImplementation
namespace PropertyInterfaceImplementation
{
	public interface IMyInterface
	{
		int MyProperty
		{
			get;
			set;
		}
	}
	public class MyClass : IMyInterface
	{
		public int MyProperty
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
}
//$$ PropertyPrivateGetPublicSet
namespace PropertyPrivateGetPublicSet
{
	public class MyClass
	{
		public int MyProperty
		{
			private get
			{
				return 3;
			}
			set
			{
			}
		}
	}
}
//$$ PropertyPublicGetProtectedSet
namespace PropertyPublicGetProtectedSet
{
	public class MyClass
	{
		public int MyProperty
		{
			get
			{
				return 3;
			}
			protected set
			{
			}
		}
	}
}
//$$ PropertyOverrideDefaultAccessorOnly
namespace PropertyOverrideDefaultAccessorOnly
{
	public class MyClass
	{
		public virtual int MyProperty
		{
			get
			{
				return 3;
			}
			protected set
			{
			}
		}
	}
	public class Derived : MyClass
	{
		public override int MyProperty
		{
			get
			{
				return 4;
			}
		}
	}
}
//$$ PropertyOverrideRestrictedAccessorOnly
namespace PropertyOverrideRestrictedAccessorOnly
{
	public class MyClass
	{
		public virtual int MyProperty
		{
			get
			{
				return 3;
			}
			protected set
			{
			}
		}
	}
	public class Derived : MyClass
	{
		public override int MyProperty
		{
			protected set
			{
			}
		}
	}
}
//$$ PropertyOverrideOneAccessor
namespace PropertyOverrideOneAccessor
{
	public class MyClass
	{
		protected internal virtual int MyProperty
		{
			get
			{
				return 3;
			}
			protected set
			{
			}
		}
	}
	public class DerivedNew : MyClass
	{
		public new virtual int MyProperty
		{
			set
			{
			}
		}
	}
	public class DerivedOverride : DerivedNew
	{
		public override int MyProperty
		{
			set
			{
			}
		}
	}
}
//$$ IndexerOverrideRestrictedAccessorOnly
namespace IndexerOverrideRestrictedAccessorOnly
{
	public class MyClass
	{
		public virtual int this[string s]
		{
			get
			{
				return 3;
			}
			protected set
			{
			}
		}
		protected internal virtual int this[int i]
		{
			protected get
			{
				return 2;
			}
			set
			{
			}
		}
	}
	public class Derived : MyClass
	{
		protected internal override int this[int i]
		{
			protected get
			{
				return 4;
			}
		}
	}
}
//$$ HideProperty
namespace HideProperty
{
	public class A
	{
		public virtual int P
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
	public class B : A
	{
		private new int P
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
	public class C : B
	{
		public override int P
		{
			set
			{
			}
		}
	}
}
//$$ HideMembers
namespace HideMembers
{
	public class A
	{
		public int F;
		public int Prop
		{
			get
			{
				return 3;
			}
		}
		public int G
		{
			get
			{
				return 3;
			}
		}
	}
	public class B : A
	{
		public new int F
		{
			get
			{
				return 3;
			}
		}
		public new string Prop
		{
			get
			{
				return "a";
			}
		}
	}
	public class C : A
	{
		public new int G;
	}
	public class D : A
	{
		public new void F()
		{
		}
	}
	public class D1 : D
	{
		public new int F;
	}
	public class E : A
	{
		private new class F
		{
		}
	}
}
//$$ HideMembers2
namespace HideMembers2
{
	public class G
	{
		public int Item
		{
			get
			{
				return 1;
			}
		}
	}
	public class G2 : G
	{
		public int this[int i]
		{
			get
			{
				return 2;
			}
		}
	}
	public class G3 : G2
	{
		public new int Item
		{
			get
			{
				return 4;
			}
		}
	}
	public class H
	{
		public int this[int j]
		{
			get
			{
				return 0;
			}
		}
	}
	public class H2 : H
	{
		public int Item
		{
			get
			{
				return 2;
			}
		}
	}
	public class H3 : H2
	{
		public new string this[int j]
		{
			get
			{
				return null;
			}
		}
	}
}
//$$ HideMembers2a
namespace HideMembers2a
{
	public interface IA
	{
		int this[int i]
		{
			get;
		}
	}
	public class A : IA
	{
		int IA.this[int i]
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
	public class A1 : A
	{
		public int this[int i]
		{
			get
			{
				return 3;
			}
		}
	}
}
//$$ HideMembers3
namespace HideMembers3
{
	public class G<T>
	{
		public void M1(T p)
		{
		}
		public int M2(int t)
		{
			return 3;
		}
	}
	public class G1<T> : G<int>
	{
		public new int M1(int i)
		{
			return 0;
		}
		public int M2(T i)
		{
			return 2;
		}
	}
	public class G2<T> : G<int>
	{
		public int M1(T p)
		{
			return 4;
		}
	}
	public class J
	{
		public int P
		{
			get
			{
				return 2;
			}
		}
	}
	public class J2 : J
	{
#pragma warning disable 0108	// Deliberate bad code for test case
		public int get_P;
#pragma warning restore 0108
	}
}
//$$ HideMembers4
namespace HideMembers4
{
	public class A
	{
		public void M<T>(T t)
		{
		}
	}
	public class A1 : A
	{
		public new void M<K>(K t)
		{
		}
		public void M(int t)
		{
		}
	}
	public class B
	{
		public void M<T>()
		{
		}
		public void M1<T>()
		{
		}
		public void M2<T>(T t)
		{
		}
	}
	public class B1 : B
	{
		public void M<T1, T2>()
		{
		}
		public new void M1<R>()
		{
		}
		public new void M2<R>(R r)
		{
		}
	}
	public class C<T>
	{
		public void M<TT>(T t)
		{
		}
	}
	public class C1<K> : C<K>
	{
		public void M<TT>(TT t)
		{
		}
	}
}
//$$ HideMembers5
namespace HideMembers5
{
	public class A
	{
		public void M(int t)
		{
		}
	}
	public class A1 : A
	{
		public void M(ref int t)
		{
		}
	}
	public class B
	{
		public void M(ref int l)
		{
		}
	}
	public class B1 : B
	{
		public void M(out int l)
		{
			l = 2;
		}
		public void M(ref long l)
		{
		}
	}
}
//$$ HideMemberSkipNotVisible
namespace HideMemberSkipNotVisible
{
	public class A
	{
		protected int F;
		protected string P
		{
			get
			{
				return null;
			}
		}
	}
	public class B : A
	{
		private new string F;
		private new int P
		{
			set
			{
			}
		}
	}
}
//$$ HideNestedClass
namespace HideNestedClass
{
	public class A
	{
		public class N1
		{
		}
		protected class N2
		{
		}
		private class N3
		{
		}
		internal class N4
		{
		}
		protected internal class N5
		{
		}
	}
	public class B : A
	{
		public new int N1;
		public new int N2;
		public int N3;
		public new int N4;
		public new int N5;
	}
}
//$$ HidePropertyReservedMethod
namespace HidePropertyReservedMethod
{
	public class A
	{
		public int P
		{
			get
			{
				return 1;
			}
		}
	}
	public class B : A
	{
		public int get_P()
		{
			return 2;
		}
		public void set_P(int value)
		{
		}
	}
}
//$$ HideIndexerDiffAccessor
namespace HideIndexerDiffAccessor
{
	public class A
	{
		public int this[int i]
		{
			get
			{
				return 2;
			}
		}
	}
	public class B : A
	{
		public new int this[int j]
		{
			set
			{
			}
		}
	}
}
//$$ HideIndexerGeneric
namespace HideIndexerGeneric
{
	public class A<T>
	{
		public virtual int this[T r]
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
	public class B : A<int>
	{
		private new int this[int k]
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
	public class C<T> : A<T>
	{
		public override int this[T s]
		{
			set
			{
			}
		}
	}
	public class D<T> : C<T>
	{
		public new virtual int this[T s]
		{
			set
			{
			}
		}
	}
}
//$$ HideMethod
namespace HideMethod
{
	public class A
	{
		public virtual void F()
		{
		}
	}
	public class B : A
	{
		private new void F()
		{
			base.F();
		}
	}
	public class C : B
	{
		public override void F()
		{
			base.F();
		}
	}
}
//$$ HideMethodGeneric
namespace HideMethodGeneric
{
	public class A<T>
	{
		public virtual void F(T s)
		{
		}
		public new static bool Equals(object o1, object o2)
		{
			return true;
		}
	}
	public class B : A<string>
	{
		private new void F(string k)
		{
		}
		public void F(int i)
		{
		}
	}
	public class C<T> : A<T>
	{
		public override void F(T r)
		{
		}
		public void G(T t)
		{
		}
	}
	public class D<T1> : C<T1>
	{
		public new virtual void F(T1 k)
		{
		}
		public virtual void F<T2>(T2 k)
		{
		}
		public virtual void G<T2>(T2 t)
		{
		}
	}
}
//$$ HideMethodGenericSkipPrivate
namespace HideMethodGenericSkipPrivate
{
	public class A<T>
	{
		public virtual void F(T t)
		{
		}
	}
	public class B<T> : A<T>
	{
		private new void F(T t)
		{
		}
		private void K()
		{
		}
	}
	public class C<T> : B<T>
	{
		public override void F(T tt)
		{
		}
		public void K()
		{
		}
	}
	public class D : B<int>
	{
		public override void F(int t)
		{
		}
	}
}
//$$ HideMethodGeneric2
namespace HideMethodGeneric2
{
	public class A
	{
		public virtual void F(int i)
		{
		}
		public void K()
		{
		}
	}
	public class B<T> : A
	{
		protected virtual void F(T t)
		{
		}
		public void K<T2>()
		{
		}
	}
	public class C : B<int>
	{
		protected override void F(int k)
		{
		}
		public new void K<T3>()
		{
		}
	}
	public class D : B<string>
	{
		public override void F(int k)
		{
		}
		public void L<T4>()
		{
		}
	}
	public class E<T>
	{
		public void M<T2>(T t, T2 t2)
		{
		}
	}
	public class F<T> : E<T>
	{
		public void M(T t1, T t2)
		{
		}
	}
}
//$$ HideMethodDiffSignatures
namespace HideMethodDiffSignatures
{
	public class C1<T>
	{
		public virtual void M(T arg)
		{
		}
	}
	public class C2<T1, T2> : C1<T2>
	{
		public new virtual void M(T2 arg)
		{
		}
	}
	public class C3 : C2<int, bool>
	{
		public new virtual void M(bool arg)
		{
		}
	}
}
//$$ HideMethodStatic
namespace HideMethodStatic
{
	public class A
	{
		public int N
		{
			get
			{
				return 0;
			}
		}
	}
	public class B
	{
		public int N()
		{
			return 0;
		}
	}
}
//$$ HideEvent
namespace HideEvent
{
	public class A
	{
		public virtual event EventHandler E;
		public event EventHandler F;
	}
	public class B : A
	{
		public new virtual event EventHandler E;
		public new event EventHandler F;
	}
	public class C : B
	{
		public override event EventHandler E;
	}
}
