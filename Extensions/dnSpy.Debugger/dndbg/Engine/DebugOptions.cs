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

using System.ComponentModel;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	public class DebugOptions : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Stepper intercept mask
		/// </summary>
		public CorDebugIntercept StepperInterceptMask {
			get { return stepperInterceptMask; }
			set {
				if (stepperInterceptMask != value) {
					stepperInterceptMask = value;
					OnPropertyChanged(nameof(StepperInterceptMask));
				}
			}
		}
		CorDebugIntercept stepperInterceptMask = CorDebugIntercept.INTERCEPT_NONE;

		/// <summary>
		/// Stepper unmapped stop mask
		/// </summary>
		public CorDebugUnmappedStop StepperUnmappedStopMask {
			get { return stepperUnmappedStopMask; }
			set {
				if (stepperUnmappedStopMask != value) {
					stepperUnmappedStopMask = value;
					OnPropertyChanged(nameof(StepperUnmappedStopMask));
				}
			}
		}
		CorDebugUnmappedStop stepperUnmappedStopMask = CorDebugUnmappedStop.STOP_NONE;

		/// <summary>
		/// Stepper JMC (Just My Code)
		/// </summary>
		public bool StepperJMC {
			get { return stepperJMC; }
			set {
				if (stepperJMC != value) {
					stepperJMC = value;
					OnPropertyChanged(nameof(StepperJMC));
				}
			}
		}
		bool stepperJMC = false;

		/// <summary>
		/// Passed to ICorDebugProcess2::SetDesiredNGENCompilerFlags() and ICorDebugModule2::SetJITCompilerFlags()
		/// </summary>
		public CorDebugJITCompilerFlags JITCompilerFlags {
			get { return jitCompilerFlags; }
			set {
				if (jitCompilerFlags != value) {
					jitCompilerFlags = value;
					OnPropertyChanged(nameof(JITCompilerFlags));
				}
			}
		}
		CorDebugJITCompilerFlags jitCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;

		/// <summary>
		/// Passed to ICorDebugModule::EnableJITDebugging()
		/// </summary>
		public bool ModuleTrackJITInfo {
			get { return moduleTrackJITInfo; }
			set {
				if (moduleTrackJITInfo != value) {
					moduleTrackJITInfo = value;
					OnPropertyChanged(nameof(ModuleTrackJITInfo));
				}
			}
		}
		bool moduleTrackJITInfo = true;

		/// <summary>
		/// Passed to ICorDebugModule::EnableJITDebugging()
		/// </summary>
		public bool ModuleAllowJitOptimizations {
			get { return moduleAllowJitOptimizations; }
			set {
				if (moduleAllowJitOptimizations != value) {
					moduleAllowJitOptimizations = value;
					OnPropertyChanged(nameof(ModuleAllowJitOptimizations));
				}
			}
		}
		bool moduleAllowJitOptimizations = true;

		/// <summary>
		/// Passed to ICorDebugModule::EnableClassLoadCallbacks()
		/// </summary>
		public bool ModuleClassLoadCallbacks {
			get { return moduleClassLoadCallbacks; }
			set {
				if (moduleClassLoadCallbacks != value) {
					moduleClassLoadCallbacks = value;
					OnPropertyChanged(nameof(ModuleClassLoadCallbacks));
				}
			}
		}
		bool moduleClassLoadCallbacks = false;

		/// <summary>
		/// true if 'break' IL instructions are ignored when executed
		/// </summary>
		public bool IgnoreBreakInstructions {
			get { return ignoreBreakInstructions; }
			set {
				if (ignoreBreakInstructions != value) {
					ignoreBreakInstructions = value;
					OnPropertyChanged(nameof(IgnoreBreakInstructions));
				}
			}
		}
		bool ignoreBreakInstructions = false;

		/// <summary>
		/// Passed to ICorDebugProcess::EnableLogMessages
		/// </summary>
		public bool LogMessages {
			get { return logMessages; }
			set {
				if (logMessages != value) {
					logMessages = value;
					OnPropertyChanged(nameof(LogMessages));
				}
			}
		}
		bool logMessages = true;

		/// <summary>
		/// Passed to ICorDebugProcess8::EnableExceptionCallbacksOutsideOfMyCode
		/// </summary>
		public bool ExceptionCallbacksOutsideOfMyCode {
			get { return exceptionCallbacksOutsideOfMyCode; }
			set {
				if (exceptionCallbacksOutsideOfMyCode != value) {
					exceptionCallbacksOutsideOfMyCode = value;
					OnPropertyChanged(nameof(ExceptionCallbacksOutsideOfMyCode));
				}
			}
		}
		bool exceptionCallbacksOutsideOfMyCode = true;

		/// <summary>
		/// Passed to ICorDebugProcess5::EnableNGENPolicy 
		/// </summary>
		public CorDebugNGENPolicy NGENPolicy {
			get { return ngenPolicy; }
			set {
				if (ngenPolicy != value) {
					ngenPolicy = value;
					OnPropertyChanged(nameof(NGENPolicy));
				}
			}
		}
		CorDebugNGENPolicy ngenPolicy;

		public DebugOptions CopyTo(DebugOptions other) {
			other.StepperInterceptMask = StepperInterceptMask;
			other.StepperUnmappedStopMask = StepperUnmappedStopMask;
			other.StepperJMC = StepperJMC;
			other.JITCompilerFlags = JITCompilerFlags;
			other.ModuleTrackJITInfo = ModuleTrackJITInfo;
			other.ModuleAllowJitOptimizations = ModuleAllowJitOptimizations;
			other.ModuleClassLoadCallbacks = ModuleClassLoadCallbacks;
			other.IgnoreBreakInstructions = IgnoreBreakInstructions;
			other.LogMessages = LogMessages;
			other.ExceptionCallbacksOutsideOfMyCode = ExceptionCallbacksOutsideOfMyCode;
			other.NGENPolicy = NGENPolicy;
			return other;
		}

		public DebugOptions Clone() => CopyTo(new DebugOptions());
	}
}
