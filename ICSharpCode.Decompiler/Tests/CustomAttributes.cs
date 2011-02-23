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
	[AttributeUsage(AttributeTargets.All)]
	public class MyAttribute : Attribute
	{
	}
	[Obsolete("some message")]
	public static void ObsoletedMethod()
	{
		//Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, (AttributeTargets)(AttributeTargets.Property | AttributeTargets.Field));
		Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, AttributeTargets.Property | AttributeTargets.Field);
		AttributeTargets attributeTargets = AttributeTargets.Property | AttributeTargets.Field;
		Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, attributeTargets);
	}
}
