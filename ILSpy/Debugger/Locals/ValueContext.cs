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
	sealed class ValueContext {
		public readonly CorFrame Frame;

		public List<CorType> GenericTypeArguments {
			get {
				if (genericTypeArguments == null)
					InitializeTypeArguments();
				return genericTypeArguments;
			}
		}

		public List<CorType> GenericMethodArguments {
			get {
				if (genericMethodArguments == null)
					InitializeTypeArguments();
				return genericMethodArguments;
			}
		}

		void InitializeTypeArguments() {
			Frame.GetTypeAndMethodGenericParameters(out genericTypeArguments, out genericMethodArguments);
		}
		List<CorType> genericTypeArguments;
		List<CorType> genericMethodArguments;

		public ValueContext(CorFrame frame) {
			this.Frame = frame;
		}
	}
}
