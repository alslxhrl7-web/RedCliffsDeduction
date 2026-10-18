param([Parameter(Mandatory=$true)][string]$Character,[ValidateSet('Apply','Review','Stop')][string]$Stage,[string[]]$Views=@('full','face','back','interaction'),[int]$CaptureWidth=1600,[int]$CaptureHeight=1000,[switch]$ScreenViews)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$archive=Join-Path $project 'LocalArchive/CharacterParts20261018'
$active=(Get-Content (Join-Path $archive 'active-character.txt') -Raw).Trim()
if($active -ne $Character){throw 'Active character mismatch'}
function Invoke-Editor([string[]]$Arguments){$raw=& unity command @Arguments --caller plugin --skill unity-cli --project-path $project --format json; $result=$raw | ConvertFrom-Json; if(!$result.success -or ($result.data.result.PSObject.Properties.Name -contains 'success' -and !$result.data.result.success)){throw $raw}; return $result}
if($Stage -eq 'Apply'){
 $r=Join-Path $archive $Character; $dest=Join-Path $project "Assets/Art/Characters/Parts20261018/$Character"
 New-Item -ItemType Directory -Force "$dest/Maps" | Out-Null
 Copy-Item -LiteralPath "$r/Parts.fbx" -Destination "$dest/Parts.fbx"
 foreach($n in @('BaseColor','Normal','MetallicSmoothness')){Copy-Item -LiteralPath "$r/Baked/Delivery/SurfaceUV2/$n.png" -Destination "$dest/Maps/$n.png"}
 "*.fbx -filter -diff -merge -text`n*.png -filter -diff -merge -text`n*.asset -filter -diff -merge" | Set-Content "$dest/.gitattributes" -Encoding ASCII
 Invoke-Editor @('menu','--path','Assets/Refresh') | Out-Null
 Invoke-Editor @('menu','--path','Tools/Character Parts/Apply Current Character') | Out-Null
 Write-Output 'Apply submitted; inspect apply-status before review.'
}
if($Stage -eq 'Review'){
 if((Get-Content "$archive/$Character/Review/apply-status.txt" -Raw).Trim() -ne 'COMPLETE'){throw 'Apply incomplete'}
 Invoke-Editor @('menu','--path','Tools/Character Parts/Validate Motion') | Out-Null
 Invoke-Editor @('editor_play') | Out-Null
 Invoke-Editor @('menu','--path','Tools/Duseok Parts/Review Start') | Out-Null
 Invoke-Editor @('menu','--path','Tools/Duseok Parts/Review Prepare') | Out-Null
 foreach($view in $Views){
  Set-Content "$archive/active-view.txt" $view -Encoding ASCII
  Invoke-Editor @('menu','--path','Tools/Character Parts/Review Current Character') | Out-Null
  if($view -eq 'interaction'){Invoke-Editor @('menu','--path','Tools/Character Parts/Check Interaction') | Out-Null}
  $source=if($ScreenViews -or $view -eq 'interaction'){'screen'}else{'camera'}
  Invoke-Editor @('capture_game_view','--width',([string]$CaptureWidth),'--height',([string]$CaptureHeight),'--source',$source,'--save_path',"LocalArchive/CharacterPartsReview/$Character-$view.png") | Out-Null
 }
 Get-Content "$archive/$Character/Review/motion.txt"
 Get-Content "$archive/$Character/Review/interaction.txt"
 Write-Output 'Play remains open. Inspect all images before Stop.'
}
if($Stage -eq 'Stop'){
 Invoke-Editor @('editor_stop') | Out-Null
 Get-ChildItem "$project/Assets/LocalArchive/CharacterPartsReview/$Character-*.png" | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination "$archive/$Character/Review/$($_.Name)"}
 Write-Output 'Play stopped, window remains open, evidence copied.'
}



