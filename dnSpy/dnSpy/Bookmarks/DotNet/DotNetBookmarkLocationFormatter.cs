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
using dnlib.DotNet;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Bookmarks.DotNet {
	abstract class DotNetBookmarkLocationFormatter : BookmarkLocationFormatter {
		readonly BookmarkFormatterServiceImpl owner;
		readonly IDotNetBookmarkLocation location;

		public DotNetBookmarkLocationFormatter(BookmarkFormatterServiceImpl owner, IDotNetBookmarkLocation location) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.location = location ?? throw new ArgumentNullException(nameof(location));
		}

		public override void Dispose() => location.Formatter = null;

		internal void RefreshLocation() => RaiseLocationChanged();

		protected string GetHexPrefix() {
			if (owner.MethodDecompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
				return "&H";
			return "0x";
		}

		void WriteToken(ITextColorWriter output, uint token) =>
			output.Write(BoxedTextColor.Number, GetHexPrefix() + token.ToString("X8"));

		public override void WriteLocation(ITextColorWriter output, BookmarkLocationFormatterOptions options) {
			bool printedToken = false;
			if ((options & BookmarkLocationFormatterOptions.Tokens) != 0) {
				WriteToken(output, location.Token);
				output.WriteSpace();
				printedToken = true;
			}

			bool success = WriteLocationCore(output, options);
			if (!success) {
				if (printedToken)
					output.Write(BoxedTextColor.Error, "???");
				else
					WriteToken(output, location.Token);
			}
		}

		protected abstract bool WriteLocationCore(ITextColorWriter output, BookmarkLocationFormatterOptions options);
		protected TDef GetDefinition<TDef>() where TDef : class => owner.GetDefinition<TDef>(location.Module, location.Token);
		protected IDecompiler MethodDecompiler => owner.MethodDecompiler;

		protected FormatterOptions GetFormatterOptions(BookmarkLocationFormatterOptions options) {
			FormatterOptions flags = 0;
			if ((options & BookmarkLocationFormatterOptions.ModuleNames) != 0)
				flags |= FormatterOptions.ShowModuleNames;
			if ((options & BookmarkLocationFormatterOptions.ParameterTypes) != 0)
				flags |= FormatterOptions.ShowParameterTypes;
			if ((options & BookmarkLocationFormatterOptions.ParameterNames) != 0)
				flags |= FormatterOptions.ShowParameterNames;
			if ((options & BookmarkLocationFormatterOptions.DeclaringTypes) != 0)
				flags |= FormatterOptions.ShowDeclaringTypes;
			if ((options & BookmarkLocationFormatterOptions.ReturnTypes) != 0)
				flags |= FormatterOptions.ShowReturnTypes;
			if ((options & BookmarkLocationFormatterOptions.Namespaces) != 0)
				flags |= FormatterOptions.ShowNamespaces;
			if ((options & BookmarkLocationFormatterOptions.IntrinsicTypeKeywords) != 0)
				flags |= FormatterOptions.ShowIntrinsicTypeKeywords;
			if ((options & BookmarkLocationFormatterOptions.DigitSeparators) != 0)
				flags |= FormatterOptions.DigitSeparators;
			if ((options & BookmarkLocationFormatterOptions.Decimal) != 0)
				flags |= FormatterOptions.UseDecimal;
			return flags;
		}

		public override void WriteModule(ITextColorWriter output) =>
			output.WriteFilename(location.Module.ModuleName);
	}

	sealed class DotNetMethodBodyBookmarkLocationFormatterImpl : DotNetBookmarkLocationFormatter {
		readonly DotNetMethodBodyBookmarkLocation location;
		WeakReference weakMethod;

		public DotNetMethodBodyBookmarkLocationFormatterImpl(BookmarkFormatterServiceImpl owner, DotNetMethodBodyBookmarkLocationImpl location)
			: base(owner, location) => this.location = location ?? throw new ArgumentNullException(nameof(location));

		void WriteILOffset(ITextColorWriter output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(BoxedTextColor.Number, GetHexPrefix() + offset.ToString("X4"));
			else
				output.Write(BoxedTextColor.Number, GetHexPrefix() + offset.ToString("X8"));
		}

		protected override bool WriteLocationCore(ITextColorWriter output, BookmarkLocationFormatterOptions options) {
			var method = weakMethod?.Target as MethodDef ?? GetDefinition<MethodDef>();
			if (method == null)
				return false;
			if (weakMethod?.Target != method)
				weakMethod = new WeakReference(method);
			MethodDecompiler.Write(output, method, GetFormatterOptions(options));

			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "+");
			output.WriteSpace();
			WriteILOffset(output, location.Offset);

			return true;
		}
	}

	sealed class DotNetTokenBookmarkLocationFormatterImpl : DotNetBookmarkLocationFormatter {
		readonly DotNetTokenBookmarkLocation location;
		WeakReference weakMember;

		public DotNetTokenBookmarkLocationFormatterImpl(BookmarkFormatterServiceImpl owner, DotNetTokenBookmarkLocationImpl location)
			: base(owner, location) => this.location = location ?? throw new ArgumentNullException(nameof(location));

		protected override bool WriteLocationCore(ITextColorWriter output, BookmarkLocationFormatterOptions options) {
			var def = weakMember?.Target as IMemberDef ?? GetDefinition<IMemberDef>();
			if (def == null)
				return false;
			if (weakMember?.Target != def)
				weakMember = new WeakReference(def);
			MethodDecompiler.Write(output, def, GetFormatterOptions(options));

			return true;
		}
	}
}
