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
//$$ DeclarationFlags
[Flags]
public enum TS_DeclarationFlags
{
	None = 0,
	Item1 = 1,
	Item2 = 2,
	Item3 = 4,
	All = 7
}
