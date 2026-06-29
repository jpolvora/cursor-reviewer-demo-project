import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuditComponent } from './audit.component';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { of } from 'rxjs';

describe('AuditComponent', () => {
  let component: AuditComponent;
  let fixture: ComponentFixture<AuditComponent>;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;
  let authSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    authSpy = jasmine.createSpyObj('AuthService', ['logout']);
    authSpy.logout.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [AuditComponent, HttpClientTestingModule],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AuditComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    component.ngOnInit();
    const req = httpMock.expectOne('/api/audit?page=1&pageSize=50');
    req.flush({ total: 0, page: 1, pageSize: 50, data: [] });
    expect(component).toBeTruthy();
  });

  it('should request /api/audit with pagination params', () => {
    component.ngOnInit();
    const req = httpMock.expectOne('/api/audit?page=1&pageSize=50');
    expect(req.request.method).toBe('GET');
    req.flush({ total: 2, page: 1, pageSize: 50, data: [] });
  });

  it('should append action filter to query string', () => {
    component.ngOnInit();
    let req = httpMock.expectOne('/api/audit?page=1&pageSize=50');
    req.flush({ total: 0, page: 1, pageSize: 50, data: [] });

    component.filterAction = 'login';
    component.loadLogs();
    req = httpMock.expectOne('/api/audit?page=1&pageSize=50&action=login');
    expect(req.request.method).toBe('GET');
    req.flush({ total: 0, page: 1, pageSize: 50, data: [] });
  });

  it('should navigate to dashboard onBack', () => {
    component.ngOnInit();
    const req = httpMock.expectOne('/api/audit?page=1&pageSize=50');
    req.flush({ total: 0, page: 1, pageSize: 50, data: [] });

    component.onBack();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should redirect to login on 401 error', () => {
    component.ngOnInit();
    const req = httpMock.expectOne('/api/audit?page=1&pageSize=50');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(authSpy.logout).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });
});
