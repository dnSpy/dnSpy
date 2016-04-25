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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Debug event context
	/// </summary>
	public interface IDebugEventContext {
		/// <summary>
		/// Gets the event kind
		/// </summary>
		DebugEventKind Kind { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.CreateProcess"/> and <see cref="DebugEventKind.ExitProcess"/>
	/// </summary>
	public interface IProcessEventContext : IDebugEventContext {
	}

	/// <summary>
	/// <see cref="DebugEventKind.CreateThread"/> and <see cref="DebugEventKind.ExitThread"/>
	/// </summary>
	public interface IThreadEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain or null
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the thread
		/// </summary>
		IDebuggerThread Thread { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.LoadModule"/> and <see cref="DebugEventKind.UnloadModule"/>
	/// </summary>
	public interface IModuleEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the module
		/// </summary>
		IDebuggerModule Module { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.LoadClass"/> and <see cref="DebugEventKind.UnloadClass"/>
	/// </summary>
	public interface IClassEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the class
		/// </summary>
		IDebuggerClass Class { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.CreateAppDomain"/> and <see cref="DebugEventKind.ExitAppDomain"/>
	/// </summary>
	public interface IAppDomainEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.LoadAssembly"/> and <see cref="DebugEventKind.UnloadAssembly"/>
	/// </summary>
	public interface IAssemblyEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the assembly
		/// </summary>
		IDebuggerAssembly Assembly { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.LogMessage"/>
	/// </summary>
	public interface ILogMessageEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the thread
		/// </summary>
		IDebuggerThread Thread { get; }

		/// <summary>
		/// Gets the logging level
		/// </summary>
		LoggingLevel Level { get; }

		/// <summary>
		/// Gets the low switch name
		/// </summary>
		string LowSwitchName { get; }

		/// <summary>
		/// Gets the message
		/// </summary>
		string Message { get; }
	}

	/// <summary>
	/// Logging level
	/// </summary>
	public enum LoggingLevel {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		TraceLevel0 = 0,
		TraceLevel1,
		TraceLevel2,
		TraceLevel3,
		TraceLevel4,
		StatusLevel0 = 20,
		StatusLevel1,
		StatusLevel2,
		StatusLevel3,
		StatusLevel4,
		WarningLevel = 40,
		ErrorLevel = 50,
		PanicLevel = 100
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Log switch call reason
	/// </summary>
	public enum LogSwitchCallReason {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		Create,
		Modify,
		Delete,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// <see cref="DebugEventKind.LogMessage"/>
	/// </summary>
	public interface ILogSwitchEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the thread
		/// </summary>
		IDebuggerThread Thread { get; }

		/// <summary>
		/// Gets the logging level
		/// </summary>
		LoggingLevel Level { get; }

		/// <summary>
		/// Gets the reason
		/// </summary>
		LogSwitchCallReason Reason { get; }

		/// <summary>
		/// Gets the low switch name
		/// </summary>
		string LowSwitchName { get; }

		/// <summary>
		/// Gets the parent name
		/// </summary>
		string ParentName { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.ControlCTrap"/>
	/// </summary>
	public interface IControlCTrapEventContext : IDebugEventContext {
	}

	/// <summary>
	/// <see cref="DebugEventKind.NameChange"/>
	/// </summary>
	public interface INameChangeEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain or null
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the thread or null
		/// </summary>
		IDebuggerThread Thread { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.UpdateModuleSymbols"/>
	/// </summary>
	public interface IUpdateModuleSymbolsEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the assembly
		/// </summary>
		IDebuggerModule Module { get; }

		//TODO: IStream SymbolStream { get; }
	}

	/// <summary>
	/// MDA flags
	/// </summary>
	public enum MDAFlags {
		/// <summary>
		/// The thread on which the MDA was fired has slipped since the MDA was fired.
		/// </summary>
		Slip = 2,
	}

	/// <summary>
	/// <see cref="DebugEventKind.MDANotification"/>
	/// </summary>
	public interface IMDANotificationEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the thread or null
		/// </summary>
		IDebuggerThread Thread { get; }

		/// <summary>
		/// true if the thread on which the MDA was fired has slipped since the MDA was fired.
		/// 
		/// When the call stack no longer describes where the MDA was originally raised, the thread
		/// is considered to have slipped. This is an unusual circumstance brought about by the
		/// thread's execution of an invalid operation upon exiting.
		/// </summary>
		bool ThreadSlipped { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		MDAFlags Flags { get; }

		/// <summary>
		/// Gets the OS thread ID. This could be a non-managed thread ID.
		/// </summary>
		uint OSThreadId { get; }

		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the description
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets the XML
		/// </summary>
		string XML { get; }
	}

	/// <summary>
	/// <see cref="DebugEventKind.CustomNotification"/>
	/// </summary>
	public interface ICustomNotificationEventContext : IDebugEventContext {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the thread
		/// </summary>
		IDebuggerThread Thread { get; }
	}
}
