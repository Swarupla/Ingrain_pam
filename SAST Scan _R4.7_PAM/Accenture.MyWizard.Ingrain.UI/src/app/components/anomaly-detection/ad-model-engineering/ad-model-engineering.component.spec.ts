import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdModelEngineeringComponent } from './ad-model-engineering.component';

describe('AdModelEngineeringComponent', () => {
  let component: AdModelEngineeringComponent;
  let fixture: ComponentFixture<AdModelEngineeringComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdModelEngineeringComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdModelEngineeringComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
