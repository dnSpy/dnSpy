/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Images;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackFrameContext {
		IImageManager ImageManager { get; }
		TypePrinterFlags TypePrinterFlags { get; }
		bool SyntaxHighlight { get; }
	}

	sealed class CallStackFrameContext : ICallStackFrameContext {
		public IImageManager ImageManager { get; private set; }
		public TypePrinterFlags TypePrinterFlags { get; set; }
		public bool SyntaxHighlight { get; set; }

		public CallStackFrameContext(IImageManager ImageManager) {
			this.ImageManager = ImageManager;
		}
	}

	interface ICallStackFrameVM {
		int Index { get; }
		bool IsCurrentFrame { get; }
		string Name { get; }
		CachedOutput CachedOutput { get; }
		ICallStackFrameContext Context { get; }
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

		public object ImageObject { get { return this; } }
		public object NameObject { get { return this; } }

		public CachedOutput CachedOutput {
			get { return cachedOutput; }
		}
		readonly CachedOutput cachedOutput;

		public ICallStackFrameContext Context {
			get { return context; }
		}
		readonly ICallStackFrameContext context;

		public MessageCallStackFrameVM(ICallStackFrameContext context, int index, string name) {
			this.context = context;
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

		public CachedOutput CachedOutput {
			get {
				if (cachedOutput == null)
					cachedOutput = CachedOutput.Create(frame, Context.TypePrinterFlags);
				return cachedOutput.Value;
			}
		}
		CachedOutput? cachedOutput;

		public object ImageObject { get { return this; } }
		public object NameObject { get { return this; } }

		public string Name {
			get { return ComputeName(); }
		}

		public CorFrame Frame {
			get {
				if (frame.IsNeutered)
					frame = process.FindFrame(frame) ?? frame;
				return frame;
			}
		}

		public void SetFrame(CorFrame frame, DnProcess process) {
			this.frame = frame;
			this.process = process;

			if (cachedOutput == null || !HasPropertyChangedHandlers) {
				cachedOutput = null;
				OnPropertyChanged("NameObject");
			}
			else {
				var newCachedOutput = CachedOutput.Create(frame, Context.TypePrinterFlags);
				if (newCachedOutput.Equals(cachedOutput.Value))
					return;

				cachedOutput = newCachedOutput;
				OnPropertyChanged("NameObject");
			}
		}
		CorFrame frame;
		DnProcess process;

		public ICallStackFrameContext Context {
			get { return context; }
		}
		readonly ICallStackFrameContext context;

		public CallStackFrameVM(ICallStackFrameContext context, int index, CorFrame frame, DnProcess process) {
			this.context = context;
			this.index = index;
			this.frame = frame;
			this.process = process;
		}

		string ComputeName() {
			var output = new StringBuilderTypeOutput();
			frame.Write(output, Context.TypePrinterFlags);
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
