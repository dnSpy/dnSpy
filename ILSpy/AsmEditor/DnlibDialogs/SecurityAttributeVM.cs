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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;
using ICSharpCode.ILSpy.TreeNodes.Filters;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class SecurityAttributeVM : ViewModelBase
	{
		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand PickAttributeTypeCommand {
			get { return new RelayCommand(a => PickAttributeType()); }
		}

		public ICommand AddNamedArgumentCommand {
			get { return new RelayCommand(a => AddNamedArgument(), a => AddNamedArgumentCanExecute()); }
		}

		public string FullName {
			get {
				var sb = new StringBuilder();
				sb.Append(AttributeType == null ? "<<<null>>>" : AttributeType.FullName);
				sb.Append('(');
				bool first = true;
				foreach (var namedArg in NamedArguments) {
					if (!first)
						sb.Append(", ");
					first = false;
					sb.Append(namedArg.ToString());
				}
				sb.Append(')');
				return sb.ToString();
			}
		}

		public ITypeDefOrRef AttributeType {
			get { return attributeType; }
			set {
				if (attributeType != value) {
					attributeType = value;
					OnPropertyChanged("AttributeType");
					OnPropertyChanged("FullName");
					HasErrorUpdated();
				}
			}
		}
		ITypeDefOrRef attributeType;

		public MyObservableCollection<CANamedArgumentVM> NamedArguments {
			get { return namedArguments; }
		}
		readonly MyObservableCollection<CANamedArgumentVM> namedArguments = new MyObservableCollection<CANamedArgumentVM>();

		readonly SecurityAttribute origSa;
		readonly TypeSigCreatorOptions typeSigOptions;
		readonly ModuleDef module;

		public SecurityAttributeVM(SecurityAttribute sa, TypeSigCreatorOptions typeSigOptions)
		{
			this.origSa = sa;
			this.module = typeSigOptions.Module;
			this.typeSigOptions = typeSigOptions;
			NamedArguments.CollectionChanged += Args_CollectionChanged;

			Reinitialize();
		}

		void Args_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Hook(e);
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void Hook(NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null) {
				foreach (INotifyPropertyChanged i in e.OldItems)
					i.PropertyChanged -= arg_PropertyChanged;
			}
			if (e.NewItems != null) {
				foreach (INotifyPropertyChanged i in e.NewItems)
					i.PropertyChanged += arg_PropertyChanged;
			}
		}

		void arg_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged("FullName");
			HasErrorUpdated();
		}

		void PickAttributeType()
		{
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newAttrType = dnlibTypePicker.GetDnlibType(new FlagsTreeViewNodeFilter(VisibleMembersFlags.TypeDef), AttributeType);
			if (newAttrType != null)
				AttributeType = newAttrType;
		}

		void AddNamedArgument()
		{
			if (!AddNamedArgumentCanExecute())
				return;
			NamedArguments.Add(new CANamedArgumentVM(new CANamedArgument(false, module.CorLibTypes.Int32, "AttributeProperty", new CAArgument(module.CorLibTypes.Int32, 0)), typeSigOptions));
		}

		bool AddNamedArgumentCanExecute()
		{
			// The named args blob length must also be at most 0x1FFFFFFF bytes but we can't verify it here
			return NamedArguments.Count < 0x1FFFFFFF;
		}

		void Reinitialize()
		{
			InitializeFrom(origSa);
		}

		void InitializeFrom(SecurityAttribute sa)
		{
			AttributeType = sa.AttributeType;
			NamedArguments.Clear();
			NamedArguments.AddRange(sa.NamedArguments.Select(a => new CANamedArgumentVM(a, typeSigOptions)));
		}

		public SecurityAttribute CreateSecurityAttribute()
		{
			var sa = new SecurityAttribute(AttributeType);
			sa.NamedArguments.AddRange(NamedArguments.Select(a => a.CreateCANamedArgument()));
			return sa;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get {
				return AttributeType == null ||
						NamedArguments.Any(a => a.HasError);
			}
		}
	}
}
