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
using System.Collections.Generic;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	abstract class DotNetClassHookFactory {
		public abstract IEnumerable<DotNetClassHookInfo> Create(IDebuggerRuntime runtime);
	}

	readonly struct DotNetClassHookInfo {
		public DotNetClassHook Hook { get; }
		public DmdTypeName? TypeName { get; }
		public DmdWellKnownType? WellKnownType { get; }

		public DotNetClassHookInfo(DotNetClassHook hook) {
			Hook = hook ?? throw new ArgumentNullException(nameof(hook));
			TypeName = null;
			WellKnownType = null;
		}

		public DotNetClassHookInfo(DotNetClassHook hook, DmdTypeName typeName) {
			Hook = hook ?? throw new ArgumentNullException(nameof(hook));
			TypeName = typeName;
			WellKnownType = null;
		}

		public DotNetClassHookInfo(DotNetClassHook hook, DmdWellKnownType wellKnownType) {
			Hook = hook ?? throw new ArgumentNullException(nameof(hook));
			TypeName = null;
			WellKnownType = wellKnownType;
		}
	}
}
