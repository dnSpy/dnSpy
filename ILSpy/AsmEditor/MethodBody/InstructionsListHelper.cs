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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AsmEditor.DnlibDialogs;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;
using ICSharpCode.ILSpy.TreeNodes.Filters;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.AsmEditor.MethodBody
{
	sealed class InstructionsListHelper : ListBoxHelperBase<InstructionVM>, IEditOperand, ISelectItems<InstructionVM>
	{
		CilBodyVM cilBodyVM;

		public InstructionsListHelper(ListView listView, Window ownerWindow)
			: base(listView, "Instruction")
		{
		}

		protected override InstructionVM[] GetSelectedItems()
		{
			return listBox.SelectedItems.Cast<InstructionVM>().ToArray();
		}

		protected override void OnDataContextChangedInternal(object dataContext)
		{
			this.cilBodyVM = ((MethodBodyVM)dataContext).CilBodyVM;
			this.cilBodyVM.SelectItems = this;
			this.coll = cilBodyVM.InstructionsListVM;
			this.coll.CollectionChanged += coll_CollectionChanged;
			InitializeInstructions(this.coll);

			Add(new ContextMenuHandler {
				Header = "_NOP Instruction",
				HeaderPlural = "_NOP Instructions",
				Command = cilBodyVM.ReplaceInstructionWithNopCommand,
				Icon = "NOP",
				InputGestureText = "N",
				Modifiers = ModifierKeys.None,
				Key = Key.N,
			});
			Add(new ContextMenuHandler {
				Header = "_Invert Branch",
				HeaderPlural = "_Invert Branches",
				Command = cilBodyVM.InvertBranchCommand,
				Icon = "Branch",
				InputGestureText = "I",
				Modifiers = ModifierKeys.None,
				Key = Key.I,
			});
			Add(new ContextMenuHandler {
				Header = "Convert to Unconditional _Branch",
				HeaderPlural = "Convert to Unconditional _Branches",
				Command = cilBodyVM.ConvertBranchToUnconditionalBranchCommand,
				Icon = "ToUncondBranch",
				InputGestureText = "B",
				Modifiers = ModifierKeys.None,
				Key = Key.B,
			});
			Add(new ContextMenuHandler {
				Header = "R_emove and Add Pops",
				Command = cilBodyVM.RemoveInstructionAndAddPopsCommand,
				InputGestureText = "P",
				Modifiers = ModifierKeys.None,
				Key = Key.P,
			});
			AddSeparator();
			Add(new ContextMenuHandler {
				Header = "_Simplify All Instructions",
				Command = cilBodyVM.SimplifyAllInstructionsCommand,
				InputGestureText = "S",
				Modifiers = ModifierKeys.None,
				Key = Key.S,
			});
			Add(new ContextMenuHandler {
				Header = "Optimi_ze All Instructions",
				Command = cilBodyVM.OptimizeAllInstructionsCommand,
				InputGestureText = "O",
				Modifiers = ModifierKeys.None,
				Key = Key.O,
			});
			AddSeparator();
			AddStandardMenuHandlers();
			Add(new ContextMenuHandler {
				Header = "Copy _MD Token",
				HeaderPlural = "Copy _MD Tokens",
				Command = new RelayCommand(a => CopyOperandMDTokens((InstructionVM[])a), a => CopyOperandMDTokensCanExecute((InstructionVM[])a)),
				InputGestureText = "Ctrl+M",
				Modifiers = ModifierKeys.Control,
				Key = Key.M,
			});
			Add(new ContextMenuHandler {
				Header = "C_opy RVA",
				HeaderPlural = "C_opy RVAs",
				Command = new RelayCommand(a => CopyInstructionRVA((InstructionVM[])a), a => CopyInstructionRVACanExecute((InstructionVM[])a)),
				InputGestureText = "Ctrl+R",
				Modifiers = ModifierKeys.Control,
				Key = Key.R,
			});
			Add(new ContextMenuHandler {
				Header = "Copy File Offset",
				HeaderPlural = "Copy File Offsets",
				Command = new RelayCommand(a => CopyInstructionFileOffset((InstructionVM[])a), a => CopyInstructionFileOffsetCanExecute((InstructionVM[])a)),
				InputGestureText = "Ctrl+F",
				Modifiers = ModifierKeys.Control,
				Key = Key.F,
			});
		}

		void CopyOffsets(uint baseOffset, InstructionVM[] instrs)
		{
			var sb = new StringBuilder();

			int lines = 0;
			for (int i = 0; i < instrs.Length; i++) {
				if (lines++ > 0)
					sb.AppendLine();
				sb.Append(string.Format("0x{0:X8}", baseOffset + instrs[i].Offset));
			}
			if (lines > 1)
				sb.AppendLine();

			var text = sb.ToString();
			if (text.Length > 0)
				Clipboard.SetText(text);
		}

		void CopyInstructionRVA(InstructionVM[] instrs)
		{
			CopyOffsets(cilBodyVM.RVA.Value, instrs);
		}

		bool CopyInstructionRVACanExecute(InstructionVM[] instrs)
		{
			return !cilBodyVM.RVA.HasError && instrs.Length > 0;
		}

		void CopyInstructionFileOffset(InstructionVM[] instrs)
		{
			CopyOffsets(cilBodyVM.FileOffset.Value, instrs);
		}

		bool CopyInstructionFileOffsetCanExecute(InstructionVM[] instrs)
		{
			return !cilBodyVM.FileOffset.HasError && instrs.Length > 0;
		}

		void CopyOperandMDTokens(InstructionVM[] instrs)
		{
			var sb = new StringBuilder();

			int lines = 0;
			for (int i = 0; i < instrs.Length; i++) {
				uint? token = GetOperandMDToken(instrs[i].InstructionOperandVM);
				if (token == null)
					continue;

				if (lines++ > 0)
					sb.AppendLine();
				sb.Append(string.Format("0x{0:X8}", token.Value));
			}
			if (lines > 1)
				sb.AppendLine();

			var text = sb.ToString();
			if (text.Length > 0)
				Clipboard.SetText(text);
		}

		bool CopyOperandMDTokensCanExecute(InstructionVM[] instrs)
		{
			return instrs.Any(a => GetOperandMDToken(a.InstructionOperandVM) != null);
		}

		static uint? GetOperandMDToken(InstructionOperandVM op)
		{
			switch (op.InstructionOperandType) {
			case InstructionOperandType.None:
			case InstructionOperandType.SByte:
			case InstructionOperandType.Byte:
			case InstructionOperandType.Int32:
			case InstructionOperandType.Int64:
			case InstructionOperandType.Single:
			case InstructionOperandType.Double:
			case InstructionOperandType.String:
			case InstructionOperandType.BranchTarget:
			case InstructionOperandType.SwitchTargets:
			case InstructionOperandType.Local:
			case InstructionOperandType.Parameter:
				return null;

			case InstructionOperandType.Field:
			case InstructionOperandType.Method:
			case InstructionOperandType.Token:
			case InstructionOperandType.Type:
				var token = op.Other as IMDTokenProvider;
				return token == null ? (uint?)null : token.MDToken.ToUInt32();

			case InstructionOperandType.MethodSig:
				var msig = op.Other as MethodSig;
				return msig == null ? (uint?)null : msig.OriginalToken;

			default:
				throw new InvalidOperationException();
			}
		}

		void coll_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
				InitializeInstructions(e.NewItems);
		}

		void InitializeInstructions(System.Collections.IList list)
		{
			foreach (InstructionVM instr in list)
				instr.InstructionOperandVM.EditOperand = this;
		}

		protected override void CopyItemsAsText(InstructionVM[] instrs)
		{
			Array.Sort(instrs, (a, b) => a.Index.CompareTo(b.Index));
			CopyItemsAsTextToClipboard(instrs);
		}

		public static void CopyItemsAsTextToClipboard(InstructionVM[] instrs)
		{
			var output = new PlainTextOutput();

			for (int i = 0; i < instrs.Length; i++) {
				if (i > 0)
					output.WriteLine();

				var instr = instrs[i];
				output.Write(instr.Index.ToString(), TextTokenType.Number);
				output.Write('\t', TextTokenType.Text);
				output.Write(string.Format("{0:X4}", instr.Offset), TextTokenType.Label);
				output.Write('\t', TextTokenType.Text);
				output.Write(instr.Code.ToOpCode().ToString(), TextTokenType.OpCode);

				switch (instr.InstructionOperandVM.InstructionOperandType) {
				case InstructionOperandType.None:
					break;

				case InstructionOperandType.SByte:
					output.Write('\t', TextTokenType.Text);
					output.Write(instr.InstructionOperandVM.SByte.StringValue, TextTokenType.Number);
					break;

				case InstructionOperandType.Byte:
					output.Write('\t', TextTokenType.Text);
					output.Write(instr.InstructionOperandVM.Byte.StringValue, TextTokenType.Number);
					break;

				case InstructionOperandType.Int32:
					output.Write('\t', TextTokenType.Text);
					output.Write(instr.InstructionOperandVM.Int32.StringValue, TextTokenType.Number);
					break;

				case InstructionOperandType.Int64:
					output.Write('\t', TextTokenType.Text);
					output.Write(instr.InstructionOperandVM.Int64.StringValue, TextTokenType.Number);
					break;

				case InstructionOperandType.Single:
					output.Write('\t', TextTokenType.Text);
					output.Write(instr.InstructionOperandVM.Single.StringValue, TextTokenType.Number);
					break;

				case InstructionOperandType.Double:
					output.Write('\t', TextTokenType.Text);
					output.Write(instr.InstructionOperandVM.Double.StringValue, TextTokenType.Number);
					break;

				case InstructionOperandType.String:
					output.Write('\t', TextTokenType.Text);
					output.Write(instr.InstructionOperandVM.String.StringValue, TextTokenType.String);
					break;

				case InstructionOperandType.Field:
				case InstructionOperandType.Method:
				case InstructionOperandType.Token:
				case InstructionOperandType.Type:
				case InstructionOperandType.MethodSig:
				case InstructionOperandType.BranchTarget:
				case InstructionOperandType.SwitchTargets:
				case InstructionOperandType.Local:
				case InstructionOperandType.Parameter:
					output.Write('\t', TextTokenType.Text);
					BodyUtils.WriteObject(output, instr.InstructionOperandVM.Value);
					break;

				default: throw new InvalidOperationException();
				}
			}
			if (instrs.Length > 1)
				output.WriteLine();

			Clipboard.SetText(output.ToString());
		}

		[Flags]
		enum MenuCommandFlags
		{
			FieldDef		= 0x00000001,
			FieldMemberRef	= 0x00000002,
			MethodDef		= 0x00000004,
			MethodMemberRef	= 0x00000008,
			MethodSpec		= 0x00000010,
			TypeDef			= 0x00000020,
			TypeRef			= 0x00000040,
			TypeSpec		= 0x00000080,
		}

		void ShowMenu(object parameter, InstructionOperandVM opvm, MenuCommandFlags flags)
		{
			var ctxMenu = new ContextMenu();

			MenuItem menuItem;
			if ((flags & (MenuCommandFlags.TypeDef | MenuCommandFlags.TypeRef)) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = "_Type...",
					Command = new RelayCommand(a => AddType(opvm)),
				});
				MainWindow.CreateMenuItemImage(menuItem, typeof(MethodBodyControl).Assembly, "Class", BackgroundType.ContextMenuItem, true);
			}
			if ((flags & MenuCommandFlags.TypeSpec) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = "Type_Spec...",
					Command = new RelayCommand(a => AddTypeSpec(opvm)),
				});
				MainWindow.CreateMenuItemImage(menuItem, typeof(MethodBodyControl).Assembly, "Generic", BackgroundType.ContextMenuItem, true);
			}
			if ((flags & MenuCommandFlags.MethodDef) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = "_Method...",
					Command = new RelayCommand(a => AddMethodDef(opvm)),
				});
				MainWindow.CreateMenuItemImage(menuItem, typeof(MethodBodyControl).Assembly, "Method", BackgroundType.ContextMenuItem, true);
			}
			if ((flags & MenuCommandFlags.MethodMemberRef) != 0) {
				ctxMenu.Items.Add(new MenuItem() {
					Header = "M_ethod MemberRef...",
					Command = new RelayCommand(a => AddMethodMemberRef(opvm)),
				});
			}
			if ((flags & MenuCommandFlags.MethodSpec) != 0) {
				ctxMenu.Items.Add(new MenuItem() {
					Header = "Met_hodSpec...",
					Command = new RelayCommand(a => AddMethodSpec(opvm)),
				});
			}
			if ((flags & MenuCommandFlags.FieldDef) != 0) {
				ctxMenu.Items.Add(menuItem = new MenuItem() {
					Header = "_Field...",
					Command = new RelayCommand(a => AddFieldDef(opvm)),
				});
				MainWindow.CreateMenuItemImage(menuItem, typeof(MethodBodyControl).Assembly, "Field", BackgroundType.ContextMenuItem, true);
			}
			if ((flags & MenuCommandFlags.FieldMemberRef) != 0) {
				ctxMenu.Items.Add(new MenuItem() {
					Header = "F_ield MemberRef...",
					Command = new RelayCommand(a => AddFieldMemberRef(opvm)),
				});
			}

			ctxMenu.Placement = PlacementMode.Bottom;
			ctxMenu.PlacementTarget = parameter as UIElement;
			ctxMenu.IsOpen = true;
		}

		void AddFieldDef(InstructionOperandVM opvm)
		{
			var picker = new DnlibTypePicker(Window.GetWindow(listBox));
			var field = picker.GetDnlibType(new FlagsTreeViewNodeFilter(VisibleMembersFlags.FieldDef), opvm.Other as IField, cilBodyVM.OwnerModule);
			if (field != null)
				opvm.Other = field;
		}

		void AddFieldMemberRef(InstructionOperandVM opvm)
		{
			MemberRef mr = opvm.Other as MemberRef;
			var fd = opvm.Other as FieldDef;
			if (fd != null)
				mr = cilBodyVM.OwnerModule.Import(fd);
			if (mr != null && mr.FieldSig == null)
				mr = null;
			AddMemberRef(opvm, mr, true);
		}

		void AddMethodDef(InstructionOperandVM opvm)
		{
			var picker = new DnlibTypePicker(Window.GetWindow(listBox));
			var method = picker.GetDnlibType(new FlagsTreeViewNodeFilter(VisibleMembersFlags.MethodDef), opvm.Other as IMethod, cilBodyVM.OwnerModule);
			if (method != null)
				opvm.Other = method;
		}

		void AddMethodMemberRef(InstructionOperandVM opvm)
		{
			MemberRef mr = opvm.Other as MemberRef;
			var md = opvm.Other as MethodDef;
			var ms = opvm.Other as MethodSpec;
			if (ms != null) {
				mr = ms.Method as MemberRef;
				md = ms.Method as MethodDef;
			}
			if (md != null)
				mr = cilBodyVM.OwnerModule.Import(md);
			if (mr != null && mr.MethodSig == null)
				mr = null;
			AddMemberRef(opvm, mr, false);
		}

		void AddMemberRef(InstructionOperandVM opvm, MemberRef mr, bool isField)
		{
			var opts = mr == null ? new MemberRefOptions() : new MemberRefOptions(mr);
			var vm = new MemberRefVM(opts, cilBodyVM.TypeSigCreatorOptions, isField);
			var creator = new EditMemberRef(Window.GetWindow(listBox));
			var title = isField ? "Edit Field MemberRef" : "Edit Method MemberRef";
			vm = creator.Edit(title, vm);
			if (vm == null)
				return;

			opvm.Other = vm.CreateMemberRefOptions().Create(cilBodyVM.OwnerModule);
		}

		void AddMethodSpec(InstructionOperandVM opvm)
		{
			var ms = opvm.Other as MethodSpec;
			var opts = ms == null ? new MethodSpecOptions() : new MethodSpecOptions(ms);
			var vm = new MethodSpecVM(opts, cilBodyVM.TypeSigCreatorOptions);
			var creator = new EditMethodSpec(Window.GetWindow(listBox));
			vm = creator.Edit("Edit MethodSpec", vm);
			if (vm == null)
				return;

			opvm.Other = vm.CreateMethodSpecOptions().Create(cilBodyVM.OwnerModule);
		}

		void AddType(InstructionOperandVM opvm)
		{
			var picker = new DnlibTypePicker(Window.GetWindow(listBox));
			var type = picker.GetDnlibType(new FlagsTreeViewNodeFilter(VisibleMembersFlags.TypeDef), opvm.Other as ITypeDefOrRef, cilBodyVM.OwnerModule);
			if (type != null)
				opvm.Other = type;
		}

		void AddTypeSpec(InstructionOperandVM opvm)
		{
			var creator = new TypeSigCreator(Window.GetWindow(listBox));
			var opts = cilBodyVM.TypeSigCreatorOptions.Clone("Create a TypeSpec");
			bool canceled;
			var newSig = creator.Create(opts, (opvm.Other as ITypeDefOrRef).ToTypeSig(), out canceled);
			if (canceled)
				return;

			opvm.Other = newSig.ToTypeDefOrRef();
		}

		void EditMethodSig(InstructionOperandVM opvm)
		{
			var creator = new CreateMethodPropertySig(Window.GetWindow(listBox));
			var opts = new MethodSigCreatorOptions(cilBodyVM.TypeSigCreatorOptions.Clone("Create MethodSig"));
			opts.CanHaveSentinel = true;
			var sig = (MethodSig)creator.Create(opts, opvm.Other as MethodSig);
			if (sig != null)
				opvm.Other = sig;
		}

		void EditSwitchOperand(InstructionOperandVM opvm)
		{
			var data = new SwitchOperandVM(cilBodyVM.InstructionsListVM, opvm.Other as InstructionVM[]);
			var win = new SwitchOperandDlg();
			win.DataContext = data;
			win.Owner = Window.GetWindow(listBox) ?? MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			opvm.Other = data.GetSwitchList();
		}

		void IEditOperand.Edit(object parameter, InstructionOperandVM opvm)
		{
			MenuCommandFlags flags;
			switch (opvm.InstructionOperandType) {
			case InstructionOperandType.Field:
				flags = MenuCommandFlags.FieldDef | MenuCommandFlags.FieldMemberRef;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.Method:
				flags = MenuCommandFlags.MethodDef | MenuCommandFlags.MethodMemberRef | MenuCommandFlags.MethodSpec;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.Token:
				flags = MenuCommandFlags.FieldDef | MenuCommandFlags.FieldMemberRef |
						MenuCommandFlags.MethodDef | MenuCommandFlags.MethodMemberRef | MenuCommandFlags.MethodSpec |
						MenuCommandFlags.TypeDef | MenuCommandFlags.TypeRef | MenuCommandFlags.TypeSpec;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.Type:
				flags = MenuCommandFlags.TypeDef | MenuCommandFlags.TypeRef | MenuCommandFlags.TypeSpec;
				ShowMenu(parameter, opvm, flags);
				break;

			case InstructionOperandType.MethodSig:
				EditMethodSig(opvm);
				break;

			case InstructionOperandType.SwitchTargets:
				EditSwitchOperand(opvm);
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		public void Select(IEnumerable<InstructionVM> items)
		{
			var instrs = items.ToArray();
			if (instrs.Length == 0)
				return;
			listBox.SelectedItems.Clear();
			foreach (var instr in instrs)
				listBox.SelectedItems.Add(instr);

			// Select the last one because the selected item is usually the last visible item in the view.
			listBox.ScrollIntoView(instrs[instrs.Length - 1]);
		}
	}
}
