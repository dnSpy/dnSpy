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
using System.ComponentModel;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Impl {
	sealed class DbgBoundCodeBreakpointImpl : DbgBoundCodeBreakpoint {
		public override DbgCodeBreakpoint Breakpoint => breakpoint;
		public override DbgRuntime Runtime => runtime;
		public override DbgModule Module => module;

		public override ulong Address {
			get {
				lock (lockObj)
					return address;
			}
		}

		public override DbgBoundCodeBreakpointMessage Message {
			get {
				lock (lockObj)
					return message;
			}
		}

		DbgDispatcher Dispatcher => Process.DbgManager.Dispatcher;

		readonly object lockObj;
		readonly DbgRuntimeImpl runtime;
		readonly DbgCodeBreakpoint breakpoint;
		DbgModule module;
		ulong address;
		DbgBoundCodeBreakpointMessage message;

		public DbgBoundCodeBreakpointImpl(DbgRuntimeImpl runtime, DbgCodeBreakpoint breakpoint, DbgModule module, ulong address, DbgBoundCodeBreakpointMessage message) {
			lockObj = new object();
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
			this.module = module;
			this.address = address;
			this.message = message;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			Dispatcher.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		internal void UpdateModule_DbgThread(DbgModule module) {
			Dispatcher.VerifyAccess();
			if (this.module != module) {
				this.module = module;
				OnPropertyChanged(nameof(Module));
			}
		}

		internal void UpdateAddress_DbgThread(ulong address) {
			Dispatcher.VerifyAccess();
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = this.address != address;
				this.address = address;
			}
			if (raiseEvent) {
				OnPropertyChanged(nameof(Address));
				OnPropertyChanged(nameof(HasAddress));
			}
		}

		internal void UpdateMessage_DbgThread(DbgBoundCodeBreakpointMessage message) {
			Dispatcher.VerifyAccess();
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = this.message != message;
				this.message = message;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(Message));
		}

		internal void Remove(DbgEngineBoundCodeBreakpointImpl[] breakpoints) => Dispatcher.BeginInvoke(() => runtime.Remove_DbgThread(breakpoints));

		protected override void CloseCore(DbgDispatcher dispatcher) => Dispatcher.VerifyAccess();
	}
}
