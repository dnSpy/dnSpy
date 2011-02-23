// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;


public static class CustomAtributes
{
	[Flags]
	public enum EnumWithFlag
	{
//		Item1,
		Item2
	}
	[Obsolete("some message")]
	public static void ObsoletedMethod()
	{
	}
}
