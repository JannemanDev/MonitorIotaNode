set version=1.0
set dotnetcore=net5.0

rem Root solution folder
cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"


rem Delete Builds folder
del /f /q /s Builds\*.* > nul
rmdir /q /s Builds
mkdir Builds


rem Delete bin folder of MonitorIotaNode
del /f /q /s Source\MonitorIotaNode\bin\*.* > nul
rmdir /q /s Source\MonitorIotaNode\bin

rem .NET Core 5
dotnet publish Source\MonitorIotaNode\MonitorIotaNode.csproj /p:PublishProfile=Source\MonitorIotaNode\Properties\PublishProfiles\linux-arm.pubxml --configuration Release
dotnet publish Source\MonitorIotaNode\MonitorIotaNode.csproj /p:PublishProfile=Source\MonitorIotaNode\Properties\PublishProfiles\linux-x64.pubxml --configuration Release
dotnet publish Source\MonitorIotaNode\MonitorIotaNode.csproj /p:PublishProfile=Source\MonitorIotaNode\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish Source\MonitorIotaNode\MonitorIotaNode.csproj /p:PublishProfile=Source\MonitorIotaNode\Properties\PublishProfiles\win-x64.pubxml --configuration Release
dotnet publish Source\MonitorIotaNode\MonitorIotaNode.csproj /p:PublishProfile=Source\MonitorIotaNode\Properties\PublishProfiles\win-x86.pubxml --configuration Release

rem .NET Core 3.1
dotnet publish Source\MonitorIotaNode\MonitorIotaNode.csproj /p:PublishProfile=Source\MonitorIotaNode\Properties\PublishProfiles\linux-arm-netcoreapp3.1.pubxml --configuration Release

rem Rar all MonitorIotaNode builds
rem .NET Core 5
cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"
cd Source\MonitorIotaNode\bin\Release\net5.0\publish\linux-arm\
"C:\Program Files\WinRAR\rar.exe" a -r ..\..\..\..\..\..\..\Builds\MonitorIotaNode-%version%-linux-arm-%dotnetcore%.rar *.*

cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"
cd Source\MonitorIotaNode\bin\Release\net5.0\publish\linux-x64\
"C:\Program Files\WinRAR\rar.exe" a -r ..\..\..\..\..\..\..\Builds\MonitorIotaNode-%version%-linux-x64-%dotnetcore%.rar *.*

cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"
cd Source\MonitorIotaNode\bin\Release\net5.0\publish\osx-x64\
"C:\Program Files\WinRAR\rar.exe" a -r ..\..\..\..\..\..\..\Builds\MonitorIotaNode-%version%-osx-x64-%dotnetcore%.rar *.*

cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"
cd Source\MonitorIotaNode\bin\Release\net5.0\publish\win-x64\
"C:\Program Files\WinRAR\rar.exe" a -r ..\..\..\..\..\..\..\Builds\MonitorIotaNode-%version%-win-x64-%dotnetcore%.rar *.*

cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"
cd Source\MonitorIotaNode\bin\Release\net5.0\publish\win-x86\
"C:\Program Files\WinRAR\rar.exe" a -r ..\..\..\..\..\..\..\Builds\MonitorIotaNode-%version%-win-x86-%dotnetcore%.rar *.*

rem Rar all MonitorIotaNode builds
rem .NET Core 3.1
cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"
cd Source\MonitorIotaNode\bin\Release\netcoreapp3.1\publish\linux-arm\
"C:\Program Files\WinRAR\rar.exe" a -r ..\..\..\..\..\..\..\Builds\MonitorIotaNode-%version%-linux-arm-netcoreapp3.1.rar *.*


cd /d "C:\Users\Jan Oonk\Source\repos\MonitorIotaNode"
