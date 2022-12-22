import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfigureCertificationFlagComponent } from './configure-certification-flag.component';

describe('ConfigureCertificationFlagComponent', () => {
  let component: ConfigureCertificationFlagComponent;
  let fixture: ComponentFixture<ConfigureCertificationFlagComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ConfigureCertificationFlagComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ConfigureCertificationFlagComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
