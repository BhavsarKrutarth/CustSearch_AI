import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AccessDeniedPage } from './access-denied-page';

describe('AccessDeniedPage', () => {
  it('explains the 403 and provides a safe sign-in destination', () => {
    TestBed.configureTestingModule({ imports: [AccessDeniedPage], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(AccessDeniedPage);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Access denied');
    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe('/login');
  });
});
