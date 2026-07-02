import type { UserRole } from './enums';

export interface User {
  id: string;
  householdId: string;
  name: string;
  email: string;
  role: UserRole;
}
