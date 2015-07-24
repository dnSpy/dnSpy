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

using System.Linq;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Forms;

namespace ICSharpCode.ILSpy.AsmEditor.Resources
{
	sealed class ImageListVM : ViewModelBase
	{
		readonly ImageListOptions origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
		string name;

		internal static readonly EnumVM[] colorDepthList = new EnumVM[] {
			new EnumVM(ColorDepth.Depth4Bit, "4-bit"),
			new EnumVM(ColorDepth.Depth8Bit, "8-bit"),
			new EnumVM(ColorDepth.Depth16Bit, "16-bit"),
			new EnumVM(ColorDepth.Depth24Bit, "24-bit"),
			new EnumVM(ColorDepth.Depth32Bit, "32-bit"),
		};
		public EnumListVM ColorDepthVM {
			get { return colorDepthVM; }
		}
		readonly EnumListVM colorDepthVM = new EnumListVM(colorDepthList);

		public Int32VM WidthVM {
			get { return widthVM; }
		}
		readonly Int32VM widthVM;

		public Int32VM HeightVM {
			get { return heightVM; }
		}
		readonly Int32VM heightVM;

		public DefaultConverterVM<Color> TransparentColorVM {
			get { return transparentColorVM; }
		}
		readonly DefaultConverterVM<Color> transparentColorVM;

		public ImageListStreamerVM ImageListStreamerVM {
			get { return imageListStreamerVM; }
		}
		readonly ImageListStreamerVM imageListStreamerVM;

		public ImageListVM(ImageListOptions options)
		{
			this.origOptions = options;

			this.imageListStreamerVM = new ImageListStreamerVM();
			ImageListStreamerVM.Collection.CollectionChanged += (s, e) => HasErrorUpdated();
			this.widthVM = new Int32VM(a => HasErrorUpdated()) {
				Min = 1,
				Max = 256,
				UseDecimal = true,
			};
			this.heightVM = new Int32VM(a => HasErrorUpdated()) {
				Min = 1,
				Max = 256,
				UseDecimal = true,
			};
			this.transparentColorVM = new DefaultConverterVM<Color>(a => HasErrorUpdated());

			Reinitialize();
		}

		void Reinitialize()
		{
			InitializeFrom(origOptions);
		}

		public ImageListOptions CreateImageListOptions()
		{
			return CopyTo(new ImageListOptions());
		}

		void InitializeFrom(ImageListOptions options)
		{
			Name = options.Name;
			HeightVM.Value = options.ImageSize.Height;
			WidthVM.Value = options.ImageSize.Width;
			TransparentColorVM.Value = options.TransparentColor;
			ColorDepthVM.SelectedItem = options.ColorDepth;
			ImageListStreamerVM.InitializeFrom(options.ImageSources);
		}

		ImageListOptions CopyTo(ImageListOptions options)
		{
			options.Name = Name;
			options.ImageSize.Height = HeightVM.Value;
			options.ImageSize.Width = WidthVM.Value;
			options.TransparentColor = TransparentColorVM.Value;
			options.ColorDepth = (ColorDepth)ColorDepthVM.SelectedItem;
			options.ImageSources.Clear();
			options.ImageSources.AddRange(ImageListStreamerVM.Collection.Select(a => a.ImageSource));
			return options;
		}

		public override bool HasError {
			get {
				return ImageListStreamerVM.Collection.Count == 0 ||
					WidthVM.HasError ||
					HeightVM.HasError ||
					TransparentColorVM.HasError;
			}
		}
	}
}
