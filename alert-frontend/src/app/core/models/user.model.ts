export interface User {
  id: number;
  email: string;
  fullName: string;
  role: 'admin' | 'user';
  isActive: boolean;
  createdAt: string;
  channelCount?: number;
  preferenceCount?: number;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: {
    id: number;
    email: string;
    fullName: string;
    role: 'admin' | 'user';
  };
}
