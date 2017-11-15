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

using System.Collections.Generic;
using System.Diagnostics;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
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

		void CloseDotNetValues_MonoDebug() {
			debuggerThread.VerifyAccess();
			//TODO:
		}
	}
}
