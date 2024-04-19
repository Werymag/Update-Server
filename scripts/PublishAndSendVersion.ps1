$currentPath = $MyInvocation.MyCommand.Path | Split-Path -Parent
Get-Content .\Input\Parameters | Foreach-Object{
   if($_.Contains('=')){
   $var = $_.Split('=')
   New-Variable -Name $var[0] -Value $var[1]  
} }

.\Subscripts\RemoveFiles.ps1 
timeout /t 4
.\Subscripts\Publish.ps1
timeout /t 4
.\Subscripts\Archive.ps1
timeout /t 4
Start-Process  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer.iss   -Wait
timeout /t 4
.\Subscripts\SendFileToServer.ps1 

Read-Host "Program pushed to server"