using System;
namespace aa
{
	public static class CustomAtributes
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
			public MyAttribute(CustomAtributes.EnumWithFlag en)
			{
			}
		}
		[CustomAtributes.MyAttribute(CustomAtributes.EnumWithFlag.Item1 | CustomAtributes.EnumWithFlag.Item2)]
		private static int field;
		[CustomAtributes.MyAttribute(CustomAtributes.EnumWithFlag.All)]
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
			Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, AttributeTargets.Property | AttributeTargets.Field);
			AttributeTargets attributeTargets = AttributeTargets.Property | AttributeTargets.Field;
			Console.WriteLine("{0} $$$ {1}", AttributeTargets.Interface, attributeTargets);
		}
	}
}
