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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.Decompiler;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class ExceptionHandlersListHelper : ListBoxHelperBase<ExceptionHandlerVM> {
		readonly TypeSigCreator typeSigCreator;

		public ExceptionHandlersListHelper(ListView listView, Window ownerWindow)
			: base(listView, "Exception Handler") {
			this.typeSigCreator = new TypeSigCreator(ownerWindow);
		}

		protected override ExceptionHandlerVM[] GetSelectedItems() {
			return listBox.SelectedItems.Cast<ExceptionHandlerVM>().ToArray();
		}

		protected override void OnDataContextChangedInternal(object dataContext) {
			this.coll = ((MethodBodyVM)dataContext).CilBodyVM.ExceptionHandlersListVM;
			this.coll.CollectionChanged += coll_CollectionChanged;
			InitializeExceptionHandlers(this.coll);

			AddStandardMenuHandlers("AddException");
			Add(new ContextMenuHandler {
				Header = "Copy _MD Token",
				HeaderPlural = "Copy _MD Tokens",
				Command = new RelayCommand(a => CopyCatchTypeMDTokens((ExceptionHandlerVM[])a), a => CopyCatchTypeMDTokensCanExecute((ExceptionHandlerVM[])a)),
				InputGestureText = "Ctrl+M",
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
			if (text.Length > 0)
				Clipboard.SetText(text);
		}

		bool CopyCatchTypeMDTokensCanExecute(ExceptionHandlerVM[] ehs) {
			return ehs.Any(a => GetCatchTypeToken(a.CatchType) != null);
		}

		static uint? GetCatchTypeToken(ITypeDefOrRef type) {
			return type == null ? (uint?)null : type.MDToken.Raw;
		}

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
				output.Write(eh.Index.ToString(), TextTokenType.Number);
				output.Write("\t", TextTokenType.Text);
				BodyUtils.WriteObject(output, eh.TryStartVM.SelectedItem);
				output.Write("\t", TextTokenType.Text);
				BodyUtils.WriteObject(output, eh.TryEndVM.SelectedItem);
				output.Write("\t", TextTokenType.Text);
				BodyUtils.WriteObject(output, eh.FilterStartVM.SelectedItem);
				output.Write("\t", TextTokenType.Text);
				BodyUtils.WriteObject(output, eh.HandlerStartVM.SelectedItem);
				output.Write("\t", TextTokenType.Text);
				BodyUtils.WriteObject(output, eh.HandlerEndVM.SelectedItem);
				output.Write("\t", TextTokenType.Text);
				output.Write(((EnumVM)eh.HandlerTypeVM.Items[eh.HandlerTypeVM.SelectedIndex]).Name, TextTokenType.Text);
				output.Write("\t", TextTokenType.Text);
				BodyUtils.WriteObject(output, eh.CatchType);
			}
			if (ehs.Length > 1)
				output.WriteLine();

			Clipboard.SetText(output.ToString());
		}
	}
}
