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
using dndbg.Engine.COM.CorDebug;
using dnlib.DotNet;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackFrameVM {
		int Index { get; }
		bool IsCurrentFrame { get; }
		string Name { get; }
		void RefreshIconFields();
	}

	sealed class MessageCallStackFrameVM : ICallStackFrameVM {
		public int Index {
			get { return index; }
		}
		readonly int index;

		public bool IsCurrentFrame {
			get { return false; }
		}

		public string Name {
			get { return name; }
		}
		readonly string name;

		public MessageCallStackFrameVM(int index, string name) {
			this.index = index;
			this.name = name;
		}

		public void RefreshIconFields() {
		}
	}

	sealed class CallStackFrameVM : ViewModelBase, ICallStackFrameVM {
		public int Index {
			get { return index; }
		}
		readonly int index;

		public bool IsUserCode {
			get { return isUserCode; }
			set {
				if (isUserCode != value) {
					isUserCode = value;
					OnPropertyChanged("IsUserCode");
				}
			}
		}
		bool isUserCode;

		public bool IsCurrentFrame {
			get { return isCurrentFrame; }
			set {
				if (isCurrentFrame != value) {
					isCurrentFrame = value;
					OnPropertyChanged("IsCurrentFrame");
				}
			}
		}
		bool isCurrentFrame;

		public string Name {
			get { return name ?? (name = CreateName()); }
		}
		string name;

		public CorFrame Frame {
			get { return frame; }
		}
		readonly CorFrame frame;

		public CallStackFrameVM(int index, CorFrame frame) {
			this.index = index;
			this.frame = frame;
		}

		string CreateName() {
			if (frame.IsILFrame)
				return CreateNameIL();
			if (frame.IsNativeFrame)
				return CreateNameNative();
			if (frame.IsInternalFrame)
				return CreateNameInternal();
			return CreateDefaultName();
		}

		string CreateNameIL() {
			//TODO: Call a CorFrame method to get the string since it could be in an in-memory module

			var serAsm = frame.GetSerializedDnModuleWithAssembly();
			if (serAsm == null)
				return CreateDefaultName();

			var loadedAsm = MainWindow.Instance.LoadAssembly(serAsm.Value);
			if (loadedAsm == null)
				return CreateDefaultName();

			var mod = loadedAsm.ModuleDefinition as ModuleDefMD;
			if (mod == null)
				return CreateDefaultName();

			var md = mod.ResolveToken(frame.Token) as MethodDef;
			if (md == null)
				return CreateDefaultName();

			return md.ToString();
		}

		string CreateNameNative() {
			//TODO:
			return CreateDefaultName();
		}

		string CreateNameInternal() {
			switch (frame.InternalFrameType) {
			case CorDebugInternalFrameType.STUBFRAME_M2U:
				return "[Managed to Native Transition]";

			case CorDebugInternalFrameType.STUBFRAME_U2M:
				return "[Native to Managed Transition]";

			case CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION:
				return "[Appdomain Transition]";

			case CorDebugInternalFrameType.STUBFRAME_LIGHTWEIGHT_FUNCTION:
				return "[Lightweight Function]";

			case CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL:
				return "[Function Evaluation]";

			case CorDebugInternalFrameType.STUBFRAME_INTERNALCALL:
				return "[Internal Call]";

			case CorDebugInternalFrameType.STUBFRAME_CLASS_INIT:
				return "[Class Init]";

			case CorDebugInternalFrameType.STUBFRAME_EXCEPTION:
				return "[Exception]";

			case CorDebugInternalFrameType.STUBFRAME_SECURITY:
				return "[Security]";

			case CorDebugInternalFrameType.STUBFRAME_JIT_COMPILATION:
				return "[JIT Compilation]";

			case CorDebugInternalFrameType.STUBFRAME_NONE:
			default:
				return string.Format("[Internal Frame 0x{0:X}]", (int)frame.InternalFrameType);
			}
		}

		string CreateDefaultName() {
			return string.Format("Frame #{0}", index);
		}

		public void RefreshIconFields() {
			// theme got changed, refresh icon
			if (IsCurrentFrame)
				OnPropertyChanged("IsCurrentFrame");
		}
	}
}
