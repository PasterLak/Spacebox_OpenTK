## Commands

### Compile
#### Windows

``` powershell
dotnet publish -c Debug - r win - x64--self - contained true
dotnet publish -c Release - r win - x64--self - contained true
dotnet publish -c Release -r win-x64 --self-contained true --output ./publish
dotnet publish -c Release -r win-x64 --self-contained true --output ./publish
dotnet publish -c Release -r win-x64 --output ./publish

dotnet publish Spacebox/Spacebox.csproj -c Release -r win-x64 --self-contained true --output ./publish
dotnet publish Spacebox/Spacebox.csproj -c Release -r win-x64 --self-contained true --output ./publish /p:PublishSingleFile=true /p:PublishTrimmed=true

```

#### OSX
``` bash
dotnet publish Spacebox/Spacebox.csproj -c Release -r osx-x64  --output ./publish
```

#### Linux
``` bash
dotnet publish -c Release -r linux-x64 --self-contained true --output ./publish
dotnet publish Spacebox/Spacebox.csproj -c Release -r linux-x64 --self-contained true --output ./publish
```
---

### Statistics

// (008): 192, 19954	   | (009 early): 197, 20753. AO: 217,23165
// (010 early): 358, 27640 | (010 middle): 373, 29431 | 010 final: 379, 30093, 
// (010 final): 386, 30885 | 011 446, 34053 | 011 trailer 547, 40409 |011 final 40916, 576 
// (012) early: 582, 43764 |  609, 45503 | 3 mai  610, 46024 | 3 sep 711, 54711 (65560)

### Calculate statistics

``` powershell
./stats.ps1
wsl ./stats.sh
chmod +x stats.sh
./stats.sh
```


### OSX/Linux
``` bash
find /Users/vladis/Documents/Projects/C#/Spacebox_OpenTK -type f -name "*.cs" | wc -l
find /Users/vladis/Documents/Projects/C#/Spacebox_OpenTK -type f -name "*.cs" -exec cat {} + | wc -l
```