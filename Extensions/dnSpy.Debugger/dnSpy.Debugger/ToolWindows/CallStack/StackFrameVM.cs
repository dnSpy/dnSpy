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

using System;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	abstract class StackFrameVM : ViewModelBase {
		public bool IsActive {
			get => isActive;
			set {
				if (isActive == value)
					return;
				isActive = value;
				OnPropertyChanged(nameof(InstructionPointerImage));
			}
		}
		bool isActive;

		public BreakpointKind? BreakpointKind {
			get => breakpointKind;
			set {
				if (breakpointKind == value)
					return;
				breakpointKind = value;
				OnPropertyChanged(nameof(BreakpointImage));
			}
		}
		BreakpointKind? breakpointKind;

		public ImageReference BreakpointImage {
			get {
				if (breakpointKind == null)
					return ImageReference.None;
				return BreakpointImageUtilities.GetImage(breakpointKind.Value);
			}
		}

		public virtual ImageReference InstructionPointerImage {
			get {
				if (Index == 0)
					return DsImages.CurrentInstructionPointer;
				if (IsActive)
					return DsImages.CallReturnInstructionPointer;
				return ImageReference.None;
			}
		}

		public object NameObject => new FormatterObject<StackFrameVM>(this, PredefinedTextClassifierTags.CallStackWindowName);

		public int Index {
			get => index;
			set {
				if (index == value)
					return;
				bool raiseEvent = index == 0 || value == 0;
				index = value;
				if (raiseEvent)
					OnPropertyChanged(nameof(InstructionPointerImage));
			}
		}
		int index;

		public abstract ICallStackContext Context { get; }

		protected abstract ClassifiedTextCollection CreateName();
		public ClassifiedTextCollection CachedOutput {
			get {
				if (cachedOutput.IsDefault)
					cachedOutput = CreateName();
				return cachedOutput;
			}
		}
		ClassifiedTextCollection cachedOutput;

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
		}

		// UI thread
		internal void RefreshHexFields_UI() => RefreshName_UI();

		// UI thread
		internal void RefreshName_UI() {
			Context.UIDispatcher.VerifyAccess();
			cachedOutput = default(ClassifiedTextCollection);
			OnPropertyChanged(nameof(NameObject));
		}
	}

	sealed class NormalStackFrameVM : StackFrameVM {
		public DbgStackFrame Frame => frame;
		public override ICallStackContext Context { get; }

		DbgStackFrame frame;

		public NormalStackFrameVM(DbgStackFrame frame, ICallStackContext context, int index) {
			this.frame = frame ?? throw new ArgumentNullException(nameof(frame));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Index = index;
		}

		public void SetFrame_UI(DbgStackFrame frame) {
			this.frame = frame;
			RefreshName_UI();
		}

		protected override ClassifiedTextCollection CreateName() {
			Frame.Format(Context.ClassifiedTextWriter, Context.StackFrameFormatOptions);
			return Context.ClassifiedTextWriter.GetClassifiedText();
		}
	}

	sealed class MessageStackFrameVM : StackFrameVM {
		public override ICallStackContext Context { get; }

		public override ImageReference InstructionPointerImage => DsImages.StatusError;

		readonly string message;

		public MessageStackFrameVM(string message, ICallStackContext context, int index) {
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Index = index;
			this.message = message ?? throw new ArgumentNullException(nameof(message));
		}

		protected override ClassifiedTextCollection CreateName() => new ClassifiedTextCollection(new[] { new ClassifiedText(BoxedTextColor.Error, message) });
	}
}
