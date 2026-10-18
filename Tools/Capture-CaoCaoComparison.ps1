param([string[]]$Views=@('face','shoulders','hands','full'))
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
function Run([string[]]$Arguments){$raw=unity command @Arguments --caller plugin --skill unity-cli --project-path $project --format json;$result=$raw|ConvertFrom-Json;if(!$result.success -or ($result.data.result.PSObject.Properties.Name -contains 'success' -and !$result.data.result.success)){throw $raw}}
try{
 foreach($view in $Views){
  Run @('menu','--path','Tools/RedCliffs/CaoCao Comparison/Current Model')
  Set-Content "$project/LocalArchive/CharacterParts20261018/active-view.txt" $view -Encoding ASCII
  Run @('menu','--path','Tools/Character Parts/Review Current Character')
  Run @('menu','--path','Tools/RedCliffs/CaoCao Comparison/Previous Demo Model')
  Start-Sleep -Milliseconds 650
  Run @('capture_game_view','--width','3840','--height','2160','--source','screen','--save_path',"LocalArchive/CaoCaoComparison/demo-$view.png")
  Run @('menu','--path','Tools/RedCliffs/CaoCao Comparison/Current Model')
  Start-Sleep -Milliseconds 650
  Run @('capture_game_view','--width','3840','--height','2160','--source','screen','--save_path',"LocalArchive/CaoCaoComparison/current-$view.png")
 }
}finally{Run @('menu','--path','Tools/RedCliffs/CaoCao Comparison/Finish')}
New-Item -ItemType Directory -Force "$project/LocalArchive/CaoCaoHairEnemies20261016/Revision16/Comparison"|Out-Null
Copy-Item "$project/Assets/LocalArchive/CaoCaoComparison/*.png" "$project/LocalArchive/CaoCaoHairEnemies20261016/Revision16/Comparison"
Write-Output 'Comparison captured with one fixed camera per pair; runtime model restored.'


