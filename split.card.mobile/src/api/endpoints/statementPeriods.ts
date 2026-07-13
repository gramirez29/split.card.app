// No existe un endpoint que devuelva un StatementPeriod directamente — el backend solo
// expone las transacciones de un periodo (GET /api/cards/{cardId}/transactions?date=...,
// ver api/endpoints/transactions.ts) y el PDF de conciliación
// (GET /api/cards/{cardId}/statement-pdf?date=..., ver api/endpoints/reconciliation.ts).
// La función que vivía acá antes (getStatementPeriod) pegaba a
// /api/cards/{cardId}/statement-period, que nunca existió en el backend real.
export {};
