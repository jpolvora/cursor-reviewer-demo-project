import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TodoService, TodoTask } from './todo.service';

@Component({
  selector: 'app-todo-board',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="board-container">
      <div class="board-card">
        <div class="board-header">
          <div class="title-section">
            <h1>Developer Task Board</h1>
            <p class="subtitle">Organize and track your development tasks.</p>
          </div>
          <button (click)="goBack()" class="btn-back">Back to Dashboard</button>
        </div>

        <!-- Add Task Form -->
        <form (ngSubmit)="onCreateTask()" class="add-task-form">
          <div class="form-row">
            <input 
              type="text" 
              name="title" 
              [(ngModel)]="newTaskTitle" 
              placeholder="Task Title..." 
              required 
              class="form-control title-input"
            />
            <select name="priority" [(ngModel)]="newTaskPriority" class="form-control priority-select">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
            <button type="submit" [disabled]="!newTaskTitle.trim()" class="btn-add">Add Task</button>
          </div>
          <textarea 
            name="description" 
            [(ngModel)]="newTaskDescription" 
            placeholder="Optional Description..." 
            class="form-control desc-input"
            rows="2"
          ></textarea>
        </form>

        <!-- Board Columns -->
        <div class="columns-grid">
          <!-- TODO COLUMN -->
          <div class="column">
            <div class="column-header todo-hdr">
              <h2>To Do ({{ getColumnTasks('Todo').length }})</h2>
            </div>
            <div class="task-list">
              <div *ngFor="let task of getColumnTasks('Todo')" class="task-item" [ngClass]="task.priority.toLowerCase()">
                <div class="task-card-header">
                  <span class="priority-badge" [ngClass]="task.priority.toLowerCase()">{{ task.priority }}</span>
                  <div class="actions">
                    <button (click)="moveTask(task, 'InProgress')" title="Start Task" class="action-btn">➔</button>
                    <button (click)="deleteTask(task.id)" title="Delete" class="delete-btn">✕</button>
                  </div>
                </div>
                <h3>{{ task.title }}</h3>
                <p *ngIf="task.description" class="task-desc">{{ task.description }}</p>
                <div class="task-footer">
                  <span class="date">{{ task.createdAt | date:'shortTime' }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- IN PROGRESS COLUMN -->
          <div class="column">
            <div class="column-header progress-hdr">
              <h2>In Progress ({{ getColumnTasks('InProgress').length }})</h2>
            </div>
            <div class="task-list">
              <div *ngFor="let task of getColumnTasks('InProgress')" class="task-item" [ngClass]="task.priority.toLowerCase()">
                <div class="task-card-header">
                  <span class="priority-badge" [ngClass]="task.priority.toLowerCase()">{{ task.priority }}</span>
                  <div class="actions">
                    <button (click)="moveTask(task, 'Todo')" title="Move to Todo" class="action-btn prev-btn">➔</button>
                    <button (click)="moveTask(task, 'Done')" title="Complete Task" class="action-btn">➔</button>
                    <button (click)="deleteTask(task.id)" title="Delete" class="delete-btn">✕</button>
                  </div>
                </div>
                <h3>{{ task.title }}</h3>
                <p *ngIf="task.description" class="task-desc">{{ task.description }}</p>
                <div class="task-footer">
                  <span class="date">{{ task.createdAt | date:'shortTime' }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- DONE COLUMN -->
          <div class="column">
            <div class="column-header done-hdr">
              <h2>Done ({{ getColumnTasks('Done').length }})</h2>
            </div>
            <div class="task-list">
              <div *ngFor="let task of getColumnTasks('Done')" class="task-item" [ngClass]="task.priority.toLowerCase()">
                <div class="task-card-header">
                  <span class="priority-badge" [ngClass]="task.priority.toLowerCase()">{{ task.priority }}</span>
                  <div class="actions">
                    <button (click)="moveTask(task, 'InProgress')" title="Re-open Task" class="action-btn prev-btn">➔</button>
                    <button (click)="deleteTask(task.id)" title="Delete" class="delete-btn">✕</button>
                  </div>
                </div>
                <h3>{{ task.title }}</h3>
                <p *ngIf="task.description" class="task-desc">{{ task.description }}</p>
                <div class="task-footer">
                  <span class="date">{{ task.createdAt | date:'shortTime' }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .board-container {
      display: flex;
      justify-content: center;
      align-items: flex-start;
      min-height: 100vh;
      background: linear-gradient(135deg, #0b0f19 0%, #111827 100%);
      font-family: 'Inter', sans-serif;
      padding: 2rem;
      color: #f3f4f6;
    }
    .board-card {
      width: 100%;
      max-width: 1200px;
      background: rgba(17, 24, 39, 0.7);
      backdrop-filter: blur(20px);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 24px;
      padding: 2rem;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
    }
    .board-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1);
      padding-bottom: 1rem;
    }
    .title-section h1 {
      margin: 0;
      font-size: 2rem;
      background: linear-gradient(90deg, #38bdf8, #818cf8);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      font-weight: 700;
    }
    .subtitle {
      margin: 0.25rem 0 0 0;
      color: #9ca3af;
      font-size: 0.95rem;
    }
    .btn-back {
      padding: 0.5rem 1.25rem;
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 10px;
      color: #cbd5e1;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }
    .btn-back:hover {
      background: rgba(255, 255, 255, 0.1);
      color: #fff;
    }
    .add-task-form {
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid rgba(255, 255, 255, 0.05);
      border-radius: 16px;
      padding: 1.25rem;
      margin-bottom: 2.5rem;
    }
    .form-row {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.75rem;
    }
    .form-control {
      background: rgba(0, 0, 0, 0.3);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 10px;
      color: #fff;
      padding: 0.65rem 1rem;
      font-size: 0.95rem;
      transition: border-color 0.2s ease;
    }
    .form-control:focus {
      outline: none;
      border-color: #6366f1;
    }
    .title-input {
      flex: 1;
    }
    .priority-select {
      width: 120px;
    }
    .desc-input {
      width: 100%;
      resize: none;
    }
    .btn-add {
      background: linear-gradient(90deg, #3b82f6, #6366f1);
      border: none;
      color: white;
      font-weight: 600;
      padding: 0 1.5rem;
      border-radius: 10px;
      cursor: pointer;
      transition: opacity 0.2s ease;
    }
    .btn-add:hover {
      opacity: 0.9;
    }
    .btn-add:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .columns-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1.5rem;
    }
    .column {
      background: rgba(0, 0, 0, 0.15);
      border-radius: 16px;
      padding: 1rem;
      border: 1px solid rgba(255, 255, 255, 0.03);
      display: flex;
      flex-direction: column;
      min-height: 450px;
    }
    .column-header {
      margin-bottom: 1rem;
      padding-bottom: 0.5rem;
      border-bottom: 2px solid transparent;
    }
    .column-header h2 {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 600;
      letter-spacing: 0.05em;
    }
    .todo-hdr { border-bottom-color: #3b82f6; color: #60a5fa; }
    .progress-hdr { border-bottom-color: #f59e0b; color: #fbbf24; }
    .done-hdr { border-bottom-color: #10b981; color: #34d399; }
    
    .task-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      flex-grow: 1;
    }
    .task-item {
      background: rgba(255, 255, 255, 0.03);
      border: 1px solid rgba(255, 255, 255, 0.05);
      border-radius: 12px;
      padding: 1rem;
      transition: all 0.2s ease;
      position: relative;
    }
    .task-item:hover {
      transform: translateY(-2px);
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.3);
      border-color: rgba(255, 255, 255, 0.1);
    }
    .task-item.high { border-left: 4px solid #ef4444; }
    .task-item.medium { border-left: 4px solid #f59e0b; }
    .task-item.low { border-left: 4px solid #3b82f6; }

    .task-card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }
    .priority-badge {
      font-size: 0.7rem;
      text-transform: uppercase;
      font-weight: 700;
      padding: 0.15rem 0.5rem;
      border-radius: 4px;
    }
    .priority-badge.high { background: rgba(239, 68, 68, 0.15); color: #fca5a5; }
    .priority-badge.medium { background: rgba(245, 158, 11, 0.15); color: #fde047; }
    .priority-badge.low { background: rgba(59, 130, 246, 0.15); color: #93c5fd; }

    .actions {
      display: flex;
      gap: 0.5rem;
    }
    .action-btn {
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      color: #fff;
      font-size: 0.75rem;
      width: 22px;
      height: 22px;
      border-radius: 4px;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .action-btn:hover { background: #6366f1; border-color: #6366f1; }
    .action-btn.prev-btn { transform: rotate(180deg); }
    .delete-btn {
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.2);
      color: #f87171;
      font-size: 0.7rem;
      width: 22px;
      height: 22px;
      border-radius: 4px;
      cursor: pointer;
    }
    .delete-btn:hover { background: #ef4444; color: #fff; }

    .task-item h3 {
      margin: 0 0 0.5rem 0;
      font-size: 1rem;
      font-weight: 600;
    }
    .task-desc {
      margin: 0;
      font-size: 0.85rem;
      color: #9ca3af;
      line-height: 1.4;
    }
    .task-footer {
      margin-top: 0.75rem;
      display: flex;
      justify-content: flex-end;
      font-size: 0.7rem;
      color: #6b7280;
    }
  `]
})
export class TodoBoardComponent implements OnInit {
  tasks: TodoTask[] = [];
  newTaskTitle = '';
  newTaskDescription = '';
  newTaskPriority: 'Low' | 'Medium' | 'High' = 'Medium';

  constructor(private todoService: TodoService, private router: Router) {}

  ngOnInit() {
    this.loadTasks();
  }

  loadTasks() {
    this.todoService.getTasks().subscribe({
      next: (data) => this.tasks = data,
      error: (err) => console.error('Failed to load tasks', err)
    });
  }

  getColumnTasks(status: 'Todo' | 'InProgress' | 'Done'): TodoTask[] {
    return this.tasks.filter(t => t.status === status);
  }

  onCreateTask() {
    if (!this.newTaskTitle.trim()) return;

    this.todoService.createTask({
      title: this.newTaskTitle,
      description: this.newTaskDescription,
      priority: this.newTaskPriority,
      status: 'Todo'
    }).subscribe({
      next: (task) => {
        this.tasks.unshift(task);
        this.newTaskTitle = '';
        this.newTaskDescription = '';
        this.newTaskPriority = 'Medium';
      },
      error: (err) => console.error('Failed to create task', err)
    });
  }

  moveTask(task: TodoTask, newStatus: 'Todo' | 'InProgress' | 'Done') {
    this.todoService.updateTask(task.id, { status: newStatus }).subscribe({
      next: (updated) => {
        const index = this.tasks.findIndex(t => t.id === task.id);
        if (index !== -1) {
          this.tasks[index] = updated;
        }
      },
      error: (err) => console.error('Failed to move task', err)
    });
  }

  deleteTask(id: number) {
    this.todoService.deleteTask(id).subscribe({
      next: () => {
        this.tasks = this.tasks.filter(t => t.id !== id);
      },
      error: (err) => console.error('Failed to delete task', err)
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
