/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Writer;

namespace ICSharpCode.ILSpy.AsmEditor.SaveModule
{
	enum ModuleSaverLogEvent
	{
		Error,
		Warning,
		Other,
	}

	sealed class ModuleSaverLogEventArgs : EventArgs
	{
		public string Message { get; private set; }
		public ModuleSaverLogEvent Event { get; private set; }

		public ModuleSaverLogEventArgs(string msg, ModuleSaverLogEvent evType)
		{
			this.Message = msg;
			this.Event = evType;
		}
	}

	sealed class ModuleSaverWriteEventArgs : EventArgs
	{
		public SaveModuleOptionsVM File { get; private set; }
		public bool Starting { get; private set; }

		public ModuleSaverWriteEventArgs(SaveModuleOptionsVM vm, bool starting)
		{
			this.File = vm;
			this.Starting = starting;
		}
	}

	sealed class ModuleSaver : IModuleWriterListener, ILogger
	{
		SaveState[] filesToSave;
		long totalSize;

		class SaveState
		{
			public readonly SaveModuleOptionsVM File;
			public double SizeRatio;

			public SaveState(SaveModuleOptionsVM vm)
			{
				this.File = vm;
			}
		}

		public double TotalProgress {
			get {
				var index = fileIndex;
				if (index >= filesToSave.Length)
					return 1.0;

				double d = 0;
				for (int i = 0; i < index; i++)
					d += filesToSave[i].SizeRatio;
				d += filesToSave[index].SizeRatio * eventIndexToCompleted[currentEventIndex];
				return d;
			}
		}

		public double CurrentFileProgress {
			get {
				if (fileIndex >= filesToSave.Length)
					return 1.0;
				return eventIndexToCompleted[currentEventIndex];
			}
		}
		int fileIndex;
		int currentEventIndex;

		static double[] eventIndexToCompleted = new double[ModuleWriterEvent.End - ModuleWriterEvent.Begin + 1] {
			0.00054765546805657849,
			0.00081204086642871977,
			0.002171737200914018,
			0.002190621872226314,
			0.00220950654353861,
			0.003814703605083754,
			0.0039657809755821206,
			0.043850206787150875,
			0.070175438596491238,
			0.093101429569618352,
			0.11487545559269542,
			0.13938775895605537,
			0.13989764508148736,
			0.1959662342076936,
			0.23590731403319926,
			0.27477196759390404,
			0.32120937435083946,
			0.35861990822049744,
			0.35877098559099579,
			0.36156591694521556,
			0.37055502048986838,
			0.39682359828527186,
			0.41584046229675375,
			0.43827545181576116,
			0.4560459275206315,
			0.4777632995297717,
			0.47780106887239632,
			0.47780106887239632,
			0.48127584839385873,
			0.48127584839385873,
			0.5258814420335014,
			0.59008932449530715,
			0.63284422034634491,
			0.667384284176534,
			0.70364285309614194,
			0.74031688478462043,
			0.77119332238022409,
			0.80656431174815413,
			0.843521613506317,
			0.87968575906936353,
			0.87972352841198809,
			0.88859932392876717,
			0.88859932392876717,
			0.88863709327139173,
			0.9442902196287275,
			0.9442902196287275,
			0.94780276849281453,
			0.94782165316412681,
			0.97120087624874907,
			0.97120087624874907,
			0.97684739297112555,
			0.97684739297112555,
			1,
			1,
		};

		public event EventHandler OnProgressUpdated;
		public event EventHandler<ModuleSaverWriteEventArgs> OnWritingFile;
		public event EventHandler<ModuleSaverLogEventArgs> OnLogMessage;

		public ModuleSaver(IEnumerable<SaveModuleOptionsVM> moduleVms)
		{
			this.filesToSave = moduleVms.Select(a => new SaveState(a)).ToArray();
			totalSize = filesToSave.Sum(a => a.File.Module.Types.Count);
			foreach (var state in filesToSave)
				state.SizeRatio = (double)state.File.Module.Types.Count / totalSize;
		}

		public void SaveAll()
		{
			mustCancel = false;
			for (int i = 0; i < filesToSave.Length; i++) {
				fileIndex = i;
				var state = filesToSave[fileIndex];
				var vm = state.File;

				// If the user tries to save to the same file as the module, disable mmap'd I/O so
				// we can write to the file.
				//TODO: Make sure that no other code tries to use this module, eg. background decompilation
				var mod = vm.Module as ModuleDefMD;
				if (mod != null && vm.FileName.Equals(mod.Location, StringComparison.OrdinalIgnoreCase))
					mod.MetaData.PEImage.UnsafeDisableMemoryMappedIO();

				var opts = vm.CreateWriterOptions();
				opts.Listener = this;
				opts.Logger = this;
				var filename = vm.FileName;
				if (OnWritingFile != null)
					OnWritingFile(this, new ModuleSaverWriteEventArgs(vm, true));
				if (opts is NativeModuleWriterOptions)
					((ModuleDefMD)vm.Module).NativeWrite(filename, (NativeModuleWriterOptions)opts);
				else
					vm.Module.Write(filename, (ModuleWriterOptions)opts);
				if (OnWritingFile != null)
					OnWritingFile(this, new ModuleSaverWriteEventArgs(vm, false));
			}

			fileIndex = filesToSave.Length;
			if (OnProgressUpdated != null)
				OnProgressUpdated(this, EventArgs.Empty);
		}

		void IModuleWriterListener.OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt)
		{
			ThrowIfCanceled();
			currentEventIndex = evt - ModuleWriterEvent.Begin;
			Debug.Assert(currentEventIndex >= 0);
			if (OnProgressUpdated != null)
				OnProgressUpdated(this, EventArgs.Empty);
		}

		void ILogger.Log(object sender, LoggerEvent loggerEvent, string format, params object[] args)
		{
			ThrowIfCanceled();
			if (OnLogMessage != null) {
				var evtType =
					loggerEvent == LoggerEvent.Error ? ModuleSaverLogEvent.Error :
					loggerEvent == LoggerEvent.Warning ? ModuleSaverLogEvent.Warning :
					ModuleSaverLogEvent.Other;
				OnLogMessage(this, new ModuleSaverLogEventArgs(string.Format(format, args), evtType));
			}
		}

		bool ILogger.IgnoresEvent(LoggerEvent loggerEvent)
		{
			ThrowIfCanceled();
			return false;
		}

		public void CancelAsync()
		{
			mustCancel = true;
		}
		volatile bool mustCancel;

		void ThrowIfCanceled()
		{
			if (mustCancel)
				throw new TaskCanceledException();
		}
	}
}
