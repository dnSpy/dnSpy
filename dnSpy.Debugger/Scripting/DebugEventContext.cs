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

using dnSpy.Contracts.Scripting.Debugger;
using DBG = dndbg.Engine;

namespace dnSpy.Debugger.Scripting {
	abstract class DebugEventContext : IDebugEventContext {
		public DebugEventKind Kind {
			get { return eventKind; }
		}
		readonly DebugEventKind eventKind;

		protected readonly Debugger debugger;

		protected DebugEventContext(Debugger debugger, DebugEventKind eventKind) {
			this.debugger = debugger;
			this.eventKind = eventKind;
		}
	}

	sealed class ProcessEventContext : DebugEventContext, IProcessEventContext {
		public ProcessEventContext(Debugger debugger, DBG.ProcessDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
		}
	}

	sealed class ThreadEventContext : DebugEventContext, IThreadEventContext {
		readonly DBG.ThreadDebugCallbackEventArgs args;

		public ThreadEventContext(Debugger debugger, DBG.ThreadDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerThread Thread {
			get { return debugger.FindThreadUI(args.CorThread); }
		}
	}

	sealed class ModuleEventContext : DebugEventContext, IModuleEventContext {
		readonly DBG.ModuleDebugCallbackEventArgs args;

		public ModuleEventContext(Debugger debugger, DBG.ModuleDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerModule Module {
			get { return debugger.FindModuleUI(args.CorModule); }
		}
	}

	sealed class ClassEventContext : DebugEventContext, IClassEventContext {
		readonly DBG.ClassDebugCallbackEventArgs args;

		public ClassEventContext(Debugger debugger, DBG.ClassDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerClass Class {
			get {
				debugger.Dispatcher.VerifyAccess();
				var cls = args.CorClass;
				return cls == null ? null : new DebuggerClass(debugger, cls);
			}
		}
	}

	sealed class AppDomainEventContext : DebugEventContext, IAppDomainEventContext {
		readonly DBG.AppDomainDebugCallbackEventArgs args;

		public AppDomainEventContext(Debugger debugger, DBG.AppDomainDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}
	}

	sealed class AssemblyEventContext : DebugEventContext, IAssemblyEventContext {
		readonly DBG.AssemblyDebugCallbackEventArgs args;

		public AssemblyEventContext(Debugger debugger, DBG.AssemblyDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerAssembly Assembly {
			get { return debugger.FindAssemblyUI(args.CorAssembly); }
		}
	}

	sealed class LogMessageEventContext : DebugEventContext, ILogMessageEventContext {
		readonly DBG.LogMessageDebugCallbackEventArgs args;

		public LogMessageEventContext(Debugger debugger, DBG.LogMessageDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerThread Thread {
			get { return debugger.FindThreadUI(args.CorThread); }
		}

		public LoggingLevel Level {
			get {
				debugger.Dispatcher.VerifyAccess();
				return (LoggingLevel)args.Level;
			}
		}

		public string LowSwitchName {
			get {
				debugger.Dispatcher.VerifyAccess();
				return args.LowSwitchName ?? string.Empty;
			}
		}

		public string Message {
			get {
				debugger.Dispatcher.VerifyAccess();
				return args.Message ?? string.Empty;
			}
		}
	}

	sealed class LogSwitchEventContext : DebugEventContext, ILogSwitchEventContext {
		readonly DBG.LogSwitchDebugCallbackEventArgs args;

		public LogSwitchEventContext(Debugger debugger, DBG.LogSwitchDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerThread Thread {
			get { return debugger.FindThreadUI(args.CorThread); }
		}

		public LoggingLevel Level {
			get {
				debugger.Dispatcher.VerifyAccess();
				return (LoggingLevel)args.Level;
			}
		}

		public LogSwitchCallReason Reason {
			get {
				debugger.Dispatcher.VerifyAccess();
				return (LogSwitchCallReason)args.Reason;
			}
		}

		public string LowSwitchName {
			get {
				debugger.Dispatcher.VerifyAccess();
				return args.LowSwitchName ?? string.Empty;
			}
		}

		public string ParentName {
			get {
				debugger.Dispatcher.VerifyAccess();
				return args.ParentName ?? string.Empty;
			}
		}
	}

	sealed class ControlCTrapEventContext : DebugEventContext, IControlCTrapEventContext {
		public ControlCTrapEventContext(Debugger debugger, DBG.ControlCTrapDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
		}
	}

	sealed class NameChangeEventContext : DebugEventContext, INameChangeEventContext {
		readonly DBG.NameChangeDebugCallbackEventArgs args;

		public NameChangeEventContext(Debugger debugger, DBG.NameChangeDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerThread Thread {
			get { return debugger.FindThreadUI(args.CorThread); }
		}
	}

	sealed class UpdateModuleSymbolsEventContext : DebugEventContext, IUpdateModuleSymbolsEventContext {
		readonly DBG.UpdateModuleSymbolsDebugCallbackEventArgs args;

		public UpdateModuleSymbolsEventContext(Debugger debugger, DBG.UpdateModuleSymbolsDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerModule Module {
			get { return debugger.FindModuleUI(args.CorModule); }
		}
	}

	sealed class MDANotificationEventContext : DebugEventContext, IMDANotificationEventContext {
		readonly DBG.MDANotificationDebugCallbackEventArgs args;
		readonly DBG.CorMDA mda;

		public MDANotificationEventContext(Debugger debugger, DBG.MDANotificationDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
			this.mda = e.CorMDA;
		}

		public IDebuggerThread Thread {
			get { return debugger.FindThreadUI(args.CorThread); }
		}

		public bool ThreadSlipped {
			get {
				debugger.Dispatcher.VerifyAccess();
				return mda.ThreadSlipped;
			}
		}

		public MDAFlags Flags {
			get {
				debugger.Dispatcher.VerifyAccess();
				return (MDAFlags)mda.Flags;
			}
		}

		public uint OSThreadId {
			get {
				debugger.Dispatcher.VerifyAccess();
				return mda.OSThreadId;
			}
		}

		public string Name {
			get {
				debugger.Dispatcher.VerifyAccess();
				return mda.Name ?? string.Empty;
			}
		}

		public string Description {
			get {
				debugger.Dispatcher.VerifyAccess();
				return mda.Description ?? string.Empty;
			}
		}

		public string XML {
			get {
				debugger.Dispatcher.VerifyAccess();
				return mda.XML ?? string.Empty;
			}
		}
	}

	sealed class CustomNotificationEventContext : DebugEventContext, ICustomNotificationEventContext {
		readonly DBG.CustomNotificationDebugCallbackEventArgs args;

		public CustomNotificationEventContext(Debugger debugger, DBG.CustomNotificationDebugCallbackEventArgs e)
			: base(debugger, e.Kind.ToDebugEventKind()) {
			this.args = e;
		}

		public IAppDomain AppDomain {
			get { return debugger.FindAppDomainUI(args.CorAppDomain); }
		}

		public IDebuggerThread Thread {
			get { return debugger.FindThreadUI(args.CorThread); }
		}
	}
}
