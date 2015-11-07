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
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackFrameVM {
		int Index { get; }
		bool IsCurrentFrame { get; }
		string Name { get; }
		object NameObject { get; }
		object ImageObject { get; }
		CachedOutput CachedOutput { get; }
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
			get { return cachedOutput.ToString(); }
		}

		public object NameObject {
			get { return owner.CallStackObjectCreator.CreateName(this); }
		}

		public object ImageObject {
			get { return this; }
		}

		public CachedOutput CachedOutput {
			get { return cachedOutput; }
		}
		readonly CachedOutput cachedOutput;

		readonly CallStackVM owner;

		public MessageCallStackFrameVM(CallStackVM owner, int index, string name) {
			this.owner = owner;
			this.index = index;
			this.cachedOutput = CachedOutput.Create(name, TypeColor.Error);
		}
	}

	sealed class CallStackFrameVM : ViewModelBase, ICallStackFrameVM {
		public int Index {
			get { return index; }
			internal set {
				if (index != value) {
					int oldIndex = index;
					index = value;
					OnPropertyChanged("Index");
					if (index == 0 || oldIndex == 0)
						RefreshImage();
				}
			}
		}
		int index;

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
			get {
				if (cachedOutput == null)
					cachedOutput = CachedOutput.Create(frame, TypePrinterFlags);

				return owner.CallStackObjectCreator.CreateName(this);
			}
		}
		CachedOutput? cachedOutput;

		public CachedOutput CachedOutput {
			get { return cachedOutput.Value; }
		}

		public object ImageObject {
			get { return this; }
		}

		public string Name {
			get { return ComputeName(); }
		}

		public CorFrame Frame {
			get { return frame; }
			internal set {
				frame = value;

				if (cachedOutput == null || !HasPropertyChangedHandlers) {
					cachedOutput = null;
					OnPropertyChanged("NameObject");
				}
				else {
					var newCachedOutput = CachedOutput.Create(frame, TypePrinterFlags);
					if (newCachedOutput.Equals(cachedOutput.Value))
						return;

					cachedOutput = newCachedOutput;
					OnPropertyChanged("NameObject");
				}
			}
		}
		CorFrame frame;

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
			cachedOutput = null;
			OnPropertyChanged("NameObject");
		}

		void RefreshImage() {
			OnPropertyChanged("ImageObject");
		}
	}
}
