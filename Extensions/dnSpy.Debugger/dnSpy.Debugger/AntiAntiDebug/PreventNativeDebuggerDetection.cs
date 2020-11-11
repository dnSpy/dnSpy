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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.AntiAntiDebug;

namespace dnSpy.Debugger.AntiAntiDebug {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class PreventNativeDebuggerDetection : IDbgManagerStartListener {
		readonly Dictionary<Key, List<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>>> toHooks;

		readonly struct Key : IEquatable<Key> {
			readonly DbgArchitecture architecture;
			readonly DbgOperatingSystem operatingSystem;
			public Key(DbgArchitecture architecture, DbgOperatingSystem operatingSystem) {
				this.architecture = architecture;
				this.operatingSystem = operatingSystem;
			}

			public bool Equals(Key other) => architecture == other.architecture && operatingSystem == other.operatingSystem;
			public override bool Equals(object? obj) => obj is Key other && Equals(other);
			public override int GetHashCode() => ((int)architecture << 16) ^ (int)operatingSystem;
		}

		[ImportingConstructor]
		PreventNativeDebuggerDetection([ImportMany] IEnumerable<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>> dbgNativeHooks) {
			toHooks = new Dictionary<Key, List<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>>>();
			foreach (var lz in dbgNativeHooks.OrderBy(a => a.Metadata.Order)) {
				foreach (var architecture in GetArchitectures(lz.Metadata.Architectures)) {
					foreach (var operatingSystem in GetOperatingSystems(lz.Metadata.OperatingSystems)) {
						var key = new Key(architecture, operatingSystem);
						if (!toHooks.TryGetValue(key, out var list))
							toHooks.Add(key, list = new List<Lazy<IDbgNativeFunctionHook, IDbgNativeFunctionHookMetadata>>());
						list.Add(lz);
					}
				}
			}
		}

		static readonly DbgArchitecture[] allArchitectures = (DbgArchitecture[])Enum.GetValues(typeof(DbgArchitecture));
		static readonly DbgOperatingSystem[] allOperatingSystems = (DbgOperatingSystem[])Enum.GetValues(typeof(DbgOperatingSystem));

		static DbgArchitecture[] GetArchitectures(DbgArchitecture[] architectures) {
			if (architectures.Length != 0)
				return architectures;
			return allArchitectures;
		}

		static DbgOperatingSystem[] GetOperatingSystems(DbgOperatingSystem[] operatingSystems) {
			if (operatingSystems.Length != 0)
				return operatingSystems;
			return allOperatingSystems;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			if (toHooks.Count != 0)
				dbgManager.MessageProcessCreated += DbgManager_MessageProcessCreated;
		}

		void DbgManager_MessageProcessCreated(object? sender, DbgMessageProcessCreatedEventArgs e) => HookFuncs(e.Process);

		void HookFuncs(DbgProcess process) {
			var key = new Key(process.Architecture, process.OperatingSystem);
			if (!toHooks.TryGetValue(key, out var hooks))
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
					string? errorMessage = null;
					try {
						lz.Value.Hook(context, out errorMessage);
					}
					catch (DbgHookException ex) {
						errorMessage = ex.Message ?? "???";
					}
					if (errorMessage is not null)
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
