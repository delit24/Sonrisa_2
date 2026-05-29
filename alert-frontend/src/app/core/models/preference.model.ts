export type Category =
  'breaking_news' | 'market' | 'natural_disaster' | 'tech';

export interface AlertPreference {
  id: number;
  category: Category;
  keyword: string | null;
  isActive: boolean;
  createdAt: string;
}

export const CATEGORY_LABELS: Record<Category, string> = {
  breaking_news: 'Breaking News',
  market: 'Market',
  natural_disaster: 'Natural Disaster',
  tech: 'Tech'
};
