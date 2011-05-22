// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public static class CustomShortCircuitOperators
{
	private class B
	{
		public static bool operator true(CustomShortCircuitOperators.B x)
		{
			return true;
		}

		public static bool operator false(CustomShortCircuitOperators.B x)
		{
			return false;
		}
	}

	private class C : CustomShortCircuitOperators.B
	{
		public static CustomShortCircuitOperators.C operator &(CustomShortCircuitOperators.C x, CustomShortCircuitOperators.C y)
		{
			return null;
		}
		
		public static CustomShortCircuitOperators.C operator |(CustomShortCircuitOperators.C x, CustomShortCircuitOperators.C y)
		{
			return null;
		}

		private static void Main()
		{
			CustomShortCircuitOperators.C c = new CustomShortCircuitOperators.C();
			CustomShortCircuitOperators.C c2 = new CustomShortCircuitOperators.C();
			CustomShortCircuitOperators.C c3 = c && c2;
			CustomShortCircuitOperators.C c4 = c || c2;
			Console.WriteLine(c3.ToString());
			Console.WriteLine(c4.ToString());
		}
	}
}