param([Parameter(Mandatory=$true)][string]$Character,[ValidateSet('prepare','sculpt','uvbake')][string]$Stage,[switch]$Armored)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$bridge=Join-Path $project 'LocalArchive/CaoCaoHairEnemies20261016/Revision2'
$status=Get-Content "$bridge/status.json" -Raw | ConvertFrom-Json
if(!$status.ready -or ([DateTimeOffset]::UtcNow.ToUnixTimeSeconds()-$status.heartbeat)-gt 10 -or (Test-Path "$bridge/request.json")){throw 'Bridge is not ready'}
if($status.project -ne $project){throw 'Wrong Blender project'}
if($Character -notmatch '^[A-Za-z]+$'){throw 'Invalid character'}
if($Stage -ne 'prepare' -and (Split-Path (Split-Path $status.file -Parent) -Leaf) -ne $Character){throw 'Wrong Blender file'}
$arm=if($Armored){'True'}else{'False'}
$script="${Character}_individual_${Stage}.py"
"import runpy`nfrom pathlib import Path`nrunpy.run_path(str(Path(__file__).with_name('character_parts_$Stage.py')),init_globals={'TARGET':'$Character','ARMORED':$arm})" | Set-Content "$bridge/$script" -Encoding UTF8
$id="$Character-$Stage-"+[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
@{id=$id;script=$script}|ConvertTo-Json -Compress|Set-Content "$bridge/request.json" -Encoding ASCII
Write-Output "$bridge/$id-result.json"
