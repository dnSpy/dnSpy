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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Exceptions {
	enum ExceptionManagerEventType {
		Restored,
		Removed,
		Added,
		ExceptionInfoPropertyChanged,
	}

	sealed class ExceptionManagerEventArgs : EventArgs {
		public ExceptionManagerEventType EventType { get; private set; }
		public object Argument { get; private set; }

		public ExceptionManagerEventArgs(ExceptionManagerEventType eventType, object arg = null) {
			this.EventType = eventType;
			this.Argument = arg;
		}
	}

	interface IExceptionManager {
		event EventHandler<ExceptionManagerEventArgs> Changed;
		IEnumerable<ExceptionInfo> ExceptionInfos { get; }
		void RestoreDefaults();
		void Remove(ExceptionInfoKey key);
		void BreakOnFirstChanceChanged(ExceptionInfo info);
		void AddOrUpdate(ExceptionInfoKey key, bool breakOnFirstChance, bool isOtherExceptions);
		void Add(ExceptionInfoKey key);
		void RemoveExceptions(IEnumerable<ExceptionInfo> infos);
		bool CanRemove(ExceptionInfo info);
		bool Exists(ExceptionInfoKey key);
	}

	[Export, Export(typeof(IExceptionManager)), Export(typeof(ILoadBeforeDebug)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ExceptionManager : IExceptionManager, ILoadBeforeDebug {
		readonly Dictionary<ExceptionInfoKey, ExceptionInfo> exceptions = new Dictionary<ExceptionInfoKey, ExceptionInfo>();
		readonly ExceptionInfo[] otherExceptions = new ExceptionInfo[(int)ExceptionType.Last];

		public event EventHandler<ExceptionManagerEventArgs> Changed;

		public IEnumerable<ExceptionInfo> ExceptionInfos {
			get { return exceptions.Values; }
		}

		readonly ITheDebugger theDebugger;
		readonly IDefaultExceptionSettings defaultExceptionSettings;

		[ImportingConstructor]
		ExceptionManager(ITheDebugger theDebugger, IDefaultExceptionSettings defaultExceptionSettings) {
			this.theDebugger = theDebugger;
			this.defaultExceptionSettings = defaultExceptionSettings;
			RestoreDefaults();
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				dbg.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				break;

			case DebuggerProcessState.Terminated:
				dbg.DebugCallbackEvent -= DnDebugger_DebugCallbackEvent;
				break;
			}
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			if (e.Kind == DebugCallbackKind.Exception2)
				OnException((Exception2DebugCallbackEventArgs)e);
		}

		void OnException(Exception2DebugCallbackEventArgs e) {
			if (e.EventType != CorDebugExceptionCallbackType.DEBUG_EXCEPTION_FIRST_CHANCE)
				return;
			var thread = e.CorThread;
			if (thread == null)
				return;
			var exValue = thread.CurrentException;
			if (exValue == null)
				return;
			var exType = exValue.ExactType;
			if (exType == null)
				return;
			var exTypeName = exType.ToString(TypePrinterFlags.ShowNamespaces);
			var key = new ExceptionInfoKey(ExceptionType.DotNet, exTypeName);
			ExceptionInfo info;
			if (!exceptions.TryGetValue(key, out info))
				info = otherExceptions[(int)ExceptionType.DotNet];
			if (!info.BreakOnFirstChance)
				return;

			e.AddPauseReason(DebuggerPauseReason.Exception);
		}

		public void BreakOnFirstChanceChanged(ExceptionInfo info) {
			ExceptionInfo info2;
			bool b = exceptions.TryGetValue(info.Key, out info2) && ReferenceEquals(info, info2);
			Debug.Assert(b);
			if (!b)
				return;
			if (Changed != null)
				Changed(this, new ExceptionManagerEventArgs(ExceptionManagerEventType.ExceptionInfoPropertyChanged, info));
		}

		public void RestoreDefaults() {
			exceptions.Clear();
			foreach (var info in defaultExceptionSettings.ExceptionInfos) {
				Debug.Assert(!exceptions.ContainsKey(info.Key));
				exceptions[info.Key] = info;
			}
			for (int i = 0; i < (int)ExceptionType.Last; i++) {
				var info = CreateOtherExceptionInfo((ExceptionType)i);
				exceptions[info.Key] = info;
				otherExceptions[i] = info;
			}
			if (Changed != null)
				Changed(this, new ExceptionManagerEventArgs(ExceptionManagerEventType.Restored));
		}

		static ExceptionInfo CreateOtherExceptionInfo(ExceptionType type) {
			switch (type) {
			case ExceptionType.DotNet:
				return new ExceptionInfo(type, dnSpy_Debugger_Resources.Exceptions_AllCLRExceptionsNotInList);

			default:
				Debug.Fail("Unknown type");
				throw new InvalidOperationException();
			}
		}

		public void Remove(ExceptionInfoKey key) {
			ExceptionInfo info;
			if (!exceptions.TryGetValue(key, out info))
				return;
			RemoveExceptions(new ExceptionInfo[] { info });
		}

		void WriteBreakOnFirstChance(ExceptionInfo info, bool breakOnFirstChance) {
			if (info.BreakOnFirstChance == breakOnFirstChance)
				return;
			info.BreakOnFirstChance = breakOnFirstChance;
			BreakOnFirstChanceChanged(info);
		}

		public void AddOrUpdate(ExceptionInfoKey key, bool breakOnFirstChance, bool isOtherExceptions) {
			if (isOtherExceptions) {
				int index = (int)key.ExceptionType;
				if ((uint)index < (uint)otherExceptions.Length)
					WriteBreakOnFirstChance(otherExceptions[index], breakOnFirstChance);
			}
			else {
				ExceptionInfo info;
				if (exceptions.TryGetValue(key, out info))
					WriteBreakOnFirstChance(info, breakOnFirstChance);
				else {
					exceptions[key] = info = new ExceptionInfo(key, breakOnFirstChance);
					if (Changed != null)
						Changed(this, new ExceptionManagerEventArgs(ExceptionManagerEventType.Added, info));
				}
			}
		}

		public void RemoveExceptions(IEnumerable<ExceptionInfo> infos) {
			var list = new List<ExceptionInfo>();
			foreach (var info in infos) {
				if (CanRemove(info) && exceptions.Remove(info.Key))
					list.Add(info);
			}
			if (list.Count != 0) {
				if (Changed != null)
					Changed(this, new ExceptionManagerEventArgs(ExceptionManagerEventType.Removed, list));
			}
		}

		public bool CanRemove(ExceptionInfo info) {
			return !info.IsOtherExceptions;
		}

		public bool Exists(ExceptionInfoKey key) {
			return exceptions.ContainsKey(key);
		}

		public void Add(ExceptionInfoKey key) {
			ExceptionInfo info;
			if (exceptions.TryGetValue(key, out info))
				return;

			info = new ExceptionInfo(key, true);
			exceptions.Add(key, info);
			if (Changed != null)
				Changed(this, new ExceptionManagerEventArgs(ExceptionManagerEventType.Added, info));
		}
	}
}
