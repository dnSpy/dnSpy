#!/usr/bin/env ruby

$providers = []

$mapping = {
	"Assem" => "IsAssembly",
	"FamORAssem" => "IsFamilyOrAssembly",
	"FamANDAssem" => "IsFamilyAndAssembly",
	"NestedFamORAssem" => "IsNestedFamilyOrAssembly",
	"NestedFamANDAssem" => "IsNestedFamilyAndAssembly",
	"RTSpecialName" => "IsRuntimeSpecialName",
	"Compilercontrolled" => "IsCompilerControlled",
	"NotNullableValueTypeConstraint" => "HasNotNullableValueTypeConstraint",
	"ReferenceTypeConstraint" => "HasReferenceTypeConstraint",
	"DefaultConstructorConstraint" => "HasDefaultConstructorConstraint",
	"SupportsLastError" => "SupportsLastError",
	"NoInlining" => "NoInlining",
}

$black_list = [
	"None", "Unused", "RequireSecObject",
	"HasFieldMarshal", "HasFieldRVA",
	"OPTIL", "MaxMethodImplVal"
]

class Attribute

	attr_accessor(:name)
	attr_accessor(:type)

	def initialize(name, type)
		@name = name
		@type = type
	end

	def full_name
		"#{@type}.#{@name}"
	end

	def to_csharp
"""		public bool #{get_name()} {
#{getter()}#{setter()}		}
"""
	end

	def to_s
		"Attribute #{full_name}"
	end

	protected
	def getter
"""			get { return (m_attributes & #{full_name()}) != 0; }
"""
	end

	def setter
"""			set {
				if (value)
					m_attributes |= #{full_name()};
				else
					m_attributes &= ~#{full_name()};
			}
"""
	end

	private
	def get_name
		name = @name
		return $mapping[name] if $mapping.include?(name)
		return name if name.rindex("Has") == 0
		"Is#{name}"
	end
end

class MaskedAttribute < Attribute

	attr_accessor(:mask)

	def initialize(name, type, mask)
		super(name, type)
		@mask = mask
	end

	def	mask_full_name
		"#{@type}.#{@mask}"
	end

	def to_s
		"MaskedAttribute #{full_name()}, masked by #{mask_full_name()}"
	end

	protected
	def getter
"""			get { return (m_attributes & #{mask_full_name()}) == #{full_name()}; }
"""
	end

	def setter
"""			set {
				if (value) {
					m_attributes &= ~#{mask_full_name()};
					m_attributes |= #{full_name()};
				} else
					m_attributes &= ~(#{mask_full_name()} & #{full_name()});
			}
"""
	end
end

class Provider

	attr_accessor(:type)
	attr_accessor(:attributes_file)
	attr_accessor(:target_file)
	attr_accessor(:attributes)

	def initialize(type, attributes_file, target_file)
		@type = type
		@attributes_file = attributes_file
		@target_file = target_file
		@attributes = []
	end

	def parse
		f = File.open(@attributes_file, File::RDONLY)
		have_mask = false
		mask = ""
		f.readlines.each { |line|
			if line.chomp().length == 0
				have_mask = false
			else
				name = get_name(line)
				if not name.nil? and name.index("Mask")
					mask = name
					have_mask = true
				elsif not name.nil?
					attr = nil
					if (have_mask)
						attr = MaskedAttribute.new(name, @type, mask)
					else
						attr = Attribute.new(name, @type)
					end
					@attributes << attr
				end
			end
		}
		f.close
	end

	def patch
		buffer = read_target_content()
		buffer = patch_buffer(buffer)
		if buffer != read_target_content()
			puts("#{@target_file} patched")
			write_target_content (buffer)
		end
	end

	private
	def patch_buffer(buffer)
		region = "#region " + @type
		endregion = "#endregion"

		endpart = buffer[(buffer.index(endregion) - 3)..buffer.length]

		rep = buffer[0..(buffer.index(region) + region.length)]

		rep += "\n"

		@attributes.each { |attr|
			rep += attr.to_csharp
			rep += "\n" if attr != @attributes.last
		}

		rep += endpart

		rep
	end

	def read_target_content
		f = File.new(@target_file, File::RDONLY)
		content = f.readlines.join
		f.close
		content
	end

	def write_target_content(content)
		File.open(@target_file, File::WRONLY | File::TRUNC) { |f|
			f.write(content)
		}
	end

	def get_name(line)
		pos = line.index("=")
		return nil if not pos
		name = line[0..(pos - 1)].strip

		return nil if $black_list.include?(name)
		name
	end
end

def to_cecil_file(file)
	"../Mono.Cecil/#{file}.cs"
end

[ "Event", "Field", "Method", "Parameter", "Property", "Type" ].each { |name|
	attributes = "#{name}Attributes"
	definition = "#{name}Definition"
	$providers << Provider.new(attributes, to_cecil_file(attributes), to_cecil_file(definition))
}

{ "GenericParameter" => "GenericParameter",
  "ManifestResource" => "Resource",
  "PInvoke" => "PInvokeInfo",
#  "MethodImpl" => "MethodDefinition",
#  "MethodSemantics" => "MethodDefinition",
}.each { |k, v|
	attributes = "#{k}Attributes"
	$providers << Provider.new(attributes, to_cecil_file(attributes), to_cecil_file(v))
}

$providers.each { |p|
	p.parse
	p.patch
}
