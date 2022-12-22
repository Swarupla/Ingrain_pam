import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CompletionCertificateComponent } from './completion-certificate.component';

describe('CompletionCertificateComponent', () => {
  let component: CompletionCertificateComponent;
  let fixture: ComponentFixture<CompletionCertificateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CompletionCertificateComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CompletionCertificateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
