/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	abstract class DbgValueNodeReader {
		public abstract DbgValueNode GetDebuggerNode(ValueNodeImpl valueNode);
		public abstract DbgValueNode GetDebuggerNodeForReuse(ValueNodeImpl valueNode);
	}

	sealed class DbgValueNodeReaderImpl : DbgValueNodeReader {
		public override DbgValueNode GetDebuggerNode(ValueNodeImpl valueNode) {
			var parent = valueNode.Parent;
			uint startIndex = valueNode.DbgValueNodeChildIndex;
			int count = 1;//TODO:
			var newNodes = parent.DebuggerValueNode.GetChildren(startIndex, count);
			newNodes[0].Runtime.CloseOnContinue(newNodes);
			Debug.Assert(count == 1);
			return newNodes[0];
		}

		public override DbgValueNode GetDebuggerNodeForReuse(ValueNodeImpl valueNode) {
			var parent = valueNode.Parent;
			uint startIndex = valueNode.DbgValueNodeChildIndex;
			//TODO: Read more than one value. This code would need help from the caller
			//		since it wants to exit its method as quickly as possible if the new
			//		value isn't equal to the old value.
			const int count = 1;
			var newNodes = parent.DebuggerValueNode.GetChildren(startIndex, count);
			newNodes[0].Runtime.CloseOnContinue(newNodes);
			Debug.Assert(count == 1);
			return newNodes[0];
		}
	}
}
