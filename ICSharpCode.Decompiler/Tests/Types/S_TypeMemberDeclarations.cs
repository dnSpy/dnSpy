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
