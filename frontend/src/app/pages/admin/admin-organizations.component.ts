import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { Organization } from '../../models';

@Component({
  selector: 'app-admin-organizations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-organizations.component.html',
  styleUrl: './admin-organizations.component.scss'
})
export class AdminOrganizationsComponent implements OnInit {
  orgs = signal<Organization[]>([]);
  loading = signal(true);
  showForm = signal(false);
  error = signal('');

  form = {
    name: '',
    code: '',
    contactEmail: '',
    subscriptionPlan: 'Standard',
    maxPlants: 10
  };

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getOrganizations().subscribe({
      next: o => { this.orgs.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    this.error.set('');
    this.api.createOrganization(this.form).subscribe({
      next: () => {
        this.showForm.set(false);
        this.form = { name: '', code: '', contactEmail: '', subscriptionPlan: 'Standard', maxPlants: 10 };
        this.load();
      },
      error: () => this.error.set('Could not create organization. Code may already exist.')
    });
  }

  toggleActive(org: Organization): void {
    this.api.updateOrganization(org.id, { isActive: !org.isActive }).subscribe(() => this.load());
  }
}
