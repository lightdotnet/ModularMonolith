/**
 * Mirrors Currency.Contracts/ExchangeRates/ExchangeRateDto.cs — one recorded rate:
 * 1 unit of `currencyCode` = `rate` units of the base currency. `id` is a backend `long`
 * (serialized as a JSON number), so it is only ever used through `String(id)`.
 */
export interface ExchangeRateDto {
  id: string | number;
  currencyCode: string;
  rate: number;
  effectiveFrom: string;
  recordedBy: string;
  note?: string | null;
}

/** Mirrors Currency.Contracts/ExchangeRates/RecordExchangeRateRequest.cs. */
export interface RecordExchangeRateRequest {
  currencyCode: string;
  rate: number;
  effectiveFrom: string;
  note?: string;
}

/** Mirrors Currency.Contracts/ExchangeRates/SearchExchangeRateRequest.cs (`from`/`to` bound `effectiveFrom`, inclusive). */
export interface ExchangeRateSearchParams {
  currencyCode?: string;
  from?: string;
  to?: string;
  pageNumber?: number;
  pageSize?: number;
}
