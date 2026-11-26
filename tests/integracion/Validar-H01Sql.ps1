param([string]$SubredDocker,[string]$Antes='practica-5',[string]$Despues='HEAD')
# Se reutilizan los helpers de procesos PS 5.1 y Compose; no se ejecuta la API.
. "$PSScriptRoot/Validar-Practica5.ps1" -SoloFunciones -SubredDocker $SubredDocker
$casos=[Collections.Generic.List[object]]::new()
try {
    $demo=[IO.File]::ReadAllText("$raiz/.env.example")
    $clave=[regex]::Match($demo,'(?m)^MSSQL_SA_PASSWORD=(.+)\r?$').Groups[1].Value.Trim()
    if(!$clave){throw 'Falta contraseña ficticia del ejemplo.'}
    EnvLocal 'MSSQL_SA_PASSWORD' $clave
    Copy-Item "$raiz/.env.example" "$evidencia/.env"
    $config=[IO.File]::ReadAllText("$raiz/docker-compose.yml")
    if($SubredDocker){
        if($SubredDocker -notmatch '^\d{1,3}(\.\d{1,3}){3}/\d{1,2}$'){throw 'SubredDocker debe ser CIDR IPv4.'}
        $config += "`nnetworks:`n  default:`n    ipam:`n      config:`n        - subnet: $SubredDocker`n"
    }
    [IO.File]::WriteAllText("$evidencia/docker-compose.yml",$config)
    $null=Compose @('up','-d','--wait','sqlserver')
    foreach($tipo in @('Antes','Despues')){
        $referencia=if($tipo -eq 'Antes'){$Antes}else{$Despues}
        $commit=(EjecutarProceso 'git' @('-C',$raiz,'rev-parse',($referencia+'^{commit}')) '' 'referencia').Trim()
        $snapshot=Join-Path $evidencia $tipo
        $zip=Join-Path $evidencia "$tipo.zip"
        $null=EjecutarProceso 'git' @('-C',$raiz,'archive','--format=zip',"--output=$zip",$commit) '' 'archivo'
        Expand-Archive -LiteralPath $zip -DestinationPath $snapshot
        $destino=Join-Path $snapshot 'tests/integracion/SqlConcurrencyProbe'
        [void](New-Item -ItemType Directory -Path $destino -Force)
        foreach($nombre in @('Program.cs','SqlConcurrencyProbe.csproj')){Copy-Item "$PSScriptRoot/SqlConcurrencyProbe/$nombre" "$destino/$nombre" -Force}
        $catalogo='SISAsignacion3_'+[guid]::NewGuid().ToString('N')
        $conexion="Server=sqlserver,1433;Database=$catalogo;User ID=sa;Password=$clave;Encrypt=True;TrustServerCertificate=True;"
        $resultado=EjecutarProceso 'docker' @('run','--rm','--network',"${proyecto}_default",'--volume',"${snapshot}:/src",'--workdir','/src','--env',"ConnectionStrings__DefaultConnection=$conexion",'mcr.microsoft.com/dotnet/sdk:10.0','dotnet','run','--project','tests/integracion/SqlConcurrencyProbe/SqlConcurrencyProbe.csproj','--verbosity','quiet') '' "sql-$tipo" -DevolverResultado
        $linea=@($resultado.Salida -split '\r?\n' | Where-Object {$_ -like 'A3_SQL_RESULT *'})
        if($linea.Count -ne 1){throw "No hay resultado SQL válido de $tipo; no se acepta un fallo de compilación como RED."}
        $datos=$linea[0].Substring('A3_SQL_RESULT '.Length) | ConvertFrom-Json
        if($tipo -eq 'Antes'){
            if($resultado.Codigo -ne 1 -or $datos.Passed -or !$datos.BothSucceeded){throw 'RED SQL no demostrado.'}
        }else{
            if($resultado.Codigo -ne 0 -or !$datos.Passed -or !$datos.BothSucceeded -or $datos.FirstTicks -ne $datos.FinalTicks){throw 'GREEN SQL no demostrado.'}
        }
        if($datos.Migrations -ne 7){throw 'No se verificaron siete migraciones.'}
        $casos.Add([pscustomobject]@{Version=$tipo;Commit=$commit;Resultado=$datos;ExitCode=$resultado.Codigo})
        Write-Host "H-01 SQL $tipo`: Passed=$($datos.Passed); ambas operaciones=$($datos.BothSucceeded); primera fecha conservada=$($datos.FirstTicks -eq $datos.FinalTicks)"
    }
}finally{
    $casos | ConvertTo-Json -Depth 6 | Set-Content "$evidencia/h01-sql.json" -Encoding UTF8
    try{$null=Compose @('stop','sqlserver')}finally{
        $http.Dispose()
        foreach($nombre in $entorno.Keys){[Environment]::SetEnvironmentVariable($nombre,$entorno[$nombre])}
    }
    Write-Host "Evidencia SQL: $evidencia/h01-sql.json; volumen de prueba conservado."
}
