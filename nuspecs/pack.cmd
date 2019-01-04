@set NUGET_PACK_OPTS= -Version 5.0.0
@set NUGET_PACK_OPTS= %NUGET_PACK_OPTS% -OutputDirectory Publish

%~dp0nuget.exe pack %~dp0Google.V8.nuspec %NUGET_PACK_OPTS%
