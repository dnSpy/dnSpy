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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.SaveModule {
	enum ModuleSaverLogEvent {
		Error,
		Warning,
		Other,
	}

	sealed class ModuleSaverLogEventArgs : EventArgs {
		public string Message { get; }
		public ModuleSaverLogEvent Event { get; }

		public ModuleSaverLogEventArgs(string msg, ModuleSaverLogEvent evType) {
			Message = msg;
			Event = evType;
		}
	}

	sealed class ModuleSaverWriteEventArgs : EventArgs {
		public SaveOptionsVM File { get; }
		public bool Starting { get; }

		public ModuleSaverWriteEventArgs(SaveOptionsVM vm, bool starting) {
			File = vm;
			Starting = starting;
		}
	}

	sealed class ModuleSaver : ILogger {
		SaveState[] filesToSave;

		sealed class SaveState {
			public readonly SaveOptionsVM File;
			public double SizeRatio;

			public SaveState(SaveOptionsVM vm) => File = vm;
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
		FileProgress? fileProgress;

		double GetFileProgress() => fileProgress!.Progress;

		abstract class FileProgress {
			public abstract double Progress { get; }
		}

		sealed class ModuleFileProgress : FileProgress {
			public double CurrentProgress;
			public override double Progress => CurrentProgress;
		}

		sealed class HexFileProgress : FileProgress {
			public ulong TotalSize;
			public ulong BytesWritten;

			public override double Progress => (double)BytesWritten / TotalSize;

			public HexFileProgress(ulong totalSize) => TotalSize = totalSize;
		}

		public event EventHandler? OnProgressUpdated;
		public event EventHandler<ModuleSaverWriteEventArgs>? OnWritingFile;
		public event EventHandler<ModuleSaverLogEventArgs>? OnLogMessage;

		public ModuleSaver(IEnumerable<SaveOptionsVM> moduleVms) {
			filesToSave = moduleVms.Select(a => new SaveState(a)).ToArray();
			var totalSize = filesToSave.Sum(a => GetSize(a.File));
			if (totalSize == 0)
				totalSize = 1;
			foreach (var state in filesToSave)
				state.SizeRatio = GetSize(state.File) / totalSize;
		}

		static ulong GetSize(HexPosition start, HexPosition end) => start == 0 && end == new HexPosition(ulong.MaxValue) + 1 ? ulong.MaxValue : (end - start).ToUInt64();

		static double GetSize(SaveOptionsVM vm) {
			switch (vm.Type) {
			case SaveOptionsType.Module:
				return ((SaveModuleOptionsVM)vm).Module.Types.Count;

			case SaveOptionsType.Hex:
				var hex = (SaveHexOptionsVM)vm;
				ulong size = GetSize(hex.Buffer.Span.Start, hex.Buffer.Span.End);
				const double m = 1.0;
				const double sizediv = 10 * 1024 * 1024;
				return m * (size / sizediv);

			default: throw new InvalidOperationException();
			}
		}

		public void SaveAll() {
			mustCancel = false;
			byte[]? buffer = null;
			for (int i = 0; i < filesToSave.Length; i++) {
				fileIndex = i;
				var state = filesToSave[fileIndex];
				OnWritingFile?.Invoke(this, new ModuleSaverWriteEventArgs(state.File, true));

				fileProgress = null;
				switch (state.File.Type) {
				case SaveOptionsType.Module:	Save((SaveModuleOptionsVM)state.File); break;
				case SaveOptionsType.Hex:		Save((SaveHexOptionsVM)state.File, ref buffer); break;
				default:						throw new InvalidOperationException();
				}
				fileProgress = null;
				if (!StringComparer.OrdinalIgnoreCase.Equals(state.File.OriginalFileName, state.File.FileName))
					SaveAppConfig(state.File.OriginalFileName!, state.File.FileName);

				OnWritingFile?.Invoke(this, new ModuleSaverWriteEventArgs(state.File, false));
			}

			fileIndex = filesToSave.Length;
			OnProgressUpdated?.Invoke(this, EventArgs.Empty);
		}

		void SaveAppConfig(string origFilename, string newFilename) {
			var origAppConfig = origFilename + ".config";
			var newAppConfig = newFilename + ".config";
			if (StringComparer.OrdinalIgnoreCase.Equals(origAppConfig, newAppConfig))
				return;
			if (!File.Exists(origAppConfig))
				return;
			if (File.Exists(newAppConfig))
				File.Delete(newAppConfig);
			File.Copy(origAppConfig, newAppConfig);
		}

		void Save(SaveModuleOptionsVM vm) {
			fileProgress = new ModuleFileProgress();
			var opts = vm.CreateWriterOptions();
			opts.ProgressUpdated += ModuleWriter_ProgressUpdated;
			opts.Logger = this;
			// Make sure the order of the interfaces don't change, see https://github.com/dotnet/roslyn/issues/3905
			opts.MetadataOptions.Flags |= MetadataFlags.RoslynSortInterfaceImpl;
			var filename = vm.FileName;
			if (opts is NativeModuleWriterOptions)
				((ModuleDefMD)vm.Module).NativeWrite(filename, (NativeModuleWriterOptions)opts);
			else
				vm.Module.Write(filename, (ModuleWriterOptions)opts);
		}

		void Save(SaveHexOptionsVM hex, ref byte[]? buffer) {
			var progress = new HexFileProgress(GetSize(hex.Buffer.Span.Start, hex.Buffer.Span.End));
			fileProgress = progress;
			if (buffer is null)
				buffer = new byte[64 * 1024];

			try {
				if (File.Exists(hex.FileName))
					File.Delete(hex.FileName);
				using (var stream = File.OpenWrite(hex.FileName)) {
					var pos = hex.Buffer.Span.Start;
					var end = hex.Buffer.Span.End;
					while (pos < end) {
						ThrowIfCanceled();

						ulong bytesLeft = GetSize(pos, end);
						int bytesToWrite = bytesLeft > (ulong)buffer.Length ? buffer.Length : (int)bytesLeft;
						hex.Buffer.ReadBytes(pos, buffer, 0, bytesToWrite);
						stream.Write(buffer, 0, bytesToWrite);
						progress.BytesWritten += (ulong)bytesToWrite;
						NotifyProgressUpdated();

						pos = pos + (ulong)bytesToWrite;
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

		void NotifyProgressUpdated() => OnProgressUpdated?.Invoke(this, EventArgs.Empty);

		void ModuleWriter_ProgressUpdated(object? sender, ModuleWriterProgressEventArgs e) {
			ThrowIfCanceled();
			((ModuleFileProgress)fileProgress!).CurrentProgress = e.Progress;
			NotifyProgressUpdated();
		}

		void ILogger.Log(object? sender, LoggerEvent loggerEvent, string format, params object[] args) {
			ThrowIfCanceled();
			if (OnLogMessage is not null) {
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

		public void CancelAsync() => mustCancel = true;
		volatile bool mustCancel;

		void ThrowIfCanceled() {
			if (mustCancel)
				throw new TaskCanceledException();
		}
	}
}
