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
| Pendiente  | Confirmada | Administrador         | La reserva cumple las condiciones para ser confirmada.    |
| Pendiente  | Cancelada  | Usuario/Administrador | La reserva es cancelada antes de confirmarse.             |
| Confirmada | Completada | Sistema               | La fecha y hora de la reserva han finalizado.             |
| Confirmada | Cancelada  | Usuario/Administrador | La reserva es cancelada después de haber sido confirmada. |

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
