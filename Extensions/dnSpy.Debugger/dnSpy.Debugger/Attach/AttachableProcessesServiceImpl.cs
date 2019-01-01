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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Debugger.Utilities;

namespace dnSpy.Debugger.Attach {
	[Export(typeof(AttachableProcessesService))]
	sealed class AttachableProcessesServiceImpl : AttachableProcessesService {
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<AttachProgramOptionsProviderFactory, IAttachProgramOptionsProviderFactoryMetadata>[] attachProgramOptionsProviderFactories;

		[ImportingConstructor]
		AttachableProcessesServiceImpl(Lazy<DbgManager> dbgManager, [ImportMany] IEnumerable<Lazy<AttachProgramOptionsProviderFactory, IAttachProgramOptionsProviderFactoryMetadata>> attachProgramOptionsProviderFactories) {
			this.dbgManager = dbgManager;
			this.attachProgramOptionsProviderFactories = attachProgramOptionsProviderFactories.ToArray();
		}

		public override Task<AttachableProcess[]> GetAttachableProcessesAsync(string[] processNames, int[] processIds, string[] providerNames, CancellationToken cancellationToken) {
			var helper = new Helper(dbgManager, attachProgramOptionsProviderFactories, processNames, processIds, providerNames, cancellationToken);
			return helper.Task;
		}

		sealed class Helper {
			public Task<AttachableProcess[]> Task { get; }
			readonly TaskCompletionSource<AttachableProcess[]> taskCompletionSource;
			readonly List<ProviderInfo> providerInfos;

			sealed class ProviderInfo {
				Thread Thread { get; }
				AttachProgramOptionsProvider Provider { get; }

				readonly Helper owner;
				readonly AttachProgramOptionsProviderContext providerContext;

				public ProviderInfo(Helper owner, AttachProgramOptionsProviderContext providerContext, AttachProgramOptionsProvider provider) {
					this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
					this.providerContext = providerContext ?? throw new ArgumentNullException(nameof(providerContext));
					Provider = provider ?? throw new ArgumentNullException(nameof(provider));
					Thread = new Thread(ThreadStartMethod);
					Thread.IsBackground = true;
					Thread.Name = "AttachToProcess Enumerator";
				}

				void ThreadStartMethod() {
					try {
						foreach (var options in Provider.Create(providerContext)) {
							providerContext.CancellationToken.ThrowIfCancellationRequested();
							owner.AddOptions(options);
						}
						owner.EnumeratorCompleted(this, canceled: false, ex: null);
					}
					catch (OperationCanceledException ex) when (ex.CancellationToken == providerContext.CancellationToken) {
						owner.EnumeratorCompleted(this, canceled: true, ex: null);
					}
					catch (Exception ex) {
						owner.EnumeratorCompleted(this, canceled: false, ex: ex);
					}
				}

				public void Start() => Thread.Start();
			}

			readonly object lockObj;
			readonly List<AttachableProcessImpl> result;
			readonly Lazy<DbgManager> dbgManager;
			ProcessProvider processProvider;
			Regex[] processNameRegexes;
			int[] processIds;

			public Helper(Lazy<DbgManager> dbgManager, Lazy<AttachProgramOptionsProviderFactory, IAttachProgramOptionsProviderFactoryMetadata>[] attachProgramOptionsProviderFactories, string[] processNames, int[] processIds, string[] providerNames, CancellationToken cancellationToken) {
				if (attachProgramOptionsProviderFactories == null)
					throw new ArgumentNullException(nameof(attachProgramOptionsProviderFactories));
				lockObj = new object();
				result = new List<AttachableProcessImpl>();
				providerInfos = new List<ProviderInfo>();
				processProvider = new ProcessProvider();
				this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
				processNameRegexes = (processNames ?? Array.Empty<string>()).Select(a => WildcardsUtils.CreateRegex(a)).ToArray();
				this.processIds = processIds ?? Array.Empty<int>();
				if (providerNames == null)
					providerNames = Array.Empty<string>();
				var providerContext = new AttachProgramOptionsProviderContext(processIds, IsValidProcess, cancellationToken);
				bool allFactories = providerNames.Length == 0;
				foreach (var lz in attachProgramOptionsProviderFactories) {
					if (providerNames.Length != 0 && Array.IndexOf(providerNames, lz.Metadata.Name) < 0)
						continue;
					var provider = lz.Value.Create(allFactories);
					if (provider == null)
						continue;
					providerInfos.Add(new ProviderInfo(this, providerContext, provider));
				}
				if (providerInfos.Count == 0)
					Task = System.Threading.Tasks.Task.FromResult(Array.Empty<AttachableProcess>());
				else {
					taskCompletionSource = new TaskCompletionSource<AttachableProcess[]>();
					Task = taskCompletionSource.Task;
					lock (lockObj) {
						foreach (var info in providerInfos)
							info.Start();
					}
				}
			}

			void AddOptions(AttachProgramOptions options) {
				if (options == null)
					throw new ArgumentNullException(nameof(options));
				lock (lockObj) {
					var info = AttachableProcessInfo.Create(processProvider, options);
					if (IsMatch(info))
						result.Add(new AttachableProcessImpl(dbgManager.Value, options, info));
				}
			}

			bool IsValidProcess(Process process) {
				if (!IsValidProcessId(process.Id))
					return false;
				if (processNameRegexes.Length != 0) {
					try {
						if (!IsValidProcessName(Path.GetFileName(process.MainModule.FileName)))
							return false;
					}
					catch (InvalidOperationException) {
						return false;
					}
					catch (ArgumentException) {
						return false;
					}
				}
				return true;
			}

			bool IsMatch(AttachableProcessInfo info) => IsValidProcessName(info.Name) && IsValidProcessId(info.ProcessId);
			bool IsValidProcessName(string name) => processNameRegexes.Length == 0 || processNameRegexes.Any(a => a.IsMatch(name));
			bool IsValidProcessId(int pid) => processIds.Length == 0 || Array.IndexOf(processIds, pid) >= 0;

			void EnumeratorCompleted(ProviderInfo info, bool canceled, Exception ex) {
				AttachableProcess[] attachableProcesses;
				lock (lockObj) {
					wasCanceled |= canceled;
					if (ex != null) {
						if (thrownExceptions == null)
							thrownExceptions = new List<Exception>();
						thrownExceptions.Add(ex);
					}
					bool b = providerInfos.Remove(info);
					Debug.Assert(b);
					if (providerInfos.Count == 0) {
						attachableProcesses = result.ToArray();
						result.Clear();
					}
					else
						attachableProcesses = null;
				}
				if (attachableProcesses != null) {
					if (thrownExceptions != null)
						taskCompletionSource.SetException(thrownExceptions);
					else if (wasCanceled)
						taskCompletionSource.SetCanceled();
					else
						taskCompletionSource.SetResult(attachableProcesses);
					thrownExceptions?.Clear();
					thrownExceptions = null;
					processProvider.Dispose();
					processProvider = null;
					processNameRegexes = null;
					processIds = null;
				}
			}
			bool wasCanceled;
			List<Exception> thrownExceptions;
		}
	}
}
