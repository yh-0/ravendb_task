# GetTvShowTotalLength

### Requirements
- Git
- Dotnet 9.0
- Python 3.12

### Usage

Clone the repository and build the C# project
```bash
git clone https://github.com/yh-0/ravendb_task.git
cd ravendb_task
dotnet build GetTvShowTotalLength.csproj
```

Set the C# application binary path as environment variable
```bash
# Linux
export GET_TVSHOW_TOTAL_LENGTH_BIN="$(pwd)/bin/Debug/net9.0/GetTvShowTotalLength"
# Windows
set GET_TVSHOW_TOTAL_LENGTH_BIN=%cd%\bin\Debug\net9.0\GetTvShowTotalLength.exe
```

To run Python script
```bash
python3 tv-time.py <show_list_file>
```

To run C# application
```bash
cd ./bin/Debug/net9.0
./GetTvShowTotalLength <show_name> # wrap show name in double quotes if it contains spaces
```