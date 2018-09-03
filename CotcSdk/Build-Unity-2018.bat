set bb.build.msbuild.exe=
for /D %%D in (%SYSTEMROOT%\Microsoft.NET\Framework\v4*) do set msbuild.exe=%%D\MSBuild.exe
%msbuild.exe% CotcSdk.csproj /p:Configuration=Release-Unity-2018;SolutionDir=%cd%\
%msbuild.exe% CotcSdk-Editor\CotcSdk-Editor.csproj /p:Configuration=Release-Unity-2018;SolutionDir=%cd%\
