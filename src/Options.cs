using System;

namespace Decompiler
{
	public static class Options
	{
		public static int CollapseExpression = int.MaxValue;
		public static int ReduceGraph = int.MaxValue;
		public static bool NodeComments = false;
		public static bool ReduceLoops = true;
		public static bool ReduceConditonals = true;
		public static bool ReduceAstJumps = true;
		public static bool ReduceAstLoops = true;
		public static bool ReduceAstOther = true;
		
		public static void NotifyCollapsingExpression()
		{
			if (CollapseExpression-- <= 0) {
				throw new StopOptimizations();
			}
		}
		
		public static void NotifyReducingGraph()
		{
			if (ReduceGraph-- <= 0) {
				throw new StopOptimizations();
			}
		}
	}
	
	class StopOptimizations: Exception
	{
		
	}
}
