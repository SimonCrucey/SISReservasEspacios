param([ValidateSet('Todos','H-01','H-02','H-03')][string]$Hallazgo='Todos')
$ErrorActionPreference='Stop'
$raiz=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$evidencia=Join-Path ([IO.Path]::GetTempPath()) ('asignacion3-regresiones-'+[guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $evidencia)
$dotnet=(Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
$casos=@(
 @{Id='H-01';Archivo='tests/SISReservas.Api.Tests/LecturaConcurrenteTests.cs';Filtro='FullyQualifiedName~LecturaConcurrenteTests'},
 @{Id='H-02';Archivo='tests/SISReservas.Api.Tests/ErroresControladosTests.cs';Filtro='FullyQualifiedName~ErroresControladosTests'},
 @{Id='H-03';Archivo='tests/SISReservas.Api.Tests/IndependenciaCoreTests.cs';Filtro='FullyQualifiedName~H03_CoreOnly_CompilaHostRealSinReservasConMigraciones'}
)
function Exportar([string]$ref,[string]$destino){
 $zip="$destino.zip"
 & git -C $raiz archive --format=zip --output=$zip $ref
 if($LASTEXITCODE -ne 0){throw 'No se pudo exportar la referencia.'}
 Expand-Archive -LiteralPath $zip -DestinationPath $destino
}
function Probar([string]$solucion,[string]$filtro,[string]$log){
 $anterior=$ErrorActionPreference
 try{
  # PS 5.1: stderr nativo no sustituye al código real de salida.
  $ErrorActionPreference='Continue'
  $salida=(& $dotnet test $solucion --filter $filtro --verbosity minimal 2>&1 | Out-String)
  $codigo=$LASTEXITCODE
 }finally{$ErrorActionPreference=$anterior}
 [IO.File]::WriteAllText($log,$salida,[Text.UTF8Encoding]::new($false))
 return @{Codigo=$codigo;Salida=$salida}
}
$resultados=@()
foreach($caso in $casos){
 if($Hallazgo -ne 'Todos' -and $Hallazgo -ne $caso.Id){continue}
 $commit=(& git -C $raiz log HEAD --format=%H --max-count=1 "--grep=^fix $($caso.Id):").Trim()
 if(!$commit){throw "No se encuentra $($caso.Id)."}
 $padre=(& git -C $raiz rev-parse "${commit}^").Trim()
 if($LASTEXITCODE -ne 0){throw 'No se pudo consultar el padre.'}
 $antes=Join-Path $evidencia "$($caso.Id)-padre"
 $corregido=Join-Path $evidencia "$($caso.Id)-corregido"
 Exportar $padre $antes
 Exportar $commit $corregido
 # La misma prueba del commit correctivo se incorpora a su padre, sin tocar producción.
 Copy-Item -LiteralPath (Join-Path $corregido $caso.Archivo) -Destination (Join-Path $antes $caso.Archivo) -Force
 $red=Probar (Join-Path $antes 'SISReservasEspacios.slnx') $caso.Filtro (Join-Path $evidencia "$($caso.Id)-padre.log")
 if($red.Codigo -eq 0 -or $red.Salida -notmatch 'Failed:\s*[1-9]'){throw "RED no demostrado: $($caso.Id). Evidencia: $evidencia"}
 $green=Probar (Join-Path $corregido 'SISReservasEspacios.slnx') $caso.Filtro (Join-Path $evidencia "$($caso.Id)-corregido.log")
 if($green.Codigo -ne 0 -or $green.Salida -notmatch 'Passed:\s*[1-9]'){throw "GREEN correctivo no demostrado: $($caso.Id). Evidencia: $evidencia"}
 $final=Probar (Join-Path $raiz 'SISReservasEspacios.slnx') $caso.Filtro (Join-Path $evidencia "$($caso.Id)-final.log")
 if($final.Codigo -ne 0 -or $final.Salida -notmatch 'Passed:\s*[1-9]'){throw "GREEN final no demostrado: $($caso.Id). Evidencia: $evidencia"}
 $resultados+=[pscustomobject]@{Hallazgo=$caso.Id;Correccion=$commit;Padre=$padre;Antes='FALLA';Corregido='PASA';Final='PASA'}
 Write-Host "$($caso.Id): RED en padre; GREEN en commit correctivo y candidato final."
}
$resultados | ConvertTo-Json | Set-Content (Join-Path $evidencia 'resultados.json') -Encoding UTF8
Write-Host "Evidencia: $evidencia"
# Solo usa copias temporales; no cambia ramas, etiquetas ni bases de datos.
