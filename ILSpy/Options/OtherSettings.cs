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

using System.Threading;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy.Options
{
	sealed class OtherSettings : AsmEditor.ViewModelBase
	{
		public static OtherSettings Instance {
			get {
				if (settings != null)
					return settings;
				Interlocked.CompareExchange(ref settings, Load(ILSpySettings.Load()), null);
				return settings;
			}
		}
		static OtherSettings settings;

		public bool UseMemoryMappedIO {
			get { return useMemoryMappedIO; }
			set {
				if (useMemoryMappedIO != value) {
					useMemoryMappedIO = value;
					OnPropertyChanged("UseMemoryMappedIO");
				}
			}
		}
		bool useMemoryMappedIO;

		const string SETTINGS_SECTION_NAME = "OtherSettings";
		internal static OtherSettings Load(ILSpySettings settings)
		{
			var xelem = settings[SETTINGS_SECTION_NAME];
			var s = new OtherSettings();
			s.UseMemoryMappedIO = (bool?)xelem.Attribute("UseMemoryMappedIO") ?? true;
			return s;
		}

		internal RefreshFlags Save(XElement root)
		{
			var flags = RefreshFlags.None;

			if (!this.UseMemoryMappedIO && Instance.UseMemoryMappedIO)
				flags |= RefreshFlags.DisableMmap;

			var xelem = new XElement(SETTINGS_SECTION_NAME);
			xelem.SetAttributeValue("UseMemoryMappedIO", this.UseMemoryMappedIO);


			var currElem = root.Element(SETTINGS_SECTION_NAME);
			if (currElem != null)
				currElem.ReplaceWith(xelem);
			else
				root.Add(xelem);

			WriteTo(Instance);

			return flags;
		}

		void WriteTo(OtherSettings other)
		{
			other.UseMemoryMappedIO = this.UseMemoryMappedIO;
		}

		protected override string Verify(string columnName) {
			return string.Empty;
		}

		public override bool HasError {
			get { return false; }
		}
	}
}
