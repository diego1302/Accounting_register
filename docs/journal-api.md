# API de asientos contables para apps externas

Permite que un sistema externo (POS, nómina, e-commerce…) registre asientos de partida doble en una empresa cliente.

## Autenticación
1. En la app: **Configuración → Integraciones (API) → Nueva clave API** (con la empresa elegida arriba).
2. La clave (`ar_…`) se muestra **una sola vez**. Guárdala como secreto del sistema externo.
3. Envíala en cada llamada en el header `X-Api-Key`.

Cada clave solo sirve para **su empresa** y solo para `/journal-entries` (no ve bancos, clientes ni reportes).
Se puede revocar en cualquier momento; los asientos ya registrados se mantienen.

## Registrar un asiento
`POST /api/companies/{companyId}/journal-entries`

```json
{
  "date": "2026-11-27",
  "reference": "Z-1001",
  "narration": "Ventas POS del día",
  "externalSource": "pos",
  "externalId": "z-1001",
  "lines": [
    { "accountCode": "610", "debit": 1100, "description": "Cobro tarjetas" },
    { "accountCode": "200", "credit": 1000 },
    { "accountCode": "820", "credit": 100, "description": "GST" }
  ]
}
```

| Campo | Obligatorio | Notas |
|---|---|---|
| `date` | sí | `yyyy-MM-dd` |
| `narration` | sí | Descripción, máx. 500 |
| `reference` | no | Máx. 100 |
| `externalId` | recomendado | Tu id del documento. Reenviar el mismo **no duplica**: devuelve el asiento existente con **200** (el primero responde **201**). |
| `externalSource` | no | Sistema de origen; por defecto, el nombre de la clave API. La unicidad es por empresa + `externalSource` + `externalId`. |
| `lines[]` | sí | Mínimo 2, máximo 500 |
| `lines[].accountCode` | sí* | Código del plan de cuentas de la empresa (o `accountId`) |
| `lines[].debit` / `credit` | sí | **Uno de los dos** mayor que cero por línea, máx. 2 decimales |
| `lines[].description` | no | |
| `lines[].taxType` | no | `GST`, `GSTFree`, `BASExcluded`, `InputTaxed` (informativo) |

Reglas: la suma de débitos debe ser igual a la de créditos; las cuentas deben existir y no estar archivadas.
El GST no se calcula: va como una línea más a la cuenta de GST (820 en el plan estándar).
Si se registra contra una cuenta bancaria, ese movimiento no aparece en la conciliación bancaria.

## Lote
`POST /api/companies/{companyId}/journal-entries/batch` con `{ "entries": [ ... ] }` (máx. 100).
Es **todo o nada**: si un asiento no es válido no se guarda ninguno. La respuesta indica por asiento si fue `created` o `existing`.

## Consultar y anular
- `GET /api/companies/{companyId}/journal-entries?from=&to=&source=External&externalId=&q=&page=&limit=`
- `GET /api/companies/{companyId}/journal-entries/{id}`
- `DELETE /api/companies/{companyId}/journal-entries/{id}`: una clave API solo puede eliminar asientos externos.

## Respuestas
| Código | Significado |
|---|---|
| 201 | Creado |
| 200 | Ya existía (mismo `externalId`) o consulta |
| 400 | No válido; `errors` trae el detalle por campo, p. ej. `lines[1].accountCode` |
| 401 | Falta la clave o está revocada |
| 403 | La clave es de otra empresa |
| 404 | Empresa o asiento inexistente |
