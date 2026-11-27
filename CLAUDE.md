# Accounting Register — Backend

API en ASP.NET Core 8 + EF Core (SQL Server) para un registro de **emisores de valores** (securities issuers)
y **empresas cliente**, con datos australianos (ABN, ACN, estados, códigos postales).
El frontend Angular vive en otro repo: `diego1302/Accounting_Register_FE`.

## Comandos
```bash
dotnet build
dotnet run --launch-profile http          # http://localhost:5222 (Swagger en /swagger, requiere login)
dotnet ef migrations add <Nombre>         # requiere: dotnet tool install --global dotnet-ef --version 9.0.6
dotnet ef database update
```
La cadena de conexión está en `appsettings.json` (`DefaultConnection`, Trusted_Connection a una máquina local).
No hay tests automatizados; para probar a mano usar `Accounting_Register_BE.http`.

## Estructura
- `Program.cs` — servicios, CORS (`http://localhost:4200`), Identity, Swagger, enums JSON como string.
- `DataBase/ApplicationDBContext.cs` — `IdentityDbContext<ApplicationUser>`; mapeo de direcciones.
- `Areas/Config/Security_Issuer/` — modelo y controlador de emisores.
- `Areas/Config/Users/` — `ApplicationUser`, `CompanyClient`, controladores de clientes y perfil.
- `Areas/Accounting/` — contabilidad por empresa cliente: `Model/` (entidades + DTOs), `Services/` (`StatementParser` CSV/OFX,
  `AccountingService` asientos y GST, `DefaultChartOfAccounts`), `Controller/` (cuentas, bancos/extractos, conciliación).
- `Resources/` — modelos compartidos (`Addreses`, `Countries`, `PaginationModel`, `PagedResult`) y `CountriesController`.
- `Migrations/` — migraciones EF (no editar a mano las ya aplicadas).

## Endpoints
| Ruta | Notas |
|---|---|
| `/api/auth/*` | ASP.NET Identity API (`MapGroup("/api/auth").MapIdentityApi`): `register`, `login`, `refresh`, `forgotPassword`, `manage/info`… Tokens bearer de Identity (no JWT propio). |
| `/api/profile` | GET/PUT nombre, segundo nombre, apellido y teléfono del usuario actual (Identity `/register` solo acepta email + password). |
| `/api/securities-issuers` | CRUD; GET lista acepta `q`, `page`, `limit` y devuelve `{ items, pagination }`. |
| `/api/company-clients` | Igual que emisores. |
| `/api/countries` | CRUD; GET devuelve array, filtro `q`. |
| `/api/companies/{companyId}/accounts` | Plan de cuentas (CRUD, `?includeArchived`), `POST load-default` carga el plan australiano estándar. |
| `/api/companies/{companyId}/bank-accounts` | CRUD (crea su cuenta contable tipo `Bank`, código 090…); `POST {id}/statements/preview` y `POST {id}/statements` (multipart `file` + `mapping` JSON opcional); `GET/DELETE {id}/statements`; `GET {id}/transactions?status&q&page&limit`. |
| `/api/companies/{companyId}/bank-transactions/{id}/reconcile` | `POST` contabiliza (genera asiento), `DELETE` deshace. |
Todos excepto `/api/auth/*` llevan `[Authorize]`.

## Convenciones y trampas
- `Nullable` está activado: un `string` no anulable se vuelve **obligatorio** en el model binding y `NOT NULL` en BD.
  Los campos opcionales deben ser `string?`/`long?`.
- **Direcciones** (`Addreses` → `AustralianAddress` / `OverseasAddress`) son owned types guardados en la misma tabla.
  Las navegaciones están configuradas como requeridas (`ConfigureAddress` en el DbContext), por eso se inicializan con
  `= new()` y los controladores llaman a `NormaliseAddresses` antes de guardar. Si se agrega un owned type nuevo, seguir el mismo patrón.
- `CompanyClient.registered_users` tiene `[JsonIgnore]`: nunca serializar `ApplicationUser` (contiene hashes de contraseña).
- El JSON sale en camelCase: `Name` → `name`, `ABN` → `abn`, `OverseasAddress` → `overseasAddress`; propiedades snake_case quedan igual.
- Hay dos `DbSet<SecuritiesIssuer>` (`SecurtiesIssuer` con typo y `securities_Issuer`); usar `SecurtiesIssuer`. No renombrar sin migración.
- Nombres de propiedades mezclan estilos (`aplha2code` con typo, snake_case); el frontend depende de ellos, cambiar en ambos lados.
- **Contabilidad**: todo cuelga de `CompanyClientId`. Montos de `BankTransaction`: > 0 entra, < 0 sale. Conciliar crea un
  `JournalEntry` de partida doble; las líneas de conciliación son montos brutos y con `TaxType.GST` se separa 1/11 a la cuenta
  con `SystemKey = "GST"` (se crea sola si falta). Duplicados de extractos: índice único `(BankAccountId, DedupeKey)`.
  Todas las FK de contabilidad son `Restrict` (SQL Server no admite rutas de cascada múltiples): los borrados se hacen
  explícitos en los controladores, y una empresa con contabilidad no se puede eliminar.
- Los controladores se ubican por atributo `[Route("api/...")]`, no por convención de Areas.
