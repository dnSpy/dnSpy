/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl : DbgEngine {
		public override DbgStartKind StartKind => DbgStartKind.Start;
		public override DbgEngineRuntimeInfo RuntimeInfo => runtimeInfo;
		public override string[] DebugTags => new[] { PredefinedDebugTags.DotNetDebugger };
		public override string[] Debugging { get; }
		public override event EventHandler<DbgEngineMessage> Message;

		readonly object lockObj;
		readonly DebuggerThread debuggerThread;
		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly DbgManager dbgManager;
		readonly DbgModuleMemoryRefreshedNotifier2 dbgModuleMemoryRefreshedNotifier;
		readonly DmdRuntime dmdRuntime;
		readonly DmdDispatcherImpl dmdDispatcher;
		readonly DbgRawMetadataService rawMetadataService;
		readonly MonoDebugRuntimeKind monoDebugRuntimeKind;
		readonly DbgEngineRuntimeInfo runtimeInfo;

		public DbgEngineImpl(DbgEngineImplDependencies deps, DbgManager dbgManager, MonoDebugRuntimeKind monoDebugRuntimeKind) {
			if (deps == null)
				throw new ArgumentNullException(nameof(deps));
			lockObj = new object();
			debuggerSettings = deps.DebuggerSettings;
			dbgDotNetCodeRangeService = deps.DotNetCodeRangeService;
			dbgDotNetCodeLocationFactory = deps.DbgDotNetCodeLocationFactory;
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			dbgModuleMemoryRefreshedNotifier = deps.DbgModuleMemoryRefreshedNotifier;
			debuggerThread = new DebuggerThread("MonoDebug");
			debuggerThread.CallDispatcherRun();
			dmdRuntime = DmdRuntimeFactory.CreateRuntime(new DmdEvaluatorImpl(this), IntPtr.Size == 4 ? DmdImageFileMachine.I386 : DmdImageFileMachine.AMD64);
			dmdDispatcher = new DmdDispatcherImpl(this);
			rawMetadataService = deps.RawMetadataService;
			this.monoDebugRuntimeKind = monoDebugRuntimeKind;
			if (monoDebugRuntimeKind == MonoDebugRuntimeKind.Mono) {
				Debugging = new[] { "MonoCLR" };
				runtimeInfo = new DbgEngineRuntimeInfo(PredefinedDbgRuntimeGuids.DotNetMono_Guid, PredefinedDbgRuntimeKindGuids.DotNet_Guid, "MonoCLR", new DotNetMonoRuntimeId(), monoRuntimeTags);
			}
			else {
				Debug.Assert(monoDebugRuntimeKind == MonoDebugRuntimeKind.Unity);
				Debugging = new[] { "Unity" };
				runtimeInfo = new DbgEngineRuntimeInfo(PredefinedDbgRuntimeGuids.DotNetUnity_Guid, PredefinedDbgRuntimeKindGuids.DotNet_Guid, "Unity", new DotNetMonoRuntimeId(), unityRuntimeTags);
			}
		}
		static readonly ReadOnlyCollection<string> monoRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNet,
			PredefinedDotNetDbgRuntimeTags.DotNetMono,
		});
		static readonly ReadOnlyCollection<string> unityRuntimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNet,
			PredefinedDotNetDbgRuntimeTags.DotNetUnity,
		});

		internal bool CheckMonoDebugThread() => debuggerThread.CheckAccess();
		internal void VerifyMonoDebugThread() => debuggerThread.VerifyAccess();
		internal T InvokeMonoDebugThread<T>(Func<T> callback) => debuggerThread.Invoke(callback);
		internal void MonoDebugThread(Action callback) => debuggerThread.BeginInvoke(callback);

		void SendMessage(DbgEngineMessage message) => Message?.Invoke(this, message);

		public override void Start(DebugProgramOptions options) {
			//TODO:
		}

		internal IDbgDotNetRuntime DotNetRuntime => internalRuntime;
		DbgMonoDebugInternalRuntimeImpl internalRuntime;
		public override DbgInternalRuntime CreateInternalRuntime(DbgRuntime runtime) {
			if (internalRuntime != null)
				throw new InvalidOperationException();
			return internalRuntime = new DbgMonoDebugInternalRuntimeImpl(this, runtime, dmdRuntime, monoDebugRuntimeKind);
		}

		public override void OnConnected(DbgObjectFactory objectFactory, DbgRuntime runtime) {
			//TODO:
		}

		public override void Break() {
			//TODO:
		}

		public override void Run() {
			//TODO:
		}

		public override void Terminate() {
			//TODO:
		}

		public override bool CanDetach => false;//TODO:

		public override void Detach() {
			//TODO:
		}

		public override DbgEngineStepper CreateStepper(DbgThread thread) {
			throw new NotImplementedException();//TODO:
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			//TODO:
		}
	}
}
