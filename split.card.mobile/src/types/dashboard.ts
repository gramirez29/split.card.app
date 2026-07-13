export interface PersonTotal {
  personId: string;
  personName: string;
  totalCrc: number;
  totalUsd: number;
}

export interface CardTotal {
  cardId: string;
  cardName: string;
  totalCrc: number;
  totalUsd: number;
}

export interface HouseholdDashboard {
  grandTotalCrc: number;
  grandTotalUsd: number;
  byPerson: PersonTotal[];
  byCard: CardTotal[];
}
