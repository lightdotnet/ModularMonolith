import { Card, CardContent } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { hasPermission } from "@/lib/server/authorization";
import { requirePermission } from "@/lib/server/require-permission";
import { getCategoryTree } from "@/modules/catalog/api/categories.api";
import { searchProducts } from "@/modules/catalog/api/products.api";
import { CategoryTree } from "@/modules/catalog/components/category-tree";
import { ProductsDataTable } from "@/modules/catalog/components/products-data-table";
import { CATEGORIES_PERMISSIONS, PRODUCTS_PERMISSIONS } from "@/modules/catalog/constants/permissions";
import { flattenCategoryTree } from "@/modules/catalog/types/category";
import { ProductStatus } from "@/modules/catalog/types/product";

const PAGE_SIZE = 10;

interface CatalogPageProps {
  searchParams: Promise<{ q?: string; page?: string; categoryId?: string; status?: string }>;
}

export async function CatalogPage({ searchParams }: CatalogPageProps) {
  const { session, denied } = await requirePermission(PRODUCTS_PERMISSIONS.View);
  if (denied) return denied;

  const canManageProducts = hasPermission(session, PRODUCTS_PERMISSIONS.Manage);
  const canViewCategories = hasPermission(session, CATEGORIES_PERMISSIONS.View);
  const canManageCategories = hasPermission(session, CATEGORIES_PERMISSIONS.Manage);

  const { q, page, categoryId, status } = await searchParams;
  const pageNumber = Math.max(Number(page) || 1, 1);

  const [treeResult, productsResult] = await Promise.all([
    getCategoryTree(),
    searchProducts({
      categoryId: categoryId || undefined,
      status: (status as ProductStatus) || undefined,
      searchValue: q,
      pageNumber,
      pageSize: PAGE_SIZE,
    }),
  ]);

  const categories = treeResult.data ? flattenCategoryTree(treeResult.data) : [];

  const error =
    !productsResult.isSuccess || !productsResult.data
      ? {
          title: "Unable to load products",
          description: productsResult.message || "Please try again.",
        }
      : undefined;
  const paged = productsResult.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Catalog</h1>
        <p className="text-sm text-muted-foreground">Manage products and their categories.</p>
      </div>

      <Tabs defaultValue="products">
        <TabsList>
          <TabsTrigger value="products">Products</TabsTrigger>
          {canViewCategories && <TabsTrigger value="categories">Categories</TabsTrigger>}
        </TabsList>

        <TabsContent value="products">
          <Card>
            <CardContent>
              <ProductsDataTable
                categories={categories}
                categoryId={categoryId ?? ""}
                status={status ?? ""}
                records={paged?.records ?? []}
                searchValue={q ?? ""}
                pageNumber={paged?.pageNumber ?? pageNumber}
                pageSize={paged?.pageSize ?? PAGE_SIZE}
                totalPages={paged?.totalPages ?? 1}
                totalRecords={paged?.totalRecords ?? 0}
                error={error}
                canManage={canManageProducts}
              />
            </CardContent>
          </Card>
        </TabsContent>

        {canViewCategories && (
          <TabsContent value="categories">
            <Card>
              <CardContent>
                <CategoryTree
                  nodes={treeResult.data ?? []}
                  error={!treeResult.isSuccess ? treeResult.message : undefined}
                  canManage={canManageCategories}
                />
              </CardContent>
            </Card>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}
