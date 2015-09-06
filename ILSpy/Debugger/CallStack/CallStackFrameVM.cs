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

using System;
using dndbg.Engine;
using dnSpy.MVVM;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackFrameVM {
		int Index { get; }
		bool IsCurrentFrame { get; }
		string Name { get; }
		object NameObject { get; }
		object ImageObject { get; }
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

		public object NameObject {
			get { return this; }
		}

		public object ImageObject {
			get { return this; }
		}

		readonly string name;

		public MessageCallStackFrameVM(int index, string name) {
			this.index = index;
			this.name = name;
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
					RefreshImage();
				}
			}
		}
		bool isCurrentFrame;

		public TypePrinterFlags TypePrinterFlags {
			get { return owner.TypePrinterFlags; }
		}

		public object NameObject {
			get { return this; }
		}

		public object ImageObject {
			get { return this; }
		}

		public string Name {
			get { return ComputeName(); }
		}

		public CorFrame Frame {
			get { return frame; }
		}
		readonly CorFrame frame;

		readonly CallStackVM owner;

		public CallStackFrameVM(CallStackVM owner, int index, CorFrame frame) {
			this.owner = owner;
			this.index = index;
			this.frame = frame;
		}

		string ComputeName() {
			var output = new StringBuilderTypeOutput();
			frame.Write(output, TypePrinterFlags);
			return output.ToString();
		}

		public void RefreshThemeFields() {
			if (Index == 0 || IsCurrentFrame)
				RefreshImage();
			RefreshName();
		}

		public void RefreshName() {
			OnPropertyChanged("NameObject");
		}

		void RefreshImage() {
			OnPropertyChanged("ImageObject");
		}
	}
}
