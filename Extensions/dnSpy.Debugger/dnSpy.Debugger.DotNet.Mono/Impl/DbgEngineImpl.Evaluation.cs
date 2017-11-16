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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		internal DbgDotNetValue CreateDotNetValue_MonoDebug(Value value, DmdType slotType) {
			debuggerThread.VerifyAccess();
			if (value == null)
				return new SyntheticNullValue(slotType);

			var dnValue = new DbgDotNetValueImpl(this, value, slotType);
			lock (lockObj)
				dotNetValuesToCloseOnContinue.Add(dnValue);
			return dnValue;
		}

		void CloseDotNetValues_MonoDebug() {
			debuggerThread.VerifyAccess();
			DbgDotNetValueImpl[] valuesToClose;
			lock (lockObj) {
				valuesToClose = dotNetValuesToCloseOnContinue.Count == 0 ? Array.Empty<DbgDotNetValueImpl>() : dotNetValuesToCloseOnContinue.ToArray();
				dotNetValuesToCloseOnContinue.Clear();
			}
			foreach (var value in valuesToClose)
				value.Dispose();
		}

		bool IsEvaluating => isEvaluatingCounter > 0;
		volatile int isEvaluatingCounter;

		Value TryInvokeMethod(ThreadMirror thread, ObjectMirror obj, MethodMirror method, IList<Value> arguments, out bool timedOut) {
			debuggerThread.VerifyAccess();
			Debug.Assert(isEvaluatingCounter == 0);
			isEvaluatingCounter++;
			try {
				//TODO: This could block
				var res = obj.InvokeMethod(thread, method, arguments);
				timedOut = false;
				return res;
			}
			finally {
				isEvaluatingCounter--;
			}
		}
	}
}
