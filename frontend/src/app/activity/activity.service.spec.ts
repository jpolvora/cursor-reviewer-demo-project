import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivityService, ActivityFilter } from './activity.service';
import { AuthService } from '../auth.service';

describe('ActivityService', () => {
  let service: ActivityService;
  let httpMock: HttpTestingController;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken']);
    authServiceSpy.getToken.and.returnValue('test-token');

    TestBed.configureTestingModule({
      providers: [
        ActivityService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    });

    service = TestBed.inject(ActivityService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('list', () => {
    it('should send GET with default params', () => {
      const filter: ActivityFilter = { page: 1, pageSize: 25 };

      service.list(filter).subscribe();

      const req = httpMock.expectOne('/api/activity?page=1&pageSize=25');
      expect(req.request.method).toBe('GET');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 25 });
    });

    it('should send GET with optional params', () => {
      const filter: ActivityFilter = {
        page: 2,
        pageSize: 10,
        actionType: 'Login',
        from: '2026-01-01',
        to: '2026-06-01',
        search: 'test',
        userId: 1
      };

      service.list(filter).subscribe();

      const req = httpMock.expectOne(
        '/api/activity?page=2&pageSize=10&actionType=Login&from=2026-01-01&to=2026-06-01&search=test&userId=1'
      );
      expect(req.request.method).toBe('GET');
      req.flush({ items: [], totalCount: 0, page: 2, pageSize: 10 });
    });
  });

  describe('record', () => {
    it('should POST to /api/activity/record', () => {
      service.record('Login', 'User logged in', '{}').subscribe();

      const req = httpMock.expectOne('/api/activity/record');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        actionType: 'Login',
        description: 'User logged in',
        metadata: '{}'
      });
      req.flush({ message: 'Activity recorded.' });
    });
  });

  describe('formatActionLabel', () => {
    it('should insert spaces before capitals', () => {
      expect(service.formatActionLabel('ProfileUpdate')).toBe('Profile Update');
      expect(service.formatActionLabel('Login')).toBe('Login');
      expect(service.formatActionLabel('PasswordChange')).toBe('Password Change');
    });
  });

  describe('buildDetailHtml', () => {
    it('should include metadata as small tag when present', () => {
      const item = {
        id: 1,
        actionType: 'Login',
        description: 'User logged in',
        ipAddress: '127.0.0.1',
        occurredAt: '2026-01-01T00:00:00Z',
        metadata: '{"ip":"127.0.0.1"}'
      };

      const html = service.buildDetailHtml(item);
      expect(html).toContain('<strong>Login</strong>');
      expect(html).toContain('<small>{"ip":"127.0.0.1"}</small>');
    });

    it('should omit metadata small tag when absent', () => {
      const item = {
        id: 1,
        actionType: 'Logout',
        description: 'User logged out',
        ipAddress: '127.0.0.1',
        occurredAt: '2026-01-01T00:00:00Z'
      };

      const html = service.buildDetailHtml(item);
      expect(html).toContain('<strong>Logout</strong>');
      expect(html).not.toContain('<small>');
    });
  });

  describe('startPolling / stopPolling', () => {
    it('should start and stop polling', () => {
      spyOn(window, 'setInterval').and.callThrough();
      spyOn(window, 'clearInterval').and.callThrough();

      const filter: ActivityFilter = { page: 1, pageSize: 25 };
      const callback = jasmine.createSpy();

      service.startPolling(filter, callback);
      expect(setInterval).toHaveBeenCalled();

      const req = httpMock.expectOne('/api/activity?page=1&pageSize=25');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 25 });

      expect(callback).toHaveBeenCalled();

      service.stopPolling();
      expect(clearInterval).toHaveBeenCalled();
    });
  });

  describe('exportCsv', () => {
    it('should open export URL with token in query', () => {
      spyOn(window, 'open');

      service.exportCsv('2026-01-01', '2026-06-01');

      expect(window.open).toHaveBeenCalledWith(
        'http://localhost:5000/api/activity/export?from=2026-01-01&to=2026-06-01&token=test-token',
        '_blank'
      );
    });

    it('should open export URL without optional dates', () => {
      spyOn(window, 'open');

      service.exportCsv();

      expect(window.open).toHaveBeenCalledWith(
        'http://localhost:5000/api/activity/export?token=test-token',
        '_blank'
      );
    });
  });
});
