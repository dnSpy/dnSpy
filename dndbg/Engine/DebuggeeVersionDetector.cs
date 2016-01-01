/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Runtime.InteropServices;
using System.Text;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaHost;

namespace dndbg.Engine {
	public static class DebuggeeVersionDetector {
		public static string GetVersion(string filename) {
			return TryGetVersion(filename) ?? RuntimeEnvironment.GetSystemVersion();
		}

		public static string TryGetVersion(string filename) {
			try {
				var clsid = new Guid("2EBCD49A-1B47-4A61-B13A-4A03701E594B");
				var riid = typeof(ICLRMetaHostPolicy).GUID;
				var mhp = (ICLRMetaHostPolicy)NativeMethods.CLRCreateInstance(ref clsid, ref riid);

				// GetRequestedRuntime() automatically reads the *.config file if it exists so
				// we don't need to send in a stream with its contents.
				IStream configStream = null;
				const int STRING_LEN = 1024;
				var sbVersion = new StringBuilder(STRING_LEN);
				uint versionLength = (uint)sbVersion.MaxCapacity;
				var sbImageVersion = new StringBuilder(STRING_LEN);
				uint imageVersionLength = (uint)sbImageVersion.MaxCapacity;
				uint configFlags;
				riid = typeof(ICLRRuntimeInfo).GUID;

				mhp.GetRequestedRuntime(
						METAHOST_POLICY_FLAGS.METAHOST_POLICY_HIGHCOMPAT,
						filename,
						configStream,
						sbVersion,
						ref versionLength,
						sbImageVersion,
						ref imageVersionLength,
						out configFlags,
						ref riid);

				return sbVersion.ToString();
			}
			catch (COMException) {
			}

			return null;
		}
	}
}
