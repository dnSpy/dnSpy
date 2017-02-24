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
using System.Text;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorAppDomain : COMObject<ICorDebugAppDomain>, IEquatable<CorAppDomain> {
		/// <summary>
		/// Gets the process or null
		/// </summary>
		public CorProcess Process {
			get {
				int hr = obj.GetProcess(out var process);
				return hr < 0 || process == null ? null : new CorProcess(process);
			}
		}

		/// <summary>
		/// AppDomain Id
		/// </summary>
		public int Id => id;
		readonly int id;

		/// <summary>
		/// true if the debugger is attached to the AppDomain
		/// </summary>
		public bool IsAttached {
			get {
				int hr = obj.IsAttached(out int attached);
				return hr >= 0 && attached != 0;
			}
		}

		/// <summary>
		/// true if the threads are running freely
		/// </summary>
		public bool IsRunning {
			get {
				int hr = obj.IsRunning(out int running);
				return hr >= 0 && running != 0;
			}
		}

		/// <summary>
		/// Gets all threads
		/// </summary>
		public IEnumerable<CorThread> Threads {
			get {
				int hr = obj.EnumerateThreads(out var threadEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = threadEnum.Next(1, out var thread, out uint count);
					if (hr != 0 || thread == null)
						break;
					yield return new CorThread(thread);
				}
			}
		}

		/// <summary>
		/// Gets all assemblies
		/// </summary>
		public IEnumerable<CorAssembly> Assemblies {
			get {
				int hr = obj.EnumerateAssemblies(out var assemblyEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = assemblyEnum.Next(1, out var assembly, out uint count);
					if (hr != 0 || assembly == null)
						break;
					yield return new CorAssembly(assembly);
				}
			}
		}

		/// <summary>
		/// Gets all steppers
		/// </summary>
		public IEnumerable<CorStepper> Steppers {
			get {
				int hr = obj.EnumerateSteppers(out var stepperEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = stepperEnum.Next(1, out var stepper, out uint count);
					if (hr != 0 || stepper == null)
						break;
					yield return new CorStepper(stepper);
				}
			}
		}

		/// <summary>
		/// AppDomain name
		/// </summary>
		public string Name => GetName(obj) ?? string.Empty;

		static string GetName(ICorDebugAppDomain appDomain) {
			int hr = appDomain.GetName(0, out uint cchName, null);
			if (hr < 0)
				return null;
			var sb = new StringBuilder((int)cchName);
			hr = appDomain.GetName(cchName, out cchName, sb);
			if (hr < 0)
				return null;
			return sb.ToString();
		}

		/// <summary>
		/// Gets the CLR AppDomain object or null if it hasn't been constructed yet
		/// </summary>
		public CorValue Object {
			get {
				int hr = obj.GetObject(out var value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		public CorAppDomain(ICorDebugAppDomain appDomain)
			: base(appDomain) {
			int hr = appDomain.GetID(out id);
			if (hr < 0)
				id = -1;

			//TODO: ICorDebugAppDomain3
			//TODO: ICorDebugAppDomain4::GetObjectForCCW 
		}

		/// <summary>
		/// Sets the debug state of all managed threads
		/// </summary>
		/// <param name="state">New state</param>
		/// <param name="thread">Thread to exempt from the new state or null</param>
		public void SetAllThreadsDebugState(CorDebugThreadState state, CorThread thread = null) =>
			obj.SetAllThreadsDebugState(state, thread == null ? null : thread.RawObject);

		/// <summary>
		/// true if any managed callbacks are currently queued for the specified thread
		/// </summary>
		/// <param name="thread">Thread or null to check all threads</param>
		/// <returns></returns>
		public bool HasQueuedCallbacks(CorThread thread) {
			int hr = obj.HasQueuedCallbacks(thread == null ? null : thread.RawObject, out int queued);
			return hr >= 0 && queued != 0;
		}

		public bool Detach() => obj.Detach() >= 0;

		public CorType GetPtr(CorType type) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.Ptr, 0, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetByRef(CorType type) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.ByRef, 0, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetSZArray(CorType type) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.SZArray, 1, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetArray(CorType type, uint rank) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.Array, rank, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetFnPtr(CorType[] args) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetFunctionPointerType(args.Length, args.ToCorDebugArray(), out var res);
			return res == null ? null : new CorType(res);
		}

		public static bool operator ==(CorAppDomain a, CorAppDomain b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorAppDomain a, CorAppDomain b) => !(a == b);
		public bool Equals(CorAppDomain other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorAppDomain);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => string.Format("[AppDomain] {0} {1}", Id, Name);
	}
}
