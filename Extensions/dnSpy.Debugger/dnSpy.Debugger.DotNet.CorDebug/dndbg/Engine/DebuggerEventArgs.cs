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
using dndbg.DotNet;

namespace dndbg.Engine {
	class DebuggerEventArgs : EventArgs {
		public new static readonly DebuggerEventArgs Empty = new DebuggerEventArgs();
	}

	sealed class ThreadDebuggerEventArgs : DebuggerEventArgs {
		public DnThread Thread { get; }
		public bool Added { get; }
		public bool ShouldPause { get; set; }

		public ThreadDebuggerEventArgs(DnThread thread, bool added) {
			Thread = thread;
			Added = added;
		}
	}

	sealed class AppDomainDebuggerEventArgs : DebuggerEventArgs {
		public DnAppDomain AppDomain { get; }
		public bool Added { get; }
		public bool ShouldPause { get; set; }

		public AppDomainDebuggerEventArgs(DnAppDomain appDomain, bool added) {
			AppDomain = appDomain;
			Added = added;
		}
	}

	sealed class AssemblyDebuggerEventArgs : DebuggerEventArgs {
		public DnAssembly Assembly { get; }
		public bool Added { get; }
		public bool ShouldPause { get; set; }

		public AssemblyDebuggerEventArgs(DnAssembly assembly, bool added) {
			Assembly = assembly;
			Added = added;
		}
	}

	sealed class ModuleDebuggerEventArgs : DebuggerEventArgs {
		public DnModule Module { get; }
		public bool Added { get; }
		public bool ShouldPause { get; set; }

		public ModuleDebuggerEventArgs(DnModule module, bool added) {
			Module = module;
			Added = added;
		}
	}

	sealed class NameChangedDebuggerEventArgs : DebuggerEventArgs {
		public DnAppDomain? AppDomain { get; }
		public DnThread? Thread { get; }

		public NameChangedDebuggerEventArgs(DnAppDomain? appDomain, DnThread? thread) {
			AppDomain = appDomain;
			Thread = thread;
		}
	}

	sealed class CorModuleDefCreatedEventArgs : DebuggerEventArgs {
		public DnModule Module { get; }
		public CorModuleDef CorModuleDef { get; }

		public CorModuleDefCreatedEventArgs(DnModule module, CorModuleDef corModuleDef) {
			Module = module;
			CorModuleDef = corModuleDef;
		}
	}

	readonly struct RedirectedOutputEventArgs {
		public string Text { get; }
		public bool IsStandardOutput { get; }
		public RedirectedOutputEventArgs(string text, bool isStandardOutput) {
			Text = text ?? throw new ArgumentNullException(nameof(text));
			IsStandardOutput = isStandardOutput;
		}
	}
}
