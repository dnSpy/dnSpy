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

using System.Collections.Generic;
using dndbg.Engine;

namespace dnSpy.Debugger.Locals {
	interface ILocalsOwner {
		void Refresh(NormalValueVM vm);
		bool AskUser(string msg);
	}

	sealed class ValueContext {
		/// <summary>
		/// The current frame but could be neutered if Continue() has been called
		/// </summary>
		public readonly CorFrame FrameCouldBeNeutered;
		public readonly CorFunction Function;
		public readonly DnThread Thread;
		public readonly DnProcess Process;
		public readonly ILocalsOwner LocalsOwner;

		public IList<CorType> GenericTypeArguments {
			get { return genericTypeArguments; }
		}
		readonly IList<CorType> genericTypeArguments;

		public IList<CorType> GenericMethodArguments {
			get { return genericMethodArguments; }
		}
		readonly IList<CorType> genericMethodArguments;

		public ValueContext(ILocalsOwner localsOwner, CorFrame frame, DnThread thread) {
			this.LocalsOwner = localsOwner;
			this.Thread = thread;
			this.Process = thread.Process;

			// Read everything immediately since the frame will be neutered when Continue() is called
			this.FrameCouldBeNeutered = frame;
			frame.GetTypeAndMethodGenericParameters(out genericTypeArguments, out genericMethodArguments);
			this.Function = frame.Function;
		}

		public ValueContext(ILocalsOwner localsOwner, CorFrame frame, DnThread thread, IList<CorType> genericTypeArguments) {
			this.LocalsOwner = localsOwner;
			this.Thread = thread;
			this.Process = thread.Process;

			// Read everything immediately since the frame will be neutered when Continue() is called
			this.FrameCouldBeNeutered = frame;
			this.genericTypeArguments = genericTypeArguments;
			this.genericMethodArguments = new CorType[0];
			this.Function = frame.Function;
		}
	}
}
