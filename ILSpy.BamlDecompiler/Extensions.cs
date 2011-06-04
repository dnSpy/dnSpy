// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Linq;

namespace ILSpy.BamlDecompiler
{
	public static class Extensions
	{
		public static string TrimEnd(this string target, Func<char, bool> predicate)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			
			while (predicate(target.LastOrDefault()))
				target = target.Remove(target.Length - 1);
			
			return target;
		}
	}
}
