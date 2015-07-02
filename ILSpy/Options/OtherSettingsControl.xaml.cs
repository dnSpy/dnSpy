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

using System.Windows.Controls;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy.Options
{
	[ExportOptionPage(Title = "Other", Order = 2)]
	sealed class OtherSettingsCreator : IOptionPageCreator
	{
		public IOptionPage Create()
		{
			return new OtherSettingsControl();
		}
	}

	/// <summary>
	/// Interaction logic for OtherSettingsControl.xaml
	/// </summary>
	public partial class OtherSettingsControl : UserControl, IOptionPage
	{
		public OtherSettingsControl()
		{
			InitializeComponent();
		}

		public void Load(ILSpySettings settings)
		{
			this.DataContext = OtherSettings.Load(settings);
		}

		public RefreshFlags Save(XElement root)
		{
			return ((OtherSettings)this.DataContext).Save(root);
		}
	}
}
