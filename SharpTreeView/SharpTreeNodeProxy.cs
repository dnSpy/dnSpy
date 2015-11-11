/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ICSharpCode.TreeView {
	internal class SharpTreeNodeProxy : CustomTypeDescriptor {
		static SharpTreeNodeProxy() {
			descMap = new Dictionary<string, IPropDesc>();
			AddPropertyDesc("Foreground", node => node.Foreground);
			AddPropertyDesc("IsExpanded", node => node.IsExpanded, (node, value) => node.IsExpanded = value);
			AddPropertyDesc("IsChecked", node => node.IsChecked, (node, value) => node.IsChecked = value);
			AddPropertyDesc("ToolTip", node => node.ToolTip);
			AddPropertyDesc("Icon", node => node.Icon);
			AddPropertyDesc("Text", node => node.Text);
			AddPropertyDesc("IsEditing", node => node.IsEditing, (node, value) => node.IsEditing = value);
			AddPropertyDesc("ShowIcon", node => node.ShowIcon);
			AddPropertyDesc("ShowExpander", node => node.ShowExpander);
			AddPropertyDesc("ExpandedIcon", node => node.ExpandedIcon);
			AddPropertyDesc("IsCheckable", node => node.IsCheckable);
			AddPropertyDesc("IsCut", node => node.IsCut);
			descs = new PropertyDescriptorCollection(descMap.Values.Cast<PropertyDescriptor>().ToArray());
		}

		static readonly PropertyDescriptorCollection descs;
		static readonly Dictionary<string, IPropDesc> descMap;

		static void AddPropertyDesc<T>(string name, Func<SharpTreeNode, T> getter, Action<SharpTreeNode, T> setter = null) {
			var desc = new PropDesc<T>(name, getter, setter);
			descMap.Add(name, desc);
		}

		public SharpTreeNodeProxy(SharpTreeNode obj) {
			UpdateObject(obj);
		}

		public void UpdateObject(SharpTreeNode obj) {
			if (Object != null)
				Object.PropertyChanged -= OnPropertyChanged;

			Object = obj;

			if (obj == null)
				IsNull = true;
			else {
				IsNull = false;
				obj.PropertyChanged += OnPropertyChanged;

				foreach (var desc in descMap)
					desc.Value.OnValueChanged(this);
			}
		}

		void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
			IPropDesc desc;
			if (descMap.TryGetValue(e.PropertyName, out desc))
				desc.OnValueChanged(this);
		}

		public bool IsNull { get; private set; }
		public SharpTreeNode Object { get; private set; }

		public override PropertyDescriptorCollection GetProperties() {
			return descs;
		}

		public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) {
			return GetProperties();
		}

		interface IPropDesc {
			void OnValueChanged(object component);
		}

		class PropDesc<T> : PropertyDescriptor, IPropDesc {
			readonly Func<SharpTreeNode, T> getter;
			readonly Action<SharpTreeNode, T> setter;

			public PropDesc(string name, Func<SharpTreeNode, T> getter, Action<SharpTreeNode, T> setter)
				: base(name, null) {
				this.getter = getter;
				this.setter = setter;
			}

			public override object GetValue(object component) {
				return getter(((SharpTreeNodeProxy)component).Object);
			}

			public override bool IsReadOnly {
				get {
					return setter == null;
					;
				}
			}

			public override Type PropertyType {
				get { return typeof(T); }
			}

			public override void SetValue(object component, object value) {
				setter(((SharpTreeNodeProxy)component).Object, (T)value);
			}

			public void OnValueChanged(object component) {
				OnValueChanged(component, new PropertyChangedEventArgs(Name));
			}

			public override bool CanResetValue(object component) {
				return false;
			}

			public override bool ShouldSerializeValue(object component) {
				return false;
			}

			public override void ResetValue(object component) {
				throw new NotSupportedException();
			}

			public override Type ComponentType {
				get { return null; }
			}
		}
	}
}