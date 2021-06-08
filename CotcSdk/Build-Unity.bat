set msbuild.exe="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
%msbuild.exe% CotcSdk.sln /p:Configuration=Release /t:Restore
%msbuild.exe% CotcSdk.csproj /p:Configuration=Release;SolutionDir=%cd%\
%msbuild.exe% CotcSdk-Editor\CotcSdk-Editor.csproj /p:Configuration=Release;SolutionDir=%cd%\
%msbuild.exe% CotcSdk-UniversalWindows\CotcSdk-UniversalWindows.csproj /p:Configuration=Release;SolutionDir=%cd%\
