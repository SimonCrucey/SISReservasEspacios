# Máquina de estados de Reserva

## Entidad central

La entidad central utilizada para la máquina de estados es `Reserva`.

Cada reserva posee un atributo `Estado` de tipo `EstadoReserva`.

## Estados

| Estado     | Descripción                                             | Terminal |
| ---------- | ------------------------------------------------------- | -------- |
| Pendiente  | La reserva fue creada y está pendiente de confirmación. | No       |
| Confirmada | La reserva fue confirmada.                              | No       |
| Cancelada  | La reserva fue cancelada y no puede continuar.          | Sí       |
| Completada | La reserva terminó correctamente.                       | Sí       |

## Transiciones

| Desde      | Hacia      | Quién la ejecuta      | Condición                                                 |
| ---------- | ---------- | --------------------- | --------------------------------------------------------- |
| Pendiente  | Confirmada | Administrador         | Espacio activo, inicio anterior al fin e inicio no pasado; sin solapamientos con otras reservas Pendientes o Confirmadas, excluyendo la propia. |
| Pendiente  | Cancelada  | Propietario/Administrador | La reserva es cancelada antes de confirmarse.             |
| Confirmada | Completada | Sistema               | La fecha y hora de la reserva han finalizado.             |
| Confirmada | Cancelada  | Propietario/Administrador | La reserva es cancelada después de haber sido confirmada. |

## Transiciones prohibidas

| Desde      | Hacia      | Resultado |
| ---------- | ---------- | --------- |
| Cancelada  | Confirmada | Prohibida |
| Cancelada  | Completada | Prohibida |
| Completada | Cancelada  | Prohibida |
| Completada | Confirmada | Prohibida |

Las transiciones permitidas y prohibidas están centralizadas en `TransicionesReserva`.

Los estados están centralizados en `EstadoReserva`.

## Estado terminal

`Cancelada` y `Completada` son estados terminales. Una vez que una reserva llega a cualquiera de ellos, no puede volver a un estado anterior.

## Ejecución del negocio

Los endpoints de Reservas exigen sesión válida con el esquema `Session` y rol
`Estandar` o `Administrador`. El servicio comprueba además usuario activo, rol y
propiedad: un Estándar solo consulta sus reservas y cancela las propias.

Crear una reserva requiere el permiso solicitable `ReservarEspacios` para el
usuario Estándar. Gestión de permisos lo concede después de la aprobación de un
Administrador; los estados de esa solicitud son independientes de los de Reserva.

Las entradas de fechas utilizan UTC (sufijo `Z`). La creación y la consulta de
disponibilidad requieren un intervalo no pasado con inicio anterior al fin.
Los solapamientos se comprueban con `inicioNuevo < finExistente && finNuevo > inicioExistente`;
los intervalos adyacentes se permiten. Solo bloquean las reservas Pendientes y
Confirmadas del mismo espacio.

La finalización se ofrece mediante `ReservaService.CompletarAsync`, un método
interno sin endpoint público. Exige que la fecha final haya llegado y delega la
validez de la transición a `TransicionesReserva.AsegurarPermitida`, como los otros
cambios de estado. Su invocación automática por una tarea del sistema queda pendiente.

## Cobertura exacta por fila (Práctica 3)

Todas las pruebas se encuentran en tests/SISReservas.Api.Tests. Las teorías
declaran los casos esperados independientemente del mapa de producción y tienen
Arrange, Act y Assert. No se requiere levantar la aplicación.

| Fila permitida | Prueba de mapa | Prueba de ejecución/condición |
| --- | --- | --- |
| Pendiente → Confirmada | TransicionesReservaTests.AsegurarPermitida_TransicionDocumentada_NoRechaza(Pendiente, Confirmada) | ReservaServiceTests.Confirmar_AdministradorYCondicionesValidas_ConfirmaExcluyendoPropiaReserva |
| Pendiente → Cancelada | misma teoría (Pendiente, Cancelada) | ReservaServiceTests.Cancelar_PropietarioOAdministrador_Cancela(Pendiente, false/true) |
| Confirmada → Completada | misma teoría (Confirmada, Completada) | ReservaServiceTests.Completar_SoloPermiteIntervaloFinalizado(true/false) |
| Confirmada → Cancelada | misma teoría (Confirmada, Cancelada) | ReservaServiceTests.Cancelar_PropietarioOAdministrador_Cancela(Confirmada, false/true) |

| Fila prohibida | Prueba de rechazo y estado intacto |
| --- | --- |
| Cancelada → Confirmada | TransicionesReservaTests.AsegurarPermitida_ProhibidaDocumentada_RechazaSinCambiarReserva(Cancelada, Confirmada) |
| Cancelada → Completada | misma teoría (Cancelada, Completada) |
| Completada → Cancelada | misma teoría (Completada, Cancelada) |
| Completada → Confirmada | misma teoría (Completada, Confirmada) |

La tabla prohibida enumera cuatro ejemplos explícitos, no todas las combinaciones
rechazadas. Toda pareja ausente del mapa Permitidas se rechaza, incluidas
autotransiciones y valores inválidos. AsegurarPermitida_DesdeTerminal_RechazaCualquierDestino
cubre los cuatro destinos desde cada terminal; EsTerminal deriva del mapa.
La teoría AsegurarPermitida_EnumInvalido_RechazaSinCambiarReserva cubre enums inválidos.

La prueba del mapa valida el guard y después simula la asignación; la ejecución
real se cubre adicionalmente en Transicion_DesdeTerminal_RechazaConEstadoPersistidoIntacto
(confirmar/cancelar) y Completar_EstadoNoPermitido_NoModifica (completar).

Primera transición condicionada: ConfirmarAsync exige Administrador y llama a
CumpleCondicionesConfirmacionAsync antes de cambiar estado. La condición comprueba
inicio < fin, inicio >= hora UTC actual, espacio activo y ausencia de conflictos
excluyendo la reserva. Confirmar_CondicionIncumplida_RechazaSinCambiarEstado cubre
inactivo, fechas, conflicto y pasado; Confirmar_Estandar_RechazaAunqueSeaPropietario
cubre el rol. Se considera la condición de negocio para el defecto 3; no se ha
plantado todavía. Las fechas UTC de entrada se validan al crear/consultar;
confirmación no exige DateTime.Kind porque SQL datetime2 no conserva esa marca.
