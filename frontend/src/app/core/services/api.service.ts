import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminUser, AnalyticsSummary, Asset, BillingSummary, CleaningSchedule, CompanyDashboard,
  DashboardSummary, EnergyLoss, Fault, Notification, OperationsEfficiency, Organization,
  PlatformStats, ReportSummary, ScadaSnapshot, SiteSummary, UserProfile, WorkTask, ZoneSoiling
} from '../../models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getDashboard(siteId?: number) {
    let params = new HttpParams();
    if (siteId) params = params.set('siteId', siteId);
    return this.http.get<DashboardSummary>(`${this.base}/dashboard/summary`, { params });
  }

  getFaults(siteId?: number, status?: string) {
    let params = new HttpParams();
    if (siteId) params = params.set('siteId', siteId);
    if (status) params = params.set('status', status);
    return this.http.get<Fault[]>(`${this.base}/faults`, { params });
  }

  getFault(id: number) {
    return this.http.get<Fault>(`${this.base}/faults/${id}`);
  }

  updateFault(id: number, body: object) {
    return this.http.put<Fault>(`${this.base}/faults/${id}`, body);
  }

  runPrediction(siteId: number) {
    return this.http.post<Fault[]>(`${this.base}/faults/predict/${siteId}`, {});
  }

  getTasks(siteId?: number, status?: string) {
    let params = new HttpParams();
    if (siteId) params = params.set('siteId', siteId);
    if (status) params = params.set('status', status);
    return this.http.get<WorkTask[]>(`${this.base}/tasks`, { params });
  }

  updateTask(id: number, body: object) {
    return this.http.put<WorkTask>(`${this.base}/tasks/${id}`, body);
  }

  getCleaningSchedules(siteId?: number) {
    let params = new HttpParams();
    if (siteId) params = params.set('siteId', siteId);
    return this.http.get<CleaningSchedule[]>(`${this.base}/cleaning/schedules`, { params });
  }

  getSoiling(siteId: number) {
    return this.http.get<ZoneSoiling[]>(`${this.base}/cleaning/soiling/${siteId}`);
  }

  getAnalytics(siteId: number, days = 30) {
    const params = new HttpParams().set('days', days);
    return this.http.get<AnalyticsSummary>(`${this.base}/analytics/${siteId}`, { params });
  }

  getProfile() {
    return this.http.get<UserProfile>(`${this.base}/users/profile`);
  }

  getNotifications() {
    return this.http.get<Notification[]>(`${this.base}/users/notifications`);
  }

  markNotificationRead(id: number) {
    return this.http.put(`${this.base}/users/notifications/${id}/read`, {});
  }

  getPlatformStats() {
    return this.http.get<PlatformStats>(`${this.base}/organizations/stats`);
  }

  getOrganizations() {
    return this.http.get<Organization[]>(`${this.base}/organizations`);
  }

  createOrganization(body: object) {
    return this.http.post<Organization>(`${this.base}/organizations`, body);
  }

  updateOrganization(id: number, body: object) {
    return this.http.put<Organization>(`${this.base}/organizations/${id}`, body);
  }

  getAdminUsers(organizationId?: number) {
    let params = new HttpParams();
    if (organizationId) params = params.set('organizationId', organizationId);
    return this.http.get<AdminUser[]>(`${this.base}/admin/users`, { params });
  }

  createUser(body: object) {
    return this.http.post<AdminUser>(`${this.base}/admin/users`, body);
  }

  updateUser(id: number, body: object) {
    return this.http.put<AdminUser>(`${this.base}/admin/users/${id}`, body);
  }

  getPlants() {
    return this.http.get<SiteSummary[]>(`${this.base}/admin/plants`);
  }

  createPlant(body: object) {
    return this.http.post<SiteSummary>(`${this.base}/admin/plants`, body);
  }

  getAssets(siteId?: number) {
    let params = new HttpParams();
    if (siteId) params = params.set('siteId', siteId);
    return this.http.get<Asset[]>(`${this.base}/assets`, { params });
  }

  getScada(siteId: number) {
    return this.http.get<ScadaSnapshot>(`${this.base}/scada/${siteId}`);
  }

  getCompanyDashboard() {
    return this.http.get<CompanyDashboard>(`${this.base}/company/dashboard`);
  }

  getOperationsEfficiency() {
    return this.http.get<OperationsEfficiency>(`${this.base}/company/operations-efficiency`);
  }

  getEnergyLoss(siteId?: number) {
    let params = new HttpParams();
    if (siteId) params = params.set('siteId', siteId);
    return this.http.get<EnergyLoss>(`${this.base}/company/energy-loss`, { params });
  }

  getReports(siteId?: number) {
    let params = new HttpParams();
    if (siteId) params = params.set('siteId', siteId);
    return this.http.get<ReportSummary[]>(`${this.base}/reports`, { params });
  }

  getBillingSummary() {
    return this.http.get<BillingSummary>(`${this.base}/billing/summary`);
  }

  acknowledgeFault(id: number) {
    return this.http.post<Fault>(`${this.base}/faults/${id}/acknowledge`, {});
  }

  escalateFault(id: number) {
    return this.http.post<Fault>(`${this.base}/faults/${id}/escalate`, {});
  }

  createTaskFromFault(faultId: number, assignedToUserId?: number) {
    return this.http.post<WorkTask>(`${this.base}/tasks/from-fault/${faultId}`, { faultId, assignedToUserId });
  }
}
