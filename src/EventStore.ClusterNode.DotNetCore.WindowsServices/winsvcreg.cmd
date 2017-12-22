@echo off

dotnet EventStore.ClusterNode.dll action:install start-immediately:false name:ESClusterDotNetCore display-name:ES.ClusterNode.DotNetCore description:ES.ClusterNode.DotNetCore

pause

:end