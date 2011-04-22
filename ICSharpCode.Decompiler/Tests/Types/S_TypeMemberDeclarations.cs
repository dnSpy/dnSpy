// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
//$$ PropertyHiding
namespace PropertyHiding
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
//$$ IndexerHidingGeneric
namespace IndexerHidingGeneric
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
//$$ MethodHiding
namespace MethodHiding
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
//$$ MethodHideGeneric
namespace MethodHideGeneric
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
//$$ MethodHideGenericSkipPrivate
namespace MethodHideGenericSkipPrivate
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
//$$ MethodHideGeneric2
namespace MethodHideGeneric2
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
//$$ EventHiding
namespace EventHiding
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
