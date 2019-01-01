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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation.Hooks {
	sealed class System_String : ClassHook {
		readonly DbgEngineImpl engine;
		readonly IMonoDebugRuntime runtime;

		public System_String(DbgEngineImpl engine, IMonoDebugRuntime runtime) {
			this.engine = engine;
			this.runtime = runtime;
		}

		AppDomainMirror GetMonoAppDomain(DmdMemberInfo member) => DbgMonoDebugInternalAppDomainImpl.GetAppDomainMirror(member.AppDomain);

		public override DbgDotNetValue CreateInstance(DmdConstructorInfo ctor, object[] arguments) {
			var appDomain = ctor.AppDomain;
			var ps = ctor.GetMethodSignature().GetParameterTypes();
			ValueLocation valueLocation;
			switch (ps.Count) {
			case 1:
				// String(char* value)
				if (ps[0].IsPointer && ps[0].GetElementType() == appDomain.System_Char) {
					//TODO:
					break;
				}
				// String(sbyte* value)
				if (ps[0].IsPointer && ps[0].GetElementType() == appDomain.System_SByte) {
					//TODO:
					break;
				}
				// String(char[] value)
				if (ps[0].IsSZArray && ps[0].GetElementType() == appDomain.System_Char) {
					var value = runtime.ValueConverter.ToCharArray(arguments[0]);
					var s = new string(value);
					valueLocation = new NoValueLocation(appDomain.System_String, MonoValueFactory.TryCreateSyntheticValue(engine, GetMonoAppDomain(ctor), appDomain.System_String, s));
					return engine.CreateDotNetValue_MonoDebug(valueLocation);
				}
				break;

			case 2:
				// String(char c, int count)
				if (ps[0] == appDomain.System_Char && ps[1] == appDomain.System_Int32) {
					char c = runtime.ValueConverter.ToChar(arguments[0]);
					int count = runtime.ValueConverter.ToInt32(arguments[1]);
					var s = new string(c, count);
					valueLocation = new NoValueLocation(appDomain.System_String, MonoValueFactory.TryCreateSyntheticValue(engine, GetMonoAppDomain(ctor), appDomain.System_String, s));
					return engine.CreateDotNetValue_MonoDebug(valueLocation);
				}
				break;

			case 3:
				// String(char* value, int startIndex, int length)
				if (ps[0].IsPointer && ps[0].GetElementType() == appDomain.System_Char && ps[1] == appDomain.System_Int32 && ps[2] == appDomain.System_Int32) {
					//TODO:
					break;
				}
				// String(sbyte* value, int startIndex, int length)
				if (ps[0].IsPointer && ps[0].GetElementType() == appDomain.System_SByte && ps[1] == appDomain.System_Int32 && ps[2] == appDomain.System_Int32) {
					//TODO:
					break;
				}
				// String(char[] value, int startIndex, int length)
				if (ps[0].IsSZArray && ps[0].GetElementType() == appDomain.System_Char && ps[1] == appDomain.System_Int32 && ps[2] == appDomain.System_Int32) {
					var value = runtime.ValueConverter.ToCharArray(arguments[0]);
					int startIndex = runtime.ValueConverter.ToInt32(arguments[1]);
					int length = runtime.ValueConverter.ToInt32(arguments[2]);
					var s = new string(value, startIndex, length);
					valueLocation = new NoValueLocation(appDomain.System_String, MonoValueFactory.TryCreateSyntheticValue(engine, GetMonoAppDomain(ctor), appDomain.System_String, s));
					return engine.CreateDotNetValue_MonoDebug(valueLocation);
				}
				break;

			case 4:
				// String(sbyte* value, int startIndex, int length, Encoding enc)
				if (ps[0].IsPointer && ps[0].GetElementType() == appDomain.System_SByte && ps[1] == appDomain.System_Int32 &&
					ps[2] == appDomain.System_Int32 && ps[3] == appDomain.GetWellKnownType(DmdWellKnownType.System_Text_Encoding, isOptional: true)) {
					//TODO:
					break;
				}
				break;
			}

			return null;
		}
	}
}
