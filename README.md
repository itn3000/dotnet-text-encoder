# dotnet-text-encoder

dotnet global tool for changing text encoding.
[nuget package](https://www.nuget.org/packages/dotnet-text-encoder/)

# Requirements

* [dotnet sdk 8.0 or later](https://dotnet.microsoft.com/download)

# Install

1. do following command
    `dotnet tool install -g dotnet-text-encoder`
2. ensure adding `$HOME/.dotnet/tools` to PATH environment

and then you can execute command by `dtenc`

## Use native binary

you can use this tool without dotnet-SDK by native binary.
binaries can be found in [release page](https://github.com/itn3000/dotnet-text-encoder/releases)

# Usage

## Basic Usage

here is the help output

```
dtenc 2.0.0.0

Usage: dtenc [options] [command]

Options:
  --version         Show version information
  -f|--from         input file encoding(default: UTF-8)
  -t|--to           output file encoding(default: UTF-8)
  -i|--input        input file path(default: standard input)
  -o|--output       output file path(default: standard output)
  -p|--preamble     enable output preamble(=BOM) if exists
  -e|--eol          converting end of line(cr,crlf,lf,none: default=none)
  -?|-h|--help      Show help information

Commands:
  getinfo           

Run 'dtenc [command] --help' for more information about a command.

changing text encoding
Examples:
input utf-8,output shift_jis(cp932) by name:
  dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt
input utf-8,output shift_jis(cp932) by code page
  dotnet tenc -f 65001 -t 932 -i utf8.txt -o sjis.txt
input utf-8,output utf-8 without BOM(BOM disabled by default)
  dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt
```

### Powershell Warning

Because of powershell spec, using pipeline may cause garbling.
```
dtenc -i some.txt -f utf-8 -t euc-jp > out.txt
```
To avoid this, you should use `-o` option.
```
dtenc -i some.txt -f utf-8 -t euc-jp -o out.txt
```

## Getting encoding info

Since 0.2.0, `getinfo` subcommand is added.
This command gets the information of specified encodings.

Here is the help text

```
Usage: dtenc getinfo [options]

Options:
  -n|--name        encoding names
  -c|--codepage    code page range(number separated by '-', number is must be 0 - 65535)
  -s|--show-fault  show fault result
  -?|-h|--help     Show help information

getting encoding info and output by CSV format

Examples:
try get info by name(multiple select is allowed):
    dtenc getinfo -n shift_jis -n utf-8
try get info by codepage number(single)
    dtenc getinfo -c 932
try get info by codepage number(range, multiple select is allowed)
    dtenc getinfo -c 0-1000 -c 2000-3000
```

and then command will output following CSV format

```
> dtenc getinfo -n utf-8
Name,CodePage,Found,DisplayName,Preamble
utf-8,65001,True,Unicode (UTF-8),efbbbf
```

## run overwrite mode

since 1.0.0, `overwrite`(in short, `ow`) subcommand added.
Here is the help text:

```
convert files with overwrite mode

Usage: dtenc overwrite [options] <Targets>

Arguments:
  Targets           target files, you can use globbing(*.txt, **/*.cs)

Options:
  -?|-h|--help      Show help information
  -f|--from         input file encoding(default: UTF-8)
  -t|--to           output file encoding(default: UTF-8)
  -b|--base         search base directory(default: current directory)
  -i|--ignore-case  search file with case insensitive
  -p|--preamble     enable output preamble(=BOM) if exists
  -e|--eol          converting end of line(cr,crlf,lf,none: default=none)
  --dry-run         do not convert file
  -x|--exclude      file exclude pattern, you can use globbing

Examples:
all files that have '.txt' extension are targeted.
  dtenc ow -f utf-8 -t utf-8 -e lf **/*.txt
all files that have '.txt' extension under the 'targetdir' are targeted.
  dtenc ow -f utf-8 -t utf-8 -e lf **/*.txt -b targetdir
all files that have '.txt' extension are targeted,excluding under the 'sub' directory
  dtenc ow -f utf-8 -t utf-8 -e lf **/*.txt -x sub/**/*
```

# Build

1. run `dotnet build` to build binary
2. run `dotnet pack` to creating nuget package

you can test local nuget package by following command.

`dotnet tool install --tool-path [installdir] --add-source [path to nupkg directory] dotnet-text-encoder`

After executing, executable files are place in `[installdir]`.

## Build Native Binary

### Prerequisits

if you are trying to build on ubuntu, you must install following.

|platform|needed|
|--------|------|
|ubuntu  |clang,libkrb5-dev|
|osx     |clang,xcode|
|windows |visual studio 2017 or later|

### Building

run `dotnet publish -c [Debug or Release] -p:PublishAot=true -p:PackAsTool=false -r [rid]`.
and the native binary will be created in `artifacts/publish/dotnet-text-encoder/release_[rid]`

if you get following error message about clang, you can avoid this error by setting `CppCompilerAndLinker=[clang command]` to environment variable.

```
error : Platform linker ('clang-3.9') not found. Try installing clang-3.9 or the appropriate package for your platform to resolve the problem.
```

available rids are listed in [Microsoft's official document](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog).
**Warning: currently, cross compiling is not supported, so OS part of rid should be same as build machine platform**

# Release Notes

## 2.1.0

* update to dotnet10.0

## 2.0.0.1

* update to dotnet8
* publish by NativeAOT
* **BREAKING:change binary name to "dtenc"**
* **BREAKING:disable BOM by default**
    * remove `-n|--no-preamble`, add `-p|--preamble`

## 1.1.1.1

* fix failing parsing EOL

## 1.1.0

* add `--dry-run` option

## 1.0.1

* add native file to release

## 1.0.0

* add `overwrite` subcommand

## 0.3.0

* add `-e`(convert eol) option

## 0.2.1

* add `getinfo` subcommand

## 0.1.0

* first release

