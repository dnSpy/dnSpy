#!/usr/bin/env ruby

require 'erb'

class Language

	attr_reader(:name)
	attr_reader(:compiler)
	attr_reader(:ext)
	attr_reader(:command)

	def initialize(name, compiler, ext, command)
		@name = name
		@compiler = compiler
		@ext = ext
		@command = command
	end
end

class TestCase

	attr_reader(:lang)
	attr_reader(:file)

	def initialize(lang, file)
		@lang = lang
		@file = file
	end

	def method()
		meth = @file.gsub(/[^a-zA-Z1-9]/, "_")
		return meth[1..meth.length] if (meth[0].chr == "_")
		return meth
	end
end

$languages = [
	Language.new("cil", "ilasm", ".il", "{0} /exe /output:{2} {1}"),
	Language.new("csharp", "mcs", ".cs", "{0} /t:exe /o:{2} {1}")
]

$tests = [
]

def analyze(dir)
	$languages.each { |l|
		pattern = File.join(dir, "**", "*" + l.ext)
		Dir[pattern].each { |file|
			$tests.push(TestCase.new(l, File.expand_path(file)))
		}
	}
end

ARGV.each { |dir|
	analyze(dir)
}

if $tests.length > 0
	erb = ERB.new(IO.read("./templates/Tests.cs"))
	print(erb.result)
end
