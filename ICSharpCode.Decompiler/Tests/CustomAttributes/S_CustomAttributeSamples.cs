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

//$$ TargetModule (ignored)
//[module: CLSCompliantAttribute(false)]
//$$ ParameterlessAttributeUsage
namespace ParameterLessAttributeUsage
{
	[Flags]
	public enum EnumWithFlagsAttribute
	{
		None = 0
	}
}
//$$ AttributeWithEnumArgument
namespace AttributeWithEnumArgument
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
}
//$$ AttributeWithEnumExpressionArgument
namespace AttributeWithEnumExpressionArgument
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
	public class MyAttributeAttribute : Attribute
	{
	}
}
//$$ AttributeWithStringExpressionArgument
namespace AttributeWithStringExpressionArgument
{
	[Obsolete("message")]
	public class ObsoletedClass
	{
	}
}
//$$ AttributeWithTypeArgument
namespace AttributeWithTypeArgument
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyTypeAttribute : Attribute
	{
		public MyTypeAttribute(Type t)
		{
		}
	}

	[MyType(typeof(Attribute))]
	public class SomeClass
	{
	}
}
//$$ AppliedToEvent
namespace AppliedToEvent
{
	[AttributeUsage(AttributeTargets.Event)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class TestClass
	{
		[MyAttribute]
		public event EventHandler MyEvent;
	}
}
//$$ AppliedToEventMethods
namespace AppliedToEventMethods
{
	[AttributeUsage(AttributeTargets.Method)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class TestClass
	{
		[method: MyAttribute]
		public event EventHandler MyEvent;
	}
}
//$$ AppliedToField
namespace AppliedToField
{
	[AttributeUsage(AttributeTargets.Field)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class TestClass
	{
		[MyAttribute]
		public int Field;
	}
}
//$$ AppliedToProperty
namespace AppliedToProperty
{
	public class TestClass
	{
		[Obsolete("reason")]
		public int Property
		{
			get
			{
				return 0;
			}
		}
	}
}
//$$ AppliedToPropertyGet
namespace AppliedToPropertyGet
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class TestClass
	{
		public int Property
		{
			[MyAttribute]
			get
			{
				return 0;
			}
		}
	}
}
//$$ AppliedToPropertySet
namespace AppliedToPropertySet
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class TestClass
	{
		public int Property
		{
			get
			{
				return 3;
			}
			[MyAttribute]
			set
			{
			}
		}
	}
}
//$$ AppliedToIndexer
namespace AppliedToIndexer
{
	public class TestClass
	{
		[Obsolete("reason")]
		public int this[int i]
		{
			get
			{
				return 0;
			}
		}
	}
}
//$$ AppliedToDelegate
[Obsolete("reason")]
public delegate int AppliedToDelegate();
//$$ AppliedToMethod
namespace AppliedToMethod
{
	[AttributeUsage(AttributeTargets.Method)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class TestClass
	{
		[MyAttribute]
		public void Method()
		{
		}
	}
}
//$$ AppliedToInterface
[Obsolete("reason")]
public interface AppliedToInterface
{
}
//$$ AppliedToStruct
[Obsolete("reason")]
public struct AppliedToStruct
{
	public int Field;
}
//$$ AppliedToParameter
namespace AppliedToParameter
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public void Method([MyAttribute] int val)
		{
		}
	}
}
//$$ NamedInitializerProperty
namespace NamedInitializerProperty
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class MyAttributeAttribute : Attribute
	{
	}
}
//$$ NamedInitializerPropertyString
namespace NamedInitializerPropertyString
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
		public string Prop
		{
			get
			{
				return "";
			}
			set
			{
			}
		}
	}
	[MyAttribute(Prop = "value")]
	public class MyClass
	{
	}
}
//$$ NamedInitializerPropertyType
namespace NamedInitializerPropertyType
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
		public Type Prop
		{
			get
			{
				return null;
			}
			set
			{
			}
		}
	}
	[MyAttribute(Prop = typeof(Enum))]
	public class MyClass
	{
	}
}
//$$ NamedInitializerPropertyEnum
namespace NamedInitializerPropertyEnum
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
		public AttributeTargets Prop
		{
			get
			{
				return AttributeTargets.All;
			}
			set
			{
			}
		}
	}
	[MyAttribute(Prop = (AttributeTargets.Class | AttributeTargets.Method))]
	public class MyClass
	{
	}
}
//$$ NamedInitializerFieldEnum
namespace NamedInitializerFieldEnum
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
		public AttributeTargets Field;
	}
	[MyAttribute(Field = (AttributeTargets.Class | AttributeTargets.Method))]
	public class MyClass
	{
	}
}
//$$ TargetReturn
namespace TargetReturn
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		[return: MyAttribute]
		public int MyMethod()
		{
			return 5;
		}
	}
}
//$$ TargetPropertyGetReturn
namespace TargetPropertyGetReturn
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public int Prop
		{
			[return: MyAttribute]
			get
			{
				return 3;
			}
		}
	}
}
//$$ TargetPropertySetParam
namespace TargetPropertySetParam
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public int Prop
		{
			[param: MyAttribute]
			set
			{
			}
		}
	}
}
//$$ TargetPropertySetReturn
namespace TargetPropertySetReturn
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public int Prop
		{
			get
			{
				return 3;
			}
			[return: MyAttribute]
			set
			{
			}
		}
	}
}
//$$ TargetPropertyIndexGetReturn
namespace TargetPropertyIndexGetReturn
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public int this[string s]
		{
			[return: MyAttribute]
			get
			{
				return 3;
			}
		}
	}
}
//$$ TargetPropertyIndexParamOnlySet
namespace TargetPropertyIndexParamOnlySet
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public int this[[MyAttribute] string s]
		{
			set
			{
			}
		}
	}
}
//$$ TargetPropertyIndexParamOnlyGet
namespace TargetPropertyIndexParamOnlyGet
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public int this[[MyAttribute] string s]
		{
			get
			{
				return 3;
			}
		}
	}
}
//$$ TargetPropertyIndexSetReturn
namespace TargetPropertyIndexSetReturn
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass
	{
		public string this[int index]
		{
			get
			{
				return "";
			}
			[return: MyAttribute]
			set
			{
			}
		}
	}
}
//$$ TargetPropertyIndexSetMultiParam
namespace TargetPropertyIndexSetMultiParam
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
		public int Field;
	}
	public class MyClass
	{
		public string this[[MyAttribute(Field = 2)] int index1, [MyAttribute(Field = 3)] int index2]
		{
			get
			{
				return "";
			}
			[param: MyAttribute]
			set
			{
			}
		}
	}
}
//$$ ClassAttributeOnTypeParameter
namespace ClassAttributeOnTypeParameter
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	public class MyClass<[MyAttribute] T>
	{
	}
}
//$$ AttributeOnReturnTypeOfDelegate
namespace AttributeOnReturnTypeOfDelegate
{
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttributeAttribute : Attribute
	{
	}
	[return: MyAttribute]
	public delegate void Test();
}
