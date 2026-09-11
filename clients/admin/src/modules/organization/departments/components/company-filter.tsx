"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { CompanySelect } from "@/modules/organization/companies/components/company-select";
import type { CompanyDto } from "@/modules/organization/companies/types/company";

interface CompanyFilterProps {
  companies: CompanyDto[];
  companyId: string;
}

/** Drives the `?companyId=` URL param that scopes the org-unit tree / employee levels panel below it. */
export function CompanyFilter({ companies, companyId }: CompanyFilterProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  function handleChange(nextCompanyId: string) {
    const params = new URLSearchParams(searchParams.toString());
    if (nextCompanyId) params.set("companyId", nextCompanyId);
    else params.delete("companyId");
    router.push(`${pathname}?${params.toString()}`);
  }

  return (
    <div className="max-w-xs">
      <CompanySelect companies={companies} value={companyId} onChange={handleChange} />
    </div>
  );
}
