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
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.AsmEditor.Resources {
	sealed class ImageResourceElementVM : ViewModelBase {
		readonly ResourceElementOptions origOptions;

		public IOpenFile OpenFile {
			set { openFile = value; }
		}
		IOpenFile openFile;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand FillDataCommand {
			get { return new RelayCommand(a => FillData()); }
		}

		ResourceTypeCode resourceTypeCode;

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
		UTF8String name;

		public byte[] Data {
			get { return data; }
			set {
				if (data != value) {
					data = value;
					OnPropertyChanged("Data");
					OnPropertyChanged("DataString");
				}
			}
		}
		byte[] data;

		public string DataString {
			get { return string.Format(dnSpy_AsmEditor_Resources.XBytes, Data == null ? 0 : Data.Length); }
		}

		public ImageResourceElementVM(ResourceElementOptions options) {
			this.origOptions = options;

			Reinitialize();
		}

		void FillData() {
			if (openFile == null)
				throw new InvalidOperationException();
			var newBytes = openFile.Open(PickFilenameConstants.ImagesFilter);
			if (newBytes != null)
				Data = newBytes;
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public ResourceElementOptions CreateResourceElementOptions() {
			return CopyTo(new ResourceElementOptions());
		}

		void InitializeFrom(ResourceElementOptions options) {
			if (options.ResourceData.Code != ResourceTypeCode.ByteArray && options.ResourceData.Code != ResourceTypeCode.Stream)
				throw new InvalidOperationException();
			var builtin = (BuiltInResourceData)options.ResourceData;

			this.resourceTypeCode = options.ResourceData.Code;
			this.Name = options.Name;
			this.Data = (byte[])builtin.Data;
		}

		ResourceElementOptions CopyTo(ResourceElementOptions options) {
			options.Name = this.Name;
			options.ResourceData = new BuiltInResourceData(resourceTypeCode, Data);
			return options;
		}
	}
}
