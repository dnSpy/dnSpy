//
// ResourceType.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	public enum ResourceType {
		Linked,
		Embedded,
		AssemblyLinked,
	}

	public abstract class Resource {

		string name;
		uint attributes;

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public ManifestResourceAttributes Attributes {
			get { return (ManifestResourceAttributes) attributes; }
			set { attributes = (uint) value; }
		}

		public abstract ResourceType ResourceType {
			get;
		}

		#region ManifestResourceAttributes

		public bool IsPublic {
			get { return attributes.GetMaskedAttributes ((uint) ManifestResourceAttributes.VisibilityMask, (uint) ManifestResourceAttributes.Public); }
			set { attributes = attributes.SetMaskedAttributes ((uint) ManifestResourceAttributes.VisibilityMask, (uint) ManifestResourceAttributes.Public, value); }
		}

		public bool IsPrivate {
			get { return attributes.GetMaskedAttributes ((uint) ManifestResourceAttributes.VisibilityMask, (uint) ManifestResourceAttributes.Private); }
			set { attributes = attributes.SetMaskedAttributes ((uint) ManifestResourceAttributes.VisibilityMask, (uint) ManifestResourceAttributes.Private, value); }
		}

		#endregion

		internal Resource (string name, ManifestResourceAttributes attributes)
		{
			this.name = name;
			this.attributes = (uint) attributes;
		}
	}
}
