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
using System.Threading;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.Dialogs.AttachToProcess {
	[Export(typeof(AttachProgramOptionsAggregatorFactory))]
	sealed class AttachProgramOptionsAggregatorFactoryImpl : AttachProgramOptionsAggregatorFactory {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<AttachProgramOptionsProviderFactory, IAttachProgramOptionsProviderFactoryMetadata>[] attachProgramOptionsProviderFactories;

		[ImportingConstructor]
		AttachProgramOptionsAggregatorFactoryImpl(UIDispatcher uiDispatcher, [ImportMany] IEnumerable<Lazy<AttachProgramOptionsProviderFactory, IAttachProgramOptionsProviderFactoryMetadata>> attachProgramOptionsProviderFactories) {
			this.uiDispatcher = uiDispatcher;
			this.attachProgramOptionsProviderFactories = attachProgramOptionsProviderFactories.ToArray();
		}

		public override AttachProgramOptionsAggregator Create(string[] providerNames) =>
			new AttachProgramOptionsAggregatorImpl(uiDispatcher, attachProgramOptionsProviderFactories, providerNames);
	}

	sealed class AttachProgramOptionsAggregatorImpl : AttachProgramOptionsAggregator {
		public override event EventHandler<AttachProgramOptionsAddedEventArgs> AttachProgramOptionsAdded;
		public override event EventHandler Completed;

		readonly object lockObj;
		readonly List<AttachProgramOptions> pendingOptions;
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<AttachProgramOptionsProviderFactory, IAttachProgramOptionsProviderFactoryMetadata>[] attachProgramOptionsProviderFactories;
		readonly List<ProviderInfo> providerInfos;
		readonly string[] providerNames;
		CancellationTokenSource cancellationTokenSource;

		sealed class ProviderInfo {
			Thread Thread { get; }
			AttachProgramOptionsProvider Provider { get; }
			public bool Done { get; set; }

			readonly AttachProgramOptionsAggregatorImpl owner;
			readonly AttachProgramOptionsProviderContext providerContext;

			public ProviderInfo(AttachProgramOptionsAggregatorImpl owner, AttachProgramOptionsProviderContext providerContext, AttachProgramOptionsProvider provider) {
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
					owner.EnumeratorCompleted(this, success: true);
				}
				catch {
					owner.EnumeratorCompleted(this, success: false);
				}
			}

			public void Start() => Thread.Start();
		}

		public AttachProgramOptionsAggregatorImpl(UIDispatcher uiDispatcher, Lazy<AttachProgramOptionsProviderFactory, IAttachProgramOptionsProviderFactoryMetadata>[] attachProgramOptionsProviderFactories, string[] providerNames) {
			this.uiDispatcher = uiDispatcher;
			this.attachProgramOptionsProviderFactories = attachProgramOptionsProviderFactories ?? throw new ArgumentNullException(nameof(attachProgramOptionsProviderFactories));
			this.providerNames = providerNames;
			lockObj = new object();
			pendingOptions = new List<AttachProgramOptions>();
			providerInfos = new List<ProviderInfo>(attachProgramOptionsProviderFactories.Length);
			cancellationTokenSource = new CancellationTokenSource();
		}

		void AddOptions(AttachProgramOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			bool start;
			lock (lockObj) {
				if (disposed)
					return;
				pendingOptions.Add(options);
				start = !emptyQueueCalled && pendingOptions.Count == 1;
				emptyQueueCalled |= start;
			}
			if (start)
				uiDispatcher.UIBackground(EmptyQueue);
		}
		bool emptyQueueCalled;

		void EnumeratorCompleted(ProviderInfo info, bool success) {
			lock (lockObj) {
				if (disposed)
					return;
				checkDone = true;
				info.Done = true;
				if (emptyQueueCalled)
					return;
				emptyQueueCalled = true;
			}
			uiDispatcher.UIBackground(EmptyQueue);
		}

		void EmptyQueue() {
			uiDispatcher.VerifyAccess();
			AttachProgramOptions[] newOptions;
			bool completed;
			lock (lockObj) {
				Debug.Assert(emptyQueueCalled);
				newOptions = pendingOptions.ToArray();
				pendingOptions.Clear();
				if (checkDone) {
					checkDone = false;
					for (int i = providerInfos.Count - 1; i >= 0; i--) {
						if (providerInfos[i].Done)
							providerInfos.RemoveAt(i);
					}
				}
				completed = !hasRaisedCompleted && providerInfos.Count == 0;
				if (disposed)
					return;
				if (hasRaisedCompleted)
					return;
				hasRaisedCompleted |= completed;
				emptyQueueCalled = false;
			}
			if (newOptions.Length > 0)
				AttachProgramOptionsAdded?.Invoke(this, new AttachProgramOptionsAddedEventArgs(newOptions));
			if (completed)
				Completed?.Invoke(this, EventArgs.Empty);
		}
		bool hasRaisedCompleted;
		bool checkDone;

		public override void Start() {
			bool completed;
			lock (lockObj) {
				if (started)
					throw new InvalidOperationException();
				if (disposed)
					return;
				started = true;
				var providerNames = this.providerNames;
				if (providerNames == null)
					providerNames = Array.Empty<string>();
				var providerContext = new AttachProgramOptionsProviderContext(cancellationTokenSource.Token);
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
					completed = true;
				else {
					completed = false;
					foreach (var info in providerInfos)
						info.Start();
				}
			}
			if (completed)
				Completed?.Invoke(this, EventArgs.Empty);
		}
		bool started;

		public override void Dispose() {
			lock (lockObj) {
				if (disposed)
					return;
				disposed = true;
				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
				cancellationTokenSource = null;
				providerInfos.Clear();
				pendingOptions.Clear();
			}
		}
		bool disposed;
	}
}
