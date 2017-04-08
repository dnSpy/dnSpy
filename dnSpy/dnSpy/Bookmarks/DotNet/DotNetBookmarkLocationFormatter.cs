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
using dnlib.DotNet;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Bookmarks.DotNet {
	abstract class DotNetBookmarkLocationFormatter : BookmarkLocationFormatter {
		readonly BookmarkFormatterServiceImpl owner;
		readonly BookmarkDisplaySettings bookmarkDisplaySettings;
		readonly IDotNetBookmarkLocation location;

		public DotNetBookmarkLocationFormatter(BookmarkFormatterServiceImpl owner, BookmarkDisplaySettings bookmarkDisplaySettings, IDotNetBookmarkLocation location) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.bookmarkDisplaySettings = bookmarkDisplaySettings ?? throw new ArgumentNullException(nameof(bookmarkDisplaySettings));
			this.location = location ?? throw new ArgumentNullException(nameof(location));
		}

		internal void RefreshLocation() => RaiseLocationChanged();

		void WriteToken(ITextColorWriter output, uint token) =>
			output.Write(BoxedTextColor.Number, "0x" + token.ToString("X8"));

		public override void WriteLocation(ITextColorWriter output) {
			bool printedToken = false;
			if (bookmarkDisplaySettings.ShowTokens) {
				WriteToken(output, location.Token);
				output.WriteSpace();
				printedToken = true;
			}

			bool success = WriteLocationCore(output);
			if (!success) {
				if (printedToken)
					output.Write(BoxedTextColor.Error, "???");
				else
					WriteToken(output, location.Token);
			}
		}

		protected abstract bool WriteLocationCore(ITextColorWriter output);
		protected TDef GetDefinition<TDef>() where TDef : class => owner.GetDefinition<TDef>(location.Module, location.Token);
		protected IDecompiler MethodDecompiler => owner.MethodDecompiler;

		protected SimplePrinterFlags GetPrinterFlags() {
			SimplePrinterFlags flags = 0;
			if (bookmarkDisplaySettings.ShowModuleNames)			flags |= SimplePrinterFlags.ShowModuleNames;
			if (bookmarkDisplaySettings.ShowParameterTypes)			flags |= SimplePrinterFlags.ShowParameterTypes;
			if (bookmarkDisplaySettings.ShowParameterNames)			flags |= SimplePrinterFlags.ShowParameterNames;
			if (bookmarkDisplaySettings.ShowDeclaringTypes)			flags |= SimplePrinterFlags.ShowOwnerTypes;
			if (bookmarkDisplaySettings.ShowReturnTypes)			flags |= SimplePrinterFlags.ShowReturnTypes;
			if (bookmarkDisplaySettings.ShowNamespaces)				flags |= SimplePrinterFlags.ShowNamespaces;
			if (bookmarkDisplaySettings.ShowIntrinsicTypeKeywords)	flags |= SimplePrinterFlags.ShowTypeKeywords;
			return flags;
		}

		public override void WriteModule(ITextColorWriter output) =>
			output.WriteFilename(location.Module.ModuleName);
	}

	sealed class DotNetMethodBodyBookmarkLocationFormatterImpl : DotNetBookmarkLocationFormatter {
		readonly DotNetMethodBodyBookmarkLocation location;

		public DotNetMethodBodyBookmarkLocationFormatterImpl(BookmarkFormatterServiceImpl owner, BookmarkDisplaySettings bookmarkDisplaySettings, DotNetMethodBodyBookmarkLocationImpl location)
			: base(owner, bookmarkDisplaySettings, location) => this.location = location ?? throw new ArgumentNullException(nameof(location));

		void WriteILOffset(ITextColorWriter output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(BoxedTextColor.Number, "0x" + offset.ToString("X4"));
			else
				output.Write(BoxedTextColor.Number, "0x" + offset.ToString("X8"));
		}

		protected override bool WriteLocationCore(ITextColorWriter output) {
			var method = GetDefinition<MethodDef>();
			if (method == null)
				return false;
			MethodDecompiler.Write(output, method, GetPrinterFlags());

			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "+");
			output.WriteSpace();
			WriteILOffset(output, location.Offset);

			return true;
		}
	}

	sealed class DotNetTokenBookmarkLocationFormatterImpl : DotNetBookmarkLocationFormatter {
		readonly DotNetTokenBookmarkLocation location;

		public DotNetTokenBookmarkLocationFormatterImpl(BookmarkFormatterServiceImpl owner, BookmarkDisplaySettings bookmarkDisplaySettings, DotNetTokenBookmarkLocationImpl location)
			: base(owner, bookmarkDisplaySettings, location) => this.location = location ?? throw new ArgumentNullException(nameof(location));

		protected override bool WriteLocationCore(ITextColorWriter output) {
			var def = GetDefinition<IMemberDef>();
			if (def == null)
				return false;
			MethodDecompiler.Write(output, def, GetPrinterFlags());

			return true;
		}
	}
}
