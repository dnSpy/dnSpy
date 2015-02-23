// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Debugger.Interop.CorDebug;
using ICSharpCode.ILSpy.Debugger;

namespace Debugger
{
	public class Breakpoint: DebuggerObject
	{
		NDebugger debugger;
		
		string fileName;
		byte[] checkSum;
		int    line;
		int    column;
		bool   enabled;

		protected SourcecodeSegment originalLocation;
		
		protected List<ICorDebugFunctionBreakpoint> corBreakpoints = new List<ICorDebugFunctionBreakpoint>();
		
		public event EventHandler<BreakpointEventArgs> Hit;
		public event EventHandler<BreakpointEventArgs> Set;
		
		[Debugger.Tests.Ignore]
		public NDebugger Debugger {
			get { return debugger; }
			protected set { debugger = value; }
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public byte[] CheckSum {
			get { return checkSum; }
		}
		
		public int Line {
			get { return line; }
			set { line = value; }
		}
		
		public int Column {
			get { return column; }
			protected set { column = value; }
		}
		
		public bool Enabled {
			get { return enabled; }
			set {
				enabled = value;
				foreach(ICorDebugFunctionBreakpoint corBreakpoint in corBreakpoints) {
					corBreakpoint.Activate(enabled ? 1 : 0);
				}
			}
		}
		
		public SourcecodeSegment OriginalLocation {
			get { return originalLocation; }
		}
		
		public bool IsSet {
			get {
				return corBreakpoints.Count > 0;
			}
		}
		
		protected virtual void OnHit(BreakpointEventArgs e)
		{
			if (Hit != null) {
				Hit(this, e);
			}
		}
		
		internal void NotifyHit()
		{
			OnHit(new BreakpointEventArgs(this));
			debugger.Breakpoints.OnHit(this);
		}
		
		protected virtual void OnSet(BreakpointEventArgs e)
		{
			if (Set != null) {
				Set(this, e);
			}
		}
		
		public Breakpoint(NDebugger debugger, ICorDebugFunctionBreakpoint corBreakpoint)
		{
			this.debugger = debugger;
			this.corBreakpoints.Add(corBreakpoint);
		}
		
		public Breakpoint() { }
		
		internal bool IsOwnerOf(ICorDebugBreakpoint breakpoint)
		{
			foreach(ICorDebugFunctionBreakpoint corFunBreakpoint in corBreakpoints) {
				if (((ICorDebugBreakpoint)corFunBreakpoint).Equals(breakpoint)) return true;
			}
			return false;
		}
		
		internal void Deactivate()
		{
			foreach(ICorDebugFunctionBreakpoint corBreakpoint in corBreakpoints) {
				#if DEBUG
				// Get repro
				corBreakpoint.Activate(0);
				#else
				try {
					corBreakpoint.Activate(0);
				} catch(COMException e) {
					// Sometimes happens, but we had not repro yet.
					// 0x80131301: Process was terminated.
					if ((uint)e.ErrorCode == 0x80131301)
						continue;
					throw;
				}
				#endif
			}
			corBreakpoints.Clear();
		}
		
		internal void MarkAsDeactivated()
		{
			corBreakpoints.Clear();
		}
		
		public virtual bool SetBreakpoint(Module module)
		{
			if (this.fileName == null)
				return false;
			
			SourcecodeSegment segment = SourcecodeSegment.Resolve(module, FileName, CheckSum, Line, Column);
			if (segment == null) return false;
			
			originalLocation = segment;
			
			ICorDebugFunctionBreakpoint corBreakpoint = segment.CorFunction.GetILCode().CreateBreakpoint((uint)segment.ILStart);
			corBreakpoint.Activate(enabled ? 1 : 0);
			
			corBreakpoints.Add(corBreakpoint);
			
			OnSet(new BreakpointEventArgs(this));
			
			return true;
		}
		
		/// <summary> Remove this breakpoint </summary>
		public void Remove()
		{
			debugger.Breakpoints.Remove(this);
		}
	}
	
	public class ILBreakpoint : Breakpoint
	{
		public ILBreakpoint(NDebugger debugger, int line, MethodKey methodKey, uint offset, bool enabled)
		{
			this.Debugger = debugger;
			this.Line = line;
			this.MethodKey = methodKey;
			this.ILOffset = offset;
			this.Enabled = enabled;
		}
		
		public MethodKey MethodKey { get; private set; }
		
		public uint ILOffset { get; private set; }
		
		public override bool SetBreakpoint(Module module)
		{
			Debug.Assert(MethodKey.IsSameModule(module.FullPath));
			SourcecodeSegment segment = SourcecodeSegment.CreateForIL(module, this.Line, (int)MethodKey.Token, (int)ILOffset);
			if (segment == null)
				return false;
			try {
				ICorDebugFunctionBreakpoint corBreakpoint = segment.CorFunction.GetILCode().CreateBreakpoint((uint)segment.ILStart);
				corBreakpoint.Activate(Enabled ? 1 : 0);
				
				corBreakpoints.Add(corBreakpoint);
				
				OnSet(new BreakpointEventArgs(this));
				
				return true;
			} catch {
				return false;
			}
		}
	}
	
	[Serializable]
	public class BreakpointEventArgs : DebuggerEventArgs
	{
		Breakpoint breakpoint;
		
		public Breakpoint Breakpoint {
			get {
				return breakpoint;
			}
		}
		
		public BreakpointEventArgs(Breakpoint breakpoint): base(breakpoint.Debugger)
		{
			this.breakpoint = breakpoint;
		}
	}
}
