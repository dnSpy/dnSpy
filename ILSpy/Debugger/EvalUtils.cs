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

using System;
using dndbg.Engine;
using dndbg.Engine.COM.MetaData;

namespace dnSpy.Debugger {
	static class EvalUtils {
		public static bool ReflectionReadValue<T>(CorValue thisRef, string fieldName, ref T value) {
			if (thisRef == null)
				return false;

			var val = thisRef.GetFieldValue(fieldName);
			if (val == null)
				return false;

			var dval = val.Value;
			if (!dval.IsValueValid)
				return false;
			if (!(dval.Value is T || Equals(default(T), dval.Value)))
				return false;

			value = (T)dval.Value;
			return true;
		}

		public static T EvaluateCallMethod<T>(DnThread thread, CorValue thisObj, string methodName) {
			try {
				if (!DebuggerSettings.Instance.PropertyEvalAndFunctionCalls)
					return default(T);
				if (DebugManager.Instance.EvalDisabled)
					return default(T);
				if (thisObj == null || thisObj.IsNull)
					return default(T);
				var derefThisObj = thisObj.NeuterCheckDereferencedValue;
				if (derefThisObj == null)
					return default(T);
				var cls = derefThisObj.Class;
				if (cls == null)
					return default(T);
				var mod = cls.Module;
				if (mod == null)
					return default(T);
				var mdi = mod.GetMetaDataInterface<IMetaDataImport>();
				if (mdi == null)
					return default(T);
				uint mdToken;
				int hr = mdi.FindMethod(cls.Token, methodName, IntPtr.Zero, 0, out mdToken);
				if (hr < 0)
					return default(T);
				var func = mod.GetFunctionFromToken(mdToken);
				if (func == null)
					return default(T);

				CorValueResult res;
				using (var eval = DebugManager.Instance.CreateEval(thread.CorThread))
					res = eval.CallResult(func, null, new CorValue[] { thisObj }, out hr);
				if (hr < 0)
					return default(T);
				return (T)res.Value;
			}
			catch {
			}
			return default(T);
		}
	}
}
