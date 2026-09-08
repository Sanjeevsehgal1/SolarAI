import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResponse, UserProfile, SUPER_ADMIN_ROLES, COMPANY_ADMIN_ROLES, PLANT_ADMIN_ROLES, ANALYTICS_ROLES, BILLING_ROLES } from '../../models';
import { PlantContextService } from './plant-context.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'solarshield_auth';
  currentUser = signal<LoginResponse | null>(this.loadUser());

  constructor(
    private http: HttpClient,
    private router: Router,
    private plantContext: PlantContextService
  ) {
    const user = this.currentUser();
    if (user && this.token) {
      this.plantContext.setAccessibleSites(user.accessibleSites ?? []);
      this.refreshProfile();
    }
  }

  private refreshProfile(): void {
    this.http.get<UserProfile>(`${environment.apiUrl}/users/profile`).subscribe({
      next: profile => {
        const sites = profile.assignedSites ?? [];
        this.plantContext.setAccessibleSites(sites);
        const current = this.currentUser();
        if (!current) return;
        const updated: LoginResponse = {
          ...current,
          role: profile.role,
          accessibleSites: sites,
          organizationId: profile.organizationId,
          organizationName: profile.organizationName
        };
        localStorage.setItem(this.storageKey, JSON.stringify(updated));
        this.currentUser.set(updated);
      }
    });
  }

  login(email: string, password: string) {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
      tap(res => {
        localStorage.setItem(this.storageKey, JSON.stringify(res));
        localStorage.setItem('token', res.token);
        this.currentUser.set(res);
        this.plantContext.setAccessibleSites(res.accessibleSites ?? []);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    localStorage.removeItem('token');
    this.currentUser.set(null);
    this.plantContext.selectSite(null);
    this.plantContext.accessibleSites.set([]);
    this.router.navigate(['/login']);
  }

  get token(): string | null {
    return localStorage.getItem('token');
  }

  isLoggedIn(): boolean {
    return !!this.token;
  }

  hasRole(...roles: string[]): boolean {
    const role = this.currentUser()?.role;
    return !!role && roles.includes(role);
  }

  isSuperAdmin(): boolean {
    return this.hasRole(...SUPER_ADMIN_ROLES);
  }

  canManageOrganizations(): boolean {
    return this.isSuperAdmin();
  }

  canManageUsers(): boolean {
    return this.hasRole(...PLANT_ADMIN_ROLES);
  }

  canManagePlants(): boolean {
    return this.hasRole(...COMPANY_ADMIN_ROLES);
  }

  canViewBilling(): boolean {
    return this.hasRole(...BILLING_ROLES);
  }

  showAnalytics(): boolean {
    return this.hasRole(...ANALYTICS_ROLES);
  }

  postLoginRoute(): string {
    if (this.isSuperAdmin()) return '/admin/platform';
    if (this.canManageUsers()) return '/admin/users';
    return '/home';
  }

  private loadUser(): LoginResponse | null {
    const data = localStorage.getItem(this.storageKey);
    if (!data) return null;
    try {
      return JSON.parse(data);
    } catch {
      return null;
    }
  }
}
