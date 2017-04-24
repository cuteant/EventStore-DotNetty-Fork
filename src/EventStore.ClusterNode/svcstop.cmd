@echo off

call defines.bat

redis-server --service-stop --service-name %NTSERVICE%


:end
