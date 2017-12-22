@echo off

dotnet EventStore.ClusterNode.dll action:uninstall name:ESClusterDotNetCore display-name:ES.ClusterNode.DotNetCore description:ES.ClusterNode.DotNetCore

pause

:end