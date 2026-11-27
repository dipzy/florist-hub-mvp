export type OrderStatusVariant = 'pending' | 'confirmed' | 'progress' | 'ready' | 'delivered' | 'cancelled' | 'rejected';

export function orderStatusVariant(status: number): OrderStatusVariant {
  switch (status) {
    case 0:
      return 'pending';
    case 1:
      return 'confirmed';
    case 2:
      return 'progress';
    case 3:
      return 'ready';
    case 4:
      return 'delivered';
    case 5:
      return 'cancelled';
    case 6:
      return 'rejected';
    default:
      return 'pending';
  }
}
