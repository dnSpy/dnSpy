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

using System;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Decompiler;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Steppers.Engine {
	static class CompilerUtils {
		public static bool IsIgnoredIteratorStateMachineMethod(MethodDef method) {
			var declType = method.DeclaringType;
			if (declType.DeclaringType is null)
				return false;
			if (!declType.IsDefined(utf8System_Runtime_CompilerServices, utf8CompilerGeneratedAttribute))
				return false;

			bool result = false;
			foreach (var ii in declType.Interfaces) {
				if (ii.Interface.FullName == "System.Collections.IEnumerator") {
					result = true;
					break;
				}
			}
			if (!result)
				return false;

			if (method.IsVirtual) {
				// Ignore everything except MoveNext
				if (method.Name == utf8MoveNext)
					return false;
				foreach (var ovr in method.Overrides) {
					if (ovr.MethodDeclaration.Name == utf8MoveNext)
						return false;
				}
			}
			return true;
		}
		static readonly UTF8String utf8MoveNext = new UTF8String("MoveNext");
		static readonly UTF8String utf8System_Runtime_CompilerServices = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String utf8CompilerGeneratedAttribute = new UTF8String("CompilerGeneratedAttribute");

		// The compiler adds a new instance method to the class if a base member is accessed using 'base.XXX'
		// from inside an iterator or async method. That method just calls the real base method.
		public static bool IsBaseWrapperMethod(DbgModule module, uint token) {
			var reflectionModule = module.GetReflectionModule();
			Debug2.Assert(!(reflectionModule is null));
			if (reflectionModule is null)
				return false;
			var method = reflectionModule.ResolveMethod((int)token, DmdResolveOptions.None);
			Debug2.Assert(!(method is null));
			if (method is null)
				return false;

			if (!method.IsPrivate || method.IsStatic || method.IsAbstract || method.IsVirtual)
				return false;
			var name = method.Name;
			if (string.IsNullOrEmpty(name))
				return false;

			bool okName = false;
			var c = name[0];
			if (c == '<') {
				// Roslyn C#, eg. "<>n__2"
				if (name.StartsWith("<>n__", StringComparison.Ordinal))
					okName = true;
				// mcs, eg. "<GetString>__BaseCallProxy1"
				else if (name.IndexOf(">__BaseCallProxy", StringComparison.Ordinal) >= 0)
					okName = true;
			}
			// VB, eg. "$VB$ClosureStub_GetString_MyBase"
			else if (c == '$' && name.StartsWith("$VB$ClosureStub_", StringComparison.Ordinal) && name.EndsWith("_MyBase", StringComparison.Ordinal))
				okName = true;

			if (!okName)
				return false;

			return method.IsDefined("System.Runtime.CompilerServices.CompilerGeneratedAttribute", inherit: false);
		}
	}
}
