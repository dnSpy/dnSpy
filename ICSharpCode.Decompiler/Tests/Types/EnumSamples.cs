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
