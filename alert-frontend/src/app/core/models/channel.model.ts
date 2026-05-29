export type ChannelType = 'email' | 'slack';

export interface NotificationChannel {
  id: number;
  channel: ChannelType;
  destination: string;
  isActive: boolean;
}
