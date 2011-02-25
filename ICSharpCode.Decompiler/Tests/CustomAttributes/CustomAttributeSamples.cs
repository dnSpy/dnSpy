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
//$$ AttributeAppliedToEvent (ignored)
namespace AttributeAppliedToEvent
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
//$$ AttributeAppliedToField
namespace AttributeAppliedToField
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
//$$ AttributeAppliedToProperty
namespace AttributeAppliedToProperty
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
//$$ AttributeAppliedToDelegate
[Obsolete("reason")]
public delegate int  AttributeAppliedToDelegate();
//$$ AttributeAppliedToMethod
namespace AttributeAppliedToMethod
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
//$$ AttributeAppliedToInterface
[Obsolete("reason")]
public interface AttributeAppliedToInterface
{
}
//$$ AttributeAppliedToStruct
[Obsolete("reason")]
public struct AttributeAppliedToStruct
{
	public int Field;
}
//$$ NamedPropertyParameter
namespace NamedPropertyParameter
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class MyAttributeAttribute : Attribute
	{
	}
}
//$$ NamedStringPropertyParameter
namespace NamedStringPropertyParameter
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
//$$ NamedTypePropertyParameter
namespace NamedTypePropertyParameter
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
//$$ NamedEnumPropertyParameter
namespace NamedEnumPropertyParameter
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
