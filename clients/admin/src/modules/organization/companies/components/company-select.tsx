"use client";

import { NativeSelect } from "@/components/ui/native-select";
import type { CompanyDto } from "@/modules/organization/companies/types/company";

interface CompanySelectProps {
  id?: string;
  name?: string;
  companies: CompanyDto[];
  value: string;
  onChange: (companyId: string) => void;
  placeholder?: string;
  className?: string;
}

/** Dumb presentational company dropdown — the caller owns fetching the `companies` list (there is no unbounded "get all companies" endpoint, so callers page it with a generously large `pageSize`). Shared by the Departments and Employees features. */
export function CompanySelect({
  id,
  name,
  companies,
  value,
  onChange,
  placeholder = "Select a company",
  className,
}: CompanySelectProps) {
  return (
    <NativeSelect
      id={id}
      name={name}
      className={className}
      placeholder={placeholder}
      value={value}
      onChange={onChange}
      options={companies.map((company) => ({
        value: company.id,
        label: `${company.name} (${company.code})`,
      }))}
    />
  );
}
