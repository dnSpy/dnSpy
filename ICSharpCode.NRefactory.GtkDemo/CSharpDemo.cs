// CSharpDemo.cs
using System;
using System.Linq;

namespace Demo
{
	#region Demo
	public class CSharpDemo
	{
		static void TestMe (int i)
		{
			Console.WriteLine (i);
			NotDefined ();
		}
		
		public static void Main (string[] args)
		{
			TestMe (args.Count ());
		}
	}
	#endregion
}

