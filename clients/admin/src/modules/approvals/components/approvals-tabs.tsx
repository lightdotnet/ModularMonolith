"use client";

import { useCallback, useEffect, useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type { DataTableErrorState } from "@/components/shared/data-table";
import { getAwaitingApprovalsAction } from "@/modules/approvals/api/get-awaiting-approvals-action";
import { getMyApprovalRequestsAction } from "@/modules/approvals/api/get-my-approval-requests-action";
import { searchAllApprovalsAction } from "@/modules/approvals/api/search-all-approvals-action";
import { AllApprovalsTable } from "@/modules/approvals/components/all-approvals-table";
import {
  ApprovalHistoryTable,
  type ApprovalOwnerRole,
} from "@/modules/approvals/components/approval-history-table";
import { MyApprovalsTable } from "@/modules/approvals/components/my-approvals-table";
import type { ApprovalRequestDto } from "@/modules/approvals/types/approval";

interface ApprovalsTabsProps {
  canViewAll: boolean;
  /** userId -> display name, resolved once by the page for every table + the history sheet. */
  userNamesById: Map<string, string>;
}

type TabState<T> =
  | { status: "loading" }
  | { status: "loaded"; value: T }
  | { status: "error"; message: string };

interface MyRequestsValue {
  records: ApprovalRequestDto[];
  rolesById: Map<string, ApprovalOwnerRole[]>;
}

/**
 * Each tab's search request is fetched only once that tab is actually activated — the default
 * tab fetches on mount, the other two fetch (and are then cached) the first time they're clicked
 * — instead of eagerly loading all three lists on every page navigation.
 */
export function ApprovalsTabs({ canViewAll, userNamesById }: ApprovalsTabsProps) {
  const [activeTab, setActiveTab] = useState("awaiting");
  const [awaiting, setAwaiting] = useState<TabState<ApprovalRequestDto[]>>({ status: "loading" });
  const [mine, setMine] = useState<TabState<MyRequestsValue> | null>(null);
  const [all, setAll] = useState<TabState<ApprovalRequestDto[]> | null>(null);

  const loadAwaiting = useCallback(async (): Promise<TabState<ApprovalRequestDto[]>> => {
    const result = await getAwaitingApprovalsAction();
    return result.data
      ? { status: "loaded", value: result.data }
      : { status: "error", message: result.error || "Please try again." };
  }, []);

  const fetchAwaiting = useCallback(async () => {
    setAwaiting({ status: "loading" });
    setAwaiting(await loadAwaiting());
  }, [loadAwaiting]);

  const fetchMine = useCallback(async () => {
    setMine({ status: "loading" });
    const result = await getMyApprovalRequestsAction();
    setMine(
      result.records
        ? {
            status: "loaded",
            value: { records: result.records, rolesById: new Map(result.roles) },
          }
        : { status: "error", message: result.error || "Please try again." },
    );
  }, []);

  const fetchAll = useCallback(async () => {
    setAll({ status: "loading" });
    const result = await searchAllApprovalsAction();
    setAll(
      result.data
        ? { status: "loaded", value: result.data }
        : { status: "error", message: result.error || "Please try again." },
    );
  }, []);

  // Fetches the default tab's data on mount — inlined (rather than calling `fetchAwaiting`
  // directly) so state updates land after the `await`, not synchronously in the effect body.
  useEffect(() => {
    let cancelled = false;

    (async () => {
      const next = await loadAwaiting();
      if (!cancelled) setAwaiting(next);
    })();

    return () => {
      cancelled = true;
    };
  }, [loadAwaiting]);

  function handleTabChange(value: string) {
    setActiveTab(value);
    if (value === "mine" && mine === null) fetchMine();
    if (value === "all" && all === null && canViewAll) fetchAll();
  }

  const awaitingError: DataTableErrorState | undefined =
    awaiting.status === "error"
      ? { title: "Unable to load your approvals", description: awaiting.message }
      : undefined;
  const mineError: DataTableErrorState | undefined =
    mine?.status === "error"
      ? { title: "Unable to load your requests", description: mine.message }
      : undefined;
  const allError: DataTableErrorState | undefined =
    all?.status === "error"
      ? { title: "Unable to load approval requests", description: all.message }
      : undefined;

  return (
    <Tabs value={activeTab} onValueChange={handleTabChange}>
      <TabsList>
        <TabsTrigger value="awaiting">Waiting on your decision</TabsTrigger>
        <TabsTrigger value="mine">My requests</TabsTrigger>
        {canViewAll && <TabsTrigger value="all">All requests</TabsTrigger>}
      </TabsList>

      <TabsContent value="awaiting">
        <MyApprovalsTable
          records={awaiting.status === "loaded" ? awaiting.value : []}
          isLoading={awaiting.status === "loading"}
          error={awaitingError}
          userNamesById={userNamesById}
          onRefresh={fetchAwaiting}
        />
      </TabsContent>

      <TabsContent value="mine">
        <ApprovalHistoryTable
          records={mine?.status === "loaded" ? mine.value.records : []}
          isLoading={mine === null || mine.status === "loading"}
          error={mineError}
          userNamesById={userNamesById}
          rolesById={mine?.status === "loaded" ? mine.value.rolesById : new Map()}
          onRefresh={fetchMine}
        />
      </TabsContent>

      {canViewAll && (
        <TabsContent value="all">
          <AllApprovalsTable
            records={all?.status === "loaded" ? all.value : []}
            isLoading={all === null || all.status === "loading"}
            error={allError}
            canCreate
            userNamesById={userNamesById}
            onRefresh={fetchAll}
          />
        </TabsContent>
      )}
    </Tabs>
  );
}
