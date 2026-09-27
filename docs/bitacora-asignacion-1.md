# Bitácora de Interacción con Agente de IA — Asignación 1

**Asignatura:** Programación III (TDS-007) — ITLA
**Período:** 2026-C-3
**Estudiante:** Wuanny Simon Crucey Vasquez
**Repositorio:** SISReservasEspacios
**Herramientas de IA:** Asistente conversacional (Gemini) y OpenCode CLI (VS Code)

---

## 1. Tarea delegada al agente

Se utilizó un agente de IA como apoyo para configurar el archivo `.gitignore` de acuerdo con el stack utilizado en el proyecto, principalmente C#/.NET Web API y React.

También se solicitó asistencia para organizar la secuencia de comandos Git utilizada durante la asignación, manteniendo un historial limpio, ramas por funcionalidad y commits con cambios específicos.

---

## 2. Solicitud inicial (Prompt)

Se solicitó al agente orientación para comenzar la asignación y comprobar correctamente el estado del repositorio local y remoto antes de realizar cambios.

Posteriormente se solicitó la generación de reglas para el archivo `.gitignore`, tomando en cuenta las tecnologías utilizadas en el proyecto.

---

## 3. Respuesta devuelta por el agente

El agente proporcionó reglas de exclusión para diferentes archivos y carpetas que no deberían formar parte del control de versiones.

Entre ellas se incluyeron reglas relacionadas con:

* Archivos generados por compilaciones de .NET, como `bin/` y `obj/`.
* Archivos y configuraciones de herramientas de desarrollo, como `.vs/` y `.vscode/`.
* Dependencias del frontend, como `node_modules/`.
* Archivos relacionados con variables de entorno y configuraciones locales.
* Archivos generados por el sistema operativo, como `Thumbs.db`.

La intención era utilizar estas reglas para evitar subir archivos generados, configuraciones locales y posibles secretos al repositorio.

---

## 4. Discrepancia / Error detectado

Durante la interacción ocurrió un error al utilizar la respuesta proporcionada por el agente.

Parte del contenido destinado al archivo `.gitignore` fue ingresado accidentalmente en la consola de Windows como si fueran comandos, en lugar de tratarlo como contenido de texto para el archivo.

La consola mostró mensajes indicando que algunos textos no eran reconocidos como comandos, por ejemplo:

```text
'Thumbs.db##' is not recognized as an internal or external command,
operable program or batch file.

'VS' is not recognized as an internal or external command,
operable program or batch file.

'React' is not recognized as an internal or external command,
operable program or batch file.
```

También se produjo un mensaje similar relacionado con las reglas de secretos y configuraciones locales.

---

## 5. Cómo se detectó el error

El error se detectó porque Windows estaba interpretando el contenido del `.gitignore` como comandos de la consola.

Los mensajes mostraban que elementos como `VS`, `React` y `Thumbs.db` estaban siendo tratados como programas o comandos que debían ejecutarse.

Esto permitió identificar que el problema no estaba en las reglas del `.gitignore`, sino en la forma en que se estaba introduciendo el contenido en la consola.

---

## 6. Cómo se corrigió

Se dejó de introducir el contenido del `.gitignore` directamente como comandos de la consola y se utilizó el archivo `.gitignore` como un archivo de texto, colocando las reglas dentro del archivo correspondiente.

Posteriormente se verificó el estado del repositorio con Git para comprobar los cambios realizados y continuar con el flujo normal de trabajo.

Este incidente permitió diferenciar entre los comandos que deben ejecutarse en la terminal y el contenido que debe guardarse dentro de archivos de configuración.

---

## 7. Resultado final

Después de corregir el error, se continuó con el flujo de trabajo establecido para la asignación:

* Uso de ramas para separar cambios.
* Commits específicos para cada modificación.
* Creación de Pull Requests.
* Revisión de cambios mediante Pull Requests.
* Comentarios sobre líneas concretas de los cambios.
* Aprobación y merge de los Pull Requests.

La interacción con el agente sirvió como apoyo para configurar el repositorio y resolver problemas durante el proceso, pero las modificaciones y verificaciones finales fueron realizadas sobre el repositorio mediante Git y GitHub.
