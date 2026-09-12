"use client";

import { useActionState } from "react";
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
import { NativeSelect } from "@/components/ui/native-select";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  updateProductAction,
  type UpdateProductFormState,
} from "@/modules/catalog/api/update-product-action";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";
import type { ProductDto } from "@/modules/catalog/types/product";

const initialState: UpdateProductFormState = {};

interface EditProductDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  product: ProductDto | null;
  categories: CategoryTreeNodeDto[];
  onUpdated: () => void;
}

export function EditProductDialog({
  open,
  onOpenChange,
  product,
  categories,
  onUpdated,
}: EditProductDialogProps) {
  const [state, formAction, pending] = useActionState(updateProductAction, initialState);

  useActionSuccessToast(state, "Product updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!product) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-lg"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Edit product</DialogTitle>
        </DialogHeader>

        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={product.id} />

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-prod-name">Name</Label>
            <Input id="edit-prod-name" name="name" defaultValue={product.name} required />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-prod-description">Description</Label>
            <Textarea
              id="edit-prod-description"
              name="description"
              defaultValue={product.description ?? ""}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="Category"
              name="categoryId"
              required
              defaultValue={product.categoryId}
              options={categories.map((category) => ({
                value: category.id,
                label: category.name,
              }))}
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="edit-prod-price">Price</Label>
              <Input
                id="edit-prod-price"
                name="price"
                type="number"
                min="0"
                step="0.01"
                defaultValue={product.price}
                required
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="edit-prod-vat-rate">VAT rate (%)</Label>
              <Input
                id="edit-prod-vat-rate"
                name="vatRate"
                type="number"
                min="0"
                max="100"
                step="0.01"
                defaultValue={product.vatRate}
                required
              />
            </div>
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" loading={pending}>
              Save
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
