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

using System.ComponentModel;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public class DebugOptions : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		/// <summary>
		/// Stepper intercept mask
		/// </summary>
		public CorDebugIntercept StepperInterceptMask {
			get { return stepperInterceptMask; }
			set {
				if (stepperInterceptMask != value) {
					stepperInterceptMask = value;
					OnPropertyChanged("StepperInterceptMask");
				}
			}
		}
		CorDebugIntercept stepperInterceptMask = CorDebugIntercept.INTERCEPT_ALL;

		/// <summary>
		/// Stepper unmapped stop mask
		/// </summary>
		public CorDebugUnmappedStop StepperUnmappedStopMask {
			get { return stepperUnmappedStopMask; }
			set {
				if (stepperUnmappedStopMask != value) {
					stepperUnmappedStopMask = value;
					OnPropertyChanged("StepperUnmappedStopMask");
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
					OnPropertyChanged("StepperJMC");
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
					OnPropertyChanged("JITCompilerFlags");
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
					OnPropertyChanged("ModuleTrackJITInfo");
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
					OnPropertyChanged("ModuleAllowJitOptimizations");
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
					OnPropertyChanged("ModuleClassLoadCallbacks");
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
					OnPropertyChanged("IgnoreBreakInstructions");
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
					OnPropertyChanged("LogMessages");
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
					OnPropertyChanged("ExceptionCallbacksOutsideOfMyCode");
				}
			}
		}
		bool exceptionCallbacksOutsideOfMyCode = true;
	}
}
