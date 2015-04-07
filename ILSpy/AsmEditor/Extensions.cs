
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	static class Extensions
	{
		public static bool IsInSameModule(this ILSpyTreeNode[] nodes)
		{
			if (nodes == null || nodes.Length == 0)
				return false;
			var module = ILSpyTreeNode.GetModule(nodes[0]);
			for (int i = 0; i < nodes.Length; i++) {
				if (module != ILSpyTreeNode.GetModule(nodes[i]))
					return false;
			}
			return true;
		}
	}
}
