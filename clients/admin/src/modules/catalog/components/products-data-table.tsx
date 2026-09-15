"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Package, Pencil, Plus } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { activateProductAction } from "@/modules/catalog/api/activate-product-action";
import { deactivateProductAction } from "@/modules/catalog/api/deactivate-product-action";
import { ProductImageThumbnail } from "@/modules/catalog/components/product-image-thumbnail";
import { ProductPanel } from "@/modules/catalog/components/product-panel";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";
import { ProductStatus, type ProductDto } from "@/modules/catalog/types/product";

interface ProductsDataTableProps {
  categories: CategoryTreeNodeDto[];
  categoryId: string;
  status: string;
  records: ProductDto[];
  searchValue: string;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canManage?: boolean;
}

const STATUS_OPTIONS = Object.values(ProductStatus).map((value) => ({ value, label: value }));

function formatPrice(price: number, currency: string): string {
  try {
    return new Intl.NumberFormat("en-US", { style: "currency", currency }).format(price);
  } catch {
    return `${price} ${currency}`;
  }
}

export function ProductsDataTable({
  categories,
  categoryId,
  status,
  records,
  searchValue,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canManage,
}: ProductsDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [, runToggle] = useGuardedAction();

  const [panelOpen, setPanelOpen] = useState(false);
  const [panelKey, setPanelKey] = useState(0);
  const [panelMode, setPanelMode] = useState<"create" | "edit">("create");
  const [selectedProduct, setSelectedProduct] = useState<ProductDto | null>(null);

  const [pendingSearch, setPendingSearch] = useState(searchValue);
  const [lastSearch, setLastSearch] = useState(searchValue);
  if (searchValue !== lastSearch) {
    setLastSearch(searchValue);
    setPendingSearch(searchValue);
  }

  const [pendingCategoryId, setPendingCategoryId] = useState(categoryId);
  const [lastCategoryId, setLastCategoryId] = useState(categoryId);
  if (categoryId !== lastCategoryId) {
    setLastCategoryId(categoryId);
    setPendingCategoryId(categoryId);
  }

  const [pendingStatus, setPendingStatus] = useState(status);
  const [lastStatus, setLastStatus] = useState(status);
  if (status !== lastStatus) {
    setLastStatus(status);
    setPendingStatus(status);
  }

  function navigate(nextParams: Record<string, string | undefined>) {
    const params = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(nextParams)) {
      if (value) params.set(key, value);
      else params.delete(key);
    }

    startTransition(() => {
      router.push(`${pathname}?${params.toString()}`);
    });
  }

  function categoryName(id: string): string {
    return categories.find((category) => category.id === id)?.name ?? "";
  }

  const actions: DataTableAction[] | undefined = canManage
    ? [
        {
          key: "create",
          label: "Create product",
          icon: Plus,
          onClick: () => {
            setPanelMode("create");
            setSelectedProduct(null);
            setPanelKey((key) => key + 1);
            setPanelOpen(true);
          },
        },
      ]
    : undefined;

  const baseColumns: DataTableColumn<ProductDto>[] = [
    {
      id: "product",
      header: "Product",
      hideable: false,
      cell: (product) => (
        <div className="flex items-center gap-3">
          <ProductImageThumbnail url={product.images[0]?.url} />
          <div className="flex flex-col">
            <span className="font-medium">{product.name}</span>
            <span className="text-xs text-muted-foreground">{product.sku}</span>
          </div>
        </div>
      ),
    },
    { id: "category", header: "Category", cell: (product) => categoryName(product.categoryId) },
    { id: "price", header: "Price", cell: (product) => formatPrice(product.price, product.currency) },
    { id: "vatRate", header: "VAT %", cell: (product) => product.vatRate },
    {
      id: "status",
      header: "Status",
      cell: (product) => (
        <Badge variant={product.status === ProductStatus.Active ? "default" : "outline"}>
          {product.status}
        </Badge>
      ),
    },
  ];

  function handleToggleStatus(product: ProductDto) {
    const isActive = product.status === ProductStatus.Active;
    runToggle(
      () => (isActive ? deactivateProductAction(product.id) : activateProductAction(product.id)),
      `"${product.name}" ${isActive ? "deactivated" : "activated"}.`,
      () => router.refresh(),
    );
  }

  const columns: DataTableColumn<ProductDto>[] = canManage
    ? [
        ...baseColumns,
        {
          id: "actions",
          header: "",
          hideable: false,
          cell: (product) => (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button aria-label="Row actions" size="icon" variant="outline">
                  <Pencil />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  onClick={() => {
                    setPanelMode("edit");
                    setSelectedProduct(product);
                    setPanelKey((key) => key + 1);
                    setPanelOpen(true);
                  }}
                >
                  Edit
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleToggleStatus(product)}>
                  {product.status === ProductStatus.Active ? "Deactivate" : "Activate"}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ),
        },
      ]
    : baseColumns;

  const customSearch = (
    <>
      <Input
        className="w-56"
        aria-label="Search products"
        placeholder="Search products..."
        value={pendingSearch}
        onChange={(event) => setPendingSearch(event.target.value)}
      />
      <NativeSelect
        className="w-56"
        aria-label="Filter by category"
        placeholder="All categories"
        value={pendingCategoryId}
        onChange={setPendingCategoryId}
        options={categories.map((category) => ({ value: category.id, label: category.name }))}
      />
      <NativeSelect
        className="w-40"
        aria-label="Filter by status"
        placeholder="All statuses"
        value={pendingStatus}
        onChange={setPendingStatus}
        options={STATUS_OPTIONS}
      />
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(product) => product.id}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            q: pendingSearch || undefined,
            categoryId: pendingCategoryId || undefined,
            status: pendingStatus || undefined,
            page: undefined,
          })
        }
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ page: String(page) })}
        error={error}
        emptyState={{
          icon: Package,
          title: "No products found",
          description: "Try adjusting your search, category or status filter.",
        }}
      />
      <ProductPanel
        key={`panel-${panelKey}`}
        open={panelOpen}
        onOpenChange={setPanelOpen}
        mode={panelMode}
        product={selectedProduct}
        categories={categories}
        onSaved={() => router.refresh()}
      />
    </>
  );
}
