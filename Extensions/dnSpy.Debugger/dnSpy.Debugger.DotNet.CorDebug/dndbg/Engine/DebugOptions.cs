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

using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	abstract class DebugOptionsProvider {
		public abstract CorDebugJITCompilerFlags GetDesiredNGENCompilerFlags(DnProcess process);
		public abstract ModuleLoadOptions GetModuleLoadOptions(DnModule module);
	}

	struct ModuleLoadOptions {
		public CorDebugJITCompilerFlags JITCompilerFlags;
		public bool ModuleTrackJITInfo;
		public bool ModuleAllowJitOptimizations;
		public bool JustMyCode;
	}

	sealed class DebugOptions {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
		public DebugOptionsProvider DebugOptionsProvider { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
		public CorDebugIntercept StepperInterceptMask { get; set; } = CorDebugIntercept.INTERCEPT_NONE;
		public CorDebugUnmappedStop StepperUnmappedStopMask { get; set; } = CorDebugUnmappedStop.STOP_NONE;
		public bool StepperJMC { get; set; } = false;
		public bool IgnoreBreakInstructions { get; set; } = false;
		public bool LogMessages { get; set; } = true;
		public bool ExceptionCallbacksOutsideOfMyCode { get; set; } = true;
		public CorDebugNGENPolicy NGENPolicy { get; set; } = 0;

		public DebugOptions CopyTo(DebugOptions other) {
			other.DebugOptionsProvider = DebugOptionsProvider;
			other.StepperInterceptMask = StepperInterceptMask;
			other.StepperUnmappedStopMask = StepperUnmappedStopMask;
			other.StepperJMC = StepperJMC;
			other.IgnoreBreakInstructions = IgnoreBreakInstructions;
			other.LogMessages = LogMessages;
			other.ExceptionCallbacksOutsideOfMyCode = ExceptionCallbacksOutsideOfMyCode;
			other.NGENPolicy = NGENPolicy;
			return other;
		}

		public DebugOptions Clone() => CopyTo(new DebugOptions());
	}
}
