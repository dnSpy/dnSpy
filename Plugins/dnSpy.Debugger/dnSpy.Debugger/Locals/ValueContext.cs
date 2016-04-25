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

using System.Collections.Generic;
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Images;

namespace dnSpy.Debugger.Locals {
	interface IPrinterContext {
		IImageManager ImageManager { get; }
		bool SyntaxHighlight { get; }
		bool UseHexadecimal { get; }
		TypePrinterFlags TypePrinterFlags { get; }
	}

	sealed class PrinterContext : IPrinterContext {
		public IImageManager ImageManager { get; private set; }
		public bool SyntaxHighlight { get; set; }
		public bool UseHexadecimal { get; set; }
		public TypePrinterFlags TypePrinterFlags { get; set; }

		public PrinterContext(IImageManager imageManager) {
			this.ImageManager = imageManager;
		}
	}

	interface ILocalsOwner {
		IPrinterContext PrinterContext { get; }
		ITheDebugger TheDebugger { get; }
		bool DebuggerBrowsableAttributesCanHidePropsFields { get; }
		bool CompilerGeneratedAttributesCanHideFields { get; }
		bool PropertyEvalAndFunctionCalls { get; }
		void Refresh(NormalValueVM vm);
		bool AskUser(string msg);
		DnEval CreateEval(ValueContext context);
	}

	sealed class ValueContext {
		/// <summary>
		/// The current frame but could be neutered if Continue() has been called
		/// </summary>
		public CorFrame FrameCouldBeNeutered {
			get {
				if (frameCouldBeNeutered?.IsNeutered == true)
					frameCouldBeNeutered = Process?.FindFrame(frameCouldBeNeutered) ?? frameCouldBeNeutered;
				return frameCouldBeNeutered;
			}
			set { frameCouldBeNeutered = value; }
		}
		CorFrame frameCouldBeNeutered;
		public readonly CorFunction Function;
		public readonly DnThread Thread;
		public readonly DnProcess Process;
		public readonly ILocalsOwner LocalsOwner;

		public IList<CorType> GenericTypeArguments {
			get { return genericTypeArguments; }
		}
		readonly List<CorType> genericTypeArguments;

		public IList<CorType> GenericMethodArguments {
			get { return genericMethodArguments; }
		}
		readonly List<CorType> genericMethodArguments;

		public ValueContext(ILocalsOwner localsOwner, CorFrame frame, DnThread thread, DnProcess process) {
			this.LocalsOwner = localsOwner;
			this.Thread = thread;
			this.Process = process;
			Debug.Assert(thread == null || thread.Process == process);

			// Read everything immediately since the frame will be neutered when Continue() is called
			this.FrameCouldBeNeutered = frame;
			if (frame == null) {
				genericTypeArguments = genericMethodArguments = new List<CorType>();
				this.Function = null;
			}
			else {
				frame.GetTypeAndMethodGenericParameters(out genericTypeArguments, out genericMethodArguments);
				this.Function = frame.Function;
			}
		}

		public ValueContext(ILocalsOwner localsOwner, CorFrame frame, DnThread thread, List<CorType> genericTypeArguments) {
			this.LocalsOwner = localsOwner;
			this.Thread = thread;
			this.Process = thread.Process;

			// Read everything immediately since the frame will be neutered when Continue() is called
			this.FrameCouldBeNeutered = frame;
			this.genericTypeArguments = genericTypeArguments;
			this.genericMethodArguments = new List<CorType>();
			this.Function = frame == null ? null : frame.Function;
		}
	}
}
