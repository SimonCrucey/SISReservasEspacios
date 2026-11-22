param([switch]$Local, [string]$MailpitPath, [int]$Puerto = 15094, [int]$PuertoCorreo = 18094, [int]$PuertoSmtp = 11094, [switch]$ProbarWeatherReal)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$handler = [Net.Http.HttpClientHandler]::new()
$handler.UseProxy = $false # Todas las peticiones del script van a loopback.
$http = [Net.Http.HttpClient]::new($handler)
$http.Timeout = [TimeSpan]::FromSeconds(30)
$raiz = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$id = [guid]::NewGuid().ToString('N')
$proyecto = "sisp5-$id"
$base = "SISReservas_P5_$id"
$evidencia = Join-Path ([IO.Path]::GetTempPath()) $proyecto
[void](New-Item -ItemType Directory -Path $evidencia)
$resultados = [Collections.Generic.List[object]]::new()
$procesos = [Collections.Generic.List[object]]::new()
$entorno = @{}
function EnvLocal($nombre, $valor) {
    if (!$entorno.ContainsKey($nombre)) { $entorno[$nombre] = [Environment]::GetEnvironmentVariable($nombre) }
    [Environment]::SetEnvironmentVariable($nombre, $valor)
}
function Check($nombre, $valor) {
    $resultados.Add([pscustomobject]@{Prueba=$nombre;Resultado= $(if($valor){'PASS'}else{'FAIL'})})
    if (!$valor) { throw "Falló: $nombre" }
}
function PuertoLibre([int]$preferido) {
    $listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,$preferido)
    try {$listener.Start(); return $preferido}
    catch [Net.Sockets.SocketException] {
        $alternativo=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
        try {$alternativo.Start(); return $alternativo.LocalEndpoint.Port}
        finally {$alternativo.Stop()}
    } finally {$listener.Stop()}
}
function ResolverEjecutable([string]$nombre) {
    $busqueda=if($nombre -eq 'docker'){'docker.exe'}else{$nombre}
    $candidatos=@(Get-Command $busqueda -CommandType Application -All -ErrorAction SilentlyContinue | ForEach-Object {$_.Path})
    if($busqueda -eq 'docker.exe'){
        $candidatos += @("$env:ProgramFiles\Docker\Docker\resources\bin\docker.exe", "$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin\docker.exe")
    }
    foreach($ruta in $candidatos){
        if($ruta -and (Test-Path -LiteralPath $ruta -PathType Leaf)){
            return (Get-Item -LiteralPath $ruta).FullName
        }
    }
    throw "No se encontró el ejecutable '$busqueda'. Comprueba su instalación y PATH. Para Docker, abre Docker Desktop y una nueva ventana de PowerShell."
}
function EjecutarProceso([string]$ejecutable, [string[]]$argumentos, [string]$entrada='', [string]$registro='proceso') {
    # Process evita que stderr se convierta en NativeCommandError en Windows PowerShell.
    # Escapado de argv para procesos Windows, sin pasar por cmd.exe ni evaluar comandos.
    $citados = foreach($argumento in $argumentos){
        if($argumento -ne '' -and $argumento -notmatch '[\s"]'){$argumento}
        else{'"' + ([regex]::Replace([regex]::Replace($argumento, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1')) + '"'}
    }
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = ResolverEjecutable $ejecutable
    $info.Arguments = $citados -join ' '
    $info.WorkingDirectory = (Get-Location).Path
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.RedirectStandardInput = $true
    $info.StandardOutputEncoding = [Text.Encoding]::UTF8
    $info.StandardErrorEncoding = [Text.Encoding]::UTF8
    $proceso = [Diagnostics.Process]::new()
    $proceso.StartInfo = $info
    try {
        try {[void]$proceso.Start()}
        catch {throw "No se pudo iniciar '$($info.FileName)' desde '$($info.WorkingDirectory)': $($_.Exception.Message)"}
        # Drenar ambos canales a la vez evita bloqueos durante builds extensos.
        $salida = $proceso.StandardOutput.ReadToEndAsync()
        $errores = $proceso.StandardError.ReadToEndAsync()
        if($entrada){$proceso.StandardInput.WriteLine($entrada)}
        $proceso.StandardInput.Close()
        $proceso.WaitForExit()
        $texto = $salida.GetAwaiter().GetResult()
        $diagnostico = $errores.GetAwaiter().GetResult()
        [IO.File]::AppendAllText("$evidencia/$registro.stderr", $diagnostico)
        if($proceso.ExitCode -ne 0){
            if ($registro -ne 'buzon') { [IO.File]::WriteAllText("$evidencia/$registro.stdout", $texto) }
            throw "$ejecutable $($argumentos -join ' ') terminó con código $($proceso.ExitCode). Revisa $evidencia/$registro.stderr"
        }
        return $texto
    } finally { $proceso.Dispose() }
}
function Compose([string[]]$comandos, [string]$entrada='', [string]$registro='compose') {
    if($comandos[0] -in @('up','down')){Write-Host "Docker Compose $($comandos[0]): esperando al proyecto aislado $proyecto..."}
    EjecutarProceso 'docker' (@('compose','-p',$proyecto,'--env-file',"$evidencia/.env",'-f',"$evidencia/docker-compose.yml") + $comandos) $entrada $registro
}
function Api($metodo, $ruta, $esperado, $cuerpo=$null, $token='', $form=$null) {
    $peticion=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($metodo),"http://localhost:$Puerto$ruta")
    $r=$null
    try {
        if($token){$peticion.Headers.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$token)}
        if($form){
            $multipart=[Net.Http.MultipartFormDataContent]::new()
            $peticion.Content=$multipart
            foreach($campo in $form.Keys){
                $archivoForm=$form[$campo]
                $parte=[Net.Http.ByteArrayContent]::new([IO.File]::ReadAllBytes($archivoForm.FullName))
                $parte.Headers.ContentType=[Net.Http.Headers.MediaTypeHeaderValue]::new('application/octet-stream')
                $multipart.Add($parte,$campo,$archivoForm.Name)
            }
        }elseif($null -ne $cuerpo){
            $peticion.Content=[Net.Http.StringContent]::new((ConvertTo-Json -InputObject $cuerpo -Compress),[Text.Encoding]::UTF8,'application/json')
        }
        # HttpClient conserva las respuestas 4xx/5xx para comprobar rechazos esperados.
        $r=$http.SendAsync($peticion).GetAwaiter().GetResult()
        $bytes=$r.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $contenidoRespuesta=[Text.Encoding]::UTF8.GetString($bytes)
    $rutaPublica=$ruta.Split('?')[0]
    Write-Host "$metodo $rutaPublica HTTP $([int]$r.StatusCode)"
    $rol=if(!$token){'Sin sesión'}elseif($cuentas -and $token -eq $cuentas.admin.Token){'Administrador'}else{'Estándar'}
    $resultados.Add([pscustomobject]@{Metodo=$metodo;Ruta=$rutaPublica;Sesion=[bool]$token;Rol=$rol;Caso=$(if($esperado -lt 400){'Válido'}else{'Rechazo esperado'});Esperado=$esperado;Obtenido=[int]$r.StatusCode;Resultado=$(if([int]$r.StatusCode -eq $esperado){'PASS'}else{'FAIL'})})
    if([int]$r.StatusCode -ne $esperado){throw "HTTP inesperado en $metodo $rutaPublica"}
    if($rutaPublica.EndsWith('/descargar') -and $esperado -eq 200){
        return [Convert]::ToBase64String($bytes)
    }
    if($contenidoRespuesta){try{return $contenidoRespuesta | ConvertFrom-Json}catch{return $contenidoRespuesta}}
    } finally { if($r){$r.Dispose()}; $peticion.Dispose() }
}
function Buzon($ruta) {
    if($Local){return $http.GetStringAsync("http://127.0.0.1:$PuertoCorreo$ruta").GetAwaiter().GetResult() | ConvertFrom-Json}
    return (Compose @('exec','-T','mail-inspector','curl','-fsS','--max-time','5',"http://mailpit:8025$ruta") '' 'buzon') | ConvertFrom-Json
}
function Mensaje($email, $antes) {
    for($i=0;$i -lt 40;$i++){
        $lista=Buzon '/api/v1/messages'
        $nuevo=@($lista.messages | Where-Object {
            $_.ID -notin $antes -and @($_.To | Where-Object { $_.Address -eq $email }).Count -gt 0
        })
        if($nuevo.Count){return Buzon "/api/v1/message/$($nuevo[0].ID)"}
        Start-Sleep -Milliseconds 500
    }
    throw "No se localizó el mensaje SMTP esperado. Capturados: $($lista.total); coincidentes nuevos: $($nuevo.Count)."
}
function EsperarApi([int]$SegundosMaximos=180) {
    # Compose publica IPv4. Evitar depender de la resolución IPv6 de localhost.
    $url="http://127.0.0.1:$Puerto/swagger/v1/swagger.json"
    $reloj=[Diagnostics.Stopwatch]::StartNew()
    $ultimo='Sin respuesta'
    Write-Host "Esperando API: GET $url; HTTP esperado: 200; límite: $SegundosMaximos segundos."
    while($reloj.Elapsed.TotalSeconds -lt $SegundosMaximos){
        $restante=[Math]::Max(1,[Math]::Min(10000,($SegundosMaximos-$reloj.Elapsed.TotalSeconds)*1000))
        $espera=[Threading.CancellationTokenSource]::new([int]$restante)
        $r=$null
        try{
            $r=$http.GetAsync($url,$espera.Token).GetAwaiter().GetResult()
            $ultimo="HTTP $([int]$r.StatusCode) $($r.ReasonPhrase)"
            if([int]$r.StatusCode -eq 200){
                Write-Host "API lista: $url — $ultimo ($([int]$reloj.Elapsed.TotalSeconds) s)."
                return
            }
        }catch{
            $errorConexion=$_.Exception.GetBaseException()
            $ultimo="$($errorConexion.GetType().Name): $($errorConexion.Message)"
        }finally{if($r){$r.Dispose()};$espera.Dispose()}
        $linea="[$([int]$reloj.Elapsed.TotalSeconds) s] GET $url -> $ultimo"
        Write-Host $linea
        [IO.File]::AppendAllText("$evidencia/espera-api.log",$linea+[Environment]::NewLine)
        Start-Sleep -Milliseconds 1000
    }
    throw "La comprobación GET $url no obtuvo HTTP 200 en $SegundosMaximos segundos. Último resultado: $ultimo. Diagnóstico: $evidencia/espera-api.log. Abrir /swagger no demuestra que /swagger/v1/swagger.json responda durante el arranque."
}
function ArrancarLocal {
    $api=Start-Process dotnet -ArgumentList @('bin/Debug/net10.0/SISReservas.Api.dll','--urls',"http://localhost:$Puerto") -WorkingDirectory "$raiz/backend/SISReservas.Api" -PassThru -WindowStyle Hidden -RedirectStandardOutput "$evidencia/api.out" -RedirectStandardError "$evidencia/api.err"
    $procesos.Add($api)
    return $api
}
function SqlLectura($consulta) {
    if($Local){
        $r=EjecutarProceso 'sqlcmd' @('-S','(localdb)\MSSQLLocalDB','-E','-d',$base,'-b','-h','-1','-W','-Q',"SET NOCOUNT ON; $consulta") '' 'sql'
    }else{
        $r=Compose @('exec','-T','sqlserver','sh','-c','SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d SISReservasEspacios -h -1 -W -Q "$1"', 'sqlcmd', "SET NOCOUNT ON; $consulta")
    }
    return ($r -join '').Trim()
}
try {
    if(!$Local){
        $dockerRuta=ResolverEjecutable 'docker'
        Write-Host "Docker CLI: $dockerRuta"
        $null=EjecutarProceso $dockerRuta @('compose','version') '' 'docker-preflight'
        try {$null=EjecutarProceso $dockerRuta @('info','--format','{{.ServerVersion}}') '' 'docker-preflight'}
        catch {throw "Docker está instalado, pero el motor no respondió. Abre Docker Desktop y espera a que termine de iniciar. $($_.Exception.Message)"}
    }
    $Puerto=PuertoLibre $Puerto
    if($Local){$PuertoCorreo=PuertoLibre $PuertoCorreo; $PuertoSmtp=PuertoLibre $PuertoSmtp}
    Write-Host "Entorno aislado: $proyecto; puerto API: $Puerto"
    if($ProbarWeatherReal){
        if([string]::IsNullOrWhiteSpace($env:WEATHER_API_KEY)){throw 'Configura WEATHER_API_KEY en el entorno privado antes de solicitar la prueba real.'}
    }else{EnvLocal 'WEATHER_API_KEY' ''}
    if($Local){
        if(!(Test-Path $MailpitPath)){throw 'Indica el ejecutable oficial de Mailpit mediante -MailpitPath.'}
        foreach($v in Get-ChildItem Env:MP_*){EnvLocal $v.Name $null}
        $mail=Start-Process $MailpitPath -ArgumentList @('--listen',"127.0.0.1:$PuertoCorreo",'--smtp',"127.0.0.1:$PuertoSmtp",'--database',"$evidencia/mailpit.db") -PassThru -WindowStyle Hidden -RedirectStandardOutput "$evidencia/mailpit.out" -RedirectStandardError "$evidencia/mailpit.err"
        $procesos.Add($mail)
        EnvLocal 'ConnectionStrings__DefaultConnection' "Server=(localdb)\MSSQLLocalDB;Database=$base;Trusted_Connection=True;TrustServerCertificate=True;"
        EnvLocal 'Database__ApplyMigrations' 'true'
        EnvLocal 'ASPNETCORE_ENVIRONMENT' 'Development'
        EnvLocal 'AppBaseUrl' "http://localhost:$Puerto"
        EnvLocal 'Documentos__Directorio' "$evidencia/documentos"
        EnvLocal 'EmailDispatcher__Enabled' 'true'
        EnvLocal 'SMTP_HOST' '127.0.0.1'
        EnvLocal 'SMTP_PORT' "$PuertoSmtp"
        EnvLocal 'SMTP_FROM' 'reservas@test.invalid'
        EnvLocal 'SMTP_ENABLE_SSL' 'false'
        EnvLocal 'SMTP_USE_AUTH' 'false'
        EnvLocal 'Logging__LogLevel__Default' 'Warning'
        $api=ArrancarLocal
    }else{
        # Snapshot de trabajo aislado. Incluye archivos nuevos de implementación y excluye ignorados.
        $archivos=EjecutarProceso 'git' @('-C',$raiz,'ls-files','--cached','--others','--exclude-standard') '' 'git'
        foreach($archivo in ($archivos -split '\r?\n' | Where-Object {$_} | Sort-Object -Unique)){
            if($archivo -match '(^|/)\.env($|\.)' -and $archivo -ne '.env.example'){continue}
            $destino=Join-Path $evidencia $archivo
            [void](New-Item -ItemType Directory -Path (Split-Path $destino) -Force)
            Copy-Item -LiteralPath (Join-Path $raiz $archivo) -Destination $destino
        }
        Copy-Item "$evidencia/.env.example" "$evidencia/.env"
        # Solo la copia temporal mantiene un inspector activo para todas las consultas.
        $composeTemporal = Join-Path $evidencia 'docker-compose.yml'
        $config = [IO.File]::ReadAllText($composeTemporal)
        $config = $config.Replace('    profiles: [inspeccion-correo]', "    entrypoint: ['sleep']`n    command: ['86400']")
        [IO.File]::WriteAllText($composeTemporal, $config)
        EnvLocal 'APP_PORT' "$Puerto"
        EnvLocal 'SMTP_HOST' 'mailpit'
        EnvLocal 'SMTP_PORT' '1025'
        EnvLocal 'SMTP_USE_AUTH' 'false'
        EnvLocal 'SMTP_ENABLE_SSL' 'false'
        EnvLocal 'EmailDispatcher__Enabled' 'true'
        $null=Compose @('up','-d','--build')
    }
    EsperarApi
    $swagger=$http.GetStringAsync("http://localhost:$Puerto/swagger/v1/swagger.json").GetAwaiter().GetResult() | ConvertFrom-Json
    $clave='Prueba9'+[guid]::NewGuid().ToString('N')
    $nueva='Cambio8'+[guid]::NewGuid().ToString('N')
    $cuentas=@{}
    foreach($nombre in @('admin','uno','dos','bloqueo')){
        $email="$nombre-$id@test.invalid"
        $antes=@((Buzon '/api/v1/messages').messages.ID)
        $null=Api POST '/api/auth/register' 200 @{nombre=$nombre;email=$email;password=$clave}
        $msg=Mensaje $email $antes
        $viejo=[regex]::Match($msg.Text,'https?://\S+').Value
        if($nombre -eq 'uno'){
            $antes=@((Buzon '/api/v1/messages').messages.ID)
            $null=Api POST '/api/auth/resend-activation' 200 $email
            $msg=Mensaje $email $antes
            $null=Api GET ([uri]$viejo).PathAndQuery 400
        }
        $url=[uri]([regex]::Match($msg.Text,'https?://\S+').Value)
        Check 'Origen SMTP correcto' ($url.Authority -eq "localhost:$Puerto")
        $null=Api GET $url.PathAndQuery 200
        $null=Api GET $url.PathAndQuery 400
        $null=Api POST '/api/auth/register' 409 @{nombre=$nombre;email=$email;password=$clave}
        if($nombre -eq 'admin'){
            if($Local){
                Push-Location "$raiz/backend/SISReservas.Api"
                try{$null=EjecutarProceso 'dotnet' @("$raiz/backend/SISReservas.Api/bin/Debug/net10.0/SISReservas.Api.dll",'--administrador-inicial',$email) '' 'admin'}finally{Pop-Location}
            }else{$null=Compose @('exec','-T','api','dotnet','SISReservas.Api.dll','--administrador-inicial',$email)}
        }
        $login=Api POST '/api/auth/login' 200 @{email=$email;password=$clave}
        $cuentas[$nombre]=@{Email=$email;Token=$login.sessionToken}
        Check 'Login emite sesión' (![string]::IsNullOrEmpty($login.sessionToken))
    }
    $a=$cuentas.admin.Token;$u=$cuentas.uno.Token;$o=$cuentas.dos.Token
    $null=Api GET '/WeatherForecast' 200
    $null=Api GET '/api/auth/me' 401
    $null=Api GET '/api/auth/me' 200 $null $u
    $null=Api POST '/api/auth/logout' 200 $null $o
    $null=Api GET '/api/auth/me' 401 $null $o
    $o=(Api POST '/api/auth/login' 200 @{email=$cuentas.dos.Email;password=$clave}).sessionToken
    $null=Api GET '/api/users' 403 $null $u
    $usuarios=Api GET '/api/users' 200 $null $a
    $otro=($usuarios | Where-Object email -eq $cuentas.dos.Email).id
    $null=Api PUT "/api/users/$otro/role" 403 @{rol=1} $u
    $null=Api PUT "/api/users/$otro/role" 204 @{rol=2} $a
    $null=Api POST "/api/users/$otro/deactivate" 204 $null $a
    $null=Api GET '/api/auth/me' 401 $null $o
    $null=Api POST "/api/users/$otro/activate" 204 $null $a
    $o=(Api POST '/api/auth/login' 200 @{email=$cuentas.dos.Email;password=$clave}).sessionToken
    $null=Api GET '/api/permission-requests/pending' 403 $null $u
    $null=Api GET '/api/permission-requests/pending' 200 $null $a
    $espacios=Api GET '/api/reservas/espacios' 200 $null $u
    $espacio=$espacios[0].id
    $inicio=[datetime]::UtcNow.Date.AddDays(1).AddHours(12);$fin=$inicio.AddHours(1)
    $datos=@{espacioId=$espacio;fechaInicio=$inicio.ToString('o');fechaFin=$fin.ToString('o')}
    $null=Api POST '/api/reservas' 403 $datos $u
    $sol=Api POST '/api/permission-requests' 201 @{permiso=4} $u
    $null=Api POST '/api/reservas' 403 $datos $u
    $null=Api POST "/api/permission-requests/$($sol.id)/approve" 403 $null $u
    $null=Api POST "/api/permission-requests/$($sol.id)/approve" 200 $null $a
    $null=Api POST "/api/permission-requests/$($sol.id)/approve" 409 $null $a
    $null=Api GET '/api/permission-requests/mine' 200 $null $u
    $sol=Api POST '/api/permission-requests' 201 @{permiso=3} $o
    $null=Api POST "/api/permission-requests/$($sol.id)/reject" 400 @{motivoRechazo=''} $a
    $null=Api POST "/api/permission-requests/$($sol.id)/reject" 200 @{motivoRechazo='Evaluación'} $a
    $null=Api POST "/api/permission-requests/$($sol.id)/approve" 409 $null $a
    $query="?inicio=$([uri]::EscapeDataString($inicio.ToString('o')))&fin=$([uri]::EscapeDataString($fin.ToString('o')))"
    $null=Api GET "/api/reservas/espacios/$espacio/disponibilidad$query" 200 $null $u
    $estadoClima=if($ProbarWeatherReal){200}else{503}
    $null=Api GET "/api/reservas/espacios/$espacio/planificacion-exterior$query" $estadoClima $null $u
    $reserva=Api POST '/api/reservas' 201 $datos $u
    $null=Api POST '/api/reservas' 409 $datos $u
    $null=Api POST "/api/reservas/$($reserva.id)/confirmar" 403 $null $u
    $null=Api POST "/api/reservas/$($reserva.id)/confirmar" 200 $null $a
    $null=Api POST "/api/reservas/$($reserva.id)/cancelar" 404 $null $o
    $null=Api POST "/api/reservas/$($reserva.id)/cancelar" 200 $null $u
    $null=Api POST "/api/reservas/$($reserva.id)/confirmar" 409 $null $a
    $null=Api GET '/api/reservas/mine' 200 $null $u
    $null=Api GET '/api/documentos' 401
    $null=Api POST '/api/documentos' 415 @{} $u
    $vacio=Join-Path $evidencia 'vacio.txt'
    [IO.File]::WriteAllText($vacio,'')
    $null=Api POST '/api/documentos' 400 $null $u @{archivo=Get-Item $vacio}
    $archivo=Join-Path $evidencia 'documento.txt'
    [IO.File]::WriteAllText($archivo,'Documento persistente de evaluación')
    $doc=Api POST '/api/documentos' 201 $null $u @{archivo=Get-Item $archivo}
    Check 'Metadatos documento' ($doc.nombreOriginal -eq 'documento.txt' -and $doc.tamano -eq (Get-Item $archivo).Length -and $doc.usuarioId -and $doc.subidoEn)
    $propios=Api GET '/api/documentos' 200 $null $u
    $ajenos=Api GET '/api/documentos' 200 $null $o
    Check 'Listado aislado' ($doc.id -in $propios.id -and @($ajenos).Count -eq 0)
    $null=Api GET "/api/documentos/$($doc.id)/descargar" 200 $null $a
    $contenido=Api GET "/api/documentos/$($doc.id)/descargar" 200 $null $u
    $null=Api GET "/api/documentos/$($doc.id)/descargar" 404 $null $o
    $null=Api DELETE "/api/documentos/$($doc.id)" 404 $null $o
    $null=Api GET '/api/documentos' 200 $null $a
    $null=Api DELETE "/api/documentos/$($doc.id)" 204 $null $u
    $null=Api GET "/api/documentos/$($doc.id)/descargar" 404 $null $a
    $lista=Api GET '/api/documentos' 200 $null $a
    Check 'Eliminado oculto' ($doc.id -notin $lista.id)
    Check 'Eliminación lógica SQL' ((SqlLectura "SELECT COUNT(*) FROM Documentos WHERE Id='$($doc.id)' AND EliminadoEn IS NOT NULL;") -match '^1')
    $persistente=Api POST '/api/documentos' 201 $null $u @{archivo=Get-Item $archivo}
    $antes=Api GET "/api/documentos/$($persistente.id)/descargar" 200 $null $u
    if($Local){Stop-Process -Id $api.Id;$api=ArrancarLocal}else{$null=Compose @('down');$null=Compose @('up','-d')}
    EsperarApi
    $u=(Api POST '/api/auth/login' 200 @{email=$cuentas.uno.Email;password=$clave}).sessionToken
    $lista=Api GET '/api/documentos' 200 $null $u
    Check 'Documento persiste en listado' ($persistente.id -in $lista.id)
    $despues=Api GET "/api/documentos/$($persistente.id)/descargar" 200 $null $u
    Check 'Contenido persiste idéntico' ($antes -ceq $despues -and $despues -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes($archivo)))
    foreach($ruta in @('/api/passwords/recovery',"/api/passwords/$otro/force-reset")){
        $antes=@((Buzon '/api/v1/messages').messages.ID)
        $body=if($ruta.EndsWith('recovery')){@{email=$cuentas.dos.Email}}else{$null}
        $null=Api POST $ruta 200 $body $a
        $msg=Mensaje $cuentas.dos.Email $antes
        $codigo=[regex]::Match($msg.Text,'Código de recuperación: (\S+)').Groups[1].Value
        Check 'Código recibido SMTP' (![string]::IsNullOrEmpty($codigo))
        $null=Api POST '/api/passwords/reset' 200 @{token=$codigo;nuevaPassword=$nueva}
        $null=Api POST '/api/passwords/reset' 400 @{token=$codigo;nuevaPassword=$nueva}
    }
    $null=Api GET '/api/auth/me' 401 $null $o
    $null=Api POST '/api/auth/login' 401 @{email=$cuentas.dos.Email;password=$clave}
    $o=(Api POST '/api/auth/login' 200 @{email=$cuentas.dos.Email;password=$nueva}).sessionToken
    $null=Api POST '/api/passwords/change' 400 @{passwordActual='Incorrecta123';nuevaPassword=$clave} $o
    $null=Api POST '/api/passwords/change' 200 @{passwordActual=$nueva;nuevaPassword=$clave} $o
    for($i=0;$i -lt 5;$i++){$null=Api POST '/api/auth/login' 401 @{email=$cuentas.bloqueo.Email;password='Incorrecta123'}}
    $bloqueo=Api POST '/api/auth/login' 200 @{email=$cuentas.bloqueo.Email;password=$clave}
    Check 'Bloqueo sin sesión' ([string]::IsNullOrEmpty($bloqueo.sessionToken))
    Check 'Siete migraciones SQL' ((SqlLectura 'SELECT COUNT(*) FROM __EFMigrationsHistory;') -match '^7')

    # Práctica 5: cuentas ficticias activadas por SMTP y SQL del proyecto aislado.
    $a=(Api POST '/api/auth/login' 200 @{email=$cuentas.admin.Email;password=$clave}).sessionToken
    $cuentas.admin.Token=$a
    $o=(Api POST '/api/auth/login' 200 @{email=$cuentas.dos.Email;password=$clave}).sessionToken
    $null=Api GET '/api/notificaciones' 401
    $bandeja=Api GET '/api/notificaciones' 200 $null $u
    Check 'Bandeja contiene permiso resuelto y reserva confirmada' ($bandeja.notificaciones.Count -ge 2)
    $notificacion=$bandeja.notificaciones[0].id
    $null=Api PATCH "/api/notificaciones/$notificacion/leida" 404 $null $o
    $null=Api PATCH "/api/notificaciones/$notificacion/leida" 204 $null $u
    $null=Api PATCH "/api/notificaciones/$notificacion/leida" 204 $null $u
    $null=Api GET '/api/notificaciones/preferencias' 200 $null $u
    $null=Api PUT '/api/notificaciones/preferencias' 400 @{} $u
    $null=Api PUT '/api/notificaciones/preferencias' 200 @{correoHabilitado=$false} $u
    $correoAntes=SqlLectura "SELECT COUNT(*) FROM CorreosEnCola WHERE UsuarioId=(SELECT Id FROM Usuarios WHERE Email='$($cuentas.uno.Email)');"
    $internaAntes=(Api GET '/api/notificaciones' 200 $null $u).notificaciones.Count
    $solicitud=Api POST '/api/permission-requests' 201 @{permiso=3} $u
    $null=Api POST "/api/permission-requests/$($solicitud.id)/reject" 200 @{motivoRechazo='Rechazo verificable P5'} $a
    $null=Api POST "/api/permission-requests/$($solicitud.id)/reject" 409 @{motivoRechazo='Duplicado'} $a
    $bandeja=Api GET '/api/notificaciones' 200 $null $u
    Check 'Rechazo con motivo y sin evento duplicado' ($bandeja.notificaciones.Count -eq $internaAntes+1 -and @($bandeja.notificaciones | Where-Object mensaje -like '*Rechazo verificable P5*').Count -eq 1)
    $correoDespues=SqlLectura "SELECT COUNT(*) FROM CorreosEnCola WHERE UsuarioId=(SELECT Id FROM Usuarios WHERE Email='$($cuentas.uno.Email)');"
    Check 'Preferencia no encola correo opcional' ($correoAntes -eq $correoDespues)
    $bandejaOtra=Api GET '/api/notificaciones' 200 $null $o
    Check 'Bandejas aisladas SQL' ($notificacion -notin $bandejaOtra.notificaciones.id)
    $null=Api GET '/api/correos' 403 $null $u
    $null=Api GET '/api/correos' 401
    $cola=Api GET '/api/correos' 200 $null $a
    Check 'Cola sin cuerpo ni tokens' ($cola.correos[0].PSObject.Properties.Name -notcontains 'cuerpo')

    # Datos deterministas de reporte. La misma reserva debe sumar una hora en el primer rango.
    $rango="?inicio=$([uri]::EscapeDataString($inicio.ToString('o')))&fin=$([uri]::EscapeDataString($fin.ToString('o')))"
    $datos.fechaInicio=$inicio.ToString('o');$datos.fechaFin=$fin.ToString('o')
    $vigente=Api POST '/api/reservas' 201 $datos $u
    $null=Api GET "/api/reportes/reservas$rango" 401
    $reporte=Api GET "/api/reportes/reservas$rango" 200 $null $u
    Check 'Reporte SQL horas de reservas' ($reporte.totalReservas -eq 1 -and [Math]::Abs($reporte.totalHoras-1) -lt 0.0001)
    $ajeno=Api GET "/api/reportes/reservas$rango" 200 $null $o
    Check 'Reporte SQL sin datos ajenos' ($ajeno.totalReservas -eq 0)
    $total=Api GET "/api/reportes/reservas$rango" 200 $null $a
    Check 'Reporte Admin coincide con demanda real' ($total.totalReservas -eq 1)
    $vacio="?inicio=$([uri]::EscapeDataString($inicio.AddDays(4).ToString('o')))&fin=$([uri]::EscapeDataString($fin.AddDays(4).ToString('o')))"
    Check 'Otro rango cambia reporte SQL' ((Api GET "/api/reportes/reservas$vacio" 200 $null $a).totalReservas -eq 0)
    $rangoPermisos="?inicio=$([uri]::EscapeDataString([DateTime]::UtcNow.Date.AddDays(-1).ToString('o')))&fin=$([uri]::EscapeDataString([DateTime]::UtcNow.Date.AddDays(1).ToString('o')))"
    $reporte=Api GET "/api/reportes/solicitudes$rangoPermisos" 200 $null $u
    $global=Api GET "/api/reportes/solicitudes$rangoPermisos" 200 $null $a
    Check 'Agregacion SQL permisos y filtro propio' ($reporte.total -ge 2 -and $global.total -gt $reporte.total -and $reporte.grupos.Count -le $reporte.total)
    Check 'Rango vacio permisos' ((Api GET "/api/reportes/solicitudes$vacio" 200 $null $u).total -eq 0)
    $null=Api GET '/api/reportes/solicitudes?inicio=2026-01-01&fin=2026-01-01' 400 $null $u
    $null=Api GET '/api/reportes/reservas?inicio=2026-01-01&fin=2026-01-01' 400 $null $u

    if(!$Local){
        # Pausar solo el despachador del proyecto aislado para controlar los intentos.
        EnvLocal 'EmailDispatcher__Enabled' 'false'
        $null=Compose @('up','-d','--force-recreate','api')
        EsperarApi
        Check 'Preferencia persiste tras recreacion' (!(Api GET '/api/notificaciones/preferencias' 200 $null $u).correoHabilitado)
        Check 'Lectura persiste tras recreacion' (@((Api GET '/api/notificaciones' 200 $null $u).notificaciones | Where-Object { $_.id -eq $notificacion -and $_.leidaEn }).Count -eq 1)
        # Procesar lo pendiente antes de contar exactamente la nueva entrega.
        $null=Compose @('exec','-T','api','dotnet','SISReservas.Api.dll','--procesar-correos')
        $null=Api PUT '/api/notificaciones/preferencias' 200 @{correoHabilitado=$true} $u
        $antesMensajes=(Buzon '/api/v1/messages').total
        $null=Api POST "/api/reservas/$($vigente.id)/confirmar" 200 $null $a
        $antesIds=@((Buzon '/api/v1/messages').messages.ID)
        $docker=ResolverEjecutable 'docker'
        $argumentos=@('compose','-p',$proyecto,'--env-file',"$evidencia/.env",'-f',"$evidencia/docker-compose.yml",'exec','-T','api','dotnet','SISReservas.Api.dll','--procesar-correos')
        $configProceso=@{Exe=$docker;Argumentos=$argumentos}
        $trabajos=@()
        try{
            1..2|ForEach-Object{
                $trabajos+=Start-Job -ScriptBlock {
                    param($config)
                    $ErrorActionPreference='Continue'
                    $commandArguments=$config.Argumentos
                    & $config.Exe @commandArguments 2>&1 | Out-Null
                    if($LASTEXITCODE -ne 0){throw 'Procesador concurrente fallo'}
                } -ArgumentList $configProceso
            }
            $null=$trabajos | Wait-Job -Timeout 60
            if(@($trabajos | Where-Object State -ne Completed).Count){throw 'Los procesadores concurrentes no terminaron correctamente'}
            $trabajos | Receive-Job -ErrorAction Stop | Out-Null
        }finally{
            foreach($trabajo in $trabajos){if($trabajo.State -eq 'Running'){Stop-Job $trabajo}; Remove-Job $trabajo}
        }
        $null=Mensaje $cuentas.uno.Email $antesIds
        Check 'Concurrencia SQL y SMTP sin duplicacion' ((Buzon '/api/v1/messages').total -eq $antesMensajes+1)
        $null=Compose @('exec','-T','api','dotnet','SISReservas.Api.dll','--procesar-correos')
        Check 'Segunda ejecucion no reenvia' ((Buzon '/api/v1/messages').total -eq $antesMensajes+1)
        # SMTP apagado no impide un evento real del negocio.
        $null=Compose @('stop','mailpit')
        $datos.fechaInicio=$inicio.AddHours(3).ToString('o');$datos.fechaFin=$fin.AddHours(3).ToString('o')
        $fallable=Api POST '/api/reservas' 201 $datos $u
        $null=Api POST "/api/reservas/$($fallable.id)/confirmar" 200 $null $a
        $cola=Api GET '/api/correos' 200 $null $a
        $pendiente=@($cola.correos | Where-Object estado -eq 1)[0]
        Check 'Negocio completa con SMTP apagado' ([bool]$pendiente)
        for($intento=0;$intento -lt 3;$intento++){
            $null=Compose @('exec','-T','api','dotnet','SISReservas.Api.dll','--procesar-correos')
        }
        $fallido=@((Api GET '/api/correos' 200 $null $a).correos | Where-Object id -eq $pendiente.id)[0]
        Check 'Tres intentos SQL y Fallido con error' ($fallido.estado -eq 3 -and $fallido.intentos -eq 3 -and $fallido.ultimoError -and $fallido.ultimoIntentoEn)
        $null=Compose @('exec','-T','api','dotnet','SISReservas.Api.dll','--procesar-correos')
        Check 'Fallido no se reprocesa' (@((Api GET '/api/correos' 200 $null $a).correos | Where-Object id -eq $pendiente.id)[0].intentos -eq 3)
        $null=Compose @('start','mailpit')
        $null=Compose @('down')
        $null=Compose @('up','-d')
        EsperarApi
        Check 'Preferencia y notificacion sobreviven down/up' ((Api GET '/api/notificaciones/preferencias' 200 $null $u).correoHabilitado -and @((Api GET '/api/notificaciones' 200 $null $u).notificaciones | Where-Object { $_.id -eq $notificacion -and $_.leidaEn }).Count -eq 1)
        Check 'Cola Fallida persiste tras down/up' (@((Api GET '/api/correos' 200 $null $a).correos | Where-Object id -eq $pendiente.id)[0].estado -eq 3)
        Check 'Reporte SQL persiste tras down/up' ((Api GET "/api/reportes/reservas$rango" 200 $null $u).totalReservas -eq 1)
    }

    $cobertura=foreach($p in $swagger.paths.PSObject.Properties){foreach($m in $p.Value.PSObject.Properties){
        $patron='^'+(($p.Name -split '/' | ForEach-Object {
            if($_ -match '^\{[^}]+\}$'){'[^/]+'}else{[regex]::Escape($_)}
        }) -join '/')+'$'
        [pscustomobject]@{Ruta=$p.Name;Metodo=$m.Name;Ejecutado=[bool]@($resultados | Where-Object { $_.Metodo -eq $m.Name -and $_.Ruta -match $patron }).Count}
    }}
    $cobertura | ConvertTo-Json | Set-Content "$evidencia/cobertura.json" -Encoding UTF8
    Write-Host "Pruebas completadas. Evidencia privada: $evidencia"
} finally {
    $http.Dispose()
    $resultados | ConvertTo-Json -Depth 5 | Set-Content "$evidencia/resultados.json" -Encoding UTF8
    foreach($p in $procesos){if(!$p.HasExited){Stop-Process -Id $p.Id}}
    foreach($nombre in $entorno.Keys){[Environment]::SetEnvironmentVariable($nombre,$entorno[$nombre])}
    Write-Host "Resultados parciales o finales: $evidencia/resultados.json"
}
