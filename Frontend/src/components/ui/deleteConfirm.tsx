import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
export interface DeleteConfirmationProps {
  onDelete: () => void;
  descriptionText: string;
  confirmationText?: string;
  children: React.ReactElement;
  deleteButtonClassName?: string;
  onCancel?: () => void;
}

export function DeleteConfirmation({
  onDelete,
  descriptionText,
  confirmationText,
  deleteButtonClassName,
  children,
  onCancel,
}: DeleteConfirmationProps) {
  return (
    <AlertDialog>
      <AlertDialogTrigger render={children}>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Tem certeza?</AlertDialogTitle>
          <AlertDialogDescription>{descriptionText}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={onCancel}>Cancelar</AlertDialogCancel>
          <AlertDialogAction className={"bg-red-500 hover:bg-red-600 " + (deleteButtonClassName || "")} onClick={onDelete}>{confirmationText}</AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
