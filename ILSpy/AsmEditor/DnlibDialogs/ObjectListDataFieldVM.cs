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
using System.Linq;
using System.Windows.Input;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class ObjectListDataFieldVM : DataFieldVM<IList<object>>
	{
		readonly TypeSigCreatorOptions options;
		List<object> objects = new List<object>();

		public ICreateConstantType CreateConstantType {
			set { createConstantType = value; }
		}
		ICreateConstantType createConstantType;

		public ICommand AddObjectCommand {
			get { return new RelayCommand(a => AddObject()); }
		}

		public ICommand RemoveObjectCommand {
			get { return new RelayCommand(a => RemoveObject(), a => RemoveObjectCanExecute()); }
		}

		public ICommand ClearObjectsCommand {
			get { return new RelayCommand(a => ClearObjects(), a => ClearObjectsCanExecute()); }
		}

		public ObjectListDataFieldVM(Action<DataFieldVM> onUpdated, TypeSigCreatorOptions options)
			: this(new object[0], onUpdated, options)
		{
		}

		public ObjectListDataFieldVM(IList<object> value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions options)
			: base(onUpdated)
		{
			if (options != null) {
				this.options = options.Clone("Create a Type");
				this.options.NullTypeSigAllowed = true;
			}
			SetValue(value);
		}

		protected override void SetValue(IList<object> value)
		{
			this.objects.Clear();
			if (value != null)
				this.objects.AddRange(value);
		}

		void InitializeStringValue()
		{
			this.StringValue = string.Join(", ", objects.Select(a => a == null ? "null" : a.ToString()));
		}

		protected override string ConvertToValue(out IList<object> value)
		{
			value = objects.ToArray();
			return null;
		}

		static readonly ConstantType[] ConstantsNoArray = new ConstantType[] {
			ConstantType.Boolean,
			ConstantType.Char,
			ConstantType.SByte,
			ConstantType.Int16,
			ConstantType.Int32,
			ConstantType.Int64,
			ConstantType.Byte,
			ConstantType.UInt16,
			ConstantType.UInt32,
			ConstantType.UInt64,
			ConstantType.Single,
			ConstantType.Double,
			ConstantType.String,
			ConstantType.Enum,
			ConstantType.Type,
		};

		void AddObject()
		{
			if (createConstantType == null)
				throw new InvalidOperationException();
			bool canceled;
			var newObject = createConstantType.Create(null, ConstantsNoArray, true, true, options, out canceled);
			if (canceled)
				return;

			objects.Add(newObject);
			InitializeStringValue();
		}

		void RemoveObject()
		{
			if (!RemoveObjectCanExecute())
				return;

			objects.RemoveAt(objects.Count - 1);
			InitializeStringValue();
		}

		bool RemoveObjectCanExecute()
		{
			return objects.Count > 0;
		}

		void ClearObjects()
		{
			if (!ClearObjectsCanExecute())
				return;

			objects.Clear();
			InitializeStringValue();
		}

		bool ClearObjectsCanExecute()
		{
			return objects.Count > 0;
		}
	}
}
