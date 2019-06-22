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

using System.Collections.Generic;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation.ViewModel {
	abstract class VariablesWindowOperations {
		public abstract bool CanCopy(IValueNodesVM vm);
		public abstract void Copy(IValueNodesVM vm);
		public abstract bool CanCopyExpression(IValueNodesVM vm);
		public abstract void CopyExpression(IValueNodesVM vm);
		public abstract bool SupportsPaste(IValueNodesVM vm);
		public abstract bool CanPaste(IValueNodesVM vm);
		public abstract void Paste(IValueNodesVM vm);
		public abstract bool SupportsDeleteWatch(IValueNodesVM vm);
		public abstract bool CanDeleteWatch(IValueNodesVM vm);
		public abstract void DeleteWatch(IValueNodesVM vm);
		public abstract bool SupportsClearAll(IValueNodesVM vm);
		public abstract bool CanClearAll(IValueNodesVM vm);
		public abstract void ClearAll(IValueNodesVM vm);
		public abstract bool CanSelectAll(IValueNodesVM vm);
		public abstract void SelectAll(IValueNodesVM vm);
		public abstract bool CanEdit(IValueNodesVM vm);
		public abstract void Edit(IValueNodesVM vm);
		public abstract void Edit(IValueNodesVM vm, string text);
		public abstract bool SupportsEditExpression(IValueNodesVM vm);
		public abstract bool CanEditExpression(IValueNodesVM vm);
		public abstract void EditExpression(IValueNodesVM vm);
		public abstract void EditExpression(IValueNodesVM vm, string text);
		public abstract bool CanEditValue(IValueNodesVM vm);
		public abstract void EditValue(IValueNodesVM vm);
		public abstract void EditValue(IValueNodesVM vm, string text);
		public abstract bool CanCopyValue(IValueNodesVM vm);
		public abstract void CopyValue(IValueNodesVM vm);
		public abstract bool CanAddWatch(IValueNodesVM vm);
		public abstract void AddWatch(IValueNodesVM vm);
		public abstract bool IsMakeObjectIdVisible(IValueNodesVM vm);
		public abstract bool CanMakeObjectId(IValueNodesVM vm);
		public abstract void MakeObjectId(IValueNodesVM vm);
		public abstract bool CanDeleteObjectId(IValueNodesVM vm);
		public abstract void DeleteObjectId(IValueNodesVM vm);
		public abstract bool CanSave(IValueNodesVM vm);
		public abstract void Save(IValueNodesVM vm);
		public abstract bool CanRefresh(IValueNodesVM vm);
		public abstract void Refresh(IValueNodesVM vm);
		public abstract bool CanShowInMemoryWindow(IValueNodesVM vm);
		public abstract void ShowInMemoryWindow(IValueNodesVM vm);
		public abstract bool CanShowInMemoryWindow(IValueNodesVM vm, int windowIndex);
		public abstract void ShowInMemoryWindow(IValueNodesVM vm, int windowIndex);
		public abstract bool CanToggleExpanded(IValueNodesVM vm);
		public abstract void ToggleExpanded(IValueNodesVM vm);
		public abstract bool CanCollapseParent(IValueNodesVM vm);
		public abstract void CollapseParent(IValueNodesVM vm);
		public abstract bool CanExpandChildren(IValueNodesVM vm);
		public abstract void ExpandChildren(IValueNodesVM vm);
		public abstract bool CanCollapseChildren(IValueNodesVM vm);
		public abstract void CollapseChildren(IValueNodesVM vm);
		public abstract IList<DbgLanguage> GetLanguages(IValueNodesVM vm);
		public abstract DbgLanguage? GetCurrentLanguage(IValueNodesVM vm);
		public abstract void SetCurrentLanguage(IValueNodesVM vm, DbgLanguage language);
		public abstract bool CanToggleUseHexadecimal { get; }
		public abstract void ToggleUseHexadecimal();
		public abstract bool UseHexadecimal { get; set; }
		public abstract bool CanToggleUseDigitSeparators { get; }
		public abstract void ToggleUseDigitSeparators();
		public abstract bool UseDigitSeparators { get; set; }
		public abstract bool ShowOnlyPublicMembers { get; set; }
		public abstract bool ShowNamespaces { get; set; }
		public abstract bool ShowIntrinsicTypeKeywords { get; set; }
		public abstract bool ShowTokens { get; set; }
	}
}
