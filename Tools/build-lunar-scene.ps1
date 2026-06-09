$unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
$project = 'F:\Unity (Game Creator)\New Game\FPS Game'
$log = Join-Path $project 'Logs\batch-lunar-build.log'
New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Temp') | Out-Null
Set-Content -Path (Join-Path $project 'Temp\PendingLunarBuild.flag') -Value 'build'
& $unity -batchmode -nographics -quit -projectPath $project -executeMethod Game.EditorTools.BuildTestScene.BuildFromCommandLine -logFile $log
Write-Host "exit=$LASTEXITCODE"
if (Test-Path $log) { Get-Content $log -Tail 40 }
