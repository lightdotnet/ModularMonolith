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
  createProductAction,
  type CreateProductFormState,
} from "@/modules/catalog/api/create-product-action";
import type { CategoryTreeNodeDto } from "@/modules/catalog/types/category";

const initialState: CreateProductFormState = {};

interface CreateProductDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  categories: CategoryTreeNodeDto[];
  onCreated: () => void;
}

export function CreateProductDialog({
  open,
  onOpenChange,
  categories,
  onCreated,
}: CreateProductDialogProps) {
  const [state, formAction, pending] = useActionState(createProductAction, initialState);

  useActionSuccessToast(state, "Product created.", () => {
    onCreated();
    onOpenChange(false);
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-lg"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <form action={formAction} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>Create product</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="prod-name">Name</Label>
            <Input id="prod-name" name="name" required />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="prod-description">Description</Label>
            <Textarea id="prod-description" name="description" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <NativeSelect
                label="Category"
                name="categoryId"
                required
                options={categories.map((category) => ({
                  value: category.id,
                  label: category.name,
                }))}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prod-sku">SKU</Label>
              <Input id="prod-sku" name="sku" required />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prod-price">Price</Label>
              <Input id="prod-price" name="price" type="number" min="0" step="0.01" required />
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
              Create
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
