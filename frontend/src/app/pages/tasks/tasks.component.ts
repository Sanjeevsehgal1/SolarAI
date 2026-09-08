import { Component, OnInit, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { WorkTask } from '../../models';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.scss'
})
export class TasksComponent implements OnInit {
  tasks = signal<WorkTask[]>([]);
  filter = signal('all');
  loading = signal(true);

  constructor(private api: ApiService, private plantContext: PlantContextService) {
    effect(() => {
      const siteId = this.plantContext.selectedSiteId();
      if (this.plantContext.accessibleSites().length === 0) return;
      this.loadTasks(siteId ?? undefined);
    });
  }

  ngOnInit(): void {}

  loadTasks(siteId?: number): void {
    this.loading.set(true);
    const status = this.filter() === 'all' ? undefined : this.filter();
    this.api.getTasks(siteId, status).subscribe({
      next: data => { this.tasks.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  setFilter(f: string): void {
    this.filter.set(f);
    this.loading.set(true);
    this.loadTasks(this.plantContext.selectedSiteId() ?? undefined);
  }

  startTask(task: WorkTask): void {
    this.api.updateTask(task.id, { status: 'InProgress' }).subscribe(updated => {
      this.tasks.update(list => list.map(t => t.id === updated.id ? updated : t));
    });
  }

  completeTask(task: WorkTask): void {
    this.api.updateTask(task.id, { status: 'Done' }).subscribe(updated => {
      this.tasks.update(list => list.map(t => t.id === updated.id ? updated : t));
    });
  }

  statusClass(status: string): string {
    return { Open: 'st-open', InProgress: 'st-progress', Done: 'st-done' }[status] ?? '';
  }
}
