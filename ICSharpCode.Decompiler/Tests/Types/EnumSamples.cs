// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

//$CS
using System;
//$CE

//$$ SingleValue
public class TS_SingleValue
{
	public AttributeTargets Method()
	{
		return AttributeTargets.Class;
	}
}
//$$ TwoValuesOr
public class TS_TwoValuesOr
{
	public AttributeTargets Method()
	{
		return AttributeTargets.Class | AttributeTargets.Method;
	}
}
//$$ ThreeValuesOr
public class TS_ThreeValuesOr
{
	public AttributeTargets Method()
	{
		return AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter;
	}
}
//$$ UnknownNumericValue
public class TS_UnknownNumericValue
{
	public AttributeTargets Method()
	{
		return (AttributeTargets)1000000;
	}
}
//$$ AllValue
public class TS_AllValue
{
	public AttributeTargets Method()
	{
		return AttributeTargets.All;
	}
}
//$$ ZeroValue
public class TS_ZeroValue
{
	public AttributeTargets Method()
	{
		return (AttributeTargets)0;
	}
}
//$$ PreservingTypeWhenBoxed
public class TS_PreservingTypeWhenBoxed
{
	public object Method()
	{
		return AttributeTargets.Delegate;
	}
}
//$$ PreservingTypeWhenBoxedTwoEnum
public class TS_PreservingTypeWhenBoxedTwoEnum
{
	public object Method()
	{
		return AttributeTargets.Class | AttributeTargets.Delegate;
	}
}
//$$ DeclarationSimpleEnum
public enum TS_DeclarationSimpleEnum
{
	Item1,
	Item2
}
//$$ DeclarationLongBasedEnum
public enum TS_DeclarationLongBasedEnum : long
{
	Item1,
	Item2
}
//$$ DeclarationLongWithInitializers
public enum TS_DeclarationLongWithInitializers : long
{
	Item1,
	Item2 = 20L,
	Item3
}
//$$ DeclarationShortWithInitializers
public enum TS_DeclarationShortWithInitializers : short
{
	Item1,
	Item2 = 20,
	Item3
}
//$$ DeclarationByteWithInitializers
public enum TS_DeclarationByteWithInitializers : byte
{
	Item1,
	Item2 = 20,
	Item3
}
