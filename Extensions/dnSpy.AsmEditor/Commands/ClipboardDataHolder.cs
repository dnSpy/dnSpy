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
using System.Runtime.InteropServices;
using System.Windows;

namespace dnSpy.AsmEditor.Commands {
	// This class helps copying non-serializable data within this process. The method body editor
	// uses classes that aren't easy to serialize.
	static class ClipboardDataHolder {
		static readonly string suffix = " - " + Process.GetCurrentProcess().Id.ToString();
		// Contains our MyDataObject instance. The system clipboard has a strong reference to it,
		// as long as our data is in the clipboard.
		static WeakReference weakRefMyDataObject = new WeakReference(suffix);

		// Dummy class that only returns dummy serialized data if it's read by someone else.
		sealed class MyDataObject : IDataObject {
			static readonly object serializedData = 42;
			readonly string dataFormat;

			// The real (non-serializable) clipboard data
			public object Data => data;
			readonly object data;

			public MyDataObject(string dataFormat, object data) {
				this.data = data;
				this.dataFormat = dataFormat;
			}

			public object? GetData(Type format) => format.FullName is string fullName ? GetData(fullName) : null;
			public object? GetData(string format) => GetData(format, true);

			public object? GetData(string format, bool autoConvert) {
				if (format != dataFormat)
					return null;
				return serializedData;
			}

			public bool GetDataPresent(Type format) => format.FullName is string fullName && GetDataPresent(fullName);
			public bool GetDataPresent(string format) => GetDataPresent(format, true);
			public bool GetDataPresent(string format, bool autoConvert) => format == dataFormat;
			public string[] GetFormats() => GetFormats(true);
			public string[] GetFormats(bool autoConvert) => new string[] { dataFormat };
			public void SetData(object data) => SetData(data.GetType(), data);
			public void SetData(Type format, object data) => SetData(format.FullName ?? throw new ArgumentException(), data, true);
			public void SetData(string format, object data) => SetData(format, data, true);
			public void SetData(string format, object data, bool autoConvert) => Debug.Fail("Shouldn't be here");
		}

		static string GetDataFormat(Type type) => type.FullName + suffix;

		static bool IsInClipboard(Type type) {
			try {
				return Clipboard.ContainsData(GetDataFormat(type));
			}
			catch (ExternalException) {
				return false;
			}
		}

		public static void Add<T>(T data) where T : class {
			var dataFormat = GetDataFormat(typeof(T));
			var mdo = new MyDataObject(dataFormat, data);
			try {
				Clipboard.SetDataObject(mdo, false);
			}
			catch (ExternalException) {
				return;
			}
			weakRefMyDataObject = new WeakReference(mdo);
		}

		public static T? TryGet<T>() where T : class {
			if (!IsInClipboard(typeof(T)))
				return null;
			var mdo = weakRefMyDataObject.Target as MyDataObject;
			return mdo?.Data as T;
		}
	}
}
