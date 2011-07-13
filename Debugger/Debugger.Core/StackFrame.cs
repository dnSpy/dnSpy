// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Debugger.MetaData;
using Debugger.Interop.CorDebug;
using Debugger.Interop.MetaData;

namespace Debugger
{
	/// <summary>
	/// A stack frame which is being executed on some thread.
	/// Use to obtain arguments or local variables.
	/// </summary>
	public class StackFrame: DebuggerObject
	{	
		Thread thread;
		AppDomain appDomain;
		Process process;
		
		ICorDebugILFrame  corILFrame;
		object            corILFramePauseSession;
		ICorDebugFunction corFunction;
		
		DebugMethodInfo methodInfo;
		uint chainIndex;
		uint frameIndex;
		
		/// <summary> The process in which this stack frame is executed </summary>
		public AppDomain AppDomain {
			get { return appDomain; }
		}
		
		public Process Process {
			get { return process; }
		}
		
		/// <summary> Get the method which this stack frame is executing </summary>
		public DebugMethodInfo MethodInfo {
			get { return methodInfo; }
		}
		
		/// <summary> A thread in which the stack frame is executed </summary>
		public Thread Thread {
			get { return thread; }
		}
		
		/// <summary> Internal index of the stack chain.  The value is increasing with age. </summary>
		public uint ChainIndex {
			get { return chainIndex; }
		}
		
		/// <summary> Internal index of the stack frame.  The value is increasing with age. </summary>
		public uint FrameIndex {
			get { return frameIndex; }
		}
		
		
		/// <summary> True if the stack frame has symbols defined. 
		/// (That is has accesss to the .pdb file) </summary>
		public bool HasSymbols {
			get {
				return GetSegmentForOffet(0) != null;
			}
		}
		
		/// <summary> Returns true is this incance can not be used any more. </summary>
		public bool IsInvalid {
			get {
				try {
					object frame = this.CorILFrame;
					return false;
				} catch (DebuggerException) {
					return true;
				}
			}
		}
		
		internal StackFrame(Thread thread, ICorDebugILFrame corILFrame, uint chainIndex, uint frameIndex)
		{
			this.process = thread.Process;
			this.thread = thread;
			this.appDomain = process.AppDomains[corILFrame.GetFunction().GetClass().GetModule().GetAssembly().GetAppDomain()];
			this.corILFrame = corILFrame;
			this.corILFramePauseSession = process.PauseSession;
			this.corFunction = corILFrame.GetFunction();
			this.chainIndex = chainIndex;
			this.frameIndex = frameIndex;
			
			MetaDataImport metaData = thread.Process.Modules[corFunction.GetClass().GetModule()].MetaData;
			int methodGenArgs = metaData.EnumGenericParams(corFunction.GetToken()).Length;
			// Class parameters are first, then the method ones
			List<ICorDebugType> corGenArgs = ((ICorDebugILFrame2)corILFrame).EnumerateTypeParameters().ToList();
			// Remove method parametrs at the end
			corGenArgs.RemoveRange(corGenArgs.Count - methodGenArgs, methodGenArgs);
			List<DebugType> genArgs = new List<DebugType>(corGenArgs.Count);
			foreach(ICorDebugType corGenArg in corGenArgs) {
				genArgs.Add(DebugType.CreateFromCorType(this.AppDomain, corGenArg));
			}
			
			DebugType debugType = DebugType.CreateFromCorClass(
				this.AppDomain,
				null,
				corFunction.GetClass(),
				genArgs.ToArray()
			);
			this.methodInfo = (DebugMethodInfo)debugType.GetMember(corFunction.GetToken());
		}
		
		/// <summary> Returns diagnostic description of the frame </summary>
		public override string ToString()
		{
			return this.MethodInfo.ToString();
		}
		
		internal ICorDebugILFrame CorILFrame {
			get {
				if (corILFramePauseSession != process.PauseSession) {
					// Reobtain the stackframe
					StackFrame stackFrame = this.Thread.GetStackFrameAt(chainIndex, frameIndex);
					if (stackFrame.MethodInfo != this.MethodInfo) throw new DebuggerException("The stack frame on the thread does not represent the same method anymore");
					corILFrame = stackFrame.corILFrame;
					corILFramePauseSession = stackFrame.corILFramePauseSession;
				}
				return corILFrame;
			}
		}
		
		[Debugger.Tests.Ignore]
		public int IP {
			get {
				uint corInstructionPtr;
				CorDebugMappingResult mappingResult;
				CorILFrame.GetIP(out corInstructionPtr, out mappingResult);
				return (int)corInstructionPtr;
			}
		}
		
		public int[] ILRanges { get; set; }
		
		public int SourceCodeLine { get; set; }
		
		SourcecodeSegment GetSegmentForOffet(int offset)
		{
			return SourcecodeSegment.ResolveForIL(this.MethodInfo.DebugModule, corFunction, SourceCodeLine, offset, ILRanges);
		}
		
		/// <summary> Step into next instruction </summary>
		public void StepInto()
		{
			AsyncStepInto();
			process.WaitForPause();
		}
		
		/// <summary> Step over next instruction </summary>
		public void StepOver()
		{
			AsyncStepOver();
			process.WaitForPause();
		}
		
		/// <summary> Step out of the stack frame </summary>
		public void StepOut()
		{
			AsyncStepOut();
			process.WaitForPause();
		}
		
		/// <summary> Step into next instruction </summary>
		public void AsyncStepInto()
		{
			AsyncStep(true);
		}
		
		/// <summary> Step over next instruction </summary>
		public void AsyncStepOver()
		{
			AsyncStep(false);
		}
		
		/// <summary> Step out of the stack frame </summary>
		public void AsyncStepOut()
		{
			Stepper.StepOut(this, "normal");
			
			AsyncContinue();
		}
		
		void AsyncStep(bool stepIn)
		{
			if (stepIn) {
				Stepper stepInStepper = Stepper.StepIn(this, ILRanges, "normal");
				this.Thread.CurrentStepIn = stepInStepper;
				Stepper clearCurrentStepIn = Stepper.StepOut(this, "clear current step in");
				clearCurrentStepIn.StepComplete += delegate {
					if (this.Thread.CurrentStepIn == stepInStepper) {
						this.Thread.CurrentStepIn = null;
					}
				};
				clearCurrentStepIn.Ignore = true;
			} else {
				Stepper.StepOver(this, ILRanges, "normal");
			}
			
			AsyncContinue();
		}
		
		void AsyncContinue()
		{
			if (process.Options.SuspendOtherThreads) {
				process.AsyncContinue(DebuggeeStateAction.Clear, new Thread[] { this.Thread }, CorDebugThreadState.THREAD_SUSPEND);
			} else {
				process.AsyncContinue(DebuggeeStateAction.Clear, this.Process.UnsuspendedThreads, CorDebugThreadState.THREAD_RUN);
			}
		}
		
		/// <summary>
		/// Get the information about the next statement to be executed.
		/// 
		/// Returns null on error.
		/// </summary>
		public SourcecodeSegment NextStatement {
			get {
				return GetSegmentForOffet(IP);
			}
		}
		
		/// <summary>
		/// Determine whether the instrustion pointer can be set to given location
		/// </summary>
		/// <returns> Best possible location. Null is not possible. </returns>
		public SourcecodeSegment CanSetIP(string filename, int line, int column)
		{
			return SetIP(true, filename, line, column);
		}
		
		/// <summary>
		/// Set the instrustion pointer to given location
		/// </summary>
		/// <returns> Best possible location. Null is not possible. </returns>
		public SourcecodeSegment SetIP(string filename, int line, int column)
		{
			return SetIP(false, filename, line, column);
		}
		
		SourcecodeSegment SetIP(bool simulate, string filename, int line, int column)
		{
			process.AssertPaused();
			
			SourcecodeSegment segment = SourcecodeSegment.Resolve(this.MethodInfo.DebugModule, filename, null, line, column);
			
			if (segment != null && segment.CorFunction.GetToken() == this.MethodInfo.MetadataToken) {
				try {
					if (simulate) {
						CorILFrame.CanSetIP((uint)segment.ILStart);
					} else {
						// Invalidates all frames and chains for the current thread
						CorILFrame.SetIP((uint)segment.ILStart);
						process.NotifyResumed(DebuggeeStateAction.Keep);
						process.NotifyPaused(PausedReason.SetIP);
						process.RaisePausedEvents();
					}
				} catch {
					return null;
				}
				return segment;
			}
			return null;
		}
		
		/// <summary> 
		/// Gets the instance of the class asociated with the current frame.
		/// That is, 'this' in C#.
		/// Note that for delegates and enumerators this returns the instance of the display class.
		/// The get the captured this, use GetLocalVariableThis.
		/// </summary>
		[Debugger.Tests.Ignore]
		public Value GetThisValue()
		{
			return new Value(appDomain, GetThisCorValue());
		}
		
		ICorDebugValue GetThisCorValue()
		{
			if (this.MethodInfo.IsStatic) throw new GetValueException("Static method does not have 'this'.");
			ICorDebugValue corValue;
			try {
				corValue = CorILFrame.GetArgument(0);
			} catch (COMException e) {
				// System.Runtime.InteropServices.COMException (0x80131304): An IL variable is not available at the current native IP. (See Forum-8640)
				if ((uint)e.ErrorCode == 0x80131304) throw new GetValueException("Not available in the current state");
				throw;
			}
			// This can be 'by ref' for value types
			if (corValue.GetTheType() == (uint)CorElementType.BYREF) {
				corValue = ((ICorDebugReferenceValue)corValue).Dereference();
			}
			return corValue;
		}
		
		/// <summary> Total number of arguments (excluding implicit 'this' argument) </summary>
		public int ArgumentCount {
			get {
				ICorDebugValueEnum argumentEnum = CorILFrame.EnumerateArguments();
				uint argCount = argumentEnum.GetCount();
				if (!this.MethodInfo.IsStatic) {
					argCount--; // Remove 'this' from count
				}
				return (int)argCount;
			}
		}
		
		/// <summary> Gets argument with a given name </summary>
		/// <returns> Null if not found </returns>
		public Value GetArgumentValue(string name)
		{
			DebugParameterInfo par = this.MethodInfo.GetParameter(name);
			if (par == null)
				return null;
			return GetArgumentValue(par.Position);
		}
		
		/// <summary> Gets argument with a given index </summary>
		/// <param name="index"> Zero-based index </param>
		public Value GetArgumentValue(int index)
		{
			return new Value(appDomain, GetArgumentCorValue(index));
		}
		
		ICorDebugValue GetArgumentCorValue(int index)
		{
			ICorDebugValue corValue;
			try {
				// Non-static methods include 'this' as first argument
				corValue = CorILFrame.GetArgument((uint)(this.MethodInfo.IsStatic? index : (index + 1)));
			} catch (COMException e) {
				if ((uint)e.ErrorCode == 0x80131304) throw new GetValueException("Unavailable in optimized code");
				throw;
			}
			// Method arguments can be passed 'by ref'
			if (corValue.GetTheType() == (uint)CorElementType.BYREF) {
				try {
					corValue = ((ICorDebugReferenceValue)corValue).Dereference();
				} catch (COMException e) {
					if ((uint)e.ErrorCode == 0x80131305) {
						// A reference value was found to be bad during dereferencing.
						// This can sometimes happen after a stack overflow
						throw new GetValueException("Bad reference");
					} else {
						throw;
					}
				}
			}
			return corValue;
		}
		
		/// <summary> Get local variable with given name </summary>
		/// <returns> Null if not found </returns>
		public Value GetLocalVariableValue(string name)
		{
			DebugLocalVariableInfo loc = this.MethodInfo.GetLocalVariable(this.IP, name);
			if (loc == null)
				return null;
			return loc.GetValue(this);
		}
		
		/// <summary> Get instance of 'this'.  It works well with delegates and enumerators. </summary>
		[Debugger.Tests.Ignore]
		public Value GetLocalVariableThis()
		{
			DebugLocalVariableInfo thisVar = this.MethodInfo.GetLocalVariableThis();
			if (thisVar != null)
				return thisVar.GetValue(this);
			return null;
		}
		
		public override bool Equals(object obj)
		{
			StackFrame other = obj as StackFrame;
			return
				other != null &&
				other.Thread == this.Thread &&
				other.ChainIndex == this.ChainIndex &&
				other.FrameIndex == this.FrameIndex &&
				other.MethodInfo == this.methodInfo;
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (thread != null) hashCode += 1000000009 * thread.GetHashCode(); 
				if (methodInfo != null) hashCode += 1000000093 * methodInfo.GetHashCode(); 
				hashCode += 1000000097 * chainIndex.GetHashCode();
				hashCode += 1000000103 * frameIndex.GetHashCode();
			}
			return hashCode;
		}
	}
}
