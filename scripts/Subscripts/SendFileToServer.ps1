clear
"Start sending the file to server " + $url 

$ErrorActionPreference = 'Stop'
 
$sourceFilePath  = $currentPath  + $sourceFilePath
$installFilePath  = $currentPath  + $installFilePath 
$changelogFilePath  = $currentPath  + $changelogFilePath
$fileExePath  = $currentPath + $fileExePath

if (!(Test-Path $sourceFilePath)) {  Write-Warning "$sourceFilePath doesn't exist" }
if (!(Test-Path $installFilePath)) {  Write-Warning "$installFilePath doesn't exist" }
if (!(Test-Path $changelogFilePath)) {  Write-Warning "$changelogFilePath doesn't exist" }
if (!(Test-Path $fileExePath)) {  Write-Warning "$fileExePath doesn't exist" }

$version = (Get-Item $fileExePath).VersionInfo.FileVersionRaw



Try {
     Add-Type -AssemblyName 'System.Net.Http'
	
    $client = New-Object System.Net.Http.HttpClient
    $content = New-Object System.Net.Http.MultipartFormDataContent	

    $loginContent = [System.Net.Http.StringContent]::new($login,[System.Text.Encoding]::UTF8,'text/csv')
    $passwordContent = [System.Net.Http.StringContent]::new($password,[System.Text.Encoding]::UTF8,'text/csv')
    $content.Add($loginContent, 'Login') 
    $content.Add($passwordContent, 'Password') 

    $porgramNameContent = [System.Net.Http.StringContent]::new($programName,[System.Text.Encoding]::UTF8,'text/csv')
    $versionContent = [System.Net.Http.StringContent]::new($version,[System.Text.Encoding]::UTF8,'text/csv')
    $content.Add($porgramNameContent, 'Program') 
    $content.Add($versionContent, 'Version') 

    $sorceFileStream = [System.IO.File]::OpenRead($sourceFilePath)    
    $sorceFileName = [System.IO.Path]::GetFileName($sourceFilePath)
    $sorceFileContent = New-Object System.Net.Http.StreamContent($sorceFileStream)
    $sorceFileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");   	
    $sorceFileContent.Headers.Add('ProgramName',$programName);    
    $sorceFileContent.Headers.Add('Version',$version);
    $content.Add($sorceFileContent, 'SourceFile', $sorceFileName)      

    $installFileStream = [System.IO.File]::OpenRead($installFilePath)    
    $installFileName = [System.IO.Path]::GetFileName($installFilePath)
    $installFileContent = New-Object System.Net.Http.StreamContent($installFileStream)
    $installFileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");   
    $content.Add($installFileContent, 'InstallFile', $installFileName)    


    $changelogFileStream = [System.IO.File]::OpenRead($changelogFilePath)    
    $changelogFileName = [System.IO.Path]::GetFileName($changelogFilePath)
    $changelogFileContent = New-Object System.Net.Http.StreamContent($changelogFileStream)
    $changelogFileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");   
    $content.Add($changelogFileContent, 'Changelog', $changelogFileName) 

    $result = $client.PostAsync($url, $content).Result
    $result.EnsureSuccessStatusCode()   	
}
Catch { 

	function Resolve-Error ($ErrorRecord=$Error[0])
	{
	   $ErrorRecord | Format-List * -Force
	   $ErrorRecord.InvocationInfo |Format-List *
	   $Exception = $ErrorRecord.Exception
	   for ($i = 0; $Exception; $i++, ($Exception = $Exception.InnerException))
	   {   "$i" * 80
		   $Exception |Format-List * -Force
	   }
	}

	Resolve-Error ($_)
	"$result"    
	Write-Host $_.ScriptStackTrace
    Read-Host "Error sending the file"	

    exit 1
}
Finally {
    if ($client -ne $null) { $client.Dispose() }
    if ($content -ne $null) { $content.Dispose() }
    if ($fileStream -ne $null) { $fileStream.Dispose() }
    if ($fileContent -ne $null) { $fileContent.Dispose() }
}


"Finish sending file"

