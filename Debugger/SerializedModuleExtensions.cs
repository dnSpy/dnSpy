/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger {
	static class SerializedModuleExtensions {
		public static SerializedDnModule ToSerializedDnModule(this SerializedDnSpyModule self) {
			return new SerializedDnModule(self.AssemblyFullName, self.ModuleName, self.IsDynamic, self.IsInMemory);
		}

		public static SerializedDnSpyModule ToSerializedDnSpyModule(this SerializedDnModule self) {
			return SerializedDnSpyModule.Create(self.AssemblyFullName, self.ModuleName, self.IsDynamic, self.IsInMemory);
		}

		public static SerializedDnSpyModule ToSerializedDnSpyModule(this ModuleDef module) {
			return MainWindow.Instance.DnSpyFileListTreeNode.GetSerializedDnSpyModule(module);
		}

		public static SerializedDnModule ToSerializedDnModule(this IMemberDef md) {
			return md.Module.ToSerializedDnSpyModule().ToSerializedDnModule();
		}
	}
}
