import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FoundationDashboard } from './foundation-dashboard';

describe('FoundationDashboard', () => {
  let fixture: ComponentFixture<FoundationDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FoundationDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(FoundationDashboard);
    fixture.detectChanges();
  });

  it('shows the phase foundation capabilities', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelectorAll('li')).toHaveLength(4);
    expect(element.querySelector('h1')?.textContent).toContain('Foundation');
  });
});
