import { Routes } from '@angular/router';
import { authGuard, guestGuard, superAdminGuard, adminGuard } from './core/guards/auth.guard';
import { LayoutComponent } from './layout/layout.component';
import { LoginComponent } from './pages/login/login.component';
import { HomeComponent } from './pages/home/home.component';
import { FaultsComponent } from './pages/faults/faults.component';
import { TasksComponent } from './pages/tasks/tasks.component';
import { CleaningComponent } from './pages/cleaning/cleaning.component';
import { AnalyticsComponent } from './pages/analytics/analytics.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { AdminPlatformComponent } from './pages/admin/admin-platform.component';
import { AdminOrganizationsComponent } from './pages/admin/admin-organizations.component';
import { AdminUsersComponent } from './pages/admin/admin-users.component';
import { AdminPlantsComponent } from './pages/admin/admin-plants.component';
import { AssetsComponent } from './pages/assets/assets.component';
import { ScadaComponent } from './pages/scada/scada.component';
import { ReportsComponent } from './pages/reports/reports.component';
import { BillingComponent } from './pages/billing/billing.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent },
      { path: 'faults', component: FaultsComponent },
      { path: 'tasks', component: TasksComponent },
      { path: 'cleaning', component: CleaningComponent },
      { path: 'assets', component: AssetsComponent },
      { path: 'scada', component: ScadaComponent },
      { path: 'analytics', component: AnalyticsComponent },
      { path: 'reports', component: ReportsComponent },
      { path: 'billing', component: BillingComponent, canActivate: [adminGuard] },
      { path: 'profile', component: ProfileComponent },
      { path: 'admin/platform', component: AdminPlatformComponent, canActivate: [superAdminGuard] },
      { path: 'admin/organizations', component: AdminOrganizationsComponent, canActivate: [superAdminGuard] },
      { path: 'admin/plants', component: AdminPlantsComponent, canActivate: [adminGuard] },
      { path: 'admin/users', component: AdminUsersComponent, canActivate: [adminGuard] }
    ]
  },
  { path: '**', redirectTo: 'home' }
];
