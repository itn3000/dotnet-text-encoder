# dotnet-text-encoder

dotnet global tool for changing text encoding.
[nuget package](https://www.nuget.org/packages/dotnet-text-encoder/)

# Requirements

* [dotnet sdk 2.1 or later](https://dotnet.microsoft.com/download)

# Install

1. do following command
    `dotnet tool install -g dotnet-text-encoder`
2. ensure adding `$HOME/.dotnet/tools` to PATH environment

and then you can execute command by `dotnet tenc` or `dotnet-tenc`

# Usage

## Basic Usage

here is the help output

```
dotnet-tenc 0.2.0

Usage: dotnet-tenc [options] [command]

Options:
  --version         Show version information
  -f|--from         input file encoding(default: UTF-8)
  -t|--to           output file encoding(default: UTF-8)
  -i|--input        input file path(default: standard input)
  -o|--output       output file path(default: standard output)
  -n|--no-preamble  disable output preamble(=BOM) if exists
  -?|-h|--help      Show help information

Commands:
  getinfo           

Run 'dotnet-tenc [command] --help' for more information about a command.

changing text encoding
Examples:
input utf-8,output shift_jis(cp932) by name:
  dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt
input utf-8,output shift_jis(cp932) by code page
  dotnet tenc -f 65001 -t 932 -i utf8.txt -o sjis.txt
input utf-8,output utf-8 without BOM(BOM added by default)
  dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt -n
```

## Getting encoding info

Since 0.2.0, `getinfo` subcommand is added.
This command gets the information of specified encodings.

Here is the help text

```
Usage: dotnet-tenc getinfo [options]

Options:
  -n|--name        encoding names
  -c|--codepage    code page range(number separated by '-', number is must be 0 - 65535)
  -s|--show-fault  show fault result
  -?|-h|--help     Show help information

getting encoding info and output by CSV format

Examples:
try get info by name:
    dotnet tenc getinfo -n shift_jis
try get info by codepage number(single)
    dotnet tenc getinfo -c 932
try get info by codepage number(range)
    dotnet tenc getinfo -c 0-1000
```

and then command will output following CSV format

```
> dotnet tenc getinfo -n utf-8
Name,CodePage,Found,DisplayName,Preamble
utf-8,65001,True,Unicode (UTF-8),efbbbf
```

# Build

1. run `dotnet build` to build binary
2. run `dotnet pack` to creating nuget package

you can test local nuget package by following command.

`dotnet tool install --tool-path [installdir] --add-source [path to nupkg directory] dotnet-text-encoder`

After executing, executable files are place in `[installdir]`.
