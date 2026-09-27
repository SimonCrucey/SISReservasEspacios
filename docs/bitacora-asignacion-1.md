# Bitácora de Interacción con Agente de IA — Asignación 1

**Asignatura:** Programación III (TDS-007) — ITLA  
**Período:** 2026-C-3  
**Estudiante:** Wuanny Simon Crucey Vasquez  
**Repositorio:** [SISReservasEspacios](https://github.com/SimonCrucey/SISReservasEspacios)  
**Herramienta de IA:** Asistente conversacional (Gemini) y OpenCode CLI (VS Code) 

---

## 1. Tarea delegada al agente
Configuración del archivo `.gitignore` adaptado al stack del proyecto (C# / .NET Web API y React) y asistencia en la secuencia de comandos Git para mantener un historial limpio con ramas por funcionalidad y commits atómicos.

---

## 2. Solicitud inicial (Prompt)
> *"Quiero comenzar la asignación prácticamente desde cero... Primero necesito que me indiques cómo comprobar correctamente el estado de mi repositorio local y remoto antes de hacer cambios."*  
> (Posteriormente se solicitó la generación de las reglas de `.gitignore` para .NET y React).

---

## 3. Respuesta devuelta por el agente
El agente proporcionó las reglas completas de exclusión para compilaciones (`[Bb]in/`, `[Oo]bj/`), herramientas de desarrollo (`.vs/`, `.vscode/*`), dependencias de Node (`node_modules/`), y variables de entorno/secretos (`.env*`, `appsettings.*.json`).

---

## 4. Discrepancia / Error detectado
* **Descripción del incidente:** Al entregar el contenido del bloque de exclusiones, las directivas de comentarios y nombres de carpetas (`Thumbs.db##`, `VS`, `React`, directivas de secretos) fueron ingresadas en la consola interactiva de Windows (CMD/PowerShell) en lugar de ser guardadas directamente como un archivo de texto en el editor.
* **Mensaje arrojado:**
  ```text
  'Thumbs.db##' is not recognized as an internal or external command, operable program or batch file.
  'VS' is not recognized as an internal or external command, operable program or batch file.
  'React' is not recognized as an internal or external command, operable program or batch file.
  'secrets.env.env.local.env.*.localappsettings.Development.jsonappsettings.Production.json##' is not recognized as an internal or external command...