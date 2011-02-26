// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

//$CS
using System;
//$CE

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
		public void Method([MyAttribute]int val)
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
				return;
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
				return;
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
				return;
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
