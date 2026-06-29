import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivityMonitorComponent } from './activity-monitor.component';
import { ActivityService, ActivityFilter } from './activity.service';
import { AuthService } from '../auth.service';
import { of } from 'rxjs';

describe('ActivityMonitorComponent', () => {
  let component: ActivityMonitorComponent;
  let fixture: ComponentFixture<ActivityMonitorComponent>;
  let activitySpy: jasmine.SpyObj<ActivityService>;
  let authSpy: jasmine.SpyObj<AuthService>;

  const mockData = {
    items: [
      { id: 1, actionType: 'Login', description: 'Logged in', ipAddress: '10.0.0.1', occurredAt: '2026-01-01T00:00:00Z' },
      { id: 2, actionType: 'Logout', description: 'Logged out', ipAddress: '10.0.0.1', occurredAt: '2026-01-02T00:00:00Z' }
    ],
    totalCount: 2,
    page: 1,
    pageSize: 25
  };

  beforeEach(async () => {
    activitySpy = jasmine.createSpyObj('ActivityService', [
      'list', 'exportCsv', 'startPolling', 'stopPolling', 'buildDetailHtml'
    ]);
    activitySpy.list.and.returnValue(of(mockData));
    activitySpy.startPolling.and.callFake((_filter, cb) => cb(mockData));
    activitySpy.buildDetailHtml.and.callFake((item) => `<strong>${item.actionType}</strong>: ${item.description}`);

    authSpy = jasmine.createSpyObj('AuthService', ['getUsername']);
    authSpy.getUsername.and.returnValue('testuser');

    await TestBed.configureTestingModule({
      imports: [ActivityMonitorComponent],
      providers: [
        { provide: ActivityService, useValue: activitySpy },
        { provide: AuthService, useValue: authSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ActivityMonitorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display username from auth service', () => {
    expect(component.username).toBe('testuser');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('testuser');
  });

  it('should call service list on init', () => {
    expect(activitySpy.list).toHaveBeenCalledWith(component.filter);
  });

  it('should render activity items', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('.row');
    expect(rows.length).toBe(2);
  });

  it('should call exportCsv on export', () => {
    component.onExport();
    expect(activitySpy.exportCsv).toHaveBeenCalled();
  });

  it('should decrement page on prevPage', () => {
    component.filter.page = 3;
    component.prevPage();
    expect(component.filter.page).toBe(2);
  });

  it('should not decrement page below 1', () => {
    component.filter.page = 1;
    component.prevPage();
    expect(component.filter.page).toBe(1);
  });

  it('should increment page on nextPage', () => {
    component.filter.page = 1;
    component.nextPage();
    expect(component.filter.page).toBe(2);
  });

  it('should reload on page change', () => {
    activitySpy.list.calls.reset();
    component.nextPage();
    expect(activitySpy.list).toHaveBeenCalled();
  });
});
