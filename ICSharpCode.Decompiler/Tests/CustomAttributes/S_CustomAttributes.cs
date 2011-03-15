// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace aa
{
	public static class CustomAttributes
	{
		[Flags]
		public enum EnumWithFlag
		{
			All = 15,
			None = 0,
			Item1 = 1,
			Item2 = 2,
			Item3 = 4,
			Item4 = 8
		}
		[AttributeUsage(AttributeTargets.All)]
		public class MyAttribute : Attribute
		{
			public MyAttribute(object val)
			{
			}
		}
		[CustomAttributes.MyAttribute(CustomAttributes.EnumWithFlag.Item1 | CustomAttributes.EnumWithFlag.Item2)]
		private static int field;
		[CustomAttributes.MyAttribute(CustomAttributes.EnumWithFlag.All)]
		public static string Property
		{
			get
			{
				return "aa";
			}
		}
		[Obsolete("some message")]
		public static void ObsoletedMethod()
		{
			//Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, (AttributeTargets)(AttributeTargets.Property | AttributeTargets.Field));
			Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, AttributeTargets.Property | AttributeTargets.Field);
			AttributeTargets attributeTargets = AttributeTargets.Property | AttributeTargets.Field;
			Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, attributeTargets);
		}
		// No Boxing
		[CustomAttributes.MyAttribute(new StringComparison[]
		{
			StringComparison.Ordinal, 
			StringComparison.CurrentCulture
		})]
		public static void ArrayAsAttribute1()
		{
		}
		// Boxing of each array element
		[CustomAttributes.MyAttribute(new object[]
		{
			StringComparison.Ordinal, 
			StringComparison.CurrentCulture
		})]
		public static void ArrayAsAttribute2()
		{
		}
	}
}