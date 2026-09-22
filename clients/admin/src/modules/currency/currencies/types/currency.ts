/** Mirrors Currency.Contracts/Currencies/CurrencyDto.cs — `code` (ISO 4217) is the identity. */
export interface CurrencyDto {
  code: string;
  name: string;
  symbol?: string | null;
  /** Minor-unit digits amounts in this currency are rounded to. */
  decimalPlaces: number;
  isActive: boolean;
  /** True for the single base currency all order calculations are done in. */
  isBase: boolean;
}

/** Mirrors Currency.Contracts/Currencies/CreateCurrencyRequest.cs. */
export interface CreateCurrencyRequest {
  code: string;
  name: string;
  symbol?: string;
  decimalPlaces: number;
}

/** Mirrors Currency.Contracts/Currencies/UpdateCurrencyRequest.cs. */
export interface UpdateCurrencyRequest {
  name: string;
  symbol?: string;
  decimalPlaces: number;
}

/** Mirrors Currency.Contracts/Currencies/SearchCurrencyRequest.cs (`searchValue` matches code or name). */
export interface CurrencySearchParams {
  searchValue?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}
