using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Decompiler.Shared;

namespace ICSharpCode.Decompiler.ILAst {
	static class Utils
	{
		public static void NopMergeILRanges(ILBlockBase block, List<ILNode> newBody, int instrIndexToRemove)
		{
			var body = block.Body;
			ILNode prevNode = null, nextNode = null;
			ILExpression prev = null, next = null;
			if (newBody.Count > 0)
				prev = (prevNode = newBody[newBody.Count - 1]) as ILExpression;
			if (instrIndexToRemove + 1 < body.Count)
				next = (nextNode = body[instrIndexToRemove + 1]) as ILExpression;

			ILNode node = null;

			if (prev != null && prev.Prefixes == null) {
				switch (prev.Code) {
				case ILCode.Call:
				case ILCode.CallGetter:
				case ILCode.Calli:
				case ILCode.CallSetter:
				case ILCode.Callvirt:
				case ILCode.CallvirtGetter:
				case ILCode.CallvirtSetter:
					node = prev;
					break;
				}
			}

			if (next != null && next.Prefixes == null) {
				if (next.Match(ILCode.Leave))
					node = next;
			}

			if (node != null && node == prevNode)
				AddILRangesTryPreviousFirst(body[instrIndexToRemove], prevNode, nextNode, block);
			else
				AddILRangesTryNextFirst(body[instrIndexToRemove], prevNode, nextNode, block);
		}

		public static void LabelMergeILRanges(ILBlockBase block, List<ILNode> newBody, int instrIndexToRemove)
		{
			var body = block.Body;
			ILNode prevNode = null, nextNode = null;
			if (newBody.Count > 0)
				prevNode = newBody[newBody.Count - 1];
			if (instrIndexToRemove + 1 < body.Count)
				nextNode = body[instrIndexToRemove + 1];

			AddILRangesTryNextFirst(body[instrIndexToRemove], prevNode, nextNode, block);
		}

		public static void AddILRangesTryPreviousFirst(ILNode removed, ILNode prev, ILNode next, ILBlockBase block)
		{
			if (removed == null)
				return;
			AddILRangesTryPreviousFirst(prev, next, block, removed);
		}

		public static void AddILRangesTryNextFirst(ILNode removed, ILNode prev, ILNode next, ILBlockBase block)
		{
			if (removed == null)
				return;
			AddILRangesTryNextFirst(prev, next, block, removed);
		}

		public static void AddILRangesTryPreviousFirst(ILNode prev, ILNode next, ILBlockBase block, ILNode removed)
		{
			if (prev != null && prev.SafeToAddToEndILRanges)
				removed.AddSelfAndChildrenRecursiveILRanges(prev.EndILRanges);
			else if (next != null)
				removed.AddSelfAndChildrenRecursiveILRanges(next.ILRanges);
			else if (prev != null)
				removed.AddSelfAndChildrenRecursiveILRanges(block.EndILRanges);
			else
				removed.AddSelfAndChildrenRecursiveILRanges(block.ILRanges);
		}

		public static void AddILRangesTryNextFirst(ILNode prev, ILNode next, ILBlockBase block, ILNode removed)
		{
			if (next != null)
				removed.AddSelfAndChildrenRecursiveILRanges(next.ILRanges);
			else if (prev != null) {
				if (prev.SafeToAddToEndILRanges)
					removed.AddSelfAndChildrenRecursiveILRanges(prev.EndILRanges);
				else
					removed.AddSelfAndChildrenRecursiveILRanges(block.EndILRanges);
			}
			else
				removed.AddSelfAndChildrenRecursiveILRanges(block.ILRanges);
		}

		public static void AddILRangesTryNextFirst(ILNode prev, ILNode next, ILBlockBase block, IEnumerable<ILRange> ilRanges)
		{
			if (next != null)
				next.ILRanges.AddRange(ilRanges);
			else if (prev != null) {
				if (prev.SafeToAddToEndILRanges)
					prev.EndILRanges.AddRange(ilRanges);
				else
					block.EndILRanges.AddRange(ilRanges);
			}
			else
				block.ILRanges.AddRange(ilRanges);
		}

		public static void AddILRangesTryPreviousFirst(List<ILNode> newBody, List<ILNode> body, int removedIndex, ILBlockBase block)
		{
			ILNode prev = newBody.Count > 0 ? newBody[newBody.Count - 1] : null;
			ILNode next = removedIndex + 1 < body.Count ? body[removedIndex + 1] : null;
			AddILRangesTryPreviousFirst(body[removedIndex], prev, next, block);
		}

		public static void AddILRangesTryNextFirst(List<ILNode> newBody, List<ILNode> body, int removedIndex, ILBlockBase block)
		{
			ILNode prev = newBody.Count > 0 ? newBody[newBody.Count - 1] : null;
			ILNode next = removedIndex + 1 < body.Count ? body[removedIndex + 1] : null;
			AddILRangesTryNextFirst(body[removedIndex], prev, next, block);
		}

		/// <summary>
		/// Adds the removed instruction's ILRanges to the next or previous instruction
		/// </summary>
		/// <param name="block">The owner block</param>
		/// <param name="body">Body</param>
		/// <param name="removedIndex">Index of removed instruction</param>
		public static void AddILRanges(ILBlockBase block, List<ILNode> body, int removedIndex)
		{
			AddILRanges(block, body, removedIndex, 1);
		}

		/// <summary>
		/// Adds the removed instruction's ILRanges to the next or previous instruction
		/// </summary>
		/// <param name="block">The owner block</param>
		/// <param name="body">Body</param>
		/// <param name="removedIndex">Index of removed instruction</param>
		/// <param name="numRemoved">Number of removed instructions</param>
		public static void AddILRanges(ILBlockBase block, List<ILNode> body, int removedIndex, int numRemoved)
		{
			var prev = removedIndex - 1 >= 0 ? body[removedIndex - 1] : null;
			var next = removedIndex + numRemoved < body.Count ? body[removedIndex + numRemoved] : null;

			ILNode node = null;
			if (node == null && next is ILExpression)
				node = next;
			if (node == null && prev is ILExpression)
				node = prev;
			if (node == null && next is ILLabel)
				node = next;
			if (node == null && prev is ILLabel)
				node = prev;
			if (node == null)
				node = next ?? prev;	// Using next before prev should work better

			for (int i = 0; i < numRemoved; i++)
				AddILRangesToInstruction(node, prev, next, block, body[removedIndex + i]);
		}

		public static void AddILRanges(ILBlockBase block, List<ILNode> body, int removedIndex, IEnumerable<ILRange> ilRanges)
		{
			var prev = removedIndex - 1 >= 0 ? body[removedIndex - 1] : null;
			var next = removedIndex + 1 < body.Count ? body[removedIndex + 1] : null;

			ILNode node = null;
			if (node == null && next is ILExpression)
				node = next;
			if (node == null && prev is ILExpression)
				node = prev;
			if (node == null && next is ILLabel)
				node = next;
			if (node == null && prev is ILLabel)
				node = prev;
			if (node == null)
				node = next ?? prev;	// Using next before prev should work better

			AddILRangesToInstruction(node, prev, next, block, ilRanges);
		}

		public static void AddILRangesToInstruction(ILNode nodeToAddTo, ILNode prev, ILNode next, ILBlockBase block, ILNode removed)
		{
			Debug.Assert(nodeToAddTo == prev || nodeToAddTo == next || nodeToAddTo == block);
			if (nodeToAddTo != null) {
				if (nodeToAddTo == prev && prev.SafeToAddToEndILRanges) {
					removed.AddSelfAndChildrenRecursiveILRanges(prev.EndILRanges);
					return;
				}
				else if (nodeToAddTo != null && nodeToAddTo == next) {
					removed.AddSelfAndChildrenRecursiveILRanges(next.ILRanges);
					return;
				}
			}
			AddILRangesTryNextFirst(prev, next, block, removed);
		}

		public static void AddILRangesToInstruction(ILNode nodeToAddTo, ILNode prev, ILNode next, ILBlockBase block, IEnumerable<ILRange> ilRanges)
		{
			Debug.Assert(nodeToAddTo == prev || nodeToAddTo == next || nodeToAddTo == block);
			if (nodeToAddTo != null) {
				if (nodeToAddTo == prev && prev.SafeToAddToEndILRanges) {
					prev.EndILRanges.AddRange(ilRanges);
					return;
				}
				else if (nodeToAddTo != null && nodeToAddTo == next) {
					next.ILRanges.AddRange(ilRanges);
					return;
				}
			}
			AddILRangesTryNextFirst(prev, next, block, ilRanges);
		}
	}
}
