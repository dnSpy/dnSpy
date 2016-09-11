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
using System.ComponentModel;
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Completion filter base class
	/// </summary>
	class IntellisenseFilter : IIntellisenseFilter {
		/// <summary>
		/// Raised when a property value changes
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets the image
		/// </summary>
		public ImageReference Image { get; }

		/// <summary>
		/// Gets the tooltip
		/// </summary>
		public string ToolTip { get; }

		/// <summary>
		/// Gets the access key
		/// </summary>
		public string AccessKey { get; }

		/// <summary>
		/// Gets/sets the checked state
		/// </summary>
		public virtual bool IsChecked {
			get { return isChecked; }
			set {
				if (isChecked != value) {
					isChecked = value;
					RaisePropertyChanged(nameof(IsChecked));
				}
			}
		}
		bool isChecked;

		/// <summary>
		/// Gets/sets the enabled state
		/// </summary>
		public virtual bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					RaisePropertyChanged(nameof(IsEnabled));
				}
			}
		}
		bool isEnabled;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="image">Image</param>
		/// <param name="toolTip">Tooltip</param>
		/// <param name="accessKey">Access key</param>
		/// <param name="isChecked">true if it's checked</param>
		/// <param name="isEnabled">true if it's enabled</param>
		public IntellisenseFilter(ImageReference image, string toolTip, string accessKey, bool isChecked = false, bool isEnabled = true) {
			if (image.IsDefault)
				throw new ArgumentException();
			if (toolTip == null)
				throw new ArgumentNullException(nameof(toolTip));
			if (accessKey == null)
				throw new ArgumentNullException(nameof(accessKey));
			Image = image;
			ToolTip = toolTip;
			AccessKey = accessKey;
			this.isChecked = isChecked;
			this.isEnabled = isEnabled;
		}

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propertyName">Name of property that changed</param>
		protected void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
