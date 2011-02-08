using System;

namespace Decompiler
{
	public static class Options
	{
		public static string TypeFilter = null;
		public static int CollapseExpression = 1000;
		public static int ReduceGraph = 1000;
		public static bool NodeComments = false;
		public static bool ReduceLoops = true;
		public static bool ReduceConditonals = true;
		public static bool ReduceAstJumps = true;
		public static bool ReduceAstLoops = true;
		public static bool ReduceAstOther = true;
	}
	
	class StopOptimizations: Exception
	{
		
	}
}
