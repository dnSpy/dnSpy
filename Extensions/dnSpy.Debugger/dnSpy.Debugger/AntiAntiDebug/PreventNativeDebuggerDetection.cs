/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.AntiAntiDebug;

namespace dnSpy.Debugger.AntiAntiDebug {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class PreventNativeDebuggerDetection : IDbgManagerStartListener {
		readonly Dictionary<DbgMachine, List<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>>> toHooks;

		[ImportingConstructor]
		PreventNativeDebuggerDetection([ImportMany] IEnumerable<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>> dbgNativeHooks) {
			toHooks = new Dictionary<DbgMachine, List<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>>>();
			foreach (var lz in dbgNativeHooks.OrderBy(a => a.Metadata.Order)) {
				foreach (var machine in GetMachines(lz.Metadata.Machines)) {
					if (!toHooks.TryGetValue(machine, out var list))
						toHooks.Add(machine, list = new List<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>>());
					list.Add(lz);
				}
			}
		}

		static readonly DbgMachine[] allMachines = (DbgMachine[])Enum.GetValues(typeof(DbgMachine));

		static DbgMachine[] GetMachines(DbgMachine[] machines) {
			if (machines.Length != 0)
				return machines;
			return allMachines;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			if (toHooks.Count != 0)
				dbgManager.Message += DbgManager_Message;
		}

		void DbgManager_Message(object sender, DbgMessageEventArgs e) {
			if (e.Kind == DbgMessageKind.ProcessCreated)
				HookFuncs(((DbgMessageProcessCreatedEventArgs)e).Process);
		}

		void HookFuncs(DbgProcess process) {
			if (!toHooks.TryGetValue(process.Machine, out var hooks))
				return;

			var hookedFuncs = new HashSet<(string dll, string function)>();
			var errors = new List<string>();
			using (var context = new DbgNativeFunctionHookContextImpl(process)) {
				foreach (var lz in hooks) {
					var id = (lz.Metadata.Dll, lz.Metadata.Function);
					if (hookedFuncs.Contains(id))
						continue;
					if (!lz.Value.IsEnabled(context))
						continue;
					hookedFuncs.Add(id);
					string errorMessage = null;
					try {
						lz.Value.Hook(context, out errorMessage);
					}
					catch (DbgHookException ex) {
						errorMessage = ex.Message ?? "???";
					}
					if (errorMessage != null)
						errors.Add($"{lz.Metadata.Dll}!{lz.Metadata.Function}: {errorMessage}");
				}

				context.Write();
			}
			if (errors.Count != 0) {
				var msg = "Couldn't patch debugger detection functions:\n\t" + string.Join("\n\t", errors.ToArray());
				Debug.Fail(msg);
				process.DbgManager.WriteMessage(msg);
			}
		}
	}
}
