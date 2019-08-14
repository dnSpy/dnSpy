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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
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

		public ImageReference BreakpointImage {
			get {
				if (!breakpointKindInitd) {
					breakpointKindInitd = true;
					breakpointKind = GetBreakpointKind();
				}
				if (breakpointKind is null)
					return ImageReference.None;
				return BreakpointImageUtilities.GetImage(breakpointKind.Value);
			}
		}
		bool breakpointKindInitd;
		BreakpointKind? breakpointKind;

		protected virtual BreakpointKind? GetBreakpointKind() => null;

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

		protected abstract ClassifiedTextCollection CreateName();
		public ClassifiedTextCollection CachedOutput {
			get {
				if (cachedOutput.IsDefault)
					cachedOutput = CreateName();
				return cachedOutput;
			}
		}
		ClassifiedTextCollection cachedOutput;

		public ICallStackContext Context { get; }

		protected StackFrameVM(ICallStackContext context) => Context = context ?? throw new ArgumentNullException(nameof(context));

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
		}

		// UI thread
		internal void RefreshName_UI() {
			Context.UIDispatcher.VerifyAccess();
			cachedOutput = default;
			OnPropertyChanged(nameof(NameObject));
		}

		// UI thread
		internal void RefreshBreakpoint_UI() {
			Context.UIDispatcher.VerifyAccess();
			breakpointKindInitd = false;
			OnPropertyChanged(nameof(BreakpointImage));
		}

		public virtual void Dispose() { }
	}

	sealed class NormalStackFrameVM : StackFrameVM {
		public DbgStackFrame Frame => frame;
		DbgLanguage? language;
		DbgStackFrame frame;

		DbgEvaluationContext? evaluationContext;

		readonly Func<NormalStackFrameVM, BreakpointKind?> getBreakpointKind;

		public NormalStackFrameVM(DbgLanguage? language, DbgStackFrame frame, ICallStackContext context, int index, Func<NormalStackFrameVM, BreakpointKind?> getBreakpointKind)
			: base(context) {
			this.language = language;
			this.frame = frame;
			Index = index;
			this.getBreakpointKind = getBreakpointKind ?? throw new ArgumentNullException(nameof(getBreakpointKind));
		}

		public void SetLanguage_UI(DbgLanguage? language) {
			this.language = language;
			evaluationContext?.Close();
			evaluationContext = null;
			RefreshName_UI();
			RefreshBreakpoint_UI();
		}

		public void SetFrame_UI(DbgLanguage? language, DbgStackFrame frame) {
			this.language = language;
			this.frame = frame;
			evaluationContext?.Close();
			evaluationContext = null;
			RefreshName_UI();
			RefreshBreakpoint_UI();
		}

		protected override ClassifiedTextCollection CreateName() {
			Debug2.Assert(!(language is null));
			if (!(language is null) && !frame.IsClosed) {
				const CultureInfo? cultureInfo = null;
				CancellationToken cancellationToken = default;

				if (evaluationContext is null)
					evaluationContext = language.CreateContext(frame, options: DbgEvaluationContextOptions.NoMethodBody, cancellationToken: cancellationToken);
				var evalInfo = new DbgEvaluationInfo(evaluationContext, frame, cancellationToken);
				language.Formatter.FormatFrame(evalInfo, Context.ClassifiedTextWriter, Context.StackFrameFormatterOptions, Context.ValueFormatterOptions, cultureInfo);
			}
			return Context.ClassifiedTextWriter.GetClassifiedText();
		}

		protected override BreakpointKind? GetBreakpointKind() => getBreakpointKind(this);

		public override void Dispose() {
			language = null;
			frame = null!;
			evaluationContext?.Close();
			evaluationContext = null;
		}
	}

	sealed class MessageStackFrameVM : StackFrameVM {
		public override ImageReference InstructionPointerImage => DsImages.StatusError;

		readonly string message;

		public MessageStackFrameVM(string message, ICallStackContext context, int index)
			: base(context) {
			Index = index;
			this.message = message ?? throw new ArgumentNullException(nameof(message));
		}

		protected override ClassifiedTextCollection CreateName() => new ClassifiedTextCollection(new[] { new ClassifiedText(DbgTextColor.Error, message) });
	}
}
