import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TodoTask {
  id: number;
  userId: number;
  title: string;
  description: string;
  status: 'Todo' | 'InProgress' | 'Done';
  priority: 'Low' | 'Medium' | 'High';
  createdAt: string;
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class TodoService {
  private apiUrl = '/api/todos';

  constructor(private http: HttpClient) {}

  getTasks(): Observable<TodoTask[]> {
    return this.http.get<TodoTask[]>(this.apiUrl);
  }

  createTask(task: { title: string; description?: string; status?: string; priority?: string }): Observable<TodoTask> {
    return this.http.post<TodoTask>(this.apiUrl, task);
  }

  updateTask(id: number, task: Partial<TodoTask>): Observable<TodoTask> {
    return this.http.put<TodoTask>(`${this.apiUrl}/${id}`, task);
  }

  deleteTask(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}
