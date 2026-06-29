import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserCharm {
  luckyNumber: number | null;
  charmEmoji: string;
  charmTagline: string | null;
  charmRolledAt: string | null;
  hasRolled: boolean;
}

@Injectable({ providedIn: 'root' })
export class UserCharmService {
  constructor(private http: HttpClient) {}

  getCharm(): Observable<UserCharm> {
    return this.http.get<UserCharm>('/api/auth/charm');
  }

  rerollCharm(): Observable<UserCharm> {
    return this.http.post<UserCharm>('/api/auth/charm/reroll', {});
  }
}
