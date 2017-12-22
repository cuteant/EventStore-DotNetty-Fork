@echo off

call defines.bat

dotnet EventStore.ClusterNode.dll action:stop name:ESClusterDotNetCore display-name:ES.ClusterNode.DotNetCore description:ES.ClusterNode.DotNetCore


:end
