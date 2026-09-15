"use client";

import { useActionState, useEffect, useState } from "react";
import { Trash2 } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect } from "@/components/ui/native-select";
import { Sheet, SheetClose, SheetContent, SheetFooter, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Spinner } from "@/components/ui/spinner";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { getProductByIdAction } from "@/modules/catalog/api/get-product-by-id-action";
import { upsertProductAction, type UpsertProductFormState } from "@/modules/catalog/api/upsert-product-action";
import { ProductImageThumbnail } from "@/modules/catalog/components/product-image-thumbnail";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";
import type { ProductDto, ProductImageDto } from "@/modules/catalog/types/product";

const initialState: UpsertProductFormState = {};

interface ProductPanelProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  mode: "create" | "edit";
  product: ProductDto | null;
  categories: CategoryTreeNodeDto[];
  onSaved: () => void;
}

export function ProductPanel({ open, onOpenChange, mode, product, categories, onSaved }: ProductPanelProps) {
  const [state, formAction, pending] = useActionState(upsertProductAction, initialState);

  useActionSuccessToast(state, mode === "create" ? "Product created." : "Product updated.", () => {
    onSaved();
    onOpenChange(false);
  });

  const [loading, setLoading] = useState(mode === "edit");
  const [loadError, setLoadError] = useState("");
  const [loadedProduct, setLoadedProduct] = useState<ProductDto | null>(null);
  const [images, setImages] = useState<ProductImageDto[]>([]);
  const [newImageUrl, setNewImageUrl] = useState("");
  const [newImageSortOrder, setNewImageSortOrder] = useState("");

  // Edit mode: the row this panel was opened with may carry a stale `images`
  // array — load the current product once on open, same "fetch full detail
  // on open" pattern as manage-product-images-dialog.tsx used to. The panel
  // remounts fresh (via `key`) on every open, so this only runs once per open.
  useEffect(() => {
    if (mode !== "edit" || !product) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getProductByIdAction(product.id);
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load product.");
        setImages(product.images);
        setLoading(false);
        return;
      }

      setLoadedProduct(result.data);
      setImages(result.data.images);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch once per mount; the panel remounts fresh (via `key`) on every open
  }, []);

  function handleAddImage() {
    if (!newImageUrl.trim()) return;

    setImages((prev) => [
      ...prev,
      { url: newImageUrl.trim(), sortOrder: newImageSortOrder ? Number(newImageSortOrder) : undefined },
    ]);
    setNewImageUrl("");
    setNewImageSortOrder("");
  }

  function handleRemoveImage(url: string) {
    setImages((prev) => prev.filter((image) => image.url !== url));
  }

  if (mode === "edit" && !product) return null;

  // Falls back to the (possibly stale) row prop if the fresh fetch failed,
  // so the form still has something to seed defaults from.
  const sourceProduct = mode === "edit" ? (loadedProduct ?? product) : null;
  const showLoading = mode === "edit" && loading;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg" onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex h-full flex-col gap-4">
          <SheetHeader>
            <SheetTitle>{mode === "create" ? "Create product" : "Edit product"}</SheetTitle>
          </SheetHeader>

          {mode === "edit" && <input type="hidden" name="id" value={product?.id ?? ""} />}

          {showLoading ? (
            <div className="flex flex-1 items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
              <Spinner />
              Loading product...
            </div>
          ) : (
            <div className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 pb-4">
              {state.error && (
                <Alert variant="destructive">
                  <AlertDescription>{state.error}</AlertDescription>
                </Alert>
              )}
              {loadError && (
                <Alert variant="destructive">
                  <AlertDescription>{loadError}</AlertDescription>
                </Alert>
              )}

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="prod-name">Name</Label>
                <Input id="prod-name" name="name" defaultValue={sourceProduct?.name} required />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="prod-description">Description</Label>
                <Textarea
                  id="prod-description"
                  name="description"
                  defaultValue={sourceProduct?.description ?? ""}
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="flex flex-col gap-1.5">
                  <NativeSelect
                    label="Category"
                    name="categoryId"
                    required
                    defaultValue={sourceProduct?.categoryId}
                    options={categories.map((category) => ({
                      value: category.id,
                      label: category.name,
                    }))}
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="prod-sku">SKU</Label>
                  <Input
                    id="prod-sku"
                    name="sku"
                    defaultValue={sourceProduct?.sku}
                    required={mode === "create"}
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="prod-price">Price</Label>
                  <Input
                    id="prod-price"
                    name="price"
                    type="number"
                    min="0"
                    step="0.01"
                    defaultValue={sourceProduct?.price}
                    required
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="prod-vat-rate">VAT rate (%)</Label>
                  <Input
                    id="prod-vat-rate"
                    name="vatRate"
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    defaultValue={sourceProduct?.vatRate}
                    required
                  />
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <Label>Images</Label>
                {images.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No images yet.</p>
                ) : (
                  <div className="flex flex-col gap-2">
                    {images.map((image) => (
                      <div
                        key={image.url}
                        className="flex items-center gap-2 rounded-md border border-border px-2.5 py-1.5"
                      >
                        <ProductImageThumbnail url={image.url} />
                        <span className="flex-1 truncate text-sm">{image.url}</span>
                        <span className="text-xs text-muted-foreground">{image.sortOrder ?? ""}</span>
                        <Button
                          aria-label="Remove image"
                          onClick={() => handleRemoveImage(image.url)}
                          size="icon-xs"
                          type="button"
                          variant="outline"
                        >
                          <Trash2 />
                        </Button>
                      </div>
                    ))}
                  </div>
                )}

                <div className="flex items-end gap-2">
                  <div className="flex flex-1 flex-col gap-1.5">
                    <Label htmlFor="image-url">Image URL</Label>
                    <Input
                      id="image-url"
                      value={newImageUrl}
                      onChange={(event) => setNewImageUrl(event.target.value)}
                    />
                  </div>
                  <div className="flex w-24 flex-col gap-1.5">
                    <Label htmlFor="image-sort-order">Sort order</Label>
                    <Input
                      id="image-sort-order"
                      type="number"
                      value={newImageSortOrder}
                      onChange={(event) => setNewImageSortOrder(event.target.value)}
                    />
                  </div>
                  <Button disabled={!newImageUrl.trim()} onClick={handleAddImage} type="button">
                    Add
                  </Button>
                </div>
              </div>

              <input type="hidden" name="imagesJson" value={JSON.stringify(images)} />
            </div>
          )}

          <SheetFooter>
            <div className="flex justify-end gap-2">
              <SheetClose asChild>
                <Button type="button" variant="outline">
                  Cancel
                </Button>
              </SheetClose>
              <Button type="submit" loading={pending} disabled={showLoading}>
                {mode === "create" ? "Create" : "Save"}
              </Button>
            </div>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
