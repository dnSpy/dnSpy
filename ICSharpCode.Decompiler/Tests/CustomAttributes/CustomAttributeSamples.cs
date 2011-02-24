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
		None
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
