// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public static class DelegateConstruction
{
	public static void Test(this string a)
	{
	}
	
	public static Action<string> ExtensionMethodUnbound()
	{
		return new Action<string>(DelegateConstruction.Test);
	}
	
	public static Action ExtensionMethodBound()
	{
		return new Action("abc".Test);
	}
	
	public static Action ExtensionMethodBoundOnNull()
	{
		return new Action(((string)null).Test);
	}
	
	public static object StaticMethod()
	{
		return new Func<Action>(DelegateConstruction.ExtensionMethodBound);
	}
	
	public static object InstanceMethod()
	{
		return new Func<string>("hello".ToUpper);
	}
	
	public static object InstanceMethodOnNull()
	{
		return new Func<string>(((string)null).ToUpper);
	}
}
