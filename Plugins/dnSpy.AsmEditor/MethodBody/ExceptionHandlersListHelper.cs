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
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class ExceptionHandlersListHelper : ListBoxHelperBase<ExceptionHandlerVM> {
		readonly TypeSigCreator typeSigCreator;

		protected override string AddNewBeforeSelectionMessage => dnSpy_AsmEditor_Resources.EH_Command1;
		protected override string AddNewAfterSelectionMessage => dnSpy_AsmEditor_Resources.EH_Command2;
		protected override string AppendNewMessage => dnSpy_AsmEditor_Resources.EH_Command3;
		protected override string RemoveSingularMessage => dnSpy_AsmEditor_Resources.EH_Command4;
		protected override string RemovePluralMessage => dnSpy_AsmEditor_Resources.EH_Command5;
		protected override string RemoveAllMessage => dnSpy_AsmEditor_Resources.EH_Command6;

		public ExceptionHandlersListHelper(ListView listView, Window ownerWindow)
			: base(listView) {
			this.typeSigCreator = new TypeSigCreator(ownerWindow);
		}

		protected override ExceptionHandlerVM[] GetSelectedItems() => listBox.SelectedItems.Cast<ExceptionHandlerVM>().ToArray();

		protected override void OnDataContextChangedInternal(object dataContext) {
			this.coll = ((MethodBodyVM)dataContext).CilBodyVM.ExceptionHandlersListVM;
			this.coll.CollectionChanged += coll_CollectionChanged;
			InitializeExceptionHandlers(this.coll);

			AddStandardMenuHandlers("AddException");
			Add(new ContextMenuHandler {
				Header = "res:CopyMetaDataToken",
				HeaderPlural = "res:CopyMetaDataTokens",
				Command = new RelayCommand(a => CopyCatchTypeMDTokens((ExceptionHandlerVM[])a), a => CopyCatchTypeMDTokensCanExecute((ExceptionHandlerVM[])a)),
				InputGestureText = "res:ShortCutKeyCtrlM",
				Modifiers = ModifierKeys.Control,
				Key = Key.M,
			});
		}

		void CopyCatchTypeMDTokens(ExceptionHandlerVM[] ehs) {
			var sb = new StringBuilder();

			int lines = 0;
			for (int i = 0; i < ehs.Length; i++) {
				uint? token = GetCatchTypeToken(ehs[i].CatchType);
				if (token == null)
					continue;

				if (lines++ > 0)
					sb.AppendLine();
				sb.Append(string.Format("0x{0:X8}", token.Value));
			}
			if (lines > 1)
				sb.AppendLine();

			var text = sb.ToString();
			if (text.Length > 0) {
				try {
					Clipboard.SetText(text);
				}
				catch (ExternalException) { }
			}
		}

		bool CopyCatchTypeMDTokensCanExecute(ExceptionHandlerVM[] ehs) => ehs.Any(a => GetCatchTypeToken(a.CatchType) != null);
		static uint? GetCatchTypeToken(ITypeDefOrRef type) => type == null ? (uint?)null : type.MDToken.Raw;

		void coll_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems != null)
				InitializeExceptionHandlers(e.NewItems);
		}

		void InitializeExceptionHandlers(System.Collections.IList list) {
			foreach (ExceptionHandlerVM eh in list)
				eh.TypeSigCreator = typeSigCreator;
		}

		protected override void CopyItemsAsText(ExceptionHandlerVM[] ehs) {
			Array.Sort(ehs, (a, b) => a.Index.CompareTo(b.Index));

			var output = new NoSyntaxHighlightOutput();

			for (int i = 0; i < ehs.Length; i++) {
				if (i > 0)
					output.WriteLine();

				var eh = ehs[i];
				output.Write(eh.Index.ToString(), BoxedTextTokenKind.Number);
				output.Write("\t", BoxedTextTokenKind.Text);
				BodyUtils.WriteObject(output, eh.TryStartVM.SelectedItem);
				output.Write("\t", BoxedTextTokenKind.Text);
				BodyUtils.WriteObject(output, eh.TryEndVM.SelectedItem);
				output.Write("\t", BoxedTextTokenKind.Text);
				BodyUtils.WriteObject(output, eh.FilterStartVM.SelectedItem);
				output.Write("\t", BoxedTextTokenKind.Text);
				BodyUtils.WriteObject(output, eh.HandlerStartVM.SelectedItem);
				output.Write("\t", BoxedTextTokenKind.Text);
				BodyUtils.WriteObject(output, eh.HandlerEndVM.SelectedItem);
				output.Write("\t", BoxedTextTokenKind.Text);
				output.Write(((EnumVM)eh.HandlerTypeVM.Items[eh.HandlerTypeVM.SelectedIndex]).Name, BoxedTextTokenKind.Text);
				output.Write("\t", BoxedTextTokenKind.Text);
				BodyUtils.WriteObject(output, eh.CatchType);
			}
			if (ehs.Length > 1)
				output.WriteLine();

			try {
				Clipboard.SetText(output.ToString());
			}
			catch (ExternalException) { }
		}
	}
}
