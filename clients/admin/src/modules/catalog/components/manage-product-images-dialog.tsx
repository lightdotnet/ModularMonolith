"use client";

import { useEffect, useState } from "react";
import { Trash2 } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { addProductImageAction } from "@/modules/catalog/api/add-product-image-action";
import { getProductByIdAction } from "@/modules/catalog/api/get-product-by-id-action";
import { removeProductImageAction } from "@/modules/catalog/api/remove-product-image-action";
import type { ProductDto, ProductImageDto } from "@/modules/catalog/types/product";

interface ManageProductImagesDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  product: ProductDto | null;
  onChanged: () => void;
}

export function ManageProductImagesDialog({
  open,
  onOpenChange,
  product,
  onChanged,
}: ManageProductImagesDialogProps) {
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [images, setImages] = useState<ProductImageDto[]>([]);
  const [url, setUrl] = useState("");
  const [sortOrder, setSortOrder] = useState("");
  const [adding, runAdd] = useGuardedAction();
  const [removingUrl, setRemovingUrl] = useState<string | null>(null);
  const [removing, runRemove] = useGuardedAction();

  // The row this dialog was opened with may carry a stale `images` array —
  // load the current product once on open, same "fetch full detail on open"
  // pattern as move-location-dialog.tsx.
  useEffect(() => {
    if (!product) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getProductByIdAction(product.id);
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load product images.");
        setLoading(false);
        return;
      }

      setImages(result.data.images);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch once per mount; the dialog remounts fresh (via `key`) on every open
  }, []);

  function refetch() {
    if (!product) return;
    onChanged();
    getProductByIdAction(product.id).then((result) => {
      if (result.data) setImages(result.data.images);
    });
  }

  function handleAdd() {
    if (!product || !url.trim()) return;

    runAdd(
      () =>
        addProductImageAction(product.id, {
          url: url.trim(),
          sortOrder: sortOrder ? Number(sortOrder) : undefined,
        }),
      "Image added.",
      () => {
        setUrl("");
        setSortOrder("");
        refetch();
      },
    );
  }

  function handleRemove(imageUrl: string) {
    if (!product) return;

    setRemovingUrl(imageUrl);
    runRemove(() => removeProductImageAction(product.id, imageUrl), "Image removed.", refetch);
  }

  if (!product) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Manage images — {product.name}</DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
            <Spinner />
            Loading images...
          </div>
        ) : loadError ? (
          <Alert variant="destructive">
            <AlertDescription>{loadError}</AlertDescription>
          </Alert>
        ) : (
          <div className="flex flex-col gap-4">
            {images.length === 0 ? (
              <p className="text-sm text-muted-foreground">No images yet.</p>
            ) : (
              <div className="flex flex-col gap-2">
                {images.map((image) => (
                  <div
                    key={image.url}
                    className="flex items-center gap-2 rounded-md border border-border px-2.5 py-1.5"
                  >
                    <span className="flex-1 truncate text-sm">{image.url}</span>
                    <span className="text-xs text-muted-foreground">{image.sortOrder ?? ""}</span>
                    <Button
                      aria-label="Remove image"
                      disabled={removing && removingUrl === image.url}
                      loading={removing && removingUrl === image.url}
                      onClick={() => handleRemove(image.url)}
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
                <Input id="image-url" value={url} onChange={(event) => setUrl(event.target.value)} />
              </div>
              <div className="flex w-24 flex-col gap-1.5">
                <Label htmlFor="image-sort-order">Sort order</Label>
                <Input
                  id="image-sort-order"
                  type="number"
                  value={sortOrder}
                  onChange={(event) => setSortOrder(event.target.value)}
                />
              </div>
              <Button disabled={!url.trim()} loading={adding} onClick={handleAdd} type="button">
                Add
              </Button>
            </div>
          </div>
        )}

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Close
            </Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
