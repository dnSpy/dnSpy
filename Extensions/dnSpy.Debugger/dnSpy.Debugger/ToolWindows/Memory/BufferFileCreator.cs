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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Memory {
	[Export(typeof(IProcessHexBufferProviderListener))]
	sealed class BufferFileCreator : IProcessHexBufferProviderListener {
		readonly Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory;
		readonly UIDispatcher uiDispatcher;

		[ImportingConstructor]
		BufferFileCreator(Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory, UIDispatcher uiDispatcher) {
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;
			this.uiDispatcher = uiDispatcher;
		}

		// random thread
		void IProcessHexBufferProviderListener.Initialize(ProcessHexBufferProvider processHexBufferProvider) =>
			processHexBufferProvider.HexBufferInfoCreated += ProcessHexBufferProvider_HexBufferInfoCreated;

		// UI thread
		void ProcessHexBufferProvider_HexBufferInfoCreated(object sender, HexBufferInfoCreatedEventArgs e) {
			uiDispatcher.VerifyAccess();
			new ModuleListener(hexBufferFileServiceFactory.Value, e.HexBufferInfo, uiDispatcher);
		}

		sealed class ModuleListener {
			readonly IHexBufferInfo hexBufferInfo;
			readonly HexBufferFileService hexBufferFileService;
			readonly UIDispatcher uiDispatcher;
			readonly List<DbgRuntime> runtimes;
			readonly Dictionary<HexPosition, int> moduleReferences;
			readonly HashSet<DbgModule> addedModules;
			DbgProcess process;

			// UI thread
			public ModuleListener(HexBufferFileServiceFactory hexBufferFileServiceFactory, IHexBufferInfo hexBufferInfo, UIDispatcher uiDispatcher) {
				uiDispatcher.VerifyAccess();
				this.hexBufferInfo = hexBufferInfo;
				this.uiDispatcher = uiDispatcher;
				runtimes = new List<DbgRuntime>();
				moduleReferences = new Dictionary<HexPosition, int>();
				addedModules = new HashSet<DbgModule>();
				hexBufferFileService = hexBufferFileServiceFactory.Create(hexBufferInfo.Buffer);
				hexBufferInfo.UnderlyingProcessChanged += HexBufferInfo_UnderlyingProcessChanged;
				hexBufferInfo.Buffer.Disposed += Buffer_Disposed;
				OnProcessChanged_UI();
			}

			// random thread
			void UI(Action callback) => uiDispatcher.UI(callback);

			// UI thread
			void OnProcessChanged_UI() {
				uiDispatcher.VerifyAccess();
				OnProcessChanged_UI(hexBufferInfo.Process);
			}

			// UI thread
			void OnProcessChanged_UI(DbgProcess newProcess) {
				uiDispatcher.VerifyAccess();
				if (disposed) {
					Debug.Assert(process == null);
					Debug.Assert(runtimes.Count == 0);
					Debug.Assert(moduleReferences.Count == 0);
					Debug.Assert(!hexBufferFileService.Files.Any());
					return;
				}
				if (process != null)
					process.RuntimesChanged -= Process_RuntimesChanged;
				foreach (var r in runtimes)
					r.ModulesChanged -= DbgRuntime_ModulesChanged;
				runtimes.Clear();
				moduleReferences.Clear();
				hexBufferFileService.RemoveAllFiles();
				addedModules.Clear();

				process = newProcess;
				if (newProcess != null)
					newProcess.DbgManager.Dispatcher.BeginInvoke(() => OnNewProcess_DbgThread(newProcess));
			}

			// DbgThread
			void OnNewProcess_DbgThread(DbgProcess newProcess) {
				newProcess.DbgManager.Dispatcher.VerifyAccess();
				if (process != newProcess)
					return;
				newProcess.RuntimesChanged += Process_RuntimesChanged;
				var runtimes = newProcess.Runtimes;
				UI(() => {
					if (process != newProcess)
						newProcess.RuntimesChanged -= Process_RuntimesChanged;
					else
						Process_RuntimesChanged_UI(runtimes, added: true);
				});
			}

			// DbgThread
			void Process_RuntimesChanged(object sender, DbgCollectionChangedEventArgs<DbgRuntime> e) =>
				UI(() => Process_RuntimesChanged_UI(e.Objects, e.Added));

			// UI thread
			void Process_RuntimesChanged_UI(IList<DbgRuntime> runtimes, bool added) {
				uiDispatcher.VerifyAccess();
				if (added) {
					foreach (var r in runtimes) {
						if (r.Process != process)
							continue;
						this.runtimes.Add(r);
						r.ModulesChanged += DbgRuntime_ModulesChanged;
						DbgRuntime_ModulesChanged_UI(r, r.Modules, added: true);
					}
				}
				else {
					foreach (var r in runtimes) {
						r.ModulesChanged -= DbgRuntime_ModulesChanged;
						this.runtimes.Remove(r);
					}
				}
			}

			// DbgThread
			void DbgRuntime_ModulesChanged(object sender, DbgCollectionChangedEventArgs<DbgModule> e) =>
				UI(() => DbgRuntime_ModulesChanged_UI((DbgRuntime)sender, e.Objects, e.Added));

			// UI thread
			void DbgRuntime_ModulesChanged_UI(DbgRuntime runtime, IList<DbgModule> modules, bool added) {
				uiDispatcher.VerifyAccess();
				if (runtime.Process != process)
					return;
				foreach (var module in modules) {
					if (added && addedModules.Contains(module))
						continue;
					if (!module.HasAddress)
						continue;
					var start = new HexPosition(module.Address);
					var end = start + module.Size;
					Debug.Assert(end <= HexPosition.MaxEndPosition);
					if (end > HexPosition.MaxEndPosition)
						continue;

					moduleReferences.TryGetValue(start, out int refCount);
					if (added) {
						addedModules.Add(module);
						if (refCount == 0) {
							string[] tags;
							switch (module.ImageLayout) {
							case DbgImageLayout.File:
								tags = new string[] { PredefinedBufferFileTags.FileLayout };
								break;
							case DbgImageLayout.Memory:
								tags = new string[] { PredefinedBufferFileTags.MemoryLayout };
								break;
							case DbgImageLayout.Unknown:
							default:
								tags = Array.Empty<string>();
								break;
							}
							hexBufferFileService.CreateFile(HexSpan.FromBounds(start, end), module.Name, module.Filename, tags);
						}
						refCount++;
					}
					else {
						addedModules.Remove(module);
						if (refCount == 0)
							continue;
						if (refCount == 1)
							hexBufferFileService.RemoveFiles(HexSpan.FromBounds(start, end));
						refCount--;
					}
					if (refCount == 0)
						moduleReferences.Remove(start);
					else
						moduleReferences[start] = refCount;
				}
			}

			// UI thread
			void HexBufferInfo_UnderlyingProcessChanged(object sender, EventArgs e) {
				uiDispatcher.VerifyAccess();
				OnProcessChanged_UI();
			}

			// UI thread
			void Buffer_Disposed(object sender, EventArgs e) {
				uiDispatcher.VerifyAccess();
				OnProcessChanged_UI(null);
				disposed = true;
				hexBufferInfo.UnderlyingProcessChanged -= HexBufferInfo_UnderlyingProcessChanged;
				hexBufferInfo.Buffer.Disposed -= Buffer_Disposed;
			}
			bool disposed;
		}
	}
}
