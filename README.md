# dotnet-text-encoder

dotnet global tool for changing text encoding.

# Requirements

* `dotnet sdk 2.1 or later`:https://dotnet.microsoft.com/download

# Install(TODO)

1. do following command
    `dotnet tool install -g dotnet-text-encoder`
2. ensure adding `$HOME/.dotnet/tools` to PATH environment

and then you can execute command by `dotnet tenc` or `dotnet-tenc`

# Usage

here is the help output

```
dotnet-tenc 0.1.0
Copyright (C) itn3000 2019
USAGE:
basic usage:
  dotnet-tenc --from "[from encoding]" --input "[input file]" --output "[output file]" --to "[to encoding]"
read standard input:
  dotnet-tenc --from "[from encoding]" --output "[output file]" --to "[to encoding]"
disable preamble(BOM):
  dotnet-tenc --from "[from encoding]" --input "[input file]" --no-preamble --output "[output file]" --to utf-8

  -f, --from           input file encoding(default: UTF-8)

  -t, --to             output file encoding(default: UTF-8)

  -i, --input          input file path(default: standard input)

  -o, --output         output file path(default: standard output)

  -n, --no-preamble    disable output preamble(=BOM) if exists

  --help               Display this help screen.

  --version            Display version information.
```

# Build

1. run `dotnet build` to build binary
2. run `dotnet pack` to creating nuget package

you can test local nuget package by following command.

`dotnet tool install --tool-path [installdir] --add-source [path to nupkg directory] dotnet-text-encoder`

After executing, executable files are place in `[installdir]`.