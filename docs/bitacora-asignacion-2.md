# Bitácora de sesión con el agente — Asignación 2

**Proyecto:** SISReservasEspacios
**Asignatura:** Programación III (TDS-007) — ITLA
**Período:** 2026-C-3
**Estudiante:** Wuanny Simon Crucey Vasquez
**Herramienta de IA:** OpenCode CLI
**Fecha:** 20 de octubre de 2026

## 1. Tarea delegada

Le pedí al agente que auditara el estado final de la Asignación 2 antes de publicar el tag `asignacion-2`. La tarea consistía en revisar el historial de Git, comparar los commits `2cb9245` y `e02677e` con la etiqueta, comprobar los cambios en `README.md` y `docs/bitacora-asignacion-2.md`, verificar la sincronización con `origin/main` y recomendar los comandos de publicación sin modificar Git.

## 2. Qué le pedí

El prompt solicitó:

- Revisar el historial y comparar `2cb9245`, `e02677e` y la etiqueta `asignacion-2`.
- Identificar los cambios en `README.md` y `docs/bitacora-asignacion-2.md`.
- Verificar si el tag contenía la documentación actualizada de RD-09.
- Comprobar la sincronización entre `main` local y `origin/main`.
- Recomendar los comandos para publicar el commit y actualizar la etiqueta.
- No modificar archivos ni ejecutar operaciones Git sin autorización.

## 3. Qué devolvió el agente

El agente presentó este diagnóstico:

- `e02677e` era un commit local con cambios en `README.md` y `docs/bitacora-asignacion-2.md`.
- `origin/main` y la etiqueta `asignacion-2` seguían en `2cb9245`.
- En la versión etiquetada, RD-09 aparecía como pendiente.
- En `e02677e`, la documentación describía la prueba de persistencia de RD-09.
- Recomendó publicar el commit, actualizar la etiqueta y verificar el resultado remoto.

## 4. Error del agente

Durante el diagnóstico, el agente incluyó `PasswordPolicyTests.cs` entre los puntos pendientes de investigar y resolver para cerrar la asignación.

Ese archivo es una prueba personal, ajena a la Asignación 2, y no forma parte de los entregables. La recomendación partía de una suposición incorrecta sobre el alcance.

### 4.1 Cómo lo detecté

Revisé la lista de pendientes y reconocí que `PasswordPolicyTests.cs` era un ejercicio personal, no un archivo de la asignación. El agente lo había tratado como parte del cierre.

### 4.2 Cómo lo corregí

Aclaré el alcance con este prompt correctivo:

> Existe un archivo sin seguimiento llamado `PasswordPolicyTests.cs`, pero es un ejercicio personal mío, ajeno a esta asignación. No lo incluyas, no lo investigues y no lo recomiendes para esta entrega.

Después de la corrección, el agente concentró el diagnóstico en Git y documentación, y el archivo quedó fuera de los commits y de los comandos de publicación.

## 5. Evidencia de la sesión

- Prompt correctivo textual incluido en la sección 4.2.
- Diagnóstico de Git: `main` local en `e02677e`, `origin/main` y etiqueta en `2cb9245`.
- Cambios detectados: `README.md` y `docs/bitacora-asignacion-2.md`.
- Resultado final: la versión publicada en `main` y en el tag `asignacion-2` incluye el README con la guía RF-GP y esta bitácora.
- `PasswordPolicyTests.cs` no se incluyó en ningún commit ni en el tag.

## 6. Resultado

La auditoría permitió detectar que la etiqueta no incluía la actualización documental y corregir el alcance del agente. La versión final quedó publicada en `main` y en el tag `asignacion-2`.

## 7. Aprendizaje

Revisar el alcance de las recomendaciones del agente evita tratar archivos personales como entregables. También es necesario verificar que el tag apunte a la versión que realmente se entrega.
