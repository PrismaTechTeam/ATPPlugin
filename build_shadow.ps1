$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
Set-Location 'C:\Dev\ATPPlugin'
$p = Start-Process -FilePath $msbuild -ArgumentList 'ATPShadowMain\ATPShadowMain.csproj', '/v:m' -Wait -PassThru -NoNewWindow -RedirectStandardOutput 'C:\Dev\ATPPlugin\build_out.txt' -RedirectStandardError 'C:\Dev\ATPPlugin\build_err.txt'
exit $p.ExitCode
