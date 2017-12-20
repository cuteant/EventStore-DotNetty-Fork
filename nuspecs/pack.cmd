@set NUGET_PACK_OPTS= -Version 4.0.0-rtm-171101
@set NUGET_PACK_OPTS= %NUGET_PACK_OPTS% -OutputDirectory Publish

%~dp0nuget.exe pack %~dp0Google.V8.nuspec %NUGET_PACK_OPTS%
