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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter.Hooks {
	sealed class System_String : DotNetClassHook {
		readonly IDebuggerRuntime runtime;

		public System_String(IDebuggerRuntime runtime) => this.runtime = runtime;

		public override DbgDotNetValue CreateInstance(DotNetClassHookCallOptions options, DmdConstructorInfo ctor, ILValue[] arguments) {
			var appDomain = ctor.AppDomain;
			var ps = ctor.GetMethodSignature().GetParameterTypes();
			switch (ps.Count) {
			case 1:
				// String(char[] value)
				if (ps[0].IsSZArray && ps[0].GetElementType() == appDomain.System_Char) {
					//TODO:
				}
				break;

			case 2:
				// String(char c, int count)
				if (ps[0] == appDomain.System_Char && ps[1] == appDomain.System_Int32) {
					char c = runtime.ToChar(arguments[0]);
					int count = runtime.ToInt32(arguments[1]);
					return runtime.CreateValue(new string(c, count));
				}
				break;

			case 3:
				// String(char[] value, int startIndex, int length)
				if (ps[0].IsSZArray && ps[0].GetElementType() == appDomain.System_Char && ps[1] == appDomain.System_Int32 && ps[2] == appDomain.System_Int32) {
					//TODO:
				}
				break;
			}

			return null;
		}
	}
}
