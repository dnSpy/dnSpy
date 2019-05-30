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

using System.Dynamic;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	static class DynamicMetaObjectProviderDebugViewHelper {
		static readonly DmdReadOnlyAssemblyName debugViewAssemblyName =
			new DmdReadOnlyAssemblyName("Microsoft.CSharp",
			null, string.Empty, DmdAssemblyNameFlags.None,
			new byte[8] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A }, DmdAssemblyHashAlgorithm.None);

		sealed class CtorState {
			public DmdAssembly? Assembly;
			public DmdConstructorInfo? Constructor;
		}

		/// <summary>
		/// Returns the debug view constructor for objects implementing <see cref="IDynamicMetaObjectProvider"/>
		/// or COM objects.
		/// 
		/// The debug view is in the <c>Microsoft.CSharp</c> assembly. If it's not been loaded, this method
		/// returns null.
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public static DmdConstructorInfo? GetDynamicMetaObjectProviderDebugViewConstructor(DmdAppDomain appDomain) {
			var state = appDomain.GetOrCreateData<CtorState>();
			if (state.Assembly is null) {
				state.Assembly = appDomain.GetAssembly(debugViewAssemblyName);
				var type = state.Assembly?.GetType("Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView", DmdGetTypeOptions.None);
				state.Constructor = type?.GetMethod(DmdConstructorInfo.ConstructorName, DmdSignatureCallingConvention.HasThis, 0, appDomain.System_Void, new[] { appDomain.System_Object }, throwOnError: false) as DmdConstructorInfo;
			}
			return state.Constructor;
		}

		public static string GetDebugViewTypeDisplayName() =>
			"Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView";
	}
}
