import type { PersonShare } from './personShare';

export interface SplitRule {
  id: string;
  householdId: string;
  descriptionPattern: string;
  defaultSplit: PersonShare[];
}
