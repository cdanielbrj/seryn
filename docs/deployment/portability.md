# User-data portability

The authenticated **Export data** and **Import data** areas transfer information entered by the Seryn user. They never include or replace host administration data such as the local user, credentials, sessions, Data Protection keys, access history, or deployment configuration.

## Export

Seryn exports one canonical `.xlsx` workbook with separate sheets:

- `Registros`: original financial records and readable classification references;
- `Objetivos`: objectives, including their original inclusion date;
- `Tipos`: every user-defined financial type, including unused values;
- `Categorias`: every root and child category, including unused values;
- `Pagamentos`: every user-defined payment method, including unused values.

Internal database IDs are not exported. Relationships use readable names, and every record row contains the information required to reconstruct its type, category hierarchy, and payment method. The workbook contains original user data, never expanded projections or host state.

## Import

Import accepts `.xlsx` and `.xlsm` OpenXML workbooks up to 10 MB. `.xlsm` is only a compatible container: macros are never executed or persisted. Formulas in Seryn columns are rejected. Styles, unknown sheets, and other external content have no import semantics.

Recognized sheets require the exact Seryn columns and valid values. A recognized but incompatible sheet makes the preview fail instead of being interpreted heuristically. Missing sheets are allowed for merge imports and never imply deletion.

The preview reports new and equivalent rows for every recognized sheet. Equivalence uses normalized classification names and the complete meaningful data of records and objectives. New rows receive new internal IDs; exact equivalents are retained without duplication. Because the workbook intentionally has no stable internal identity, import never guesses that an edited row should update a different existing row.

Two confirmed operations are available:

- **Merge data** creates new rows and keeps equivalent and unrelated current data.
- **Replace all data** requires all five canonical sheets, reports the current item count, deletes all current user-entered records, objectives, and classifications, and imports only the workbook content.

Replacement has a separate destructive confirmation. Deletion and import run in one database transaction, so any failure preserves the previous data. Neither operation changes authentication or host configuration.

Spreadsheet text is neutralized against formula execution during export. Keep exported workbooks private because they contain financial planning data.

## Backup boundary

User-data portability is suitable for migration and recovery of information entered through the application, but it is not a complete installation backup. It excludes credentials, sessions, keys, and deployment state and does not replace the persistent-volume procedure in [Docker operations](docker.md#persistent-data-and-backup).
