// Estos union types asumen que el backend serializa los enums de C# como string
// (requiere registrar JsonStringEnumConverter en Program.cs — por defecto
// System.Text.Json serializa enums como número). Confirmar al construir los
// Controllers de la API.
export type CardType = 'Credit' | 'Debit';
export type Currency = 'CRC' | 'USD';
export type StatementStatus = 'Open' | 'Closed' | 'Paid';
export type UserRole = 'Owner' | 'Contributor' | 'RestrictedViewer';
