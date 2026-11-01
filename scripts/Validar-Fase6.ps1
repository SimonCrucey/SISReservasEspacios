param([int]$Puerto = 15064)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent
$cadena = 'Server=(localdb)\MSSQLLocalDB;Database=SISReservas_Practica3_Test;Trusted_Connection=True;TrustServerCertificate=True;'
$conexion = [System.Data.SqlClient.SqlConnection]::new($cadena)
$resultados = [System.Collections.Generic.List[object]]::new()
$api = $null
function Sql([string]$texto, [hashtable]$parametros = @{}) {
    if ($conexion.Database -cne 'SISReservas_Practica3_Test') { throw 'Catalogo incorrecto' }
    $comando = $conexion.CreateCommand(); $comando.CommandText = $texto
    foreach ($clave in $parametros.Keys) { [void]$comando.Parameters.AddWithValue($clave, $parametros[$clave]) }
    try { return $comando.ExecuteScalar() } finally { $comando.Dispose() }
}
function Comprobar([string]$nombre, [bool]$condicion) {
    if (!$condicion) { throw ('FALLO: ' + $nombre) }
    $resultados.Add([pscustomobject]@{Prueba=$nombre;Resultado='APROBADO'})
    Write-Output ('APROBADO: ' + $nombre)
}
function Http([string]$nombre, [string]$metodo, [string]$ruta, [int]$esperado, $cuerpo = $null, [string]$token = '') {
    $opciones = @{ Uri="http://127.0.0.1:$Puerto$ruta"; Method=$metodo; SkipHttpErrorCheck=$true; TimeoutSec=30 }
    if ($token) { $opciones.Headers = @{ Authorization="Bearer $token" } }
    if ($null -ne $cuerpo) { $opciones.Body = ConvertTo-Json -InputObject $cuerpo -Compress; $opciones.ContentType='application/json' }
    $respuesta = Invoke-WebRequest @opciones
    if ([int]$respuesta.StatusCode -ne $esperado) { throw "$nombre esperaba HTTP $esperado y recibio $($respuesta.StatusCode)" }
    $resultados.Add([pscustomobject]@{Prueba=$nombre;Resultado="HTTP $esperado"})
    Write-Host "$nombre : HTTP $esperado"
    if ($respuesta.Content) { try { return ConvertFrom-Json -InputObject $respuesta.Content } catch { return $respuesta.Content } }
}
function Arrancar {
    $env:ASPNETCORE_ENVIRONMENT='Development'
    $env:ConnectionStrings__DefaultConnection=$cadena
    $env:EmailDispatcher__Enabled='false'
    $env:AppBaseUrl="http://127.0.0.1:$Puerto"
    $env:Logging__LogLevel__Default='Warning'
    $dll = Join-Path $raiz 'backend/SISReservas.Api/bin/Debug/net10.0/SISReservas.Api.dll'
    $log = Join-Path ([IO.Path]::GetTempPath()) ('sisreservas-fase6-' + [guid]::NewGuid().ToString('N'))
    $script:api = Start-Process -FilePath 'dotnet' -ArgumentList @(('"' + $dll + '"'),'--urls',"http://127.0.0.1:$Puerto") -WorkingDirectory (Join-Path $raiz 'backend/SISReservas.Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput ($log+'.out') -RedirectStandardError ($log+'.err')
    for ($intento=0; $intento -lt 40; $intento++) {
        if ($api.HasExited) { throw "API termino; revisar $log" }
        try { $r = Invoke-WebRequest "http://127.0.0.1:$Puerto/swagger/v1/swagger.json" -TimeoutSec 1; if ($r.StatusCode -eq 200) { return } } catch {}
        Start-Sleep -Milliseconds 250
    }
    throw 'API no disponible'
}
try {
    $conexion.Open()
    if ((Sql 'SELECT COUNT(*) FROM Usuarios') -ne 0) { throw 'La prueba requiere el catalogo nuevo sin usuarios. No se modifican datos preexistentes.' }
    Comprobar 'Cuatro migraciones en catalogo aislado' ((Sql 'SELECT COUNT(*) FROM __EFMigrationsHistory') -eq 4)
    Arrancar
    $password = 'Prueba9' + [guid]::NewGuid().ToString('N')
    $nueva = 'Nueva8' + [guid]::NewGuid().ToString('N')
    $cuentas = @{}
    foreach ($nombre in @('admin','estandar','otro')) {
        $email = "$nombre@fase6.test.invalid"
        $null = Http "Registro $nombre (correo solo en cola)" POST '/api/auth/register' 200 @{nombre=$nombre;email=$email;password=$password}
        $cuerpo = Sql 'SELECT TOP(1) Cuerpo FROM CorreosEnCola WHERE Destinatario=@email ORDER BY CreadoEn DESC' @{'@email'=$email}
        $enlace = [regex]::Match($cuerpo, 'https?://\S+').Value
        $uri = [uri]$enlace
        Comprobar "Origen configurado de activacion $nombre" ($uri.Authority -eq "127.0.0.1:$Puerto")
        $null = Http "Activacion $nombre con token extraido de cola, sin SMTP" GET $uri.PathAndQuery 200
        $null = Http "Activacion de un uso $nombre" GET $uri.PathAndQuery 400
        if ($nombre -eq 'admin') { $null = Sql "UPDATE Usuarios SET Rol='Administrador' WHERE Email=@email; SELECT 1" @{'@email'=$email} }
        $login = Http "Login $nombre" POST '/api/auth/login' 200 @{email=$email;password=$password}
        Comprobar "Login devuelve token $nombre" (![string]::IsNullOrWhiteSpace($login.sessionToken))
        $cuentas[$nombre] = $login.sessionToken
    }
    $a=$cuentas.admin; $u=$cuentas.estandar; $o=$cuentas.otro
    $null = Http 'Sin sesion' GET '/api/reservas/mine' 401
    $null = Http 'Estandar no accede a bandeja administrativa' GET '/api/permission-requests/pending' 403 $null $u
    $null = Http 'Logout' POST '/api/auth/logout' 200 $null $o
    $null = Http 'Sesion revocada' GET '/api/auth/me' 401 $null $o
    $login = Http 'Nuevo login otro' POST '/api/auth/login' 200 @{email='otro@fase6.test.invalid';password=$password}; $o=$login.sessionToken
    $espacio = [int](Sql "INSERT INTO Espacios (Nombre,Tipo,Activo) OUTPUT INSERTED.Id VALUES ('Salon Prueba','Salon',1)")
    $inactivo = [int](Sql "INSERT INTO Espacios (Nombre,Tipo,Activo) OUTPUT INSERTED.Id VALUES ('Equipo Inactivo','Equipo',0)")
    $espacios = @(Http 'Listar espacios activos' GET '/api/reservas/espacios' 200 $null $u)
    Comprobar 'Excluye espacio inactivo' ($espacios.Count -eq 1 -and $espacios[0].id -eq $espacio)
    $inicio=[DateTime]::UtcNow.Date.AddDays(3).AddHours(12); $fin=$inicio.AddHours(1)
    $reserva=@{espacioId=$espacio;fechaInicio=$inicio.ToString('o');fechaFin=$fin.ToString('o')}
    $null = Http 'Reserva sin permiso' POST '/api/reservas' 403 $reserva $u
    Comprobar 'Sin reserva tras rechazo' ((Sql 'SELECT COUNT(*) FROM Reservas') -eq 0)
    $solicitud=Http 'Solicitar ReservarEspacios' POST '/api/permission-requests' 201 @{permiso=4} $u
    $null = Http 'Permiso pendiente no autoriza' POST '/api/reservas' 403 $reserva $u
    $null = Http 'Estandar no aprueba' POST "/api/permission-requests/$($solicitud.id)/approve" 403 $null $u
    $aprobada=Http 'Administrador aprueba' POST "/api/permission-requests/$($solicitud.id)/approve" 200 $null $a
    Comprobar 'Solicitud Aplicada' ($aprobada.estado -eq 4)
    $null=Http 'Doble aprobacion rechazada' POST "/api/permission-requests/$($solicitud.id)/approve" 409 $null $a
    $rechazo=Http 'Otra solicitud' POST '/api/permission-requests' 201 @{permiso=3} $o
    $null=Http 'Rechazo sin motivo' POST "/api/permission-requests/$($rechazo.id)/reject" 400 @{motivoRechazo=''} $a
    $rechazada=Http 'Administrador rechaza' POST "/api/permission-requests/$($rechazo.id)/reject" 200 @{motivoRechazo='Prueba controlada'} $a
    Comprobar 'Solicitud Rechazada' ($rechazada.estado -eq 3)
    $null=Http 'Rechazada no se aprueba' POST "/api/permission-requests/$($rechazo.id)/approve" 409 $null $a
    $disponibilidad="/api/reservas/espacios/$espacio/disponibilidad?inicio=$([uri]::EscapeDataString($inicio.ToString('o')))&fin=$([uri]::EscapeDataString($fin.ToString('o')))"
    $libre=Http 'Disponibilidad inicial' GET $disponibilidad 200 $null $u
    Comprobar 'Horario inicialmente libre' ($libre.disponible -eq $true)
    $creada=Http 'Crear con permiso aprobado' POST '/api/reservas' 201 $reserva $u
    Comprobar 'Reserva nace Pendiente' ($creada.estado -eq 1)
    $id=$creada.id
    $null=Http 'Rechazo solapamiento' POST '/api/reservas' 409 $reserva $u
    $ocupado=Http 'Disponibilidad ocupada' GET $disponibilidad 200 $null $u
    Comprobar 'Horario ocupado' ($ocupado.disponible -eq $false)
    $null=Http 'Estandar no confirma' POST "/api/reservas/$id/confirmar" 403 $null $u
    $null=Http 'Otro propietario no cancela' POST "/api/reservas/$id/cancelar" 404 $null $o
    $ajenas=@(Http 'Reservas propias otro usuario' GET '/api/reservas/mine' 200 $null $o)
    Comprobar 'Otro usuario no ve reservas ajenas' ($ajenas.Count -eq 0)
    $confirmada=Http 'Confirmar Administrador' POST "/api/reservas/$id/confirmar" 200 $null $a
    Comprobar 'Reserva Confirmada' ($confirmada.estado -eq 2)
    $adyacente=@{espacioId=$espacio;fechaInicio=$fin.ToString('o');fechaFin=$fin.AddHours(1).ToString('o')}
    $ady=Http 'Horario adyacente permitido' POST '/api/reservas' 201 $adyacente $u
    $cancelada=Http 'Propietario cancela confirmada' POST "/api/reservas/$id/cancelar" 200 $null $u
    Comprobar 'Reserva Cancelada' ($cancelada.estado -eq 3)
    $null=Http 'Terminal no confirma' POST "/api/reservas/$id/confirmar" 409 $null $a
    $null=Http 'Terminal no cancela de nuevo' POST "/api/reservas/$id/cancelar" 409 $null $u
    Comprobar 'Terminal persistido intacto' ((Sql 'SELECT Estado FROM Reservas WHERE Id=@id' @{'@id'=$id}) -eq 'Cancelada')
    $invalida=@{espacioId=$espacio;fechaInicio=$fin.ToString('o');fechaFin=$inicio.ToString('o')}
    $null=Http 'Fechas invalidas' POST '/api/reservas' 400 $invalida $u
    $invalida=@{espacioId=$inactivo;fechaInicio=$inicio.ToString('o');fechaFin=$fin.ToString('o')}
    $null=Http 'Espacio inactivo' POST '/api/reservas' 404 $invalida $u
    $null=Http 'Solicitar recuperacion (cola)' POST '/api/passwords/recovery' 200 @{email='otro@fase6.test.invalid'}
    $cuerpo=Sql "SELECT TOP(1) Cuerpo FROM CorreosEnCola WHERE Destinatario='otro@fase6.test.invalid' AND Asunto=N'Recuperación de contraseña' ORDER BY CreadoEn DESC"
    $codigo=[regex]::Match($cuerpo,'Código de recuperación: (\S+)').Groups[1].Value
    Comprobar 'Codigo de recuperacion en cola, no enviado' (![string]::IsNullOrWhiteSpace($codigo))
    $null=Http 'Restablecer password sin SMTP' POST '/api/passwords/reset' 200 @{token=$codigo;nuevaPassword=$nueva}
    $null=Http 'Codigo no reutilizable' POST '/api/passwords/reset' 400 @{token=$codigo;nuevaPassword=$password}
    $null=Http 'Recuperacion revoca sesion anterior' GET '/api/auth/me' 401 $null $o
    $viejo=Http 'Login password anterior' POST '/api/auth/login' 401 @{email='otro@fase6.test.invalid';password=$password}
    $nuevo=Http 'Login password nueva' POST '/api/auth/login' 200 @{email='otro@fase6.test.invalid';password=$nueva}
    Comprobar 'Nueva password produce sesion' (![string]::IsNullOrWhiteSpace($nuevo.sessionToken))
    $antes=@(Http 'RD09 antes de reinicio' GET '/api/reservas/mine' 200 $null $u)
    Stop-Process -Id $api.Id; $api.WaitForExit(); $api=$null
    Arrancar
    $despues=@(Http 'RD09 despues de reinicio' GET '/api/reservas/mine' 200 $null $u)
    Comprobar 'RD09 reserva Pendiente persiste' (@($despues | Where-Object { $_.id -eq $ady.id -and $_.estado -eq 1 }).Count -eq 1)
    Comprobar 'RD09 contenido completo conserva identidad fechas y estado' ((ConvertTo-Json -InputObject $antes -Compress) -ceq (ConvertTo-Json -InputObject $despues -Compress))
    Comprobar 'Correo no fue enviado' ((Sql "SELECT COUNT(*) FROM CorreosEnCola WHERE Estado <> 'Pendiente'") -eq 0)
    $resultados | ConvertTo-Json | Set-Content (Join-Path $raiz 'docs/evidencia-fase-6.json') -Encoding utf8
    Write-Output ('TOTAL VERIFICACIONES: ' + $resultados.Count)
} finally {
    if ($null -ne $api -and !$api.HasExited) { Stop-Process -Id $api.Id; $api.WaitForExit() }
    $conexion.Dispose()
}
