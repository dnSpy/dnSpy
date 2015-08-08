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

namespace dnSpy.AsmEditor.SaveModule {
	enum ModuleSaverLogEvent {
		Error,
		Warning,
		Other,
	}

	sealed class ModuleSaverLogEventArgs : EventArgs {
		public string Message { get; private set; }
		public ModuleSaverLogEvent Event { get; private set; }

		public ModuleSaverLogEventArgs(string msg, ModuleSaverLogEvent evType) {
			this.Message = msg;
			this.Event = evType;
		}
	}

	sealed class ModuleSaverWriteEventArgs : EventArgs {
		public SaveOptionsVM File { get; private set; }
		public bool Starting { get; private set; }

		public ModuleSaverWriteEventArgs(SaveOptionsVM vm, bool starting) {
			this.File = vm;
			this.Starting = starting;
		}
	}

	sealed class ModuleSaver : IModuleWriterListener, ILogger {
		SaveState[] filesToSave;

		class SaveState {
			public readonly SaveOptionsVM File;
			public double SizeRatio;

			public SaveState(SaveOptionsVM vm) {
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
				d += filesToSave[index].SizeRatio * GetFileProgress();
				return d;
			}
		}

		public double CurrentFileProgress {
			get {
				if (fileIndex >= filesToSave.Length)
					return 1.0;
				return GetFileProgress();
			}
		}
		int fileIndex;
		FileProgress fileProgress;

		double GetFileProgress() {
			return fileProgress.Progress;
		}

		abstract class FileProgress {
			public abstract double Progress { get; }
		}

		sealed class ModuleFileProgress : FileProgress {
			public int CurrentEventIndex;

			public override double Progress {
				get { return eventIndexToCompleted[CurrentEventIndex]; }
			}
		}

		sealed class HexFileProgress : FileProgress {
			public ulong TotalSize;
			public ulong BytesWritten;

			public override double Progress {
				get { return (double)BytesWritten / TotalSize; }
			}

			public HexFileProgress(ulong totalSize) {
				this.TotalSize = totalSize;
			}
		}

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

		public ModuleSaver(IEnumerable<SaveOptionsVM> moduleVms) {
			this.filesToSave = moduleVms.Select(a => new SaveState(a)).ToArray();
			var totalSize = filesToSave.Sum(a => GetSize(a.File));
			if (totalSize == 0)
				totalSize = 1;
			foreach (var state in filesToSave)
				state.SizeRatio = GetSize(state.File) / totalSize;
		}

		static ulong GetSize(ulong start, ulong end) {
			return start == 0 && end == ulong.MaxValue ? ulong.MaxValue : end - start + 1;
		}

		static double GetSize(SaveOptionsVM vm) {
			switch (vm.Type) {
			case SaveOptionsType.Module:
				return ((SaveModuleOptionsVM)vm).Module.Types.Count;

			case SaveOptionsType.Hex:
				var hex = (SaveHexOptionsVM)vm;
				ulong size = GetSize(hex.Document.StartOffset, hex.Document.EndOffset);
				const double m = 1.0;
				const double sizediv = 10 * 1024 * 1024;
				return m * (size / sizediv);

			default: throw new InvalidOperationException();
			}
		}

		public void SaveAll() {
			mustCancel = false;
			byte[] buffer = null;
			for (int i = 0; i < filesToSave.Length; i++) {
				fileIndex = i;
				var state = filesToSave[fileIndex];
				if (OnWritingFile != null)
					OnWritingFile(this, new ModuleSaverWriteEventArgs(state.File, true));

				fileProgress = null;
				switch (state.File.Type) {
				case SaveOptionsType.Module:	Save((SaveModuleOptionsVM)state.File); break;
				case SaveOptionsType.Hex:		Save((SaveHexOptionsVM)state.File, ref buffer); break;
				default:						throw new InvalidOperationException();
				}
				fileProgress = null;

				if (OnWritingFile != null)
					OnWritingFile(this, new ModuleSaverWriteEventArgs(state.File, false));
			}

			fileIndex = filesToSave.Length;
			if (OnProgressUpdated != null)
				OnProgressUpdated(this, EventArgs.Empty);
		}

		void Save(SaveModuleOptionsVM vm) {
			fileProgress = new ModuleFileProgress();
			var opts = vm.CreateWriterOptions();
			opts.Listener = this;
			opts.Logger = this;
			var filename = vm.FileName;
			if (opts is NativeModuleWriterOptions)
				((ModuleDefMD)vm.Module).NativeWrite(filename, (NativeModuleWriterOptions)opts);
			else
				vm.Module.Write(filename, (ModuleWriterOptions)opts);
		}

		void Save(SaveHexOptionsVM hex, ref byte[] buffer) {
			var progress = new HexFileProgress(GetSize(hex.Document.StartOffset, hex.Document.EndOffset));
			fileProgress = progress;
			if (buffer == null)
				buffer = new byte[64 * 1024];

			try {
				if (File.Exists(hex.FileName))
					File.Delete(hex.FileName);
				using (var stream = File.OpenWrite(hex.FileName)) {
					ulong offs = hex.Document.StartOffset;
					ulong end = hex.Document.EndOffset;
					while (offs <= end) {
						ThrowIfCanceled();

						ulong bytesLeft = GetSize(offs, end);
						int bytesToWrite = bytesLeft > (ulong)buffer.Length ? buffer.Length : (int)bytesLeft;
						hex.Document.Read(offs, buffer, 0, bytesToWrite);
						stream.Write(buffer, 0, bytesToWrite);
						progress.BytesWritten += (ulong)bytesToWrite;
						NotifyProgressUpdated();

						ulong nextOffs = offs + (ulong)bytesToWrite;
						if (nextOffs < offs)
							break;
						offs = nextOffs;
					}
				}
			}
			catch {
				DeleteFile(hex.FileName);
				throw;
			}
		}

		static void DeleteFile(string filename) {
			try {
				if (!File.Exists(filename))
					return;
				File.Delete(filename);
			}
			catch {
			}
		}

		void NotifyProgressUpdated() {
			if (OnProgressUpdated != null)
				OnProgressUpdated(this, EventArgs.Empty);
		}

		void IModuleWriterListener.OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt) {
			ThrowIfCanceled();
			((ModuleFileProgress)fileProgress).CurrentEventIndex = evt - ModuleWriterEvent.Begin;
			Debug.Assert(((ModuleFileProgress)fileProgress).CurrentEventIndex >= 0);
			NotifyProgressUpdated();
		}

		void ILogger.Log(object sender, LoggerEvent loggerEvent, string format, params object[] args) {
			ThrowIfCanceled();
			if (OnLogMessage != null) {
				var evtType =
					loggerEvent == LoggerEvent.Error ? ModuleSaverLogEvent.Error :
					loggerEvent == LoggerEvent.Warning ? ModuleSaverLogEvent.Warning :
					ModuleSaverLogEvent.Other;
				OnLogMessage(this, new ModuleSaverLogEventArgs(string.Format(format, args), evtType));
			}
		}

		bool ILogger.IgnoresEvent(LoggerEvent loggerEvent) {
			ThrowIfCanceled();
			return false;
		}

		public void CancelAsync() {
			mustCancel = true;
		}
		volatile bool mustCancel;

		void ThrowIfCanceled() {
			if (mustCancel)
				throw new TaskCanceledException();
		}
	}
}
