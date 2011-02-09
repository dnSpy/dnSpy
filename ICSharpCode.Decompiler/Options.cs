using System;

namespace Decompiler
{
	public static class Options
	{
		public static readonly bool NodeComments = false;
		public static readonly bool ReduceLoops = true;
		public static readonly bool ReduceConditonals = true;
		public static readonly bool ReduceAstJumps = true;
		public static readonly bool ReduceAstLoops = true;
		public static readonly bool ReduceAstOther = true;
	}
}
