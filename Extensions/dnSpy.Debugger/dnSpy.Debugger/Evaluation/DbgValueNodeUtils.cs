/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	static class DbgValueNodeUtils {
		public static DbgValueNode[] ToValueNodeArray(DbgLanguage language, DbgRuntime runtime, DbgEngineValueNode[] engineNodes) {
			var nodes = new DbgValueNode[engineNodes.Length];
			for (int i = 0; i < nodes.Length; i++)
				nodes[i] = new DbgValueNodeImpl(language, runtime, engineNodes[i]);
			runtime.CloseOnContinue(nodes);
			return nodes;
		}

		public static DbgLocalsValueNodeInfo[] ToLocalsValueNodeInfoArray(DbgLanguage language, DbgRuntime runtime, DbgEngineLocalsValueNodeInfo[] engineNodeInfos) {
			var infos = new DbgLocalsValueNodeInfo[engineNodeInfos.Length];
			var nodes = new DbgValueNode[engineNodeInfos.Length];
			for (int i = 0; i < nodes.Length; i++) {
				var node = new DbgValueNodeImpl(language, runtime, engineNodeInfos[i].ValueNode);
				nodes[i] = node;
				infos[i] = new DbgLocalsValueNodeInfo(engineNodeInfos[i].Kind, node);
			}
			runtime.CloseOnContinue(nodes);
			return infos;
		}
	}
}
