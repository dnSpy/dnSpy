// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using Debugger.Interop.CorDebug;

namespace Debugger
{
	enum StepperOperation {StepIn, StepOver, StepOut};
	
	class Stepper
	{
		StackFrame stackFrame;
		StepperOperation operation;
		int[] stepRanges;
		string name;
		
		ICorDebugStepper corStepper;
		
		bool ignore;
		
		public event EventHandler<StepperEventArgs> StepComplete;
		
		public ICorDebugStepper CorStepper {
			get { return corStepper; }
		}
		
		public Process Process {
			get { return stackFrame.Process; }
		}
		
		public StackFrame StackFrame {
			get { return stackFrame; }
		}
		
		public StepperOperation Operation {
			get { return operation; }
		}
		
		public int[] StepRanges {
			get { return stepRanges; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public bool Ignore {
			get { return ignore; }
			set { ignore = value; }
		}
		
		private Stepper(StackFrame stackFrame, StepperOperation operation, int[] stepRanges, string name, bool justMyCode)
		{
			this.stackFrame = stackFrame;
			this.operation = operation;
			this.stepRanges = stepRanges;
			this.name = name;
			
			this.corStepper = stackFrame.CorILFrame.CreateStepper();
			this.ignore = false;
			this.StackFrame.Process.Steppers.Add(this);
			
			if (justMyCode) {
				corStepper.SetUnmappedStopMask(CorDebugUnmappedStop.STOP_NONE);
				((ICorDebugStepper2)corStepper).SetJMC(1);
			}
		}
		
		protected internal virtual void OnStepComplete(CorDebugStepReason reason) {
			this.corStepper = null;
			if (StepComplete != null) {
				StepComplete(this, new StepperEventArgs(this, reason));
			}
		}
		
		internal bool IsCorStepper(ICorDebugStepper corStepper)
		{
			return this.corStepper == corStepper;
		}
		
		internal bool IsInStepRanges(int offset)
		{
			for(int i = 0; i < stepRanges.Length / 2; i++) {
				if (stepRanges[2*i] <= offset && offset < stepRanges[2*i + 1]) {
					return true;
				}
			}
			return false;
		}
		
		public static Stepper StepOut(StackFrame stackFrame, string name)
		{
			// JMC off - Needed for multiple events. See docs\Stepping.txt
			Stepper stepper = new Stepper(stackFrame, StepperOperation.StepOut, null, name, false);
			stepper.corStepper.StepOut();
			return stepper;
		}
		
		public static Stepper StepIn(StackFrame stackFrame, int[] stepRanges, string name)
		{
			Stepper stepper = new Stepper(stackFrame, StepperOperation.StepIn, stepRanges, name, stackFrame.Process.Options.EnableJustMyCode);
			stepper.corStepper.StepRange(true /* step in */, stepRanges);
			return stepper;
		}
		
		public static Stepper StepOver(StackFrame stackFrame, int[] stepRanges, string name)
		{
			Stepper stepper = new Stepper(stackFrame, StepperOperation.StepOver, stepRanges, name, stackFrame.Process.Options.EnableJustMyCode);
			stepper.corStepper.StepRange(false /* step over */, stepRanges);
			return stepper;
		}
		
		public override string ToString()
		{
			return string.Format("{0} from {1} name=\"{2}\"", this.Operation, this.StackFrame.ToString(), this.Name);
		}
	}
	
	[Serializable]
	class StepperEventArgs: ProcessEventArgs
	{
		Stepper stepper;
		CorDebugStepReason reason;
		
		public Stepper Stepper {
			get { return stepper; }
		}
		
		public CorDebugStepReason Reason {
			get { return reason; }
		}
		
		public StepperEventArgs(Stepper stepper, CorDebugStepReason reason): base(stepper.Process)
		{
			this.stepper = stepper;
			this.reason = reason;
		}
	}
}
