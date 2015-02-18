// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using Debugger.Interop.CorDebug;

namespace Debugger
{
	public class EvalCollection: CollectionWithEvents<Eval>
	{
		public EvalCollection(NDebugger debugger): base(debugger) {}
		
		internal Eval this[ICorDebugEval corEval] {
			get {
				foreach(Eval eval in this) {
					if (eval.IsCorEval(corEval)) {
						return eval;
					}
				}
				throw new DebuggerException("Eval not found for given ICorDebugEval");
			}
		}
	}
}
