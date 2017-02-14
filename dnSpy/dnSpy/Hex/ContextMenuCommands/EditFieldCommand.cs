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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Hex.ContextMenuCommands {
	sealed class EditFieldCommandContext {
		public HexView HexView { get; }
		public ComplexData Structure { get; }
		public SimpleData Field { get; }
		public EditFieldCommandContext(HexView hexView, ComplexData structure, SimpleData field) {
			HexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			Structure = structure ?? throw new ArgumentNullException(nameof(structure));
			Field = field ?? throw new ArgumentNullException(nameof(field));
		}
	}

	abstract class EditFieldCommandTargetMenuItemBase : MenuItemBase<EditFieldCommandContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory;

		protected EditFieldCommandTargetMenuItemBase(Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory) => this.hexBufferFileServiceFactory = hexBufferFileServiceFactory ?? throw new ArgumentNullException(nameof(hexBufferFileServiceFactory));

		protected override EditFieldCommandContext CreateContext(IMenuItemContext context) {
			var hexView = context.Find<HexView>();
			if (hexView == null)
				return null;
			var hexBufferFileService = hexBufferFileServiceFactory.Value.Create(hexView.Buffer);
			var pos = hexView.Caret.Position.Position.ActivePosition.BufferPosition.Position;
			var info = hexBufferFileService.GetFileAndStructure(pos);
			if (info == null)
				return null;
			var field = info.Value.Structure.GetSimpleField(pos)?.Data as SimpleData;
			if (field == null)
				return null;
			if (!(field is FlagsData || field is EnumData))
				return null;
			return new EditFieldCommandContext(hexView, info.Value.Structure, field);
		}

		protected bool IsReadOnly(HexViewContext context) => context.HexView.Buffer.IsReadOnly || context.HexView.Options.DoesViewProhibitUserInput();
	}

	static class EditFieldConstants {
		public const string EDIT_FIELD_GUID = "415A06C7-AA07-430D-853B-61CF2ADD5BC9";
		public const string GROUP_EDIT_FIELD = "0,C961BE42-4145-4D3D-BE62-503CC95CD259";
	}

	[ExportMenuItem(Header = "res:HexEditFieldCommand", Guid = EditFieldConstants.EDIT_FIELD_GUID, Group = MenuConstants.GROUP_CTX_HEXVIEW_EDIT, Order = 20)]
	sealed class EditFieldCommand : EditFieldCommandTargetMenuItemBase {
		[ImportingConstructor]
		EditFieldCommand(Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory)
			: base(hexBufferFileServiceFactory) {
		}

		public override void Execute(EditFieldCommandContext context) { }
	}

	[ExportMenuItem(OwnerGuid = EditFieldConstants.EDIT_FIELD_GUID, Group = EditFieldConstants.GROUP_EDIT_FIELD, Order = 0)]
	sealed class EditFieldCreatorCommand : EditFieldCommandTargetMenuItemBase, IMenuItemProvider {
		[ImportingConstructor]
		EditFieldCreatorCommand(Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory)
			: base(hexBufferFileServiceFactory) {
		}

		public override void Execute(EditFieldCommandContext context) { }

		IEnumerable<CreatedMenuItem> IMenuItemProvider.Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			if (ctx == null)
				yield break;

			if (ctx.Field is FlagsData flagsData) {
				var isEnum = new HashSet<ulong>();
				foreach (var info in flagsData.FlagInfos) {
					if (info.IsEnumName)
						isEnum.Add(info.Mask);
				}
				foreach (var info in flagsData.FlagInfos) {
					if (info.IsEnumName)
						continue;
					var attr = new ExportMenuItemAttribute {
						Header = UIUtilities.EscapeMenuItemHeader(info.Name),
					};
					yield return new CreatedMenuItem(attr, new EditFlagsFieldCommand(hexBufferFileServiceFactory, info, !isEnum.Contains(info.Mask)));
				}
			}
			else if (ctx.Field is EnumData enumData) {
				foreach (var info in enumData.EnumFieldInfos) {
					var attr = new ExportMenuItemAttribute {
						Header = UIUtilities.EscapeMenuItemHeader(info.Name),
					};
					yield return new CreatedMenuItem(attr, new EditEnumFieldCommand(hexBufferFileServiceFactory, info));
				}
			}
			else
				Debug.Fail($"Unknown type: {ctx.Field.GetType()}");
		}
	}

	sealed class EditFlagsFieldCommand : EditFieldCommandTargetMenuItemBase {
		readonly FlagInfo flagInfo;
		readonly bool isFlag;

		public EditFlagsFieldCommand(Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory, FlagInfo flagInfo, bool isFlag)
			: base(hexBufferFileServiceFactory) {
			if (flagInfo.IsEnumName)
				throw new ArgumentOutOfRangeException(nameof(flagInfo));
			this.flagInfo = flagInfo;
			this.isFlag = isFlag;
		}

		public override bool IsChecked(EditFieldCommandContext context) {
			var flagsData = (FlagsData)context.Field;
			var buffer = flagsData.Span.Buffer;
			var pos = flagsData.Span.Span.Start;
			switch (flagsData.Span.Length.ToUInt64()) {
			case 1:	return (buffer.ReadByte(pos) & flagInfo.Mask) == flagInfo.Value;
			case 2:	return (buffer.ReadUInt16(pos) & flagInfo.Mask) == flagInfo.Value;
			case 4:	return (buffer.ReadUInt32(pos) & flagInfo.Mask) == flagInfo.Value;
			case 8:	return (buffer.ReadUInt64(pos) & flagInfo.Mask) == flagInfo.Value;
			default:
				Debug.Fail($"Unknown size: {flagsData.Span.Length}");
				return false;
			}
		}

		public override void Execute(EditFieldCommandContext context) {
			var flagsData = (FlagsData)context.Field;
			var buffer = flagsData.Span.Buffer;
			var pos = flagsData.Span.Span.Start;
			if (isFlag) {
				switch (flagsData.Span.Length.ToUInt64()) {
				case 1:
					if ((buffer.ReadByte(pos) & flagInfo.Mask) != 0)
						buffer.Replace(pos, (byte)(buffer.ReadByte(pos) & ~flagInfo.Mask));
					else
						buffer.Replace(pos, (byte)((buffer.ReadByte(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				case 2:
					if ((buffer.ReadUInt16(pos) & flagInfo.Mask) != 0)
						buffer.Replace(pos, (ushort)(buffer.ReadUInt16(pos) & ~flagInfo.Mask));
					else
						buffer.Replace(pos, (ushort)((buffer.ReadUInt16(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				case 4:
					if ((buffer.ReadUInt32(pos) & flagInfo.Mask) != 0)
						buffer.Replace(pos, (uint)(buffer.ReadUInt32(pos) & ~flagInfo.Mask));
					else
						buffer.Replace(pos, (uint)((buffer.ReadUInt32(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				case 8:
					if ((buffer.ReadUInt64(pos) & flagInfo.Mask) != 0)
						buffer.Replace(pos, (ulong)(buffer.ReadUInt64(pos) & ~flagInfo.Mask));
					else
						buffer.Replace(pos, (ulong)((buffer.ReadUInt64(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				default:
					Debug.Fail($"Unknown size: {flagsData.Span.Length}");
					break;
				}
			}
			else {
				switch (flagsData.Span.Length.ToUInt64()) {
				case 1:
					buffer.Replace(pos, (byte)((buffer.ReadByte(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				case 2:
					buffer.Replace(pos, (ushort)((buffer.ReadUInt16(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				case 4:
					buffer.Replace(pos, (uint)((buffer.ReadUInt32(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				case 8:
					buffer.Replace(pos, (ulong)((buffer.ReadUInt64(pos) & ~flagInfo.Mask) | flagInfo.Value));
					break;

				default:
					Debug.Fail($"Unknown size: {flagsData.Span.Length}");
					break;
				}
			}
		}
	}

	sealed class EditEnumFieldCommand : EditFieldCommandTargetMenuItemBase {
		readonly EnumFieldInfo enumFieldInfo;

		public EditEnumFieldCommand(Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory, EnumFieldInfo enumFieldInfo)
			: base(hexBufferFileServiceFactory) => this.enumFieldInfo = enumFieldInfo;

		public override bool IsChecked(EditFieldCommandContext context) {
			var enumData = (EnumData)context.Field;
			var buffer = enumData.Span.Buffer;
			var pos = enumData.Span.Span.Start;
			switch (enumData.Span.Length.ToUInt64()) {
			case 1:	return buffer.ReadByte(pos) == enumFieldInfo.Value;
			case 2:	return buffer.ReadUInt16(pos) == enumFieldInfo.Value;
			case 4:	return buffer.ReadUInt32(pos) == enumFieldInfo.Value;
			case 8:	return buffer.ReadUInt64(pos) == enumFieldInfo.Value;
			default:
				Debug.Fail($"Unknown size: {enumData.Span.Length}");
				return false;
			}
		}

		public override void Execute(EditFieldCommandContext context) {
			var enumData = (EnumData)context.Field;
			var buffer = enumData.Span.Buffer;
			var pos = enumData.Span.Span.Start;
			switch (enumData.Span.Length.ToUInt64()) {
			case 1:
				buffer.Replace(pos, (byte)enumFieldInfo.Value);
				break;

			case 2:
				buffer.Replace(pos, (ushort)enumFieldInfo.Value);
				break;

			case 4:
				buffer.Replace(pos, (uint)enumFieldInfo.Value);
				break;

			case 8:
				buffer.Replace(pos, (ulong)enumFieldInfo.Value);
				break;

			default:
				Debug.Fail($"Unknown size: {enumData.Span.Length}");
				break;
			}
		}
	}
}
