import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { BillingSummary } from '../../models';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './billing.component.html',
  styleUrl: './billing.component.scss'
})
export class BillingComponent implements OnInit {
  billing = signal<BillingSummary | null>(null);
  loading = signal(true);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getBillingSummary().subscribe({
      next: data => { this.billing.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
