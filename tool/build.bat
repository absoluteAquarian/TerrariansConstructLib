:: MAKE ABSOLUTELY SURE THAT BAT FILES ARE SAVED IN THE "US-ASCII - Codepage 20127" ENCODING!!!
@echo off

:: Return prematurely if TerrariansConstruct isn't present
if not exist "..\TerrariansConstruct\" (
	echo TerrariansConstruct not found
	exit 0
)

echo Copying built files to the TerrariansConstruct\lib directory...
:: Error codes less than 8 should just be ignored
(robocopy bin\Debug\net6.0 ..\TerrariansConstruct\lib TerrariansConstructLib.dll TerrariansConstructLib.pdb TerrariansConstructLib.xml /S /XX) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0