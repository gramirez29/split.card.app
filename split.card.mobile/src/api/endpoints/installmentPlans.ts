import { apiRequest } from '@/api/client';
import type { InstallmentPlan } from '@/types/installmentPlan';

export function getInstallmentPlan(planId: string) {
  return apiRequest<InstallmentPlan>(`/api/installment-plans/${planId}`);
}
